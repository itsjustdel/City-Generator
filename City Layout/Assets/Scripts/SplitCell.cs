using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SplitCell : MonoBehaviour
{
    //class splits a cell created by voronoi algorithm in to smaller cells
    //mesh vertices need to be 0 for central vertice and then run in clockwise order
    public float minSize = 30f;
    public MeshGenerator meshGenerator;
    private void Awake()
    {
        enabled = false; //calling staight from mesh generator when added
    }
    // Start is called before the first frame update
    public void Start()
    {
        GetComponent<MeshRenderer>().enabled = true;
        BreakDown();
    }

    void BreakDown()
    {
        //for each cell in working list
        //if large enough
        List<GameObject> workingList = new List<GameObject>() { gameObject };
        List<GameObject> returnedList = new List<GameObject>();

        //make a list of cells which we will remove from the mesh generators cell list once we have finished loop
        //List<GameObject> toRemove = new List<GameObject>();
        List<GameObject> toAdd = new List<GameObject>();
        while (workingList.Count > 0)
        {
            //we need edge info for cells when splitting, add and work out
            workingList[0].AddComponent<AdjacentCells>().Edges();
           // Debug.Log("edge count for working cell = " + workingList[0].GetComponent<AdjacentCells>().edges.Count);
            if (workingList[0].GetComponent<MeshRenderer>().bounds.size.magnitude > 80f) //avg around 80
            {
                List<GameObject> tempReturnedCells = Split(workingList[0]);
                //add to our working list so while loop will eventually get to it
                workingList.AddRange(tempReturnedCells);

                workingList[0].name = "ToRemove";
                //      toRemove.Add(workingList[0]);

                Destroy(workingList[0]);
            }
            else //keep as is
            {
               
                    toAdd.Add(workingList[0]);

                workingList[0].GetComponent<MeshRenderer>().enabled = true;  
            }
            
            //get rid of this and move on to the next       


            workingList.RemoveAt(0);
            
        }

        //now add all the cells we created to a list that will add/remove them once mesh generater script has worked its way through initial loop
        //foreach (GameObject go in toRemove)
            //meshGenerator.cellsToRemove.Add(go);

        foreach (GameObject go in toAdd)
            meshGenerator.cellsToAdd.Add(go);
    }

    List<GameObject> Split(GameObject toSplit)
    {

        List<List<Vector3>> splitVertices = LongestSplit(toSplit);
        List<GameObject> cells = Cells(splitVertices);

        return cells;
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

        Debug.DrawLine(vertices[pair[0]], vertices[pair[1]], Color.blue);
        Debug.Break();
    }    

    List<List<Vector3>> LongestSplit(GameObject toSplit)
    {
       // GetComponent<MeshRenderer>().enabled = true;

        //we will return a list of vector3a for each split (2)
        List<List<Vector3>> splitVertices = new List<List<Vector3>>();

        Mesh mesh = toSplit.GetComponent<MeshFilter>().mesh;
        Vector3[] vertices = mesh.vertices;

        //finds the two longest edges, then creates a line between them
        List<List<int>> edges = toSplit.GetComponent<AdjacentCells>().edges;

        //order by length
        List<List<int>> edgesByLength = new List<List<int>>(edges);
        edgesByLength.Sort(delegate (List<int> a, List<int> b)
        {
            return Vector3.Distance(vertices[a[0]], vertices[a[1]])
            .CompareTo(
              Vector3.Distance(vertices[b[0]], vertices[b[1]]));
        });
        //make largest number first in list
        edgesByLength.Reverse();

        Debug.DrawLine(vertices[edgesByLength[0][0]], vertices[edgesByLength[0][1]], Color.cyan);
        Debug.DrawLine(vertices[edgesByLength[1][0]], vertices[edgesByLength[1][1]]);

        //now choose where to split these edges
        Vector3 split0 = Vector3.Lerp(vertices[edgesByLength[0][0]], vertices[edgesByLength[0][1]], .5f); //random?
        Vector3 split1 = Vector3.Lerp(vertices[edgesByLength[1][0]], vertices[edgesByLength[1][1]], .5f);

        Debug.DrawLine(split0, split1, Color.red);

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

            cells.Add(cell); ;
            
        }

        

        return cells;
    }

}
