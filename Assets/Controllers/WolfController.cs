using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum eWolfStates { Idle, Hungry, Attacking, Eating, Scared}
[Serializable]
public class StateVariables
{
    public float speed;
    [Range(0f, 1f)]
    public float turnSpeed;
    public Transform target;
    public float distanceToTarget;
}
[SelectionBase]
public class WolfController : Animal {

    public eWolfStates currentState;
    private List<GameObject> scaredOfObjects;

    [Header("Wolf stats")]
    public float health;
    public float maxHunger;
    public float currentHunger;
    public float hungerThreshold;
    public float maxThirst;
    public float currentThirst;
    public float thirstThreshold;
    public float maxTired;
    public float currentTired;
    public float tiredThreshold;

    [Header("Idle state variables")]
    public StateVariables IdleStateVars;
    public Vector3 nextIdleTarget;

    [Header("Attack state variables")]
    public StateVariables attackStateVars;
    public float wolfDamage;

    [Header("Eating state variables")]
    public StateVariables eatingStateVars;

    [Header("Scared state variables")]
    public float scaredSpeed;
    public float scaredTurnSpeed;
    public float minimumScaredDistance;

    [Header("Hungry state variables")]
    public StateVariables hungryStateVars;
    private bool foundFood;


    // Use this for initialization
    protected override void Start() {
        base.Start();
        nextIdleTarget = GetRandomPointInsideCircle();
        rb = GetComponent<Rigidbody>();
        scaredOfObjects = new List<GameObject>();
    }

    // Update is called once per frame
    protected override void Update() {
        base.Update();
        //TODO: Make the wolves hear what is around it
        //TODO: Decide if this shouldn't be in just certain states
        //Check to see if there are scary things around the wolf to make it flee
        if (currentState != eWolfStates.Scared && CheckScaryThingsAround().Length > 0)
        {
            SetStateToScared();
        }
        //TODO: Make the wolf use energy depending on what it is doing
        UseFoodEnergy();
        switch (currentState)
        {
            case eWolfStates.Idle:
                IdlingState();
                break;
            case eWolfStates.Attacking:
                AttackingState();
                break;
            case eWolfStates.Eating:
                EatingState();
                break;
            case eWolfStates.Scared:
                ScaredState();
                break;
            case eWolfStates.Hungry:
                HungryState();
                break;
            default:
                break;
        }
    }

    #region Scared State
    /// <summary>
    /// Check if any scary Things are around
    /// </summary>
    internal GameObject[] CheckScaryThingsAround()
    {
        List<GameObject> scaryThings = new List<GameObject>();
        if(scaredOfObjects.Count > 0)
        {
            //Check all the scary things from last time
            foreach (GameObject scaredObject in scaredOfObjects)
            {
                if (Helper.DistanceToVector(this.transform.position, scaredObject.transform.position) < minimumScaredDistance)
                {
                    scaryThings.Add(scaredObject);
                }
            }
            scaredOfObjects = scaryThings;
            //scaredOfObjects.Clear();
            RaycastHit[] surroundingObjects = GetThingsInView();

            foreach (RaycastHit hit in surroundingObjects)
            {
                if (!scaredOfObjects.Contains(hit.collider.gameObject))
                {
                    FlammableTrait flame = IsGOBurning(hit.collider.gameObject);
                    if (flame)
                    {
                        scaredOfObjects.Add(flame.gameObject);
                    }
                }
                //Other scary traits for a wolf
            }
        }
        else
        {
            //scaredOfObjects.Clear();
            RaycastHit[] surroundingObjects = GetThingsInView();

            foreach (RaycastHit hit in surroundingObjects)
            {
                FlammableTrait flame = IsGOBurning(hit.collider.gameObject);
                if (flame)
                {
                    scaredOfObjects.Add(flame.gameObject);
                }
                //Other scary traits for a wolf
            }
        }

        return scaredOfObjects.ToArray();
    }

    private FlammableTrait IsGOBurning(GameObject go)
    {
        FlammableTrait flammableTrait = go.GetComponent<FlammableTrait>();
        if (flammableTrait)
        {
            if (flammableTrait.isBurning)
            {
                return flammableTrait;
            }
        }
        return null;
    }

    private void SetStateToScared()
    {
        currentState = eWolfStates.Scared;
        //GetComponentInChildren<MeshRenderer>().materials[0].color = Color.green;
    }

