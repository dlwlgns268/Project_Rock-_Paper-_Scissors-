using Networking;

namespace GameLogic
{
    public class FieldCardDisplay : CardDisplay
    {
        protected override void Start() {}
        
        protected override void Update()
        {
            if (isOpponent)
            {
                image.enabled = GameStatics.OpponentFieldCards.Count > index;
                if (GameStatics.OpponentFieldCards.Count <= index) return;
                if (card.id != GameStatics.OpponentFieldCards[index]) UpdateCardData();
            }
            else
            {
                image.enabled = GameStatics.FieldCards.Count > index;
                if (GameStatics.FieldCards.Count <= index) return;
                if (card.id != GameStatics.FieldCards[index]) UpdateCardData();
            }
            image.sprite = cardImage;
        }

        private void UpdateCardData()
        {
            API.GetCardData(isOpponent ? GameStatics.OpponentFieldCards[index] : GameStatics.FieldCards[index]).OnResponse(c =>
            {
                card = c;
                cardImage = GameManager.Instance.cards[card.starRate - 3].cards[card.indexByStar];
            }).Build();
        }
    }
}
