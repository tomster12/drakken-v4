using System;
using Unity.Netcode;

[Serializable]
public struct ClientSetupPhaseData : INetworkSerializable
{
    public ulong firstPlayerClientID;
    public string[] initialGameTokenIDs;
    public string[] player1DraftTokenIDs;
    public string[] player2DraftTokenIDs;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref firstPlayerClientID);
        NetworkUtility.SerializeStringArray(ref initialGameTokenIDs, serializer);
        NetworkUtility.SerializeStringArray(ref player1DraftTokenIDs, serializer);
        NetworkUtility.SerializeStringArray(ref player2DraftTokenIDs, serializer);
    }
}
