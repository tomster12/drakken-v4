using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Assertions;

public class GameClient : MonoBehaviour
{
    public static GameClient Instance;

    public bool IsConnected { get; private set; }
    public ulong MyClientID { get; private set; }
    public ulong OpClientID { get; private set; }
    public bool IsPlayer1 { get; private set; }
    public bool IsFirstTurn { get; private set; }
    public GameObject OpPlayerObject { get; set; }

    [Header("References")]
    [SerializeField] public GameObject TurnTokenPrefab;
    [SerializeField] public GameObject opPlayerObjectPrefab;
    [SerializeField] public GameObject bagObject;
    [SerializeField] public GameBoard myBoard;
    [SerializeField] public GameBoard opBoard;

    public void OnNetworkConnect()
    {
        IsConnected = true;

        phaseStates = new Dictionary<GamePhase, ClientPhaseState>
        {
            { GamePhase.CONNECTING, new ConnectingPhaseState(this) },
            { GamePhase.SETUP, new SetupPhaseState(this) }
        };

        TransitionToPhase(GamePhase.CONNECTING);

        // We only want to do this once when the client is spawned
        // If we have reached this point then we are connected, so just try connect to game
        // The connecting state runs while we wait
        GameManager.Instance.ConnectToGameServerRpc();
    }

    public void OnNetworkDisconnect()
    {
        IsConnected = false;

        // Exit out of the current phase
        currentGamePhaseState.Exit(null);
    }

    public void OnGameStarted(ClientSetupPhaseData data)
    {
        // Set the client IDs
        MyClientID = NetworkManager.Singleton.LocalClientId;
        IsPlayer1 = MyClientID == data.player1ClientID;
        OpClientID = IsPlayer1 ? data.player2ClientID : data.player1ClientID;
        IsFirstTurn = MyClientID == data.firstTurnClientID;

        // Transition into SETUP phase
        Assert.AreEqual(GamePhase.CONNECTING, currentGamePhase);
        ((SetupPhaseState)phaseStates[GamePhase.SETUP]).SetData(data);
        TransitionToPhase(GamePhase.SETUP);
    }

    public void OnGameReset()
    {
        TransitionToPhase(GamePhase.CONNECTING);
    }

    public void TransitionToPhase(GamePhase gamePhase)
    {
        currentGamePhaseState?.Exit(gamePhase);
        currentGamePhaseState = phaseStates[gamePhase];
        currentGamePhaseState?.Enter(this.currentGamePhase);
        this.currentGamePhase = gamePhase;
    }

    private GamePhase? currentGamePhase;
    private ClientPhaseState currentGamePhaseState;
    private Dictionary<GamePhase, ClientPhaseState> phaseStates;

    private void Awake()
    {
        Assert.IsNull(Instance);
        Instance = this;
    }

    private void OnDestroy()
    {
        currentGamePhaseState?.Exit(null);
    }

    private void OnApplicationQuit()
    {
        currentGamePhaseState?.Exit(null);
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

        // Start the main logic with a try catch
        mainLogicCTS = new CancellationTokenSource();
        StartMainLogic(mainLogicCTS.Token).ContinueWith(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError("Main logic faulted: " + task.Exception);
            }
        });
    }

    public override void Exit(GamePhase? nextPhase)
    {
        // Cancel the main logic
        mainLogicCTS?.Cancel();
        mainLogicCTS?.Dispose();
        mainLogicCTS = null;

        // Destroy all game objects
        GameObject.Destroy(gameClient.OpPlayerObject);
        gameClient.OpPlayerObject = null;

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

        gameClient.myBoard.ResetBoard();
        gameClient.opBoard.ResetBoard();

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
        // Initialize game boards
        gameClient.myBoard.Init(isLocalBoard: true);
        gameClient.opBoard.Init(isLocalBoard: false);

        // Spawn in other player object
        gameClient.OpPlayerObject = GameObject.Instantiate(gameClient.opPlayerObjectPrefab);
        gameClient.OpPlayerObject.transform.position = new(0.0f, -2.0f, 30.0f);

        // Spawn in turn token and flip to show first player
        Vector3 startPos = new(0.0f, 0.15f, 0.0f);
        GameObject turnTokenObject = GameObject.Instantiate(gameClient.TurnTokenPrefab, startPos, Quaternion.identity);
        turnToken = turnTokenObject.GetComponent<TurnToken>();
        ParticleManager.Instance.SpawnPoof(startPos);
        await Task.Delay(850, ctoken);
        await turnToken.DoFlipAnimation(ctoken, gameClient.IsFirstTurn, 1.0f, 4.0f, 2);
        await Task.Delay(300, ctoken);

        // Raise the turn token upwards, change to indicator, then move to position
        Vector3 upPos = startPos + Vector3.up * 2.5f;
        _ = AnimationUtility.AnimatePosToPosWithEasing(ctoken, turnToken.transform, startPos, upPos, 0.6f, Easing.EaseOutBack);
        await Task.Delay(100, ctoken);
        await turnToken.DoChangeAnimation(ctoken, 0.4f);
        await Task.Delay(50, ctoken);
        Vector3 turnTokenPos = gameClient.IsFirstTurn ? gameClient.myBoard.GetTurnTokenPosition() : gameClient.opBoard.GetTurnTokenPosition();
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

        // Make player draw the 6 tokens
        await Task.WhenAll(
            gameClient.myBoard.DrawTokens(ctoken, gameClient.IsPlayer1 ? data.player1DraftTokenInstances : data.player2DraftTokenInstances),
            gameClient.opBoard.DrawTokens(ctoken, gameClient.IsPlayer1 ? data.player2DraftTokenInstances : data.player1DraftTokenInstances)
        );

        // Listen and wait until discarded down to 4
        TaskCompletionSource<bool> discardTask = new();
        void OnBoardTokenDiscarded(int index)
        {
            if (gameClient.myBoard.Tokens.Count <= 4)
            {
                discardTask.SetResult(true);
            }
        }
        gameClient.myBoard.OnTokenDiscard += OnBoardTokenDiscarded;
        gameClient.myBoard.SetTokenMode(TokenInteractMode.DISCARDING);
        await discardTask.Task;
        gameClient.myBoard.SetTokenMode(TokenInteractMode.NONE);
        gameClient.myBoard.OnTokenDiscard -= OnBoardTokenDiscarded;
    }
}
