using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.Assertions;

// Singleton
public partial class GameClient : MonoBehaviour
{
    public static GameClient Instance;

    private void Awake()
    {
        Assert.IsNull(Instance);
        Instance = this;
    }
}

public partial class GameClient : MonoBehaviour
{
    [Header("References")]
    [SerializeField] public UnityTransport transport;
    [SerializeField] public GameObject TurnTokenPrefab;
    [SerializeField] public GameObject opPlayerObjectPrefab;
    [SerializeField] public GameObject bagObject;
    [SerializeField] public GameBoard myBoard;
    [SerializeField] public GameBoard opBoard;

    [Header("Config")]
    [SerializeField] public string serverAddress = "0.0.0.0";
    [SerializeField] public ushort serverPort = 7777;

    public ulong MyClientID { get; private set; }
    public ulong OpClientID { get; private set; }
    public bool IsPlayer1 { get; private set; }
    public bool IsFirstTurn { get; private set; }
    public GameObject OpPlayerObject { get; set; }
    public TurnToken TurnToken { get; set; }

    public void Init()
    {
        Assert.IsFalse(isInitialized);
        isInitialized = true;

        // Initialize variables
        MyClientID = 0;
        OpClientID = 0;
        IsPlayer1 = false;
        IsFirstTurn = false;
        OpPlayerObject = null;
        currentPhase = null;
        currentState = null;

        // Setup phases
        states = new Dictionary<GamePhase, ClientState.Base>
        {
            { GamePhase.CONNECTING, new ClientState.Connecting(this) },
            { GamePhase.SETUP, new ClientState.Setup(this) },
            { GamePhase.PLAY, new ClientState.Play(this) }
        };

        // Start in connecting phase
        TransitionToPhase(GamePhase.CONNECTING);

        // On connect, try and join game
        NetworkManager.Singleton.OnClientConnectedCallback += (ulong clientID) =>
        {
            GameCommunication.Instance.JoinGameServerRpc();
        };

        // On disconnect, go back to connecting phase
        NetworkManager.Singleton.OnClientDisconnectCallback += (ulong clientID) =>
        {
            TransitionToPhase(GamePhase.CONNECTING);
        };

        // Setup networking and try connect to server
        transport.ConnectionData.Address = serverAddress;
        transport.ConnectionData.Port = serverPort;
        NetworkManager.Singleton.StartClient();
    }

    public void TransitionToPhase(GamePhase? phase)
    {
        currentState?.Exit(phase);

        if (phase != null)
        {
            currentState = states[(GamePhase)phase];
            currentState?.Enter(currentPhase);
        }

        currentPhase = phase;
    }

    private bool isInitialized;
    private GamePhase? currentPhase;
    private ClientState.Base currentState;
    private Dictionary<GamePhase, ClientState.Base> states;

    private void OnDestroy()
    {
        currentState?.Exit(null);
    }

    private void OnApplicationQuit()
    {
        currentState?.Exit(null);
    }
}

// Game communication functions
public partial class GameClient : MonoBehaviour
{
    public void OnStartSetupPhase(SetupPhaseStartData startData)
    {
        Assert.AreEqual(GamePhase.CONNECTING, currentPhase);

        MyClientID = NetworkManager.Singleton.LocalClientId;
        IsPlayer1 = MyClientID == startData.Player1ClientID;
        OpClientID = IsPlayer1 ? startData.Player2ClientID : startData.Player1ClientID;
        IsFirstTurn = MyClientID == startData.FirstTurnClientID;

        ((ClientState.Setup)states[GamePhase.SETUP]).SetStartData(startData);
        TransitionToPhase(GamePhase.SETUP);
    }

    public void OnStartPlayPhase(PlayPhaseStartData data)
    {
        Assert.AreEqual(GamePhase.SETUP, currentPhase);

        ((ClientState.Play)states[GamePhase.PLAY]).SetStartData(data);
        TransitionToPhase(GamePhase.PLAY);
    }

    public void OnResetGame()
    {
        TransitionToPhase(GamePhase.CONNECTING);
    }
}

namespace ClientState
{
    internal abstract class Base
    {
        public Base(GameClient gameClient)
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

    internal class Connecting : Base
    {
        public Connecting(GameClient gameClient) : base(gameClient) { }
    }

    internal class Setup : Base
    {
        public Setup(GameClient gameClient) : base(gameClient) { }

        public override void Enter(GamePhase? previousPhase)
        {
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
            Assert.IsTrue(nextPhase == GamePhase.CONNECTING || nextPhase == GamePhase.PLAY);

            // Cancel the main logic
            mainLogicCTS?.Cancel();
            mainLogicCTS?.Dispose();
            mainLogicCTS = null;

            // If cleaning up to CONNECTING then destroy object
            if (nextPhase == GamePhase.CONNECTING)
            {
                GameObject.Destroy(gameClient.OpPlayerObject);
                gameClient.OpPlayerObject = null;

                if (gameClient.TurnToken != null)
                {
                    GameObject.Destroy(gameClient.TurnToken.gameObject);
                    gameClient.TurnToken = null;
                }

                if (displayTokens != null)
                {
                    for (int i = 0; i < displayTokens.Count; i++)
                    {
                        displayTokens[i].Destroy();
                    }
                    displayTokens = null;
                }

                gameClient.myBoard.ResetBoard();
                gameClient.opBoard.ResetBoard();
            }

            startData = null;
        }

