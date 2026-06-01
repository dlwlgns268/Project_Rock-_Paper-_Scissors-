using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    public class CatacombsCardDisplay : MonoBehaviour
    {
        public Image image;
        public bool isOpponent;

        private void Update()
        {
            image.enabled = isOpponent 
                ? GameStatics.OpponentCatacombCards.Count > 0
                : GameStatics.CatacombCards.Count > 0;
        }
    }
}
