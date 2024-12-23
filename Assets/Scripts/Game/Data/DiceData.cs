using System;
using Unity.Netcode;

[Serializable]
public class DiceData : INetworkSerializable
{
    public int Sides = 6;
    public int Value = 1;

    public void Roll()
    {
        Value = UnityEngine.Random.Range(1, Sides + 1);
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref Sides);
        serializer.SerializeValue(ref Value);
    }
}
