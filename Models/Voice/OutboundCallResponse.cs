namespace AIReception.Mvc.Models.Voice;

public class OutboundCallResponse
{
    public bool Success { get; set; }
    public string? CallSid { get; set; }
    public string? Message { get; set; }
    public string? NormalizedTo { get; set; }
    public string? Mode { get; set; }
}
