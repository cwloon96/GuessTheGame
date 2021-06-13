using GuessTheGame.Models;
using GuessTheGame.Services.UserSession;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

namespace GuessTheGame.Hubs
{
    public class GameHub : Hub
    {
        private IUserSessionService _userSessionService;
        private IServiceProvider _serviceProvider;
        private static readonly ConcurrentDictionary<Guid, GameRoom> _rooms = new ConcurrentDictionary<Guid, GameRoom>();

        public GameHub(IUserSessionService userSessionService,
            IServiceProvider serviceProvider)
        {
            _userSessionService = userSessionService;
            _serviceProvider = serviceProvider;
        }

        public override async Task OnConnectedAsync()
        {
            if (_rooms.Count > 0)
            {
                await Clients.Caller.SendAsync("PopulateRooms", _rooms.Select(x => new { x.Key, x.Value.PlayerCount, x.Value.SpectatorsCount }));
                _userSessionService.AddUserSession(Context.ConnectionId);
            }
        }

        public async Task<Guid> CreateRoom()
        {
            var room = (GameRoom)_serviceProvider.GetService(typeof(GameRoom));
            await room.AddSpectator(Context.ConnectionId);
            _rooms.TryAdd(room.RoomGuid, room);

            return room.RoomGuid;
        }

        public async Task JoinRoom(Guid roomGuid)
        {
            if (_rooms.TryGetValue(roomGuid, out GameRoom gameRoom))
            {
                await gameRoom.AddSpectator(Context.ConnectionId);
                _userSessionService.UpdateUserSession(Context.ConnectionId, roomGuid);

                await Clients.Group(roomGuid.ToString()).SendAsync("UpdateSpectators", new { Count = gameRoom.SpectatorsCount });
            }
        }

        public async Task LeaveRoom(Guid roomGuid)
        {
            if (_rooms.TryGetValue(roomGuid, out GameRoom gameRoom))
            {
                gameRoom.RemoveSpectator(Context.ConnectionId);
                _userSessionService.UpdateUserSession(Context.ConnectionId, Guid.Empty);

                await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomGuid.ToString());
                await Clients.Group(roomGuid.ToString()).SendAsync("UpdateSpectators", new { Count = gameRoom.SpectatorsCount });
            }
        }

        public async Task Login(string username, Guid roomGuid)
        {
            if (_rooms.TryGetValue(roomGuid, out GameRoom gameRoom))
                await gameRoom.AddPlayer(username, Context.ConnectionId);
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            Guid userLastRoom = _userSessionService.GetRoomGuidByConnectionIdAsync(Context.ConnectionId);

            if(_rooms.TryGetValue(userLastRoom, out GameRoom gameRoom))
            {
                await gameRoom.DisconnectUser(Context.ConnectionId);
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}