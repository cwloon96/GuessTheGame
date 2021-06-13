using System;

namespace GuessTheGame.Services.UserSession
{
    public interface IUserSessionService
    {
        Guid GetRoomGuidByConnectionIdAsync(string connectionId);

        void AddUserSession(string connectionId);

        void UpdateUserSession(string connectionId, Guid roomGuid);
    }
}