        public void SetStartData(SetupPhaseStartData startData)
        {
            this.startData = startData;
        }

        private SetupPhaseStartData startData;
        private List<DisplayToken> displayTokens;
        private CancellationTokenSource mainLogicCTS;

        private async Task StartMainLogic(CancellationToken ctoken)
        {
            // Initialize game boards
            gameClient.myBoard.Init(gameClient, true);
            gameClient.opBoard.Init(gameClient, false);

            // Spawn in other player object
            gameClient.OpPlayerObject = GameObject.Instantiate(gameClient.opPlayerObjectPrefab);
            gameClient.OpPlayerObject.transform.position = new(0.0f, -2.0f, 30.0f);

            // Spawn in turn token, flip to show first player, go to position
            Vector3 startPos = new(0.0f, 0.15f, 0.0f);
            GameObject turnTokenObject = GameObject.Instantiate(gameClient.TurnTokenPrefab, startPos, Quaternion.identity);
            gameClient.TurnToken = turnTokenObject.GetComponent<TurnToken>();
            GameBoard turnTokenBoard = gameClient.IsFirstTurn ? gameClient.myBoard : gameClient.opBoard;

            ParticleManager.Instance.SpawnPoof(startPos);
            await Task.Delay(850, ctoken);
            await gameClient.TurnToken.DoFlipAnimation(ctoken, gameClient.IsFirstTurn, 1.0f, 4.0f, 2);
            await Task.Delay(300, ctoken);
            await gameClient.TurnToken.DoChangeAndPlaceAnimation(ctoken, 2.0f, turnTokenBoard);

            // Display initial game tokens and animate them onto the table
            displayTokens = new List<DisplayToken>();
            int boardTokenGridWidth = 6;
            int boardTokenGridHeight = 4;
            float boardTokenSpacing = 2.5f;
            float boardOffsetX = -boardTokenSpacing * (boardTokenGridWidth - 1) / 2;
            float boardOffsetY = boardTokenSpacing * (boardTokenGridHeight - 1) / 2;

            List<Task> tokenAnimations = new List<Task>();
            for (int i = 0; i < startData.AllTokens.Length; i++)
            {
                int x = i % boardTokenGridWidth;
                int y = i / boardTokenGridWidth;
                Vector3 targetPos = new Vector3(x * boardTokenSpacing + boardOffsetX, 0.15f, -y * boardTokenSpacing + boardOffsetY);
                DisplayToken displayToken = TokenManager.Instance.CreateDisplayToken(startData.AllTokens[i], gameClient.bagObject.transform.position);
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
                int delay = 60 * (int)((boardTokenGridWidth - x) + Mathf.Abs(1.5f - y));
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
                displayTokens[i].Destroy();
            }

            displayTokens.Clear();

            // Make player draw the 6 tokens
            await Task.WhenAll(
                gameClient.myBoard.DrawTokens(ctoken, gameClient.IsPlayer1 ? startData.Player1BoardTokens : startData.Player2BoardTokens),
                gameClient.opBoard.DrawTokens(ctoken, gameClient.IsPlayer1 ? startData.Player2BoardTokens : startData.Player1BoardTokens)
            );

            // Listen and wait until discarded down to 4
            List<TokenInstance> discardedTokens = new();
            TaskCompletionSource<bool> discardTask = new();
            void OnBoardTokenDiscarded(DisplayToken token, int index)
            {
                discardedTokens.Add(token.TokenInstance);

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

            // Send the discarded tokens to the server to finish up the setup phase
            SetupPhaseEndData endData = new()
            {
                DiscardedTokens = discardedTokens.ToArray()
            };
            GameCommunication.Instance.EndSetupPhaseServerRpc(endData);
        }
    }

    internal class Play : Base
    {
        public Play(GameClient gameClient) : base(gameClient) { }

        public override async void Enter(GamePhase? previousPhase)
        {
            foreach (var dice in startData.Player1Dice)
            {
                Debug.Log(dice.Value);
            }

            foreach (var dice in startData.Player2Dice)
            {
                Debug.Log(dice.Value);
            }

            await Task.WhenAll(
                gameClient.myBoard.RollDice(gameClient.IsPlayer1 ? startData.Player1Dice : startData.Player2Dice),
                gameClient.opBoard.RollDice(gameClient.IsPlayer1 ? startData.Player2Dice : startData.Player1Dice)
            );
        }

        public override void Exit(GamePhase? nextPhase)
        {
        }

        public void SetStartData(PlayPhaseStartData startData)
        {
            this.startData = startData;
        }

        private PlayPhaseStartData startData;
    }
}
