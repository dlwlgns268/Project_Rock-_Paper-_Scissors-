using UnityEngine;

namespace GameLogic
{
    public class CardSelector : MonoBehaviour
    {
        public static long SelectedCard;
        public static long SelectedTarget;

        public void SelectCard(CardDisplay card)
        {
            SelectedCard = card.card.id;
        }
        
        public void SelectTarget(CardDisplay card)
        {
            SelectedTarget = card.card.id;
        }
    }
}
