using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using NativeWebSocket;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Networking
{
    public class WebSocketClient : MonoBehaviour
    {
        private static WebSocket _websocket;
        private const string ServerUrl = "wss://rps-backend.thinkinggms.com/ws";

        public bool IsConnected { get; private set; }
        public bool IsConnecting { get; private set; }

        public event Action<WsMessage, string> OnMessageReceived; // WsMessage, original payload

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
                StartCoroutine(PingCheck());
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

        private IEnumerator PingCheck()
        {
            while (IsConnected)
            {
                API.Ping();
                yield return new WaitForSeconds(10f);
            }
        }

        public new static async Task SendMessage(string type, object data)
        {
            if (_websocket == null || _websocket.State != WebSocketState.Open) return;

            var wsMessage = WsMessage.Of(type, data);
            var json = JsonConvert.SerializeObject(wsMessage);
            await _websocket.SendText(json);
        }

        private void HandleMessage(string message)
        {
            try
            {
                var wsMessage = JsonConvert.DeserializeObject<WsMessage>(message);
                if (wsMessage == null || string.IsNullOrEmpty(wsMessage.type)) return;

                Debug.Log($"Received message: {wsMessage.type}");
                switch (wsMessage.type)
                {
                    case "PONG":
                        break;
                    case "CONNECTED":
                        SceneManager.LoadScene("Pending");
                        break;
                    case "MATCH_FOUND":
                        SceneManager.LoadScene("MainGame");
                        break;
                }

                OnMessageReceived?.Invoke(wsMessage, message);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error parsing message: {e.Message}\n{message}");
            }
        }

        public static bool IsConnectedToService() => _websocket != null && _websocket.State == WebSocketState.Open;

        public static async Task Message(string message)
        {
            if (_websocket.State == WebSocketState.Open)
            {
                await _websocket.SendText(message);
            }
        }
    }
}
