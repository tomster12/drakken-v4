using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.Assertions;

public class GameClient : MonoBehaviour
{
    public static GameClient Instance;

    [Header("References")]
    [SerializeField] public GameObject TurnTokenPrefab;
    [SerializeField] public GameObject OtherPlayerPrefab;
    [SerializeField] public GameObject bagObject;

    public ulong OwnClientID { get; private set; }
    public bool IsConnected { get; private set; }
    [HideInInspector] public GameObject OtherPlayerObject;

    public void OnConnect()
    {
        IsConnected = true;

        // Initialize variables
        OwnClientID = NetworkManager.Singleton.LocalClientId;
        phaseStates = new Dictionary<GamePhase, ClientPhaseState>
        {
            { GamePhase.CONNECTING, new ConnectingPhaseState(this) },
            { GamePhase.SETUP, new SetupPhaseState(this) }
        };

        // Start in connecting phase
        gamePhase = null;
        currentPhaseState = phaseStates[GamePhase.CONNECTING];
        currentPhaseState.Enter(null);

        // Tell the game server we are a new client
        // We only want to do this once when the client is spawned
        // The connecting state doesn't handle this connection
        // If GameClient exists then we are connected
        GameManager.Instance.ConnectToGameServerRpc();
    }

    public void OnDisconnect()
    {
        IsConnected = false;

        // Exit out of the current phase
        currentPhaseState.Exit(null);
    }

    public void GameStart(ClientSetupPhaseData data)
    {
        ((SetupPhaseState)phaseStates[GamePhase.SETUP]).SetData(data);
        TransitionToPhase(GamePhase.SETUP);
    }

    public void GameReset()
    {
        TransitionToPhase(GamePhase.CONNECTING);
    }

    public void TransitionToPhase(GamePhase gamePhase)
    {
        currentPhaseState?.Exit(gamePhase);
        currentPhaseState = phaseStates[gamePhase];
        currentPhaseState?.Enter(this.gamePhase);
        this.gamePhase = gamePhase;
    }

    private GamePhase? gamePhase;
    private ClientPhaseState currentPhaseState;
    private Dictionary<GamePhase, ClientPhaseState> phaseStates;

    private void Awake()
    {
        Assert.IsNull(Instance);
        Instance = this;
    }
}

public abstract class ClientPhaseState
{
    public ClientPhaseState(GameClient gameClient)
    {
        this.gameClient = gameClient;
    }

    public virtual void Enter(GamePhase? previousPhase)
    {
    }

    public virtual void Exit(GamePhase? nextPhase)
    {
    }

    public virtual void Update()
    { }

    protected GameClient gameClient;
}

public class ConnectingPhaseState : ClientPhaseState
{
    public ConnectingPhaseState(GameClient gameClient) : base(gameClient) { }

    public override void Enter(GamePhase? previousPhase)
    {
    }

    public override void Exit(GamePhase? nextPhase)
    {
    }
}

public class SetupPhaseState : ClientPhaseState
{
    public SetupPhaseState(GameClient gameClient) : base(gameClient) { }

    public override void Enter(GamePhase? previousPhase)
    {
        Assert.IsNotNull(data);
        coroutine = gameClient.StartCoroutine(MainEnum());
    }

    public override void Exit(GamePhase? nextPhase)
    {
        GameObject.Destroy(gameClient.OtherPlayerObject);
        gameClient.OtherPlayerObject = null;

        if (coroutine != null)
        {
            gameClient.StopCoroutine(coroutine);
            coroutine = null;
        }

        if (turnToken != null)
        {
            GameObject.Destroy(turnToken.gameObject);
            turnToken = null;
        }

        if (displayTokens != null)
        {
            for (int i = 0; i < displayTokens.Count; i++)
            {
                GameObject.Destroy(displayTokens[i]);
            }
            displayTokens = null;
        }

        data = null;
    }

    public void SetData(ClientSetupPhaseData data)
    {
        this.data = data;
    }

    private Coroutine coroutine;
    private ClientSetupPhaseData data;
    private TurnToken turnToken;
    private List<GameObject> displayTokens;

    private IEnumerator MainEnum()
    {
        // Spawn in other player
        gameClient.OtherPlayerObject = GameObject.Instantiate(gameClient.OtherPlayerPrefab);
        gameClient.OtherPlayerObject.transform.position = new Vector3(0.0f, -2.0f, 30.0f);

        yield return new WaitForSeconds(0.7f);

        // Flip turn token
        Vector3 startPos = new Vector3(0.0f, 0.15f, 0.0f);
        GameObject turnTokenObject = GameObject.Instantiate(gameClient.TurnTokenPrefab, startPos, Quaternion.identity);
        turnToken = turnTokenObject.GetComponent<TurnToken>();
        ParticleManager.Instance.SpawnPoof(startPos);
        yield return new WaitForSeconds(0.7f);
        bool isFirstPlayer = gameClient.OwnClientID == data.firstPlayerClientID;
        yield return turnToken.FlipAnimationEnum(isFirstPlayer, 0.7f);
        ParticleManager.Instance.SpawnPoof(startPos);
        yield return new WaitForSeconds(0.7f);

        // Display initial game tokens
        int tokenGridWidth = 6;
        int tokenGridHeight = 4;
        float spacing = 2.5f;
        float offsetX = -spacing * (tokenGridWidth - 1) / 2;
        float offsetY = spacing * (tokenGridHeight - 1) / 2;

        float tokenAnimationTime = 0.7f;
        float tokenWaitTime = 0.06f;
        displayTokens = new List<GameObject>();
        for (int i = 0; i < data.initialGameTokenIDs.Length; i++)
        {
            int x = i % tokenGridWidth;
            int y = i / tokenGridWidth;
            Vector3 targetPos = new Vector3(x * spacing + offsetX, 0.15f, -y * spacing + offsetY);
            GameObject displayToken = TokenManager.Instance.CreateDisplayToken(data.initialGameTokenIDs[i], gameClient.bagObject.transform.position);
            displayTokens.Add(displayToken);

            // Animate from bag to position after a delay based on I
            gameClient.StartCoroutine(AnimationUtility.StartAfterDuration(tokenWaitTime * i, () =>
            {
                gameClient.StartCoroutine(AnimationUtility.AnimateFromBagToPosition(displayToken.transform, gameClient.bagObject.transform.position, targetPos, 6.0f, tokenAnimationTime));
            }));
        }

        // Destroy after all tokens animated
        float displayEndWaitTime = data.initialGameTokenIDs.Length * tokenWaitTime + tokenAnimationTime + 2.5f;
        yield return new WaitForSeconds(displayEndWaitTime);
        for (int i = 0; i < displayTokens.Count; i++)
        {
            GameObject.Destroy(displayTokens[i]);
        }
        displayTokens.Clear();

        yield return null;
    }
}
