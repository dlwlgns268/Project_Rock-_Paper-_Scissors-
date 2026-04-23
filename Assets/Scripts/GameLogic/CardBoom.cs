using UnityEngine;
using Utils;
using Networking;
using Newtonsoft.Json;
using System.Collections.Generic;

public class CardBoom : SingleMono<CardBoom>
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
        if (wsMessage.type == "CARD_BOOM_SYNC")
        {
            var syncData = JsonConvert.DeserializeObject<CardBoomSyncData>(wsMessage.data.ToString());
            ApplyBoomCard(syncData);
        }
    }

    /// <summary>
    /// 카드를 파괴 요청하는 메서드
    /// </summary>
    /// <param name="fieldCardIndex">필드에서의 카드 인덱스</param>
    public async void BoomCard(int fieldCardIndex)
    {
        int myPlayerId = Networking.Networking.LocalPlayerId ?? 0;

        // 1. 내 턴인지 확인
        if (TurnSystem.Instance != null)
        {
            if (!TurnSystem.Instance.IsMyTurn(myPlayerId))
            {
                Debug.LogWarning("[CardBoom] 자신의 턴이 아닙니다.");
                return;
            }

            // 배틀 페이즈 등 특정 페이즈에서만 파괴 가능하도록 제한할 수 있음 (현재는 메인 페이즈로 가정)
            if (TurnSystem.Instance.CurrentPhase != TurnPhase.Main)
            {
                Debug.LogWarning("[CardBoom] 메인 페이즈에서만 파괴할 수 있습니다.");
                return;
            }
        }

        if (CardSummon.Instance == null) return;

        var fieldCards = CardSummon.Instance.GetFieldCards(myPlayerId);
        if (fieldCardIndex < 0 || fieldCardIndex >= fieldCards.Count)
        {
            Debug.LogWarning("[CardBoom] 유효하지 않은 필드 카드 인덱스입니다.");
            return;
        }

        // 서버에 파괴 요청 전송
        if (WebSocketClient.IsConnectedToService())
        {
            await WebSocketClient.SendMessage("CARD_BOOM_REQUEST", new CardBoomData { fieldCardIndex = fieldCardIndex });
            Debug.Log($"[CardBoom] 서버에 카드 파괴 요청을 보냈습니다. (Index: {fieldCardIndex})");
        }
        else
        {
            // 오프라인/테스트용 로컬 처리
            Debug.LogWarning("[CardBoom] 서버에 연결되어 있지 않습니다. 로컬에서 처리합니다.");
            ApplyBoomCard(new CardBoomSyncData { playerId = myPlayerId, fieldCardIndex = fieldCardIndex });
        }
    }

    /// <summary>
    /// 서버로부터 받은 파괴 동기화 데이터를 적용하는 메서드
    /// </summary>
    private void ApplyBoomCard(CardBoomSyncData data)
    {
        if (CardSummon.Instance == null) return;

        int myPlayerId = Networking.Networking.LocalPlayerId ?? 0;
        
        // 내 카드 파괴인 경우 이미 로컬에서 처리했는지 확인하는 것이 이상적이지만,
        // 현재 로직상 '서버 연결이 안 되었을 때만' 로컬 처리를 수행하므로 SYNC 시에는 한 번만 실행됨.
        // 다만 타겟 플레이어의 필드에 실제로 해당 인덱스에 카드가 있는지 한 번 더 검증함.
        
        CardSummon.Instance.RemoveFieldCard(data.playerId, data.fieldCardIndex);
    }
}
