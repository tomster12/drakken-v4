using System;
using TMPro;
using UnityEngine;

public partial class DisplayToken : MonoBehaviour, IInteractable
{
    public Action<DisplayToken, bool> OnHoveredChanged { get; set; }
    public Action<DisplayToken> OnInteract { get; set; }
    public TokenInstance TokenInstance { get; private set; }

    public void Initialize(TokenInstance tokenInstance)
    {
        TokenInstance = tokenInstance;

        if (TokenInstance != null)
        {
            tokenData = TokenManager.Instance.GetTokenData(tokenInstance.tokenID);
            tokenIDText.text = tokenData.ID;
        }
    }

    public void Destroy()
    {
        Destroy(gameObject);
    }

    public void SetHighlighted(bool isHighlighted)
    {
        outline.enabled = isHighlighted;
    }

    [Header("References")]
    [SerializeField] private TextMeshProUGUI tokenIDText;
    [SerializeField] private Outline outline;

    private TokenData tokenData;
    private bool isHovered;
}

// IInteractable
public partial class DisplayToken : MonoBehaviour, IInteractable
{
    public void SetHovered(bool isHovered)
    {
        if (this == null) return;

        if (isHovered != this.isHovered)
        {
            this.isHovered = isHovered;
            OnHoveredChanged?.Invoke(this, isHovered);
        }
    }

    public void Interact()
    {
        OnInteract?.Invoke(this);
    }
}
