using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DualGraph2d;
using System;
using System.Linq;
using UnityEditor;
using UnityEngine.Audio;

[RequireComponent (typeof(MeshFilter),typeof(MeshRenderer),typeof(MeshCollider))]
public class MeshGenerator : MonoBehaviour {

	//public BuildController buildController;
	public Material[] materials;
	public Transform cubePrefab;
	
	//public BezierSpline roadCurveL;
	//public BezierSpline roadCurveR;
	//public float roadCurveFrequency;
	//public float roadSpread = 3f;
	//public GridPlayer gridPlayer;
    private List<Vector3> roadList = new List<Vector3>();
    public Vector3 volume;//= new Vector3(20.0f,0.0f,20.0f);
	public float rootTriMultiplier=1.0f;
	public int cellNumber= 20;
    public int lloydIterations = 3;
    public float minEdgeSize = 3f;
    public bool useSortedGeneration;//=true;
	public bool drawCells=false;
	public bool drawDeluany=false;
	public bool drawRoots=false;
	public bool drawVoronoi=false;
	public bool drawGhostEdges=false;
	public bool drawPartialGhostEdge=false;
	public bool drawCircumspheres=false;
	public Color sphereColor= Color.cyan;
	

	//public DualGraph dualGraph;
//	private float totalTime;
	private float computeTime;
	private Mesh graphMesh;

	
	public bool fillWithRandom;
	public bool fillWithPoints;
    public bool interior = false;

    public bool extrudeCells;
    public bool walls = true;
    
    public bool weldCells = true;
    public float weldThreshold = 10f;//how wide should minimum  ledge size be?
    public bool makeSkyscraperTraditional = true;
    public bool doBuildControl;
    public float threshold = 2f;

	public List<Vector3> yardPoints = new List<Vector3>();
 //   public List<GameObject> cellsList = new List<GameObject>();
   // public List<Mesh> meshList = new List<Mesh>();
    public List<Vector3[]> meshVerts = new List<Vector3[]>();
    public List<int[]> meshTris = new List<int[]>();

    public int density = 5;
    public float minSplitSize = 50;

    public List<GameObject> cells = new List<GameObject>();
    public List<GameObject> cellsToAdd = new List<GameObject>();
    public List<GameObject> cellsToSplit = new List<GameObject>();
    public List<GameObject> cellsToMerge = new List<GameObject>();
    // public List<GameObject> cellsToRemove = new List<GameObject>();
    //public List<List<GameObject>> adjacentCells = new List<List<GameObject>>();//moved t0 saving on each cell with AdjacentCells class
    public int adjacentCellsCount = 0;

    public int counter = 0;
    List<Vector3> centroids = new List<Vector3>();

    float tolerance =5f;
    public List<List<List<int>>> edges = new List<List<List<int>>>();//cell number//edge number//edge numbers (create struct)

    Material standardMaterial;

    public void Start ()
    {

        //steal the main material froma  primitive
        GameObject tempCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        standardMaterial = tempCube.GetComponent<MeshRenderer>().material;
        standardMaterial.SetFloat("_Metallic", 0.5f);
        Destroy(tempCube);


        tolerance = minEdgeSize;//testing


       

        Lloyds();

    }

    void GeneratePoints(Vector3[] points,DualGraph dualGraph)
    {
       
        //set up cells
        dualGraph.DefineCells(points, rootTriMultiplier);
    //    computeTime = Time.realtimeSinceStartup;

        //compute
        if (useSortedGeneration)
            dualGraph.ComputeForAllSortedCells();
        else
            dualGraph.ComputeForAllCells();




        //  yield return new WaitForEndOfFrame();

    
        

       // Debug.Log("coroutine");

        
    }