    private void ScaredState()
    {
        //TODO: Make sure to run away from the closest enemy
        //      Use the distance as a weight to make the animal move more away from the closet threat
        CheckScaryThingsAround();
        Vector3 direction = new Vector3();
        if(scaredOfObjects.Count == 0)
        {
            SetStateToIdle();
        }
        foreach (GameObject scaredObject in scaredOfObjects)
        {
            float distance = Helper.DistanceToVector(this.transform.position, scaredObject.transform.position);
            direction += (scaredObject.transform.position - this.transform.position).normalized * (1 - (distance/ viewDistance));
        }
        direction /= scaredOfObjects.Count;
        direction.Normalize();
        MoveToTarget(this.transform.position + direction, scaredSpeed, scaredTurnSpeed);
    }
    private void UseFoodEnergy()
    {
        currentHunger -= 0.01f;
        if(currentHunger < 0)
        {
            currentHunger = 0;
        }
    }
    #endregion

    #region Idle State
    private void SetStateToIdle()
    {
        currentState = eWolfStates.Idle;

        //GetComponentInChildren<MeshRenderer>().materials[0].color = Color.white;
        nextIdleTarget = GetRandomPointInsideCircle();
    }

    private void IdlingState()
    {
        MoveToTarget(nextIdleTarget, IdleStateVars.speed, IdleStateVars.turnSpeed);
        //Look around to see what the wolf can see
        //TODO: Make the wolf see with a cone
        RaycastHit[] hits = GetThingsInView();
        if (currentHunger < hungerThreshold)
        {
            SetStateToHungry(hits);
        }

        if (Helper.DistanceToVector(this.transform.position, nextIdleTarget) < IdleStateVars.distanceToTarget)
        {
            nextIdleTarget = GetRandomPointInsideCircle();
        }
    }
    #endregion

    #region Hungry state
    private void SetStateToHungry(RaycastHit[] hits)
    {
        currentState = eWolfStates.Hungry;

        //GetComponentInChildren<MeshRenderer>().materials[0].color = Color.yellow;
        if (!FindFood(hits))
        {
            GetRandomPointInsideCircle();
            hungryStateVars.target = randomMovementObject.transform;
        }
    }

