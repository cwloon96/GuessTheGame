using System.Collections.Generic;

namespace GuessTheGame.Services
{
    public class WordService : IWordService
    {
        private List<string> _words;

        public WordService()
        {
            _words = new List<string>
            {
                "Dota 2",
                "Counter Strike",
                "League of Legend"
            };
        }

        public List<string> GetWords() => _words;
    }
}