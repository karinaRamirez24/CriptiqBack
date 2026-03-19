using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Microsoft.Extensions.Configuration;

public class SmsService
{
    private readonly string _accountSid;
    private readonly string _authToken;
    private readonly string _fromPhone;

    public SmsService(IConfiguration config)
    {
        _accountSid = config["Twilio:AccountSid"]!;
        _authToken = config["Twilio:AuthToken"]!;
        _fromPhone = config["Twilio:FromPhone"]!;

        TwilioClient.Init(_accountSid, _authToken);
    }

    public async Task SendSmsAsync(string toPhone, string message)
    {
        // ✅ Formato E.164 para México
        var formattedTo = toPhone.StartsWith("+") ? toPhone : $"+52{toPhone}";

        try
        {
            var result = await MessageResource.CreateAsync(
                body: message,
                from: new Twilio.Types.PhoneNumber(_fromPhone),
                to: new Twilio.Types.PhoneNumber(formattedTo)
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error Twilio: {ex.Message}");
            throw;
        }
    }
}

