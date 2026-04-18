using System.Collections.Concurrent;
using System.Text;

namespace AIReception.Mvc.Services;

public class CallTranscriptService
{
    private sealed class TranscriptEntry
    {
        public StringBuilder Buffer { get; } = new();
        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    }

    private readonly ConcurrentDictionary<string, TranscriptEntry> _store = new();

    public void Append(string callSid, string line)
    {
        if (string.IsNullOrWhiteSpace(callSid) || string.IsNullOrWhiteSpace(line))
            return;

        var entry = _store.GetOrAdd(callSid, _ => new TranscriptEntry());

        lock (entry)
        {
            if (entry.Buffer.Length > 0)
                entry.Buffer.AppendLine();

            entry.Buffer.Append(line.Trim());
            entry.UpdatedAtUtc = DateTime.UtcNow;
        }
    }

    public string Get(string callSid)
    {
        if (string.IsNullOrWhiteSpace(callSid))
            return string.Empty;

        if (!_store.TryGetValue(callSid, out var entry))
            return string.Empty;

        lock (entry)
        {
            return entry.Buffer.ToString();
        }
    }

    public DateTime? GetUpdatedAtUtc(string callSid)
    {
        if (string.IsNullOrWhiteSpace(callSid))
            return null;

        return _store.TryGetValue(callSid, out var entry)
            ? entry.UpdatedAtUtc
            : null;
    }
}
