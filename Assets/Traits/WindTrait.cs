using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WindTrait : MonoBehaviour {
    public Vector3 direction;
    public float force;

    public Vector3 GetWindVector()
    {
        return direction.normalized * force;
    }
}
