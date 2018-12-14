using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseFollower : MonoBehaviour {
    public Vector3 mousePosition;
    private NavMeshGenerator navMeshInstance;
    private Stack<GridCell> path;
    private bool waiting = false;
    public float speed;
	// Use this for initialization
	void Start () {
        navMeshInstance = NavMeshGenerator.instance;
	}
	
	// Update is called once per frame
	void Update () {
        this.transform.position += new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical")) * Time.deltaTime * speed;
        if (!waiting)
        {
            StartCoroutine("GetPathToMouse");
        }
    }

    public IEnumerator GetPathToMouse()
    {
        waiting = true;
        GridCell targetCell = navMeshInstance.GetClosetOpenCell(this.transform.position, Vector3.zero);
        GridCell startCell = navMeshInstance.GetClosetOpenCell(Vector3.zero, this.transform.position);
        if (targetCell != null && startCell != null)
        {
            path = navMeshInstance.GetPathBetweenTwoPoints(startCell.position, targetCell.position);
            yield return new WaitForSeconds(0.1f);
        }
        waiting = false;
    }

    private void OnDrawGizmos()
    {
        if(Application.isPlaying)
        {
            if(path != null)
            {
                foreach (GridCell cell in path)
                {
                    Gizmos.DrawWireSphere(cell.position, 0.5f);
                }
            }
        }
    }
}
