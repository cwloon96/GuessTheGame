using GuessTheGame.Models;
using GuessTheGame.Services;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GuessTheGame.Hubs
{
    public class GameHub : Hub
    {
        private static ConcurrentDictionary<string, Player> _players = new ConcurrentDictionary<string, Player>();
        private IWordService _wordService;
        private static List<string> words;
        private static string currentWord;
        private static string maskedWord;
        private const int MAX_PLAYER = 2;

        public GameHub(IWordService wordService)
        {
            _wordService = wordService;
        }

        public override async Task OnConnectedAsync()
        {
            if (_players.Count > 0)
            {
                await Clients.Caller.SendAsync("PopulatePlayers", _players.Select(x => x.Value).Select(x => new { x.Username, x.Money }));

                // if there are players playing, will show the words
                if (_players.Count == MAX_PLAYER)
                    await Clients.Caller.SendAsync("RefreshWord", maskedWord);
            }
        }

        public async Task StartGame()
        {
            // some buffer time before changing the word
            Thread.Sleep(1500);
            await Clients.All.SendAsync("RefreshWord", GetRandomMaskedWord());
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

        public async Task<bool> Login(string username)
        {
            int initialMoney = 100;

            if (_players.Count < MAX_PLAYER && _players.TryAdd(username, new Player(username, initialMoney, Context.ConnectionId)))
            {
                await Clients.All.SendAsync("UserJoined", new { username, money = initialMoney });

                if (_players.Count == MAX_PLAYER)
                    await StartGame();

                return true;
            }

            return false;
        }

        public async Task Logout(string username)
        {
            if (_players.TryRemove(username, out Player player))
                await Clients.All.SendAsync("UserLeaved", username);

            // not enough player, hide the word
            if (_players.Count < MAX_PLAYER)
                await Clients.All.SendAsync("RefreshWord", string.Empty);
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

                        await Clients.All.SendAsync("RefreshWord", maskedWord);

                        player.Money -= 10;
                        await UpdateBalance(username, player.Money);

                        // all masked removed
                        if (maskedWord == currentWord)
                            await StartGame();
                    }
                }
            }
        }

        private Task UpdateBalance(string username, int money) => Clients.All.SendAsync("UpdatePlayer", username, money);
        
        public async Task SubmitAnswer(string username, string answer)
        {
            if (_players.TryGetValue(username, out Player player))
            {
                if (player.Money >= 10)
                {
                    player.Money -= 10;
                    await UpdateBalance(username, player.Money);

                    bool correct = answer == currentWord;
                    await Clients.All.SendAsync("ReceiveAnswer", username, answer, correct);
                    if (correct)
                    {
                        int maskedCount = maskedWord.Select(x => x == '*').Count();
                        int earned = maskedCount * 10;
                        player.Money += earned;

                        await UpdateBalance(username, player.Money);

                        await StartGame();
                    }
                }
            }
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            if (_players.Count > 0)
            {
                string username = _players.FirstOrDefault(entry => entry.Value.ConnectionId == Context.ConnectionId).Key;

                await Logout(username);
            }
            await base.OnDisconnectedAsync(exception);
        }
    }
}