using System;
using Unity.Netcode;

[Serializable]
public class SetupPhaseEndData : INetworkSerializable
{
    public TokenInstance[] discardedTokenInstances;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        NetworkUtility.SerializeTokenInstanceArray(ref discardedTokenInstances, serializer);
    }
}
