using System;
using System.Collections.Generic;

namespace Networking
{
    [Serializable]
    public class ErrorBody
    {
        public int errorId;
        public string message;
    }

    [Serializable]
    public class LoginRequest
    {
        public string username;
        public string password;

        public LoginRequest(string username, string password)
        {
            this.username = username;
            this.password = password;
        }
    }

    [Serializable]
    public class JwtResponse
    {
        public string token;
    }

    public class WsMessage
    {
        public string type;
        public object data;

        public static WsMessage Of(string type, object data) => new WsMessage { type = type, data = data };
    }

    [Serializable]
    public class CardSummonData
    {
        public int cardIndex;
    }

    [Serializable]
    public class CardSummonSyncData
    {
        public int playerId;
        public int cardIndex;
        public string cardName;
    }

    [Serializable]
    public class CardBoomData
    {
        public int fieldCardIndex;
    }

    [Serializable]
    public class CardBoomSyncData
    {
        public int playerId;
        public int fieldCardIndex;
    }

    [Serializable]
    public class CardInteractionData
    {
        public int sourceCardIndex; // 내 필드 카드 인덱스
        public int targetPlayerId;  // 대상 플레이어 ID
        public int targetCardIndex; // 대상 플레이어의 필드 카드 인덱스
    }

    [Serializable]
    public class CardInteractionSyncData
    {
        public int sourcePlayerId;
        public int sourceCardIndex;
        public int targetPlayerId;
        public int targetCardIndex;
        public string effectType; // 효과 종류 (예: ATTACK, HEAL 등)
    }
    
    public class Void { }
}