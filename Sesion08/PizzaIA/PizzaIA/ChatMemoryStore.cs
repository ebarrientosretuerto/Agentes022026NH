using Microsoft.Extensions.AI;
using System.Collections.Concurrent;

namespace PizzaIA;

public class ChatMemoryStore
{
    private readonly ConcurrentDictionary<string, List<ChatMessage>> _sessions = new();

    private const int MaxMessagesPerSession = 50;

    public List<ChatMessage> GetHistory(string sessionId)
    {
        return _sessions.GetOrAdd(sessionId, _ => []);

    }

    public void AddMessage(string sessionId, ChatMessage message)
    {
        var history = _sessions.GetOrAdd(sessionId, _ => []);
        lock (history)
        {
            history.Add(message);
            if (history.Count > MaxMessagesPerSession)
                history.RemoveRange(0, history.Count - MaxMessagesPerSession);
        }
    }

    public void AddMessages(string sessionId, IEnumerable<ChatMessage> messages)
    {
        foreach (var msg in messages)
            AddMessage(sessionId, msg);
    }
}

