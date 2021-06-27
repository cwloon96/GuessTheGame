using GuessTheGame.Common.Constants;
using GuessTheGame.Services;
using GuessTheGame.Services.Hubs;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GuessTheGame.Models
{
    public class GameRoom
    {
        public int PlayerCount => _players.Count;
        public int SpectatorsCount => _spectators.Count;
        public bool IsEmpty() => PlayerCount == 0 && SpectatorsCount == 0;

        public Guid RoomGuid { get; private set; }

        private ConcurrentDictionary<string, Player> _players;
        private List<string> _spectators;
        private IWordService _wordService;
        private List<string> words;
        private string currentWord;
        private string maskedWord;
        private const int MAX_PLAYER = 2;

        private IGameHubService _gameHubService;

        public GameRoom(IWordService wordService, IGameHubService gameHubService)
        {
            RoomGuid = Guid.NewGuid();
            _wordService = wordService;
            _gameHubService = gameHubService;
            _spectators = new List<string>();
            _players = new ConcurrentDictionary<string, Player>();
        }

        public async Task AddSpectator(string connectionId)
        {
            _spectators.Add(connectionId);
            await _gameHubService.PopulatePlayersAsync(connectionId, _players.Select(x => x.Value).Select(x => new { x.Username, x.Money }));

            await _gameHubService.AddToGroupAsync(RoomGuid, connectionId);
            await _gameHubService.SendToGroupAsync(RoomGuid, GameHubMethod.UPDATE_ROOM_SPECTATOR_COUNT, new { Count = SpectatorsCount });

            // if there are players playing, will show the words
            if (_players.Count == MAX_PLAYER)
                await _gameHubService.UpdateCurrentWordAsync(maskedWord, connectionId);
        }

        public async Task RemoveSpectator(string connectionId)
        {
            _spectators.Remove(connectionId);
            await _gameHubService.RemoveFromGroupAsync(RoomGuid, connectionId);
            await _gameHubService.SendToGroupAsync(RoomGuid, GameHubMethod.UPDATE_ROOM_SPECTATOR_COUNT, new { Count = SpectatorsCount });
        }

        public async Task<bool> AddPlayer(string username, string connectionId)
        {
            int initialMoney = 100;

            _spectators.Remove(connectionId);
            await _gameHubService.SendToGroupAsync(RoomGuid, GameHubMethod.UPDATE_ROOM_SPECTATOR_COUNT, new { Count = SpectatorsCount });

            if (_players.Count < MAX_PLAYER && _players.TryAdd(username, new Player(username, initialMoney, connectionId)))
            {
                await _gameHubService.PlayerJoinAsync(RoomGuid, new { username, money = initialMoney });

                if (_players.Count == MAX_PLAYER)
                    await RestartGame();

                return true;
            }

            return false;
        }

        public async Task RemovePlayer(string username, string connectionId)
        {
            _players.TryRemove(username, out _);
            await AddSpectator(connectionId);

            await _gameHubService.SendToGroupAsync(RoomGuid, GameHubMethod.UPDATE_ROOM_PLAYER_INFO, _players.Select(x => x.Value).Select(x => new { x.Username, x.Money }));
            await _gameHubService.PlayerLeftAsync(RoomGuid, connectionId, username); 
        }

        public async Task ViewWord(string username, int index)
        {
            if (_players.TryGetValue(username, out Player player))
            {
                if (player.Money >= 10)
                {
                    if (maskedWord[index] == '*')
                    {
                        var newMasked = new StringBuilder(maskedWord);
                        newMasked[index] = currentWord[index];
                        maskedWord = newMasked.ToString();

                        await _gameHubService.RefreshWordAsync(RoomGuid, maskedWord);

                        player.Money -= 10;
                        await UpdatePlayerBalance(username, player.Money);

                        // all masked removed
                        if (maskedWord == currentWord)
                            await RestartGame();
                    }
                }
            }
        }

        private async Task RestartGame()
        {
            // some buffer time before changing the word
            Thread.Sleep(1500);
            await _gameHubService.RefreshWordAsync(RoomGuid, GetRandomMaskedWord());
        }

        private Task UpdatePlayerBalance(string username, int money) => _gameHubService.UpdatePlayerBalanceAsync(RoomGuid, username, money);

        public async Task SubmitAnswer(string username, string answer)
        {
            if (_players.TryGetValue(username, out Player player))
            {
                if (player.Money >= 10)
                {
                    player.Money -= 10;
                    await UpdatePlayerBalance(username, player.Money);

                    bool correct = answer == currentWord;
                    await _gameHubService.ReceiveAnswerAsync(RoomGuid, username, answer, correct);
                    if (correct)
                    {
                        int maskedCount = maskedWord.Select(x => x == '*').Count();
                        int earned = maskedCount * 10;
                        player.Money += earned;

                        await UpdatePlayerBalance(username, player.Money);

                        await RestartGame();
                    }
                }
            }
        }

        public async Task DisconnectUser(string connectionId)
        {
            string username = _players.FirstOrDefault(entry => entry.Value.ConnectionId == connectionId).Key;
            if (!string.IsNullOrWhiteSpace(username))
            {
                if (_players.TryRemove(username, out Player player))
                {
                    await _gameHubService.SendToGroupAsync(RoomGuid, GameHubMethod.UPDATE_ROOM_PLAYER_INFO, _players.Select(x => x.Value).Select(x => new { x.Username, x.Money }));
                    await _gameHubService.PlayerLeftAsync(RoomGuid, connectionId, username);
                }
            }

            await RemoveSpectator(connectionId);
        }

        private void EnsureWordsExist()
        {
            if (words == null || (words != null && words.Count == 0))
                words = _wordService.GetWords();
        }

        private string GetRandomMaskedWord()
        {
            EnsureWordsExist();
            string word = words[new Random().Next(words.Count)];
            words.Remove(word);

            currentWord = word;
            maskedWord = string.Join("", currentWord.Select(x => x == ' ' ? ' ' : '*'));

            return maskedWord;
        }
    }
}