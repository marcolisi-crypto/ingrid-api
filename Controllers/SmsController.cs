using AIReception.Mvc.Models.Sms;
using AIReception.Mvc.Services;
using Microsoft.AspNetCore.Mvc;
using Twilio.TwiML;

namespace AIReception.Mvc.Controllers;

[ApiController]
public class SmsController : ControllerBase
{
    private readonly TwilioSmsService _smsService;
    private readonly ILogger<SmsController> _logger;

    public SmsController(TwilioSmsService smsService, ILogger<SmsController> logger)
    {
        _smsService = smsService;
        _logger = logger;
    }

    [HttpPost("/api/sms/send")]
    public async Task<IActionResult> SendSms([FromBody] SmsSendRequest request)
    {
        try
        {
            if (request == null || string.IsNullOrWhiteSpace(request.To) || string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest(new { error = "Destination number and message are required." });
            }

            var sent = await _smsService.SendAsync(request);
            return Ok(new
            {
                success = true,
                sid = sent.MessageSid,
                from = sent.From,
                to = sent.To,
                body = sent.Body,
                message = sent.Body,
                status = sent.Status,
                routedDepartment = sent.RoutedDepartment,
                detectedIntent = sent.DetectedIntent,
                language = sent.Language,
                createdAt = sent.CreatedAt,
                updatedAt = sent.UpdatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send SMS.");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("/sms/inbound")]
    public async Task<IActionResult> InboundSms(
        [FromForm] string? From,
        [FromForm] string? To,
        [FromForm] string? Body,
        [FromForm] string? MessageSid)
    {
        try
        {
            await _smsService.HandleInboundAsync(From, To, Body, MessageSid);
            var response = new MessagingResponse();
            return Content(response.ToString(), "text/xml");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process inbound SMS.");
            return Ok();
        }
    }

    [HttpGet("/api/inbox")]
    public IActionResult GetInbox()
    {
        var conversations = _smsService.GetConversations();
        return Ok(new { conversations });
    }

    [HttpGet("/api/inbox/{phone}")]
    public IActionResult GetThread(string phone)
    {
        var messages = _smsService.GetThread(phone);
        return Ok(new { messages });
    }
}
