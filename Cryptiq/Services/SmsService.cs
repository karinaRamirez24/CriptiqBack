using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Microsoft.Extensions.Configuration;

public class SmsService
{
    public SmsService()
    {
        var accountSid = Environment.GetEnvironmentVariable("TWILIO_ACCOUNT_SID");
        var authToken = Environment.GetEnvironmentVariable("TWILIO_AUTH_TOKEN");
        Console.WriteLine($"Twilio Account SID: {accountSid}");
        Console.WriteLine($"Twilio Auth Token: {authToken}");
        TwilioClient.Init(accountSid, authToken);
    }

    public async Task SendSmsAsync(string toPhone, string message)
    {
        var fromPhone = Environment.GetEnvironmentVariable("TWILIO_FROM_PHONE");

        await MessageResource.CreateAsync(
            body: message,
            from: new Twilio.Types.PhoneNumber(fromPhone),
            to: new Twilio.Types.PhoneNumber(toPhone)
        );
    }

}