    void Lloyds()
    {
        
    
       
        //for (int x = 0; x < lloydIterations; x++)
        
        //we will keep relaxing until all our edges are at least 
        bool edgeShortEnough = false;
        //int maxCounts = 50;
        int count = 0;
        List<Vector3> borderPoints = new List<Vector3>();
        while(!edgeShortEnough && count < lloydIterations)
        {
            //  Debug.Log("LastIteration =" + count);

            //destroy old cells (if any), we will create new ones on this iteration
            for (int i = 0; i < cells.Count; i++)
            {
                //extruded cell is not parented
                if (cells[i].GetComponent<ExtrudeCell>()!= null)
                    Destroy(cells[i].GetComponent<ExtrudeCell>().extrudedCell);

                Destroy(cells[i].gameObject);
            }
            

            DualGraph dualGraph = new DualGraph(volume);

            //Debug.Log("cells count before split = " + cells.Count);
            cells.Clear();


            if (!interior)
            {

                cellNumber = (int)volume.x / density;

                Vector3[] points = new Vector3[cellNumber];

                if (count > 0)
                    points = new Vector3[centroids.Count];


                //switch to relaxing after initial first randomisation
                if (count > 0)
                {
                    fillWithPoints = true;
                    fillWithRandom = false;
                }

                GenSortedRandCells(ref points);



                centroids.Clear();

                dualGraph.DefineCells(points, rootTriMultiplier);
                dualGraph.ComputeForAllSortedCells();
                // dualGraph.ComputeForAllCells();

                dualGraph.PrepareCellsForMesh();

            }
            else
            {
                cellNumber = yardPoints.Count;

                Vector3[] points = new Vector3[cellNumber];

                GenSortedRandCellsForInterior(ref points);

                centroids.Clear();

                dualGraph.DefineCells(points, rootTriMultiplier);
                dualGraph.ComputeForAllSortedCells();
                // dualGraph.ComputeForAllCells();

                dualGraph.PrepareCellsForMesh();
            }
            
            


            
            //work out centroids for next iteration 
            for (int i = 0; i < dualGraph.cells.Count; i++)
            {
                if (!dualGraph.cells[i].root && !dualGraph.cells[i].IsOpenEdge())
                {                       //use only interior until faux edges are added
                    if (dualGraph.cells[i].IsOpenEdge())
                    {
                        Debug.Log("open edge");
                    }
                    Vector3 avg = Vector3.zero;
                    for (int a = 0; a < dualGraph.cells[i].mesh.verts.Length; a++)
                    {
                        if (a != 0)
                            avg += dualGraph.cells[i].mesh.verts[a];//make temp []

                    }
                    avg /= dualGraph.cells[i].mesh.verts.Length - 1;
                    centroids.Add(avg);
                }
                else
                {
                    //saving outside border points, if we dont the graph will get smaller and smaller with each lloyd iteration
                    if (( dualGraph.cells[i].IsOpenEdge() && !dualGraph.cells[i].root))
                    {
                        centroids.Add(dualGraph.cells[i].point);
                       // GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        //c.transform.position = dualGraph.cells[i].point;
                    }
                }
            }

           

            if (count == 0)
            {

                for (int i = 0; i < centroids.Count; i++)
                {
                   // GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                   // c.transform.position = centroids[i];
                  //  c.name = "c";// x.ToString() + " " + i.ToString();
                }

            }

            bool doShortestEdge = false;
            if (doShortestEdge)
            {
                //work out if we have relaxed enough
                float shortestEdgeDistance = FindShortestEdgeDistance(dualGraph);
                if (shortestEdgeDistance >= minEdgeSize)
                {
                    Debug.Log("min edge distance reached ");
                    edgeShortEnough = true;
                }
                else
                    Debug.Log("shortest distance = " + shortestEdgeDistance);
            }

            count++;



            //cells = new List<GameObject>();
            GenerateMesh(dualGraph);

            CellMeshes();
            

          //  yield return new WaitForSeconds(.5f);

           
        }

        //city layout
        if (!interior)
        {
            //take small corners out to simplify the shape
            Edges();

            CalculateAdjacents();

            RemoveSmallEdges();

            ReMesh(true);

            //SplitCells();//////////removing atm

            //create a list of edges for each polygon and save on Adjacent Edges script added to each cell
            Edges();
            CalculateAdjacents();
            //prob not necessary ^^

           // if (cells.Count > 6)////////////removing atm
           //     MergeCells();


            //choose colours for each cell
            SetPalletes();

            //add ...
            AddToCells();
        }
        else//interiro
        {
            
            Edges();

            
            CalculateAdjacents();


             RemoveSmallEdges();
            //ReMesh(true);//NEED if not removinfg small edges - small edges does this at bottom of method


            //create a list of edges for each polygon and save on Adjacent Edges script added to each cell
            Edges();

            CalculateAdjacents();
           
            //interiors - each floor
            for (int i = 0; i < cells.Count; i++)
            {
                cells[i].GetComponent<MeshRenderer>().enabled = false;
                
            }

            
        }
     //   yield break;
    }

