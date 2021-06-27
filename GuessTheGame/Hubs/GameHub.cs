using GuessTheGame.Common.Constants;
using GuessTheGame.Models;
using GuessTheGame.Services.UserSession;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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

        public override Task OnConnectedAsync()
        {
            _userSessionService.AddUserSession(Context.ConnectionId);

            return base.OnConnectedAsync();
        }

        public IEnumerable<GameRoomDto> RetrieveRooms()
        {
            return _rooms.Select(x => new GameRoomDto { Id = x.Key, PlayerCount = x.Value.PlayerCount, SpectatorCount = x.Value.SpectatorsCount });
        }

        public async Task<Guid> CreateRoom()
        {
            var room = (GameRoom)_serviceProvider.GetService(typeof(GameRoom));
            _rooms.TryAdd(room.RoomGuid, room);
            await UpdateLobbyRooms();

            return room.RoomGuid;
        }

        public async Task JoinRoom(Guid roomGuid)
        {
            if (_rooms.TryGetValue(roomGuid, out GameRoom gameRoom))
            {
                await gameRoom.AddSpectator(Context.ConnectionId);
                _userSessionService.UpdateUserSession(Context.ConnectionId, roomGuid);

                await UpdateLobbyRooms();
            }
        }

        public async Task LeaveRoom(Guid roomGuid)
        {
            if (_rooms.TryGetValue(roomGuid, out GameRoom gameRoom))
            {
                await gameRoom.RemoveSpectator(Context.ConnectionId);
                _userSessionService.UpdateUserSession(Context.ConnectionId, Guid.Empty);

                RemoveRoomIfEmpty(roomGuid);

                await UpdateLobbyRooms();
            }
        }

        public async Task<bool> JoinGame(string username, Guid roomGuid)
        {
            if (_rooms.TryGetValue(roomGuid, out GameRoom gameRoom))
            {
                if(await gameRoom.AddPlayer(username, Context.ConnectionId))
                {
                    await UpdateLobbyRooms();
                    return true;
                }
            }

            return false;
        }

        public async Task ViewWord(Guid roomGuid, string username, int index)
        {
            if (_rooms.TryGetValue(roomGuid, out GameRoom gameRoom))
                await gameRoom.ViewWord(username, index);
        }

        public async Task SubmitAnswer(Guid roomGuid, string username, string answer)
        {
            if (_rooms.TryGetValue(roomGuid, out GameRoom gameRoom))
                await gameRoom.SubmitAnswer(username, answer);
        }

        public async Task LeaveGame(string username, Guid roomGuid)
        {
            if (_rooms.TryGetValue(roomGuid, out GameRoom gameRoom))
            {
                await gameRoom.RemovePlayer(username, Context.ConnectionId);
                await UpdateLobbyRooms();
            }
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            Guid userLastRoom = _userSessionService.GetRoomGuidByConnectionIdAsync(Context.ConnectionId);

            if (_rooms.TryGetValue(userLastRoom, out GameRoom gameRoom))
            {
                await gameRoom.DisconnectUser(Context.ConnectionId);
            }

            await base.OnDisconnectedAsync(exception);
            RemoveRoomIfEmpty(userLastRoom);
            await UpdateLobbyRooms();
        }

        private async Task UpdateLobbyRooms()
        {
            await Clients.All.SendAsync(GameHubMethod.UPDATE_LOBBY_ROOMS, RetrieveRooms());
        }

        private void RemoveRoomIfEmpty(Guid roomGuid)
        {
            if(_rooms.TryGetValue(roomGuid, out var room))
            {
                if (room.IsEmpty())
                    _rooms.Remove(roomGuid, out _);
            }
        }
    }
}