using System;
using Unity.Collections;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine.PlayerLoop;
using UnityEngine;

public static class NetworkUtility
{
    public static ClientRpcParams MakeClientRpcParams(ulong id)
    {
        return new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new[] { id }
            }
        };
    }

    public static void SerializeArray<T, D>(ref D[] array, BufferSerializer<T> serializer) where T : IReaderWriter where D : INetworkSerializable, new()
    {
        if (serializer.IsWriter)
        {
            FastBufferWriter fastBufferWriter = serializer.GetFastBufferWriter();
            fastBufferWriter.WriteValueSafe(array.Length);
            for (int i = 0; i < array.Length; i++)
            {
                fastBufferWriter.WriteValueSafe(array[i]);
            }
        }

        if (serializer.IsReader)
        {
            FastBufferReader fastBufferReader = serializer.GetFastBufferReader();
            fastBufferReader.ReadValueSafe(out int length);
            array = new D[length];
            for (int i = 0; i < length; i++)
            {
                fastBufferReader.ReadValueSafe(out array[i]);
            }
        }
    }
}
