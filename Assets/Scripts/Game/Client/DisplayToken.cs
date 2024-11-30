using TMPro;
using UnityEngine;

public class DisplayToken : MonoBehaviour
{
    public void SetTokenID(string tokenID)
    {
        tokenData = TokenManager.Instance.GetTokenData(tokenID);
        tokenIDText.text = tokenData.ID;
    }

    public void SetHighlighted(bool highlighted)
    {
        meshRenderer.material = highlighted ? highlightedMaterial : baseMaterial;
    }

    [Header("References")]
    [SerializeField] private TextMeshProUGUI tokenIDText;
    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private Material baseMaterial;
    [SerializeField] private Material highlightedMaterial;

    private TokenData tokenData;
}
