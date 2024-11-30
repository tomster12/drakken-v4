using System.Collections;
using UnityEngine;

public class TurnToken : MonoBehaviour
{
    public IEnumerator FlipAnimationEnum(bool isFirst, float waitTime = 0.5f)
    {
        Vector3 bottomPos = transform.position;
        Vector3 topPos = bottomPos + Vector3.up * 6.5f;
        Vector3 endRotation = isFirst ? Vector3.zero : new Vector3(180, 0, 0);
        float totalTime = 1f;
        int flipCount = 2;

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

        yield return new WaitForSeconds(waitTime);
        Destroy(transform.gameObject);
    }

    [Header("References")]
    [SerializeField] private GameObject token;
}
