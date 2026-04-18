using AIReception.Mvc.Models.Voice;
using AIReception.Mvc.Services;
using Microsoft.AspNetCore.Mvc;

namespace AIReception.Mvc.Controllers;

[ApiController]
public class VoiceOpsController : ControllerBase
{
    private readonly TwilioVoiceService _voiceService;
    private readonly DmsCoreService _dmsCore;
    private readonly ILogger<VoiceOpsController> _logger;

    public VoiceOpsController(TwilioVoiceService voiceService, DmsCoreService dmsCore, ILogger<VoiceOpsController> logger)
    {
        _voiceService = voiceService;
        _dmsCore = dmsCore;
        _logger = logger;
    }

    [HttpGet("/api/voice/health")]
    public IActionResult VoiceHealth()
    {
        return Ok(new
        {
            success = true,
            message = "Voice orchestration controller is available.",
            expectedEndpoints = new[]
            {
                "/api/voice/token",
                "/api/voice/outbound",
                "/twilio/voice/bridge",
                "/twilio/voice/status"
            }
        });
    }

    [HttpPost("/api/voice/token")]
    public IActionResult CreateToken([FromBody] Dictionary<string, string>? body = null)
    {
        try
        {
            body ??= new Dictionary<string, string>();
            body.TryGetValue("identity", out var identity);
            var token = _voiceService.CreateVoiceToken(identity);
            return Ok(token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create voice token.");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("/api/voice/outbound")]
    public async Task<IActionResult> StartOutbound([FromBody] OutboundCallRequest request)
    {
        try
        {
            if (request == null || string.IsNullOrWhiteSpace(request.To))
            {
                return BadRequest(new { error = "Destination phone number is required." });
            }

            var call = await _voiceService.CreateOutboundCallAsync(request, Request);
            _dmsCore.RecordCallStatus(new AIReception.Mvc.Models.Dms.RecordCallStatusRequest
            {
                CallSid = call.Sid,
                Direction = "outbound-api",
                FromPhone = Environment.GetEnvironmentVariable("TWILIO_VOICE_NUMBER"),
                ToPhone = request.To,
                Status = call.Status?.ToString() ?? "queued",
                StartedAtUtc = DateTime.UtcNow,
                SourceSystem = "ingrid.communications"
            });

            return Ok(new
            {
                success = true,
                callSid = call.Sid,
                status = call.Status?.ToString() ?? "queued",
                to = request.To,
                mode = request.Mode,
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start outbound call.");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("/twilio/voice/bridge")]
    [HttpPost("/twilio/voice/bridge")]
    public ContentResult VoiceBridge(
        [FromQuery] string? to,
        [FromQuery] string? mode,
        [FromQuery] string? scriptId)
    {
        var twiml = _voiceService.BuildBridgeTwiml(to, mode, scriptId);
        return Content(twiml, "text/xml");
    }

    [HttpPost("/twilio/voice/status")]
    public IActionResult VoiceStatus()
    {
        var form = Request.HasFormContentType ? Request.Form : null;
        var callSid = form?["CallSid"].ToString();
        var status = form?["CallStatus"].ToString();
        var direction = form?["Direction"].ToString();
        var from = form?["From"].ToString();
        var to = form?["To"].ToString();
        var recordingUrl = form?["RecordingUrl"].ToString();
        var recordingSid = form?["RecordingSid"].ToString();

        _logger.LogInformation(
            "Twilio voice status callback | CallSid: {CallSid} | CallStatus: {CallStatus} | Direction: {Direction}",
            callSid,
            status,
            direction
        );

        try
        {
            _dmsCore.RecordCallStatus(new AIReception.Mvc.Models.Dms.RecordCallStatusRequest
            {
                CallSid = callSid,
                Direction = direction,
                FromPhone = from,
                ToPhone = to,
                Status = status,
                RecordingUrl = recordingUrl,
                RecordingSid = recordingSid,
                EndedAtUtc = IsTerminalCallStatus(status) ? DateTime.UtcNow : null,
                SourceSystem = "twilio.voice.status"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist Twilio voice status callback.");
        }

        return Ok(new { success = true });
    }

    [HttpPost("/twilio/voice/transcription")]
    public IActionResult VoiceTranscription(
        [FromForm] string? CallSid,
        [FromForm] string? TranscriptionText,
        [FromForm] string? TranscriptionStatus,
        [FromForm] string? RecordingSid)
    {
        try
        {
            _dmsCore.RecordCallTranscript(new AIReception.Mvc.Models.Dms.RecordCallTranscriptRequest
            {
                CallSid = CallSid,
                Transcript = TranscriptionText,
                TranscriptionStatus = TranscriptionStatus,
                RecordingSid = RecordingSid
            });

            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist call transcription.");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    private static bool IsTerminalCallStatus(string? status)
    {
        var normalized = (status ?? "").Trim().ToLowerInvariant();
        return normalized is "completed" or "failed" or "busy" or "no-answer" or "canceled";
    }
}
