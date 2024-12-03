using System;
using System.Collections;
using UnityEngine;

public static class AnimationUtility
{
    public static IEnumerator StartAfterDuration(float duration, IEnumerator action)
    {
        yield return new WaitForSeconds(duration);
        yield return action;
    }

    public static IEnumerator AnimatePosToPosWithLift(
        Transform target,
        Vector3 start,
        Vector3 end,
        float liftHeight,
        float totalTime,
        float moveStartPct = 0.2f,
        float moveEndPct = 1.0f)
    {
        float time = 0f;
        while (time < totalTime)
        {
            time += Time.deltaTime;
            float pct = time / totalTime;

            float liftPct = Mathf.Sin(pct * Mathf.PI);
            Vector3 liftOffset = Vector3.up * (liftHeight * liftPct);

            float targetPct = (pct - moveStartPct) / (moveEndPct - moveStartPct);
            targetPct = Mathf.Min(Mathf.Max(targetPct, 0.0f), 1.0f);
            Vector3 movePos = Vector3.Lerp(start, end, targetPct) + liftOffset;
            target.position = movePos;

            yield return null;
        }

        target.position = end;
    }

    public static IEnumerator AnimatePosToPosWithEasing(
        Transform target,
        Vector3 start,
        Vector3 end,
        float totalTime,
        Func<float, float> easingFunction)
    {
        float time = 0f;
        while (time < totalTime)
        {
            time += Time.deltaTime;
            float pct = time / totalTime;
            float easedPct = easingFunction(pct);
            target.position = start + (end - start) * easedPct;
            yield return null;
        }

        target.position = end;
    }
}
