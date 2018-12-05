using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum SheepStates { Idle, Hungry, Scared }
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
    Vector3 moveTo = Vector3.zero;

    [Header("State stuff")]
    public SheepStates startingState;
    private Stack<SheepStates> currentState;

    [Header("Hungry state vars")]
    public float hungerLevel;
    public float foodConsumeRate;
    public float maxFoodLevel;
    public float cantTakeItAnymoreHungerLevel;
    private bool foundFood;
    public float eatDistance;
    private EdibleTrait targetEdibleTrait;

    [Header("Scared state vars")]
    public float minDistanceToScaredObj;
    private List<GameObject> scaredOfObjects;

    // Use this for initialization
    void Start () {
        rb = this.GetComponent<Rigidbody>();
        GetNewTargetVector();
        targetExists = true;
        foundFood = false;
        targetEdibleTrait = null;
        currentState = new Stack<SheepStates>();
        currentState.Push(startingState);

        scaredOfObjects = new List<GameObject>();

        maxFoodLevel += UnityEngine.Random.Range(-5f, 5f);
        hungerLevel += UnityEngine.Random.Range(-1f, 1f);
        foodConsumeRate += UnityEngine.Random.Range(-0.005f, 0.005f);
        cantTakeItAnymoreHungerLevel = maxFoodLevel / 2;
    }
	
	// Update is called once per frame
	void Update () {

        //TODO: Make this happen less or from what the sheep is doing
        ConsumeFood();
        startingState = currentState.Peek();    //Debug, remove when done
        SheepStates currentSheepState = currentState.Peek();
        //Check to see if there are scary things around the sheep to make it flee
        if (currentSheepState != SheepStates.Scared && CheckScaryThingsAround().Length > 0)
        {
            SetStateToScared();
        }

        switch (currentSheepState)
        {
            case SheepStates.Idle:
                GetComponent<MeshRenderer>().materials[0].color = Color.blue;
                IdleStateFunction();
                break;
            case SheepStates.Hungry:
                GetComponent<MeshRenderer>().materials[0].color = Color.red;
                HungryStateFunction();
                break;
            case SheepStates.Scared:
                GetComponent<MeshRenderer>().materials[0].color = Color.green;
                ScaredStateFunction();
                break;
            default:
                break;
        }
    }
    /// <summary>
    /// Check if any scary Things are around
    /// </summary>
    private GameObject[] CheckScaryThingsAround()
    {
        scaredOfObjects.Clear();
        RaycastHit[] surroundingObjects = GetVisibleObjects();

        //TODO: Make sure to run away from the closest enemy
        foreach (RaycastHit hit in surroundingObjects)
        {
            FlammableTrait flammableTrait = hit.transform.GetComponent<FlammableTrait>();
            if (flammableTrait)
            {
                if (flammableTrait.isBurning)
                {
                    scaredOfObjects.Add(flammableTrait.gameObject);
                }
                continue;
            }

            //Other scary traits for a sheep (Like wolves, coming soon)
        }

        return scaredOfObjects.ToArray();
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

        if (!targetExists || Helper.DistanceToVector(this.transform.position, target) < minDistanceToTarget)
        {
            GetNewTargetVector();
        }
        MoveWhileAvoidingThingsSheep();
    }

    private void SetStateToHungry()
    {
        currentState.Push(SheepStates.Hungry);
        targetExists = false;
        foundFood = false;
    }

    private void HungryStateFunction()
    {
        if (hungerLevel <= maxFoodLevel)
        {
            if (foundFood)
            {
                if(Helper.DistanceToVector(this.transform.position, target) < eatDistance)
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
                    MoveToPoint(foundFood);
                }
            }
            else
            {
                RaycastHit[] hits = GetVisibleObjects();
                List<EdibleTrait> traitHits = new List<EdibleTrait>();
                //TODO: This is quite ineffective if there is only one element
                foreach (RaycastHit hit in hits)
                {
                    EdibleTrait trait = hit.transform.GetComponent<EdibleTrait>();
                    if (trait)
                    {
                        traitHits.Add(trait);
                    }
                }

                //Get the closest edible thing
                if (traitHits.Count == 1)
                {
                    targetEdibleTrait = traitHits[0];
                    target = hits[0].transform.position;
                    targetExists = true;
                    foundFood = true;
                }
                else if (traitHits.Count > 0)
                {
                    float closestDistance = viewRadius * 2; //Make the  min more then it could possibly be
                    GameObject closetObject = null;
                    foreach (EdibleTrait trait in traitHits)
                    {
                        if (trait)
                        {
                            float dis = Vector3.Distance(this.transform.position, trait.gameObject.transform.position);
                            if (dis < closestDistance)
                            {
                                closestDistance = dis;
                                closetObject = trait.gameObject;
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
                        }
                        else
                        {
                            foundFood = false;
                        }
                    }
                    else
                    {
                        if (foundFood && Vector3.Distance(this.transform.position, target) < minDistanceToTarget || !foundFood)
                        {
                            GetNewTargetVector();
                        }
                        MoveWhileAvoidingThingsSheep();
                    }
                }
                else if(traitHits.Count == 0)
                {
                    if (targetExists && Helper.DistanceToVector(this.transform.position, target) < minDistanceToTarget || !targetExists)
                    {
                        GetNewTargetVector();
                    }
                    MoveWhileAvoidingThingsSheep();
                }
                MoveToPoint(foundFood);
            }
        }
        else
        {
            targetEdibleTrait = null;
            currentState.Pop();
        }
    }

    private void SetStateToScared()
    {
        currentState.Push(SheepStates.Scared);
    }

    private bool IsScaredOfObj(GameObject obj)
    {
        if (obj.GetComponent<FlammableTrait>().isBurning)
        {
            return true;
        }
        return false;
    }

    private void ScaredStateFunction()
    {
        CheckScaryThingsAround();
        Vector3 scaredOfDirection = Vector3.zero;
        foreach (GameObject scaryThing in scaredOfObjects)
        {
            Vector3 toTheObj = this.transform.position - scaryThing.transform.position;
            toTheObj = toTheObj.normalized;
            scaredOfDirection += toTheObj;
        }

        target = this.transform.position + scaredOfDirection;
        MoveToPoint(scaredOfObjects.Count > 0);

        if (scaredOfObjects.Count == 0)
        {
            currentState.Pop();
        }
    }

    public void MoveWhileAvoidingThingsSheep()
    {
        //Make the sheep avoid other objects in the scene
        //TODO: Avoidance counts itself
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
            moveTo.y = 0;
            Vector3 newPosition = this.transform.position + moveTo;
            
            Vector3 origin = this.transform.position + this.transform.forward + moveTo.normalized / 2;
            bool movementHits = Physics.CheckBox(origin, moveTo.normalized);
            if (movementHits)
            {
                targetExists = false;
            }
            else
            {
                rb.MovePosition(newPosition);

                rb.MoveRotation(Quaternion.LookRotation(moveTo.normalized, Vector3.up));
            }
        }
    }

    public void MoveToPoint(bool targetExistance)
    {
        if (targetExistance)
        {
            Vector3 moveToPoint = target - this.transform.position;
            moveToPoint.Normalize();
            moveToPoint.y = 0;
            rb.MovePosition(this.transform.position + moveToPoint * speed * Time.deltaTime);
        }
    }

    public void GetNewTargetVector()
    {
        Vector3 newTarget = this.transform.position + UnityEngine.Random.insideUnitSphere*5;
        targetExists = true;
        target = newTarget;
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

