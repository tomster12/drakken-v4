using System;
using Unity.Netcode;

[Serializable]
public class SetupPhaseEndData : INetworkSerializable
{
    public TokenInstance[] DiscardedTokens;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        NetworkUtility.SerializeArray(ref DiscardedTokens, serializer);
    }
}
