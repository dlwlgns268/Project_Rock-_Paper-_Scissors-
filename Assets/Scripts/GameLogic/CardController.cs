using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GameLogic
{
    public class CardController : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        [Header("Card UI")]
        [SerializeField] private Text idText;
        [SerializeField] private Text atkText;
        [SerializeField] private Text hpText;
        [SerializeField] private Image hpBar;

        [Header("Card Meta")]
        [SerializeField] private bool isHandCard;
        [SerializeField] private bool isMine;

        private string instanceId;
        private string cardId;
        private int atk;
        private int hp;

        private Transform originalParent;
        private Vector3 originalPosition;
        private CanvasGroup canvasGroup;

        private static CardController selectedAttacker;

        public string InstanceId => instanceId;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        public void BindFromServer(FieldCardData data, bool mine)
        {
            isMine = mine;
            instanceId = data.instanceId;
            cardId = data.cardId;
            atk = data.atk;
            hp = data.hp;

            RefreshView();
        }

        public void SetAsHandCard(string handCardId)
        {
            isHandCard = true;
            cardId = handCardId;
            if (idText != null) idText.text = handCardId;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!isHandCard) return;

            originalParent = transform.parent;
            originalPosition = transform.position;
            transform.SetParent(originalParent.root);
            canvasGroup.blocksRaycasts = false;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!isHandCard) return;
            transform.position = eventData.position;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!isHandCard) return;

            canvasGroup.blocksRaycasts = true;
            transform.SetParent(originalParent);
            transform.position = originalPosition;

            if (eventData.pointerEnter == null) return;

            var dropZone = eventData.pointerEnter.GetComponentInParent<FieldDropZone>();
            if (dropZone == null || !dropZone.IsMyField) return;

            // 서버 권한 방식: 요청만 보내고, 로컬 상태는 변경하지 않음
            BattleFieldUI.Instance?.NetworkManager?.RequestSummon(cardId);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (isHandCard || string.IsNullOrEmpty(instanceId)) return;

            if (isMine)
            {
                selectedAttacker = this;
                return;
            }

            if (selectedAttacker == null) return;

            // 서버 권한 방식: 공격 요청만 전송
            BattleFieldUI.Instance?.NetworkManager?.RequestAttack(selectedAttacker.InstanceId, instanceId);
            selectedAttacker = null;
        }

        private void RefreshView()
        {
            if (idText != null) idText.text = string.IsNullOrEmpty(instanceId) ? cardId : instanceId;
            if (atkText != null) atkText.text = atk.ToString();
            if (hpText != null) hpText.text = hp.ToString();
            if (hpBar != null)
            {
                var normalizedHp = Mathf.Clamp01(hp / 10f);
                hpBar.fillAmount = normalizedHp;
            }
        }
    }

    public class FieldDropZone : MonoBehaviour
    {
        [SerializeField] private bool isMyField;
        public bool IsMyField => isMyField;
    }

    public class BattleFieldUI : MonoBehaviour
    {
        public static BattleFieldUI Instance { get; private set; }

        [SerializeField] private NetworkManager networkManager;
        [SerializeField] private Transform myFieldRoot;
        [SerializeField] private Transform opponentFieldRoot;
        [SerializeField] private GameObject cardPrefab;

        private readonly Dictionary<string, CardController> cardViews = new Dictionary<string, CardController>();

        public NetworkManager NetworkManager => networkManager;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            if (networkManager == null)
            {
                networkManager = FindObjectOfType<NetworkManager>();
            }

            if (networkManager != null)
            {
                networkManager.OnFieldUpdated += ApplyFieldUpdate;
                networkManager.OnCardDestroyed += RemoveCard;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            if (networkManager != null)
            {
                networkManager.OnFieldUpdated -= ApplyFieldUpdate;
                networkManager.OnCardDestroyed -= RemoveCard;
            }
        }

        private void ApplyFieldUpdate(FieldUpdateData data)
        {
            if (data == null) return;

            RebuildField(data.myField, myFieldRoot, true);
            RebuildField(data.opponentField, opponentFieldRoot, false);
        }

        private void RebuildField(List<FieldCardData> cards, Transform root, bool mine)
        {
            if (cards == null || root == null || cardPrefab == null) return;

            var aliveIds = new HashSet<string>();
            foreach (var cardData in cards)
            {
                if (string.IsNullOrEmpty(cardData.instanceId)) continue;
                aliveIds.Add(cardData.instanceId);

                if (!cardViews.TryGetValue(cardData.instanceId, out var cardView))
                {
                    var go = Instantiate(cardPrefab, root);
                    cardView = go.GetComponent<CardController>();
                    if (cardView == null)
                    {
                        cardView = go.AddComponent<CardController>();
                    }

                    cardViews[cardData.instanceId] = cardView;
                }
                else
                {
                    cardView.transform.SetParent(root, false);
                }

                cardView.BindFromServer(cardData, mine);
            }

            var removeList = new List<string>();
            foreach (var pair in cardViews)
            {
                if (pair.Value == null) continue;
                if (pair.Value.transform.parent != root) continue;
                if (!aliveIds.Contains(pair.Key)) removeList.Add(pair.Key);
            }

            foreach (var id in removeList)
            {
                RemoveCard(id);
            }
        }

        private void RemoveCard(string instanceId)
        {
            if (string.IsNullOrEmpty(instanceId)) return;

            if (cardViews.TryGetValue(instanceId, out var cardView))
            {
                if (cardView != null)
                {
                    Destroy(cardView.gameObject);
                }

                cardViews.Remove(instanceId);
            }
        }
    }
}
