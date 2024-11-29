using Unity.Netcode;
using UnityEngine;

public class HelloWorldPlayer : NetworkBehaviour
{
    public NetworkVariable<Vector3> Position = new NetworkVariable<Vector3>();

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            MoveClient();
        }
    }

    public void MoveClient()
    {
        MoveServerRpc();
    }

    [Rpc(SendTo.Server)]
    private void MoveServerRpc(RpcParams rpcParams = default)
    {
        var randomPosition = new Vector3(Random.Range(-3f, 3f), 1f, Random.Range(-3f, 3f));
        transform.position = randomPosition;
        Position.Value = randomPosition;
    }

    private void Update()
    {
        transform.position = Position.Value;
    }
}
