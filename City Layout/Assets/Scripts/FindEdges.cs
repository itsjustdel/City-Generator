using UnityEngine;
using System.Collections;
using System.Collections.Generic;




//http://answers.unity3d.com/questions/1019436/get-outeredge-vertices-c.html ///try this class instead
public class FindEdges : MonoBehaviour {
	public Edge[] outline;
	//public Edge[] sortedOutline;
	public Transform cubePrefab;
	public List<Edge> orderedEdges = new List<Edge>();
    public bool addBushesForCell = false;
    public List<EdgeHelpers.Edge> boundaryPath;
    public List<Vector3> pointsOnEdge = new List<Vector3>();
    public List<int> edgeVertices = new List<int>();
  //  public bool houseCell = false;
    public Mesh autoWeldedMesh;

    void Awake()
	{
		//enabled = false;
	}

	void Start()
	{

        ////to get a nice and clear mesh without too many points close to each other
        //make a mesh which has a high autoweld threshold. Too high to be a visual mesh - it will leave gaps

        //work out what edges are on the outside
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        Mesh mesh = meshFilter.mesh;

        autoWeldedMesh = new Mesh();
        autoWeldedMesh = mesh;

        autoWeldedMesh = AutoWeld.AutoWeldFunction(autoWeldedMesh, 1, 2000);

    //  meshFilter.mesh = autoWeldedMesh;
	//	outline = BuildManifoldEdges(meshFilter.mesh);

        //use the EdgeHelpers static class to work out the boundary could also be called in BushesForCell?
        boundaryPath = EdgeHelpers.GetEdges(autoWeldedMesh.triangles).FindBoundary().SortEdges();

        //use this new glued emsh to create a ring of edge points
        //fills pointsOnEdge
        CreateListOfEdgePoints(autoWeldedMesh);
        //PathOnEdge(boundaryPath);

    
       
    }

    /// <summary>
    /// Returns a list of ints. The edge Vertice number of the mesh passed to it.
    /// </summary>
    /// <param name="mesh"></param>
    /// <returns></returns>
    public static List<int> EdgeVertices(Mesh mesh,float threshold)
    {
        //returns a list of ints. Vertices numbers of the outsie of a mesh. Does not "complete the loop"

        List<int> edgeVertices = new List<int>();
        //use the EdgeHelpers static class to work out the boundary could also be called in BushesForCell?
        List<EdgeHelpers.Edge> boundaryPath = EdgeHelpers.GetEdges(mesh.triangles).FindBoundary().SortEdges();

        //use temporary array
        Vector3[] vertices = mesh.vertices;

        //-1, boundary edges completes the loop for us. we don't want any duplicates
        for (int i = 0; i < boundaryPath.Count - 1; i++)
        {
            if (i == 0)
            {
                //add first point regardless
                
                edgeVertices.Add(boundaryPath[i].v1);
                Vector3 pos = vertices[edgeVertices[0]];
                //check for duplicate on second, add if not
                if (Vector3.Distance(pos, vertices[boundaryPath[i].v2]) >= threshold)
                {
                    edgeVertices.Add(boundaryPath[i].v2);
                }

            }
            else if (i > 0)
            {
                Vector3 pos = vertices[boundaryPath[i].v2];
                Vector3 lastAdded = vertices[edgeVertices[edgeVertices.Count - 1]];
                //check for leftover duplicate left by autoweld
                //if we find one, we are done, we have made it back to the start of the loop
                if (pos == vertices[edgeVertices[0]])
                {
                    //we have found the first point. Don't add it
                    break;
                }

                //if this does not match the previous entry, add it
                //autowelding the mesh doesn not delete all triangels/vertices that arent used so boundary path can get confused
                
                //not autowelding at higher threshold- so this may not be necessary
                if (Vector3.Distance(pos, lastAdded) >= threshold)
                    edgeVertices.Add(boundaryPath[i].v2);

                // edgeVertices.Add(boundaryPath[i].v2);//anything using this now? was deform mesh stitch
            }
            
        }

        return edgeVertices;
    }

