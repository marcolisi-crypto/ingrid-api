using AIReception.Mvc.Models.Voice;
using Twilio;
using Twilio.Jwt.AccessToken;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;
using Twilio.TwiML;
using Twilio.TwiML.Voice;

namespace AIReception.Mvc.Services;

public class TwilioVoiceService
{
    private readonly ILogger<TwilioVoiceService> _logger;

    public TwilioVoiceService(ILogger<TwilioVoiceService> logger)
    {
        _logger = logger;
    }

    public string NormalizePhoneNumber(string? raw)
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

    public VoiceTokenResponse CreateVoiceToken(string? preferredIdentity = null)
    {
        var accountSid = GetRequiredEnvironmentVariable("TWILIO_ACCOUNT_SID");
        var apiKeySid = GetRequiredEnvironmentVariable("TWILIO_API_KEY_SID");
        var apiKeySecret = GetRequiredEnvironmentVariable("TWILIO_API_KEY_SECRET");
        var twimlAppSid = GetRequiredEnvironmentVariable("TWILIO_TWIML_APP_SID");

        var identity = string.IsNullOrWhiteSpace(preferredIdentity)
            ? "reception-dock"
            : preferredIdentity.Trim();

        var token = new Token(
            accountSid,
            apiKeySid,
            apiKeySecret,
            identity: identity,
            grants: new HashSet<IGrant>
            {
                new VoiceGrant
                {
                    OutgoingApplicationSid = twimlAppSid,
                    IncomingAllow = true
                }
            }
        );

        return new VoiceTokenResponse
        {
            Token = token.ToJwt(),
            Identity = identity,
            OutboundApplicationSid = twimlAppSid
        };
    }

    public async Task<CallResource> CreateOutboundCallAsync(OutboundCallRequest request, HttpRequest httpRequest)
    {
        var accountSid = GetRequiredEnvironmentVariable("TWILIO_ACCOUNT_SID");
        var authToken = GetRequiredEnvironmentVariable("TWILIO_AUTH_TOKEN");
        var callerId = GetRequiredEnvironmentVariable("TWILIO_VOICE_NUMBER");

        var browserIdentity = string.IsNullOrWhiteSpace(request.AgentIdentity)
            ? (Environment.GetEnvironmentVariable("TWILIO_BROWSER_IDENTITY") ?? "reception-dock")
            : request.AgentIdentity.Trim();

        var normalizedTo = NormalizePhoneNumber(request.To);
        if (string.IsNullOrWhiteSpace(normalizedTo))
        {
            throw new InvalidOperationException("A valid destination number is required.");
        }

        var baseUrl = Environment.GetEnvironmentVariable("PUBLIC_BASE_URL")
            ?? $"{httpRequest.Scheme}://{httpRequest.Host.Value}";

        TwilioClient.Init(accountSid, authToken);

        var query = new List<string>
        {
            $"to={Uri.EscapeDataString(normalizedTo)}",
            $"mode={Uri.EscapeDataString(request.Mode ?? "manual-softphone")}",
            $"source={Uri.EscapeDataString(request.Source ?? "communications-dock")}",
            $"initiatedBy={Uri.EscapeDataString(request.InitiatedBy ?? "dashboard")}"
        };

        if (!string.IsNullOrWhiteSpace(request.ScriptId))
            query.Add($"scriptId={Uri.EscapeDataString(request.ScriptId)}");

        if (!string.IsNullOrWhiteSpace(request.Department))
            query.Add($"department={Uri.EscapeDataString(request.Department)}");

        if (!string.IsNullOrWhiteSpace(request.Notes))
            query.Add($"notes={Uri.EscapeDataString(request.Notes)}");

        var voiceUrl = new Uri($"{baseUrl.TrimEnd('/')}/twilio/voice/bridge?{string.Join("&", query)}");
        var statusUrl = new Uri($"{baseUrl.TrimEnd('/')}/twilio/voice/status?mode={Uri.EscapeDataString(request.Mode ?? "manual-softphone")}");

        _logger.LogInformation(
            "Creating outbound call | Mode: {Mode} | To: {To} | Identity: {Identity}",
            request.Mode,
            normalizedTo,
            browserIdentity
        );

        return await CallResource.CreateAsync(
            to: new Twilio.Types.Client(browserIdentity),
            from: new PhoneNumber(callerId),
            url: voiceUrl,
            statusCallback: statusUrl,
            statusCallbackMethod: Twilio.Http.HttpMethod.Post
        );
    }

    public string BuildBridgeTwiml(string? to, string? mode, string? scriptId)
    {
        var response = new VoiceResponse();
        var normalizedTo = NormalizePhoneNumber(to);

        if (string.IsNullOrWhiteSpace(normalizedTo))
        {
            response.Say("We were unable to determine the destination number for this call.");
            response.Hangup();
            return response.ToString();
        }

        var activeMode = (mode ?? "manual-softphone").Trim().ToLowerInvariant();
        var callerId = Environment.GetEnvironmentVariable("TWILIO_VOICE_NUMBER") ?? string.Empty;

        if (activeMode == "scripted-outbound" || activeMode == "campaign-ai")
        {
            var intro = string.IsNullOrWhiteSpace(scriptId)
                ? "Starting the scripted outbound call now."
                : $"Starting scripted outbound call for {scriptId}.";
            response.Say(intro);
        }

        var dial = new Dial(callerId: callerId, answerOnBridge: true);
        dial.Number(normalizedTo);
        response.Append(dial);

        return response.ToString();
    }

    private static string GetRequiredEnvironmentVariable(string key)
    {
        return Environment.GetEnvironmentVariable(key)
            ?? throw new InvalidOperationException($"Missing required environment variable: {key}");
    }
}