    private bool FindFood(RaycastHit[] hits)
    {
        EdibleTrait[] edibleTraits = GetEdibleTraits(hits);
        if (edibleTraits.Length > 0)
        {
            SetStateToEating(edibleTraits[UnityEngine.Random.Range(0, edibleTraits.Length)]);
            return true;
        }
        else
        {
            AttackableTrait[] attackableTraits = GetAllAliveAttackableFoodTraits(hits);
            if (attackableTraits.Length > 0)
            {
                //TODO: Make this choice more intelligent (Closer, weaker, more food worty)
                AttackableTrait attackableTrait = attackableTraits[UnityEngine.Random.Range(0, attackableTraits.Length)];
                SetStateToAttacking(attackableTrait);
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Hungry state focuses on finding food
    /// The wolf will walk around randomly until it finds food then change to the state corresponding to the foodSource
    /// </summary>
    private void HungryState()
    {
        //Set the state back to idle if the creature has eaten enough
        //TODO: Make the animal stop when it has eaten max only when he has something to eat, otherwise at a lower level
        if (currentHunger >= maxHunger)
        {
            currentHunger = maxHunger;
            SetStateToIdle();
        }

        //See if there is anything that we can target around then find if it is edible or attackable then edible
        RaycastHit[] hits = GetThingsInView();
        if (FindFood(hits))
        {
            return;
        }

        if (Helper.DistanceToVector(this.transform.position, hungryStateVars.target.transform.position) < hungryStateVars.distanceToTarget)
        {
            GetRandomPointInsideCircle();
            hungryStateVars.target = randomMovementObject.transform;
        }

        //The hungry target would be a idle walk thing
        if (hungryStateVars.target != null)
        {
            Vector3 targetVector = (hungryStateVars.target.transform.position - this.transform.position).normalized;
            MoveToTarget(this.transform.position + targetVector, hungryStateVars.speed, hungryStateVars.turnSpeed);
        }
    }
    #endregion
    /// <summary>
    /// Gets all the food things that are alive, edible, attackable, not a wolf and not self
    /// </summary>
    /// <param name="hits"></param>
    /// <returns></returns>
    private AttackableTrait[] GetAllAliveAttackableFoodTraits(RaycastHit[] hits)
    {
        List<AttackableTrait> attackingTraits = new List<AttackableTrait>();
        foreach (RaycastHit hit in hits)
        {
            if (ReferenceEquals(hit.transform.gameObject, this.gameObject))
            {
                continue;
            }
            //Is the thing edible?
            EdibleTrait edibleTrait = hit.transform.GetComponent<EdibleTrait>();
            if (edibleTrait)
            {
                // wolves won't eat wolves
                WolfController wolfController = hit.transform.GetComponent<WolfController>();
                if (!wolfController)
                {
                    // They aren't vegetarien
                    MeatTrait meatTrait = hit.transform.GetComponent<MeatTrait>();
                    if (meatTrait)
                    {
                        AttackableTrait attackableTrait = hit.transform.GetComponent<AttackableTrait>();
                        if (attackableTrait)
                        {
                            attackingTraits.Add(attackableTrait);
                        }
                    }
                }
            }
        }
        return attackingTraits.ToArray();
    }

    private EdibleTrait[] GetEdibleTraits(RaycastHit[] hits)
    {
        List<EdibleTrait> edibleTraits = new List<EdibleTrait>();
        foreach (RaycastHit hit in hits)
        {
            if (ReferenceEquals(hit.transform.gameObject, this.gameObject))
            {
                continue;
            }
            //Is the thing edible?
            EdibleTrait edibleTrait = hit.transform.GetComponent<EdibleTrait>();
            if (edibleTrait)
            {
                // wolves won't eat wolves
                WolfController wolfController = hit.transform.GetComponent<WolfController>();
                if (!wolfController)
                {
                    // They aren't vegetarien
                    MeatTrait meatTrait = hit.transform.GetComponent<MeatTrait>();
                    if (meatTrait)
                    {
                        HealthTrait healthTrait = hit.transform.GetComponent<HealthTrait>();
                        if(healthTrait && !healthTrait.isAlive)
                        {
                            edibleTraits.Add(edibleTrait);
                        }
                    }
                }
            }
        }
        return edibleTraits.ToArray();
    }

    #region Attacking state
    private void SetStateToAttacking(AttackableTrait thingToAttack)
    {
        currentState = eWolfStates.Attacking;

        //GetComponentInChildren<MeshRenderer>().materials[0].color = Color.red;

        attackStateVars.target = thingToAttack.transform;
    }

    private void AttackingState()
    {
        if(attackStateVars.target != null)
        {
            Vector3 targetPosition = (attackStateVars.target.position - this.transform.position).normalized;
            MoveToTarget(this.transform.position + targetPosition, attackStateVars.speed, attackStateVars.turnSpeed);
            if(Helper.DistanceToVector(this.transform.position, attackStateVars.target.transform.position) < attackStateVars.distanceToTarget)
            {
                //TODO: Attack the thing if need be
                HealthTrait healthTrait = attackStateVars.target.GetComponent<HealthTrait>();
                if (healthTrait)
                {
                    if (healthTrait.isAlive)
                    {
                        attackStateVars.target.GetComponent<AttackableTrait>().Attack(wolfDamage);
                    }
                    else
                    {
                        SetStateToEating(attackStateVars.target.GetComponent<EdibleTrait>());
                    }
                }
                else
                {
                    attackStateVars.target = null;
                }
            }
        }
        else
        {
            SetStateToHungry(GetThingsInView());
        }
    }
    #endregion

    #region Eating state
    private void SetStateToEating(EdibleTrait trait)
    {
        currentState = eWolfStates.Eating;

        //GetComponentInChildren<MeshRenderer>().materials[0].color = Color.black;
        eatingStateVars.target = trait.transform;
    }

    private void EatingState()
    {
        //TODO: Maybe check if the wolf is close enough to the 
        //TODO: Use a coroutine to make the wolf eat for a while rather than golping everything down one shot
        if(eatingStateVars.target != null)
        {
            currentHunger += eatingStateVars.target.GetComponent<EdibleTrait>().GetFoodValue();
            if (currentHunger >= maxHunger)
            {
                SetStateToIdle();
            }
        }
        else
        {
            SetStateToIdle();
        }
    }
    #endregion
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(nextIdleTarget, 0.4f);

        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(this.transform.position, Vector3.one * viewConeSize);
        Gizmos.DrawLine(this.transform.position, this.transform.position + this.transform.forward * viewDistance + this.transform.forward * viewConeSize);
    }
}
