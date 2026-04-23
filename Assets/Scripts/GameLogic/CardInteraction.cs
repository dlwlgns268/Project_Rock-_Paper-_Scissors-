using UnityEngine;
using Utils;
using Networking;
using Newtonsoft.Json;
using System.Collections.Generic;

public class CardInteraction : SingleMono<CardInteraction>
{
    private void Start()
    {
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
        if (wsMessage.type == "CARD_INTERACTION_SYNC")
        {
            var syncData = JsonConvert.DeserializeObject<CardInteractionSyncData>(wsMessage.data.ToString());
            ApplyInteraction(syncData);
        }
    }

    /// <summary>
    /// 내 필드 카드가 특정 플레이어의 필드 카드를 대상으로 효과를 발동할 때 호출
    /// </summary>
    /// <param name="sourceCardIndex">효과를 발동할 내 필드 카드 인덱스</param>
    /// <param name="targetPlayerId">대상 플레이어 ID</param>
    /// <param name="targetCardIndex">대상 플레이어의 필드 카드 인덱스</param>
    public async void RequestInteraction(int sourceCardIndex, int targetPlayerId, int targetCardIndex)
    {
        if (Networking.Networking.LocalPlayerId == null)
        {
            Debug.LogError("[CardInteraction] 로컬 플레이어 ID가 초기화되지 않았습니다.");
            return;
        }

        int myPlayerId = Networking.Networking.LocalPlayerId.Value;

        // 1. 내 턴인지 확인
        if (TurnSystem.Instance != null)
        {
            if (!TurnSystem.Instance.IsMyTurn(myPlayerId))
            {
                Debug.LogWarning("[CardInteraction] 자신의 턴이 아닙니다.");
                return;
            }

            // 상호작용은 주로 메인 페이즈나 배틀 페이즈에서 발생한다고 가정
            if (TurnSystem.Instance.CurrentPhase != TurnPhase.Main)
            {
                Debug.LogWarning("[CardInteraction] 메인 페이즈에서만 상호작용할 수 있습니다.");
                return;
            }
        }

        if (CardSummon.Instance == null) return;

        // 내 카드 확인
        var myFieldCards = CardSummon.Instance.GetFieldCards(myPlayerId);
        if (sourceCardIndex < 0 || sourceCardIndex >= myFieldCards.Count)
        {
            Debug.LogWarning("[CardInteraction] 유효하지 않은 내 필드 카드 인덱스입니다.");
            return;
        }

        // 상대 카드 확인
        var targetFieldCards = CardSummon.Instance.GetFieldCards(targetPlayerId);
        if (targetCardIndex < 0 || targetCardIndex >= targetFieldCards.Count)
        {
            Debug.LogWarning("[CardInteraction] 유효하지 않은 대상 필드 카드 인덱스입니다.");
            return;
        }

        // 서버에 상호작용 요청 전송
        if (WebSocketClient.IsConnectedToService())
        {
            var data = new CardInteractionData
            {
                sourceCardIndex = sourceCardIndex,
                targetPlayerId = targetPlayerId,
                targetCardIndex = targetCardIndex
            };
            await WebSocketClient.SendMessage("CARD_INTERACTION_REQUEST", data);
            Debug.Log($"[CardInteraction] 서버에 상호작용 요청을 보냈습니다. (Source: {sourceCardIndex}, TargetPlayer: {targetPlayerId}, TargetCard: {targetCardIndex})");
        }
        else
        {
            // 오프라인/테스트용 로컬 처리
            Debug.LogWarning("[CardInteraction] 서버에 연결되어 있지 않습니다. 로컬에서 처리합니다.");
            ApplyInteraction(new CardInteractionSyncData
            {
                sourcePlayerId = myPlayerId,
                sourceCardIndex = sourceCardIndex,
                targetPlayerId = targetPlayerId,
                targetCardIndex = targetCardIndex,
                effectType = "BASIC_EFFECT"
            });
        }
    }

    /// <summary>
    /// 서버로부터 받은 상호작용 동기화 데이터를 적용하는 메서드
    /// </summary>
    private void ApplyInteraction(CardInteractionSyncData data)
    {
        if (CardSummon.Instance == null) return;

        // 불필요한 GetFieldCards() 호출을 줄이고 직접 Dictionary나 내부 필드에 접근하는 것이 좋으나,
        // 현재 CardSummon의 캡슐화를 존중하여 한 번만 호출하도록 최적화.
        var sourceCards = CardSummon.Instance.GetFieldCards(data.sourcePlayerId);
        var targetCards = CardSummon.Instance.GetFieldCards(data.targetPlayerId);

        // 인덱스 유효성 검사 (상호작용 도중 카드가 파괴되었을 가능성 대비)
        if (data.sourceCardIndex >= 0 && data.sourceCardIndex < sourceCards.Count &&
            data.targetCardIndex >= 0 && data.targetCardIndex < targetCards.Count)
        {
            Card sourceCard = sourceCards[data.sourceCardIndex];
            Card targetCard = targetCards[data.targetCardIndex];

            Debug.Log($"[CardInteraction] 효과 발동: Player {data.sourcePlayerId}의 '{sourceCard.Name}' -> Player {data.targetPlayerId}의 '{targetCard.Name}' (효과: {data.effectType})");
            
            // 실제 게임 로직 처리 예시
            ProcessEffect(data.effectType, data.sourcePlayerId, data.sourceCardIndex, data.targetPlayerId, data.targetCardIndex);
        }
        else
        {
            Debug.LogWarning($"[CardInteraction] 동기화 경고: 대상 카드가 더 이상 존재하지 않거나 인덱스가 유효하지 않습니다. (SourceIdx: {data.sourceCardIndex}, TargetIdx: {data.targetCardIndex})");
        }
    }

    private void ProcessEffect(string effectType, int sourcePlayerId, int sourceCardIndex, int targetPlayerId, int targetCardIndex)
    {
        // 향후 여기에 구체적인 효과 로직을 확장 가능
        switch (effectType)
        {
            case "ATTACK":
                // 예: 공격 시 대상 카드 제거 (현재는 로그만 출력)
                // CardSummon.Instance.RemoveFieldCard(targetPlayerId, targetCardIndex);
                break;
            case "BASIC_EFFECT":
            default:
                // 기본 효과 처리
                break;
        }
    }
}
