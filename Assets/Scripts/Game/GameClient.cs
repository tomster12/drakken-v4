using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.Assertions;

public class GameClient : MonoBehaviour
{
    public static GameClient Instance;

    public static Vector3 GetTurnTokenPosition(bool isLocalPlayer)
    {
        return isLocalPlayer ? new Vector3(-7.0f, 0.15f, -6.5f) : new Vector3(-7.0f, 0.15f, 6.5f);
    }

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
        mainAnimation = DoMainAnimation();
    }

    public override void Exit(GamePhase? nextPhase)
    {
        GameObject.Destroy(gameClient.OtherPlayerObject);
        gameClient.OtherPlayerObject = null;

        if (mainAnimation != null)
        {
            mainAnimation.Stop();
            mainAnimation = null;
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

    private CompositeCoroutine mainAnimation;
    private ClientSetupPhaseData data;
    private TurnToken turnToken;
    private List<GameObject> displayTokens;

    private CompositeCoroutine DoMainAnimation()
    {
        IEnumerator ParentCoroutine(CompositeCoroutine composite)
        {
            // Spawn in other player
            gameClient.OtherPlayerObject = GameObject.Instantiate(gameClient.OtherPlayerPrefab);
            gameClient.OtherPlayerObject.transform.position = new Vector3(0.0f, -2.0f, 30.0f);

            // Spawn in, flip turn token, animate to relevant position
            Vector3 startPos = new Vector3(0.0f, 0.15f, 0.0f);
            GameObject turnTokenObject = GameObject.Instantiate(gameClient.TurnTokenPrefab, startPos, Quaternion.identity);
            turnToken = turnTokenObject.GetComponent<TurnToken>();
            ParticleManager.Instance.SpawnPoof(startPos);
            yield return new WaitForSeconds(0.85f);

            bool isFirstPlayer = gameClient.OwnClientID == data.firstPlayerClientID;
            yield return turnToken.DoFlipAnimation(isFirstPlayer, 1.0f, 4.0f, 2);
            yield return new WaitForSeconds(0.3f);

            Vector3 upPos = startPos + Vector3.up * 2.5f;
            composite.StartCouroutine(
                AnimationUtility.AnimatePosToPosWithEasing(turnToken.transform, startPos, upPos, 0.6f, Easing.EaseOutBack)
            );
            yield return new WaitForSeconds(0.1f);
            yield return turnToken.DoChangeAnimation(0.4f);
            yield return new WaitForSeconds(0.05f);

            yield return AnimationUtility.AnimatePosToPosWithEasing(turnToken.transform, upPos, GameClient.GetTurnTokenPosition(isFirstPlayer), 0.95f, Easing.EaseInOutCubic);
            yield return new WaitForSeconds(0.55f);

            // Display initial game tokens
            int tokenGridWidth = 6;
            int tokenGridHeight = 4;
            float spacing = 2.5f;
            float offsetX = -spacing * (tokenGridWidth - 1) / 2;
            float offsetY = spacing * (tokenGridHeight - 1) / 2;

            float tokenAnimateTime = 0.7f;
            float tokenWaitTime = 0.06f;
            float lastAnimateEndTime = 0.0f;

            displayTokens = new List<GameObject>();
            for (int i = 0; i < data.initialGameTokenIDs.Length; i++)
            {
                int x = i % tokenGridWidth;
                int y = i / tokenGridWidth;

                Vector3 targetPos = new Vector3(x * spacing + offsetX, 0.15f, -y * spacing + offsetY);
                GameObject displayToken = TokenManager.Instance.CreateDisplayToken(data.initialGameTokenIDs[i], gameClient.bagObject.transform.position);
                displayTokens.Add(displayToken);

                float tokenTimeStart = tokenWaitTime * i;
                lastAnimateEndTime = Mathf.Max(lastAnimateEndTime, tokenTimeStart + tokenAnimateTime);
                composite.StartCouroutine(
                    AnimationUtility.StartAfterDuration(tokenTimeStart,
                        AnimationUtility.AnimatePosToPosWithLift(displayToken.transform, gameClient.bagObject.transform.position, targetPos, 6.0f, tokenAnimateTime, 0.2f, 1.0f)
                    )
                );
            }

            yield return new WaitForSeconds(lastAnimateEndTime + 2.5f);

            // Animate them all back into the bag
            lastAnimateEndTime = 0.0f;
            for (int i = 0; i < displayTokens.Count; i++)
            {
                int x = i % tokenGridWidth;
                int y = i / tokenGridWidth;

                float tokenTimeStart = tokenWaitTime * ((tokenGridWidth - x) + Mathf.Abs(1.5f - y));
                lastAnimateEndTime = Mathf.Max(lastAnimateEndTime, tokenTimeStart + tokenAnimateTime);
                composite.StartCouroutine(
                    AnimationUtility.StartAfterDuration(tokenTimeStart,
                        AnimationUtility.AnimatePosToPosWithLift(displayTokens[i].transform, displayTokens[i].transform.position, gameClient.bagObject.transform.position, 6.0f, tokenAnimateTime, 0.0f, 0.8f)
                    )
                );
            }

            yield return new WaitForSeconds(lastAnimateEndTime + 0.3f);

            // Delete them all once theyre all back in the bag
            for (int i = 0; i < displayTokens.Count; i++)
            {
                GameObject.Destroy(displayTokens[i]);
            }
            displayTokens.Clear();
        }

        CompositeCoroutine composite = new CompositeCoroutine(gameClient);
        composite.StartCouroutine(ParentCoroutine(composite));
        return composite;
    }
}
