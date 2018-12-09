using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class GridCell
{
    public Vector3 position;
    public bool isBlocked;

    public GridCell(Vector3 pos, bool isBlocked)
    {
        position = pos;
        this.isBlocked = isBlocked;
    }

    internal void SetBlocked(bool value)
    {
        isBlocked = value;
    }

    public override string ToString()
    {
        return "The cell is " + (isBlocked?"is blocked": "is free") + " and positioned at " + position;
    }
}