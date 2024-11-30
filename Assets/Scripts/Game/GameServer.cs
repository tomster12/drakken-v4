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
        currentGameTokenIDs = TokenManager.Instance.GetTokenSelection(24);
        string[] initialGameTokenIDs = currentGameTokenIDs.ToArray();

        // Take from top of available tokens for each players draft
        List<string> player1DraftTokenIDs = new List<string>();
        List<string> player2DraftTokenIDs = new List<string>();
        for (int i = 0; i < 6; i++)
        {
            player1DraftTokenIDs.Add(currentGameTokenIDs[0]);
            currentGameTokenIDs.RemoveAt(0);
            player2DraftTokenIDs.Add(currentGameTokenIDs[0]);
            currentGameTokenIDs.RemoveAt(0);
        }

        // Send setup game state to clients
        ClientSetupPhaseData clientData = new ClientSetupPhaseData();
        clientData.firstPlayerClientID = firstPlayerClientID;
        clientData.initialGameTokenIDs = initialGameTokenIDs;
        clientData.player1DraftTokenIDs = player1DraftTokenIDs.ToArray();
        clientData.player2DraftTokenIDs = player2DraftTokenIDs.ToArray();
        GameManager.Instance.GameStartClientRpc(clientData);
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
    private List<string> currentGameTokenIDs;

    private void Awake()
    {
        Assert.IsNull(Instance);
        Instance = this;
    }
}
