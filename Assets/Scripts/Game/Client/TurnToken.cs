using System.Collections;
using TMPro;
using UnityEngine;

public class TurnToken : MonoBehaviour
{
    public IEnumerator DoFlipAnimation(bool isFirst, float totalTime, float height, int flipCount)
    {
        Vector3 bottomPos = transform.position;
        Vector3 topPos = bottomPos + Vector3.up * height;
        Vector3 endRotation = isFirst ? Vector3.zero : new Vector3(180, 0, 0);

        token.transform.position = bottomPos;
        token.transform.eulerAngles = Vector3.zero;

        float time = 0f;
        while (time < totalTime)
        {
            time += Time.deltaTime;
            float t = time / totalTime;

            // Lerp smoothly (sin(0.5 * PI) = 1)
            float heightT = Mathf.Sin(t * Mathf.PI);
            Vector3 position = Vector3.Lerp(bottomPos, topPos, heightT);

            // Add additional x rotation to the rotation lerp
            float rotationT = t * flipCount * 360;
            Vector3 rotation = Vector3.Lerp(Vector3.zero, endRotation, t);
            rotation.x += rotationT;

            token.transform.position = position;
            token.transform.eulerAngles = rotation;

            yield return null;
        }

        token.transform.position = bottomPos;
        token.transform.eulerAngles = endRotation;
    }

    public IEnumerator DoChangeAnimation(float totalTime)
    {
        float time = 0f;
        while (time < totalTime)
        {
            time += Time.deltaTime;
            float t = time / totalTime;

            float rotationT = t * 180;
            token.transform.eulerAngles = new Vector3(0, 0, rotationT);

            if (t > 0.5f)
            {
                topText.text = "X";
                bottomText.text = "X";
            }

            yield return null;
        }
    }

    [Header("References")]
    [SerializeField] private GameObject token;
    [SerializeField] private TextMeshProUGUI topText;
    [SerializeField] private TextMeshProUGUI bottomText;
}
