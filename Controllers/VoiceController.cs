using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Twilio.TwiML;
using Twilio.TwiML.Voice;
using AIReception.Mvc.Services;
using AIReception.Mvc.Models.Directory;

namespace AIReception.Mvc.Models.Voice;

public class VoiceController : Controller
{
    private readonly RuntimeRulesService _runtime;
    private readonly DirectoryRoutingService _directory;
    private readonly CallerMemoryService _memory;
    private readonly CallTranscriptService _transcripts;
    private readonly ILogger<VoiceController> _logger;

    public VoiceController(
        RuntimeRulesService runtime,
        DirectoryRoutingService directory,
        CallerMemoryService memory,
        CallTranscriptService transcripts,
        ILogger<VoiceController> logger)
    {
        _runtime = runtime;
        _directory = directory;
        _memory = memory;
        _transcripts = transcripts;
        _logger = logger;
    }

    [HttpPost("/incoming-call")]
    public IActionResult IncomingCall([FromForm] string? From, [FromForm] string? CallSid)
    {
        var response = new VoiceResponse();

        _memory.Upsert(
            phoneNumber: From ?? "",
            preferredLanguage: "fr-CA");

        const string greeting =
            "Bonjour. Merci d'avoir appelé BMW MINI Laval. Comment puis-je vous aider aujourd'hui? Pour continuer en anglais, dites English.";

        AppendAi(CallSid, greeting);

        response.Say(
            greeting,
            voice: "Polly.Celine",
            language: "fr-CA");

        response.Redirect(new Uri("/gather", UriKind.Relative));
        return Content(response.ToString(), "text/xml");
    }

    [HttpPost("/gather")]
    public IActionResult GatherPrompt([FromForm] string? From, [FromForm] string? CallSid)
    {
        var response = new VoiceResponse();
        var callerMemory = _memory.Get(From);
        var preferredLanguage = callerMemory?.PreferredLanguage ?? "fr-CA";

        var gatherLanguage = preferredLanguage == "en-US" ? "en-US" : "fr-CA";
        var gatherVoice = preferredLanguage == "en-US" ? "Polly.Joanna" : "Polly.Celine";

        var prompt = preferredLanguage == "en-US" ? "Yes, I'm listening." : "Je vous écoute.";
        AppendAi(CallSid, prompt);

        var gather = new Gather(
            input: new List<Gather.InputEnum>
            {
                Gather.InputEnum.Speech
            },
            action: new Uri("/process-speech", UriKind.Relative),
            speechTimeout: "auto",
            language: gatherLanguage
        );

        gather.Say(
            prompt,
            voice: gatherVoice,
            language: gatherLanguage);

        response.Append(gather);
        response.Redirect(new Uri("/gather", UriKind.Relative));

        return Content(response.ToString(), "text/xml");
    }

