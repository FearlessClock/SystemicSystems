using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class GridCell
{
    public Vector3 position;
    public bool isBlocked;
    public float weight;

    //A star variables
    public float gScore;
    public float hScore;
    public float fScore;
    public GridCell parent;

    public GridCell(Vector3 pos, bool isBlocked)
    {
        gScore = 0;
        hScore = 0;
        fScore = 0;
        parent = null;
        position = pos;
        this.isBlocked = isBlocked;
        weight = 1;
    }

    public GridCell(GridCell cell)
    {
        gScore = cell.gScore;
        hScore = cell.hScore;
        fScore = cell.fScore;
        parent = cell.parent;
        position = cell.position;
        this.isBlocked = cell.isBlocked;
        weight = cell.weight;
    }

    internal void SetBlocked(bool value)
    {
        isBlocked = value;
    }

    public override string ToString()
    {
        return fScore + " fscore and the cell is " + (isBlocked?"is blocked": "is free") + " and positioned at " + position;
    }

    internal void Reset()
    {
        gScore = 0;
        hScore = 0;
        fScore = 0;
        parent = null;
    }
}