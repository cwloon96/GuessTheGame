using System.Collections.Generic;

namespace GuessTheGame.Services
{
    public interface IWordService
    {
        List<string> GetWords();
    }
}