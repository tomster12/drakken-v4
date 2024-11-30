using NUnit.Framework;
using Unity.Netcode;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;

    [Header("References")]
    [SerializeField] private GameClient client;
    [SerializeField] private GameServer server;

    private void Awake()
    {
        Assert.IsNull(Instance);
        Instance = this;

        NetworkManager.Singleton.OnClientConnectedCallback += (clientId) =>
        {
            if (IsClient)
            {
                Assert.AreEqual(NetworkManager.Singleton.LocalClientId, clientId);
                client.OnConnect();
                Destroy(server);
            }
        };

        NetworkManager.Singleton.OnClientDisconnectCallback += (clientId) =>
        {
            if (IsServer)
            {
                GameServer.Instance.OnClientDisconnect(clientId);
            }
            else if (IsClient)
            {
                client.OnDisconnect();
            }
        };
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            server.OnSpawn();
            Destroy(client);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void ConnectToGameServerRpc(ServerRpcParams serverRpcParams = default)
    {
        ulong clientID = serverRpcParams.Receive.SenderClientId;
        GameServer.Instance.OnClientConnect(clientID);
    }

    [ServerRpc(RequireOwnership = false)]
    public void DisconnectFromGameServerRpc(ServerRpcParams serverRpcParams = default)
    {
        ulong clientID = serverRpcParams.Receive.SenderClientId;
        GameServer.Instance.OnClientDisconnect(clientID);
    }

    [ClientRpc]
    public void GameStartClientRpc(ClientSetupPhaseData data)
    {
        GameClient.Instance.GameStart(data);
    }

    [ClientRpc]
    public void GameResetClientRpc()
    {
        GameClient.Instance.GameReset();
    }
}