    [HttpPost("/process-speech")]
    public IActionResult ProcessSpeech(
        [FromForm] string? SpeechResult,
        [FromForm] string? From,
        [FromForm] string? CallSid)
    {
        var response = new VoiceResponse();
        var text = (SpeechResult ?? "").Trim();

        if (string.IsNullOrWhiteSpace(text))
        {
            response.Redirect(new Uri("/gather", UriKind.Relative));
            return Content(response.ToString(), "text/xml");
        }

        AppendCaller(CallSid, text);

        var callerMemory = _memory.Get(From);
        var normalizedText = Normalize(text);

        if (IsEnglishSwitch(normalizedText))
        {
            _memory.Upsert(
                phoneNumber: From ?? "",
                preferredLanguage: "en-US");

            const string line = "Of course, we can continue in English.";
            AppendAi(CallSid, line);

            response.Say(
                line,
                voice: "Polly.Joanna",
                language: "en-US");

            response.Redirect(new Uri("/gather", UriKind.Relative));
            return Content(response.ToString(), "text/xml");
        }

        if (IsFrenchSwitch(normalizedText))
        {
            _memory.Upsert(
                phoneNumber: From ?? "",
                preferredLanguage: "fr-CA");

            const string line = "Oui, bien sûr. On peut continuer en français.";
            AppendAi(CallSid, line);

            response.Say(
                line,
                voice: "Polly.Celine",
                language: "fr-CA");

            response.Redirect(new Uri("/gather", UriKind.Relative));
            return Content(response.ToString(), "text/xml");
        }

        var detectedLanguage = _runtime.DetectLanguage(text);
        var previousLanguage = callerMemory?.PreferredLanguage ?? "fr-CA";
        var language = previousLanguage;

        if (detectedLanguage == "en-US")
        {
            language = "en-US";
        }
        else if (detectedLanguage == "fr-CA" && text.Length >= 8)
        {
            language = "fr-CA";
        }

        if (normalizedText.StartsWith("hi") ||
            normalizedText.StartsWith("hello") ||
            normalizedText.StartsWith("hey") ||
            normalizedText.StartsWith("yes hi") ||
            normalizedText.Contains("do you speak english") ||
            normalizedText.Contains("i would like") ||
            normalizedText.Contains("book an appointment") ||
            normalizedText.Contains("purchase a vehicle") ||
            normalizedText.Contains("purchasing a vehicle") ||
            normalizedText.Contains("buy a car") ||
            normalizedText.Contains("buy a vehicle"))
        {
            language = "en-US";
        }

        if (string.IsNullOrWhiteSpace(language))
        {
            language = "fr-CA";
        }

        _memory.Upsert(
            phoneNumber: From ?? "",
            preferredLanguage: language);

        var voice = language == "fr-CA" ? "Polly.Celine" : "Polly.Joanna";

        _logger.LogInformation("User said: {Text}", text);
        _logger.LogInformation(
            "Detected language: {DetectedLanguage} | Active language: {Language}",
            detectedLanguage,
            language);

        if (IsAddressQuestion(normalizedText))
        {
            var line = language == "fr-CA"
                ? "Notre adresse est le 2450 boulevard Chomedey, à Laval."
                : "Our address is 2450 Boulevard Chomedey in Laval.";

            AppendAi(CallSid, line);

            response.Say(
                line,
                voice: voice,
                language: language);

            response.Redirect(new Uri("/gather", UriKind.Relative));
            return Content(response.ToString(), "text/xml");
        }

        if (callerMemory?.LastIntentId == "asked_hours")
        {
            if (normalizedText.Contains("service") ||
                normalizedText.Contains("parts") ||
                normalizedText.Contains("pieces") ||
                normalizedText.Contains("pièces"))
            {
                var line = language == "fr-CA"
                    ? "Le service et les pièces sont ouverts du lundi au jeudi, de 8 heures à 17 heures, et le vendredi, de 8 heures à 14 heures 30."
                    : "Service and parts are open Monday to Thursday, from 8 A M to 5 P M, and Friday, from 8 A M to 2 30 P M.";

                AppendAi(CallSid, line);

                response.Say(
                    line,
                    voice: voice,
                    language: language);

                _memory.Upsert(
                    phoneNumber: From ?? "",
                    preferredLanguage: language,
                    lastIntentId: "hours_answered");

                response.Redirect(new Uri("/gather", UriKind.Relative));
                return Content(response.ToString(), "text/xml");
            }

            if (normalizedText.Contains("sales") || normalizedText.Contains("ventes"))
            {
                var line = language == "fr-CA"
                    ? "Les ventes sont ouvertes du lundi au jeudi, de 9 heures à 20 heures, et le vendredi, de 9 heures à 18 heures."
                    : "Sales is open Monday to Thursday, from 9 A M to 8 P M, and Friday, from 9 A M to 6 P M.";

                AppendAi(CallSid, line);

                response.Say(
                    line,
                    voice: voice,
                    language: language);

                _memory.Upsert(
                    phoneNumber: From ?? "",
                    preferredLanguage: language,
                    lastIntentId: "hours_answered");

                response.Redirect(new Uri("/gather", UriKind.Relative));
                return Content(response.ToString(), "text/xml");
            }
        }

        if (IsSalesHoursQuestion(normalizedText))
        {
            var line = language == "fr-CA"
                ? "Les ventes sont ouvertes du lundi au jeudi de 9 heures à 20 heures, et le vendredi de 9 heures à 18 heures. Nous sommes fermés le samedi et le dimanche."
                : "Sales is open Monday to Thursday from 9 A M to 8 P M, and Friday from 9 A M to 6 P M. We are closed Saturday and Sunday.";

            AppendAi(CallSid, line);

            response.Say(
                line,
                voice: voice,
                language: language);

            response.Redirect(new Uri("/gather", UriKind.Relative));
            return Content(response.ToString(), "text/xml");
        }

        if (IsServiceHoursQuestion(normalizedText))
        {
            var line = language == "fr-CA"
                ? "Le service et les pièces sont ouverts du lundi au jeudi de 8 heures à 17 heures, et le vendredi de 8 heures à 14 heures 30. Nous sommes fermés le samedi et le dimanche."
                : "Service and parts are open Monday to Thursday from 8 A M to 5 P M, and Friday from 8 A M to 2 30 P M. We are closed Saturday and Sunday.";

            AppendAi(CallSid, line);

            response.Say(
                line,
                voice: voice,
                language: language);

            response.Redirect(new Uri("/gather", UriKind.Relative));
            return Content(response.ToString(), "text/xml");
        }

        if (IsHoursQuestion(normalizedText))
        {
            _memory.Upsert(
                phoneNumber: From ?? "",
                preferredLanguage: language,
                lastIntentId: "asked_hours");

            var line = language == "fr-CA"
                ? "Voulez-vous les heures des ventes, ou du service?"
                : "Do you want sales hours, or service hours?";

            AppendAi(CallSid, line);

            response.Say(
                line,
                voice: voice,
                language: language);

            response.Redirect(new Uri("/gather", UriKind.Relative));
            return Content(response.ToString(), "text/xml");
        }

        if (IsSalesDepartmentRequest(normalizedText))
        {
            var dept = FindSalesDepartment();
            if (dept != null)
            {
                return TransferToDepartment(response, dept, language, voice, From, "sales_department_direct", CallSid);
            }
        }

        if (IsServiceDepartmentRequest(normalizedText))
        {
            var dept = FindServiceDepartment();
            if (dept != null)
            {
                return TransferToDepartment(response, dept, language, voice, From, "service_department_direct", CallSid);
            }
        }

        if (IsPartsDepartmentRequest(normalizedText))
        {
            var dept = FindPartsDepartment();
            if (dept != null)
            {
                return TransferToDepartment(response, dept, language, voice, From, "parts_department_direct", CallSid);
            }
        }

        bool soundsLikeNamedPersonRequest =
            normalizedText.Contains("speak to") ||
            normalizedText.Contains("talk to") ||
            normalizedText.Contains("parler a") ||
            normalizedText.Contains("parler au") ||
            normalizedText.Contains("parler avec") ||
            normalizedText.Contains("joindre") ||
            normalizedText.Contains("transfer me to") ||
            normalizedText.Contains("transfere moi a") ||
            normalizedText.Contains("transfere moi au");

        if (soundsLikeNamedPersonRequest)
        {
            var employeeMatch = _directory.FindBestEmployeeMatch(text);
            if (employeeMatch != null)
            {
                _logger.LogInformation(
                    "Best employee match: {Name} | Score: {Score} | MatchedOn: {MatchedOn}",
                    employeeMatch.Entry.Name,
                    employeeMatch.Score,
                    employeeMatch.MatchedOn);

                if (employeeMatch.Score >= 2.5)
                {
                    _memory.Upsert(
                        phoneNumber: From ?? "",
                        preferredLanguage: language,
                        lastDepartment: employeeMatch.Entry.Department,
                        lastAdvisorName: employeeMatch.Entry.Name);

                    return Transfer(response, employeeMatch.Entry, language, voice, CallSid);
                }

                if (employeeMatch.Score >= 1.2)
                {
                    var line = language == "fr-CA"
                        ? $"Voulez-vous parler à {employeeMatch.Entry.Name}?"
                        : $"Did you mean {employeeMatch.Entry.Name}?";

                    AppendAi(CallSid, line);

                    response.Say(
                        line,
                        voice: voice,
                        language: language);

                    response.Redirect(new Uri("/gather", UriKind.Relative));
                    return Content(response.ToString(), "text/xml");
                }
            }
        }

        var directoryMatch = _directory.FindBestMatch(text);
        if (directoryMatch != null && directoryMatch.Score >= 0.4)
        {
            _logger.LogInformation(
                "Directory-first match: {Name} | Score: {Score} | MatchedOn: {MatchedOn}",
                directoryMatch.Entry.Name,
                directoryMatch.Score,
                directoryMatch.MatchedOn);

            _memory.Upsert(
                phoneNumber: From ?? "",
                preferredLanguage: language,
                lastDepartment: directoryMatch.Entry.Department,
                lastAdvisorName: directoryMatch.Entry.Type == "employee" ? directoryMatch.Entry.Name : null,
                lastIntentId: "directory_match");

            if (directoryMatch.Entry.Type == "employee")
            {
                return Transfer(response, directoryMatch.Entry, language, voice, CallSid);
            }

            var line = language == "fr-CA"
                ? $"Parfait, je vous transfère à {directoryMatch.Entry.Name}."
                : $"Perfect, I'll connect you to {directoryMatch.Entry.Name}.";

            AppendAi(CallSid, line);

            response.Say(
                line,
                voice: voice,
                language: language);

            var dial = new Dial(record: Dial.RecordEnum.RecordFromAnswer);
            dial.Number(directoryMatch.Entry.PhoneE164);
            response.Append(dial);

            return Content(response.ToString(), "text/xml");
        }

        var intent = _runtime.FindIntent(text);
        _logger.LogInformation("Detected intent: {IntentId}", intent?.Id ?? "null");

        _memory.Upsert(
            phoneNumber: From ?? "",
            preferredLanguage: language,
            lastIntentId: intent?.Id ?? "");

        if (intent != null)
        {
            var dept = FindDepartmentFromIntent(intent.Department);
            if (dept != null)
            {
                _memory.Upsert(
                    phoneNumber: From ?? "",
                    preferredLanguage: language,
                    lastDepartment: dept.Department,
                    lastIntentId: intent.Id);

                var line = language == "fr-CA"
                    ? $"Parfait, je vous transfère à {dept.Name}."
                    : $"Perfect, I'll connect you to {dept.Name}.";

                AppendAi(CallSid, line);

                response.Say(
                    line,
                    voice: voice,
                    language: language);

                var dial = new Dial(record: Dial.RecordEnum.RecordFromAnswer);
                dial.Number(dept.PhoneE164);
                response.Append(dial);

                return Content(response.ToString(), "text/xml");
            }
        }

        return LowConfidenceFallback(response, language, voice, From, CallSid);
    }

