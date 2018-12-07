using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class HealthTrait : MonoBehaviour {
    public float maxHealth;
    public float currentHealth;
    public bool isAlive;
    public UnityEvent thingDied;
	
    public void ReduceHealth(float amount)
    {
        currentHealth -= amount;
        if(currentHealth <= 0)
        {
            isAlive = false;
            thingDied.Invoke();
            Debug.Log(this.ToString());
        }
    }

    public override string ToString()
    {
        return "Health trait: IsAlive: " + isAlive;
    }
}
