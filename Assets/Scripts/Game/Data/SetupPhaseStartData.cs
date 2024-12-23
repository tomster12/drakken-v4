using System;
using Unity.Netcode;

[Serializable]
public class SetupPhaseStartData : INetworkSerializable
{
    public ulong FirstTurnClientID;
    public ulong Player1ClientID;
    public ulong Player2ClientID;
    public TokenInstance[] AllTokens;
    public TokenInstance[] Player1BoardTokens;
    public TokenInstance[] Player2BoardTokens;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref FirstTurnClientID);
        serializer.SerializeValue(ref Player1ClientID);
        serializer.SerializeValue(ref Player2ClientID);
        NetworkUtility.SerializeArray(ref AllTokens, serializer);
        NetworkUtility.SerializeArray(ref Player1BoardTokens, serializer);
        NetworkUtility.SerializeArray(ref Player2BoardTokens, serializer);
    }
}
