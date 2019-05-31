using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DualGraph2d;
using System;
using System.Linq;
using UnityEditor;

[RequireComponent (typeof(MeshFilter),typeof(MeshRenderer),typeof(MeshCollider))]
public class MeshGeneratorBU : MonoBehaviour {

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
    public bool useSortedGeneration;//=true;
	public bool drawCells=false;
	public bool drawDeluany=false;
	public bool drawRoots=false;
	public bool drawVoronoi=false;
	public bool drawGhostEdges=false;
	public bool drawPartialGhostEdge=false;
	public bool drawCircumspheres=false;
	public Color sphereColor= Color.cyan;
	

	public DualGraph dualGraph;
//	private float totalTime;
	private float computeTime;
	private Mesh graphMesh;

	
	public bool fillWithRandom;
	public bool fillWithPoints;

    public bool doBuildControl = false;
    public bool extrudeCells;
    public bool makeSkyscraper = true;
    public float threshold = 2f;
    public bool spawnPlayers = true;
	public List<Vector3> yardPoints = new List<Vector3>();
 //   public List<GameObject> cellsList = new List<GameObject>();
   // public List<Mesh> meshList = new List<Mesh>();
    public List<Vector3[]> meshVerts = new List<Vector3[]>();
    public List<int[]> meshTris = new List<int[]>();

    public int density = 5;

    public List<GameObject> cells = new List<GameObject>();
    //public List<List<GameObject>> adjacentCells = new List<List<GameObject>>();//moved t0 saving on each cell with AdjacentCells class
    public int adjacentCellsCount = 0;

	public void Start ()
    {
     
        //Go get points from Road Curve       

		dualGraph = new DualGraph(volume);


        cellNumber = (int)volume.x / density;

        StartCoroutine("GeneratePoints");
		


	}

    IEnumerator GeneratePoints()
    {

        Vector3[] points = new Vector3[cellNumber];

        if (useSortedGeneration)
            GenSortedRandCells(ref points);
        else
            GenRandCells(ref points);


        dualGraph.DefineCells(points, rootTriMultiplier);
    //    computeTime = Time.realtimeSinceStartup;

        if (useSortedGeneration)
            dualGraph.ComputeForAllSortedCells();
        else
            dualGraph.ComputeForAllCells();

      //  yield return new WaitForEndOfFrame();

        StartCoroutine("GenerateMesh");

       // Debug.Log("coroutine");

        yield break;
    }

