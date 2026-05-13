using System.Collections.Generic;
using System.Threading.Tasks;

namespace Networking
{
    public static class API
    {
        public static Networking.Get<Void> Log(string message)
        {
            return new Networking.Get<Void>($"/debug/log?message={message}");
        }

        public static Networking.Post<JwtResponse> Login(string username, string password)
        {
            return new Networking.Post<JwtResponse>("/api/auth/login", new LoginRequest(username, password));
        }

        public static Networking.Post<JwtResponse> Signup(string username, string password)
        {
            return new Networking.Post<JwtResponse>("/api/auth/signup", new LoginRequest(username, password));
        }

        public static Networking.Get<List<CardData>> GetAllCards()
        {
            return new Networking.Get<List<CardData>>("/api/cards");
        }

        public static Networking.Get<CardData> GetCardData(long id)
        {
            return new Networking.Get<CardData>($"/api/cards/{id}");
        }

        public static Task Ping()
        {
            return WebSocketClient.SendMessage("PING", null);
        }

        public static Task GetCards()
        {
            return WebSocketClient.SendMessage("DRAW_CARDS", null);
        }

        public static Task StartMatch()
        {
            return WebSocketClient.SendMessage("JOIN_MATCHMAKING", null);
        }

        public static Task CancelMatch()
        {
            return WebSocketClient.SendMessage("CANCEL_MATCHMAKING", null);
        }

        public static Task GameStartRequest()
        {
            return WebSocketClient.SendMessage("GAME_START_REQUEST", null);
        }

        public static Task Summon(long cardId)
        {
            return WebSocketClient.SendMessage("SUMMON", new SummonData
            {
                cardId = cardId
            });
        }

        public static Task Attack(long cardId, long targetId)
        {
            return WebSocketClient.SendMessage("ATTACK", new AttackData
            { 
                attackerCardId = cardId,
                targetCardId = targetId
            });
        }
    }
}