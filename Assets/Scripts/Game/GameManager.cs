using NUnit.Framework;
using Unity.Netcode;
using UnityEngine;

// This class manages enabling / disabling game objects based on the network role
// Also acts as a communication bridge between the server and client
// - Server setup   -> OnNetworkSpawn() : server.OnNetworkStart()
// - Client connect -> Awake()          : client.OnNetworkConnect()

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
            // We as the client are connecting to the server
            if (IsClient)
            {
                Assert.AreEqual(NetworkManager.Singleton.LocalClientId, clientId);
                Destroy(server);
                client.gameObject.SetActive(true);
                client.OnNetworkConnect();
            }
        };

        NetworkManager.Singleton.OnClientDisconnectCallback += (clientId) =>
        {
            // Server has heard that a client has disconnected
            if (IsServer)
            {
                GameServer.Instance.OnClientDisconnect(clientId);
            }

            // We as the client are disconnecting from the server
            else if (IsClient)
            {
                client.OnNetworkDisconnect();
                client.gameObject.SetActive(false);
            }
        };
    }

    public override void OnNetworkSpawn()
    {
        // Server is starting up
        if (IsServer)
        {
            Destroy(client);
            server.gameObject.SetActive(true);
            server.OnNetworkStart();
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
    public void GameStartedClientRpc(ClientSetupPhaseData data)
    {
        GameClient.Instance.OnGameStarted(data);
    }

    [ClientRpc]
    public void GameResetClientRpc()
    {
        GameClient.Instance.OnGameReset();
    }
}
