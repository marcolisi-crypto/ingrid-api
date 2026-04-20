using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AIReception.Mvc.Data;
using AIReception.Mvc.Services;
using Microsoft.EntityFrameworkCore;
using Twilio;
using Twilio.Rest.Api.V2010.Account;

var builder = WebApplication.CreateBuilder(args);

var ingridConnectionString = ResolveIngridConnectionString(builder.Configuration);

builder.Services.AddControllersWithViews();

builder.Services.AddDbContextFactory<IngridDmsDbContext>(options =>
{
    if (string.IsNullOrWhiteSpace(ingridConnectionString))
    {
        options.UseInMemoryDatabase("ingrid-dms-dev");
        return;
    }

    options.UseNpgsql(NormalizeDbConnectionString(ingridConnectionString));
});

builder.Services.AddSingleton<ReceptionConfigService>();
builder.Services.AddSingleton<IntentRouterService>();
builder.Services.AddSingleton<RuntimeRulesService>();
builder.Services.AddSingleton<DirectoryRoutingService>();
builder.Services.AddSingleton<CallerMemoryService>();
builder.Services.AddSingleton<CallTranscriptService>();
builder.Services.AddSingleton<TwilioVoiceService>();
builder.Services.AddSingleton<SmsConversationService>();
builder.Services.AddSingleton<DmsCoreService>();
builder.Services.AddScoped<DmsDatabaseStatusService>();
builder.Services.AddScoped<NotesService>();
builder.Services.AddScoped<TasksService>();
builder.Services.AddScoped<AppointmentsService>();
builder.Services.AddScoped<ServiceOperationsService>();
builder.Services.AddScoped<PartsManagementService>();
builder.Services.AddScoped<AccountingOperationsService>();
builder.Services.AddSingleton<TwilioSmsService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("NetlifyFrontend", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dmsCore = scope.ServiceProvider.GetRequiredService<DmsCoreService>();
    dmsCore.Warmup();
}

app.UseRouting();
app.UseCors("NetlifyFrontend");

app.MapGet("/", () => Results.Text("AI Reception C# Running"));

app.MapControllers();

/* =========================
   CALLS API
========================= */

app.MapGet("/api/calls", async (CallTranscriptService transcripts, DmsCoreService dmsCore) =>
{
    var accountSid = Environment.GetEnvironmentVariable("TWILIO_ACCOUNT_SID");
    var authToken = Environment.GetEnvironmentVariable("TWILIO_AUTH_TOKEN");
    var backendUrl = Environment.GetEnvironmentVariable("PUBLIC_BASE_URL")
                     ?? "https://ai-reception-csharp-production.up.railway.app";
    var persistedCalls = dmsCore.GetCalls(100);
    var results = new List<object>();

    if (string.IsNullOrWhiteSpace(accountSid) || string.IsNullOrWhiteSpace(authToken))
    {
        return Results.Ok(new
        {
            calls = persistedCalls
                .Select(call => MapPersistedCallResponse(call, backendUrl))
                .ToArray()
        });
    }

    TwilioClient.Init(accountSid, authToken);

    var calls = CallResource.Read(limit: 50).ToList();
    var seenCallSids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    foreach (var call in calls.Where(c => string.IsNullOrWhiteSpace(c.ParentCallSid)))
    {
        var callSid = call.Sid ?? "";
        seenCallSids.Add(callSid);
        var recordingSid = await GetFirstRecordingSidAsync(accountSid, authToken, callSid);
        dmsCore.RecordCallStatus(new AIReception.Mvc.Models.Dms.RecordCallStatusRequest
        {
            CallSid = callSid,
            ParentCallSid = call.ParentCallSid,
            Direction = call.Direction?.ToString(),
            FromPhone = call.From?.ToString(),
            ToPhone = call.To?.ToString(),
            Status = call.Status?.ToString(),
            RecordingSid = recordingSid,
            StartedAtUtc = call.StartTime?.ToUniversalTime(),
            EndedAtUtc = call.EndTime?.ToUniversalTime(),
            SourceSystem = "twilio.calls.api"
        });

        var persistedCall = dmsCore.GetCallBySid(callSid);
        var transcriptText = string.IsNullOrWhiteSpace(persistedCall?.Transcript)
            ? transcripts.Get(callSid)
            : persistedCall.Transcript;
        var detectedDepartment = string.IsNullOrWhiteSpace(persistedCall?.DetectedDepartment)
            ? DetectDepartmentFromTranscript(transcriptText)
            : persistedCall.DetectedDepartment;
        var detectedLanguage = string.IsNullOrWhiteSpace(persistedCall?.DetectedLanguage)
            ? DetectLanguageFromTranscript(transcriptText)
            : persistedCall.DetectedLanguage;

        results.Add(new
        {
            callSid,
            from = call.From?.ToString() ?? "",
            to = call.To?.ToString() ?? "",
            startedAt = call.StartTime,
            updatedAt = call.DateUpdated ?? call.DateCreated,
            status = call.Status?.ToString() ?? "",
            language = detectedLanguage,
            detectedIntent = "",
            routedDepartment = detectedDepartment,
            userName = "",
            userNumber = call.From?.ToString() ?? "",
            transcript = transcriptText,
            recordingUrl = string.IsNullOrWhiteSpace(recordingSid)
                ? ""
                : $"{backendUrl}/api/calls/{callSid}/recording",
            notes = persistedCall?.Notes ?? "",
            duration = call.Duration?.ToString() ?? "",
            type = "call"
        });
    }

    foreach (var persistedCall in persistedCalls.Where(x => !seenCallSids.Contains(x.CallSid)))
    {
        results.Add(MapPersistedCallResponse(persistedCall, backendUrl));
    }

    return Results.Ok(new
    {
        calls = results
            .Where(c => ((string)c.GetType().GetProperty("callSid")!.GetValue(c)!).StartsWith("CA"))
            .ToArray()
    });
});

