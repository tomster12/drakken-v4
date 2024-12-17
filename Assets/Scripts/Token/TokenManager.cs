using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Assertions;
using System;

public class TokenManager : MonoBehaviour
{
    public static TokenManager Instance;

    public static Dictionary<TokenRarity, float> RARITY_DISTRIBUTION = new Dictionary<TokenRarity, float>()
    {
        { TokenRarity.COMMON, 0.6f },
        { TokenRarity.RARE, 0.3f },
        { TokenRarity.EPIC, 0.08f },
        { TokenRarity.LEGENDARY, 0.02f }
    };

    public static Dictionary<TokenRarity, int> RARITY_MAX_COUNT = new Dictionary<TokenRarity, int>()
    {
        { TokenRarity.COMMON, 30 },
        { TokenRarity.RARE, 30 },
        { TokenRarity.EPIC, 30 },
        { TokenRarity.LEGENDARY, 30 }
    };

    public List<TokenInstance> GetTokenSelection(int count)
    {
        // Randomly sample tokens from each rarity with a maximumum amount of each rarity
        List<TokenInstance> selection = new List<TokenInstance>();
        Dictionary<string, int> tokenCounts = new Dictionary<string, int>();

        while (selection.Count < count)
        {
            TokenRarity rarity = GetRandomWeightedRarity();

            if (tokenByRarity[rarity].Count == 0) continue;

            int index = UnityEngine.Random.Range(0, tokenByRarity[rarity].Count);
            TokenData tokenData = tokenByRarity[rarity][index];

            if (!tokenCounts.ContainsKey(tokenData.ID))
            {
                tokenCounts[tokenData.ID] = 0;
            }

            if (tokenCounts[tokenData.ID] < RARITY_MAX_COUNT[rarity])
            {
                tokenCounts[tokenData.ID]++;
                selection.Add(GetTokenInstance(tokenData.ID));
            }
        }

        return selection;
    }

    public TokenRarity GetRandomWeightedRarity()
    {
        float randomValue = UnityEngine.Random.value;

        foreach (KeyValuePair<TokenRarity, float> entry in RARITY_DISTRIBUTION)
        {
            randomValue -= entry.Value;
            if (randomValue <= 0)
            {
                return entry.Key;
            }
        }

        throw new Exception("Failed to get random rarity");
    }

    public TokenData GetTokenData(string tokenID)
    {
        return tokenByID[tokenID];
    }

    public DisplayToken CreateDisplayToken(TokenInstance tokenInstance, Vector3 position)
    {
        GameObject displayTokenObject = Instantiate(displayTokenPrefab, position, Quaternion.identity);
        DisplayToken displayToken = displayTokenObject.GetComponent<DisplayToken>();
        displayToken.SetTokenInstance(tokenInstance);
        return displayToken;
    }

    [Header("References")]
    [SerializeField] private GameObject displayTokenPrefab;

    [Header("Config")]
    [SerializeField] private List<TokenData> tokens;

    private Dictionary<TokenRarity, List<TokenData>> tokenByRarity;
    private Dictionary<string, TokenData> tokenByID;
    private Dictionary<string, Type> tokenImplTypes;
    private int nextTokenInstanceID = 0;

    private void Awake()
    {
        Assert.IsNull(Instance);
        Instance = this;

        // Initialize token references
        tokenByRarity = new Dictionary<TokenRarity, List<TokenData>>();
        tokenByID = new Dictionary<string, TokenData>();
        foreach (TokenRarity rarity in RARITY_DISTRIBUTION.Keys)
        {
            tokenByRarity[rarity] = new List<TokenData>();
        }
        foreach (TokenData token in tokens)
        {
            tokenByRarity[token.Rarity].Add(token);
            tokenByID[token.ID] = token;
        }

        // Initialize token implementation types
        tokenImplTypes = new Dictionary<string, Type>();
        foreach (TokenData token in tokens)
        {
            Type tokenImplType = Type.GetType(token.ImplType);

            if (tokenImplType == null)
            {
                throw new Exception("Failed to get type for token implementation: " + token.ImplType);
            }

            tokenImplTypes[token.ID] = tokenImplType;
        }
    }

    private TokenInstance GetTokenInstance(string tokenID)
    {
        // This shouldn't be used on the client
        return new TokenInstance(tokenID, (nextTokenInstanceID++).ToString());
    }
}
