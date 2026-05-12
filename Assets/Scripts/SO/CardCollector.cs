using UnityEngine;

namespace SO
{
    [CreateAssetMenu(fileName = "CardCollector", menuName = "Scriptable Objects/RPS/CardCollector")]
    public class CardCollector : ScriptableObject
    {
        public int starRate;
        public Sprite[] cards;
    }
}
