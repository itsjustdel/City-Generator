using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MergeCell : MonoBehaviour
{
    public GameObject target;
    public int mergedWith = 0;
    public bool keepMerging = true;

    public List<GameObject> previousCells = new List<GameObject>();
    private void Awake()
    {
        enabled = false;
    }
    // Start is called before the first frame update
    public void Start()
    {
        GameObject mergedCell = ChooseEdgeToMergeWith();

        if(mergedCell != null)
        {
            if (mergedWith < 4)
            {

                MergeCell mergeCell = mergedCell.GetComponent<MergeCell>();
                //keep a track of how many cells we have merged in this chain
                mergeCell.mergedWith = mergedWith + 1;
                mergeCell.previousCells.AddRange(previousCells);
                mergeCell.Start();
            }

            else
            {
                Vector3[] vertices = mergedCell.GetComponent<MeshFilter>().mesh.vertices;
                for (int i = 0; i < vertices.Length; i++)
                {
                    GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    c.transform.position = transform.position + vertices[i];
                }
            }
        }
        
        
    }
    
    GameObject ChooseEdgeToMergeWith()
    {

        
        //choose from adjacent cells
        List<GameObject> adjacentCells = GetComponent<AdjacentCells>().adjacentCells;

        List<GameObject> possibleTargets = new List<GameObject>();
        //check if any possible targets have already been merged, as we merge them we drop an disabled merge cells component on them as a bookmark
        for (int i = 0; i < adjacentCells.Count; i++)
        {
            //if no merge cell on cell it means we havent tried to merge it at all
            if (adjacentCells[i].GetComponent<MergeCell>() == null)          
                possibleTargets.Add(adjacentCells[i]);
        }
        
        if(possibleTargets.Count == 0)
        {
            Debug.Log("Can't merge");
            
            return null;
        }
        GameObject targetCell = possibleTargets[Random.Range(0, possibleTargets.Count)];
        //if allocated in inspector (for tests)
        if (target != null)
            targetCell = target;

        targetCell.name = "target";
        targetCell.GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("White") as Material;
        //add a merge cell component to book mark
        targetCell.AddComponent<MergeCell>().enabled = false;
        //find which edge we share with target cell
        int[] edge = GetComponent<AdjacentCells>().FindSharedEdgeFromTargetCell(targetCell);
        if( edge == null)
        {
            //no suitable edges found, cancel merge
            Debug.Log("no good edge, stopping merge");
            
            return null;
        }
        //first edge entry is target edge number, second is this - not exactly trivial
        List<List<int>> edges = GetComponent<AdjacentCells>().edges;
        List<List<int>> otherEdges = targetCell.GetComponent<AdjacentCells>().edges;

        List<int> thisEdge = edges[edge[0]];
        List<int> otherEdge = otherEdges[edge[1]];
        Vector3[] targetVertices = targetCell.GetComponent<MeshFilter>().mesh.vertices;
        Vector3[] vertices = GetComponent<MeshFilter>().mesh.vertices;
        Debug.DrawLine(vertices[thisEdge[0]], vertices[thisEdge[1]], Color.cyan);
        Debug.DrawLine(targetVertices[otherEdge[0]] + Vector3.up, targetVertices[otherEdge[1]] + Vector3.up, Color.white);

        //organise points from the two cells by angle - probably an eaiser way todo this but I was having problems doing it
        //centre point is average point between 
        Vector3 centrePoint = Vector3.Lerp(vertices[0], targetVertices[0], 0.5f);
        List<Vector3[]> cellPoints = new List<Vector3[]>();
        cellPoints.Add(vertices);
        cellPoints.Add(targetVertices);

        // List<Vector3> ring = OrganiseRingPoints(cellPoints);
        //List<Vector3> ring = OrganiseRingEdges();
        
        Mesh combinedAndWeldedMesh = WeldedMesh(new List<GameObject>() { gameObject, targetCell } );
        List<Vector3> ring = OutsidePath(combinedAndWeldedMesh);
        //add central point to this ring
        ring.Insert(0, centrePoint);
      
        //now add this to mesh, we don't need to bother with triangles- we will create a triangulate street/pavement for it anyway
        //GetComponent<MeshFilter>().mesh.vertices = ring.ToArray();
        List<int> triangles = new List<int>();
        //now triangulate cell
        for (int j = 0; j < ring.Count; j++)
        {
            if (j == 0)
                continue;//skip centre

           // GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
         //   c.transform.position = ring.po
            if (j < ring.Count - 1)
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

        
        GameObject mergedCell = new GameObject();
        mergedCell.name = "Merged Cell x " + mergedWith.ToString(); ;
        //place a disabled merge cell as a marker (we will not merge two merged cells)
        mergedCell.AddComponent<MergeCell>().enabled = false;
        mergedCell.transform.parent = transform.parent;
        MeshFilter mf = mergedCell.AddComponent<MeshFilter>();
        mergedCell.AddComponent<MeshRenderer>().sharedMaterial = Resources.Load("Ground") as Material;

        
        Mesh mesh = new Mesh();
        mesh.vertices = ring.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
        
        mf.mesh = mesh;
        //remove from main cells list
        MeshGenerator mg = GameObject.FindGameObjectWithTag("Code").GetComponent<MeshGenerator>();
        mg.cells.Remove(gameObject);
        mg.cells.Remove(targetCell);
        mg.cells.Add(mergedCell);

        //add adjacent cells script to new cell//
        mergedCell.AddComponent<AdjacentCells>().Edges();
        
        //redo edges for all adjacents
        for (int i = 0; i < adjacentCells.Count; i++)
        {
            AdjacentCells.CalculateAdjacents(mg.cells, adjacentCells[i], mg.minEdgeSize);
        }

        List<GameObject> targetAdjacents = targetCell.GetComponent<AdjacentCells>().adjacentCells;
        //and for each on targets
        for (int i = 0; i < targetAdjacents.Count; i++)
        {
            AdjacentCells.CalculateAdjacents(mg.cells, targetAdjacents[i], mg.minEdgeSize);
        }
        //now we can add adjacents to our new merged cell
        AdjacentCells.CalculateAdjacents(mg.cells, mergedCell, mg.minEdgeSize);

        //option for debuggin to keep track of what heppened
        bool destroyOldCells = false;
        if (destroyOldCells)
        {
            Destroy(gameObject);
            Destroy(targetCell);
            
        }
        else
        {
            //turn renderer off and save to a list which will be passed on to next merged cell we can view in inspector
            gameObject.GetComponent<MeshRenderer>().enabled = false;
            targetCell.GetComponent<MeshRenderer>().enabled = false;

            //gameObject.transform.parent = mergedCell.transform;
            //targetCell.transform.parent = mergedCell.transform; //positional issues with this

            previousCells.Add(gameObject);
            previousCells.Add(targetCell);
        }

      //  if (canMerge)
        //    mergedCell.AddComponent<MergeCell>();

        return mergedCell;
    }

    Mesh WeldedMesh(List<GameObject> cellsToWeld)
    {
        //combine meshes
        MeshFilter[] meshFilters = new MeshFilter[cellsToWeld.Count];
        for (int a = 0; a < cellsToWeld.Count; a++)
        {
            meshFilters[a] = cellsToWeld[a].GetComponent<MeshFilter>();
        }
        CombineInstance[] combine = new CombineInstance[meshFilters.Length];

        int i = 0;
        while (i < meshFilters.Length)
        {
            combine[i].mesh = meshFilters[i].sharedMesh;
            combine[i].transform = meshFilters[i].transform.localToWorldMatrix;
            meshFilters[i].gameObject.SetActive(false);

            i++;
        }
        Mesh combinedMesh = new Mesh();
        combinedMesh.CombineMeshes(combine);

        //now weld vertices together
        Mesh weldedMesh = AutoWeld.AutoWeldFunction(combinedMesh, .01f, 100);

        //check for vertices with no triangles?
        Vector3[] vertices = weldedMesh.vertices;
        int[] triangles = weldedMesh.triangles;
        for (int a = 0; a < vertices.Length; a++)
        {
            int matches = 0;
            for (int b = 0; b < triangles.Length; b++)
            {
                if(triangles[b] == a)
                {
                    matches++;
                }
            }

            if(matches == 0)
            {
                GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                c.transform.position = vertices[a];
                c.name = "No Tris";
            }
        }


        return weldedMesh;
    }

    List<Vector3> OutsidePath(Mesh weldedMesh)
    {
        List<Vector3> borderPoints = new List<Vector3>();

        List<EdgeHelpers.Edge> boundaryPath = EdgeHelpers.GetEdges(weldedMesh.triangles).FindBoundary().SortEdges();

        Vector3[] vertices = weldedMesh.vertices;

        //now organise these edges in two one sequential vector3 list for mesh to build from
        for (int i = 1; i < boundaryPath.Count; i++)
        {
            borderPoints.Add(vertices[ boundaryPath[i].v1 ]);

            //we need to add the 2nd point the last edge to complete the loop
            if(i == boundaryPath.Count-1)
                borderPoints.Add(vertices[boundaryPath[i].v2]);

            /*
            
            GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
            c.transform.position = vertices[boundaryPath[i].v1];
            c.name = "1";
            Destroy(c, 3);

            c = GameObject.CreatePrimitive(PrimitiveType.Cube);
            c.transform.position = vertices[boundaryPath[i].v2];
            Destroy(c, 3);
            c.name = "2";
            */
        }

        return borderPoints;
    }

    List<Vector3> OrganiseRingPoints(List<Vector3[]> cells)
    {
        //take passed lists of vector3s and create a sequential ring 

        List<Vector3> ringPoints = new List<Vector3>();

        int matches = 0;        
        int index = 1;//don't start at 0
        int nextIndex = 0;
        int safety = 0;
        
        while (matches <3 || safety > 100)
        {

            int thisCell = 0;
            int otherCell = 1;
            
            if (matches == 1)
            {
                //switching cells round once we find our first match
                thisCell = 1;
                otherCell = 0;
            }

            if (index > cells[thisCell].Length - 1)
                index -= cells[thisCell].Length;

            if (index == 0)
            {
                index++;
                continue;//skip any central point
            }

            //look for match with first point - if we hit this we have completed our loop
            //only check on third match
            if (matches == 2)
            {
                if (cells[thisCell][index] == cells[thisCell][1])
                {
                   // Debug.Log("end");
                    break;
                }
            }

            //look for match with second cell
            bool match = false;
            for (int i = 0; i < cells[otherCell].Length; i++)
            {
                if(cells[thisCell][index] == cells[otherCell][i])
                {
                    match = true;

                    //remember index for next loop
                    nextIndex = i;
                }
            }

            

            if (match == false)
            {
                GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                c.transform.position = cells[thisCell][index];
                Destroy(c, 3);
                
                ringPoints.Add(cells[thisCell][index]);

                index++;
            }
            else//if true
            {
                GameObject c = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                c.transform.position = cells[thisCell][index];

                Destroy(c, 3);

                ringPoints.Add(cells[thisCell][index]);

                //if we have a match but we haven't added any points yet, don't swap cell, just keep going round this cell until we can add a non match
                if (ringPoints.Count == 0)
                {
                    Debug.Log("Special case");
                    
                    index++;
                }
                else
                {
                    //jump to next cell and keep adding points

                    matches++;
                    index = nextIndex + 1;

                    continue;
                }

                
            }

            safety++;
        }
        if (safety > 300)
        {
            Debug.Break();
            Debug.Log("safety, matches = " + matches);
        }
        return ringPoints;
    }

    List<Vector3> OrganiseRingEdges(List<GameObject> cells)
    {
        List<Vector3> ringPoints = new List<Vector3>();
        Vector3[] thisVertices = cells[0].GetComponent<MeshFilter>().mesh.vertices;
        Vector3[] otherVertices = cells[1].GetComponent<MeshFilter>().mesh.vertices;

        List<List<int>> thisEdges = cells[0].GetComponent<AdjacentCells>().edges;
        List<List<int>> otherEdges = cells[1].GetComponent<AdjacentCells>().edges;

        //find starting edge( the edge which is shared)
        List<int> sharedEdge = new List<int>();
        GameObject c = null;
        for (int i = 0; i < thisEdges.Count; i++)
        {
            for (int j = 0; j < otherEdges.Count; j++)
            {
                

             //   if(thisVertices[ thisEdges[i][0] ]==otherVertices[ otherEdges[j][0] ] && thisVertices[ thisEdges[i][0] ]==otherVertices[ otherEdges[j][0]]
               //     || thisVertices[thisEdges[i][0]] == otherVertices[otherEdges[j][1]] && thisVertices[thisEdges[i][0]] == otherVertices[otherEdges[j][1]])
                {
                    sharedEdge = thisEdges[i];

                    c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    c.transform.position = thisVertices[thisEdges[i][0]];
                    c.name = "I 0 " + i.ToString();
                    c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    c.transform.position = thisVertices[thisEdges[i][1]];
                    c.name = "I 1 " + i.ToString();

                    c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    c.transform.position = otherVertices[otherEdges[j][0]];
                    c.name = "J 0 " + j.ToString();

                    c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    c.transform.position = otherVertices[otherEdges[j][1]];
                    c.name = "J 1 " + j.ToString();
                }
            }
        }

        c = GameObject.CreatePrimitive(PrimitiveType.Cube);
        c.transform.position = thisVertices[sharedEdge[0]];
        c.name = "shared 0 ";

        c = GameObject.CreatePrimitive(PrimitiveType.Cube);
        c.transform.position = thisVertices[sharedEdge[1]];
        c.name = "shared 1 " ;
        

        return ringPoints;
    }

    List<Vector3> Snip(List<Vector3> ringPoints)
    {
        //find any points not on outer rim - 
        //do this by finding any duplicates and removing points between them
        List<Vector3> toRemove = new List<Vector3>();
        for (int i = 0; i < ringPoints.Count; i++)
        {
            for (int j = i+1; j < ringPoints.Count; j++)
            {
               // if (i == j) continue;

                if(ringPoints[i] == ringPoints[j])
                {

                    GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    c.transform.position = ringPoints[i];
                }
            }
        }

        for (int i = 0; i < toRemove.Count; i++)
        {
           
        }

        return ringPoints;
    }
    
   
}
