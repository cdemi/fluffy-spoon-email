using System;
using System.Diagnostics;
using System.Net;
using System.Net.Mail;
using Orleans;
using Orleans.Streams;
using System.Threading.Tasks;
using demofluffyspoon.contracts;
using demofluffyspoon.contracts.Grains;
using demofluffyspoon.contracts.Models;
using fluffyspoon.email.ViewModels;
using Microsoft.Extensions.Options;

namespace fluffyspoon.email.Grains
{
    [ImplicitStreamSubscription(nameof(UserVerifiedEvent))]
    public class EmailSenderGrain : Grain, IEmailGrain, IAsyncObserver<UserVerifiedEvent>
    {
        private readonly SmtpSettings _smtpSettings;
        private IAsyncStream<EmailSentEvent> _emailSentStream;

        public EmailSenderGrain(IOptions<SmtpSettings> smtpSettings)
        {
            _smtpSettings = smtpSettings.Value;
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
            SmtpClient smtpClient = new SmtpClient(_smtpSettings.Hostname)
            {
                Credentials = new NetworkCredential(_smtpSettings.Username, _smtpSettings.Password)
            };

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            smtpClient.Send("test@librenms.99bits.net", item.Email, "User Verified Successfully", "Congratulations, your user has been verified");
            stopwatch.Stop();
            
            await _emailSentStream.OnNextAsync(new EmailSentEvent()
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