namespace GuessTheGame.Models
{
    public class Player
    {
        public Player(string username, int money)
        {
            Username = username;
            Money = money;
        }

        public string Username { get; set; }

        public int Money { get; set; }
    }
}