    void CreateListOfEdgePoints(Mesh mesh)
    {
        //creates a list of points round the edge of a field/cell with no duplicates or poitns within one unity of each other
       // Mesh mesh = GetComponent<MeshFilter>().mesh;
        //centre point on renderer
        //Used to find out which way the bushes should move towards so that they leave a space around the edge of the field
        Vector3 centrePoint = GetComponent<MeshRenderer>().bounds.center;
        //use temporary array
        Vector3[] vertices = mesh.vertices;

        //-1, boundary edges completes the loop for us. we don't want any duplicates
        for (int i = 0; i < boundaryPath.Count - 1; i++)
        {
            if (i == 0)
            {
                //add first pint regardless
                pointsOnEdge.Add(vertices[boundaryPath[i].v1]);

                Vector3 pos = pointsOnEdge[0];
                //check for duplicate on second, add if not
                if (Vector3.Distance(pos, vertices[boundaryPath[i].v2]) > 0.1f)
                {
                    pointsOnEdge.Add(vertices[boundaryPath[i].v2]);
                }

            }
            else if (i > 0)
            {
                Vector3 pos = vertices[boundaryPath[i].v2];
                //check for leftover duplicate left by autoweld
                //if we find one, we are done, we have made it back to the start of the loop
                if (pos == pointsOnEdge[0])
                {
                    //add the last point so it easier to find the intersection points
                    //other wise we would need to reverse a direction fromt he 2nd point to the 1st to get a driectional vector to find a point on
                    pointsOnEdge.Add(pos);
                    break;
                }
                    
                
          
                //if this does not match the previous entry, add it
                //autowelding the mesh doesn not delete all triangels/vertices that arent used so boundary path can get confused
                   if (Vector3.Distance(pos, pointsOnEdge[pointsOnEdge.Count - 1]) > 0.1f)
                    pointsOnEdge.Add(vertices[boundaryPath[i].v2]);

                // edgeVertices.Add(boundaryPath[i].v2);//anything using this now? was deform mesh stitch
            }

            //the autoweld script leaves some vertices at the end sometimes, so we need to check if we have finished our loop
            //do this by checking the distance to the first vertice
            //do not do on first 2 points
/*
            if (i < 2)
                continue;

            if (Vector3.Distance(pointsOnEdge[0], pointsOnEdge[pointsOnEdge.Count - 1]) < 0.5f)//
            {
                //jump out the for loop
                break;
            }
            */
        }
            //not doing below, using higher threshold autoweld

       

            
                    


        foreach(Vector3 v3 in pointsOnEdge)
        {
     //       GameObject cube2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
    //              cube2.name = "find edges cube";
    //        cube2.transform.position = v3;
        }

        foreach (int i in edgeVertices)//not working?
        {
           //      GameObject cube2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
           //      cube2.name = "find edges cube";
           //     cube2.transform.position = GetComponent<MeshFilter>().mesh.vertices[i] + transform.position;
        }


    }

