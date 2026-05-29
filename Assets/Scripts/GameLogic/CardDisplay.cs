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
        public Sprite cardImage;
        public bool isOpponent;

        protected virtual void Start()
        {
            API.GetCardData(isOpponent ? GameStatics.OpponentHandCards[index] : GameStatics.HandCards[index]).OnResponse(c =>
            {
                card = c;
                cardImage = GameManager.Instance.cards[card.starRate - 3].cards[card.indexByStar];
            }).Build();
        }

        protected virtual void Update()
        {
            gameObject.SetActive(!GameStatics.FieldCards.Contains(card.id));
            image.sprite = isOpponent && !GameStatics.OpponentFieldCards.Contains(card.id) ? GameManager.Instance.cardBack : cardImage;
        }
    }
}
