using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]
public class PreyController : Agent
{
    // Fruit
    public int fruitCount = 5;
    public GameObject fruitPrefab;
    [SerializeField] private List<GameObject> fruits = new();
    [SerializeField] public float fruitPadding = 5f;

    // Agent    
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float turnSpeed = 180f;

    // Environment
    public GameObject plane;
    public Transform EnvironmentLocation;

    private Rigidbody rb;

    [SerializeField] private HunterController hunter;
    private int fruitsCollectedThisEpisode;
    private float previousDistanceToNearestFruit;
    [SerializeField] private float fruitCollectReward = 0.5f;
    [SerializeField] private float fruitCompletionReward = 1f;
    [SerializeField] private float hunterPenaltyFromFruitScale = 0.5f;
    [SerializeField] private float fruitMoveAwayPenaltyScale = 0.003f;
    [SerializeField] private float fruitDistanceDeadZone = 0.01f;
    [SerializeField] private float turnPenaltyScale = 0.0003f;

    public float FruitCollectionProgress
    {
        get
        {
            if (fruitCount <= 0)
            {
                return 0f;
            }

            return Mathf.Clamp01((float)fruitsCollectedThisEpisode / fruitCount);
        }
    }

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody>();
    }

    public override void OnEpisodeBegin()
    {
        // Agent
        transform.SetLocalPositionAndRotation(new Vector3(Random.Range(-9f, 9f), 0.5f, Random.Range(-9f, 9f)), Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
        fruitsCollectedThisEpisode = 0;
        rb.linearVelocity = Vector3.zero;

        CleanupFruits();
        CreateFruit();
        previousDistanceToNearestFruit = GetDistanceToNearestFruit();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(transform.localPosition);
        sensor.AddObservation(transform.InverseTransformPoint(hunter.transform.position));

        var nearestFruit = GetNearestFruit();
        if (nearestFruit != null)
        {
            sensor.AddObservation(transform.InverseTransformPoint(nearestFruit.transform.position));
        }
        else
        {
            sensor.AddObservation(Vector3.zero);
        }
    }

    public GameObject GetNearestFruit()
    {
        GameObject nearestFruit = null;
        float nearestDistance = float.MaxValue;

        foreach (var fruit in fruits)
        {
            if (fruit == null)
            {
                continue;
            }

            float distance = Vector3.Distance(transform.localPosition, fruit.transform.localPosition);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestFruit = fruit;
            }
        }

        if (nearestFruit != null)
        {
            return nearestFruit;
        }
        else
        {
            return null;
        }
    }

    private float GetDistanceToNearestFruit()
    {
        GameObject nearestFruit = GetNearestFruit();
        if (nearestFruit == null)
        {
            return 0f;
        }

        return Vector3.Distance(transform.localPosition, nearestFruit.transform.localPosition);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float moveRotate = actions.ContinuousActions[0];
        float moveForward = actions.ContinuousActions[1];

        float moveAmount = moveForward * moveSpeed * Time.fixedDeltaTime;
        float rotateAmount = moveRotate * turnSpeed * Time.fixedDeltaTime;

        rb.MovePosition(rb.position + transform.forward * moveAmount);
        rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, rotateAmount, 0f));

        AddReward(-0.001f);
        AddReward(-Mathf.Abs(moveRotate) * turnPenaltyScale);

        float currentDistanceToNearestFruit = GetDistanceToNearestFruit();
        if (currentDistanceToNearestFruit > 0f && previousDistanceToNearestFruit > 0f)
        {
            float distanceDelta = currentDistanceToNearestFruit - previousDistanceToNearestFruit;
            if (distanceDelta > fruitDistanceDeadZone)
            {
                AddReward(-distanceDelta * fruitMoveAwayPenaltyScale);
            }
        }

        previousDistanceToNearestFruit = currentDistanceToNearestFruit;
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActionsOut = actionsOut.ContinuousActions;
        continuousActionsOut[0] = Input.GetAxisRaw("Horizontal");
        continuousActionsOut[1] = Input.GetAxisRaw("Vertical");
    }

    private void CreateFruit()
    {
        const int maxPlacementAttempts = 10;

        for (int i = 0; i < fruitCount; i++)
        {
            GameObject fruit = Instantiate(fruitPrefab, EnvironmentLocation);
            bool placed = false;

            for (int attempt = 0; attempt < maxPlacementAttempts; attempt++)
            {
                fruit.transform.localPosition = new Vector3(Random.Range(-9f, 9f), fruitPrefab.transform.localScale.y / 2, Random.Range(-9f, 9f));

                placed = true;

                if (!CheckOverlap(fruit.transform.localPosition, transform.localPosition, fruitPadding))
                {
                    placed = false;
                    continue;
                }

                if (hunter != null && !CheckOverlap(fruit.transform.localPosition, hunter.transform.localPosition, fruitPadding))
                {
                    placed = false;
                    continue;
                }

                for (int j = 0; j < fruits.Count; j++)
                {
                    if (fruits[j] == null)
                    {
                        continue;
                    }

                    if (!CheckOverlap(fruit.transform.localPosition, fruits[j].transform.localPosition, fruitPadding))
                    {
                        placed = false;
                        break;
                    }
                }

                if (placed)
                {
                    break;
                }
            }

            fruits.Add(fruit);
        }
    }

    private void CleanupFruits()
    {
        for (int i = fruits.Count - 1; i >= 0; i--)
        {
            if (fruits[i] != null)
            {
                Destroy(fruits[i]);
            }
        }

        fruits.Clear();
    }

    public bool CheckOverlap(Vector3 objectPosition, Vector3 existingObjectPosition, float minDistancePadding)
    {
        float distanceBetweenObjects = Vector3.Distance(objectPosition, existingObjectPosition);
        
        if (minDistancePadding <= distanceBetweenObjects)
        {
            return true;
        }
        return false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Fruit"))
        {
            fruitsCollectedThisEpisode++;
            float fruitReward = fruitCollectReward;

            AddReward(fruitReward);
            if (hunter != null)
            {
                hunter.AddReward(-fruitReward * hunterPenaltyFromFruitScale);
            }

            fruits.Remove(other.gameObject);
            Destroy(other.gameObject);
            previousDistanceToNearestFruit = GetDistanceToNearestFruit();

            if (fruits.Count == 0)
            {
                AddReward(fruitCompletionReward);
                if (hunter != null)
                {
                    hunter.AddReward(-fruitCompletionReward);
                }

                plane.GetComponent<Renderer>().material.color = Color.softGreen;
                EndEpisode();
                if (hunter != null)
                {
                    hunter.EndEpisode();
                }
            }
        }
    }

    // Stronger penalty for colliding with walls to encourage better navigation
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Wall"))
        {
            AddReward(-0.1f);
        }
    }

    // Additional small penalty for staying in contact with walls to discourage hugging them
    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("Wall")) {
            AddReward(-0.01f);
        }
    }
}