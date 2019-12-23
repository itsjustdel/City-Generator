using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AdjacentCells : MonoBehaviour {

    public List<GameObject> adjacentCells = new List<GameObject>();
    public List<GameObject> controlledAdjacents = new List<GameObject>();

    public bool edgeCell;
    public float targetY = 1f;
    public int controlledBy = -1;
    public bool frontlineCell = false;

    public bool beingMadeTransparent = false;

    public float miterSize = 1f;
    public float tolerance = 5f;
    //attach to gameobject to store adjacent cells 


    public List<List<int>> edges = new List<List<int>>();
    public int edgesCount = 0;
    public List<List<Vector3>> miters = new List<List<Vector3>>();
    public List<List<Vector3>> mitersSorted = new List<List<Vector3>>();

    public List<int[]> edgesAdjacents = new List<int[]>();
    public List<GameObject> adjacentEdgeCells = new List<GameObject>();

    private void Start()
    {
        tolerance = 0.1f;// GameObject.FindGameObjectWithTag("Code").GetComponent<MeshGenerator>().minEdgeSize;//testing

    }
    public void FindSharedEdges()
    {
        edgesAdjacents.Clear();
        adjacentEdgeCells.Clear();
        mitersSorted.Clear();

        //go through edge list and find what edge it is sharing with
        // MeshGenerator mg = GameObject.FindGameObjectWithTag("Code").GetComponent<MeshGenerator>();
        Vector3[] vertices = GetComponent<MeshFilter>().mesh.vertices;
        //s Mesh originalMesh = GetComponent<ExtrudeCell>().originalMesh;
        //for each each adjacent cell

        //for each edge in this cell
        for (int a = 0; a < edges.Count; a++)
        {
            //check eah edge in each adjacent cell
            for (int i = 0; i < adjacentCells.Count; i++)
            {
                Vector3[] adjacentVertices = adjacentCells[i].GetComponent<MeshFilter>().mesh.vertices;
                List<List<int>> otherEdges = adjacentCells[i].GetComponent<AdjacentCells>().edges;

                /*
                GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                c.transform.position = vertices[edges[a][0]];
                c.name = "0";
                c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                c.transform.position = vertices[edges[a][1]];
                c.name = "1";
                */
                //is cell adjacent low enough to warrant a wall?
                //    Debug.Log("other edges = " + otherEdges.Count);
                for (int b = 0; b < otherEdges.Count; b++)
                {
                    /*
                    c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    c.transform.position = adjacentOriginalVertices[otherEdges[b][0]];
                    c.name = "other 0";
                    c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    c.transform.position = adjacentOriginalVertices[otherEdges[b][1]];
                    c.name = "other 1";                    
                    */
                    Vector3 a0 = vertices[edges[a][0]];
                    Vector3 a1 = vertices[edges[a][1]];
                    Vector3 b0 = adjacentVertices[otherEdges[b][0]];
                    Vector3 b1 = adjacentVertices[otherEdges[b][1]];

                    // if (a0 == b0 && a1 == b1 || a0 == b1 && b0 == a1)
                    if (Vector3.Distance(a0, b0) < tolerance && Vector3.Distance(a1, b1) < tolerance
                    || Vector3.Distance(a0, b1) < tolerance && Vector3.Distance(b0, a1) < tolerance)
                    {
                        int[] adEdge = new int[] { otherEdges[b][0], otherEdges[b][1] };
                        //save edge
                        edgesAdjacents.Add(adEdge);/// do we need to re run loop with new edgesAdjacents list? 
                        //and which cell it belongs to
                        adjacentEdgeCells.Add(adjacentCells[i]);
                        //save miters too
                        mitersSorted.Add(miters[a]);
                    }
                }
            }
        }

        //adjacentEdgesCount = adjacentEdgeCells.Count;
    }

    public int[] FindSharedEdgeFromTargetCell(GameObject targetCell)
    {
        edgesAdjacents.Clear();
        adjacentEdgeCells.Clear();
        mitersSorted.Clear();

        //go through edge list and find what edge it is sharing with
        // MeshGenerator mg = GameObject.FindGameObjectWithTag("Code").GetComponent<MeshGenerator>();
        Vector3[] vertices = GetComponent<MeshFilter>().mesh.vertices;
        //s Mesh originalMesh = GetComponent<ExtrudeCell>().originalMesh;
        //for each each adjacent cell

        //for each edge in this cell
        for (int a = 0; a < edges.Count; a++)
        {
            //check eah edge in each adjacent cell
           // for (int i = 0; i < adjacentCells.Count; i++)
            {
                Vector3[] adjacentVertices = targetCell.GetComponent<MeshFilter>().mesh.vertices;
                //targetCell.GetComponent<AdjacentCells>().Edges();
                List<List<int>> otherEdges = targetCell.GetComponent<AdjacentCells>().edges;

                /*
                GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                c.transform.position = vertices[edges[a][0]];
                c.name = "0";
                c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                c.transform.position = vertices[edges[a][1]];
                c.name = "1";
                */
                //is cell adjacent low enough to warrant a wall?
                //    Debug.Log("other edges = " + otherEdges.Count);
                for (int b = 0; b < otherEdges.Count; b++)
                {
                    /*
                    c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    c.transform.position = adjacentOriginalVertices[otherEdges[b][0]];
                    c.name = "other 0";
                    c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    c.transform.position = adjacentOriginalVertices[otherEdges[b][1]];
                    c.name = "other 1";                    
                    */
                    Vector3 a0 = vertices[edges[a][0]];
                    Vector3 a1 = vertices[edges[a][1]];
                    Vector3 b0 = adjacentVertices[otherEdges[b][0]];
                    Vector3 b1 = adjacentVertices[otherEdges[b][1]];

                    // if (a0 == b0 && a1 == b1 || a0 == b1 && b0 == a1)
                    if (Vector3.Distance(a0, b0) < tolerance && Vector3.Distance(a1, b1) < tolerance
                    || Vector3.Distance(a0, b1) < tolerance && Vector3.Distance(b0, a1) < tolerance)
                    {
                        //store result in int array, first number is this edge index, second is other edge index
                        int[] edge = new int[] { a, b };
                        return edge;
                        
                    }
                }
            }
        }

        Debug.Log("returning null!");

        return null;
    }

    public void Edges()
    {
        edges.Clear();
        miters.Clear();

        //makes list of edges

        //adjacentCells = gameObject.GetComponent<AdjacentCells>().adjacentCells;

        //build game objects for each wall/edge
        Mesh mesh = GetComponent<MeshFilter>().mesh;

        Vector3[] verts = mesh.vertices;
        for (int i = 1; i < verts.Length; i++)
        {

            int prevInt = i - 1;
            int thisInt = i;
            int nextInt = i + 1;
            int nextNextInt = i + 2;

            if (prevInt <= 0)
            {
                //if central point, move to last
                prevInt = verts.Length - 1;
            }
            if (nextInt > verts.Length - 1)
            {
                //if next is over vertices length, put to start
                nextInt -= verts.Length - 1; //-1 less so we skip 0
            }
            if (nextNextInt > verts.Length - 1)
            {
                nextNextInt -= verts.Length - 1;
            }
            if (verts.Length == 2)
            {
                Debug.Log("verts length = " + verts.Length);
                Debug.Log("next next " + nextNextInt);
                Debug.Log(gameObject.name.ToString());
            }
            Vector3 p0 = verts[prevInt];
            Vector3 p1 = verts[thisInt];
            Vector3 p2 = verts[nextInt];
            Vector3 p3 = verts[nextNextInt];//shouldn't get to here, need to remove cell if edges get removed

            //so order around cell is previous,p0,p1,next
            Vector3 miterDirection0 = MiterDirection(p0, p1, p2,miterSize);
            Vector3 miterDirection1 = MiterDirection(p1, p2, p3,miterSize);

            List<int> edge = new List<int>() { thisInt, nextInt };

            bool showCubes = false; //helps show how points are running around cell
            if (showCubes)
            {
                // Debug.Log("pff");

                GameObject c0 = GameObject.CreatePrimitive(PrimitiveType.Cube);
                c0.transform.position = verts[thisInt];
                c0.name = "this" + thisInt;
                c0.transform.parent = transform;

                GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                c.transform.position = verts[prevInt];
                c.name = "prev" + prevInt;
                c.transform.parent = c0.transform;



                c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                c.transform.position = verts[nextInt];
                c.name = "next" + nextInt;
                c.transform.parent = c0.transform;


                c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                c.name = "next next" + nextNextInt;
                c.transform.position = verts[nextNextInt];

                c.transform.parent = c0.transform;
            }


            edges.Add(edge);

            //save
            miters.Add(new List<Vector3>() { -miterDirection0, -miterDirection1 });

            /*
            wall = new GameObject();
            wall.AddComponent<MeshRenderer>().sharedMaterial = Resources.Load("White") as Material;
            wall.AddComponent<MeshFilter>();
            //wall.transform.SetParent(transform, true);
            wall.transform.position = transform.position;// - Vector3.up * maxHeight;
            Mesh originalMesh = GetComponent<ExtrudeCell>().originalMesh;
            wall.GetComponent<MeshFilter>().mesh = IndividualWall(originalMesh, p1, p0, playerClassValues.maxClimbHeight, -miterDirection0, -miterDirection1);
            */

        }
        edgesCount = edges.Count;
    }

    Vector3 MiterDirection(Vector3 p0, Vector3 p1, Vector3 p2, float borderSize)
    {
        Vector3 miterDirection = new Vector3();

        //directions facing away from center point p1
        Vector3 dir0 = (p2 - p1).normalized;
        // Vector3 dir1 = (p2 - p1).normalized;

        //find the normal vector
        Vector3 normal0 = new Vector3(-dir0.z, 0f, dir0.x);

        //find the tangent vector at both end
        Vector3 tan0 = ((p1 - p0).normalized + dir0).normalized;

        //find the miter line, which is the normal of tangent
        miterDirection = new Vector3(-tan0.z, 0f, tan0.x);

        //find the lnegth of the miter by projecting the miter on to the normal
        float length = borderSize / Vector3.Dot(normal0, miterDirection);
        miterDirection *= length;

        //  Debug.DrawLine(p0, p1, Color.blue);
        //  Debug.DrawLine(p2, p1, Color.blue);
        // if(draw)
           //Debug.DrawLine(p1, p1 + miterDirection * -length , Color.red);
        //Debug.Break();
        //GetComponent<MeshRenderer>().enabled = true;
        /*
        GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
        c.transform.position = p0;
        c.name = "p0";
        c = GameObject.CreatePrimitive(PrimitiveType.Cube);
        c.transform.position = p1;
        c.name = "p1";
        c = GameObject.CreatePrimitive(PrimitiveType.Cube);
        c.transform.position = p1 + dir0;
        c.name = "m";
        */


        //
        return miterDirection;
    }

    public static void CalculateAdjacents(List<GameObject> cells, GameObject cell, float tolerance)
    {
        //work out which cells are adjacent tocell, save in a list

        List<GameObject> adjacents = new List<GameObject>();

        Vector3[] thisVertices = cell.GetComponent<MeshFilter>().mesh.vertices;
        for (int j = 0; j < cells.Count; j++)
        {
            //don't check own cell
            if (cell == cells[j])
                continue;

            Vector3[] otherVertices = cells[j].GetComponent<MeshFilter>().mesh.vertices;
            int matches = 0;

            for (int a = 0; a < thisVertices.Length; a++)
            {
                for (int b = 0; b < otherVertices.Length; b++)
                {
                    //if we have a match, add "other" cell to a list of adjacents for this cell
                    if (Vector3.Distance(thisVertices[a], otherVertices[b]) <= tolerance) //opt0- think this is ok as ==
                    {
                        //adjacents.Add(cells[j]); //making so we need two points for an adjacent cell

                        //force out of the loops
                        //a = thisVertices.Length;


                        matches++;
                    }
                }
            }

            if (matches > 1)//means if cell mathces one ponton a corner, we ignore. it has to be a solid edge
                adjacents.Add(cells[j]);
        
        }

        AdjacentCells aJ = null;
        if (cell.GetComponent<AdjacentCells>() == null)
            aJ = cell.AddComponent<AdjacentCells>();
        else
            aJ = cell.GetComponent<AdjacentCells>();

        aJ.adjacentCells = adjacents;
    }
}
