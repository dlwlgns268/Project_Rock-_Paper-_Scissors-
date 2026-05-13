using Networking;
using SO;
using TMPro;
using UnityEngine;
using Utils;

namespace GameLogic
{
    public class GameManager : SingleMono<GameManager>
    {
        public Sprite cardBack;
        public CardCollector[] cards;
        public TextMeshProUGUI opponentName;
        public TextMeshProUGUI turnText;

        public void Summon()
        {
            if (!GameStatics.IsMyTurn) return;
            API.Summon(CardSelector.SelectedCard);
        }

        public void Attack()
        {
            if (!GameStatics.IsMyTurn) return;
            API.Attack(CardSelector.SelectedCard, CardSelector.SelectedTarget);
        }

        private void Update()
        {
            opponentName.text = GameStatics.OpponentName;
            turnText.text = GameStatics.IsMyTurn ? "My Turn" : "Opponent's Turn";
        }
    }
}
