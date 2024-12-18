using System;
using Unity.Netcode;

[Serializable]
public class ClientSetupPhaseData : INetworkSerializable
{
    public ulong firstTurnClientID;
    public ulong player1ClientID;
    public ulong player2ClientID;
    public TokenInstance[] initialGameTokenInstances;
    public TokenInstance[] player1DraftTokenInstances;
    public TokenInstance[] player2DraftTokenInstances;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref firstTurnClientID);
        serializer.SerializeValue(ref player1ClientID);
        serializer.SerializeValue(ref player2ClientID);
        NetworkUtility.SerializeTokenInstanceArray(ref initialGameTokenInstances, serializer);
        NetworkUtility.SerializeTokenInstanceArray(ref player1DraftTokenInstances, serializer);
        NetworkUtility.SerializeTokenInstanceArray(ref player2DraftTokenInstances, serializer);
    }
}