    void SplitCells()
    {
        //we can split some of the voronoi cells for smaller internal patterns
        List<GameObject> originalCells = new List<GameObject>(cells);


        //we will split only one cell at the moment
        //add one to the build list to get it going
        //cellsToSplit.Add(cells[UnityEngine.Random.Range(0,cells.Count)]);

        for (int i = 0; i < cells.Count; i++)
        {
            cellsToSplit.Add(cells[i]);
        }
        

        while (cellsToSplit.Count > 0)
        {
            //need to add adjacent cells stuff to new cells
            SplitCell splitCell = cellsToSplit[0].AddComponent<SplitCell>();
            splitCell.meshGenerator = this;
            splitCell.minSize = minSplitSize;
            splitCell.Start();

            cellsToSplit.RemoveAt(0);

            Edges();
            CalculateAdjacents();

            
        }
    }

   
    void MergeCells()
    {
        //merge some cells together
        int maxMerges = 1;
        for (int i = 0; i < maxMerges; i++)
        {
            
        
            int r = UnityEngine.Random.Range(0, cells.Count);

            //don't merge a cell already merged, it will be huge -just continue, use up one of the chances to merge
            if (cells[r].GetComponent<MergeCell>() != null)
                continue;

            cells[r].AddComponent<MergeCell>();
            cellsToMerge.Add(cells[r]);

            while (cellsToMerge.Count > 0)
            {
                cellsToMerge[0].GetComponent<MergeCell>().maxMerges = UnityEngine.Random.Range(1, 1);
                cellsToMerge[0].GetComponent<MergeCell>().Start();

                cellsToMerge.RemoveAt(0);

                Edges();
                CalculateAdjacents();
            }
        }
    }

    void ReMesh(bool enableRenderer)
    {  

        for (int i = cells.Count-1; i >= 0; i--)// can be removing
        {


            if (interior)
            {
              //  Debug.Log("before = " + cells[i].GetComponent<MeshFilter>().mesh.vertexCount);
            }

            if (enableRenderer)
                cells[i].GetComponent<MeshRenderer>().enabled = true;

            Vector3[] vertices = cells[i].GetComponent<MeshFilter>().mesh.vertices;




            List<Vector3> newVertices = new List<Vector3>();
            

            for (int j = 0; j < vertices.Length; j++)
            {
                //we are looking for welded vertices
                if (j < 2)
                {
                    //always add [0] (centre) and 1(we will compare this in the next iteration)
                    newVertices.Add(vertices[j]);
                }
                else if (Vector3.Distance(vertices[j], vertices[j - 1]) <= tolerance)//was ==
                {
                    //  GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    //  c.transform.position = vertices[j];
                    //  c.name = "match";
                    //  c.transform.parent = cells[i].transform;

                    //don't add this duplicate
                    continue;

                }
                else
                {
                    if (Vector3.Distance(vertices[j], newVertices[1]) > tolerance)//can loop, so catch if it tries to weld last to first (instead !=. doing distance)
                        newVertices.Add(vertices[j]);
                }
            }

            //check we can still make a mesh
            if(newVertices.Count <=2)
            {
             //   Destroy(cells[i]);
             //   cells.RemoveAt(i);
             //   continue;
            }

            //create new mesh

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

            Mesh mesh = new Mesh();
            mesh.vertices = newVertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.RecalculateNormals();

            bool test = false;
            if (test)
            {
                //create test body
                GameObject cell = new GameObject();
                cell.AddComponent<MeshRenderer>().sharedMaterial = Resources.Load("White") as Material;

                cell.AddComponent<MeshFilter>().sharedMesh = mesh;
            }
            else
            {
                //replace cell's mesh with new welded one
                cells[i].GetComponent<MeshFilter>().mesh = mesh;
                //also replace extrude cell's "original" mesh with this one

                // Debug.Log("mesh count before update");
             
            }

            if (interior)
            {
             //   Debug.Log("after = " + cells[i].GetComponent<MeshFilter>().mesh.vertexCount);
            }
        }


       
    }

