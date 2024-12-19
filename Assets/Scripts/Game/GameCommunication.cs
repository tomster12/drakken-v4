using Unity.Netcode;
using UnityEngine;
using UnityEngine.Assertions;

// Singleton
public partial class GameCommunication : NetworkBehaviour
{
    public static GameCommunication Instance;

    private void Awake()
    {
        Assert.IsNull(Instance);
        Instance = this;
    }
}

// Main game communication logic with client <-> client
public partial class GameCommunication : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private GameClient client;
    [SerializeField] private GameServer server;

    [ServerRpc(RequireOwnership = false)]
    public void JoinGameServerRpc(ServerRpcParams serverRpcParams = default)
    {
        ulong clientID = serverRpcParams.Receive.SenderClientId;
        GameServer.Instance.OnClientJoin(clientID);
    }

    [ServerRpc(RequireOwnership = false)]
    public void ExitGameServerRpc(ServerRpcParams serverRpcParams = default)
    {
        ulong clientID = serverRpcParams.Receive.SenderClientId;
        GameServer.Instance.OnClientExit(clientID);
    }

    [ClientRpc]
    public void StartSetupPhaseClientRpc(SetupPhaseStartData startData)
    {
        GameClient.Instance.OnStartSetupPhase(startData);
    }

    [ServerRpc(RequireOwnership = false)]
    public void EndSetupPhaseServerRpc(SetupPhaseEndData endData, ServerRpcParams serverRpcParams = default)
    {
        ulong clientID = serverRpcParams.Receive.SenderClientId;
        GameServer.Instance.OnClientEndSetupPhase(clientID, endData);
    }

    [ClientRpc]
    public void StartPlayPhaseClientRpc(PlayPhaseStartData startData)
    {
        GameClient.Instance.OnStartPlayPhase(startData);
    }

    [ClientRpc]
    public void ResetGameClientRpc()
    {
        GameClient.Instance.OnResetGame();
    }
}

// Quick synchronization methods for client <-> client
public partial class GameCommunication : NetworkBehaviour
{
    [ServerRpc(RequireOwnership = false)]
    public void QuickOnTokenHighlightedChangeServerRpc(int index, bool isHighlighted, ServerRpcParams serverRpcParams = default)

    {
        ulong otherClientID = server.OtherPlayer(serverRpcParams.Receive.SenderClientId);
        QuickOnTokenHighlightChangedClientRpc(index, isHighlighted, NetworkUtility.MakeClientRpcParams(otherClientID));
    }

    [ClientRpc]
    public void QuickOnTokenHighlightChangedClientRpc(int index, bool isHighlighted, ClientRpcParams clientRpcParams = default)
    {
        GameBoard.OpGameBoardInstance.OnOpTokenHoveredChange(index, isHighlighted);
    }

    [ServerRpc(RequireOwnership = false)]
    public void QuickOnTokenDiscardedServerRpc(int index, ServerRpcParams serverRpcParams = default)
    {
        ulong otherClientID = server.OtherPlayer(serverRpcParams.Receive.SenderClientId);
        QuickOnTokenDiscardedClientRpc(index, NetworkUtility.MakeClientRpcParams(otherClientID));
    }

    [ClientRpc]
    public void QuickOnTokenDiscardedClientRpc(int index, ClientRpcParams clientRpcParams = default)
    {
        GameBoard.OpGameBoardInstance.OnOpTokenDiscarded(index);
    }
}
