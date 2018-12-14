using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlammableTrait : MonoBehaviour {
    public bool isBurning;
    public float burntime;
    [Range(0f, 1f)]
    public float randomChanceToLightSomethingOnFire;
    public float burnRadius;
    public float burnCheckTime;
    private float burnCheckTimer;
    public ParticleSystem burningParticleEffect;
	// Use this for initialization
	void Start () {
        burnCheckTimer = burnCheckTime;
	}
	
	// Update is called once per frame
	void Update () {
        if (isBurning)
        {
            //Check if the thing is hit with a water trait
            if (CheckForWater())
            {
                PutOutFire();
                return;
            }
            //TODO: Make this check if it can be lit on fire rather then lighting on fire
            //After a defined time, check if something burnable exists
            burnCheckTimer -= Time.deltaTime;
            if(burnCheckTimer <= 0)
            {
                //Random check to see if the fire spreads
                if(Random.Range(0f, 1f) < randomChanceToLightSomethingOnFire)
                {
                    RaycastHit[] hits = Physics.SphereCastAll(this.transform.position, burnRadius, Vector3.up, 0);
                    if(hits.Length > 0)
                    {
                        foreach(RaycastHit hit in hits)
                        {
                            if (hit.transform.gameObject.Equals(this.gameObject))
                            {
                                continue;
                            }
                            if(Random.Range(0f, 1f) < randomChanceToLightSomethingOnFire)   //TODO: Make this a seperate chance
                            {
                                FlammableTrait trait = hit.transform.GetComponent<FlammableTrait>();
                                if (trait)
                                {
                                    trait.LightFire();
                                }

                            }
                        }
                    }
                }
                burnCheckTimer = burnCheckTime;
            }
            burntime -= Time.deltaTime;
            if(burntime <= 0)
            {
                PutOutFire();
                EdibleTrait trait = GetComponent<EdibleTrait>();
                if (trait)
                {
                    Destroy(trait);  //.enabled = false;
                }
                //Destroy the fire trait to indicate that the thing isn't flammable anymore
                Destroy(this, 1);
                GetComponent<MeshRenderer>().materials[0].color = Color.grey;
                //TODO: Sizzle out effect
            }
        }
	}

    public void LightFire()
    {
        isBurning = true;
        GetComponent<MeshRenderer>().materials[0].color = Color.red;
        burningParticleEffect.Play(true);
    }

    public void PutOutFire()
    {
        //TODO: Make the resulting color of the object be infunction of the amount burned
        isBurning = false;
        GetComponent<MeshRenderer>().materials[0].color = Color.yellow;
        burningParticleEffect.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }
    
    public bool CheckForWater()
    {
        Collider[] res = Physics.OverlapBox(this.transform.position, Vector3.one, Quaternion.identity);
        List<WaterTrait> traits = new List<WaterTrait>();
        foreach (Collider coll in res)
        {
            WaterTrait waterTrait = coll.transform.GetComponent<WaterTrait>();
            if (waterTrait)
            {
                traits.Add(waterTrait);
            }
        }
        return traits.Count > 0;
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(this.transform.position, burnRadius);
    }
}
