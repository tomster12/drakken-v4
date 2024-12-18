using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class GameBoard : MonoBehaviour
{
    public Action<int> OnTokenDiscard = delegate { };

    public void HardReset()
    {
        foreach (DisplayToken displayToken in tokens)
        {
            displayToken.InstantDestroy();
        }

        tokens.Clear();
    }

    public async Task DrawTokens(CancellationToken ctoken, TokenInstance[] tokenInstances)
    {
        List<Task> animationTasks = new();

        foreach (TokenInstance tokenInstance in tokenInstances)
        {
            // Only show the token icon if this is the local player
            DisplayToken displayToken;
            if (isLocalPlayer)
            {
                displayToken = TokenManager.Instance.CreateDisplayToken(tokenInstance, bagObject.transform.position);
                displayToken.OnInteract += OnTokenInteract;
            }
            else
            {
                displayToken = TokenManager.Instance.CreateDisplayToken(null, bagObject.transform.position);
            }
            tokens.Add(displayToken);

            // Animate the token from the bag to the board
            Vector3 targetPosition = GetTokenPosition(tokens.Count - 1);
            animationTasks.Add(AnimationUtility.AnimatePosToPosWithLift(ctoken, displayToken.transform, bagObject.transform.position, targetPosition, 6.0f, 0.65f, 0.2f, 1.0f));
        }

        await Task.WhenAll(animationTasks);
    }

    public void SetTokenMode(TokenInteractMode tokenInteractMode)
    {
        foreach (DisplayToken displayToken in tokens)
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
        return tokenSpacingTfm.position + tokenSpacingTfm.right * tokenSpacingTfm.localScale.x * index;
    }

    [Header("References")]
    [SerializeField] private Transform board;
    [SerializeField] private Transform tokenSpacingTfm;
    [SerializeField] private Transform turnTokenTfm;
    [SerializeField] private GameObject bagObject;

    [Header("Config")]
    [SerializeField] private bool isLocalPlayer;
    private List<DisplayToken> tokens = new();
    private TokenInteractMode tokenInteractMode = TokenInteractMode.NONE;

    private void OnTokenInteract(DisplayToken displayToken)
    {
        if (tokenInteractMode == TokenInteractMode.DISCARDING)
        {
            tokens.Remove(displayToken);
            displayToken.InstantDestroy();
            OnTokenDiscard(tokens.Count);
        }
    }
}