    private IActionResult TransferToDepartment(
        VoiceResponse response,
        DirectoryEntry entry,
        string language,
        string voice,
        string? from,
        string intentId,
        string? callSid)
    {
        _memory.Upsert(
            phoneNumber: from ?? "",
            preferredLanguage: language,
            lastDepartment: entry.Department,
            lastIntentId: intentId);

        var line = language == "fr-CA"
            ? $"Parfait, je vous transfère à {entry.Name}."
            : $"Perfect, I'll connect you to {entry.Name}.";

        AppendAi(callSid, line);

        response.Say(
            line,
            voice: voice,
            language: language);

        var dial = new Dial(record: Dial.RecordEnum.RecordFromAnswer);
        dial.Number(entry.PhoneE164);
        response.Append(dial);

        return Content(response.ToString(), "text/xml");
    }

    private IActionResult Transfer(
        VoiceResponse response,
        DirectoryEntry entry,
        string language,
        string voice,
        string? callSid)
    {
        var line = language == "fr-CA"
            ? $"Je vous transfère à {entry.Name}."
            : $"Transferring you to {entry.Name}.";

        AppendAi(callSid, line);

        response.Say(
            line,
            voice: voice,
            language: language);

        var dial = new Dial(record: Dial.RecordEnum.RecordFromAnswer);
        dial.Number(entry.PhoneE164);
        response.Append(dial);

        return Content(response.ToString(), "text/xml");
    }

