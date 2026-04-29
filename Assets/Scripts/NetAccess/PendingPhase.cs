using Networking;
using UnityEngine;

namespace NetAccess
{
    public class PendingPhase : MonoBehaviour
    {
        public GameObject matchingModal;
        
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
            // todo: 카드 정보 불러오는 중.. 모달 추가하기
        }

        public void Exit()
        {
            Networking.Networking.DisconnectAllSessions();
            Application.Quit();
        }
    }
}
