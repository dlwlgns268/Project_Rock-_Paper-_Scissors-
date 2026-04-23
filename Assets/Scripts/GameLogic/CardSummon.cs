using System.Collections.Generic;
using UnityEngine;
using Utils;
using Networking;
using Newtonsoft.Json;

public class Card
{
    public string Name;
    public int Id;

    public Card(int id, string name)
    {
        Id = id;
        Name = name;
    }
}

public class CardSummon : SingleMono<CardSummon>
{
    private List<Card> handCards = new List<Card>();
    private Dictionary<int, List<Card>> fieldCardsPerPlayer = new Dictionary<int, List<Card>>();

    private const int MAX_HAND = 5;
    private const int MAX_FIELD = 2;

    private void Start()
    {
        // 필드 상태 초기화 (예: 2인 플레이어)
        fieldCardsPerPlayer[0] = new List<Card>();
        fieldCardsPerPlayer[1] = new List<Card>();

        // 게임 시작 전 5장을 얻게 됨
        GetInitialCards();

        if (Networking.Networking.Instance != null && Networking.Networking.Instance.webSocketClient != null)
        {
            Networking.Networking.Instance.webSocketClient.OnMessageReceived += HandleNetworkMessage;
        }
    }

    private void OnDestroy()
    {
        if (Networking.Networking.Instance != null && Networking.Networking.Instance.webSocketClient != null)
        {
            Networking.Networking.Instance.webSocketClient.OnMessageReceived -= HandleNetworkMessage;
        }
    }

    private void HandleNetworkMessage(WsMessage wsMessage, string payload)
    {
        if (wsMessage.type == "CARD_SUMMON_SYNC")
        {
            var syncData = JsonConvert.DeserializeObject<CardSummonSyncData>(wsMessage.data.ToString());
            ApplySummonCard(syncData);
        }
    }

    private void GetInitialCards()
    {
        for (int i = 0; i < MAX_HAND; i++)
        {
            handCards.Add(new Card(i, $"Card {i}"));
        }
        Debug.Log($"[CardSummon] 5장의 카드를 획득했습니다. 현재 패: {handCards.Count}장");
    }

    /// <summary>
    /// 카드를 소환 요청하는 메서드 (멀티플레이어 대응)
    /// </summary>
    /// <param name="cardIndex">패에서의 카드 인덱스</param>
    public async void SummonCard(int cardIndex)
    {
        // 1. 내 턴인지 및 메인 페이즈인지 확인
        int myPlayerId = Networking.Networking.LocalPlayerId ?? 0; // 로컬 ID가 없으면 0으로 가정 (테스트용)
        
        if (TurnSystem.Instance != null)
        {
            if (!TurnSystem.Instance.IsMyTurn(myPlayerId))
            {
                Debug.LogWarning("[CardSummon] 자신의 턴이 아닙니다.");
                return;
            }

            if (TurnSystem.Instance.CurrentPhase != TurnPhase.Main)
            {
                Debug.LogWarning("[CardSummon] 메인 페이즈에서만 소환할 수 있습니다.");
                return;
            }
        }

        if (cardIndex < 0 || cardIndex >= handCards.Count)
        {
            Debug.LogWarning("[CardSummon] 유효하지 않은 카드 인덱스입니다.");
            return;
        }

        if (!fieldCardsPerPlayer.ContainsKey(myPlayerId)) fieldCardsPerPlayer[myPlayerId] = new List<Card>();
        if (fieldCardsPerPlayer[myPlayerId].Count >= MAX_FIELD)
        {
            Debug.LogWarning("[CardSummon] 필드에는 최대 2장까지만 소환할 수 있습니다.");
            return;
        }

        // 서버에 소환 요청 전송
        if (WebSocketClient.IsConnectedToService())
        {
            await WebSocketClient.SendMessage("CARD_SUMMON_REQUEST", new CardSummonData { cardIndex = cardIndex });
            Debug.Log($"[CardSummon] 서버에 카드 소환 요청을 보냈습니다. (Index: {cardIndex})");
        }
        else
        {
            // 오프라인/테스트용 로컬 처리 (필요 시)
            Debug.LogWarning("[CardSummon] 서버에 연결되어 있지 않습니다. 로컬에서 처리합니다.");
            Card cardToSummon = handCards[cardIndex];
            handCards.RemoveAt(cardIndex);
            fieldCardsPerPlayer[myPlayerId].Add(cardToSummon);
            Debug.Log($"[CardSummon] 로컬 소환: '{cardToSummon.Name}' (필드: {fieldCardsPerPlayer[myPlayerId].Count}/{MAX_FIELD})");
        }
    }