app.MapGet("/api/calls/{callSid}", async (string callSid, CallTranscriptService transcripts, DmsCoreService dmsCore) =>
{
    var accountSid = Environment.GetEnvironmentVariable("TWILIO_ACCOUNT_SID");
    var authToken = Environment.GetEnvironmentVariable("TWILIO_AUTH_TOKEN");
    var backendUrl = Environment.GetEnvironmentVariable("PUBLIC_BASE_URL")
                     ?? "https://ai-reception-csharp-production.up.railway.app";
    var persistedCall = dmsCore.GetCallBySid(callSid);

    if (string.IsNullOrWhiteSpace(accountSid) || string.IsNullOrWhiteSpace(authToken))
    {
        return persistedCall == null
            ? Results.Json(new { error = "Missing Twilio credentials" }, statusCode: 500)
            : Results.Ok(MapPersistedCallResponse(persistedCall, backendUrl));
    }

    TwilioClient.Init(accountSid, authToken);

    CallResource call;
    string? recordingSid;

    try
    {
        call = CallResource.Fetch(pathSid: callSid);
        recordingSid = await GetFirstRecordingSidAsync(accountSid, authToken, callSid);
    }
    catch
    {
        return persistedCall == null
            ? Results.NotFound(new { error = "Call not found" })
            : Results.Ok(MapPersistedCallResponse(persistedCall, backendUrl));
    }

    dmsCore.RecordCallStatus(new AIReception.Mvc.Models.Dms.RecordCallStatusRequest
    {
        CallSid = callSid,
        ParentCallSid = call.ParentCallSid,
        Direction = call.Direction?.ToString(),
        FromPhone = call.From?.ToString(),
        ToPhone = call.To?.ToString(),
        Status = call.Status?.ToString(),
        RecordingSid = recordingSid,
        StartedAtUtc = call.StartTime?.ToUniversalTime(),
        EndedAtUtc = call.EndTime?.ToUniversalTime(),
        SourceSystem = "twilio.calls.api"
    });

    persistedCall = dmsCore.GetCallBySid(callSid);
    var transcriptText = string.IsNullOrWhiteSpace(persistedCall?.Transcript)
        ? transcripts.Get(callSid)
        : persistedCall.Transcript;
    var detectedDepartment = string.IsNullOrWhiteSpace(persistedCall?.DetectedDepartment)
        ? DetectDepartmentFromTranscript(transcriptText)
        : persistedCall.DetectedDepartment;
    var detectedLanguage = string.IsNullOrWhiteSpace(persistedCall?.DetectedLanguage)
        ? DetectLanguageFromTranscript(transcriptText)
        : persistedCall.DetectedLanguage;

    return Results.Ok(new
    {
        callSid = call.Sid ?? "",
        from = call.From?.ToString() ?? "",
        to = call.To?.ToString() ?? "",
        startedAt = call.StartTime,
        updatedAt = call.DateUpdated ?? call.DateCreated,
        status = call.Status?.ToString() ?? "",
        language = detectedLanguage,
        detectedIntent = "",
        routedDepartment = detectedDepartment,
        userName = "",
        userNumber = call.From?.ToString() ?? "",
        transcript = transcriptText,
        recordingUrl = string.IsNullOrWhiteSpace(recordingSid)
            ? ""
            : $"{backendUrl}/api/calls/{callSid}/recording",
        notes = persistedCall?.Notes ?? "",
        duration = call.Duration?.ToString() ?? "",
        type = "call"
    });
});

