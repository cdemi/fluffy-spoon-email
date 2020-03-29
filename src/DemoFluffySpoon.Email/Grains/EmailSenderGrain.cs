using DemoFluffySpoon.Contracts;
using DemoFluffySpoon.Contracts.Grains;
using DemoFluffySpoon.Contracts.Models;
using DemoFluffySpoon.Email.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Streams;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace DemoFluffySpoon.Email.Grains
{
    [ImplicitStreamSubscription(nameof(UserVerificationEvent))]
    public class EmailSenderGrain : Grain, IEmailGrain, IAsyncObserver<UserVerificationEvent>
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
            // Producer
            var streamProvider = GetStreamProvider(Constants.StreamProviderName);
            _emailSentStream = streamProvider.GetStream<EmailSentEvent>(this.GetPrimaryKey(), nameof(EmailSentEvent));

            // Consumer
            var userVerificationStream =
                streamProvider.GetStream<UserVerificationEvent>(this.GetPrimaryKey(), nameof(UserVerificationEvent));
            await userVerificationStream.SubscribeAsync(this);

            await base.OnActivateAsync();
        }

        public async Task OnNextAsync(UserVerificationEvent item, StreamSequenceToken token = null)
        {
            if (item.Status != UserVerificationStatusEnum.Verified)
            {
                return;
            }

            var smtpClient = CreateSmtpClient();
            var mailMessage = CreateVerificationEmail(item);

            _logger.LogInformation("Sending email to {email}", item.Email);

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            smtpClient.Send(mailMessage);
            stopwatch.Stop();

            await _emailSentStream.OnNextAsync(new EmailSentEvent {TimeTakeToSend = stopwatch.Elapsed});
        }

        private static MailMessage CreateVerificationEmail(UserVerificationEvent item)
        {
            var mailMessage = new MailMessage
            {
                From = new MailAddress("verification@librenms.99bits.net", "Fluffy Spoon"),
                Subject = "User Verified!",
                Body = "Congratulations, your user has been verified!!!11"
            };
            mailMessage.To.Add(item.Email);

            return mailMessage;
        }

        private SmtpClient CreateSmtpClient()
        {
            var smtpClient = new SmtpClient(_smtpOptions.Hostname)
            {
                Credentials = new NetworkCredential(_smtpOptions.Username, _smtpOptions.Password)
            };

            return smtpClient;
        }

        public Task OnCompletedAsync()
        {
            return Task.CompletedTask;
        }

        public Task OnErrorAsync(Exception ex)
        {
            _logger.LogError(ex, ex.Message);

            return Task.CompletedTask;
        }
    }
}