    void PathOnEdge(List<EdgeHelpers.Edge> path)
    {


        List<FindEdges.Edge> outline = GetComponent<FindEdges>().orderedEdges;
        Mesh mesh = GetComponent<MeshFilter>().mesh;
        Vector3[] vertices = mesh.vertices;

        for (int i = 0; i <path.Count; i++)
        {

            List<Vector3> pathVertices = new List<Vector3>();
            List<int> pathTriangles = new List<int>();

            Vector3 p1 = vertices[path[i].v1];
            Vector3 p2 = vertices[path[i].v2];
            float distance = Vector3.Distance(p1, p2);

            for (float j = 0; j <= distance; j++) //plus here grid size  e.g j+gridscale
            {
                for (float k = 0; k < 2; k++)//plus here grid size  e.g j+gridscale
                {
                    //   if (j > distance - 2)//skip last part to stop overlap
                    //      continue;

                    Vector3 dirZ = p2 - p1;
                    dirZ.Normalize();

                    Vector3 dirX = Quaternion.Euler(0f, 90f, 0f) * dirZ;//this might need to point to middle instead of just right(fields)

                    Vector3 pos = p1 + (dirZ * j) + (dirX * k);
                    Vector3 pos2 = p1 + (dirZ * (j + 1)) + (dirX * k);

                    Vector3 pos3 = p1 + (dirZ * j) + (dirX * (k + 1));
                    Vector3 pos4 = p1 + (dirZ * (j + 1)) + (dirX * (k + 1));

                    /*
                    GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cube.transform.position = pos;
                    cube.name = "Grid";
                    cube.transform.parent = transform;
                    cube.transform.localScale *= 0.1f;

                    GameObject cube2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cube2.transform.position = pos2;
                    cube2.name = "Grid2";
                    cube2.transform.parent = transform;
                    cube2.transform.localScale *= 0.1f;

                    GameObject cube3 = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cube3.transform.position = pos3;
                    cube3.name = "Grid3";
                    cube3.transform.parent = transform;
                    cube3.transform.localScale *= 0.1f;

                    GameObject cube4 = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cube4.transform.position = pos4;
                    cube4.name = "Grid4";
                    cube4.transform.parent = transform;
                    cube4.transform.localScale *= 0.1f;
                    */

                    //add a quad's worth of vertices
                    pathVertices.Add(pos);
                    pathVertices.Add(pos2);
                    pathVertices.Add(pos3);
                    pathVertices.Add(pos4);


                    /*grrr
                    //add triangles for the quad we jsut added for loop below is +=4
                    int index = (int)((j*4)+(k*4));
                    pathTriangles.Add(index + 0);
                    pathTriangles.Add(index + 1);
                    pathTriangles.Add(index + 2);

                    pathTriangles.Add(index + 3);
                    pathTriangles.Add(index + 2);
                    pathTriangles.Add(index + 1);
                    */
                }


            }

            for (int t = 0; t < pathVertices.Count - 2; t += 4)
            {
                pathTriangles.Add(t + 0);
                pathTriangles.Add(t + 1);
                pathTriangles.Add(t + 2);

                pathTriangles.Add(t + 3);
                pathTriangles.Add(t + 2);
                pathTriangles.Add(t + 1);
            }


            Mesh pathMesh = new Mesh();
            pathMesh.vertices = pathVertices.ToArray();
            pathMesh.triangles = pathTriangles.ToArray();

            GameObject pathway = new GameObject();
            pathway.transform.parent = transform;
            MeshFilter meshFilter = pathway.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = pathway.AddComponent<MeshRenderer>();
            meshRenderer.material = Resources.Load("Path", typeof(Material)) as Material;

            meshFilter.mesh = pathMesh;
        }


    }

    void OrderEdges()
	{
		//Mesh mesh= GetComponent<MeshFilter>().mesh;

		//check the last item in orderedEdgesist (not outline)
		orderedEdges.Add(outline[0]);
		Edge edge = orderedEdges[orderedEdges.Count-1];

		//check 1st edge against all other edges, if ends are at the same position
		for (int i = 0; i < outline.Length;i++)
		{
			//check the end of one edge vs the start of all others
			if ( edge.vertexIndex[1] == outline[i].vertexIndex[0] )
			{
				//add successful match to orderedEdgesist
				orderedEdges.Add(outline[i]);
				//make succesfully matched edge the new edge to be checked against (build the chain)
				edge = outline[i];
				//reset the index to make sure it checks all edges, not just the rest of the unchecked ones
				i=0;
			}
          
		}
       
    }



	/// Builds an array of edges that connect to only one triangle.
	/// In other words, the outline of the mesh    
	public Edge[] BuildManifoldEdges(Mesh mesh)
	{
		// Build a edge list for all unique edges in the mesh
		Edge[] edges = BuildEdges(mesh.vertexCount, mesh.triangles);
		
		// We only want edges that connect to a single triangle
		ArrayList culledEdges = new ArrayList();
		foreach (Edge edge in edges)
		{
			if (edge.faceIndex[0] == edge.faceIndex[1])
			{
				culledEdges.Add(edge);
			}
		}
		
		return culledEdges.ToArray(typeof(Edge)) as Edge[];
	}
	
