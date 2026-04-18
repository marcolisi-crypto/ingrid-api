using System.Collections.Concurrent;
using AIReception.Mvc.Models.Sms;

namespace AIReception.Mvc.Services;

public class SmsConversationService
{
    private readonly ConcurrentDictionary<string, List<SmsMessageRecord>> _messages = new();

    public void Add(SmsMessageRecord record)
    {
        var phoneKey = NormalizeThreadPhone(record);
        var thread = _messages.GetOrAdd(phoneKey, _ => new List<SmsMessageRecord>());
        lock (thread)
        {
            thread.Add(record);
        }
    }

    public IReadOnlyList<SmsMessageRecord> GetThread(string phone)
    {
        var key = NormalizePhone(phone);
        if (!_messages.TryGetValue(key, out var thread))
        {
            return Array.Empty<SmsMessageRecord>();
        }

        lock (thread)
        {
            return thread
                .OrderBy(m => m.CreatedAt)
                .ToList();
        }
    }

    public IReadOnlyList<InboxConversationSummary> GetConversations()
    {
        return _messages
            .Select(pair =>
            {
                List<SmsMessageRecord> snapshot;
                lock (pair.Value)
                {
                    snapshot = pair.Value.OrderBy(m => m.CreatedAt).ToList();
                }

                var last = snapshot.LastOrDefault();
                if (last == null)
                {
                    return null;
                }

                return new InboxConversationSummary
                {
                    Phone = pair.Key,
                    DisplayName = pair.Key,
                    LastMessage = last.Body,
                    LastTimestamp = last.CreatedAt
                };
            })
            .Where(x => x != null)
            .OrderByDescending(x => x!.LastTimestamp)
            .Cast<InboxConversationSummary>()
            .ToList();
    }

    private static string NormalizeThreadPhone(SmsMessageRecord record)
    {
        var from = NormalizePhone(record.From);
        var to = NormalizePhone(record.To);
        var twilioVoice = NormalizePhone(Environment.GetEnvironmentVariable("TWILIO_VOICE_NUMBER") ?? "");
        var twilioPhone = NormalizePhone(Environment.GetEnvironmentVariable("TWILIO_PHONE_NUMBER") ?? "");

        if (!string.IsNullOrWhiteSpace(from) && from != twilioVoice && from != twilioPhone)
        {
            return from;
        }

        return to;
    }

    private static string NormalizePhone(string? input)
    {
        var digits = new string((input ?? "").Where(char.IsDigit).ToArray());
        if (string.IsNullOrWhiteSpace(digits)) return "";
        if (digits.Length == 10) return $"+1{digits}";
        if (digits.Length == 11 && digits.StartsWith("1")) return $"+{digits}";
        return input?.StartsWith("+") == true ? $"+{digits}" : $"+{digits}";
    }
}
