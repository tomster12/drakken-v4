using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;
using UnityEngine.Assertions;

public class GameBoard : MonoBehaviour
{
    public static GameBoard MyGameBoardInstance { get; private set; } = null;
    public static GameBoard OpGameBoardInstance { get; private set; } = null;

    public Action<DisplayToken, int> OnTokenDiscard = delegate { };
    public List<DisplayToken> Tokens { get; private set; }

    public void Init(GameClient gameClient, bool isLocalBoard)
    {
        // Initialize singletons
        if (isLocalBoard)
        {
            Assert.IsNull(MyGameBoardInstance);
            MyGameBoardInstance = this;
        }
        else
        {
            Assert.IsNull(OpGameBoardInstance);
            OpGameBoardInstance = this;
        }

        this.gameClient = gameClient;
        this.isLocalBoard = isLocalBoard;
        Tokens = new List<DisplayToken>();
        ResetBoard();
    }

    public void ResetBoard()
    {
        foreach (DisplayToken displayToken in Tokens)
        {
            displayToken.Destroy();
        }

        Tokens.Clear();

        OnTokenDiscard = delegate { };
    }

    public async Task DrawTokens(CancellationToken ctoken, TokenInstance[] tokenInstances)
    {
        List<Task> animationTasks = new();

        for (int i = 0; i < tokenInstances.Length; i++)
        {
            // Only show the token icon if this is the local player
            DisplayToken displayToken;
            if (isLocalBoard)
            {
                displayToken = TokenManager.Instance.CreateDisplayToken(tokenInstances[i], bagObject.transform.position);
                displayToken.OnHoveredChange += OnTokenHoveredChange;
                displayToken.OnInteract += OnTokenInteract;
            }
            else
            {
                displayToken = TokenManager.Instance.CreateDisplayToken(null, bagObject.transform.position);
            }
            Tokens.Add(displayToken);

            // Animate the token from the bag to the board
            int delay = i * 60;
            Vector3 targetPosition = GetTokenPosition(Tokens.Count - 1);
            animationTasks.Add(AnimationUtility.DelayTask(ctoken, delay,
                () => AnimationUtility.AnimatePosToPosWithLift(ctoken, displayToken.transform, bagObject.transform.position, targetPosition, 6.0f, 0.65f, 0.2f, 1.0f)
            ));
        }

        await Task.WhenAll(animationTasks);

        for (int i = 0; i < Tokens.Count; i++)
        {
            Tokens[i].SetTarget(GetTokenPosition(i), Quaternion.identity, 0.1f, 0.1f);
        }
    }

    public void SetTokenMode(TokenInteractMode tokenInteractMode)
    {
        Assert.IsTrue(isLocalBoard);

        this.tokenInteractMode = tokenInteractMode;

        if (tokenInteractMode == TokenInteractMode.NONE)
        {
            foreach (DisplayToken displayToken in Tokens)
            {
                if (displayToken.IsHighlighted)
                {
                    displayToken.IsHighlighted = false;
                    GameCommunication.Instance.QuickOnTokenHighlightedChangeServerRpc(Tokens.IndexOf(displayToken), false);
                }
            }
        }
    }

    public Vector3 GetTurnTokenPosition()
    {
        return turnTokenTfm.position;
    }

    public Vector3 GetTokenPosition(int index)
    {
        return tokenSpacingTfm.position + index * tokenSpacingTfm.localScale.x * tokenSpacingTfm.right;
    }

    public void OnOpTokenHoveredChange(int index, bool isHovered)
    {
        Assert.IsTrue(!isLocalBoard);
        Tokens[index].IsHighlighted = isHovered;
    }

    public void OnOpTokenDiscarded(int index)
    {
        Assert.IsTrue(!isLocalBoard);

        Tokens[index].Destroy();
        Tokens.RemoveAt(index);

        for (int i = 0; i < Tokens.Count; i++)
        {
            Tokens[i].SetTarget(GetTokenPosition(i), Quaternion.identity, 0.1f, 0.1f);
        }
    }

    [Header("References")]
    [SerializeField] private Transform board;
    [SerializeField] private Transform tokenSpacingTfm;
    [SerializeField] private Transform turnTokenTfm;
    [SerializeField] private GameObject bagObject;

    private GameClient gameClient;
    private bool isLocalBoard;
    private TokenInteractMode tokenInteractMode = TokenInteractMode.NONE;

    private void OnTokenHoveredChange(DisplayToken displayToken, bool isHovered)
    {
        if (!isLocalBoard) return;
        if (tokenInteractMode == TokenInteractMode.DISCARDING)
        {
            displayToken.IsHighlighted = isHovered;
            GameCommunication.Instance.QuickOnTokenHighlightedChangeServerRpc(Tokens.IndexOf(displayToken), isHovered);
        }
    }

    private void OnTokenInteract(DisplayToken displayToken)
    {
        if (tokenInteractMode == TokenInteractMode.DISCARDING) DiscardToken(displayToken);
    }

    private void DiscardToken(DisplayToken displayToken)
    {
        Assert.IsTrue(isLocalBoard);
        Assert.IsTrue(tokenInteractMode == TokenInteractMode.DISCARDING);

        int index = Tokens.IndexOf(displayToken);
        displayToken.Destroy();
        Tokens.RemoveAt(index);

        for (int i = 0; i < Tokens.Count; i++)
        {
            Tokens[i].SetTarget(GetTokenPosition(i), Quaternion.identity, 0.1f, 0.1f);
        }

        OnTokenDiscard(displayToken, index);
        GameCommunication.Instance.QuickOnTokenDiscardedServerRpc(index);
    }
}