    void RemoveSmallEdges()
    {
        for (int a = 0; a < cells.Count; a++)
        {
           // Debug.Log("before cell " + a.ToString() +", edge count = " + cells[a].GetComponent<AdjacentCells>().edges.Count);
        }
        //now we need to look for small edges
        for (int a = 0; a < cells.Count; a++)
        {
            List<List<int>> edges = cells[a].GetComponent<AdjacentCells>().edges;
            Vector3[] vertices = cells[a].GetComponent<MeshFilter>().mesh.vertices;
            for (int i = 0; i < edges.Count; i++)
            {
                Vector3 p0 = vertices[edges[i][0]];
                Vector3 p1 = vertices[edges[i][1]];

                float distance = Vector3.Distance(p0, p1);
                if (distance < minEdgeSize)
                {

                    if (interior)
                    {
                        /*
                        GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        c.transform.position = p0;
                        c.name = a.ToString() + " " + i.ToString() + " 0 first";
                        c.transform.parent = cells[a].transform;
                        c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        c.transform.position = p1;
                        c.name = a.ToString() + " " + i.ToString() + " 1 ";
                        c.transform.parent = cells[a].transform;
                        //find all other vertices which equal this
                        */
                    }
                    //add this
                    List<List<int>> toMove = new List<List<int>>();
                    toMove.Add(new List<int>() { a, i });
                    for (int b = 0; b < cells.Count; b++)
                    {
                        if (a == b)
                            continue;

                        Vector3[] otherVertices = cells[b].GetComponent<MeshFilter>().mesh.vertices;
                        List<List<int>> otherEdges = cells[b].GetComponent<AdjacentCells>().edges;
                        for (int j = 0; j < otherEdges.Count; j++)
                        {
                            Vector3 q0 = otherVertices[otherEdges[j][0]];
                            Vector3 q1 = otherVertices[otherEdges[j][1]];

                            // if (p0 == q0 && p1 == q1 || p0 == q1 && p1 == q0) ////////////working (p0 == q0 && p1 == q1)
                            if (Vector3.Distance(p0, q0) < tolerance && Vector3.Distance(p1, q1) < tolerance
                                || Vector3.Distance(p0, q1) < tolerance && Vector3.Distance(p1, q0) < tolerance)
                            {
                                //we have a match

                                // c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                                // c.transform.position = q0;
                                //  c.name = b.ToString() + " " + j.ToString() + " 0 second";
                                //  c.transform.parent = cells[b].transform;

                                toMove.Add(new List<int>() { b, j });
                                //c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                                //c.transform.position = q1;
                                //c.name = b.ToString() + " " + j.ToString() + " 1 ";
                                //  Vector3 movedPos = q0 + Vector3.up * 10f;

                                //  otherVertices[otherEdges[j][0]] = movedPos;
                                //  cells[b].GetComponent<MeshFilter>().mesh.vertices = otherVertices;
                            }
                        }

                        for (int x = 0; x < toMove.Count; x++)
                        {
                            Vector3[] verticesToMove = cells[toMove[x][0]].GetComponent<MeshFilter>().mesh.vertices;
                            List<List<int>> edgesToMove = cells[toMove[x][0]].GetComponent<AdjacentCells>().edges;

                            p0 = verticesToMove[edgesToMove[toMove[x][1]][0]];
                            p1 = verticesToMove[edgesToMove[toMove[x][1]][1]];

                            Vector3 centre = Vector3.Lerp(p0, p1, 0.5f);
                            //parallel lists - other fixes we need to make
                            List<GameObject> cellsToFix = new List<GameObject>();
                            List<int> vertsToFix = new List<int>();
                            List<Vector3> targetsForFix = new List<Vector3>();

                            //there may be solo vertices still not moved, search for them now

                            for (int y = 0; y < cells.Count; y++)
                            {
                                //skip our own cell
                                if (cells[toMove[x][0]] == cells[y])
                                    continue;

                                //List<List<int>> edges = cells[x].GetComponent<Wall>().edges;
                                Vector3[] verticesOther = cells[y].GetComponent<MeshFilter>().mesh.vertices;

                                for (int z = 0; z < verticesOther.Length; z++)
                                {
                                    if (verticesOther[z] == p0 || verticesOther[z] == p1)
                                    {
                                        //            vertices[i] = centre;//remember and mvoe later
                                        cellsToFix.Add(cells[y]);
                                        vertsToFix.Add(z);
                                        targetsForFix.Add(centre);

                                    }
                                }

                            }


                            //using parallel list tomove points
                            for (int y = 0; y < cellsToFix.Count; y++)
                            {

                                Vector3[] verticesToFix = cellsToFix[y].GetComponent<MeshFilter>().mesh.vertices;
                                verticesToFix[vertsToFix[y]] = targetsForFix[y];
                                cellsToFix[y].GetComponent<MeshFilter>().mesh.vertices = verticesToFix;

                                /*GameObject c0 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                                c0.transform.position = targetsForFix[y];
                                c0.transform.parent = cellsToFix[y].transform;
                                c0.name = "solo";
                                */
                            }


                            /*
                            GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                            c.transform.position = p0;
                            c.transform.parent = cells[toMove[x][0]].transform;
                            c.name = "0";

                            c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                            c.transform.position = p1;
                            c.name = "1";
                            c.transform.parent = cells[toMove[x][0]].transform;

                            c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                            c.transform.position = centre;
                            c.name = "c";
                            c.transform.parent = cells[toMove[x][0]].transform;

                        */

                            verticesToMove[edgesToMove[toMove[x][1]][0]] = centre;
                            verticesToMove[edgesToMove[toMove[x][1]][1]] = centre;

                            // now we use these edges to make a new mesh

                            cells[toMove[x][0]].GetComponent<MeshFilter>().mesh.vertices = verticesToMove;

                        }
                        //move first pos
                        //   Vector3 movedPosA = p0 + Vector3.up * 10;
                        //  vertices[edges[i][0]] = movedPosA;
                        //  cells[a].GetComponent<MeshFilter>().mesh.vertices = vertices;
                    }


                }
            }
        }

        //create meshes again
        ReMesh(true);
        //and work out edges for each cell again now we have changed them
        Edges();

        for (int a = 0; a < cells.Count; a++)
        {
          //  Debug.Log("after cell " + a.ToString() + cells[a].GetComponent<AdjacentCells>().edges.Count);
        }
        
    }


