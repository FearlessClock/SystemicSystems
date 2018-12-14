using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum SheepStates { Idle, Hungry, Scared }
public class SheepController : Animal {
    //TODO: Make the sheep flee from fire
    public float viewRadius;
    HealthTrait healthTrait;
    public bool isAlive;

    [Header("Movement")]
    public float speed;
    [Range(0f, 1f)]
    public float turnSpeed;
    public float avoidanceAmount;
    public Vector3 target;
    private bool targetExists;
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
    protected override void Start () {
        base.Start();
        rb = this.GetComponent<Rigidbody>();
        targetExists = false;
        foundFood = false;
        targetEdibleTrait = null;
        currentState = new Stack<SheepStates>();
        currentState.Push(startingState);

        scaredOfObjects = new List<GameObject>();

        maxFoodLevel += UnityEngine.Random.Range(-5f, 5f);
        hungerLevel += UnityEngine.Random.Range(-1f, 1f);
        foodConsumeRate += UnityEngine.Random.Range(-0.005f, 0.005f);
        cantTakeItAnymoreHungerLevel = maxFoodLevel / 2;

        healthTrait = GetComponent<HealthTrait>();
        isAlive = true;
        healthTrait.thingDied.AddListener(() => { Debug.Log("The sheep died");  isAlive = false; });
    }

    // Update is called once per frame
    protected override void Update () {
        if (!isAlive)
        {
            this.GetComponent<MeshRenderer>().materials[0].color = Color.grey;
            this.enabled = false;
        }
        base.Update();
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
            WolfController wolfController = hit.transform.GetComponent<WolfController>();
            if (wolfController)
            {
                scaredOfObjects.Add(wolfController.gameObject);
                continue;
            }

            //Other scary traits for a sheep (Like wolves, coming soon)
        }

        return scaredOfObjects.ToArray();
    }

    private void ConsumeFood()
    {
        hungerLevel -= foodConsumeRate;
        if(hungerLevel < 0)
        {
            hungerLevel = 0;
        }
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
        //MoveWhileAvoidingThingsSheep();
        RandomMoveOnNavMeshPath(speed, 1);
    }

    private void SetStateToHungry()
    {
        currentState.Push(SheepStates.Hungry);
        targetExists = false;
        foundFood = false;
    }

    private void HungryStateFunction()
    {
        // If the sheep is hungry, get food!
        if (hungerLevel <= maxFoodLevel)
        {
            if (foundFood)
            {
                // If the sheep has reached the end of the path, walk straight to the target and eat
                if(MoveOnPath(speed, turnSpeed))
                {
                    if (Helper.DistanceToVector(this.transform.position, target) < eatDistance)
                    {
                        if (targetEdibleTrait != null)
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
                        MoveToTarget(target, speed, turnSpeed);
                    }
                }
            }
            else
            {
                // Look for edible things in the enviroment.

                RaycastHit[] hits = GetVisibleObjects();
                List<EdibleTrait> traitHits = new List<EdibleTrait>();
                //TODO: This is quite ineffective if there is only one element
                foreach (RaycastHit hit in hits)
                {
                    if(ReferenceEquals(hit.transform.gameObject, this.gameObject))
                    {
                        continue;
                    }
                    EdibleTrait edibleTrait = hit.transform.GetComponent<EdibleTrait>();
                    PlantTrait plantTrait = hit.transform.GetComponent<PlantTrait>();
                    if (edibleTrait && plantTrait)
                    {
                        traitHits.Add(edibleTrait);
                    }
                }
                
                if (traitHits.Count == 0)
                {
                    // If the sheep is close to its target or if it doesn't have one, get a new target
                    if (MoveOnPath(speed, turnSpeed) || !targetExists)
                    {
                        GetNewTargetVector();
                    }
                }
                //Get the closest edible thing
                else if (traitHits.Count == 1)
                {
                    targetEdibleTrait = traitHits[0];
                    target = hits[0].transform.position;
                    targetExists = true;
                    foundFood = true;
                }
                else if (traitHits.Count > 0)
                {
                    float closestDistance = viewRadius * 2; //Make the  min more then it could possibly be
                    GameObject closetObject = traitHits[0].gameObject; //default the closest object to something 
                    // Get the closest edible thing
                    foreach (EdibleTrait trait in traitHits)
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
                    // If I found food, make it the target
                    target = closetObject.transform.position;
                }
                // Either the sheep will be going to eat some food or it will roam
                targetExists = !MoveOnPath(speed, turnSpeed);
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
    [Obsolete("MoveToPoint is deprecated, please use MoveToTarget from Animal instead.")]
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
        SetPathToPoint(target);
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
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(Vector3.zero, idleMovementDistanceSize);

        if (Application.isPlaying && path != null)
        {
            foreach (GridCell cell in path)
            {
                Gizmos.DrawSphere(cell.position, 0.4f);
            }
        }
    }
}

