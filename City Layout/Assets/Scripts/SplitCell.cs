using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SplitCell : MonoBehaviour
{
    //class splits a cell created by voronoi algorithm in to smaller cells
    //mesh vertices need to be 0 for central vertice and then run in clockwise order
    public float minSize = 30f;//too small and it can get stuck in a loop constantly finding next 
    public MeshGenerator meshGenerator;
    private void Awake()
    {
        enabled = false; //calling staight from mesh generator when added
    }
    // Start is called before the first frame update
    public void Start()
    {
        if(meshGenerator == null)
        {
            meshGenerator = GameObject.FindWithTag("Code").GetComponent<MeshGenerator>();
        }
        GetComponent<MeshRenderer>().enabled = true;

        BreakDown();
    }

    void BreakDown()
    {

        //check to see if any edge on the cell is below the min edge size dictated by minSize variable
        //if edge is too small, skyscraper will be unable to build

        //grab edges, if not created yet, create
        List<List<int>> edges = new List<List<int>>();
        if (gameObject.GetComponent<AdjacentCells>() == null)
        {
            AdjacentCells aJ = gameObject.AddComponent<AdjacentCells>();
            aJ.Edges();
            edges = aJ.edges;
        }
        else
        {
            edges = gameObject.GetComponent<AdjacentCells>().edges;
        }


        //check for min size
        float distance = 0;
        Vector3[] vertices = GetComponent<MeshFilter>().mesh.vertices;

        for (int i = 0; i < edges.Count; i++)
        {
            float temp = Vector3.Distance(vertices[edges[i][0]], vertices[edges[i][1]]);
            if (temp > distance)
                distance = temp;
        }

        //if longest edge is over the size set in variable ( variable passed from mesh generator prefab)
        if (distance > minSize)        
        {

            List<GameObject> toAdd =  Split(edges);

            foreach (GameObject go in toAdd)
            {
                //main list of cells in mesh generator
                meshGenerator.cells.Add(go);
            }

            if(toAdd.Count > 0)
            {
                //we received splits back
                //remove the cell we are working on from the main cell list
                meshGenerator.cells.Remove(gameObject);
            }

            //add to working list 
            AddToWorkingSplitList(toAdd); 

            //for debuggion purposes
            //gameObject.name = "ToRemove";

            //let there be no light
            GetComponent<MeshRenderer>().enabled = false;

        }
    }

    void AddToWorkingSplitList(List<GameObject> cells)
    {
        //adds to the cell list which we are working through. All cells in the list will be split (or at least checked if they can be split)

        for (int i = 0; i < cells.Count; i++)
        {
            meshGenerator.cellsToSplit.Add(cells[i]);
        }

    }

    IEnumerator AddOverTime(List<GameObject> cells)//i like watching things happen one at a time for debugging - but addin coroutines for anything more means making sure of thread safety
    {
        for (int i = 0; i < cells.Count; i++)
        {
            yield return new WaitForSeconds(.5f);

            cells[i].AddComponent<SplitCell>().Start();
            Debug.Log("Starting");

            
            
        }

        yield break;
    }

    List<GameObject> Split( List<List<int>> edges)
    {
        

        //sort edges by length
        List<List<int>> edgesByLength = EdgesSortedByLength(edges);

        //check to see if the two longest edges are connected to each other
        bool connected = CheckForConnectedEdge(edgesByLength);

        if (connected)
        {
            //if connected edges cancel split. It creates too dramatic a shape for the building algorithm
            //return empty list
          
            return new List<GameObject>();
        }
        else
        {

            List<List<Vector3>> splitVertices = LongestSplit(edgesByLength, edges);

            //find point on adjacent cells so we can add geometry to them ( all points should haev a shared point on other cells
            SharedEdge(edgesByLength);

            //return new cells
            List<GameObject> cells = Cells(splitVertices);

            return cells;
        }
    }

    void SharedEdge(List<List<int>> edgesByLength)
    {

        //find the edge on an adjacent cell which streches across both split cells
        Vector3[] originalVertices = GetComponent<MeshFilter>().mesh.vertices;
        
        //find which edges on an adjacent cells match the one we selected
        List<GameObject> adjacentCells = GetComponent<AdjacentCells>().adjacentCells;
        //for each adjacent cell
        for (int a = 0; a < adjacentCells.Count; a++)
        {
            List<List<int>> otherEdges = adjacentCells[a].GetComponent<AdjacentCells>().edges;
            Vector3[] otherVertices = adjacentCells[a].GetComponent<MeshFilter>().mesh.vertices;
            //for each edge in each adjacent cell
            for (int b = 0; b < otherEdges.Count; b++)
            {


                //GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                //c.transform.position = otherVertices[otherEdges[b][0]];
                //c.name = "other edge 0";
                
                //c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                //c.transform.position = otherVertices[otherEdges[b][1]];
                //c.name = "other edge1";
                
                //look for a match with one of the edges we split
                for (int i = 0; i < 2; i++)//edges by length loop//this list is sorted by length, the first two were the ones we split
                {
                    //if(otherVertices [otherEdges[b][0] ] == originalVertices[ edgesByLength[i][1] ]&& otherVertices[otherEdges[b][1]] == originalVertices[edgesByLength[i][0]])
                      if (Vector3.Distance(otherVertices[otherEdges[b][0]], originalVertices[edgesByLength[i][1]]) < 0.1f &&
                            Vector3.Distance(otherVertices[otherEdges[b][1]], originalVertices[edgesByLength[i][0]]) < 0.1f)
                        {
                        /*
                        GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        c.transform.position = originalVertices[edgesByLength[i][0]];

                        c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        c.transform.position = originalVertices[edgesByLength[i][1]];
                        c.name = "1";
                        */

                        //we have found the edge on the other cell we need to split
                        //now insert new point in vertices
                        //split was made at halfway ( will need to make var if we want to randomise this split) or save in list
                        Vector3 splitPoint = Vector3.Lerp(originalVertices[edgesByLength[i][0]], originalVertices[edgesByLength[i][1]], 0.5f);

                        
                        List<Vector3> newVertices = new List<Vector3>(otherVertices);
                        //insert new vertice
                        newVertices.Insert(otherEdges[b][1], splitPoint);
                        //apply to cell
                        adjacentCells[a].GetComponent<MeshFilter>().mesh.vertices = newVertices.ToArray();
                        //do triangles too
                        List<int> triangles = new List<int>();
                        for (int j = 0; j < newVertices.Count; j++)
                        {
                            if (j < 2) continue;

                            triangles.Add(j);
                            triangles.Add(0);
                            triangles.Add(j - 1);
                        }

                        //add last
                        triangles.Add(1);
                        triangles.Add(0);
                        triangles.Add(newVertices.Count - 1);

                        adjacentCells[a].GetComponent<MeshFilter>().mesh.triangles = triangles.ToArray();
                        adjacentCells[a].GetComponent<MeshFilter>().mesh.RecalculateNormals();

                        
                    }

                    
                }

                
            }

            //update this instantly -
            adjacentCells[a].GetComponent<AdjacentCells>().Edges();
        }
    }

    void LongestPair()
    {
        //which vertices are furthest from each other //could definitely be optimised // closest neighbour research
        Mesh mesh = GetComponent<MeshFilter>().mesh;
        Vector3[] vertices = mesh.vertices;
        float furthest = 0;
        int[] pair = null;
        for (int i = 0; i < vertices.Length; i++)
        {
            for (int j = 0; j < vertices.Length; j++)
            {
                //don't check self or central point
                if (i == j || i == 0)
                    continue;
                float d = Vector3.Distance(vertices[i], vertices[j]);
                if (d > furthest)
                {
                    furthest = d;
                    pair = new int[2] { i, j };
                }
            }
        }

      //////  Debug.DrawLine(vertices[pair[0]], vertices[pair[1]], Color.blue);
      //  Debug.Break();
    }

    List<List<int>> EdgesSortedByLength(List<List<int>> edges)
    {
        Mesh mesh = gameObject.GetComponent<MeshFilter>().mesh;
        Vector3[] vertices = mesh.vertices;

       
        //order by length
        List<List<int>> edgesByLength = new List<List<int>>(edges);
        edgesByLength.Sort(delegate (List<int> a, List<int> b)
        {
            return Vector3.Distance(vertices[a[0]], vertices[a[1]])
            .CompareTo(
              Vector3.Distance(vertices[b[0]], vertices[b[1]]));
        });

        //reverse so largest is first. Sort() organises from low to high
        edgesByLength.Reverse();

        return edgesByLength;
    }

    bool CheckForConnectedEdge(List<List<int>> edgesByLength)
    {
        bool connected = false;

        //check to see if the two longest edges are connected to each other
        Mesh mesh = gameObject.GetComponent<MeshFilter>().mesh;
        Vector3[] vertices = mesh.vertices;

        Vector3 p0 = vertices[edgesByLength[0][0]];
        Vector3 p1 = vertices[edgesByLength[0][1]];
        Vector3 p2 = vertices[edgesByLength[1][0]];
        Vector3 p3 = vertices[edgesByLength[1][1]];

        if (p0 == p2 || p0 == p3
            || p1 == p2 || p1 == p3)
        {
            connected = true;

            bool draw = true;
            if (draw)
            {
                Debug.DrawLine(p0, p1, Color.red);
                Debug.DrawLine(p2, p3, Color.blue);

            }

        }
      
        return connected;
    }

    List<List<Vector3>> LongestSplit(List<List<int>> edgesByLength, List<List<int>> edges)
    {
       // GetComponent<MeshRenderer>().enabled = true;

        //we will return a list of vector3a for each split (2)
        List<List<Vector3>> splitVertices = new List<List<Vector3>>();

        Mesh mesh = gameObject.GetComponent<MeshFilter>().mesh;
        Vector3[] vertices = mesh.vertices;

      

      

       // Debug.DrawLine(vertices[edgesByLength[0][0]], vertices[edgesByLength[0][1]], Color.cyan);
        //Debug.DrawLine(vertices[edgesByLength[1][0]], vertices[edgesByLength[1][1]]);

        //now choose where to split these edges
        Vector3 split0 = Vector3.Lerp(vertices[edgesByLength[0][0]], vertices[edgesByLength[0][1]], .5f); //random?
        Vector3 split1 = Vector3.Lerp(vertices[edgesByLength[1][0]], vertices[edgesByLength[1][1]], .5f);

        //Debug.DrawLine(split0, split1, Color.red);

        //now we need to make two polygons out of this
        //start at [1]
        
        //find longest edgea in edge list
        int edge0Index = 0;
        int edge1Index = 0;
        for (int i = 0; i < edges.Count; i++)
        {
         
            if(edges[i] == edgesByLength[0])
            {
                edge0Index = i;
            }

            if (edges[i] == edgesByLength[1])
            {
                edge1Index = i;
            }
        }

        //now start from the end of the split and then work our way round the edges until we get back to the start of the split
        

       
        //using  for loop to switch between target edges
        for (int a = 0; a < 2; a++)
        {
            List<Vector3> list0 = new List<Vector3>();

            list0.Add(split0);
            list0.Add(split1);

            //have to reverse when going other way
            if (a == 1)
                list0.Reverse();

            //where to start on polygon changes on what side we are doing
            int start = edge1Index;
            if (a == 1)
                start = edge0Index;

            for (int i = start; i < 1000; i++) // a while loop with an upper safety number is used for debuggin so it doesn't stick forever
            {
                int index = i;
                if (index > edges.Count - 1)
                    index -= edges.Count;

                //continue round until we hit our other longest pair
                if (edges[index] == edgesByLength[a])
                    break;

                if (i == 999)
                    Debug.Log("Problem");

                list0.Add(vertices[edges[index][1]]);
            }

            splitVertices.Add(list0);
        }
        
        for (int i = 0; i < splitVertices.Count; i++)
        {
            for (int j = 0; j < splitVertices[i].Count; j++)
            {
                // GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
               // c.transform.position = splitVertices[i][j];
               // c.transform.parent = transform;
                //c.name = i.ToString();
            }
        }

       
        return splitVertices;

        
    }

    List<GameObject> Cells(List<List<Vector3>> splitVertices)
    {
        List<GameObject> cells = new List<GameObject>();

        //use passed list to make game objects with renderers etc
        //MeshGenerator mG = GameObject.FindGameObjectWithTag("Code").GetComponent<MeshGenerator>();
        for (int i = 0; i < splitVertices.Count; i++)
        {
            GameObject cell = new GameObject();
            cell.transform.position = transform.position;
            cell.name = "Split Cell " + i.ToString();
            cell.transform.parent = meshGenerator.transform;
            
            cell.layer = LayerMask.NameToLayer("Cells");

            MeshRenderer mR = cell.AddComponent<MeshRenderer>();
            mR.sharedMaterial = Resources.Load("Ground") as Material;

            MeshFilter mF = cell.AddComponent<MeshFilter>();

            //find center point of passed points. Cell needs set up like this for building algorithm
            Vector3 centroid = Vector3.zero;
            for (int j = 0; j < splitVertices[i].Count; j++)
            {
                centroid += splitVertices[i][j];
            }
            centroid /= splitVertices[i].Count;
            
            //add centre first
            splitVertices[i].Insert(0,centroid);

            List<int> triangles = new List<int>();
            //now triangulate cell
            for (int j = 0; j < splitVertices[i].Count; j++)
            {
                if (j == 0)
                    continue;

                if (j < splitVertices[i].Count - 1)
                {
                    triangles.Add(j);
                    triangles.Add(j + 1);
                    triangles.Add(0);
                }
                else
                {
                    //finish loop by attaching to the first index
                    triangles.Add(j);
                    triangles.Add(1);
                    triangles.Add(0);
                }
            }

            //add to mesh
            Mesh mesh = new Mesh();
            mesh.vertices = splitVertices[i].ToArray();
            mesh.triangles = triangles.ToArray();

            mF.mesh = mesh;

            //add aedge info and work out adjacents
            AdjacentCells aJ = cell.AddComponent<AdjacentCells>();
            aJ.Edges();

           // MergeCell.CalculateAdjacents(meshGenerator.cells,cell, meshGenerator.minEdgeSize);

            cells.Add(cell); ;
            
        }

        

        return cells;
    }

}
