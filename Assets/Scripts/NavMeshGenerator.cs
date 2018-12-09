using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NavMeshGenerator : MonoBehaviour {
    public int gridX;
    public int gridY;

    public float gridStepSize;
    private GridCell[,] grid;

    public bool runningCheck = false;
    public int nmbrOfChecks;
    // Use this for initialization
    void Start() {
        grid = new GridCell[gridX, gridY];
        for (int i = 0; i < grid.GetLength(0); i++)
        {
            for (int j = 0; j < grid.GetLength(1); j++)
            {
                if (Physics.CheckBox(this.transform.position + new Vector3(i * gridStepSize, 0, j * gridStepSize), Vector3.one * gridStepSize, Quaternion.identity))
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
                if (Physics.CheckBox(grid[i, j].position, Vector3.one * (gridStepSize/2), Quaternion.identity))
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
            return grid[(int)localCoord.x, (int)localCoord.z];
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
            if (cell != null)
            {
                cells.Add(cell);
            }
        }
        return cells.ToArray();
    }

    private void OnDrawGizmos()
    {
        if(Application.isPlaying){

            for (int i = 0; i < grid.GetLength(0); i++)
            {
                for (int j = 0; j < grid.GetLength(1); j++)
                {
                    Vector3 thisPos = this.transform.position + new Vector3(i * gridStepSize, 0, j * gridStepSize);
                    if (!grid[i, j].isBlocked)
                    {
                        GridCell[] surroundingCells = GetSurroundingCells(thisPos);
                        foreach (GridCell cell in surroundingCells)
                        {
                            if (!cell.isBlocked)
                            {
                                Gizmos.color = Color.magenta;
                                Gizmos.DrawLine(thisPos, cell.position);
                            }
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
        }
    }
}
