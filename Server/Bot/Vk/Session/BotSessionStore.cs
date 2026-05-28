using System.Collections.Concurrent;

namespace Server.Bot.Vk.Session
{
    public class BotSessionStore
    {
        private readonly ConcurrentDictionary<long, BotSession> _sessions = new();

        public BotSession? Get(long vkUserId)
        {
            if (_sessions.TryGetValue(vkUserId, out var session))
            {
                if (session.IsExpired)
                {
                    _sessions.TryRemove(vkUserId, out _);
                    return null;
                }
                return session;
            }
            return null;
        }

        public void Set(long vkUserId, BotSession session)
        {
            session.LastActivity = DateTime.UtcNow;
            _sessions[vkUserId] = session;
        }

        public void Remove(long vkUserId)
        {
            _sessions.TryRemove(vkUserId, out _);
        }

        /// <summary>
        /// Периодическая очистка протухших сессий (опционально)
        /// </summary>
        public void Cleanup()
        {
            foreach (var kvp in _sessions)
            {
                if (kvp.Value.IsExpired)
                    _sessions.TryRemove(kvp.Key, out _);
            }
        }
    }
}
