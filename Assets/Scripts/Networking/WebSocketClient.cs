using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using NativeWebSocket;
using Newtonsoft.Json;
using UnityEngine;

namespace Networking
{
    public class WebSocketClient : MonoBehaviour
    {
        private static WebSocket _websocket;
        private const string ServerUrl = "wss://rps-backend.thinkinggms.com/ws";

        public bool IsConnected { get; private set; }
        public bool IsConnecting { get; private set; }

        public async void ConnectOn()
        {
            try
            {
                if ((_websocket == null || _websocket.State == WebSocketState.Closed) && Networking.AccessToken != null) await Connect(Networking.AccessToken);
            }
            catch (Exception)
            {
                // ignored
            }
        }

        private async Task Connect(string authToken = "")
        {
            if (IsConnected || IsConnecting) return;

            IsConnecting = true;

            var dictionary = new Dictionary<string, string>();
            if (!authToken.Equals("")) dictionary.Add("Authorization", $"Bearer {authToken}");

            _websocket = new WebSocket(ServerUrl, dictionary);

            _websocket.OnOpen += () =>
            {
                Debug.Log("Connection open");
                IsConnected = true;
                IsConnecting = false;
            };

            _websocket.OnError += e =>
            {
                Debug.LogError($"Error: {e}");
                IsConnected = false;
                IsConnecting = false;
            };

            _websocket.OnClose += _ =>
            {
                Debug.Log("Connection closed");
                IsConnected = false;
                IsConnecting = false;
            };

            _websocket.OnMessage += bytes =>
            {
                var message = Encoding.UTF8.GetString(bytes);
                HandleMessage(message);
            };

            await _websocket.Connect();
        }

        public async Task DisconnectAsync()
        {
            if (_websocket != null)
            {
                await _websocket.Close();
                _websocket = null;
            }
        }

        private void Update()
        {
            ConnectOn();
#if !UNITY_WEBGL || UNITY_EDITOR
            _websocket?.DispatchMessageQueue();
#endif
        }

        private void HandleMessage(string message)
        {
            var command = JsonConvert.DeserializeObject<Void>(message);
            switch ("match/matched")
            {
                case "match/matched":
                    // 뭐라도 써봐
                    break;
            }
        }

        public static async Task Message(string message)
        {
            if (_websocket.State == WebSocketState.Open)
            {
                await _websocket.SendText(message);
            }
        }
    }
}
