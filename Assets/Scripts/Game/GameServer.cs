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

    public int PlayersConnected { get; set; }
    public ulong Player1ClientID { get; private set; }
    public ulong Player2ClientID { get; private set; }
    public ulong FirstTurnClientID { get; set; }
    public List<TokenInstance> CurrentGameTokenInstances { get; set; }

    public void Init()
    {
        Assert.IsFalse(isInitialized);
        isInitialized = true;

        // Initialize variables
        PlayersConnected = 0;
        Player1ClientID = 0;
        Player2ClientID = 0;
        FirstTurnClientID = 0;
        CurrentGameTokenInstances = null;
        currentPhase = null;
        currentState = null;

        // Setup phases
        states = new Dictionary<GamePhase, ServerState.Base>
        {
            { GamePhase.CONNECTING, new ServerState.Connecting(this) },
            { GamePhase.SETUP, new ServerState.Setup(this) }
        };

        // Start in connecting phase
        TransitionToPhase(GamePhase.CONNECTING);

        // Setup networking and start server
        transport.ConnectionData.Address = "127.0.0.1";
        transport.ConnectionData.Port = 25565;
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
            // Initialize game state
            ulong firstTurnPlayer = (ulong)Random.Range(0, 2);
            gameServer.FirstTurnClientID = firstTurnPlayer == 0 ? gameServer.Player1ClientID : gameServer.Player2ClientID;
            gameServer.CurrentGameTokenInstances = TokenManager.Instance.GetTokenSelection(24);

            // Cache initial tokens in the game to send to client
            TokenInstance[] initialGameTokenInstances = gameServer.CurrentGameTokenInstances.ToArray();

            // Take from top of available tokens for each players draft
            List<TokenInstance> player1DraftTokenIDs = new List<TokenInstance>();
            List<TokenInstance> player2DraftTokenIDs = new List<TokenInstance>();
            for (int i = 0; i < 6; i++)
            {
                player1DraftTokenIDs.Add(gameServer.CurrentGameTokenInstances[0]);
                gameServer.CurrentGameTokenInstances.RemoveAt(0);
                player2DraftTokenIDs.Add(gameServer.CurrentGameTokenInstances[0]);
                gameServer.CurrentGameTokenInstances.RemoveAt(0);
            }

            // Send setup game state to clients
            SetupPhaseStartData clientData = new SetupPhaseStartData();
            clientData.firstTurnClientID = gameServer.FirstTurnClientID;
            clientData.player1ClientID = gameServer.Player1ClientID;
            clientData.player2ClientID = gameServer.Player2ClientID;
            clientData.initialGameTokenInstances = initialGameTokenInstances;
            clientData.player1DraftTokenInstances = player1DraftTokenIDs.ToArray();
            clientData.player2DraftTokenInstances = player2DraftTokenIDs.ToArray();
            GameCommunication.Instance.StartSetupPhaseClientRpc(clientData);
        }

        public void OnClientEndSetupPhase(ulong clientID, SetupPhaseEndData data)
        {
            Debug.Log("Client setup phase end: " + clientID);
            foreach (TokenInstance token in data.discardedTokenInstances)
            {
                Debug.Log("Discarded token: " + token.tokenID);
            }
        }
    }
}
