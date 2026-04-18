namespace AIReception.Mvc.Models.Sms;

public class SmsMessageRecord
{
    public string Id { get; set; } = "";
    public string Type { get; set; } = "sms";
    public string MessageSid { get; set; } = "";
    public string From { get; set; } = "";
    public string To { get; set; } = "";
    public string Body { get; set; } = "";
    public string Status { get; set; } = "";
    public string RoutedDepartment { get; set; } = "sms";
    public string DetectedIntent { get; set; } = "";
    public string Language { get; set; } = "";
    public string CreatedAt { get; set; } = DateTime.UtcNow.ToString("O");
    public string UpdatedAt { get; set; } = DateTime.UtcNow.ToString("O");
}

public class InboxConversationSummary
{
    public string Phone { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string LastMessage { get; set; } = "";
    public string LastTimestamp { get; set; } = "";
}
