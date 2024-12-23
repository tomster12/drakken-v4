using System;
using Unity.Netcode;
using UnityEngine;

[Serializable]
public class DiceData : INetworkSerializable
{
    public int MaxValue = 6;
    public int FaceValue = 1;

    public void Roll()
    {
        FaceValue = UnityEngine.Random.Range(1, MaxValue + 1);
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref MaxValue);
        serializer.SerializeValue(ref FaceValue);
    }
}
