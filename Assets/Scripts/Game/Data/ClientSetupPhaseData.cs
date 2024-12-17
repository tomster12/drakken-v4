using System;
using Unity.Netcode;

[Serializable]
public class ClientSetupPhaseData : INetworkSerializable
{
    public ulong firstPlayerClientID;
    public TokenInstance[] initialGameTokenInstances;
    public TokenInstance[] player1DraftTokenInstances;
    public TokenInstance[] player2DraftTokenInstances;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref firstPlayerClientID);
        NetworkUtility.SerializeTokenInstanceArray(ref initialGameTokenInstances, serializer);
        NetworkUtility.SerializeTokenInstanceArray(ref player1DraftTokenInstances, serializer);
        NetworkUtility.SerializeTokenInstanceArray(ref player2DraftTokenInstances, serializer);
    }
}
