using UnityEngine;
using System;
using System.Collections.Generic;
using Utils;
using Networking;
using Newtonsoft.Json;

public enum TurnPhase { Main, Battle, End }

public class TurnSystem : SingleMono<TurnSystem>
{
    public class GameStartData { public int totalPlayers; public int startingPlayer; public List<int> turnOrder; }
    public class TurnSyncData { public int playerId; public int turnCount; public TurnPhase phase; public float remainingTime; }
    public class PhaseSyncData { public TurnPhase phase; public float remainingTime; }
    public class SkipSyncData { public int playerId; public bool skip; }
    public class PlayerDisconnectData { public int playerId; }

    public event Action<int> OnTurnChanged;
    public event Action<int> OnTurnStarted;
    public event Action<int> OnTurnEnded;
    public event Action<float> OnTimerUpdated;
    public event Action<TurnPhase> OnPhaseChanged; // 페이즈 변경 이벤트
    
    [Header("Settings")]
    [SerializeField] private int totalPlayers = 2;
    [SerializeField] private float turnTimeLimit = 30f;
    
    private int currentPlayerTurn = 0;
    private int turnCount = 1; // 턴 카운트 (로그용)
    private float currentTurnTimer;
    private bool isGameActive = false;
    private TurnPhase currentPhase = TurnPhase.Main;
    
    // 턴 스킵을 위한 상태 관리
    private bool[] playerSkipNextTurn;
    
    private Coroutine turnTimerCoroutine;
    private WaitForSeconds timerWait = new WaitForSeconds(0.1f);
    
    private float pauseTime;
    private bool isPaused = false;
    
    // 턴 순서 관리를 위한 리스트 ( modulo 대신 사용 가능 )
    private System.Collections.Generic.List<int> turnOrder = new System.Collections.Generic.List<int>();
    
    public int CurrentPlayerTurn => currentPlayerTurn;
    public int TurnCount => turnCount;
    public float CurrentTurnTimer => currentTurnTimer;
    public bool IsMyTurn(int playerId) => currentPlayerTurn == playerId;
    public TurnPhase CurrentPhase => currentPhase;
    public bool IsPaused => isPaused;
    
    private void Start()
    {
        playerSkipNextTurn = new bool[totalPlayers];
        InitializeTurnOrder();

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
        try
        {
            switch (wsMessage.type)
            {
                case "GAME_START":
                    var startData = JsonConvert.DeserializeObject<GameStartData>(wsMessage.data.ToString());
                    ApplyGameStart(startData);
                    break;
                case "TURN_START":
                    var turnData = JsonConvert.DeserializeObject<TurnSyncData>(wsMessage.data.ToString());
                    ApplyTurnStart(turnData);
                    break;
                case "PHASE_CHANGE":
                    var phaseData = JsonConvert.DeserializeObject<PhaseSyncData>(wsMessage.data.ToString());
                    ApplyPhaseChange(phaseData);
                    break;
                case "SKIP_SYNC":
                    var skipData = JsonConvert.DeserializeObject<SkipSyncData>(wsMessage.data.ToString());
                    SetSkipNextTurn(skipData.playerId, skipData.skip);
                    break;
                case "PLAYER_DISCONNECTED":
                    var disconnectData = JsonConvert.DeserializeObject<PlayerDisconnectData>(wsMessage.data.ToString());
                    HandlePlayerDisconnected(disconnectData.playerId);
                    break;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"TurnSystem: Network message handling error: {e.Message}");
        }
    }

    private void ApplyGameStart(GameStartData data)
    {
        totalPlayers = data.totalPlayers;
        UpdateTurnOrder(data.turnOrder);
        
        // 내 플레이어 ID를 설정 (시작 시점에 서버에서 준 데이터에 포함되어 있다고 가정하거나, 다른 경로로 얻어야 함)
        // 일단 GameStartData에 myPlayerId가 있다면 좋겠지만, 없다면 일단 시작 플레이어와 비교하는 등 다른 방식 필요
        // (보통 서버에서 본인의 ID를 알려주는 메시지가 따로 있거나, 시작 시 알려줌)
        // 만약 Networking.LocalPlayerId가 아직 null이라면, 여기서 설정하는 로직이 필요할 수 있음
        
        StartGame();
        currentPlayerTurn = data.startingPlayer;
    }

