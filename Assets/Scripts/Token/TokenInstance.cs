using System;
using Unity.Netcode;

[Serializable]
public class TokenInstance : INetworkSerializable
{
    public string tokenID;
    public string instanceID;

    public TokenInstance()
    {
        tokenID = "";
        instanceID = "";
    }

    public TokenInstance(string tokenID, string instanceID)
    {
        this.tokenID = tokenID;
        this.instanceID = instanceID;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref tokenID);
        serializer.SerializeValue(ref instanceID);
    }

    public static TokenInstance FromString(string str)
    {
        string[] parts = str.Split('_');
        return new TokenInstance(parts[0], parts[1]);
    }

    public override string ToString()
    {
        return tokenID + "_" + instanceID;
    }

    public override bool Equals(object obj)
    {
        if (obj == null || GetType() != obj.GetType())
        {
            return false;
        }

        TokenInstance other = (TokenInstance)obj;
        return tokenID == other.tokenID && instanceID == other.instanceID;
    }

    public override int GetHashCode()
    {
        return tokenID.GetHashCode() ^ instanceID.GetHashCode();
    }
}