    public void Edges()
    {
        //makes list of edges for each cell and removes any that are too small

        for (int a = 0; a < cells.Count; a++)
        {
            //hold edge info in wall script in cell 
            if (cells[a].GetComponent<AdjacentCells>() == null)
                cells[a].AddComponent<AdjacentCells>();

            AdjacentCells aJ = cells[a].GetComponent<AdjacentCells>();

            //this will figure out edges and save them on the script

            aJ.Edges();
        }

        //now we have worked out all edges, find adjacent edges
        for (int a = 0; a < cells.Count; a++)
        {
          //  cells[a].GetComponent<AdjacentCells>().FindSharedEdges(); //using?
        }

    }

    void CallAdjacents()
    {
        for (int i = 0; i < cells.Count; i++)
        {
            cells[i].GetComponent<AdjacentCells>().FindSharedEdges();
        }
    }

    float FindShortestEdgeDistance(DualGraph dualGraph)
    {
        float shortestDistance = Mathf.Infinity;

        Vector3 p0;
        Vector3 p1;

        int shortestIndexI = 0;
        int shortestIndexJa = 0;
        int shortestIndexJb = 0;
        for (int i = 0; i < dualGraph.cells.Count; i++)
        {
            if (!dualGraph.cells[i].root && !dualGraph.cells[i].IsOpenEdge())
            {
                for (int j = 0; j < dualGraph.cells[i].mesh.verts.Length; j++)
                {
                    if (j == 0)
                        continue;

                    int nextIndex = j + 1;
                    //looping but skipping 0 central point
                    if (nextIndex > dualGraph.cells[i].mesh.verts.Length - 1)
                        nextIndex = 1;

                    p0 = (dualGraph.cells[i].mesh.verts[j]);
                    p1 = (dualGraph.cells[i].mesh.verts[nextIndex]);

                    /*
                    GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    c.transform.position = p0;// dualGraph.cells[shortestIndexI].mesh.verts[shortestIndexJa];
                    c.name = i.ToString() +" "+ j.ToString();// "shortest A";

                    c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    c.transform.position = p1;//dualGraph.cells[shortestIndexI].mesh.verts[shortestIndexJb];
                    c.name = c.name = i.ToString()+" " + nextIndex.ToString();
                    */

                    float d = Vector2.Distance(p0, p1);

                    if (d < shortestDistance)
                    {
                        shortestIndexI = i;
                        shortestIndexJa = j;
                        shortestIndexJb = nextIndex;
                        shortestDistance = d;
                    }
                }
            }
        }
        /*
        GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
        c.transform.position = dualGraph.cells[shortestIndexI].mesh.verts[shortestIndexJa];
        c.name = shortestIndexI.ToString() + " " + shortestIndexJa.ToString();// "shortest A";

        c = GameObject.CreatePrimitive(PrimitiveType.Cube);
        c.transform.position = dualGraph.cells[shortestIndexI].mesh.verts[shortestIndexJb];
        c.name = shortestIndexI.ToString() + " " + shortestIndexJb.ToString();// 
        */
        return shortestDistance;
    }