    /// <summary>
    /// 서버로부터 받은 소환 동기화 데이터를 적용하는 메서드
    /// </summary>
    private void ApplySummonCard(CardSummonSyncData data)
    {
        // 2. 누가 소환했는지 구분하여 처리
        int myPlayerId = Networking.Networking.LocalPlayerId ?? 0;
        bool isMySummon = data.playerId == myPlayerId;

        if (!fieldCardsPerPlayer.ContainsKey(data.playerId)) fieldCardsPerPlayer[data.playerId] = new List<Card>();

        if (isMySummon)
        {
            // 내 소환인 경우 이미 로컬에서 처리했는지 확인 (로컬 처리 시 이미 handCards에서 제거됨)
            // 서버에서 오는 동기화 데이터는 확인용으로만 사용하거나, 로컬 처리를 서버 SYNC 이후로 미룰 필요가 있음.
            // 현재 구조(BoomCard 등)를 보면 서버 미연결 시에만 로컬 처리를 하므로, 
            // SYNC가 왔다는 것은 서버를 거쳐왔음을 의미함.
            
            // 만약 서버를 거치지 않은 로컬 소환을 이미 수행했다면 여기서 다시 지우지 않도록 방어 로직 추가
            if (data.cardIndex >= 0 && data.cardIndex < handCards.Count)
            {
                Card cardToSummon = handCards[data.cardIndex];
                handCards.RemoveAt(data.cardIndex);
                fieldCardsPerPlayer[data.playerId].Add(cardToSummon);
                Debug.Log($"[CardSummon] 내 카드 서버 동기화 완료: '{cardToSummon.Name}' (필드: {fieldCardsPerPlayer[data.playerId].Count}/{MAX_FIELD})");
            }
            else
            {
                // 이미 로컬에서 처리되어 리스트가 비어있거나 인덱스가 바뀐 경우일 수 있음.
                // 이 경우 필드에 이미 추가되었는지 확인이 필요함.
                Debug.LogWarning($"[CardSummon] 내 카드 서버 동기화 건너뜀 (이미 처리되었거나 인덱스 불일치): {data.cardIndex}");
            }
        }
        else
        {
            // 타인의 소환인 경우 내 패와 상관없이 필드에만 추가 (또는 상대 패 관리 시스템이 있다면 거기서 제거)
            Card remoteCard = new Card(-1, data.cardName);
            fieldCardsPerPlayer[data.playerId].Add(remoteCard);
            Debug.Log($"[CardSummon] 상대방 카드 소환 동기화: '{data.cardName}' (Player: {data.playerId}, 필드: {fieldCardsPerPlayer[data.playerId].Count}/{MAX_FIELD})");
        }
    }

    public List<Card> GetHandCards() => handCards;
    public List<Card> GetFieldCards(int playerId) => fieldCardsPerPlayer.ContainsKey(playerId) ? fieldCardsPerPlayer[playerId] : new List<Card>();

    /// <summary>
    /// 특정 플레이어의 필드에서 카드를 제거하는 메서드
    /// </summary>
    public void RemoveFieldCard(int playerId, int fieldCardIndex)
    {
        if (fieldCardsPerPlayer.ContainsKey(playerId))
        {
            var fieldCards = fieldCardsPerPlayer[playerId];
            if (fieldCardIndex >= 0 && fieldCardIndex < fieldCards.Count)
            {
                Card removedCard = fieldCards[fieldCardIndex];
                fieldCards.RemoveAt(fieldCardIndex);
                Debug.Log($"[CardSummon] 카드 파괴 동기화: '{removedCard.Name}' (Player: {playerId}, 필드: {fieldCards.Count}/{MAX_FIELD})");
            }
            else
            {
                Debug.LogError($"[CardSummon] 파괴 오류: 인덱스 {fieldCardIndex}가 플레이어 {playerId}의 필드 범위를 벗어남");
            }
        }
    }
}
