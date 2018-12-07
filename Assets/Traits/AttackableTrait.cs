using UnityEngine;
using System.Collections;
using UnityEngine.Events;

[System.Serializable]
public class FloatEvent : UnityEvent<float>
{
}
public class AttackableTrait : MonoBehaviour
{
    public FloatEvent reduceHealthDelegate;
    // Use this for initialization
    public void Attack(float value)
    {
        reduceHealthDelegate.Invoke(value);
    }
}
