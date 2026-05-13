using Networking;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    public class CardDisplay : MonoBehaviour
    {
        public CardData card;
        public int index;
        public Image image;
        public bool isOpponent;

        private void Start()
        {
            API.GetCardData(isOpponent ? GameStatics.OpponentCards[index] : GameStatics.Cards[index]).OnResponse(c =>
            {
                card = c;
                image.sprite = GameManager.Instance.cards[card.starRate - 3].cards[card.indexByStar];
            }).Build();
        }
    }
}