    /// <summary>
    /// Generates random cells.
    /// </summary>
    /// <param name="p">P.</param>
    /// <summary>
    /// Generates random cells.
    /// </summary>
    /// <param name="p">P.</param>
    private void GenRandCells(ref Vector3[] p){		

		List<Vector3> tempList = new List<Vector3>();

	/*	for (int i = 0; i <gridPlayer.Path.Count;i++)
		{
			Vector3 position = gridPlayer.Path[i] - transform.position;
			position.y = 0f;
			tempList.Add(position);

		}

		for(int i=0; i<p.Length - gridPlayer.Path.Count; i++){

			Vector3 position = new Vector3(UnityEngine.Random.Range(-volume.x,volume.x),0.0f,UnityEngine.Random.Range(-volume.z,volume.z));
			tempList.Add(position);
			//p[i]= new Vector3(Random.Range(-volume.x,volume.x),0.0f,Random.Range(-volume.z,volume.z));
		}

		p = tempList.ToArray();

*/
		if(fillWithPoints)
		{
			//cellNumber = yardPoints.Count;
			
			for(int i = 0; i < yardPoints.Count; i++)
			{					
				tempList.Add(yardPoints[i]);
			}

			p = tempList.ToArray();
		}
	}

    private void AddCentroids(ref Vector3[] p,List<Vector3> centroids)
    {

        List<Vector3> tempList = new List<Vector3>();

        /*	for (int i = 0; i <gridPlayer.Path.Count;i++)
            {
                Vector3 position = gridPlayer.Path[i] - transform.position;
                position.y = 0f;
                tempList.Add(position);

            }

            for(int i=0; i<p.Length - gridPlayer.Path.Count; i++){

                Vector3 position = new Vector3(UnityEngine.Random.Range(-volume.x,volume.x),0.0f,UnityEngine.Random.Range(-volume.z,volume.z));
                tempList.Add(position);
                //p[i]= new Vector3(Random.Range(-volume.x,volume.x),0.0f,Random.Range(-volume.z,volume.z));
            }

            p = tempList.ToArray();

    */
            //cellNumber = yardPoints.Count;

            for (int i = 0; i < centroids.Count; i++)
            {
                tempList.Add(centroids[i]);
            }

            p = tempList.ToArray();
        
    }

    /// <summary>
    /// Generates random cells, sorted by x value.
    /// </summary>
    /// <param name="points">Points.</param>
    //Note about sorting: using a sorted list requires the x values to always be different
    private void GenSortedRandCells(ref Vector3[] points){
		SortedList<float, Vector3> p= new SortedList<float,Vector3>();



		//adds random values for the rest
		if(fillWithRandom)
			{
			for(int i=0; i<cellNumber - roadList.Count; i++){
				Vector3 v = new Vector3(UnityEngine.Random.Range(-volume.x,volume.x),0.0f,UnityEngine.Random.Range(-volume.z,volume.z));
				try{
					p.Add(v.x, v);

				
				}
				catch(System.ArgumentException){
					i--;
					//Debug.Log("sort conflict");
				}
			}
			p.Values.CopyTo(points,0);
		}

		if(fillWithPoints)
		{
			//cellNumber = yardPoints.Count;

			for(int i = 0; i < centroids.Count; i++)
			{
				try{
					p.Add(centroids[i].x, centroids[i]);
				}
				catch(System.ArgumentException)
				{

					Array.Resize(ref points,points.Length-1);
					cellNumber-=1;
				}
			}
			p.Values.CopyTo(points,0);
		}
	}

