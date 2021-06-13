using System;
using System.Threading.Tasks;

namespace GuessTheGame.Services.Hubs
{
    public interface IGameHubService
    {
        Task AddToGroupAsync(Guid roomGuid, string connectionId);

        Task UpdateSpectatorView(string maskedWord, string connectionId);

        Task RefreshWordAsync(Guid roomGuid, string word);

        Task PlayerLeftAsync(Guid roomGuid, string connectionId, string playerUsername);

        Task PlayerJoinAsync(Guid roomGuid, object obj);

        Task UpdateBalanceAsync(Guid roomGuid, string username, int money);

        Task ReceiveAnswerAsync(Guid roomGuid, string username, string answer, bool correct);

        Task PopulatePlayersAsync(string connectionId, object obj);
    }
}