using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum SheepStates { Idle, Hungry }
public class SheepController : MonoBehaviour {
    //TODO: Make the sheep flee from fire
    public float viewRadius;

    [Header("Movement")]
    public float speed;
    public float avoidanceAmount;
    public Vector3 target;
    private bool targetExists;
    Rigidbody rb;
    public float minDistanceToTarget;
    public float collisionAvoidCheckDistance;

    [Header("State stuff")]
    private Stack<SheepStates> currentState;
    public SheepStates startingState;

    [Header("Hungry state vars")]
    public float hungerLevel;
    public float foodConsumeRate;
    public float maxFoodLevel;
    public float cantTakeItAnymoreHungerLevel;
    private bool foundFood;
    public float eatDistance;
    private EdibleTrait targetEdibleTrait;

    // Use this for initialization
    void Start () {
        rb = this.GetComponent<Rigidbody>();
        GetNewTargetVector();
        targetExists = true;
        foundFood = false;
        targetEdibleTrait = null;
        currentState = new Stack<SheepStates>();
        currentState.Push(startingState);

        maxFoodLevel += Random.Range(-5f, 5f);
        hungerLevel += Random.Range(-1f, 1f);
        foodConsumeRate += Random.Range(-0.005f, 0.005f);
        cantTakeItAnymoreHungerLevel = maxFoodLevel / 2;
    }
	
	// Update is called once per frame
	void Update () {
        //TODO: Make this happen less or from what the sheep is doing
        ConsumeFood();
        startingState = currentState.Peek();    //Debug, remove when done
        switch (currentState.Peek())
        {
            case SheepStates.Idle:
                GetComponent<MeshRenderer>().materials[0].color = Color.blue;
                IdleStateFunction();
                break;
            case SheepStates.Hungry:
                GetComponent<MeshRenderer>().materials[0].color = Color.red;
                HungryStateFunction();
                break;
            default:
                break;
        }
    }

    private void ConsumeFood()
    {
        hungerLevel -= foodConsumeRate;
    }

    private RaycastHit[] GetVisibleObjects()
    {
        //TODO: View cone in front of the sheep to
        return Physics.SphereCastAll(this.transform.position, viewRadius, Vector3.up, 0);
    }

    private void IdleStateFunction()
    {

        if (hungerLevel < cantTakeItAnymoreHungerLevel)
        {
            SetStateToHungry();
        }

        if (!targetExists || Vector3.Distance(this.transform.position, target) < minDistanceToTarget)
        {
            GetNewTargetVector();
        }
        MoveSheep();
    }

    private void SetStateToHungry()
    {
        currentState.Push(SheepStates.Hungry);
        
        RaycastHit[] hits = GetVisibleObjects();
        if (hits.Length > 0)
        {
            //Get the closest edible object
            float closestDistance = viewRadius * 2; //Make the min more then it could possibly be
            GameObject closetObject = null;
            foreach (RaycastHit hit in hits)
            {
                EdibleTrait trait = hit.transform.gameObject.GetComponent<EdibleTrait>();
                if (trait)
                {
                    float dis = Vector3.Distance(this.transform.position, hit.transform.position);
                    if (dis < closestDistance)
                    {
                        closestDistance = dis;
                        closetObject = hit.transform.gameObject;
                        targetEdibleTrait = trait;
                        foundFood = true;
                    }
                }
            }
            if (foundFood)
            {
                if(closetObject != null)
                {
                    target = closetObject.transform.position;
                    target.y = this.transform.position.y;
                    targetExists = true;
                }
                else
                {
                    foundFood = false;
                }
            }
        }
    }

