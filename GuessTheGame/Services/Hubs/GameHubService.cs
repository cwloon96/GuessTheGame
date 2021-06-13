using GuessTheGame.Hubs;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;

namespace GuessTheGame.Services.Hubs
{
    public class GameHubService : IGameHubService
    {
        private IHubContext<GameHub> _hubContext;

        public GameHubService(IHubContext<GameHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task AddToGroupAsync(Guid roomGuid, string connectionId)
        {
            await _hubContext.Groups.AddToGroupAsync(connectionId, roomGuid.ToString());
        }

        public async Task UpdateSpectatorView(string maskedWord, string connectionId)
        {
            await _hubContext.Clients.Client(connectionId).SendAsync("RefreshWord", maskedWord);
        }

        public async Task RefreshWordAsync(Guid roomGuid, string word)
        {
            await _hubContext.Clients.Group(roomGuid.ToString()).SendAsync("RefreshWord", word);
        }

        public async Task PlayerJoinAsync(Guid roomGuid, object obj)
        {
            await _hubContext.Clients.Group(roomGuid.ToString()).SendAsync("PlayerJoined", obj);
        }

        public async Task PlayerLeftAsync(Guid roomGuid, string connectionId, string playerUsername)
        {
            await _hubContext.Clients.Group(roomGuid.ToString()).SendAsync("PlayerLeaved", playerUsername);

            // not enough player, hide the word
            await RefreshWordAsync(roomGuid, string.Empty);
        }

        public async Task UpdateBalanceAsync(Guid roomGuid, string username, int money)
        {
            await _hubContext.Clients.Group(roomGuid.ToString()).SendAsync("UpdatePlayer", username, money);
        }

        public async Task ReceiveAnswerAsync(Guid roomGuid, string username, string answer, bool correct)
        {
            await _hubContext.Clients.Group(roomGuid.ToString()).SendAsync("ReceiveAnswer", username, answer, correct);
        }

        public async Task PopulatePlayersAsync(string connectionId, object obj)
        {
            await _hubContext.Clients.Client(connectionId).SendAsync("PopulatePlayers", obj);
        }
    }
}