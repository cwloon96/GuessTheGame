using System;
using System.Collections.Concurrent;

namespace GuessTheGame.Services.UserSession
{
    public class UserSessionService : IUserSessionService
    {
        private ConcurrentDictionary<string, Guid> _userSessions;

        public UserSessionService()
        {
            _userSessions = new ConcurrentDictionary<string, Guid>();
        }

        public void AddUserSession(string connectionId)
        {
            _userSessions.TryAdd(connectionId, Guid.Empty);
        }

        public Guid GetRoomGuidByConnectionIdAsync(string connectionId)
        {
            if (_userSessions.TryGetValue(connectionId, out Guid roomGuid))
                return roomGuid;

            return Guid.Empty;
        }

        public void UpdateUserSession(string connectionId, Guid roomGuid)
        {
            _userSessions.TryUpdate(connectionId, roomGuid, _userSessions[connectionId]);
        }
    }
}