    private void GenSortedRandCellsForInterior(ref Vector3[] points)
    {
        SortedList<float, Vector3> p = new SortedList<float, Vector3>();



      

        if (fillWithPoints)
        {
            cellNumber = yardPoints.Count;

            for (int i = 0; i < yardPoints.Count; i++)
            {
                try
                {
                    p.Add(yardPoints[i].x, yardPoints[i]);
                }
                catch (System.ArgumentException)
                {

                    Array.Resize(ref points, points.Length - 1);
                    cellNumber -= 1;
                }
            }
            p.Values.CopyTo(points, 0);
        }
    }
    /// <summary>
    /// Generates the mesh.
    /// </summary>
    void GenerateMesh(DualGraph dualGraph)
    {
    //    Debug.Log("prepare cells for mesh start");
        dualGraph.PrepareCellsForMesh();
        //yield return new WaitForEndOfFrame();
     //   Debug.Log("prepare cells for mesh end");
		if (graphMesh==null){
			graphMesh= new Mesh();
			graphMesh.name= "Graph Mesh";
		}
		else{
			//For the love of god, why are you calling this twice?!?!
			graphMesh.Clear();
		}

        meshVerts.Clear();
        meshTris.Clear();

	//	List<Vector3> vert= new List<Vector3>();
	//	List<Vector2> uvs= new List<Vector2>();
	//	List<int> tris= new List<int>();
	//	int vertCount=0;

	//	foreach(Cell c in dualGraph.cells)
     //   {
        for(int i = 0; i < dualGraph.cells.Count; i++)
        {
            //bottleneck protection
           // if(i!=0 && i % 100 == 0)
            //    yield return new WaitForEndOfFrame();




            List<Vector3> vert= new List<Vector3>();
			List<Vector2> uvs= new List<Vector2>();
			List<int> tris= new List<int>();
			int vertCount=0;
			if(!dualGraph.cells[i].root && !dualGraph.cells[i].IsOpenEdge()){						//use only interior until faux edges are added
				if(dualGraph.cells[i].IsOpenEdge()){
					Debug.Log("open edge");
				}

                for (int a = 0; a < dualGraph.cells[i].mesh.verts.Length; a++)
                {

                    //Debug.Log("in verts");
                    vert.Add(dualGraph.cells[i].mesh.verts[a] + transform.position);

                   // GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                   // c.transform.position= dualGraph.cells[i].mesh.verts[a];
                   // c.name = a.ToString();

                }
                foreach (Vector2 v in dualGraph.cells[i].mesh.uv){
					uvs.Add(v);
                   // Debug.Log("in uv");
                }

				for(int j = 2; j < dualGraph.cells[i].mesh.verts.Length; j++){
					tris.Add(vertCount);
					tris.Add(vertCount + j - 1);
					tris.Add(vertCount + j);
				}

				//finishing the loop
				tris.Add(vertCount);
				tris.Add(vertCount+ dualGraph.cells[i].mesh.verts.Length-1);
				tris.Add(vertCount+1);

				vertCount=vert.Count;
			}
			//Check for empty meshes and skip
			if (vert.Count == 0) continue;

			///Export to individual GameObject
		//	GameObject cell = new GameObject();

            
            //add mesh info to lists to crate mesh in a coroutine and drip feed in to unity
            //Mesh mesh = new Mesh();
		//	mesh.vertices = vert.ToArray();
        //    mesh.triangles = tris.ToArray();
          
            meshVerts.Add(vert.ToArray());
            meshTris.Add(tris.ToArray());       
    
            
		}

        //StartCoroutine("AddToCells");

       
    }

    void CellMeshes()
    {
        for (int i = 0; i < meshVerts.Count; i++)
        {
            if (meshVerts.Count == 0)
                continue;

            //create a game object for each cell in the mesh list
            GameObject cell = new GameObject();
            cell.transform.parent = this.gameObject.transform;
            cell.name = "Cell";
            
            // cell.tag = "Cell";


            //create a mesh from the already populated lists
            Mesh mesh = new Mesh();
            mesh.vertices = meshVerts[i];
            mesh.triangles = meshTris[i];
            mesh.RecalculateNormals();


            MeshFilter meshFilter = cell.AddComponent<MeshFilter>();
            meshFilter.mesh = mesh;


            MeshRenderer meshRenderer = cell.AddComponent<MeshRenderer>();
            meshRenderer.enabled = false;


            meshRenderer.sharedMaterial = Resources.Load("Ground") as Material;



            //bottleneck protection, build a 100 at a time
            //  if (i != 0 && i % 100 == 0)
            //       yield return new WaitForEndOfFrame();


            //master list of cells
            cells.Add(cell);


        }
    }

