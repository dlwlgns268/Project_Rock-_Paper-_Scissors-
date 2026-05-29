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
                gameObject.SetActive(GameStatics.OpponentFieldCards.Count > index);
                if (card.id != GameStatics.OpponentFieldCards[index]) UpdateCardData();
            }
            else
            {
                gameObject.SetActive(GameStatics.FieldCards.Count > index);
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
