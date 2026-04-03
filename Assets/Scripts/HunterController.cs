using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class HunterController : Agent
{
    // Agent    
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float turnSpeed = 180f;

    // Environment  
    public GameObject plane;

    private Rigidbody rb;

    [SerializeField] private PreyController prey;
    private float previousDistanceToPrey;
    [SerializeField] private float distanceToPreyRewardScale = 0.002f;
    [SerializeField] private float reverseMovePenaltyScale = 0.002f;

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody>();
    }

    public override void OnEpisodeBegin()
    {
        // Hunter
        transform.SetLocalPositionAndRotation(new Vector3(Random.Range(-9f, 9f), 0.5f, Random.Range(-9f, 9f)), Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
        previousDistanceToPrey = Vector3.Distance(transform.localPosition, prey.transform.localPosition);
        rb.linearVelocity = Vector3.zero;

    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(transform.localPosition);

        if (prey != null)
        {
            sensor.AddObservation(transform.InverseTransformPoint(prey.transform.position));
            sensor.AddObservation(prey.FruitCollectionProgress);
        }
        else
        {
            sensor.AddObservation(Vector3.zero);
            sensor.AddObservation(0f);
        }
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

        if (moveForward < 0f)
        {
            AddReward(moveForward * reverseMovePenaltyScale);
        }

        var currentDistanceToPrey = Vector3.Distance(transform.localPosition, prey.transform.localPosition);
        var distanceChange = previousDistanceToPrey - currentDistanceToPrey;
        AddReward(distanceChange * distanceToPreyRewardScale);
        previousDistanceToPrey = currentDistanceToPrey;

    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActionsOut = actionsOut.ContinuousActions;
        continuousActionsOut[0] = Input.GetAxisRaw("Horizontal");
        continuousActionsOut[1] = Input.GetAxisRaw("Vertical");
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Wall"))
        {
            AddReward(-0.1f);
        }
        if (collision.gameObject.CompareTag("Predator"))
        {
            AddReward(1f);
            prey.AddReward(-1f);
            plane.GetComponent<Renderer>().material.color = Color.softYellow;
            EndEpisode();
            prey.EndEpisode();
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("Wall")) {
            AddReward(-0.01f);
        }
    }
}