    private IActionResult LowConfidenceFallback(
        VoiceResponse response,
        string language,
        string voice,
        string? from,
        string? callSid)
    {
        _logger.LogWarning(
            "LowConfidenceFallback triggered. Language={Language}, Caller={Caller}",
            language,
            from ?? "unknown");

        var line = language == "fr-CA"
            ? "Je peux vous aider. Est-ce pour le service, les ventes, ou les pièces?"
            : "I can help with that. Is this for service, sales, or parts?";

        AppendAi(callSid, line);

        response.Say(
            line,
            voice: voice,
            language: language);

        response.Redirect(new Uri("/gather", UriKind.Relative));
        return Content(response.ToString(), "text/xml");
    }

    private DirectoryEntry? FindDepartmentByAliases(params string[] aliases)
    {
        foreach (var alias in aliases)
        {
            var dept = _directory.FindDepartment(alias);
            if (dept != null)
            {
                return dept;
            }
        }

        return null;
    }

    private DirectoryEntry? FindServiceDepartment()
    {
        return FindDepartmentByAliases(
            "service",
            "Service",
            "service department",
            "Service Department",
            "bmw service",
            "mini service"
        );
    }

    private DirectoryEntry? FindSalesDepartment()
    {
        return FindDepartmentByAliases(
            "sales",
            "Sales",
            "sales department",
            "Sales Department",
            "ventes",
            "Ventes"
        );
    }

