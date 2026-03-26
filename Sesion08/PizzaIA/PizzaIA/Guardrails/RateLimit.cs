using System.Collections.Concurrent;

namespace PizzaIA.Guardrails;

public class RateLimit
{
    private readonly int _maxRequests;
    private readonly TimeSpan _window;
    private readonly ConcurrentDictionary<string, Queue<DateTime>> _requests = new();

    public RateLimit(int maxRequest, TimeSpan window)
    {
        _maxRequests = maxRequest;
        _window = window;
    }

    public (bool IsAllowed, string? ErrorMessage) TryAcquire(string sessionId)
    {
        var queue = _requests.GetOrAdd(sessionId, _ => new Queue<DateTime>());
        var now = DateTime.UtcNow;

        lock (queue)
        {
            while (queue.Count > 0 && now - queue.Peek() > _window)
                queue.Dequeue();

            if(queue.Count >= _maxRequests)
            {
                var oldestInWindow = queue.Peek();
                var waitTime = _window - (now - oldestInWindow);
                return (false, $"Has alcanzado el límite de {_maxRequests} mensajes por minuto");
            }

            queue.Enqueue(now);
            return (true, null);
        }
    }
}
