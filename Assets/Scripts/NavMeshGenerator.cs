using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class NavMeshGenerator : MonoBehaviour {
    public static NavMeshGenerator instance;

    public int gridX;
    public int gridY;

    //TODO: Make a gridstepsize different to 1 work
    public float gridStepSize;
    private GridCell[,] grid;

    public bool runningCheck = false;
    public int nmbrOfChecks;

    public GameObject sheep1;
    public GameObject sheep2;

    public LayerMask solidLayerMask;

    private Vector3 lastCalculatedTarget;

    private void Awake()
    {
        if(instance != null)
        {
            Destroy(this);
        }
        else
        {
            instance = this;
        }
    }

    void Start() {
        grid = new GridCell[gridX, gridY];
        for (int i = 0; i < grid.GetLength(0); i++)
        {
            for (int j = 0; j < grid.GetLength(1); j++)
            {
                if (Physics.CheckBox(this.transform.position + new Vector3(i * gridStepSize, 0, j * gridStepSize), Vector3.one * gridStepSize, Quaternion.identity, solidLayerMask))
                {
                    grid[i, j] = new GridCell(this.transform.position + new Vector3(i * gridStepSize, 0, j * gridStepSize), true);
                }
                else
                {
                    grid[i, j] = new GridCell(this.transform.position + new Vector3(i * gridStepSize, 0, j * gridStepSize), false);
                }
            }
        }
    }
	
	// Update is called once per frame
	void Update () {
        if (!runningCheck)
        {
            StartCoroutine("CheckGrid");
        }
	}

    public IEnumerator CheckGrid()
    {
        runningCheck = true;
        for (int i = 0; i < grid.GetLength(0); i++)
        {
            for (int j = 0; j < grid.GetLength(1); j++)
            {
                if (Physics.CheckBox(grid[i, j].position, Vector3.one * (gridStepSize/2), Quaternion.identity, solidLayerMask))
                {
                    grid[i, j].SetBlocked(true);
                }
                else
                {
                    grid[i, j].SetBlocked(false);
                }
            }
            if(i%nmbrOfChecks == 0)
            {
                yield return 1;
            }
        }
        runningCheck = false;
    }

    public GridCell GetCellAtCoord(Vector3 coord)
    {
        Vector3 localCoord = coord - this.transform.position;
        if(localCoord.x >= 0 && localCoord.x < grid.GetLength(0) && localCoord.z >= 0 && localCoord.z < grid.GetLength(1))
        {
            return grid[Mathf.RoundToInt(localCoord.x), Mathf.RoundToInt(localCoord.z)];
        }
        return null;
    }

    public GridCell[] GetSurroundingCells(Vector3 coord)
    {
        List<GridCell> cells = new List<GridCell>();
        Vector3[] directions = new Vector3[8]
        {
            new Vector3(-1, 0,-1),
            new Vector3(-1, 0, 0),
            new Vector3(-1, 0, 1),
            new Vector3( 0, 0,-1),
            new Vector3( 0, 0, 1),
            new Vector3( 1, 0,-1),
            new Vector3( 1, 0, 0),
            new Vector3( 1, 0, 1),
        };
        foreach (Vector3 pos in directions)
        {
            GridCell cell = GetCellAtCoord(coord + pos);
            //Debug.Log(coord + " " + pos + " " + cell);
            if (cell != null && !cell.isBlocked)
            {
                cells.Add(cell);
            }
        }
        return cells.ToArray();
    }
    public Vector3 globalGoal;
    public Stack<GridCell> GetPathBetweenTwoPoints(Vector3 start, Vector3 goal)
    {
        globalGoal = goal;
        Stack<GridCell> path = new Stack<GridCell>();

        List<GridCell> closed = new List<GridCell>();
        List<GridCell> open = new List<GridCell>();

        GridCell currentCell = GetCellAtCoord(start);
        if(currentCell == null)
        {
            Debug.Log("Couldn't find the currentCell");
            return path;
        }
        currentCell.parent = null;
        GridCell goalCell = GetCellAtCoord(goal);
        if (goalCell == null)
        {
            Debug.Log("Couldn't find the goal cell");
            return path;
        }
        lastCalculatedTarget = goalCell.position;

        currentCell.gScore = 0;
        currentCell.hScore = GetHScore(currentCell.position, goalCell.position);
        currentCell.fScore = currentCell.gScore + currentCell.fScore;

        AddToOrderedList(currentCell, open);

        while(open.Count > 0 && open[0].position != goalCell.position && open.Count < 200)
        {
            currentCell = open[0];
            open.RemoveAt(0);
            closed.Add(currentCell);

            GridCell[] surroundingCells = GetSurroundingCells(currentCell.position);

            for (int i = 0; i < surroundingCells.Length; i++)
            {
                GridCell aCell = surroundingCells[i];
                if (closed.Contains(aCell))
                {
                    continue;
                }

                if (!open.Contains(aCell))
                {
                    aCell.gScore = GetGScore(currentCell, aCell);
                    aCell.hScore = GetHScore(aCell.position, goalCell.position);
                    aCell.fScore = aCell.gScore + aCell.hScore;
                    aCell.parent = currentCell;
                    AddToOrderedList(aCell, open);
                }
                else
                {
                    GridCell newCell = new GridCell(aCell);
                    newCell.gScore = GetGScore(currentCell, aCell);
                    newCell.hScore = GetHScore(aCell.position, goalCell.position);
                    newCell.fScore = aCell.gScore + aCell.hScore;
                    GridCell oldCell = open.Find((cell) =>
                    {
                        return cell.position == newCell.position;
                    });
                    if(oldCell.fScore > newCell.fScore)
                    {
                        open.Remove(oldCell);
                        newCell.parent = currentCell;
                        AddToOrderedList(newCell, open);
                    }
                }
            }
        }
        path = GetPathFromGoal(goalCell);
        //foreach (GridCell closedCell in closed)
        //{
        //    closedCell.Reset();
        //}
        //foreach (GridCell openCell in open)
        //{
        //    openCell.Reset();
        //}
        return path;
    }

    private float GetGScore(GridCell parentCell, GridCell currentCell)
    {
        return parentCell.gScore + Helper.DistanceToVector(parentCell.position, currentCell.position) * parentCell.weight;
    }

    private Stack<GridCell> GetPathFromGoal(GridCell goal)
    {
        Stack<GridCell> path = new Stack<GridCell>();

        while(goal.parent != null && path.Count < 100) //To avoid infite looping
        {
            path.Push(goal);
            goal = goal.parent;
        }

        return path;
    }

    /// <summary>
    /// Add a cell to the list from smallest fScore to largest
    /// </summary>
    /// <param name="cell"></param>
    /// <param name="list"></param>
    /// <returns></returns>
    public bool AddToOrderedList(GridCell cell, List<GridCell> list)
    {
        GridCell newCell = new GridCell(cell);
        if(list.Count == 0)
        {
            list.Add(cell);
            return true;
        }

        for (int i = 0; i < list.Count; i++)
        {
            if(list[i].fScore > cell.fScore)
            {
                list.Insert(i, cell);
                
                return true;
            }
            else
            {

            }
        }
        list.Add(cell);
        return true;
    }

    private float GetHScore(Vector3 a, Vector3 b)
    {
        return Vector3.Distance(a, b);
    }

    private void OnDrawGizmos()
    {
        if(Application.isPlaying){

            GUIStyle style = new GUIStyle();
            style.fontSize = 10;
            if(globalGoal != null)
            {
                Gizmos.DrawWireSphere(globalGoal, 0.6f);
            }
            for (int i = 0; i < grid.GetLength(0); i++)
            {
                for (int j = 0; j < grid.GetLength(1); j++)
                {
                    Vector3 thisPos = this.transform.position + new Vector3(i * gridStepSize, 0, j * gridStepSize);
                    if(lastCalculatedTarget == grid[i, j].position)
                    {
                        Handles.Label(thisPos + new Vector3(1, 0, 1) * 0.2f, "F: " + grid[i, j].fScore.ToString(), style);
                        Handles.Label(thisPos + new Vector3(-1, 0, 1) * 0.3f, "G: " + grid[i, j].gScore.ToString(), style);
                        Handles.Label(thisPos - new Vector3(1, 0, 1) * 0.3f, "H: " + grid[i, j].hScore.ToString(), style);
                        if (grid[i, j].parent != null)
                        {
                            Vector3 dir = grid[i, j].parent.position - grid[i, j].position;
                            Gizmos.DrawLine(grid[i, j].position, grid[i, j].parent.position - dir.normalized * 0.3f);
                        }
                        Gizmos.color = Color.cyan;
                    }
                    else if (!grid[i, j].isBlocked)
                    {
                        //GridCell[] surroundingCells = GetSurroundingCells(thisPos);
                        //foreach (GridCell cell in surroundingCells)
                        //{
                        //    if (!cell.isBlocked)
                        //    {
                        //        Gizmos.color = Color.magenta;
                        //        Gizmos.DrawLine(thisPos, cell.position);
                        //    }
                        //}
                        Handles.Label(thisPos + new Vector3(1, 0, 1) * 0.2f,    "F: " + grid[i, j].fScore.ToString(), style);
                        Handles.Label(thisPos + new Vector3(-1, 0, 1) * 0.3f,         "G: " + grid[i, j].gScore.ToString(), style);
                        Handles.Label(thisPos - new Vector3(1, 0, 1) * 0.3f,    "H: " + grid[i, j].hScore.ToString(), style);
                        if(grid[i, j].parent != null)
                        {
                            Vector3 dir = grid[i, j].parent.position - grid[i, j].position;
                            Gizmos.DrawLine(grid[i, j].position, grid[i, j].parent.position - dir.normalized*0.3f);
                        }
                        Gizmos.color = Color.green;
                    }
                    else
                    {
                        Gizmos.color = Color.red;
                    }
                    Gizmos.DrawWireCube(thisPos, Vector3.one * gridStepSize);
                }
            }
            //counter += 1;
            //if(counter > 25)
            //{
            //    counter = 0;
            //    Stack<GridCell> path = GetPathBetweenTwoPoints(sheep1.transform.position, sheep2.transform.position);
            //    foreach (GridCell cell in path)
            //    {
            //        Gizmos.DrawSphere(cell.position, 0.5f);
            //    }
            //}
        }
    }
}
