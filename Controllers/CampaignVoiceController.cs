using Microsoft.AspNetCore.Mvc;
using Twilio.TwiML;
using Twilio.TwiML.Voice;
using AIReception.Mvc.Services;

namespace AIReception.Mvc.Controllers;

public class CampaignVoiceController : Controller
{
    private readonly RuntimeRulesService _runtime;
    private readonly DirectoryRoutingService _directory;
    private readonly ILogger<CampaignVoiceController> _logger;

    public CampaignVoiceController(
        RuntimeRulesService runtime,
        DirectoryRoutingService directory,
        ILogger<CampaignVoiceController> logger)
    {
        _runtime = runtime;
        _directory = directory;
        _logger = logger;
    }

    [HttpPost("/campaign/outbound")]
    [HttpGet("/campaign/outbound")]
    public IActionResult Outbound(
        [FromForm] string? script,
        [FromForm] string? first_name,
        [FromForm] string? language)
    {
        var response = new VoiceResponse();
        var baseUrl = $"{Request.Scheme}://{Request.Host}";

        var scriptValue = !string.IsNullOrWhiteSpace(script)
            ? script
            : Request.Query["script"].ToString();

        var firstNameValue = !string.IsNullOrWhiteSpace(first_name)
            ? first_name
            : Request.Query["first_name"].ToString();

        var languageValue = !string.IsNullOrWhiteSpace(language)
            ? language
            : Request.Query["language"].ToString();

        var lang = string.Equals(languageValue, "en", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(languageValue, "en-US", StringComparison.OrdinalIgnoreCase)
            ? "en-US"
            : "fr-CA";

        var voice = lang == "fr-CA" ? "Polly.Celine" : "Polly.Joanna";

        var line = string.IsNullOrWhiteSpace(scriptValue)
            ? (lang == "fr-CA"
                ? $"Bonjour{(string.IsNullOrWhiteSpace(firstNameValue) ? "" : $" {firstNameValue}")}, ici BMW MINI Laval."
                : $"Hello{(string.IsNullOrWhiteSpace(firstNameValue) ? "" : $" {firstNameValue}")}, this is BMW MINI Laval.")
            : scriptValue.Trim();

        var gather = new Gather(
            input: new List<Gather.InputEnum> { Gather.InputEnum.Speech },
            action: new Uri($"{baseUrl}/campaign/process-speech?lang={Uri.EscapeDataString(lang)}"),
            method: Twilio.Http.HttpMethod.Post,
            speechTimeout: "auto",
            timeout: 8,
            language: lang,
            hints: lang == "fr-CA"
                ? "rendez vous,rendez-vous,prendre rendez vous,prendre rendez-vous,rdv,service,appointment"
                : "appointment,book appointment,schedule appointment,service"
        );

        gather.Say(line, voice: voice, language: lang);
        response.Append(gather);

        response.Redirect(new Uri($"{baseUrl}/campaign/no-response?lang={Uri.EscapeDataString(lang)}"));
        return Content(response.ToString(), "text/xml");
    }

    [HttpPost("/campaign/process-speech")]
    [HttpGet("/campaign/process-speech")]
    public IActionResult ProcessSpeech(
        [FromForm] string? SpeechResult,
        [FromQuery] string? lang)
    {
        var response = new VoiceResponse();

        _logger.LogInformation("Campaign speech raw: '{SpeechResult}'", SpeechResult ?? "(null)");

        var language = string.Equals(lang, "en-US", StringComparison.OrdinalIgnoreCase)
            ? "en-US"
            : "fr-CA";

        var voice = language == "fr-CA" ? "Polly.Celine" : "Polly.Joanna";
        var rawText = (SpeechResult ?? string.Empty).Trim();
        var normalized = Normalize(rawText);

        if (string.IsNullOrWhiteSpace(normalized))
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            response.Redirect(new Uri($"{baseUrl}/campaign/no-response?lang={Uri.EscapeDataString(language)}"));
            return Content(response.ToString(), "text/xml");
        }

        var intent = _runtime.FindIntent(rawText) ?? _runtime.FindIntent(normalized);
        var departmentKey = intent?.Department ?? string.Empty;

        if (string.IsNullOrWhiteSpace(departmentKey))
        {
            if (ContainsAny(normalized, "rendez vous", "prendre rendez vous", "rdv", "appointment", "book appointment", "schedule appointment"))
                departmentKey = "bdc";
            else if (ContainsAny(normalized, "service"))
                departmentKey = "service";
            else if (ContainsAny(normalized, "sales", "vente", "ventes"))
                departmentKey = "sales";
            else if (ContainsAny(normalized, "parts", "piece", "pieces", "pièce", "pièces"))
                departmentKey = "parts";
        }

        var dept = string.IsNullOrWhiteSpace(departmentKey)
            ? null
            : _directory.FindDepartment(departmentKey);

        if (dept != null && !string.IsNullOrWhiteSpace(dept.PhoneE164))
        {
            response.Say(
                language == "fr-CA"
                    ? $"Parfait, je vous transfère à {dept.Name}."
                    : $"Perfect, transferring you to {dept.Name}.",
                voice: voice,
                language: language
            );

            var dial = new Dial(record: Dial.RecordEnum.RecordFromAnswer);
            dial.Number(dept.PhoneE164);
            response.Append(dial);

            return Content(response.ToString(), "text/xml");
        }

        response.Say(
            language == "fr-CA"
                ? "Merci. Un conseiller vous rappellera sous peu."
                : "Thank you. An advisor will follow up shortly.",
            voice: voice,
            language: language
        );
        response.Hangup();
        return Content(response.ToString(), "text/xml");
    }

    [HttpPost("/campaign/no-response")]
    [HttpGet("/campaign/no-response")]
    public IActionResult NoResponse([FromQuery] string? lang)
    {
        var response = new VoiceResponse();

        var language = string.Equals(lang, "en-US", StringComparison.OrdinalIgnoreCase)
            ? "en-US"
            : "fr-CA";

        var voice = language == "fr-CA" ? "Polly.Celine" : "Polly.Joanna";

        response.Say(
            language == "fr-CA"
                ? "Nous n'avons pas reçu de réponse. Merci et au revoir."
                : "We did not receive a response. Thank you and goodbye.",
            voice: voice,
            language: language
        );
        response.Hangup();

        return Content(response.ToString(), "text/xml");
    }

    private static bool ContainsAny(string text, params string[] values)
    {
        foreach (var value in values)
        {
            if (text.Contains(value, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static string Normalize(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var lowered = text.ToLowerInvariant()
            .Replace("-", " ")
            .Replace("’", "'")
            .Replace("'", " ");

        var cleaned = new string(
            lowered.Where(c => char.IsLetterOrDigit(c) || char.IsWhiteSpace(c)).ToArray()
        );

        return string.Join(" ", cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }
}
