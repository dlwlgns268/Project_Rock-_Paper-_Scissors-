using Networking;
using SO;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using Utils;

namespace GameLogic
{
    public class GameManager : SingleMono<GameManager>
    {
        public Sprite cardBack;
        public CardCollector[] cards;
        public TextMeshProUGUI opponentName;
        public TextMeshProUGUI turnText;
        public TextMeshProUGUI resultText;
        public GameObject resultModal;

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

        public void OpenResultModal(bool isWin)
        {
            resultModal.SetActive(true);
            resultText.text = isWin ? "YOU WIN!" : "YOU LOSE..";
        }

        public void BackToPending()
        {
            SceneManager.LoadScene("Pending");
        }
    }
}