    private DirectoryEntry? FindPartsDepartment()
    {
        return FindDepartmentByAliases(
            "parts",
            "Parts",
            "parts department",
            "Parts Department",
            "pieces",
            "pièces",
            "departement des pieces",
            "département des pièces"
        );
    }

    private DirectoryEntry? FindDepartmentFromIntent(string? department)
    {
        var normalizedDepartment = Normalize(department ?? string.Empty);

        if (normalizedDepartment.Contains("service"))
        {
            return FindServiceDepartment();
        }

        if (normalizedDepartment.Contains("sales") || normalizedDepartment.Contains("ventes"))
        {
            return FindSalesDepartment();
        }

        if (normalizedDepartment.Contains("parts") || normalizedDepartment.Contains("pieces") || normalizedDepartment.Contains("pièces"))
        {
            return FindPartsDepartment();
        }

        return FindDepartmentByAliases(department ?? string.Empty);
    }

    private bool IsEnglishSwitch(string normalizedText)
    {
        return normalizedText == "english"
            || normalizedText == "in english"
            || normalizedText == "english please"
            || normalizedText == "can we do this in english"
            || normalizedText == "speak english"
            || normalizedText == "do you speak english";
    }

    private bool IsFrenchSwitch(string normalizedText)
    {
        return normalizedText == "francais"
            || normalizedText == "en francais"
            || normalizedText == "french"
            || normalizedText == "on peut continuer en francais";
    }

    private bool IsAddressQuestion(string normalizedText)
    {
        return normalizedText.Contains("address") ||
               normalizedText.Contains("what is your address") ||
               normalizedText.Contains("where are you located") ||
               normalizedText.Contains("location") ||
               normalizedText.Contains("adresse") ||
               normalizedText.Contains("quelle est votre adresse") ||
               normalizedText.Contains("ou etes vous") ||
               normalizedText.Contains("ou est le concessionnaire");
    }

    private bool IsHoursQuestion(string normalizedText)
    {
        return normalizedText.Contains("hours") ||
               normalizedText.Contains("open") ||
               normalizedText.Contains("opening hours") ||
               normalizedText.Contains("what time are you open") ||
               normalizedText.Contains("what time do you close") ||
               normalizedText.Contains("heures") ||
               normalizedText.Contains("horaire") ||
               normalizedText.Contains("ouvert") ||
               normalizedText.Contains("ferme");
    }

