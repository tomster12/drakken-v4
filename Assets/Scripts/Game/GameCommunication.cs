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

    private void Awake()
    {
        Assert.IsNull(Instance);
        Instance = this;
    }
}
