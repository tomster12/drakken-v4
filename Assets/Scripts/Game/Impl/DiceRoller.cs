using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;

public class RolledDice
{
    public Transform tf;
    public Rigidbody rb;
    public Collider cl;
    public DiceData diceData;
    public List<Vector3> positions;
    public List<Quaternion> rotations;
    public List<Quaternion> correctedRotations;
}

public class DiceRoller : MonoBehaviour
{
    [ContextMenu("Roll Debug Dice")]
    public void RollDice()
    {
        _ = RollDice(dbgDiceData);
    }

    [ContextMenu("Clear")]
    public void Clear()
    {
        while (transform.childCount > 0) DestroyImmediate(transform.GetChild(0).gameObject);
    }

    public async Task RollDice(DiceData[] diceData)
    {
        Physics.simulationMode = SimulationMode.Script;

        // Instantiate new set of dice
        Clear();
        rolledDice = new RolledDice[diceData.Length];
        for (int i = 0; i < diceData.Length; i++)
        {
            int x = i % gridWidth;
            int y = gridOffsetY - i / gridWidth;
            Vector3 initialPosition = startTransform.position + new Vector3(x * gridSize - gridSize * (gridWidth - 1) / 2, 0, y * gridSize);
            Quaternion initialRotation = Quaternion.Euler(Random.Range(0, 360), Random.Range(0, 360), Random.Range(0, 360));

            GameObject dice = Instantiate(dicePrefab, initialPosition, initialRotation);
            dice.transform.SetParent(transform);

            rolledDice[i] = new RolledDice
            {
                tf = dice.transform,
                rb = dice.GetComponent<Rigidbody>(),
                cl = dice.GetComponent<BoxCollider>(),
                diceData = diceData[i],
                positions = new List<Vector3>() { initialPosition },
                rotations = new List<Quaternion>() { initialRotation },
            };

            Vector3 forceDirection = (targetTransform.position - dice.transform.position).normalized;
            rolledDice[i].rb.isKinematic = false;
            rolledDice[i].rb.AddForce(forceDirection * rollForceStrength, ForceMode.VelocityChange);
            rolledDice[i].rb.AddTorque(Random.insideUnitSphere * rollRotationStrength, ForceMode.VelocityChange);
        }

        // Simulate dice until they stop
        bool allStopped = false;
        for (frameCount = 1; !allStopped; frameCount++)
        {
            Physics.Simulate(Time.deltaTime);
            allStopped = true;
            foreach (var rolledDice in rolledDice)
            {
                rolledDice.positions.Add(rolledDice.rb.transform.position);
                rolledDice.rotations.Add(rolledDice.rb.transform.rotation);
                if (rolledDice.rb.linearVelocity.magnitude > 0.01f || rolledDice.rb.angularVelocity.magnitude > 0.01f) allStopped = false;
            }

            if (frameCount > 5000)
            {
                Debug.LogWarning("Dice roll simulation took too long, breaking out of loop");
                break;
            }
        }

        // Delete physics off of dice and apply corrections to rotations
        foreach (var rolledDice in rolledDice)
        {
            rolledDice.rb.isKinematic = true;
            DestroyImmediate(rolledDice.rb);
            DestroyImmediate(rolledDice.cl);
            ApplyCorrectionRotation(rolledDice);
        }

        // Re-enable physics then start coroutine to replay dice
        Physics.simulationMode = SimulationMode.FixedUpdate;

        if (skipAnimation)
        {
            foreach (var rolledDice in rolledDice)
            {
                rolledDice.tf.SetPositionAndRotation(rolledDice.positions[frameCount - 1], rolledDice.correctedRotations[frameCount - 1]);
            }
        }
        else await ReplayDice();
    }

    [Header("References")]
    [SerializeField] private GameObject dicePrefab;

    [Header("Config")]
    [SerializeField] private int gridWidth = 4;
    [SerializeField] private int gridOffsetY = 1;
    [SerializeField] private float gridSize = 1.0f;
    [SerializeField] private float rollForceStrength = 4.0f;
    [SerializeField] private float rollRotationStrength = 4.0f;
    [SerializeField] private Transform startTransform;
    [SerializeField] private Transform targetTransform;

    [Header("Debug")]
    [SerializeField] private DiceData[] dbgDiceData;
    [SerializeField] private bool skipAnimation;

    private RolledDice[] rolledDice;
    private int frameCount = 0;

    private static Vector3 GetFaceNormal(int faceValue)
    {
        return faceValue switch
        {
            1 => Vector3.down,
            2 => Vector3.forward,
            3 => Vector3.right,
            4 => Vector3.left,
            5 => Vector3.back,
            6 => Vector3.up,
            _ => Vector3.zero,
        };
    }

    private static int GetDiceValue(Transform tf)
    {
        Vector3 localUp = tf.InverseTransformDirection(Vector3.up);
        float maxDot = float.MinValue;
        int faceValue = 1;

        for (int i = 1; i <= 6; i++)
        {
            Vector3 faceNormal = GetFaceNormal(i);
            float dot = Vector3.Dot(localUp, faceNormal);
            if (dot > maxDot)
            {
                maxDot = dot;
                faceValue = i;
            }
        }

        return faceValue;
    }

    private static Quaternion GetCorrectionRotation(RolledDice rolledDice)
    {
        int actualValue = GetDiceValue(rolledDice.tf);
        if (actualValue == rolledDice.diceData.Value) return Quaternion.identity;

        Vector3 actualNormal = GetFaceNormal(actualValue);
        Vector3 targetNormal = GetFaceNormal(rolledDice.diceData.Value);
        return Quaternion.FromToRotation(targetNormal, actualNormal);
    }

    private static void ApplyCorrectionRotation(RolledDice rolledDice)
    {
        Quaternion correction = GetCorrectionRotation(rolledDice);

        rolledDice.correctedRotations = new();
        for (int i = 0; i < rolledDice.rotations.Count; i++)
        {
            rolledDice.correctedRotations.Add(rolledDice.rotations[i] * correction);
        }
    }

    private async Task ReplayDice()
    {
        for (int i = 0; i < frameCount; i++)
        {
            for (int j = 0; j < rolledDice.Length; j++)
            {
                rolledDice[j].tf.SetPositionAndRotation(rolledDice[j].positions[i], rolledDice[j].correctedRotations[i]);
            }

            await Task.Yield();
        }
    }
}
