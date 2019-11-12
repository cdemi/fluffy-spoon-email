using demofluffyspoon.contracts;
using demofluffyspoon.contracts.Grains;
using demofluffyspoon.contracts.Models;
using fluffyspoon.registration.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Streams;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace fluffyspoon.registration.Grains
{
    [ImplicitStreamSubscription(nameof(UserVerifiedEvent))]
    public class EmailSenderGrain : Grain, IEmailGrain, IAsyncObserver<UserVerifiedEvent>
    {
        private readonly SmtpOptions _smtpOptions;
        private readonly ILogger<EmailSenderGrain> _logger;

        private IAsyncStream<EmailSentEvent> _emailSentStream;

        public EmailSenderGrain(IOptions<SmtpOptions> smtpOptionsAccessor, ILogger<EmailSenderGrain> logger)
        {
            _smtpOptions = smtpOptionsAccessor.Value;
            _logger = logger;
        }

        public override async Task OnActivateAsync()
        {
            var streamProvider = GetStreamProvider(Constants.StreamProviderName);
            _emailSentStream = streamProvider.GetStream<EmailSentEvent>(this.GetPrimaryKey(), nameof(EmailSentEvent));

            var userVerifiedStream =
                streamProvider.GetStream<UserVerifiedEvent>(this.GetPrimaryKey(), nameof(UserVerifiedEvent));
            await userVerifiedStream.SubscribeAsync(this);

            await base.OnActivateAsync();
        }

        public async Task OnNextAsync(UserVerifiedEvent item, StreamSequenceToken token = null)
        {
            var smtpClient = new SmtpClient(_smtpOptions.Hostname)
            {
                Credentials = new NetworkCredential(_smtpOptions.Username, _smtpOptions.Password)
            };
            
            MailMessage mailMessage = new MailMessage();
            mailMessage.From = new MailAddress("verification@librenms.99bits.net", "Fluffy Spoon");
            mailMessage.To.Add(item.Email);
            mailMessage.Subject = "User Verified!";
            mailMessage.Body = "Congratulations, your user has been verified";

            _logger.LogInformation("Sending email to {email}", item.Email);

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            smtpClient.Send(mailMessage);
            stopwatch.Stop();

            await _emailSentStream.OnNextAsync(new EmailSentEvent
            {
                TimeTakeToSend = stopwatch.Elapsed
            });
        }

        public Task OnCompletedAsync()
        {
            return Task.CompletedTask;
        }

        public Task OnErrorAsync(Exception ex)
        {
            return Task.CompletedTask;
        }
    }
}