    private void HungryStateFunction()
    {
        if (hungerLevel <= maxFoodLevel)
        {
            if (foundFood)
            {
                if(targetExists && DistanceToVector(target) < eatDistance)
                {
                    if(targetEdibleTrait != null)
                    {
                        //TODO: Add cooldown to eating
                        hungerLevel += targetEdibleTrait.GetFoodValue();
                    }
                    else
                    {
                        foundFood = false;
                    }
                }
                else
                {
                    MoveSheep();
                }
            }
            else
            {
                RaycastHit[] hits = GetVisibleObjects();
                //Get the closest edible thing
                if (hits.Length == 1)
                {
                    EdibleTrait trait = hits[0].transform.gameObject.GetComponent<EdibleTrait>();
                    targetEdibleTrait = trait;
                    foundFood = true;
                }
                else if (hits.Length > 0)
                {
                    float closestDistance = viewRadius * 2; //Make the  min more then it could possibly be
                    GameObject closetObject = null;
                    foreach (RaycastHit hit in hits)
                    {
                        EdibleTrait trait = hit.transform.gameObject.GetComponent<EdibleTrait>();
                        if (trait)
                        {
                            float dis = Vector3.Distance(this.transform.position, hit.transform.position);
                            if (dis < closestDistance)
                            {
                                closestDistance = dis;
                                closetObject = hit.transform.gameObject;
                                targetEdibleTrait = trait;
                                foundFood = true;
                            }
                        }
                    }
                    if (foundFood)
                    {
                        if (closetObject != null)
                        {
                            target = closetObject.transform.position;
                            target.y = this.transform.position.y;
                        }
                        else
                        {
                            foundFood = false;
                        }
                    }
                    else
                    {
                        if (targetExists && Vector3.Distance(this.transform.position, target) < minDistanceToTarget || !targetExists)
                        {
                            GetNewTargetVector();
                        }
                        MoveSheep();
                    }
                }
            }
        }
        else
        {
            targetEdibleTrait = null;
            currentState.Pop();
        }
    }

    Vector3 moveTo = Vector3.zero;
    public void MoveSheep()
    {
        //Make the sheep avoid other objects in the scene
        RaycastHit[] hits = Physics.SphereCastAll(new Ray(this.transform.position, Vector3.up), collisionAvoidCheckDistance, 0);
        Vector3 avoidance = Vector3.zero;
        if (hits.Length > 0)
        {
            //Create a vector that points away from all objects around it
            foreach (RaycastHit hit in hits)
            {
                avoidance += (this.transform.position - hit.transform.position).normalized;
            }
            avoidance.y = this.transform.position.y;
        }
        if (targetExists)
        {
            Vector3 targetMovement = (target - this.transform.position).normalized * speed + avoidance * avoidanceAmount;
            targetMovement.Normalize();
            moveTo = (targetMovement * speed * Time.deltaTime);
            Vector3 newPosition = this.transform.position + moveTo;
            newPosition.y = this.transform.position.y;
            Vector3 origin = this.transform.position + this.transform.forward + moveTo.normalized / 2;
            bool movementHits = Physics.CheckBox(origin, moveTo.normalized);
            if (movementHits)
            {
                Debug.Log("You hit a wall!");
                targetExists = false;
            }
            else
            {
                rb.MovePosition(newPosition);

                //rb.MoveRotation(Quaternion.LookRotation(moveTo.normalized, Vector3.up));
            }
        }
    }

    public void GetNewTargetVector()
    {
        Vector3 newTarget = this.transform.position + Random.insideUnitSphere*5;
        targetExists = true;
        target = newTarget;
    }

    private float DistanceToVector(Vector3 definedTarget)
    {
        return Vector3.Distance(this.transform.position, definedTarget);
    }
    
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.transform.CompareTag("Solid"))
        {
            GetNewTargetVector();
        }
    }

    private void OnDrawGizmosSelected()
    {

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(this.transform.position, eatDistance);
        
        Vector3 origin = this.transform.position + this.transform.forward * (this.transform.localScale.x / 2 - moveTo.magnitude/2f);
        Gizmos.color = Color.red;
        Gizmos.DrawLine(origin, origin + moveTo * 5);
    }
}

