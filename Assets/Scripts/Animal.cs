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

    /// <summary>
    /// Movement using a navmesh and A*
    /// </summary>
    private NavMeshGenerator navMeshInstance;
    protected Stack<GridCell> path;
    private Vector3 nextTargetPoint = Vector3.zero;
    [Header("NavMesh navigation")]
    public float minDistanceToNode;
    private bool getNewPath = true;

    protected virtual void Start()
    {
        randomMovementObject = new GameObject("RandomPosition").GetComponent<Transform>();
        path = new Stack<GridCell>();
        navMeshInstance = NavMeshGenerator.instance;
    }
    protected virtual void Update()
    {
    }

    protected void GetPathToPoint(Vector3 target)
    {
        Debug.Log(this.transform.position + " " + target);
        path = navMeshInstance.GetPathBetweenTwoPoints(this.transform.position, target);
    }

    protected void GetNextPointTarget()
    {
        if(path.Count > 0)
        {
            nextTargetPoint = path.Pop().position;
        }
    }

    protected void RandomMoveOnNavMeshPath(float speed, float turnSpeed)
    {
        if(path == null)
        {
            GetPathToPoint(GetRandomPointInsideCircle());
        }
        else if(path != null)
        {
            float dis = Helper.DistanceToVector(this.transform.position, nextTargetPoint);

            if (dis < minDistanceToNode)
            {
                // If there are no nodes left, get a new path
                if (path.Count == 0)
                {
                    GetPathToPoint(GetRandomPointInsideCircle());
                }

                //If there are nodes, get the next point to go to
                if (path != null && path.Count > 0)
                {
                    GetNextPointTarget();
                }
            }
            else
            {
                if (path.Count >= 0)
                {
                    MoveToTarget(nextTargetPoint, speed, turnSpeed);
                }
            }
        }
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
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(Vector3.zero, idleMovementDistanceSize);
    }
}
