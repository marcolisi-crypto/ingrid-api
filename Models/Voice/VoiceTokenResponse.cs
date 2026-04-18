namespace AIReception.Mvc.Models.Voice;

public class VoiceTokenResponse
{
    public string Token { get; set; } = "";
    public string Identity { get; set; } = "";
    public int ExpiresInSeconds { get; set; } = 3600;
    public string? OutboundApplicationSid { get; set; }
}
