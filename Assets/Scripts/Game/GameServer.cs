using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Assertions;

public class GameServer : MonoBehaviour
{
    public static GameServer Instance;

    public ulong Player1ClientID { get; private set; }
    public ulong Player2ClientID { get; private set; }

    public void Init()
    {
        NetworkManager.Singleton.OnServerStarted += () => { OnNetworkStart(); };
        NetworkManager.Singleton.StartServer();
    }

    public void OnNetworkStart()
    {
        Debug.Log("Server started");
        playersConnected = 0;
        gamePhase = GamePhase.CONNECTING;
    }

    public void OnGameClientConnect(ulong clientID)
    {
        Debug.Log("Client connected: " + clientID);
        Assert.AreEqual(GamePhase.CONNECTING, gamePhase);

        if (playersConnected == 0) Player1ClientID = clientID;
        else if (playersConnected == 1) Player2ClientID = clientID;

        playersConnected++;

        if (playersConnected == 2)
        {
            StartSetupPhase();
        }
    }

    public void OnGameClientDisconnect(ulong clientID)
    {
        // Rotate player IDs if necessary
        if (Player1ClientID == clientID)
        {
            Player1ClientID = Player2ClientID;
            Player2ClientID = 0;
        }
        else if (Player2ClientID == clientID)
        {
            Player2ClientID = 0;
        }

        playersConnected--;

        ResetToConnectingPhase();
    }

    public void StartSetupPhase()
    {
        Assert.AreEqual(GamePhase.CONNECTING, gamePhase);
        gamePhase = GamePhase.SETUP;

        // Initialize game state
        currentRound = 1;
        ulong firstTurnPlayer = (ulong)Random.Range(0, 2);
        firstTurnClientID = firstTurnPlayer == 0 ? Player1ClientID : Player2ClientID;
        currentGameTokenInstances = TokenManager.Instance.GetTokenSelection(24);
        TokenInstance[] initialGameTokenInstances = currentGameTokenInstances.ToArray();

        // Take from top of available tokens for each players draft
        List<TokenInstance> player1DraftTokenIDs = new List<TokenInstance>();
        List<TokenInstance> player2DraftTokenIDs = new List<TokenInstance>();
        for (int i = 0; i < 6; i++)
        {
            player1DraftTokenIDs.Add(currentGameTokenInstances[0]);
            currentGameTokenInstances.RemoveAt(0);
            player2DraftTokenIDs.Add(currentGameTokenInstances[0]);
            currentGameTokenInstances.RemoveAt(0);
        }

        // Send setup game state to clients
        ClientSetupPhaseData clientData = new ClientSetupPhaseData();
        clientData.firstTurnClientID = firstTurnClientID;
        clientData.player1ClientID = Player1ClientID;
        clientData.player2ClientID = Player2ClientID;
        clientData.initialGameTokenInstances = initialGameTokenInstances;
        clientData.player1DraftTokenInstances = player1DraftTokenIDs.ToArray();
        clientData.player2DraftTokenInstances = player2DraftTokenIDs.ToArray();
        GameCommunication.Instance.GameStartedClientRpc(clientData);
    }

    public void ResetToConnectingPhase()
    {
        gamePhase = GamePhase.CONNECTING;
        GameCommunication.Instance.GameResetClientRpc();
    }

    public ulong OtherPlayer(ulong clientID)
    {
        if (clientID == Player1ClientID) return Player2ClientID;
        if (clientID == Player2ClientID) return Player1ClientID;
        return 0;
    }

    private GamePhase gamePhase;
    private int playersConnected = 0;
    private ulong firstTurnClientID;
    private List<TokenInstance> currentGameTokenInstances;
    private int currentRound;

    private void Awake()
    {
        Assert.IsNull(Instance);
        Instance = this;
    }
}
