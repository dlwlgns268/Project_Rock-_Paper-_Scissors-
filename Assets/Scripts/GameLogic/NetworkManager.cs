using System;
using System.Collections.Generic;
using Networking;
using Newtonsoft.Json;
using UnityEngine;

namespace GameLogic
{
    [Serializable]
    public class FieldCardData
    {
        public string instanceId;
        public string cardId;
        public int atk;
        public int hp;
    }

    [Serializable]
    public class FieldUpdateData
    {
        public List<FieldCardData> myField;
        public List<FieldCardData> opponentField;
    }

    [Serializable]
    public class CardDestroyedData
    {
        public string instanceId;
    }

    public class NetworkManager : MonoBehaviour
    {
        public event Action<FieldUpdateData> OnFieldUpdated;
        public event Action<string> OnCardDestroyed;

        private void Start()
        {
            if (Networking.Networking.Instance != null && Networking.Networking.Instance.webSocketClient != null)
            {
                Networking.Networking.Instance.webSocketClient.OnMessageReceived += HandleMessage;
            }
        }

        private void OnDestroy()
        {
            if (Networking.Networking.Instance != null && Networking.Networking.Instance.webSocketClient != null)
            {
                Networking.Networking.Instance.webSocketClient.OnMessageReceived -= HandleMessage;
            }
        }

        public async void RequestSummon(string cardId)
        {
            if (!IsMyTurn()) return;
            if (!WebSocketClient.IsConnectedToService()) return;

            await WebSocketClient.SendMessage("SUMMON", new { cardId });
        }

        public async void RequestAttack(string fromId, string toId)
        {
            if (!IsMyTurn()) return;
            if (!WebSocketClient.IsConnectedToService()) return;

            await WebSocketClient.SendMessage("ATTACK", new { fromId, toId });
        }

        private void HandleMessage(WsMessage wsMessage, string payload)
        {
            try
            {
                switch (wsMessage.type)
                {
                    case "FIELD_UPDATE":
                        var fieldData = JsonConvert.DeserializeObject<FieldUpdateData>(wsMessage.data.ToString());
                        OnFieldUpdated?.Invoke(fieldData);
                        break;
                    case "CARD_DESTROYED":
                        var destroyedData = JsonConvert.DeserializeObject<CardDestroyedData>(wsMessage.data.ToString());
                        if (!string.IsNullOrEmpty(destroyedData.instanceId))
                        {
                            OnCardDestroyed?.Invoke(destroyedData.instanceId);
                        }
                        break;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[NetworkManager] 메시지 처리 실패: {e.Message}");
            }
        }

        private bool IsMyTurn()
        {
            if (TurnSystem.Instance == null)
            {
                return true;
            }

            if (Networking.Networking.LocalPlayerId == null)
            {
                return false;
            }

            return TurnSystem.Instance.IsMyTurn(Networking.Networking.LocalPlayerId.Value);
        }
    }
}
