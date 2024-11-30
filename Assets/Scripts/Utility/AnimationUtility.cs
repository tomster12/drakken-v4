using System.Collections;
using UnityEngine;

public static class AnimationUtility
{
    public static IEnumerator StartAfterDuration(float duration, System.Action action)
    {
        yield return new WaitForSeconds(duration);
        action();
    }

    public static IEnumerator AnimateFromBagToPosition(Transform target, Vector3 bagPos, Vector3 targetPos, float liftHeight, float totalTime)
    {
        // Move up and out of the bag by lift height, then move over towards targetPos
        float targetStartPct = 0.2f;

        float time = 0f;
        while (time < totalTime)
        {
            time += Time.deltaTime;
            float pct = time / totalTime;

            float liftPct = Mathf.Sin(pct * Mathf.PI);
            Vector3 liftOffset = Vector3.up * (liftHeight * liftPct);
            float targetPct = Mathf.Min(Mathf.Max((pct - targetStartPct) / (1.0f - targetStartPct), 0.0f), 1.0f);
            Vector3 movePos = Vector3.Lerp(bagPos, targetPos, targetPct) + liftOffset;
            target.position = movePos;

            yield return null;
        }

        target.position = targetPos;
    }
}
