using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

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

    public IEnumerator DrawTokens(TokenInstance[] tokenInstances)
    {
        foreach (TokenInstance tokenInstance in tokenInstances)
        {
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

            Vector3 targetPosition = GetTokenPosition(tokens.Count - 1);
            yield return AnimationUtility.AnimatePosToPosWithLift(displayToken.transform, bagObject.transform.position, targetPosition, 6.0f, 0.65f, 0.2f, 1.0f);
        }
    }

    public IEnumerator DiscardUntilAmount(int amount)
    {
        Assert.IsTrue(tokens.Count > amount);
        Assert.IsFalse(isDiscarding);

        isDiscarding = true;

        foreach (DisplayToken displayToken in tokens)
        {
            displayToken.SetTokenInteractMode(TokenInteractMode.DISCARDING);
        }

        yield return new WaitUntil(() => tokens.Count <= amount);

        foreach (DisplayToken displayToken in tokens)
        {
            displayToken.SetTokenInteractMode(TokenInteractMode.NONE);
        }

        isDiscarding = false;
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
    private List<DisplayToken> tokens = new List<DisplayToken>();
    private bool isDiscarding;

    private void OnTokenInteract(DisplayToken displayToken)
    {
        if (isDiscarding)
        {
            displayToken.InstantDestroy();
            int index = tokens.IndexOf(displayToken);
            tokens.Remove(displayToken);
            OnTokenDiscard?.Invoke(index);
        }
    }
}
