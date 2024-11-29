using Unity.Netcode;
using UnityEngine;

namespace HelloWorld
{
    public class HelloWorldManager : MonoBehaviour
    {
        [SerializeField] private NetworkManager networkManager;

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 300, 300));
            if (!networkManager.IsClient && !networkManager.IsServer)
            {
                DrawGUIStartButtons();
            }
            else
            {
                DrawGUIStatusLabels();
                DrawGUISubmitNewPosition();
            }

            GUILayout.EndArea();
        }

        private void DrawGUIStartButtons()
        {
            if (GUILayout.Button("Host")) networkManager.StartHost();
            if (GUILayout.Button("Client")) networkManager.StartClient();
            if (GUILayout.Button("Server")) networkManager.StartServer();
        }

        private void DrawGUIStatusLabels()
        {
            var mode = networkManager.IsHost ?
                "Host" : networkManager.IsServer ? "Server" : "Client";

            GUILayout.Label("Transport: " +
                networkManager.NetworkConfig.NetworkTransport.GetType().Name);
            GUILayout.Label("Mode: " + mode);
        }

        private void DrawGUISubmitNewPosition()
        {
            if (GUILayout.Button(networkManager.IsServer ? "Move" : "Request Position Change"))
            {
                if (networkManager.IsServer && !networkManager.IsClient)
                {
                    foreach (ulong uid in networkManager.ConnectedClientsIds)
                    {
                        networkManager.SpawnManager.GetPlayerNetworkObject(uid).GetComponent<HelloWorldPlayer>().MoveClient();
                    }
                }
                else
                {
                    var playerObject = networkManager.SpawnManager.GetLocalPlayerObject();
                    var player = playerObject.GetComponent<HelloWorldPlayer>();
                    player.MoveClient();
                }
            }
        }
    }
}
