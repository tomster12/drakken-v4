using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CompositeCoroutine
{
    public CompositeCoroutine(MonoBehaviour mb)
    {
        this.mb = mb;
    }

    public CompositeCoroutine StartCouroutine(IEnumerator enumerable)
    {
        coroutines.Add(mb.StartCoroutine(enumerable));
        return this;
    }

    public void Stop()
    {
        foreach (var coroutine in coroutines)
        {
            mb.StopCoroutine(coroutine);
        }
    }

    private MonoBehaviour mb;
    private List<Coroutine> coroutines = new List<Coroutine>();
}
