using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Properties;
using UnityEngine;

public class GameBoard : MonoBehaviour
{
    public List<DisplayToken> Tokens { get; private set; }
    public Action<int> OnTokenDiscard = delegate { };

    public void Init(bool isLocalBoard)
    {
        this.isLocalBoard = isLocalBoard;
        Tokens = new List<DisplayToken>();

        ResetBoard();
    }

    public void ResetBoard()
    {
        foreach (DisplayToken displayToken in Tokens)
        {
            displayToken.InstantDestroy();
        }

        Tokens.Clear();

        OnTokenDiscard = delegate { };
    }

    public async Task DrawTokens(CancellationToken ctoken, TokenInstance[] tokenInstances)
    {
        List<Task> animationTasks = new();

        foreach (TokenInstance tokenInstance in tokenInstances)
        {
            // Only show the token icon if this is the local player
            DisplayToken displayToken;
            if (isLocalBoard)
            {
                displayToken = TokenManager.Instance.CreateDisplayToken(tokenInstance, bagObject.transform.position);
                displayToken.OnInteract += OnTokenInteract;
            }
            else
            {
                displayToken = TokenManager.Instance.CreateDisplayToken(null, bagObject.transform.position);
            }
            Tokens.Add(displayToken);

            // Animate the token from the bag to the board
            Vector3 targetPosition = GetTokenPosition(Tokens.Count - 1);
            animationTasks.Add(AnimationUtility.AnimatePosToPosWithLift(ctoken, displayToken.transform, bagObject.transform.position, targetPosition, 6.0f, 0.65f, 0.2f, 1.0f));
        }

        await Task.WhenAll(animationTasks);
    }

    public void SetTokenMode(TokenInteractMode tokenInteractMode)
    {
        this.tokenInteractMode = tokenInteractMode;

        foreach (DisplayToken displayToken in Tokens)
        {
            displayToken.SetTokenInteractMode(tokenInteractMode);
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

    [Header("References")]
    [SerializeField] private Transform board;
    [SerializeField] private Transform tokenSpacingTfm;
    [SerializeField] private Transform turnTokenTfm;
    [SerializeField] private GameObject bagObject;

    private bool isLocalBoard;
    private TokenInteractMode tokenInteractMode = TokenInteractMode.NONE;

    private void OnTokenInteract(DisplayToken displayToken)
    {
        if (tokenInteractMode == TokenInteractMode.DISCARDING)
        {
            Tokens.Remove(displayToken);
            displayToken.InstantDestroy();
            OnTokenDiscard(Tokens.Count);
        }
    }
}