    private bool IsSalesHoursQuestion(string normalizedText)
    {
        return (normalizedText.Contains("sales") && IsHoursQuestion(normalizedText)) ||
               normalizedText.Contains("sales hours") ||
               normalizedText.Contains("hours for sales") ||
               normalizedText.Contains("ventes") ||
               normalizedText.Contains("heures des ventes") ||
               normalizedText.Contains("horaire des ventes");
    }

    private bool IsServiceHoursQuestion(string normalizedText)
    {
        return ((normalizedText.Contains("service") || normalizedText.Contains("parts") || normalizedText.Contains("pieces") || normalizedText.Contains("pièces")) &&
                IsHoursQuestion(normalizedText)) ||
               normalizedText.Contains("service hours") ||
               normalizedText.Contains("parts hours") ||
               normalizedText.Contains("hours for service") ||
               normalizedText.Contains("hours for parts") ||
               normalizedText.Contains("heures du service") ||
               normalizedText.Contains("horaire du service") ||
               normalizedText.Contains("heures des pieces") ||
               normalizedText.Contains("heures des pièces");
    }

    private bool IsSalesDepartmentRequest(string normalizedText)
    {
        return normalizedText.Contains("sales department") ||
               normalizedText.Contains("speak to sales") ||
               normalizedText.Contains("talk to sales") ||
               normalizedText.Contains("for sales") ||
               normalizedText.Contains("purchase a vehicle") ||
               normalizedText.Contains("purchasing a vehicle") ||
               normalizedText.Contains("buy a car") ||
               normalizedText.Contains("buy a vehicle") ||
               normalizedText.Contains("interested in purchasing") ||
               normalizedText.Contains("interested in a vehicle") ||
               normalizedText.Contains("new car") ||
               normalizedText.Contains("used car") ||
               normalizedText.Contains("inventory") ||
               normalizedText.Contains("vehicle for sale") ||
               normalizedText.Contains("purchase vehicle") ||
               normalizedText.Contains("department des ventes") ||
               normalizedText.Contains("departement des ventes") ||
               normalizedText.Contains("parler aux ventes") ||
               normalizedText.Contains("acheter une voiture") ||
               normalizedText.Contains("acheter un vehicule") ||
               normalizedText.Contains("je veux acheter") ||
               normalizedText.Contains("je cherche une voiture") ||
               normalizedText.Contains("je cherche un vehicule");
    }

    private bool IsServiceDepartmentRequest(string normalizedText)
    {
        return normalizedText.Contains("service department") ||
               normalizedText.Contains("speak to service") ||
               normalizedText.Contains("talk to service") ||
               normalizedText.Contains("for service") ||
               normalizedText.Contains("departement du service") ||
               normalizedText.Contains("parler au service");
    }

    private bool IsPartsDepartmentRequest(string normalizedText)
    {
        return normalizedText.Contains("parts department") ||
               normalizedText.Contains("speak to parts") ||
               normalizedText.Contains("talk to parts") ||
               normalizedText.Contains("for parts") ||
               normalizedText.Contains("departement des pieces") ||
               normalizedText.Contains("parler aux pieces");
    }

    private string Normalize(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var cleaned = new string(
            text
                .ToLowerInvariant()
                .Where(c => char.IsLetterOrDigit(c) || char.IsWhiteSpace(c))
                .ToArray());

        return cleaned.Trim();
    }

    private void AppendCaller(string? callSid, string line)
    {
        if (!string.IsNullOrWhiteSpace(callSid))
        {
            _transcripts.Append(callSid, $"Caller: {line}");
        }
    }

    private void AppendAi(string? callSid, string line)
    {
        if (!string.IsNullOrWhiteSpace(callSid))
        {
            _transcripts.Append(callSid, $"AI: {line}");
        }
    }
}
