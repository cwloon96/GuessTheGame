namespace GuessTheGame.Models
{
    public class Player
    {
        public Player(string username, int money, string connectionId)
        {
            Username = username;
            Money = money;
            ConnectionId = connectionId;
        }

        public string Username { get; set; }

        public int Money { get; set; }

        public string ConnectionId { get; set; }
    }
}