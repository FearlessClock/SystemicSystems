using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

class Helper
{
    public static float DistanceToVector(Vector3 start, Vector3 end)
    {
        float dis = (Mathf.Sqrt(Mathf.Pow(start.x - end.x, 2) + 0
                        + Mathf.Pow(start.z - end.z, 2)));
        return dis;
    }
}