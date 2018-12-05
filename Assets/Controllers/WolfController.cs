using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum eWolfStates { Idle, Attacking, Eating}

public class WolfController : MonoBehaviour {

    public eWolfStates currentState;
    private Rigidbody rb;
    [Header("View variables")]
    public float viewDistance;
    public float viewConeSize;

    [Header("Movement variables")]
    public float idleSpeed;
    [Range(0f, 1f)]
    public float idleTurnSpeed;

    [Header("Idle state variables")]
    public Transform topLeft;
    public Transform bottomRight;
    public Vector3 nextIdleTarget;
    public float distanceToTarget;


    private Vector3 GetRandomPointInsideSquare()
    {
        Vector3 pos = new Vector3(Random.Range(topLeft.position.x, bottomRight.position.x), 0, Random.Range(topLeft.position.z, bottomRight.position.z));
        return pos;
    }

	// Use this for initialization
	void Start () {
        nextIdleTarget = GetRandomPointInsideSquare();
        rb = GetComponent<Rigidbody>();
    }
	
	// Update is called once per frame
	void Update () {
        switch (currentState)
        {
            case eWolfStates.Idle:
                //Move the wolf to the next idle point
                Vector3 direction = (nextIdleTarget - this.transform.position).normalized;
                Vector3 newPosition = Vector3.Lerp(this.transform.position, this.transform.position + direction * idleSpeed, Time.deltaTime);
                rb.MovePosition(newPosition);

                // Turn the wolf to face the forward direction
                //Look target direction is the Vector3 direction
                Quaternion rot = Quaternion.Lerp(this.transform.localRotation, Quaternion.LookRotation(direction, Vector3.up), idleTurnSpeed);
                rb.MoveRotation(rot);

                //Look around to see what the wolf can see
                //TODO: Make the wolf see a cone
                RaycastHit[] hits = Physics.BoxCastAll(this.transform.position, Vector3.one * viewConeSize, direction, rot, viewDistance);
                if(hits.Length > 0)
                {
                    Debug.Log("I can see something");

                }

                if(Helper.DistanceToVector(this.transform.position, nextIdleTarget) < distanceToTarget)
                {
                    nextIdleTarget = GetRandomPointInsideSquare();
                }
                break;
            case eWolfStates.Attacking:
                break;
            case eWolfStates.Eating:
                break;
            default:
                break;
        }
	}

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(nextIdleTarget, 0.4f);

        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(this.transform.position, Vector3.one * viewConeSize);
        Gizmos.DrawLine(this.transform.position, this.transform.position + this.transform.forward * viewDistance);
    }
}
