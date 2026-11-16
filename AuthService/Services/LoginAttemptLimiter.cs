using System.Collections.Concurrent;

namespace AuthService.Services
{
    /// <summary>
    /// In-memory brute-force protection for a single AuthService instance.
    /// For a multi-instance deployment this should be replaced by a distributed store (Redis).
    /// </summary>
    public sealed class LoginAttemptLimiter
    {
        private const int MaxAttempts = 5;
        private static readonly TimeSpan Window = TimeSpan.FromMinutes(15);
        private readonly ConcurrentDictionary<string, AttemptWindow> _attempts = new(StringComparer.OrdinalIgnoreCase);

        public bool IsBlocked(string email, out TimeSpan retryAfter)
        {
            retryAfter = TimeSpan.Zero;
            if (!_attempts.TryGetValue(email, out var window))
            {
                return false;
            }

            lock (window)
            {
                if (DateTime.UtcNow - window.StartedAt >= Window)
                {
                    _attempts.TryRemove(email, out _);
                    return false;
                }

                if (window.Count < MaxAttempts)
                {
                    return false;
                }

                retryAfter = Window - (DateTime.UtcNow - window.StartedAt);
                return true;
            }
        }

        public void RegisterFailure(string email)
        {
            var now = DateTime.UtcNow;
            var window = _attempts.GetOrAdd(email, _ => new AttemptWindow { StartedAt = now });

            lock (window)
            {
                if (now - window.StartedAt >= Window)
                {
                    window.StartedAt = now;
                    window.Count = 0;
                }

                window.Count++;
            }
        }

        public void Reset(string email)
        {
            _attempts.TryRemove(email, out _);
        }

        private sealed class AttemptWindow
        {
            public DateTime StartedAt { get; set; }

            public int Count { get; set; }
        }
    }
}
