using System;
using TMPro;
using UnityEngine;

public partial class DisplayToken : MonoBehaviour, IInteractable
{
    public Action<DisplayToken> OnInteract { get; set; }
    public TokenInstance TokenInstance { get; private set; }

    public void SetTokenInstance(TokenInstance tokenInstance)
    {
        TokenInstance = tokenInstance;

        if (TokenInstance != null)
        {
            tokenData = TokenManager.Instance.GetTokenData(tokenInstance.tokenID);
            tokenIDText.text = tokenData.ID;
        }

        interactMode = TokenInteractMode.NONE;
    }

    public void SetTokenInteractMode(TokenInteractMode interactMode)
    {
        this.interactMode = interactMode;
        if (!CanInteract) outline.enabled = false;
    }

    public void InstantDestroy()
    {
        Destroy(gameObject);
    }

    [Header("References")]
    [SerializeField] private TextMeshProUGUI tokenIDText;
    [SerializeField] private Outline outline;

    private TokenData tokenData;
    private TokenInteractMode interactMode;
    private bool CanInteract => interactMode != TokenInteractMode.NONE;
}

// IInteractable
public partial class DisplayToken : MonoBehaviour, IInteractable
{
    public void SetHovered(bool isHovered)
    {
        if (!CanInteract) return;
        if (!outline) return;
        outline.enabled = isHovered;
    }

    public void Interact()
    {
        if (!CanInteract) return;
        OnInteract?.Invoke(this);
    }
}
