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

    public async Task DoChangeAndPlaceAnimation(CancellationToken ctoken, float totalTime, GameBoard initialBoard)
    {
        // Setup variables
        Vector3 startPos = transform.position;
        Vector3 upPos = transform.position + Vector3.up * 2.5f;
        float hoverUpPctEnd = 0.5f;
        float flipPctStart = 0.2f;
        float flipPctEnd = 0.4f;
        float hoverDownPctStart = 0.55f;

        // Hover upwards, flip to X, hover to the board location
        float time = 0f;
        while (time < totalTime)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / totalTime);

            // Hover upwards
            if (t < hoverUpPctEnd)
            {
                float upPct = Mathf.Clamp01(t / hoverUpPctEnd);
                float easedPct = Easing.EaseOutBack(upPct);
                transform.position = startPos + (upPos - startPos) * easedPct;
            }

            // Flip over and change text
            if (t > flipPctStart && t < flipPctEnd)
            {
                float flipPct = Mathf.Clamp01((t - flipPctStart) / (flipPctEnd - flipPctStart));
                float rotationT = flipPct * 180;
                token.transform.eulerAngles = new Vector3(0, 0, rotationT);

                if (flipPct > 0.65f)
                {
                    topText.text = "X";
                    bottomText.text = "X";
                }
            }

            // Hover to the board location
            if (t > hoverDownPctStart)
            {
                float downPct = Mathf.Clamp01((t - hoverDownPctStart) / (1.0f - hoverDownPctStart));
                float easedPct = Easing.EaseInOutCubic(downPct);
                transform.position = upPos + (initialBoard.GetTurnTokenPosition() - upPos) * easedPct;
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
