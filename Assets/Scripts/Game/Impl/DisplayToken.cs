using System;
using TMPro;
using UnityEngine;

public partial class DisplayToken : MonoBehaviour, IInteractable
{
    public Action<DisplayToken, bool> OnHoveredChange { get; set; }
    public Action<DisplayToken> OnInteract { get; set; }
    public TokenInstance TokenInstance { get; private set; }
    public bool AtTargetPos => Vector3.Distance(transform.position, targetPos) < 0.01f;
    public bool AtTargetRot => Quaternion.Angle(transform.rotation, targetRot) < 0.01f;

    public bool IsHighlighted
    {
        get => outline.enabled;
        set => outline.enabled = value;
    }

    public void Initialize(TokenInstance tokenInstance)
    {
        TokenInstance = tokenInstance;

        if (TokenInstance != null)
        {
            tokenData = TokenManager.Instance.GetTokenData(tokenInstance.tokenID);
            tokenIDText.text = tokenData.ID;
        }
    }

    public void SetTarget(Vector3 targetPos, Quaternion targetRot, float posLerp = 0.1f, float rotLerp = 0.1f)
    {
        hasTarget = true;
        if (posLerp <= 0.0f) transform.position = targetPos;
        if (rotLerp <= 0.0f) transform.rotation = targetRot;
        this.targetPos = targetPos;
        this.targetRot = targetRot;
        targetPosLerp = posLerp;
        targetRotLerp = posLerp;
    }

    public void ClearTarget()
    {
        hasTarget = false;
    }

    public void Destroy()
    {
        Destroy(gameObject);
    }

    [Header("References")]
    [SerializeField] private TextMeshProUGUI tokenIDText;
    [SerializeField] private Outline outline;

    private TokenData tokenData = null;
    private bool isHovered = false;
    private bool hasTarget = false;
    private Vector3 targetPos;
    private Quaternion targetRot;
    private float targetPosLerp = 0.1f;
    private float targetRotLerp = 0.1f;

    private void Update()
    {
        if (hasTarget)
        {
            if (!AtTargetPos) transform.position = Vector3.Lerp(transform.position, targetPos, targetPosLerp);
            if (!AtTargetRot) transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, targetRotLerp);
        }
    }
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
            OnHoveredChange?.Invoke(this, isHovered);
        }
    }

    public void Interact()
    {
        OnInteract?.Invoke(this);
    }
}
