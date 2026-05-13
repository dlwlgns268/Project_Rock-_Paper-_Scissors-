using System;
using GameLogic;
using Networking;
using UnityEngine;
using UnityEngine.UI;

namespace NetAccess
{
    public class PendingPhase : MonoBehaviour
    {
        public GameObject matchingModal;
        public GameObject libraryModal;
        public Button matchingButton;
        public Button drawButton;

        private void Update()
        {
            matchingButton.interactable = GameStatics.HandCards?.Count == 5;
            drawButton.interactable = GameStatics.HandCards?.Count != 5;
        }

        public void DrawCards()
        {
            API.GetCards();
        }

        public void Match()
        {
            API.StartMatch();
            matchingModal.SetActive(true);
        }

        public void Cancel()
        {
            API.CancelMatch();
            matchingModal.SetActive(false);
        }

        public void Library()
        {
            API.GetAllCards().OnResponse(cards =>
            {
                // todo: 가져온 카드 보여주기
            });
            libraryModal.SetActive(true);
        }

        public void Exit()
        {
            Networking.Networking.DisconnectAllSessions();
            Application.Quit();
        }
    }
}
