using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class ParticleManager : MonoBehaviour
{
    public static ParticleManager Instance { get; private set; }

    public void SpawnPoof(Vector3 position)
    {
        GameObject poofObject = Instantiate(poofPrefab, position, Quaternion.identity);
        ParticleSystem poof = poofObject.GetComponent<ParticleSystem>();
        activeParticleSystems.Add(poof);
    }

    [Header("References")]
    [SerializeField] private GameObject poofPrefab;

    private List<ParticleSystem> activeParticleSystems = new List<ParticleSystem>();

    private void Awake()
    {
        Assert.IsNull(Instance);
        Instance = this;
    }

    private void Update()
    {
        for (int i = activeParticleSystems.Count - 1; i >= 0; i--)
        {
            if (!activeParticleSystems[i].IsAlive())
            {
                Destroy(activeParticleSystems[i].gameObject);
                activeParticleSystems.RemoveAt(i);
            }
        }
    }
}
