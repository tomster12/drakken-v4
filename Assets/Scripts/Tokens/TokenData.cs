using UnityEngine;

[CreateAssetMenu(fileName = "TokenData", menuName = "TokenData", order = 1)]
public class TokenData : ScriptableObject
{
    public string ID;
    public TokenRarity Rarity;
    public string ImplType;
}
