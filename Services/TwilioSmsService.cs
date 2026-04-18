using AIReception.Mvc.Models.Sms;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace AIReception.Mvc.Services;

public class TwilioSmsService
{
    private readonly RuntimeRulesService _runtimeRules;
    private readonly SmsConversationService _conversations;
    private readonly DmsCoreService _dmsCore;
    private readonly ILogger<TwilioSmsService> _logger;

    public TwilioSmsService(
        RuntimeRulesService runtimeRules,
        SmsConversationService conversations,
        DmsCoreService dmsCore,
        ILogger<TwilioSmsService> logger)
    {
        _runtimeRules = runtimeRules;
        _conversations = conversations;
        _dmsCore = dmsCore;
        _logger = logger;
    }

    public async Task<SmsMessageRecord> SendAsync(SmsSendRequest request)
    {
        var accountSid = GetRequiredEnvironmentVariable("TWILIO_ACCOUNT_SID");
        var authToken = GetRequiredEnvironmentVariable("TWILIO_AUTH_TOKEN");
        var from = Environment.GetEnvironmentVariable("TWILIO_PHONE_NUMBER")
            ?? Environment.GetEnvironmentVariable("TWILIO_VOICE_NUMBER")
            ?? throw new InvalidOperationException("Missing required environment variable: TWILIO_PHONE_NUMBER or TWILIO_VOICE_NUMBER");

        var to = NormalizePhoneNumber(request.To);
        var body = (request.Message ?? "").Trim();

        if (string.IsNullOrWhiteSpace(to) || string.IsNullOrWhiteSpace(body))
        {
            throw new InvalidOperationException("A valid destination number and message are required.");
        }

        TwilioClient.Init(accountSid, authToken);

        var msg = await MessageResource.CreateAsync(
            to: new PhoneNumber(to),
            from: new PhoneNumber(from),
            body: body
        );

        var record = new SmsMessageRecord
        {
            Id = $"sms-out-{msg.Sid}",
            Type = "sms-reply",
            MessageSid = msg.Sid ?? "",
            From = msg.From?.ToString() ?? from,
            To = msg.To?.ToString() ?? to,
            Body = body,
            Status = msg.Status?.ToString() ?? "sent",
            RoutedDepartment = request.Department ?? "sms",
            DetectedIntent = "sms-outbound",
            Language = _runtimeRules.DetectLanguage(body),
            CreatedAt = DateTime.UtcNow.ToString("O"),
            UpdatedAt = DateTime.UtcNow.ToString("O")
        };

        _conversations.Add(record);
        _dmsCore.RecordSmsMessage(record);
        return record;
    }

    public async Task<SmsMessageRecord?> HandleInboundAsync(string? from, string? to, string? body, string? messageSid)
    {
        var text = (body ?? "").Trim();
        var normalizedFrom = NormalizePhoneNumber(from);
        var normalizedTo = NormalizePhoneNumber(to);

        var inboundRecord = new SmsMessageRecord
        {
            Id = $"sms-in-{messageSid}",
            Type = "sms",
            MessageSid = messageSid ?? "",
            From = normalizedFrom,
            To = normalizedTo,
            Body = text,
            Status = "received",
            RoutedDepartment = "sms",
            DetectedIntent = _runtimeRules.FindIntent(text)?.Id ?? "",
            Language = _runtimeRules.DetectLanguage(text),
            CreatedAt = DateTime.UtcNow.ToString("O"),
            UpdatedAt = DateTime.UtcNow.ToString("O")
        };

        _conversations.Add(inboundRecord);
        _dmsCore.RecordSmsMessage(inboundRecord);

        var autoReply = BuildAutoReply(text, inboundRecord.Language, inboundRecord.DetectedIntent);
        if (string.IsNullOrWhiteSpace(autoReply))
        {
            return null;
        }

        return await SendAsync(new SmsSendRequest
        {
            To = normalizedFrom,
            Message = autoReply,
            Department = _runtimeRules.FindIntent(text)?.Department ?? "sms",
            Source = "inbound-auto-reply"
        });
    }

    public IReadOnlyList<InboxConversationSummary> GetConversations() => _conversations.GetConversations();

    public IReadOnlyList<SmsMessageRecord> GetThread(string phone) => _conversations.GetThread(phone);

    private string BuildAutoReply(string text, string language, string detectedIntent)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return "";
        }

        if (!string.IsNullOrWhiteSpace(detectedIntent))
        {
            return language == "en-US"
                ? $"Thanks for your message. I can help with {detectedIntent.Replace("-", " ")} and connect you with the right department."
                : $"Merci pour votre message. Je peux vous aider avec {detectedIntent.Replace("-", " ")} et vous diriger vers le bon département.";
        }

        return language == "en-US"
            ? "Thanks for your message to BMW MINI Laval. An advisor will follow up shortly. If you'd like, reply with Service, Sales, or Parts."
            : "Merci pour votre message chez BMW MINI Laval. Un conseiller vous répondra sous peu. Si vous voulez, répondez Service, Ventes ou Pièces.";
    }

    private static string NormalizePhoneNumber(string? raw)
    {
        var text = (raw ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        var hasLeadingPlus = text.StartsWith("+");
        var digits = new string(text.Where(char.IsDigit).ToArray());
        if (string.IsNullOrWhiteSpace(digits)) return string.Empty;

        if (hasLeadingPlus) return $"+{digits}";
        if (digits.Length == 10) return $"+1{digits}";
        if (digits.Length == 11 && digits.StartsWith("1")) return $"+{digits}";
        return $"+{digits}";
    }

    private static string GetRequiredEnvironmentVariable(string key)
    {
        return Environment.GetEnvironmentVariable(key)
            ?? throw new InvalidOperationException($"Missing required environment variable: {key}");
    }
}
