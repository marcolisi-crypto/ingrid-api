namespace AIReception.Mvc.Models.Sms;

public class SmsSendRequest
{
    public string? To { get; set; }
    public string? Message { get; set; }
    public string? Department { get; set; }
    public string? Source { get; set; }
}
