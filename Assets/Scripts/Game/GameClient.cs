using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEditor.PackageManager;
using UnityEditor.Playables;
using UnityEngine;
using UnityEngine.Assertions;

public class GameClient : MonoBehaviour
{
    public static GameClient Instance;

    [Header("References")]
    [SerializeField] public GameObject TurnTokenPrefab;
    [SerializeField] public GameObject OtherPlayerPrefab;
    [SerializeField] public GameObject bagObject;
    [SerializeField] public GameBoard ownGameBoard;
    [SerializeField] public GameBoard opponentGameBoard;

    public ulong OwnClientID { get; private set; }
    public bool IsConnected { get; private set; }
    [HideInInspector] public GameObject OtherPlayerObject;
    [HideInInspector] public bool IsPlayer1;

    public void OnNetworkConnect()
    {
        IsConnected = true;

        // Initialize variables
        OwnClientID = NetworkManager.Singleton.LocalClientId;
        phaseStates = new Dictionary<GamePhase, ClientPhaseState>
        {
            { GamePhase.CONNECTING, new ConnectingPhaseState(this) },
            { GamePhase.SETUP, new SetupPhaseState(this) }
        };

        // Start in connecting phase
        gamePhase = null;
        currentPhaseState = phaseStates[GamePhase.CONNECTING];
        currentPhaseState.Enter(null);

        // Tell the game server we are a new client
        // We only want to do this once when the client is spawned
        // The connecting state doesn't handle this connection
        // If GameClient (this class) exists then we are connected so can just connect
        GameManager.Instance.ConnectToGameServerRpc();
    }

    public void OnNetworkDisconnect()
    {
        IsConnected = false;

        // Exit out of the current phase
        currentPhaseState.Exit(null);
    }

    public void OnGameStarted(ClientSetupPhaseData data)
    {
        ((SetupPhaseState)phaseStates[GamePhase.SETUP]).SetData(data);
        TransitionToPhase(GamePhase.SETUP);
    }

    public void OnGameReset()
    {
        TransitionToPhase(GamePhase.CONNECTING);
    }

    public void TransitionToPhase(GamePhase gamePhase)
    {
        currentPhaseState?.Exit(gamePhase);
        currentPhaseState = phaseStates[gamePhase];
        currentPhaseState?.Enter(this.gamePhase);
        this.gamePhase = gamePhase;
    }

    private GamePhase? gamePhase;
    private ClientPhaseState currentPhaseState;
    private Dictionary<GamePhase, ClientPhaseState> phaseStates;

    private void Awake()
    {
        Assert.IsNull(Instance);
        Instance = this;
    }

    private void OnDestroy()
    {
        currentPhaseState?.Exit(null);
    }

    private void OnApplicationQuit()
    {
        currentPhaseState?.Exit(null);
    }
}

public abstract class ClientPhaseState
{
    public ClientPhaseState(GameClient gameClient)
    {
        this.gameClient = gameClient;
    }

    public virtual void Enter(GamePhase? previousPhase)
    {
    }

    public virtual void Exit(GamePhase? nextPhase)
    {
    }

    public virtual void Update()
    { }

    protected GameClient gameClient;
}

public class ConnectingPhaseState : ClientPhaseState
{
    public ConnectingPhaseState(GameClient gameClient) : base(gameClient) { }

    public override void Enter(GamePhase? previousPhase)
    {
    }

    public override void Exit(GamePhase? nextPhase)
    {
    }
}

public class SetupPhaseState : ClientPhaseState
{
    public SetupPhaseState(GameClient gameClient) : base(gameClient) { }

    public override void Enter(GamePhase? previousPhase)
    {
        Assert.IsNotNull(data);

        // Start the main logic and listen to cancellation
        mainLogicCTS = new CancellationTokenSource();
        Task mainLogicTask = StartMainLogic(mainLogicCTS.Token);
    }

    public override void Exit(GamePhase? nextPhase)
    {
        // Cancel the main logic
        mainLogicCTS?.Cancel();
        mainLogicCTS?.Dispose();
        mainLogicCTS = null;

        // Destroy all game objects
        GameObject.Destroy(gameClient.OtherPlayerObject);
        gameClient.OtherPlayerObject = null;

        if (turnToken != null)
        {
            GameObject.Destroy(turnToken.gameObject);
            turnToken = null;
        }

        if (displayTokens != null)
        {
            for (int i = 0; i < displayTokens.Count; i++)
            {
                displayTokens[i].InstantDestroy();
            }
            displayTokens = null;
        }

        gameClient.ownGameBoard.HardReset();
        gameClient.opponentGameBoard.HardReset();

        data = null;
    }

    public void SetData(ClientSetupPhaseData data)
    {
        this.data = data;
    }

    private ClientSetupPhaseData data;
    private TurnToken turnToken;
    private List<DisplayToken> displayTokens;
    private CancellationTokenSource mainLogicCTS;