	/// Builds an array of unique edges
	/// This requires that your mesh has all vertices welded. However on import, Unity has to split
	/// vertices at uv seams and normal seams. Thus for a mesh with seams in your mesh you
	/// will get two edges adjoining one triangle.
	/// Often this is not a problem but you can fix it by welding vertices 
	/// and passing in the triangle array of the welded vertices.
	public static Edge[] BuildEdges(int vertexCount, int[] triangleArray)
	{
		int maxEdgeCount = triangleArray.Length;
		int[] firstEdge = new int[vertexCount + maxEdgeCount];
		int nextEdge = vertexCount;
		int triangleCount = triangleArray.Length / 3;
		
		for (int a = 0; a < vertexCount; a++)
			firstEdge[a] = -1;
		
		// First pass over all triangles. This finds all the edges satisfying the
		// condition that the first vertex index is less than the second vertex index
		// when the direction from the first vertex to the second vertex represents
		// a counterclockwise winding around the triangle to which the edge belongs.
		// For each edge found, the edge index is stored in a linked list of edges
		// belonging to the lower-numbered vertex index i. This allows us to quickly
		// find an edge in the second pass whose higher-numbered vertex index is i.
		Edge[] edgeArray = new Edge[maxEdgeCount];
		
		int edgeCount = 0;
		for (int a = 0; a < triangleCount; a++)
		{
			int i1 = triangleArray[a * 3 + 2];
			for (int b = 0; b < 3; b++)
			{
				int i2 = triangleArray[a * 3 + b];
				if (i1 < i2)
				{
					Edge newEdge = new Edge();
					newEdge.vertexIndex[0] = i1;
					newEdge.vertexIndex[1] = i2;
					newEdge.faceIndex[0] = a;
					newEdge.faceIndex[1] = a;
					edgeArray[edgeCount] = newEdge;
					
					int edgeIndex = firstEdge[i1];
					if (edgeIndex == -1)
					{
						firstEdge[i1] = edgeCount;
					}
					else
					{
						while (true)
						{
							int index = firstEdge[nextEdge + edgeIndex];
							if (index == -1)
							{
								firstEdge[nextEdge + edgeIndex] = edgeCount;
								break;
							}
							
							edgeIndex = index;
						}
					}
					
					firstEdge[nextEdge + edgeCount] = -1;
					edgeCount++;
				}
				
				i1 = i2;
			}
		}
		
		// Second pass over all triangles. This finds all the edges satisfying the
		// condition that the first vertex index is greater than the second vertex index
		// when the direction from the first vertex to the second vertex represents
		// a counterclockwise winding around the triangle to which the edge belongs.
		// For each of these edges, the same edge should have already been found in
		// the first pass for a different triangle. Of course we might have edges with only one triangle
		// in that case we just add the edge here
		// So we search the list of edges
		// for the higher-numbered vertex index for the matching edge and fill in the
		// second triangle index. The maximum number of comparisons in this search for
		// any vertex is the number of edges having that vertex as an endpoint.
		
		for (int a = 0; a < triangleCount; a++)
		{
			int i1 = triangleArray[a * 3 + 2];
			for (int b = 0; b < 3; b++)
			{
				int i2 = triangleArray[a * 3 + b];
				if (i1 > i2)
				{
					bool foundEdge = false;
					for (int edgeIndex = firstEdge[i2]; edgeIndex != -1; edgeIndex = firstEdge[nextEdge + edgeIndex])
					{
						Edge edge = edgeArray[edgeIndex];
						if ((edge.vertexIndex[1] == i1) && (edge.faceIndex[0] == edge.faceIndex[1]))
						{
							edgeArray[edgeIndex].faceIndex[1] = a;
							foundEdge = true;
							break;
						}
					}
					
					if (!foundEdge)
					{
						Edge newEdge = new Edge();
						newEdge.vertexIndex[0] = i1;
						newEdge.vertexIndex[1] = i2;
						newEdge.faceIndex[0] = a;
						newEdge.faceIndex[1] = a;
						edgeArray[edgeCount] = newEdge;
						edgeCount++;
					}
				}
				
				i1 = i2;
			}
		}
		
		Edge[] compactedEdges = new Edge[edgeCount];
		for (int e = 0; e < edgeCount; e++)
			compactedEdges[e] = edgeArray[e];
		
		return compactedEdges;
	}


public class Edge
{
	// The index to each vertex
	public int[] vertexIndex = new int[2];
	// The index into the face.
	// (faceindex[0] == faceindex[1] means the edge connects to only one triangle)
	public int[] faceIndex = new int[2];
}

}
