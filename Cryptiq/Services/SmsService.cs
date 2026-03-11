using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Microsoft.Extensions.Configuration;

public class SmsService
{
    public SmsService()
    {
        var accountSid = Environment.GetEnvironmentVariable("TWILIO_ACCOUNT_SID");
        var authToken = Environment.GetEnvironmentVariable("TWILIO_AUTH_TOKEN");

        TwilioClient.Init(accountSid, authToken);
    }

    public async Task SendSmsAsync(string toPhone, string message)
    {
        await MessageResource.CreateAsync(
            body: message,
            from: new Twilio.Types.PhoneNumber(Environment.GetEnvironmentVariable("TWILIO_FROM_PHONE")),
            to: new Twilio.Types.PhoneNumber(toPhone)
        );
    }
}

