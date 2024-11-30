using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Assertions;

public class GameClient : MonoBehaviour
{
    public static GameClient Instance;

    private void Awake()
    {
        Assert.IsNull(Instance);
        Instance = this;

        gamePhase = GamePhase.CONNECTING;
        ownClientID = NetworkManager.Singleton.LocalClientId;
        GameManager.Instance.ConnectToGameServerRpc();
    }

    public void StartSetupPhase(ClientSetupPhaseData data)
    {
        Assert.AreEqual(GamePhase.CONNECTING, gamePhase);
        gamePhase = GamePhase.SETUP;
        setupData = data;
        StartCoroutine(SetupPhaseEnum());
    }

    [Header("References")]
    [SerializeField] private GameObject turnDisplayTokenPrefab;

    private GamePhase gamePhase;
    private ulong ownClientID = 50;
    private ClientSetupPhaseData setupData;

    private IEnumerator SetupPhaseEnum()
    {
        // Flip turn display token
        GameObject turnDisplayTokenObject = Instantiate(turnDisplayTokenPrefab);
        TurnToken turnDisplayToken = turnDisplayTokenObject.GetComponent<TurnToken>();
        bool isFirstPlayer = ownClientID == setupData.firstPlayerClientID;
        yield return turnDisplayToken.FlipAnimationEnum(isFirstPlayer);

        // Display initial game tokens
        int tokenGridWidth = 6;
        int tokenGridHeight = 4;
        float spacing = 2.5f;
        float offsetX = -spacing * (tokenGridWidth - 1) / 2;
        float offsetY = -spacing * (tokenGridHeight - 1) / 2;

        List<GameObject> displayTokens = new List<GameObject>();
        for (int i = 0; i < setupData.initialGameTokenIDs.Length; i++)
        {
            int x = i % tokenGridWidth;
            int y = i / tokenGridWidth;
            Vector3 position = new Vector3(x * spacing + offsetX, 0.15f, y * spacing + offsetY);
            GameObject displayToken = TokenManager.Instance.CreateDisplayToken(setupData.initialGameTokenIDs[i], position);
            displayTokens.Add(displayToken);
        }

        yield return new WaitForSeconds(5.0f);
        for (int i = 0; i < displayTokens.Count; i++)
        {
            DestroyImmediate(displayTokens[i]);
        }
        displayTokens.Clear();

        yield return null;
    }
}
