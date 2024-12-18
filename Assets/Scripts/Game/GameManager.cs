using UnityEngine;
using Unity.Multiplayer.Playmode;
using System.Linq;

public class GameManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameClient client;
    [SerializeField] private GameServer server;

    private void Start()
    {
        // If you detect batch mode then you are in a headless environment therefore server
        if (Application.isBatchMode)
        {
            StartServer();
        }

        // Otherwise, check if the current player has the MPPM "Server" tag, otherwise just run client
        else
        {
            var mppmTags = CurrentPlayer.ReadOnlyTags();
            if (mppmTags.Contains("Server"))
            {
                StartServer();
            }
            else
            {
                StartClient();
            }
        }
    }

    private void StartClient()
    {
        Destroy(server.gameObject);
        client.Init();
    }

    private void StartServer()
    {
        Destroy(client.gameObject);
        server.Init();
    }
}
