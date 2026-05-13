using Networking;
using SO;
using Utils;

namespace GameLogic
{
    public class GameManager : SingleMono<GameManager>
    {
        public CardCollector[] cards;

        public void Summon()
        {
            API.Summon(CardSelector.SelectedCard);
        }

        public void Attack()
        {
            API.Attack(CardSelector.SelectedCard, CardSelector.SelectedTarget);
        }
    }
}