    void AddToCells()
    {

        for(int i = 0; i < meshVerts.Count; i++)
        {
            //create a game object for each cell in the mesh list
            GameObject cell = new GameObject();
            cell.transform.parent = this.gameObject.transform;            
            cell.name = "Cell";
           // cell.tag = "Cell";
           

            //create a mesh from the already populated lists
            Mesh mesh = new Mesh();
            mesh.vertices = meshVerts[i];
            mesh.triangles = meshTris[i];

            MeshFilter meshFilter = cell.AddComponent<MeshFilter>();
            meshFilter.mesh = mesh;


            MeshRenderer meshRenderer = cell.AddComponent<MeshRenderer>();
       

            meshRenderer.sharedMaterial = Resources.Load("White") as Material;

            if (makeSkyscraper)
            {
                cell.AddComponent<SkyscraperFromVoronoiCell>();

                //add to build control list
                GetComponent<BuildControl>().cells.Add(cell);
              //  cell.GetComponent<MeshRenderer>().enabled = false;


            }

          
            //bottleneck protection, build a 100 at a time
          //  if (i != 0 && i % 100 == 0)
         //       yield return new WaitForEndOfFrame();


            //master list of cells
            cells.Add(cell);


        }

        //work out which cells are adjacent to each cell, save in a list
        for (int i = 0; i < cells.Count; i++)
        {
            //set layer here
            cells[i].layer = LayerMask.NameToLayer("Cells");

            List<GameObject> adjacents = new List<GameObject>();

            Vector3[] thisVertices = cells[i].GetComponent<MeshFilter>().mesh.vertices;
            for (int j = 0; j < cells.Count; j++)
            {
                //don't check own cell
                if (i == j)
                    continue;

                Vector3[] otherVertices = cells[j].GetComponent<MeshFilter>().mesh.vertices;

                for (int a = 0; a < thisVertices.Length; a++)
                {
                    for (int b = 0; b < otherVertices.Length; b++)
                    {
                        //if we have a match, add "other" cell to a list of adjacents for this cell
                        if(thisVertices[a] == otherVertices[b])
                        {
                            adjacents.Add(cells[j]);

                            //force out of the loops
                            a = thisVertices.Length;
                            break;
                        }
                    }
                }
            }

            //adjacentCells.Add(adjacents); //removing
            //add to list and save it on game object. Doing it this way allows us to hot reload, if we save it all in a list here, it won't serialize
           // AdjacentCells aj = cells[i].AddComponent<AdjacentCells>();
           // aj.adjacentCells = adjacents;
        }


        //now we have found adjacents, we can scale cells
        for (int i = 0; i < cells.Count; i++)
        {
            if (extrudeCells)
            {
                ExtrudeCell ex = cells[i].AddComponent<ExtrudeCell>();
                ex.uniqueVertices = true;
                //call start straight away
                ex.Start();
            }
        }

      
        if(doBuildControl)
        GetComponent<BuildControl>().enabled= true;

       // yield break;
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

			for(int i = 0; i < yardPoints.Count; i++)
			{
				try{
					p.Add(yardPoints[i].x,yardPoints[i]);
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
	/// <summary>
	/// Generates the mesh.
	/// </summary>
	IEnumerator GenerateMesh()
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

				foreach(Vector3 v in dualGraph.cells[i].mesh.verts){
                    //Debug.Log("in verts");
					vert.Add(v);
				}
				foreach(Vector2 v in dualGraph.cells[i].mesh.uv){
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

        StartCoroutine("AddToCells");


        yield break;
        
    }

	void OnDrawGizmos(){
		if(dualGraph!=null){
			if(drawCells){
				foreach(Cell c in dualGraph.cells){
					if(c.root){
						Gizmos.color=Color.red;
					}
					else{
						Gizmos.color= Color.blue;
					}
					Gizmos.DrawCube(c.point,Vector3.one);
				}
			}
			
			if (drawDeluany){
				foreach(Cell c in dualGraph.cells){
					foreach(VoronoiEdge e in c.edges){
						if (e.cellPair.root || c.root){
							if (drawRoots){
								Gizmos.color= Color.gray;
								Gizmos.DrawLine(c.point, e.cellPair.point);
							}
						}
						else{
							Gizmos.color= Color.green;
							Gizmos.DrawLine(c.point, e.cellPair.point);
						}
					}
				}
			}
			if (drawVoronoi){
				foreach(Cell c in dualGraph.cells){
					foreach (VoronoiEdge e in c.edges){
						if(e.isConnected){
							Gizmos.color=Color.black;
							if(e.ghostStatus==Ghosting.none){
								Gizmos.DrawLine(e.Sphere.Circumcenter, e.SpherePair.Circumcenter);
							}
							else if(drawGhostEdges){
								Gizmos.DrawLine(e.Sphere.Circumcenter, e.SpherePair.Circumcenter);
							}
							else if(drawPartialGhostEdge&& e.ghostStatus== Ghosting.partial){
								Gizmos.DrawLine(e.Sphere.Circumcenter, e.SpherePair.Circumcenter);
							}
						}
					}
				}
			}
			if (drawCircumspheres){
				Gizmos.color= sphereColor;
				foreach(Circumcircle c in dualGraph.spheres){
					Gizmos.DrawSphere(c.Circumcenter, c.circumradius);
				}
			}
			
		}
	}
}