app.MapGet("/api/calls/{callSid}/recording", async (string callSid) =>
{
    var accountSid = Environment.GetEnvironmentVariable("TWILIO_ACCOUNT_SID");
    var authToken = Environment.GetEnvironmentVariable("TWILIO_AUTH_TOKEN");

    if (string.IsNullOrWhiteSpace(accountSid) || string.IsNullOrWhiteSpace(authToken))
    {
        return Results.Json(new { error = "Missing Twilio credentials" }, statusCode: 500);
    }

    var recordingSid = await GetFirstRecordingSidAsync(accountSid, authToken, callSid);

    if (string.IsNullOrWhiteSpace(recordingSid))
    {
        return Results.NotFound(new { error = "No recording found" });
    }

    var mediaUrl =
        $"https://api.twilio.com/2010-04-01/Accounts/{accountSid}/Recordings/{recordingSid}.mp3";

    using var http = CreateTwilioHttpClient(accountSid, authToken);
    using var response = await http.GetAsync(mediaUrl);

    if (!response.IsSuccessStatusCode)
    {
        return Results.StatusCode((int)response.StatusCode);
    }

    var bytes = await response.Content.ReadAsByteArrayAsync();

    if (bytes.Length == 0)
    {
        return Results.NotFound(new { error = "Recording file is empty" });
    }

    return Results.File(
        bytes,
        contentType: "audio/mpeg",
        fileDownloadName: $"{callSid}.mp3",
        enableRangeProcessing: true
    );
});

app.MapPost("/api/calls/{callSid}/notes", async (string callSid, HttpRequest request) =>
{
    var body = await request.ReadFromJsonAsync<UpdateNotesRequest>();
    var dmsCore = request.HttpContext.RequestServices.GetRequiredService<DmsCoreService>();
    var call = dmsCore.SaveCallNotes(callSid, body?.Notes ?? "");

    return Results.Ok(new
    {
        success = true,
        call = new
        {
            callSid,
            notes = call.Notes,
            updatedAt = call.UpdatedAtUtc
        }
    });
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);

var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
app.Urls.Add($"http://0.0.0.0:{port}");

app.Run();

static HttpClient CreateTwilioHttpClient(string accountSid, string authToken)
{
    var http = new HttpClient();
    var authBytes = Encoding.ASCII.GetBytes($"{accountSid}:{authToken}");
    http.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authBytes));
    return http;
}

static async Task<string?> GetFirstRecordingSidAsync(string accountSid, string authToken, string callSid)
{
    if (string.IsNullOrWhiteSpace(callSid))
    {
        return null;
    }

    var url =
        $"https://api.twilio.com/2010-04-01/Accounts/{accountSid}/Calls/{callSid}/Recordings.json?PageSize=1";

    using var http = CreateTwilioHttpClient(accountSid, authToken);
    using var response = await http.GetAsync(url);

    if (!response.IsSuccessStatusCode)
    {
        return null;
    }

    await using var stream = await response.Content.ReadAsStreamAsync();
    using var doc = await JsonDocument.ParseAsync(stream);

    if (!doc.RootElement.TryGetProperty("recordings", out var recordings))
    {
        return null;
    }

    if (recordings.ValueKind != JsonValueKind.Array || recordings.GetArrayLength() == 0)
    {
        return null;
    }

    var first = recordings[0];
    if (!first.TryGetProperty("sid", out var sidElement))
    {
        return null;
    }

    return sidElement.GetString();
}

