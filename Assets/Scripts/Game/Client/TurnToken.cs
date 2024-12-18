using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

public class TurnToken : MonoBehaviour
{
    public async Task DoFlipAnimation(CancellationToken ctoken, bool isFirst, float totalTime, float height, int flipCount)
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
            float t = Mathf.Clamp01(time / totalTime);

            // Lerp up and down smoothly (sin(0.5 * PI) = 1)
            float heightT = Mathf.Sin(t * Mathf.PI);
            Vector3 position = Vector3.Lerp(bottomPos, topPos, heightT);

            // Lerp towards the end rotation with additional x-axis rotation
            float rotationT = t * flipCount * 360;
            Vector3 rotation = Vector3.Lerp(Vector3.zero, endRotation, t);
            rotation.x += rotationT;

            token.transform.position = position;
            token.transform.eulerAngles = rotation;

            await Task.Yield();
            ctoken.ThrowIfCancellationRequested();
        }

        token.transform.position = bottomPos;
        token.transform.eulerAngles = endRotation;
    }

    public async Task DoChangeAnimation(CancellationToken ctoken, float totalTime)
    {
        float time = 0f;
        while (time < totalTime)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / totalTime);

            // Flip the token 180 and change the text halfway through
            float rotationT = t * 180;
            token.transform.eulerAngles = new Vector3(0, 0, rotationT);
            if (t > 0.5f)
            {
                topText.text = "X";
                bottomText.text = "X";
            }

            await Task.Yield();
            ctoken.ThrowIfCancellationRequested();
        }
    }

    [Header("References")]
    [SerializeField] private GameObject token;
    [SerializeField] private TextMeshProUGUI topText;
    [SerializeField] private TextMeshProUGUI bottomText;
}
