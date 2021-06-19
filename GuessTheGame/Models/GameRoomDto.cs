using System;

namespace GuessTheGame.Models
{
    public class GameRoomDto
    {
        public Guid Id { get; set; }

        public int PlayerCount { get; set; }

        public int SpectatorCount { get; set; }
    }
}