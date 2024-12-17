using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class GameServer : MonoBehaviour
{
    public static GameServer Instance;

    public void OnSpawn()
    {
        playersConnected = 0;
        clientIDs = new List<ulong>();
        gamePhase = GamePhase.CONNECTING;
    }

    public void OnClientConnect(ulong clientID)
    {
        Assert.AreEqual(GamePhase.CONNECTING, gamePhase);

        clientIDs.Add(clientID);
        playersConnected++;

        if (playersConnected == 2)
        {
            StartSetupPhase();
        }
    }

    public void OnClientDisconnect(ulong clientID)
    {
        clientIDs.Remove(clientID);
        playersConnected--;
        ResetGamePhase();
    }

    public void StartSetupPhase()
    {
        Assert.AreEqual(GamePhase.CONNECTING, gamePhase);
        gamePhase = GamePhase.SETUP;

        // Initialize game state
        currentRound = 1;
        firstPlayerClientID = clientIDs[Random.Range(0, 2)];
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
        clientData.firstPlayerClientID = firstPlayerClientID;
        clientData.initialGameTokenInstances = initialGameTokenInstances;
        clientData.player1DraftTokenInstances = player1DraftTokenIDs.ToArray();
        clientData.player2DraftTokenInstances = player2DraftTokenIDs.ToArray();
        GameManager.Instance.GameStartedClientRpc(clientData);
    }

    public void ResetGamePhase()
    {
        gamePhase = GamePhase.CONNECTING;
        GameManager.Instance.GameResetClientRpc();
    }

    private int playersConnected = 0;
    private List<ulong> clientIDs = new List<ulong>();
    private GamePhase gamePhase;
    private int currentRound;
    private ulong firstPlayerClientID;
    private List<TokenInstance> currentGameTokenInstances;

    private void Awake()
    {
        Assert.IsNull(Instance);
        Instance = this;
    }
}
