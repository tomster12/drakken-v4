using NUnit.Framework;
using System;
using System.Collections.Generic;
using Unity.Netcode;

[Serializable]
public class PlayPhaseStartData : INetworkSerializable
{
    public DiceData[] Player1Dice;
    public DiceData[] Player2Dice;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        NetworkUtility.SerializeArray(ref Player1Dice, serializer);
        NetworkUtility.SerializeArray(ref Player2Dice, serializer);
    }
}
