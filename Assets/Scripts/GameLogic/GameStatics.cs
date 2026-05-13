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
        public static string RoomId;
        public static string OpponentName;
        public static bool IsMyTurn;
        public static bool IsPlayer1;
    }
}
