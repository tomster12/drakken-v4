using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class GameServer : MonoBehaviour
{
    public static GameServer Instance;

    public void OnNetworkStart()
    {
        playersConnected = 0;
        gamePhase = GamePhase.CONNECTING;
    }

    public void OnClientConnect(ulong clientID)
    {
        Assert.AreEqual(GamePhase.CONNECTING, gamePhase);

        if (playersConnected == 0) player1ClientID = clientID;
        else if (playersConnected == 1) player2ClientID = clientID;

        playersConnected++;

        if (playersConnected == 2)
        {
            StartSetupPhase();
        }
    }

    public void OnClientDisconnect(ulong clientID)
    {
        // Rotate player IDs if necessary
        if (player1ClientID == clientID)
        {
            player1ClientID = player2ClientID;
            player2ClientID = 0;
        }
        else if (player2ClientID == clientID)
        {
            player2ClientID = 0;
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
        firstTurnClientID = firstTurnPlayer == 0 ? player1ClientID : player2ClientID;
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
        clientData.player1ClientID = player1ClientID;
        clientData.player2ClientID = player2ClientID;
        clientData.initialGameTokenInstances = initialGameTokenInstances;
        clientData.player1DraftTokenInstances = player1DraftTokenIDs.ToArray();
        clientData.player2DraftTokenInstances = player2DraftTokenIDs.ToArray();
        GameManager.Instance.GameStartedClientRpc(clientData);
    }

    public void ResetToConnectingPhase()
    {
        gamePhase = GamePhase.CONNECTING;
        GameManager.Instance.GameResetClientRpc();
    }

    private GamePhase gamePhase;
    private int playersConnected = 0;
    private ulong firstTurnClientID;
    private ulong player1ClientID;
    private ulong player2ClientID;
    private List<TokenInstance> currentGameTokenInstances;
    private int currentRound;

    private void Awake()
    {
        Assert.IsNull(Instance);
        Instance = this;
    }
}
