using Unity.Netcode;
using UnityEngine;
using UnityEngine.Assertions;

public class GameCommunication : NetworkBehaviour
{
    public static GameCommunication Instance;

    [Header("References")]
    [SerializeField] private GameClient client;
    [SerializeField] private GameServer server;

    [ServerRpc(RequireOwnership = false)]
    public void ConnectToGameServerRpc(ServerRpcParams serverRpcParams = default)
    {
        ulong clientID = serverRpcParams.Receive.SenderClientId;
        GameServer.Instance.OnGameClientConnect(clientID);
    }

    [ServerRpc(RequireOwnership = false)]
    public void DisconnectFromGameServerRpc(ServerRpcParams serverRpcParams = default)
    {
        ulong clientID = serverRpcParams.Receive.SenderClientId;
        GameServer.Instance.OnGameClientDisconnect(clientID);
    }

    [ClientRpc]
    public void GameStartedClientRpc(ClientSetupPhaseData data)
    {
        GameClient.Instance.OnGameStarted(data);
    }

    [ClientRpc]
    public void GameResetClientRpc()
    {
        GameClient.Instance.OnGameReset();
    }

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

    private void Awake()
    {
        Assert.IsNull(Instance);
        Instance = this;
    }
}