    private void ApplyTurnStart(TurnSyncData data)
    {
        StopTurnTimer();
        currentPlayerTurn = data.playerId;
        turnCount = data.turnCount;
        currentPhase = data.phase;
        currentTurnTimer = data.remainingTime;
        StartTurnTimer();
        
        OnTurnStarted?.Invoke(currentPlayerTurn);
        OnPhaseChanged?.Invoke(currentPhase);
    }

    private void ApplyPhaseChange(PhaseSyncData data)
    {
        currentPhase = data.phase;
        currentTurnTimer = data.remainingTime;
        ResetTimer(); // Or apply specific remaining time if server dictates
        OnPhaseChanged?.Invoke(currentPhase);
    }

    private void InitializeTurnOrder()
    {
        turnOrder.Clear();
        for (int i = 0; i < totalPlayers; i++)
        {
            turnOrder.Add(i);
        }
    }

    private System.Collections.IEnumerator TurnTimerCoroutine()
    {
        float remainingTime = turnTimeLimit;
        float lastUpdateTime = Time.time;

        while (isGameActive && remainingTime > 0)
        {
            if (isPaused)
            {
                yield return timerWait;
                continue;
            }

            yield return timerWait;
            
            float deltaTime = 0.1f; // timerWait와 일치시킴
            remainingTime -= deltaTime;
            currentTurnTimer = Mathf.Max(0, remainingTime);

            float currentTime = Time.time;
            if (currentTime - lastUpdateTime >= 0.09f)
            {
                var timerUpdated = OnTimerUpdated;
                if (timerUpdated != null) timerUpdated(currentTurnTimer);
                lastUpdateTime = currentTime;
            }
        }

        if (isGameActive && remainingTime <= 0)
        {
            currentTurnTimer = 0;
            var timerUpdated = OnTimerUpdated;
            if (timerUpdated != null) timerUpdated(0);
            Debug.Log($"[Turn {turnCount}] 시간 초과! Player {currentPlayerTurn}의 턴을 강제 종료합니다.");
            EndTurn(currentPlayerTurn);
        }
    }
    
    public void StartGame()
    {
        StopTurnTimer();
        InitializeTurnOrder();
        currentPlayerTurn = turnOrder[0];
        turnCount = 1;
        isGameActive = true;
        isPaused = false;
        currentPhase = TurnPhase.Main;
        
        // 스킵 상태 초기화
        if (playerSkipNextTurn == null || playerSkipNextTurn.Length != totalPlayers)
            playerSkipNextTurn = new bool[totalPlayers];
        for (int i = 0; i < totalPlayers; i++) playerSkipNextTurn[i] = false;

        ResetTimer();
        StartTurnTimer();
        
        var turnStarted = OnTurnStarted;
        if (turnStarted != null) turnStarted(currentPlayerTurn);
        var phaseChanged = OnPhaseChanged;
        if (phaseChanged != null) phaseChanged(currentPhase);
        Debug.Log($"게임 시작! [Turn {turnCount}] Player {currentPlayerTurn}의 {currentPhase} 페이즈입니다.");
    }

    /// <summary>
    /// 페이즈를 다음 단계로 전환 (Main -> Battle -> End -> 다음 플레이어 턴)
    /// </summary>
    public async void NextPhase()
    {
        if (!isGameActive) return;

        // If network is active, we should request phase change to server
        if (Networking.WebSocketClient.IsConnectedToService())
        {
            await Networking.WebSocketClient.SendMessage("NEXT_PHASE_REQUEST", new { playerId = currentPlayerTurn, currentPhase = currentPhase });
            return;
        }

        switch (currentPhase)
        {
            case TurnPhase.Main:
                currentPhase = TurnPhase.Battle;
                break;
            case TurnPhase.Battle:
                currentPhase = TurnPhase.End;
                break;
            case TurnPhase.End:
                EndTurn(currentPlayerTurn);
                return;
        }

        ResetTimer(); // 페이즈 전환 시 타이머 리셋 여부는 게임 룰에 따라 조정 가능
        var phaseChanged = OnPhaseChanged;
        if (phaseChanged != null) phaseChanged(currentPhase);
        Debug.Log($"[Turn {turnCount}] Player {currentPlayerTurn}의 {currentPhase} 페이즈로 전환되었습니다.");
    }
    
