using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public static class AnimationUtility
{
    public static async Task DelayTask(CancellationToken ctoken, int delay, Func<Task> taskFunc)
    {
        await Task.Delay(delay, ctoken);
        await taskFunc();
    }

    public static async Task AnimatePosToPosWithLift(
        CancellationToken ctoken,
        Transform target,
        Vector3 start,
        Vector3 end,
        float liftHeight,
        float totalTime,
        float moveStartPct,
        float moveEndPct,
        Func<float, float> easingFunction = null)
    {
        easingFunction ??= Easing.Identity;

        float time = 0f;
        while (time < totalTime)
        {
            time += Time.deltaTime;
            float pct = Mathf.Clamp01(time / totalTime);

            float liftPct = Mathf.Sin(pct * Mathf.PI);
            Vector3 liftOffset = Vector3.up * (liftHeight * liftPct);

            float targetPct = Mathf.Clamp01((pct - moveStartPct) / (moveEndPct - moveStartPct));
            Vector3 movePos = Vector3.Lerp(start, end, targetPct) + liftOffset;
            target.position = movePos;

            await Task.Yield();
            ctoken.ThrowIfCancellationRequested();
        }

        target.position = end;
    }

    public static async Task AnimatePosToPosWithEasing(
        CancellationToken ctoken,
        Transform target,
        Vector3 start,
        Vector3 end,
        float totalTime,
        Func<float, float> easingFunction = null)
    {
        easingFunction ??= Easing.Identity;

        float time = 0f;
        while (time < totalTime)
        {
            time += Time.deltaTime;
            float pct = Mathf.Clamp01(time / totalTime);
            float easedPct = easingFunction(pct);
            target.position = start + (end - start) * easedPct;

            await Task.Yield();
            ctoken.ThrowIfCancellationRequested();
        }

        target.position = end;
    }
}
