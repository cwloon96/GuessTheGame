﻿using GuessTheGame.Hubs;
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

        public Task AddToGroupAsync(Guid roomGuid, string connectionId) =>
            _hubContext.Groups.AddToGroupAsync(connectionId, roomGuid.ToString());

        public Task RemoveFromGroupAsync(Guid roomGuid, string connectionId) =>
            _hubContext.Groups.RemoveFromGroupAsync(connectionId, roomGuid.ToString());

        public Task SendToGroupAsync(Guid roomGuid, string method, object obj) =>
            _hubContext.Clients.Group(roomGuid.ToString()).SendAsync(method, obj);

        public Task UpdateCurrentWordAsync(string maskedWord, string connectionId) =>
            _hubContext.Clients.Client(connectionId).SendAsync("RefreshWord", maskedWord);

        public Task RefreshWordAsync(Guid roomGuid, string word) =>
            _hubContext.Clients.Group(roomGuid.ToString()).SendAsync("RefreshWord", word);

        public Task PlayerJoinAsync(Guid roomGuid, object obj) =>
            _hubContext.Clients.Group(roomGuid.ToString()).SendAsync("PlayerJoined", obj);

        public async Task PlayerLeftAsync(Guid roomGuid, string connectionId, string playerUsername)
        {
            await _hubContext.Clients.Group(roomGuid.ToString()).SendAsync("PlayerLeaved", playerUsername);

            // not enough player, hide the word
            await RefreshWordAsync(roomGuid, string.Empty);
        }

        public Task UpdateBalanceAsync(Guid roomGuid, string username, int money) =>
            _hubContext.Clients.Group(roomGuid.ToString()).SendAsync("UpdatePlayer", username, money);

        public Task ReceiveAnswerAsync(Guid roomGuid, string username, string answer, bool correct) =>
            _hubContext.Clients.Group(roomGuid.ToString()).SendAsync("ReceiveAnswer", username, answer, correct);

        public Task PopulatePlayersAsync(string connectionId, object obj) =>
            _hubContext.Clients.Client(connectionId).SendAsync("PopulatePlayers", obj);
    }
}