    public async void EndTurn(int requestingPlayerId)
    {
        if (!isGameActive) return;
        
        if (requestingPlayerId != currentPlayerTurn)
        {
            Debug.LogWarning($"잘못된 턴 종료 요청: 현재 {currentPlayerTurn}, 요청자 {requestingPlayerId}");
            return;
        }

        if (Networking.WebSocketClient.IsConnectedToService())
        {
            await Networking.WebSocketClient.SendMessage("END_TURN_REQUEST", new { playerId = requestingPlayerId });
            return;
        }

        StopTurnTimer();

        var turnEnded = OnTurnEnded;
        if (turnEnded != null) turnEnded(currentPlayerTurn);
        Debug.Log($"[Turn {turnCount}] Player {currentPlayerTurn}의 턴이 종료되었습니다.");
        
        NextTurn();
    }
    
    private void NextTurn()
    {
        int currentIndex = turnOrder.IndexOf(currentPlayerTurn);
        int nextIndex = (currentIndex + 1) % turnOrder.Count;
        int nextPlayer = turnOrder[nextIndex];
        int safetyCounter = 0;

        // 턴 스킵 체크 (루프로 변경하여 재귀 방지)
        while (playerSkipNextTurn[nextPlayer] && safetyCounter < totalPlayers)
        {
            Debug.Log($"Player {nextPlayer}의 턴이 스킵되었습니다.");
            playerSkipNextTurn[nextPlayer] = false; // 소모됨
            nextIndex = (nextIndex + 1) % turnOrder.Count;
            nextPlayer = turnOrder[nextIndex];
            safetyCounter++;
        }

        currentPlayerTurn = nextPlayer;
        turnCount++;
        currentPhase = TurnPhase.Main;
        ResetTimer();
        
        StartTurnTimer();
        
        var turnChanged = OnTurnChanged;
        if (turnChanged != null) turnChanged(currentPlayerTurn);
        var turnStarted = OnTurnStarted;
        if (turnStarted != null) turnStarted(currentPlayerTurn);
        var phaseChanged = OnPhaseChanged;
        if (phaseChanged != null) phaseChanged(currentPhase);
        Debug.Log($"[Turn {turnCount}] Player {currentPlayerTurn}의 턴이 시작되었습니다.");
    }
    
    /// <summary>
    /// 타이머 일시 정지 (연출이나 애니메이션 시 사용)
    /// </summary>
    public void SetPause(bool pause)
    {
        isPaused = pause;
        Debug.Log(pause ? "턴 타이머 일시 정지" : "턴 타이머 재개");
    }

    /// <summary>
    /// 턴 순서를 강제로 변경 (특수 카드 효과 등)
    /// </summary>
    public void UpdateTurnOrder(System.Collections.Generic.List<int> newOrder)
    {
        if (newOrder != null && newOrder.Count > 0)
        {
            turnOrder = new System.Collections.Generic.List<int>(newOrder);
        }
    }

    /// <summary>
    /// 특정 플레이어의 다음 턴을 스킵하도록 설정
    /// </summary>
    public void SetSkipNextTurn(int playerId, bool skip)
    {
        if (playerId >= 0 && playerId < totalPlayers)
        {
            playerSkipNextTurn[playerId] = skip;
        }
    }

    /// <summary>
    /// 플레이어가 탈주했을 때 호출하여 턴을 강제로 넘기거나 게임을 정지
    /// </summary>
    public void HandlePlayerDisconnected(int playerId)
    {
        if (!isGameActive) return;
        
        Debug.LogWarning($"Player {playerId} 연결 끊김 감지.");
        
        if (currentPlayerTurn == playerId)
        {
            EndTurn(playerId);
        }
        else
        {
            // 아직 자기 턴이 아니면 다음 턴을 스킵하도록 설정
            SetSkipNextTurn(playerId, true);
        }
    }

    private void StartTurnTimer()
    {
        StopTurnTimer();
        if (isGameActive)
        {
            turnTimerCoroutine = StartCoroutine(TurnTimerCoroutine());
        }
    }

    private void StopTurnTimer()
    {
        if (turnTimerCoroutine != null)
        {
            StopCoroutine(turnTimerCoroutine);
            turnTimerCoroutine = null;
        }
    }

    private void ResetTimer()
    {
        currentTurnTimer = turnTimeLimit;
    }
    
    public void SetTotalPlayers(int count)
    {
        totalPlayers = count;
        playerSkipNextTurn = new bool[totalPlayers];
        InitializeTurnOrder();
    }

    public void StopGame()
    {
        isGameActive = false;
        StopTurnTimer();
    }
}