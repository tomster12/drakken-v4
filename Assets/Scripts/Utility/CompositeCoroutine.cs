using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CompositeCoroutine
{
    public CompositeCoroutine(MonoBehaviour mb)
    {
        this.mb = mb;
    }

    public Coroutine StartCouroutine(IEnumerator enumerable)
    {
        Coroutine cr = mb.StartCoroutine(enumerable);
        coroutines.Add(cr);
        return cr;
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
