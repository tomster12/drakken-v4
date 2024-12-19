using System;
using Unity.Netcode;

[Serializable]
public class PlayPhaseStartData : INetworkSerializable
{
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
    }
}
