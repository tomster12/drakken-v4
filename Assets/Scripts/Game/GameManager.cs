using NUnit.Framework;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;

    [Header("References")]
    [SerializeField] private GameObject clientPrefab;
    [SerializeField] private GameObject serverPrefab;

    private void Awake()
    {
        Assert.IsNull(Instance);
        Instance = this;

        NetworkManager.Singleton.OnClientConnectedCallback += (clientId) =>
        {
            if (IsClient)
            {
                Assert.AreEqual(NetworkManager.Singleton.LocalClientId, clientId);
                Instantiate(clientPrefab);
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
                DestroyImmediate(GameClient.Instance.gameObject);
            }
        };
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            Instantiate(serverPrefab);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void ConnectToGameServerRpc(ServerRpcParams serverRpcParams = default)
    {
        ulong clientID = serverRpcParams.Receive.SenderClientId;
        GameServer.Instance.OnClientConnect(clientID);
    }

    [ClientRpc]
    public void StartSetupPhaseClientRpc(ClientSetupPhaseData data)
    {
        GameClient.Instance.StartSetupPhase(data);
    }

    [ClientRpc]
    public void ResetToConnectingPhaseClientRpc()
    {
        GameClient.Instance.ResetToConnectingPhase();
    }
}
