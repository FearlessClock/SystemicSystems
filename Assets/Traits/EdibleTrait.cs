using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EdibleTrait : MonoBehaviour {
    public float foodValue;
    public float amountOfFood;

    private void Update()
    {
        if (amountOfFood < 0)
        {
            if (this.gameObject != null)
            {
                Destroy(this.gameObject);
            }
        }
    }


    public float GetFoodValue()
    {
        amountOfFood -= foodValue;
        
        return foodValue;
    }
}
