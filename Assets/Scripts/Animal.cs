using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Basic template for the animals
/// </summary>
public class Animal : MonoBehaviour
{
    public eWolfStates currentState;
    protected Rigidbody rb;
    protected Transform randomMovementObject;
    [Header("View variables")]
    public float viewDistance;
    public float viewConeSize;

    [Header("Movement inside circle")]
    public float idleMovementDistanceSize;

    protected virtual void Start()
    {
        randomMovementObject = new GameObject("RandomPosition").GetComponent<Transform>();
    }
    protected virtual void Update()
    {
    }

    protected Vector3 GetRandomPointInsideCircle()
    {
        Vector3 pos = Random.insideUnitCircle * idleMovementDistanceSize;
        pos = new Vector3(pos.x, 0, pos.y);
        randomMovementObject.transform.position = pos;
        return pos;
    }

    protected RaycastHit[] GetThingsInView()
    {
        return Physics.BoxCastAll(this.transform.position, Vector3.one * viewConeSize, this.transform.forward, this.transform.localRotation, viewDistance);
    }
    
    protected void MoveToTarget(Vector3 target, float speed, float turnSpeed)
    {
        //Move the wolf to the next idle point
        Vector3 direction = (target - this.transform.position).normalized;
        Vector3 newPosition = Vector3.Lerp(this.transform.position, this.transform.position + direction * speed, Time.deltaTime);
        rb.MovePosition(newPosition);

        // Turn the wolf to face the forward direction
        //Look target direction is the Vector3 direction
        Quaternion rot = Quaternion.Lerp(this.transform.localRotation, Quaternion.LookRotation(direction, Vector3.up), turnSpeed);
        rb.MoveRotation(rot);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(Vector3.zero, idleMovementDistanceSize);
    }
}