    private async Task StartMainLogic(CancellationToken ctoken)
    {
        gameClient.IsPlayer1 = gameClient.OwnClientID == data.firstPlayerClientID;

        // Spawn in other player
        gameClient.OtherPlayerObject = GameObject.Instantiate(gameClient.OtherPlayerPrefab);
        gameClient.OtherPlayerObject.transform.position = new(0.0f, -2.0f, 30.0f);

        // Spawn in turn token and flip to show first player
        Vector3 startPos = new(0.0f, 0.15f, 0.0f);
        GameObject turnTokenObject = GameObject.Instantiate(gameClient.TurnTokenPrefab, startPos, Quaternion.identity);
        turnToken = turnTokenObject.GetComponent<TurnToken>();
        ParticleManager.Instance.SpawnPoof(startPos);
        await Task.Delay(850, ctoken);
        await turnToken.DoFlipAnimation(ctoken, gameClient.IsPlayer1, 1.0f, 4.0f, 2);
        await Task.Delay(300, ctoken);

        // Raise the turn token upwards, change to indicator, then move to position
        Vector3 upPos = startPos + Vector3.up * 2.5f;
        _ = AnimationUtility.AnimatePosToPosWithEasing(ctoken, turnToken.transform, startPos, upPos, 0.6f, Easing.EaseOutBack);
        await Task.Delay(100, ctoken);
        await turnToken.DoChangeAnimation(ctoken, 0.4f);
        await Task.Delay(50, ctoken);
        Vector3 turnTokenPos = gameClient.IsPlayer1 ? gameClient.ownGameBoard.GetTurnTokenPosition() : gameClient.opponentGameBoard.GetTurnTokenPosition();
        await AnimationUtility.AnimatePosToPosWithEasing(ctoken, turnToken.transform, upPos, turnTokenPos, 0.95f, Easing.EaseInOutCubic);
        await Task.Delay(550, ctoken);

        // Display initial game tokens and animate them onto the table
        displayTokens = new List<DisplayToken>();
        int boardTokenGridWidth = 6;
        int boardTokenGridHeight = 4;
        float boardTokenSpacing = 2.5f;
        float boardOffsetX = -boardTokenSpacing * (boardTokenGridWidth - 1) / 2;
        float boardOffsetY = boardTokenSpacing * (boardTokenGridHeight - 1) / 2;

        List<Task> tokenAnimations = new List<Task>();
        for (int i = 0; i < data.initialGameTokenInstances.Length; i++)
        {
            int x = i % boardTokenGridWidth;
            int y = i / boardTokenGridWidth;
            Vector3 targetPos = new Vector3(x * boardTokenSpacing + boardOffsetX, 0.15f, -y * boardTokenSpacing + boardOffsetY);
            DisplayToken displayToken = TokenManager.Instance.CreateDisplayToken(data.initialGameTokenInstances[i], gameClient.bagObject.transform.position);
            displayTokens.Add(displayToken);

            tokenAnimations.Add(AnimationUtility.DelayTask(ctoken, 60 * i,
                () => AnimationUtility.AnimatePosToPosWithLift(ctoken, displayToken.transform, gameClient.bagObject.transform.position, targetPos, 6.0f, 0.7f, 0.2f, 1.0f)
            ));
        }
        await Task.WhenAll(tokenAnimations);
        await Task.Delay(2500, ctoken);

        // Animate them all back into the bag
        tokenAnimations = new List<Task>();
        for (int i = 0; i < displayTokens.Count; i++)
        {
            int x = i % boardTokenGridWidth;
            int y = i / boardTokenGridWidth;
            int delay = (int)(60 * (boardTokenGridWidth - x) + Mathf.Abs(1.5f - y) * i);
            DisplayToken token = displayTokens[i];
            tokenAnimations.Add(AnimationUtility.DelayTask(ctoken, delay,
                () => AnimationUtility.AnimatePosToPosWithLift(ctoken, token.transform, token.transform.position, gameClient.bagObject.transform.position, 6.0f, 0.7f, 0.0f, 0.8f)
            ));
        }
        await Task.WhenAll(tokenAnimations);
        await Task.Delay(300, ctoken);

        // Delete them all once theyre all back in the bag
        for (int i = 0; i < displayTokens.Count; i++)
        {
            displayTokens[i].InstantDestroy();
        }

        displayTokens.Clear();

        // Make player draw the 6 tokens then let them discard down to 4
        await Task.WhenAll(
            gameClient.ownGameBoard.DrawTokens(ctoken, gameClient.IsPlayer1 ? data.player1DraftTokenInstances : data.player2DraftTokenInstances),
            gameClient.opponentGameBoard.DrawTokens(ctoken, gameClient.IsPlayer1 ? data.player2DraftTokenInstances : data.player1DraftTokenInstances)
        );

        // Start the game
        Debug.Log("Start");
    }
}