static string DetectDepartmentFromTranscript(string? transcript)
{
    var text = (transcript ?? "").ToLowerInvariant();

    if (string.IsNullOrWhiteSpace(text))
        return "";

    // Explicit transfer destination wins first
    if (text.Contains("service reception") ||
        text.Contains("bmw service") ||
        text.Contains("mini service") ||
        text.Contains("connect you to service") ||
        text.Contains("transferring you to service") ||
        text.Contains("transfère à service") ||
        text.Contains("transfere a service"))
        return "service";

    if (text.Contains("bmw sales") ||
        text.Contains("mini sales") ||
        text.Contains("connect you to sales") ||
        text.Contains("i'll connect you to sales") ||
        text.Contains("ill connect you to sales") ||
        text.Contains("connect you to bmw sales") ||
        text.Contains("i'll connect you to bmw sales") ||
        text.Contains("ill connect you to bmw sales") ||
        text.Contains("transferring you to sales") ||
        text.Contains("transfère à sales") ||
        text.Contains("transfere a sales") ||
        text.Contains("sales department"))
        return "sales";

    if (text.Contains("parts reception") ||
        text.Contains("bmw parts") ||
        text.Contains("mini parts") ||
        text.Contains("connect you to parts") ||
        text.Contains("transferring you to parts") ||
        text.Contains("transfère à parts") ||
        text.Contains("transfere a parts") ||
        text.Contains("parts department"))
        return "parts";

    if (text.Contains("bdc") ||
        text.Contains("connect you to bdc") ||
        text.Contains("transferring you to bdc"))
        return "bdc";

    // Fallback to explicit caller answer
    if (text.Contains("caller: service"))
        return "service";

    if (text.Contains("caller: sales") ||
        text.Contains("caller: ventes"))
        return "sales";

    if (text.Contains("caller: parts") ||
        text.Contains("caller: pièces") ||
        text.Contains("caller: pieces"))
        return "parts";

    return "";
}

static string DetectLanguageFromTranscript(string? transcript)
{
    var text = (transcript ?? "").ToLowerInvariant();

    if (string.IsNullOrWhiteSpace(text))
        return "";

    if (text.Contains("caller: english") ||
        text.Contains("continue in english") ||
        text.Contains("yes, i'm listening") ||
        text.Contains("i would like") ||
        text.Contains("transferring you"))
        return "en-US";

    if (text.Contains("bonjour") ||
        text.Contains("je vous écoute") ||
        text.Contains("je vous ecoute") ||
        text.Contains("transfère") ||
        text.Contains("transfere") ||
        text.Contains("caller: français") ||
        text.Contains("caller: francais"))
        return "fr-CA";

    return "";
}

static string NormalizeDbConnectionString(string connectionString)
{
    if (!connectionString.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) &&
        !connectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
    {
        return connectionString;
    }

    var uri = new Uri(connectionString);
    var userInfo = uri.UserInfo.Split(':', 2, StringSplitOptions.TrimEntries);
    var username = userInfo.Length > 0 ? Uri.UnescapeDataString(userInfo[0]) : "postgres";
    var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "";
    var database = uri.AbsolutePath.Trim('/');
    var sslMode = uri.Query.Contains("sslmode=require", StringComparison.OrdinalIgnoreCase)
        ? "Require"
        : "Prefer";

    return $"Host={uri.Host};Port={uri.Port};Database={database};Username={username};Password={password};SSL Mode={sslMode};Trust Server Certificate=true";
}

static string? ResolveIngridConnectionString(IConfiguration configuration)
{
    var configuredConnection = configuration.GetConnectionString("IngridDms");
    if (!string.IsNullOrWhiteSpace(configuredConnection))
    {
        return configuredConnection;
    }

    var ingridEnvironmentConnection = Environment.GetEnvironmentVariable("INGRID_DATABASE_URL");
    if (!string.IsNullOrWhiteSpace(ingridEnvironmentConnection))
    {
        return ingridEnvironmentConnection;
    }

    var railwayConnection = Environment.GetEnvironmentVariable("DATABASE_URL");
    return string.IsNullOrWhiteSpace(railwayConnection) ? null : railwayConnection;
}

static object MapPersistedCallResponse(AIReception.Mvc.Models.Dms.CallRecord call, string backendUrl)
{
    return new
    {
        callSid = call.CallSid,
        from = call.FromPhone,
        to = call.ToPhone,
        startedAt = call.StartedAtUtc,
        updatedAt = call.UpdatedAtUtc,
        status = call.Status,
        language = call.DetectedLanguage,
        detectedIntent = "",
        routedDepartment = call.DetectedDepartment,
        userName = "",
        userNumber = call.FromPhone,
        transcript = call.Transcript,
        recordingUrl = string.IsNullOrWhiteSpace(call.RecordingSid)
            ? call.RecordingUrl
            : $"{backendUrl}/api/calls/{call.CallSid}/recording",
        notes = call.Notes,
        duration = call.StartedAtUtc.HasValue && call.EndedAtUtc.HasValue
            ? Math.Max(0, (int)(call.EndedAtUtc.Value - call.StartedAtUtc.Value).TotalSeconds).ToString()
            : "",
        type = "call"
    };
}

public record UpdateNotesRequest(string Notes);