    void AddToCells()
    {
     
        GetComponent<BuildControl>().cells.Clear();

        for (int i = 0; i < cells.Count; i++)
        {
           // meshRenderer.sharedMaterial = Resources.Load("White") as Material;

            if (makeSkyscraperTraditional)
            {

                cells[i].AddComponent<TraditionalSkyscraper>();

                //add to build control list
                GetComponent<BuildControl>().cells.Add(cells[i]);
                //  cell.GetComponent<MeshRenderer>().enabled = false;


            }
        }

//        Debug.Log("cells count before extrude loop = " + cells.Count);
        //now we have found adjacents, we can scale cells
        for (int i = 0; i < cells.Count; i++)
        { //set layer here
            cells[i].layer = LayerMask.NameToLayer("Cells");
            if (extrudeCells)
            {
                ExtrudeCell ex = cells[i].AddComponent<ExtrudeCell>();
                ex.uniqueVertices = true;
                //call start straight away
                ex.Start();

                //also extrude any cells that were used to merge cells(we use them to compare points later)
                if (cells[i].GetComponent<MergeCell>() != null)
                {
                    MergeCell mergeCell = cells[i].GetComponent<MergeCell>();
                    for (int j = 0; j < mergeCell.previousCells.Count; j++)
                    {
                        mergeCell.previousCells[j].SetActive(true);
                        ExtrudeCell extrudeCell = mergeCell.previousCells[j].AddComponent<ExtrudeCell>();
                        extrudeCell.onlyScale = true;
                        extrudeCell.Start();
                        mergeCell.previousCells[j].SetActive(false);
                    }
                }

            }
        }

      
        if (doBuildControl)
            GetComponent<BuildControl>().enabled = true;

        // yield break;
    }

    public void CalculateAdjacents()
    {
        //work out which cells are adjacent to each cell, save in a list
        for (int i = 0; i < cells.Count; i++)
        {
            //set layer here
            cells[i].layer = LayerMask.NameToLayer("Cells");

            AdjacentCells.CalculateAdjacents(cells, cells[i], 0.1f);
            
        }
    }
    
    void SetPalletes()
    {
        //choose a starting main colour randomly

        float hue = 0f;
        bool strictColours = false;//**
        if (strictColours)
        {
            float hueChooser = 30 * UnityEngine.Random.Range(0, 12);
            hue = (1f / 360) * hueChooser;
        }
        else if (!strictColours)
        {
            //or use this to create non standard palettes - can movd away from main colours
            hue = 1f / 360 * (UnityEngine.Random.Range(0f, 360f));
        }

        

        //randomly choose saturation - will aplly to all cells
        float satChooser = UnityEngine.Random.Range(.5f, .8f);//still playin with these//and whether top put inside loop or not -- keeping outside of loop, tints and shades are there to get different tones
        float saturation = satChooser;

        //changing value makes it look darker or light-almost liekthe lighting engine does. keeping static
        float value = 1f;// UnityEngine.Random.Range(.5f, 1f);// 0.5f;

       

        int tintsAndShades = UnityEngine.Random.Range(3, 7);//randomimse?
        //run through cells
        for (int i = 0; i < cells.Count; i++)
        {
          

            if (cells[i].GetComponent<PaletteInfo>() == null)
            {
                //we havent set any colours for this cell
                PaletteInfo pI = cells[i].AddComponent<PaletteInfo>();
                //make a pallete for this cell
                //randomly choose hwat type of step is used to create the harmonies(degres on colour wheel)
                int harmonyStep = 30 * UnityEngine.Random.Range(1, 5);

                List<PaletteInfo.MaterialAndShades> palette = PaletteInfo.Palette(hue, saturation, value,harmonyStep, tintsAndShades, standardMaterial);
                pI.palette = palette;

                //now for each adjacent, create a pallete from one of the harmonious materials
                List<GameObject> adjacents = cells[i].GetComponent<AdjacentCells>().adjacentCells;
                for (int j = 0; j < adjacents.Count; j++)
                {

                    //loads of palletts this cell?
                    PaletteInfo adjacentPI = adjacents[j].AddComponent<PaletteInfo>();

                    //choose random harmony - or cluster colours by clamping to one  - or cluster to adjacent colours in the pallet (0,1,2) //or always contrast - last half of pallete
                    int randomHarmonyIndex = UnityEngine.Random.Range(0, palette.Count/2);//1;// UnityEngine.Random.Range(1, palette.Count);//start at 1 because 0 is main colour//UnityEngine.Random.Range(palette.Count/2, palette.Count )

                    int randomTintIndex = UnityEngine.Random.Range(0, tintsAndShades);

                    Color harmonyColour = palette[randomHarmonyIndex].material.color;                  

                    //get hue from this
                    Color.RGBToHSV(harmonyColour, out hue, out saturation, out value);
                    //now make a palette
                    harmonyStep = 30 * UnityEngine.Random.Range(1, 5);
                    adjacentPI.palette = PaletteInfo.Palette(hue, saturation, value, harmonyStep, tintsAndShades, standardMaterial);

                }
            }
        }
    }
}
