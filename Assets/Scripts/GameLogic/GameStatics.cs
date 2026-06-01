using System.Collections.Generic;
using UnityEngine;

namespace GameLogic
{
    public class GameStatics : MonoBehaviour
    {
        public static List<long> HandCards;
        public static List<long> OpponentHandCards;
        public static List<long> FieldCards;
        public static List<long> OpponentFieldCards;
        public static List<long> CatacombCards;
        public static List<long> OpponentCatacombCards;
        public static string RoomId;
        public static string OpponentName;
        public static bool IsMyTurn;
        public static bool IsPlayer1;

        public static void ResetGameStatic()
        {
            HandCards = new List<long>();
            OpponentHandCards = new List<long>();
            FieldCards = new List<long>();
            OpponentFieldCards = new List<long>();
            CatacombCards = new List<long>();
            OpponentCatacombCards = new List<long>();
            RoomId = null;
            OpponentName = null;
            IsMyTurn = false;
            IsPlayer1 = false;
        }
    }
}
