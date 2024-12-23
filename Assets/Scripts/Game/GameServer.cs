using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.Assertions;

// Singleton
public partial class GameServer : MonoBehaviour
{
    public static GameServer Instance;

    private void Awake()
    {
        Assert.IsNull(Instance);
        Instance = this;
    }
}

public partial class GameServer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] public UnityTransport transport;

    [Header("Config")]
    [SerializeField] public string clientAddress = "0.0.0.0";
    [SerializeField] public ushort clientPort = 7777;

    public int PlayersConnected { get; set; }
    public ulong Player1ClientID { get; private set; }
    public ulong Player2ClientID { get; private set; }
    public ulong CurrentTurnClientID { get; set; }
    public List<TokenInstance> CurrentBagTokens { get; set; }
    public List<TokenInstance> Player1BoardTokens { get; set; }
    public List<TokenInstance> Player2BoardTokens { get; set; }
    public List<DiceData> Player1Dice { get; set; }
    public List<DiceData> Player2Dice { get; set; }

    public void Init()
    {
        Assert.IsFalse(isInitialized);
        isInitialized = true;

        // Initialize variables
        PlayersConnected = 0;
        Player1ClientID = 0;
        Player2ClientID = 0;
        CurrentTurnClientID = 0;
        CurrentBagTokens = null;
        currentPhase = null;
        currentState = null;

        // Setup phases
        states = new Dictionary<GamePhase, ServerState.Base>
        {
            { GamePhase.CONNECTING, new ServerState.Connecting(this) },
            { GamePhase.SETUP, new ServerState.Setup(this) },
            { GamePhase.PLAY, new ServerState.Play(this) }
        };

        // Start in connecting phase
        TransitionToPhase(GamePhase.CONNECTING);

        // Setup networking and start server
        transport.ConnectionData.Address = clientAddress;
        transport.ConnectionData.Port = clientPort;
        NetworkManager.Singleton.StartServer();
    }

    public ulong OtherPlayer(ulong clientID)
    {
        if (clientID == Player1ClientID) return Player2ClientID;
        if (clientID == Player2ClientID) return Player1ClientID;
        return 0;
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
    private ServerState.Base currentState;
    private Dictionary<GamePhase, ServerState.Base> states;

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
public partial class GameServer : MonoBehaviour
{
    public void OnClientJoin(ulong clientID)
    {
        Assert.AreEqual(GamePhase.CONNECTING, currentPhase);

        // Update player IDs
        if (PlayersConnected == 0) Player1ClientID = clientID;
        else if (PlayersConnected == 1) Player2ClientID = clientID;
        PlayersConnected++;

        // Once both players are connected start the game
        if (PlayersConnected == 2)
        {
            TransitionToPhase(GamePhase.SETUP);
        }
    }

    public void OnClientExit(ulong clientID)
    {
        // Rotate player IDs
        if (Player1ClientID == clientID)
        {
            Player1ClientID = Player2ClientID;
            Player2ClientID = 0;
        }
        else if (Player2ClientID == clientID)
        {
            Player2ClientID = 0;
        }
        PlayersConnected--;

        // Reset game back to connecting
        TransitionToPhase(GamePhase.CONNECTING);
        GameCommunication.Instance.ResetGameClientRpc();
    }

    public void OnClientEndSetupPhase(ulong clientID, SetupPhaseEndData data)
    {
        Assert.AreEqual(GamePhase.SETUP, currentPhase);

        ((ServerState.Setup)currentState).OnClientEndSetupPhase(clientID, data);
    }
}

namespace ServerState
{
    public abstract class Base
    {
        public Base(GameServer gameServer)
        {
            this.gameServer = gameServer;
        }

        public virtual void Enter(GamePhase? previousPhase)
        {
        }

        public virtual void Exit(GamePhase? nextPhase)
        {
        }

        public virtual void Update()
        { }

        protected GameServer gameServer;
    }

    internal class Connecting : Base
    {
        public Connecting(GameServer gameServer) : base(gameServer) { }
    }

    internal class Setup : Base
    {
        public Setup(GameServer gameServer) : base(gameServer) { }

        public override void Enter(GamePhase? previousPhase)
        {
            finishedPlayerCount = 0;

            // Select first player
            ulong firstTurnPlayer = (ulong)Random.Range(0, 2);
            gameServer.CurrentTurnClientID = firstTurnPlayer == 0 ? gameServer.Player1ClientID : gameServer.Player2ClientID;

            // Draw the bag tokens (and cache for later)
            gameServer.CurrentBagTokens = TokenManager.Instance.GetTokenSelection(24);
            TokenInstance[] allTokens = gameServer.CurrentBagTokens.ToArray();

            // Take from top of bag tokens for each players draft
            gameServer.Player1BoardTokens = new List<TokenInstance>();
            gameServer.Player2BoardTokens = new List<TokenInstance>();
            for (int i = 0; i < 6; i++)
            {
                gameServer.Player1BoardTokens.Add(gameServer.CurrentBagTokens[0]);
                gameServer.CurrentBagTokens.RemoveAt(0);
                gameServer.Player2BoardTokens.Add(gameServer.CurrentBagTokens[0]);
                gameServer.CurrentBagTokens.RemoveAt(0);
            }

            // Send setup game state to clients
            SetupPhaseStartData clientData = new SetupPhaseStartData();
            clientData.FirstTurnClientID = gameServer.CurrentTurnClientID;
            clientData.Player1ClientID = gameServer.Player1ClientID;
            clientData.Player2ClientID = gameServer.Player2ClientID;
            clientData.AllTokens = allTokens;
            clientData.Player1BoardTokens = gameServer.Player1BoardTokens.ToArray();
            clientData.Player2BoardTokens = gameServer.Player2BoardTokens.ToArray();
            GameCommunication.Instance.StartSetupPhaseClientRpc(clientData);
        }

        public void OnClientEndSetupPhase(ulong clientID, SetupPhaseEndData data)
        {
            // Update players board tokens
            foreach (TokenInstance token in data.DiscardedTokens)
            {
                if (clientID == gameServer.Player1ClientID) gameServer.Player1BoardTokens.Remove(token);
                else gameServer.Player2BoardTokens.Remove(token);
            }

            // Update current state
            finishedPlayerCount++;
            if (finishedPlayerCount == 2)
            {
                gameServer.TransitionToPhase(GamePhase.PLAY);
            }
        }

        private int finishedPlayerCount = 0;
    }

    internal class Play : Base
    {
        public Play(GameServer gameServer) : base(gameServer) { }

        public override void Enter(GamePhase? previousPhase)
        {
            // Roll dice for each player
            gameServer.Player1Dice = new List<DiceData>();
            gameServer.Player2Dice = new List<DiceData>();

            for (int i = 0; i < 5; i++)
            {
                DiceData dice1 = new();
                dice1.Roll();
                gameServer.Player1Dice.Add(dice1);

                DiceData dice2 = new();
                dice2.Roll();
                gameServer.Player2Dice.Add(dice2);
            }

            // Send starting data to client
            PlayPhaseStartData data = new PlayPhaseStartData();
            data.Player1Dice = gameServer.Player1Dice.ToArray();
            data.Player2Dice = gameServer.Player2Dice.ToArray();
            GameCommunication.Instance.StartPlayPhaseClientRpc(data);
        }
    }
}
