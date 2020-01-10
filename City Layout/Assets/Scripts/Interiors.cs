using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
public class Interiors : MonoBehaviour
{

    public List<int> isolates = new List<int>();


    public List<GameObject> areas = new List<GameObject>();
    public List<Vector3> ringPoints = new List<Vector3>();
    public List<Vector3> cornerPoints = new List<Vector3>();

    
    public int corners = 0;
    public float doorWidth = 1f;
    public float hallWidth = 2f;

    public bool snip;
    public bool fullReset; 
    GameObject interiorObj;
    int frames = 0;

    private void Start()
    {
        //
        CreateVoronoi();

        //need to wait for voronoi to finish before snipping

        //force update loop to pick up on next render
        snip = true;
    }

    private void Update()
    {
        if(snip)
        {

           // ShowCubesOnRing(gameObject, ringPoints);
            if (frames > 1)
            {


                FloorPlan();
                snip = false;
            }

            frames++;

            //now check for line intersections and adjust mesh
        }

        if(fullReset)
        {
            ClearAndReset();
            fullReset = false;
        }
    }

    public static void ShowCubesOnRing(GameObject gameObject, List<Vector3> ringPoints)
    {
        for (int i = 0; i < ringPoints.Count; i++)
        {
            GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
            c.transform.position = ringPoints[i];// - gameObject.transform.position;
            c.name = "cubes on ring";
            Destroy(c, 3);
        }
    }

    public void CreateVoronoi()
    {

        List<Vector3> ringPoints = cornerPoints;
        //add halfway to ringpoints
        List<Vector3> ringPointsHalfway = new List<Vector3>();
        ringPointsHalfway.Add(ringPoints[0]);
        for (int i = 1; i < ringPoints.Count; i++)
        {
            ringPointsHalfway.Add(Vector3.Lerp(ringPoints[i - 1], ringPoints[i], 0.5f));

            ringPointsHalfway.Add(ringPoints[i]);
        }
        ringPointsHalfway.Add(Vector3.Lerp(ringPoints[0], ringPoints[ringPoints.Count-1], 0.5f));

        ringPoints = new List<Vector3>(ringPointsHalfway);
        


        interiorObj = new GameObject();
        interiorObj.name = "Interior";
        interiorObj.transform.parent = gameObject.transform;
        interiorObj.transform.position = gameObject.transform.position;

        MeshGenerator mg = interiorObj.AddComponent<MeshGenerator>();

        List<Vector3> pointsInside = new List<Vector3>();
        
        //find random positions within floor

        Vector3 shootFrom = gameObject.transform.position - 10f * Vector3.up;
        //GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
        //c.transform.position = shootFrom;
        GameObject underSide = gameObject.transform.Find("UnderSide").gameObject;
        underSide.AddComponent<MeshCollider>();
        int cap = (int)((underSide.GetComponent<MeshRenderer>().bounds.extents.x + underSide.GetComponent<MeshRenderer>().bounds.extents.z )*0.5f);// 5;
        cap = corners/2 +1;
        //cap = Random.Range(3, 10);
        //?work needs done for this
        //add random
        RaycastHit hit;
        float limit = (underSide.GetComponent<MeshRenderer>().bounds.extents.x + underSide.GetComponent<MeshRenderer>().bounds.extents.z)*.5f;
        int a = 0;
        for (int i = 0; i < cap; i++)
        {
            a++;
            if(a>100)
            {
                Debug.Log("SAFETY, yard points = " + mg.yardPoints.Count);

                //too small for rooms, just add main flor for full room and return - to do
                mg.enabled = false;//don't do just now- will need to make a method for this - make two small rooms from middle point?
                
                return;
            }
            Vector2 modV2 = Random.insideUnitCircle;
            Vector3 modV3 = new Vector3(modV2.x, 0f, modV2.y);
            //create a float with average size of bounds
            modV3 *= limit;
           // GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
           // c.transform.position = shootFrom + modV3;
           // c.name = "shootfrom";
            bool add = false;
            Vector3 toAdd = Vector3.zero;
            if (Physics.Raycast(shootFrom + modV3, Vector3.up, out hit, 20f, LayerMask.GetMask("Roof")))
            {
                if (hit.transform == underSide.transform)
                {
                   // c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                   // c.transform.position = hit.point;
                   // c.name = "shootfrom hit " + i.ToString(); ;

                    //can be optimised by using triangles https://gis.stackexchange.com/questions/6412/generate-points-that-lie-inside-polygon  //**OPTO
                    bool allHit = true;
                    for (int j = 0; j < 360; j += 360 / 6)
                    {
                        Vector3 arm = Quaternion.Euler(0, j, 0) * Vector3.right;
                        Vector3 armShootFrom = shootFrom + modV3 + (arm * hallWidth * 2);

                        if (!Physics.Raycast(armShootFrom, Vector3.up, out hit, 20f, LayerMask.GetMask("Roof")))
                        {
                            //if it doesnt hit this object

                           // c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                           // c.transform.position = armShootFrom;
                           // c.name = "MISS " + i.ToString(); ;
                            allHit = false;

                            break;
                        }
                        else
                        {
                           // c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                           // c.transform.position = armShootFrom;
                           // c.name = "Hit " + j.ToString(); ;
                        }

                    }

                    if (allHit)
                    {
                        add = true;

                        //points can't be too near a ring point - breaks interesect code
                        Vector3 zeroYHitPoint = shootFrom + modV3;
                        toAdd = new Vector3(zeroYHitPoint.x, 0f, zeroYHitPoint.z);
                    }
                }
            }

            if(add)
            {
                //c = GameObject.CreatePrimitive(PrimitiveType.Cube);
               // c.transform.position = toAdd;
               // c.name = "ADD " + i.ToString(); ;

                mg.yardPoints.Add(toAdd - transform.position);
            }
            else
                i--;
        }

        //add border points
        for (int i = 0; i < ringPoints.Count; i++)
        {
            Vector3 zeroGameObj = new Vector3(gameObject.transform.position.x, 0f, gameObject.transform.position.z);
            Vector3 p = ringPoints[i] -zeroGameObj;

            p.y = 0f;

            

            Vector3 dir = ringPoints[i] - zeroGameObj;

           // mg.yardPoints.Add(p);
           // mg.yardPoints.Add(p + dir);
            mg.yardPoints.Add(p + dir*5f);//how big?
            mg.yardPoints.Add(p + dir * 10f);
            //mg.yardPoints.Add(p - dir * 0.1f);
            // mg.yardPoints.Add(p - dir * 0.2f);

            //*** try and fit edge of voronoi pattern to edge of floor
        }


        for (int i = 0; i < 360; i+= 30)
        {
           // Vector3 p = Quaternion.Euler(0, i, 0) * Vector3.right * limit;
            //mg.yardPoints.Add(p);
           // mg.yardPoints.Add(p*1.1f);
        }

       // Destroy(c, 3);

        for (int i = 0; i < mg.yardPoints.Count; i++)
        {
          //  GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
         //   c.transform.position = mg.yardPoints[i] + interiorObj.transform.position;
         //   c.transform.localScale *= 0.5f;
            //Destroy(c, 3);
        }

        //mg.enabled = false;

        mg.minEdgeSize = hallWidth;
        mg.volume = Vector3.one * 1000;
        mg.lloydIterations = 1;
        mg.interior = true;
        mg.fillWithPoints = true; ;// mg.fillWithRandom = true;
        mg.weldCells = false;
        mg.walls = false;
        mg.makeSkyscraperTraditional = false;
        mg.useSortedGeneration = true;
        mg.doBuildControl = false;

    }

    void FloorPlan()
    {
        List<GameObject> interiorCells = transform.Find("Interior").GetComponent<MeshGenerator>().cells;
        //find where voronoi pattern intersects with building outline
        List<CellInfo> cellInfos = Intersects(interiorCells);
        //create rooms
        MakeCells(interiorCells, cellInfos);
    }    

    List<CellInfo> Intersects(List<GameObject> cellsToSnip)
    {
        List<CellInfo> cellInfos = new List<CellInfo>();
        for (int j = 0; j < cellsToSnip.Count; j++)//cellsToSnip.Count
        {
            //skip for tests
            if (!isolates.Contains(j) && isolates.Count > 0)
                continue;

            //disable snipped
            cellsToSnip[j].SetActive(false);
            cellsToSnip[j].name = j.ToString();

            Vector3[] vertices = cellsToSnip[j].GetComponent<MeshFilter>().mesh.vertices;

            List<int[]> edges = new List<int[]>();
            List<List<Vector3>> intersectsForEdges = new List<List<Vector3>>();
            List<List<int>> intersectIndexesForEdges = new List<List<int>>();

            //for each cell work out how many intersects (with the ring points) each edge has
            for (int k = 1; k < vertices.Length; k++)
            {
                int next = k + 1;
                if (next >= vertices.Length)
                    next -= vertices.Length - 1; //was 1



                Vector2 p0 = new Vector2(vertices[k].x, vertices[k].z);
                Vector2 p1 = new Vector2(vertices[next].x, vertices[next].z);


               // Debug.DrawLine(vertices[k], vertices[next], Color.white);

                /*
                    GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    c.transform.position = vertices[k];
                    c.name = "vertices k";

                    c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    c.transform.position = vertices[next];
                    c.name = "vertices next";
                  */

                List<Vector3> intersects = new List<Vector3>();
                List<int> intersectIndexes = new List<int>();
                for (int i = 0; i < ringPoints.Count; i++)//can use last found point instead of 1? **opto
                {
                    int nextRingPoint = i + 1;
                    if (nextRingPoint > ringPoints.Count - 1)
                        nextRingPoint -= ringPoints.Count;

                    //Debug.DrawLine(ringPoints[i - 1], ringPoints[i], Color.yellow);

                    Vector2 p2 = new Vector2(ringPoints[i].x, ringPoints[i].z);
                    Vector2 p3 = new Vector2(ringPoints[nextRingPoint].x, ringPoints[nextRingPoint].z);

                    Vector2 intersection = Vector2.zero;
                    //check for curve vs cell intersection

                    if (LineSegmentsIntersection(p0, p1, p2, p3, out intersection))
                    {
                        Vector3 intersectV3 = new Vector3(intersection.x, 0f, intersection.y);
                        
                        /*
                        GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        c.transform.position = vertices[k];
                        c.name = "Cell " + j.ToString() + "vertices k";

                        c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        c.transform.position = vertices[next];
                        c.name = "vertices next";
                        
                         c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        c.transform.position = intersectV3;
                        c.name = "interesect";
                        */

                        //keep track of how many times this edge has been intersected, and positions too

                        intersects.Add(intersectV3);
                        intersectIndexes.Add(i);


                    }
                }

                int[] edge = new int[2] { k, next };
                edges.Add(edge);

                intersectsForEdges.Add(intersects);
                //get these intersect in ring running order?
                //intersectIndexes.Sort();
                intersectIndexesForEdges.Add(intersectIndexes);

            }

            //save
            cellInfos.Add(new CellInfo(edges, intersectsForEdges, intersectIndexesForEdges, j));

        }
        return cellInfos;
    }

    void MakeCells(List<GameObject> cellsToSnip, List<CellInfo> cellInfos)
    {
        bool doCubes = false;//debug

        GameObject underSide = gameObject.transform.Find("UnderSide").gameObject;
        bool makeDoor = true;
        List<Vector3[]> edgesForHallway = new List<Vector3[]>();

        Vector3 doorEdgePos = Vector3.zero;
        bool doorEdgeFound0 = false;
        bool doorEdgeFound1 = false;
        for (int i = 0; i < cellInfos.Count; i++)
        {
            
            List<int[]> edges = cellInfos[i].edges;
            //List<List<int>> intersectIndexesForCell = cellInfos[i].intersectIndexesForEdges;
            //List<List<Vector3>> intersectsForCell = cellInfos[i].intersectsForEdges;
            //go through edges and add points whether it is ring points or edge points
           
            List<Vector3> newPoints = new List<Vector3>();

            Vector3[] vertices = cellsToSnip[cellInfos[i].cellNumber].GetComponent<MeshFilter>().mesh.vertices;
            for (int j = 0; j < edges.Count; j++)
            {
                int[] edge = edges[j];
                List<Vector3> intersects = cellInfos[i].intersectsForEdges[j];
                List<int> intersectIndexes = cellInfos[i].intersectIndexesForEdges[j];

                //Debug.DrawLine(vertices[edge[0]], vertices[edge[1]], Color.white);

                if (doCubes)
                {
                    GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    c.transform.position = vertices[edge[0]];
                    c.name = "vertice 0";
                    c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    c.transform.position = vertices[edge[1]];
                    c.name = "vertice 1";
                }


                RaycastHit hit;
                Vector3 shootFrom = vertices[edge[0]];
                shootFrom -= Vector3.up;

                //find out if we are starting from apoint inside the floor cell or not
                bool inside = false;
                if (Physics.Raycast(shootFrom, Vector3.up, out hit, 2f, LayerMask.GetMask("Roof")))
                {
                    if (hit.transform.gameObject == underSide)
                    {
                        inside = true;
                    }
                }


                //organise these ifs better

                if (intersects.Count == 0 || intersects.Count == 1)
                {
                    if(inside)
                    {
                        Debug.DrawLine(vertices[edge[0]], vertices[edge[1]], Color.red);//don't add
                    }

                    if (inside)
                    {
                        //starting inside
                        if (intersects.Count == 0)// && inside)
                        {
                            //had one case where intersect was marginal and was adding edge even though it was outside? expensive edge case check
                            bool insideSecond = false;
                            shootFrom = vertices[edge[1]];
                            shootFrom -= Vector3.up;
                            if (Physics.Raycast(shootFrom, Vector3.up, out hit, 2f, LayerMask.GetMask("Roof")))////*** if ditance check for voronoi placement, remove this
                            {
                                if (hit.transform.gameObject == underSide)
                                {
                                    insideSecond = true;
                                }
                            }
                            if (insideSecond)
                            {
                                Debug.DrawLine(vertices[edge[0]], vertices[edge[1]], Color.blue);//add
                                newPoints.Add(vertices[edge[0]]);
                                newPoints.Add(vertices[edge[1]]);//?-- this is a hall way
                                //add to list to work out after
                                edgesForHallway.Add(new Vector3[] { vertices[edge[0]], vertices[edge[1]] } );
                            }
                            else
                            {
                                Debug.Log("Remove marginal edge ? sometimes works sometimes doesnt - redo? voronoi pattern is right on ringpoints edge- could do a distance check when placing vor points - distance to ring point");
                                Debug.DrawLine(vertices[edge[0]], vertices[edge[1]], Color.yellow);//add
                            }

                        }

                        else if (intersects.Count == 1)// && inside)
                        {


                            Debug.DrawLine(vertices[edge[0]], intersects[0], Color.cyan);//add to intersection then look for ring points

                            //add intersection
                            newPoints.Add(intersects[0]);

                            //run and add to next intersection point
                            //next intersect point is on an edge on this cell
                            //start at the ring index of the intersects
                            int start = intersectIndexes[0];

                            AddRingPointsToNextIntersect(newPoints, cellInfos, i, j, start,intersects[0]);

                        }
                    }
                    else//starting outside
                    {

                        if (intersects.Count == 1)
                        {
                            Debug.DrawLine(vertices[edge[1]], intersects[0], Color.magenta);//add from intersection


                            newPoints.Add(intersects[0]);
                            newPoints.Add(vertices[edge[1]]);

                        }
                    }
                }

                else if (intersects.Count == 2)
                {
                    //organise intersects by distance from edge point[0]
                    if (Vector3.Distance(vertices[edge[0]], intersects[0]) > Vector3.Distance(vertices[edge[0]], intersects[1]))
                    {
                        Vector3 temp = intersects[0];
                        intersects[0] = intersects[1];
                        intersects[1] = temp;
                        //we also need to swap the parallel list paired with intersects
                        int tempInt = intersectIndexes[0];
                        intersectIndexes[0] = intersectIndexes[1];
                        intersectIndexes[1] = tempInt;
                    }

                    //starting outside and finsihing outside
                    if (!inside)
                    {
                        Debug.DrawLine(vertices[edge[0]], vertices[edge[1]], Color.red);//add from intersection
                        //first we need to organise intersects so that that first intersect is close to edge point [0]
                      
                        newPoints.Add(intersects[0]);
                        newPoints.Add(intersects[1]);

                        //now find ring points to next intersect
                        if (intersectIndexes.Count == 0)
                        {
                            //internal? kidney bean?
                        }
                        int start = intersectIndexes[1];

                        newPoints = AddRingPointsToNextIntersect(newPoints, cellInfos, i, j, start, intersects[1]);
                    }
                    //starting inside and finishin inside
                    else
                    {
                        Debug.Log("Rare Inside 2 intersects - debug line yellow - redo voronoi - two tiny rooms from one interior shape cell - not worth figuring out method");
    
                        Debug.DrawLine(vertices[edge[0]], vertices[edge[1]], Color.yellow);//add from intersection
                        ClearAndReset();
                        return;
                        /*
                        //add intersect
                        newPoints.Add(intersects[0]);

                        //add ring points to next intersect
                        int start = intersectIndexes[0];
                        newPoints = AddRingPointsToNextIntersect(newPoints, cellInfos, i, j, start, intersects[0]);

                        //now add edge[1]
                        newPoints.Add(vertices[edge[1]]);
                        */

                    }
                    
                }
                else if (intersects.Count ==3)
                {
                    Debug.DrawLine(vertices[edge[0]], vertices[edge[1]], Color.green);
                    //start from outside
                    if(!inside)
                    {

                        
                        //bubble sort ( is fine for 3 entries!)
                        Vector3 tempV3;
                        int tempInt;
                        for (int p = 0; p <= intersects.Count- 2; p++)
                        {
                            for (int q = 0; q <= intersects.Count- 2; q++)
                            {
                                if (Vector3.Distance(vertices[edge[0]], intersects[q]) > Vector3.Distance(vertices[edge[0]], intersects[ q + 1]))//distance eq
                                {
                                    //move parallel lists
                                    tempV3 = intersects[q + 1];
                                    intersects[q + 1] = intersects[q];
                                    intersects[q] = tempV3;

                                    tempInt = intersectIndexes[q + 1];
                                    intersectIndexes[q + 1] = intersectIndexes[q];
                                    intersectIndexes[q] = tempInt;
                                }
                            }
                        }


                        if (doCubes)
                        {
                            for (int k = 0; k < intersects.Count; k++)
                            {
                                GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                                c.transform.position = intersects[k];
                                c.transform.name = k.ToString();
                            }
                        }

                        //add first intersect
                        newPoints.Add(intersects[0]);
                        //run to next intersect
                        int start = intersectIndexes[0];
                        newPoints = AddRingPointsToNextIntersect(newPoints, cellInfos, i, j, start,intersects[0]);
                        //add next ring part (internal)
                        start = intersectIndexes[1];
                        newPoints = AddRingPointsToNextIntersect(newPoints, cellInfos, i, j, start, intersects[1]);

                        newPoints.Add(intersects[2]);
                        newPoints.Add(vertices[edge[1]]);

                    }
                    else//starts inside
                    {
                        //to do!
                        Debug.Log("inside3 - redo");
                        ClearAndReset();
                        if (doCubes)
                        {
                            GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                            c.transform.position = intersects[0];
                            c.name = "intersect 0";
                            c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                            c.transform.position = intersects[1];
                            c.name = "intersect 1";
                            c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                            c.transform.position = intersects[2];
                            c.name = "intersect 2";
                        }

                        return;
                    }
                }
                else if (intersects.Count == 4)
                {
                    Debug.DrawLine(vertices[edge[0]], vertices[edge[1]], Color.magenta);
                    Debug.Log("intersects == 4! I've seen it");
                }
            }

            //make cell mesh and object
            if (newPoints.Count > 0)
            {
                //create hallways
                int totalInteriorPoints = 0;
                int totalExteriorPoints = 0;

                for (int a = 0; a < newPoints.Count; a++)
                {
                    if (newPoints[a].y > 0f)
                        totalInteriorPoints++;
                    else
                        totalExteriorPoints++;
                }

                //**TODO DELLY
                //need to get remove small edges working
                //need to add edge slide to the other cell which is on the door side-

                newPoints = Hallways(vertices, cellInfos[i].intersectsForEdges, newPoints,ref makeDoor,ref doorEdgePos,ref doorEdgeFound0, ref doorEdgeFound1);


                if (newPoints.Count > 3)//needs 4
                {
                    GameObject area = Cell(newPoints);
                    areas.Add(area);
                }
              
            }
        }

  
    }

    private void ClearAndReset()
    {

        //clear any areas/rooms we have made
        for (int a = 0; a < areas.Count; a++)
        {
            Destroy(areas[a].GetComponent<MeshFilter>().sharedMesh);
            Destroy(areas[a]);
        }
        areas.Clear();

        //and run again
        Debug.Log("Restarting interior");
        //Start();
        Interiors interiors = gameObject.AddComponent<Interiors>();
        interiors.ringPoints = ringPoints;
        interiors.cornerPoints = cornerPoints;
        interiors.corners = corners;
        interiors.hallWidth = hallWidth;

        Destroy(interiorObj);
        Destroy(this);

    }

    List<Vector3> AddRingPointsToNextIntersect(List<Vector3> newPoints, List<CellInfo> cellInfos,int currentCellIndex,int currentEdgeIndex, int start, Vector3 currentIntersect)
    {
        bool doCubes = false;//debug
        bool found = false;

        //List<Vector3> intersects = cellInfos[currentCellIndex].intersectsForEdges[currentEdgeIndex];

        for (int i = 0; i < ringPoints.Count; i++)
        {
           // GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
          //  c.transform.position = ringPoints[i];
          //  c.name = "ring point " + i.ToString();// + currentEdgeIndex;
        }

        for (int a = start, z = 0; z < ringPoints.Count; a++, z++)
        {
            if (found)
                break;

            if (a > ringPoints.Count - 1)
                a -= ringPoints.Count;
            int next = a + 1;
            if (next > ringPoints.Count - 1)
                next -= ringPoints.Count ;


            //check for intersects on all edges matchin ring points
            for (int b = 0; b < cellInfos[currentCellIndex].intersectIndexesForEdges.Count; b++)
            {
                if (found)
                    break;

                for (int q = 0; q < cellInfos[currentCellIndex].intersectIndexesForEdges[b].Count; q++)
                {
                    if (found)
                        break;
                    //if we find a match
                    if (a == cellInfos[currentCellIndex].intersectIndexesForEdges[b][q])
                    {
                        if (found)
                            break;

                        //and the match isn't this already found intersect
                        //if (!intersects.Contains( cellInfos[currentCellIndex].intersectsForEdges[b][q]))// != currentIntersect)
                        if (cellInfos[currentCellIndex].intersectsForEdges[b][q] != currentIntersect)
                        {

                            if (doCubes)
                            {
                                GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                                c.transform.position = cellInfos[currentCellIndex].intersectsForEdges[b][q];
                                c.name = "intersect " + b.ToString();

                                c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                                c.transform.position = ringPoints[a];
                                c.name = "ring point on interesect " + currentEdgeIndex;
                                c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                                c.transform.position = ringPoints[next];
                                c.name = "next ring point on interesect " + currentEdgeIndex;
                            }
                            

                            found = true;
                            break;
                        }
                    }
                }
            }

         
            if (!found)
            {
                //add next point to mesh if we never found an intersect ( next point because of the way we looked for intersects)                
                newPoints.Add(ringPoints[next]);

                if (doCubes)
                {
                    GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    c.transform.position = ringPoints[a];
                    c.name = "ring point " + a.ToString();// + currentEdgeIndex;
                }
            }

          
        }

        return newPoints;
    }

    List<Vector3> Hallways(Vector3[] vertices, List<List<Vector3>> intersectsForCell, List<Vector3> newPoints,ref bool makeDoor,ref Vector3 doorEdgePos,ref bool doorEdgeFound0, ref bool doorEdgeFound1)
    {
        //as we make hallways, remember which points are on interior halls
        List<Vector3> hallEdges = new List<Vector3>();

        float hallSize = 1f;

        List<Vector3> tempList = new List<Vector3>();

        //why are there duplicates? -removed anyway - still dupes?
       // Debug.Log("newpoints count before distinct() = " + newPoints.Count);
            
        newPoints = newPoints.Distinct().ToList();
        //   Debug.Log("newpoints count after distinct() = " + newPoints.Count);


        List<Vector3> toRemove = new List<Vector3>();
        for (int i = 0; i < newPoints.Count; i++)
        {

            int prev = i - 1;
            if (prev < 0)
                prev = newPoints.Count-1;

            int next = i + 1;
            if (next > newPoints.Count - 1)
                next -= newPoints.Count;

            int nextNext = i + 2;
            if (nextNext > newPoints.Count - 1)
                nextNext -= newPoints.Count;

            // // Debug.Log("prev " + prev);
            //  Debug.Log("this " + i.ToString());
            //  Debug.Log("next " + next);


            //the vertices are higher than the edge cells - Not flattened it out yet( this is a handy result from flipping the underside vertices - I think!)
            //otherwise we can do a .Contains check but this way is faster

         

            //interior point (not on ring points)

            //GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
            //c.transform.position = newPoints[i];
            //c.name = "interior";

            //flatten and move towards centre
            // Vector3 centre = new Vector3(vertices[0].x, 0f, vertices[0].z);
            //dont touch next proper in array
            Vector3 thisV3 = new Vector3(newPoints[i].x, 0f, newPoints[i].z);
            Vector3 prevV3 = new Vector3(newPoints[prev].x, 0f, newPoints[prev].z);
            Vector3 nextV3 = new Vector3(newPoints[next].x, 0f, newPoints[next].z);
            //Vector3 nextNextV3 = new Vector3(newPoints[nextNext].x, 0f, newPoints[nextNext].z);

            if (thisV3 == nextV3 || thisV3 == prevV3)//if using adjacent/remove small edges, this should be fixed
                {
                    Debug.Log("Same - cell will be skipped");
                    return newPoints;
                }

            //hall points are higher that 0 on y axis

            //all not hall
            if (newPoints[prev].y == 0f && newPoints[i].y == 0f && newPoints[next].y == 0f)
            {
                //add this first
                tempList.Add(newPoints[i]);

                /*
                //slide next point towards this point
                GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                c.transform.position = newPoints[prev];
                c.name = "all non hall ";

                c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                c.transform.position = newPoints[i];
                c.name = i.ToString();

                c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                c.transform.position = newPoints[next];
                c.name = next.ToString();
                */
            }

            //if coming from non hall and this is non hall but going to hall
            else if (newPoints[prev].y == 0f && newPoints[i].y == 0f && newPoints[next].y > 0f)
            {
                /*
                GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                c.transform.position = newPoints[prev];
                c.name = "non,non, hall";

                c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                c.transform.position = newPoints[i];
                c.name = i.ToString();

                */

                if (!doorEdgeFound0)
                {
                    if (newPoints[i] == doorEdgePos)
                    {
                        //c.name = "Door pos found";

                        //slide to prev --//need to check for previous mesh points overlap

                        //work back through cell points list and remove any points that are closer to the point we are moving than where are putting the new point
                        //basically, stop the mesh from folding by removing points


                        //ref
                        doorEdgeFound0 = true;

                        Vector3 miter = MiterDirection(newPoints[prev], newPoints[i], newPoints[next], hallSize);
                        // tempList.Add(newPoints[i] - miter);



                        Vector3 dirFromNext = (newPoints[i] - newPoints[next]).normalized;
                        Vector2 intersectV2 = Vector2.zero;
                        //looking to see where the line projects on to to the outside of the building, suing miter position to help

                        Vector3 p2 = newPoints[i] - miter;
                        Vector3 p3 = p2 + (newPoints[i] - newPoints[next]);
                        Debug.DrawLine(p2, p3, Color.green);
                        for (int aa = i, bb = 0; bb < newPoints.Count; bb++, aa--)
                        {
                            if (aa < 0)
                                aa += newPoints.Count;

                            int aaPrev = aa - 1;
                            if (aaPrev < 0)
                                aaPrev += newPoints.Count;



                            Vector3 p0 = newPoints[aaPrev];
                            Vector3 p1 = newPoints[aa];
                            Debug.DrawLine(p0, p1, Color.yellow);

                            Vector2 a0 = new Vector2(p0.x, p0.z);
                            Vector2 a1 = new Vector2(p1.x, p1.z);
                            Vector2 a2 = new Vector2(p2.x, p2.z);
                            Vector2 a3 = new Vector2(p3.x, p3.z);


                            if (LineSegmentsIntersection(a0, a1, a2, a3, out intersectV2))
                            {
                                Vector3 intersectV3 = new Vector3(intersectV2.x, 0f, intersectV2.y);
                               // c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                               // c.transform.position = intersectV3;
                              //  c.name = "intersect";

                                tempList.Add(intersectV3);


                                //remeber this edge
                                hallEdges.Add(intersectV3);

                                break;

                            }
                            else
                            {
                                toRemove.Add(newPoints[aaPrev]);
                                tempList.Remove(newPoints[aaPrev]);//working?


                            }

                        }

                    }
                    else
                        tempList.Add(newPoints[i]);
                }
                else
                    tempList.Add(newPoints[i]);


                //debugs in order so we can change name of middle cube
               //  c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                // c.transform.position = newPoints[next];
                // c.name = next.ToString();

            }
            //coming from non hall but then on to double hall
            else if (newPoints[prev].y == 0f && newPoints[i].y > 0f && newPoints[next].y > 0f)
            {

                
                /*
                GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                c.transform.position = newPoints[prev];
                c.name = "non,hall, hall";

                c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                c.transform.position = newPoints[i];
                c.name = i.ToString();

                c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                c.transform.position = newPoints[next];
                c.name = next.ToString();
                */
                //if the previous point was part of the door edge we need to use a miter to give the hall a little more room
                if (newPoints[prev] == doorEdgePos)
                {
                    Vector3 miter = MiterDirection(newPoints[prev], newPoints[i], newPoints[next], hallSize);


                    doorEdgeFound1 = true;

                    tempList.Add(newPoints[i] - miter);//can also normalize the miter and * by hallsize?

                    //     c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    //    c.transform.position = newPoints[i] - miter;
                    //  c.name = "using miter";

                   

                }
                else
                {
                    /*
                    //we don't need the space, slide along edge
                    Vector3 slide = newPoints[i] + (newPoints[prev] - newPoints[i]).normalized * hallSize;
                    tempList.Add(slide);

                    c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    c.transform.position = slide;
                    c.name = "using edge slide 0";
                    */

                    Vector3 slide = newPoints[i] + (newPoints[prev] - newPoints[i]).normalized * hallSize;
                    tempList.Add(slide);
                    //add extra geometry with miter
                    Vector3 miter = MiterDirection(newPoints[prev], newPoints[i], newPoints[next], hallSize);
                    tempList.Add(newPoints[i] - miter);

                    //remeber this edge
                    hallEdges.Add( newPoints[i] - miter);


                    //  c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    //  c.transform.position = slide;
                    //  c.name = "using edge slide and miter " + ", dE0 = " + doorEdgeFound0 + ", makedoor = " + makeDoor + ", dE1 = " + doorEdgeFound1;
                }
            }
            //all hall
            else if (newPoints[prev].y > 0f && newPoints[i].y > 0f && newPoints[next].y > 0f)
            {
                Vector3 miter = MiterDirection(newPoints[prev], newPoints[i], newPoints[next], hallSize);

                /*
                GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                c.transform.position = newPoints[i];
                c.name = i.ToString();

                c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                c.transform.position = newPoints[i] - miter;
                c.name = "Miter";

                c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                c.transform.position = newPoints[next];
                c.name = next.ToString();
                */
                //use miter direction


                tempList.Add(newPoints[i] - miter);//can also normalize the miter and * by hallsize?
            }
            //if coming from hall, this is hall, and next is non hall
            else if (newPoints[prev].y > 0f && newPoints[i].y > 0f && newPoints[next].y == 0f)
            {
              
                //slide to next
                Vector3 slide = newPoints[i] + (newPoints[next] - newPoints[i]).normalized * hallSize;
                /*
              GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
              c.transform.position = newPoints[prev];
              c.name = "hall,hall, non";

              c = GameObject.CreatePrimitive(PrimitiveType.Cube);
              c.transform.position = newPoints[i];
              c.name = i.ToString();

              c = GameObject.CreatePrimitive(PrimitiveType.Cube);
              c.transform.position = newPoints[next];
              c.name = next.ToString();

              c = GameObject.CreatePrimitive(PrimitiveType.Cube);
               c.transform.position = slide;
               c.name = "slide to next";
               */
                //we need a little more geometry, use mtier points to ensure halls stay nice and wide
                Vector3 miter = MiterDirection(newPoints[prev], newPoints[i], newPoints[next],hallSize);
                 
                tempList.Add(newPoints[i] - miter);
                tempList.Add(slide);

                //remeber this edge
                hallEdges.Add(   newPoints[i] - miter);
            }
            //
            else if (newPoints[prev].y > 0f && newPoints[i].y == 0f && newPoints[next].y == 0f)
            {
                //we can make an exterior door on this edge
                if (makeDoor)
                {
                    int prevPrev = prev - 1;
                    if (prevPrev < 0)
                        prevPrev += ringPoints.Count;

                    /*
                    GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    c.transform.position = newPoints[prev];
                    c.name = "hall,non, non ";

                    c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    c.transform.position = newPoints[i];
                    c.name = i.ToString();

                    c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    c.transform.position = newPoints[next];
                    c.name = next.ToString();
                    
                    */
                    //this is a referenced argument
                    makeDoor = false;//ref
                                     //remember the position we started at so the adjacent cell knows to leave space for a door hallway

                    doorEdgePos = newPoints[i];//ref//already asigned

                    tempList.RemoveAt(tempList.Count - 1);//should we do this from another edge type - hall to no hall?

                    //add a miter
                    Vector3 miter = MiterDirection(newPoints[prevPrev], newPoints[prev], newPoints[i],hallSize);
                    tempList.Add(newPoints[prev] - miter);

                    Vector3 p2 = newPoints[prev] - miter;
                    Vector3 p3 = p2 + (newPoints[i] - newPoints[prev])*2;//enough?
                    Debug.DrawLine(p2, p3, Color.green);
                    for (int aa = i, bb = 0; bb < newPoints.Count;bb++, aa++)
                    {
                        if (aa > newPoints.Count - 1)
                            aa -= newPoints.Count;

                        int aaPrev = aa - 1;
                        if (aaPrev < 0)
                            aaPrev += newPoints.Count;

                        Vector3 p0 = newPoints[aaPrev];
                        Vector3 p1 = newPoints[aa];
                        Debug.DrawLine(p0, p1, Color.red);

                        Vector2 a0 = new Vector2(p0.x, p0.z);
                        Vector2 a1 = new Vector2(p1.x, p1.z);
                        Vector2 a2 = new Vector2(p2.x, p2.z);
                        Vector2 a3 = new Vector2(p3.x, p3.z);

                        Vector2 intersectV2 = Vector2.zero;
                        if (LineSegmentsIntersection(a0, a1, a2, a3, out intersectV2))
                        {
                            Vector3 intersectV3 = new Vector3(intersectV2.x, 0f, intersectV2.y);
                          //  c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                          //  c.transform.position = intersectV3;
                          //  c.name = "intersect";

                            tempList.Add(intersectV3);
                         //    tempList.Add(newPoints[aa]);

                            //remeber this edge
                            hallEdges.Add(intersectV3);

                            //force skip to this point - we don't need these points, just the intersect
                            i = aaPrev;
                                    
                            break;

                        }
                    }
                }
                else
                {
                  //  GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                  //  c.transform.position = newPoints[i];
                  //  c.name = "else";

                    tempList.Add(newPoints[i]);
                }

            }
            else if (newPoints[prev].y == 0f && newPoints[i].y > 0f && newPoints[next].y == 0f)
            {


                /*
                GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                c.transform.position = newPoints[prev];
                c.name = "non,hall , non";

                c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                c.transform.position = newPoints[i];
                c.name = i.ToString();

                c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                c.transform.position = newPoints[next];
                c.name = next.ToString();

                */
                //create geometry for corner if previous point isnt an exterior door
                if (doorEdgePos != newPoints[prev])
                {
                   

                    //add a littl extra point to make a wall for an interior door
                    Vector3 slide = newPoints[i] + (newPoints[prev] - newPoints[i]).normalized * hallSize;
                    tempList.Add(slide);

                    slide = newPoints[i] + (newPoints[next] - newPoints[i]).normalized * hallSize;
                    tempList.Add(slide);

                   // tempList.Add(newPoints[i]);
                }
                else
                {
                    Vector3 miter = MiterDirection(newPoints[prev], newPoints[i], newPoints[next], hallSize);
                    tempList.Add(newPoints[i] - miter);
                    //the exterior door has created an edge point already so we don't need to add one, just slide
                    Vector3 slide = newPoints[i] + (newPoints[next] - newPoints[i]).normalized * hallSize;
                    tempList.Add(slide);

                    
                }


            }
        }

        for (int i = 0; i < toRemove.Count; i++)
        {
            GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
            c.transform.position = toRemove[i];
            c.name = "to remove";

            tempList.Remove(toRemove[i]);
        }

        Door(hallEdges);

        newPoints = new List<Vector3>( tempList);

        return newPoints;
    }

    void Door(List<Vector3> edges)
    {
        for (int i = 0; i < edges.Count; i++)
        {
              //  GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
              //  c.transform.position = edges[i];
               


            }
        
    }

    void HallwaysFromEdges(List<Vector3[]> edges)//doesnt consider figure of eight hall shapes
    {
        //create a snake made from the edges earmarked for hallways
        //find a start and an end point - one which doent share and edge with another
        int start = -1;
        int end = 0;

        for (int i = 0; i < edges.Count; i++)
        {
            Vector3[] edge = edges[i];
            int matches = 0;

            for (int j = 0; j < edges.Count; j++)
            {
                if (i == j)
                    continue;

                Vector3[] otherEdge = edges[j];
                if(edge[0] == otherEdge[0] || edge[0] == otherEdge[1])
                {
                    matches++;
                }
            }

            if (matches == 1)
            {
               

                if (start == -1)
                    start = i;
                else
                {
                    end = i;
                    break;
                    
                }
            }
        }

        
        

        int lookingFor = start;
        GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
        c.transform.position = edges[start][0];
        c.name = "start";
        //find a path through the edges from start to end
        int safety = 0;
        for (int i = 0; i < edges.Count; i++)//safety debugging
        {
            if (edges[lookingFor][1] == edges[i][0] && edges[lookingFor][0] != edges[i][1])
            {

                c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                c.transform.position = edges[i][0];
                c.name = "edges - i 0";
                c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                c.transform.position = edges[i][1];
                c.name = "edges - i 1";




                lookingFor = i;
                i = 0;

            }
            //check for end
            if (edges[i][1] == edges[end][0])
            {
                c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                c.transform.position = edges[end][0];
                c.name = "end";
                break;
            }
            else
            {
                //reset loop with new target
                // lookingFor = i;

                // i = 0;
            }
            safety++;
            if (safety > 1000)
            {
                Debug.Log("break");
                break;
            }
         
        
        }
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

        //  Debug.DrawLine(p0, p1, Color.yellow);
       //   Debug.DrawLine(p2, p1, Color.yellow);
        // if(draw)
        Debug.DrawLine(p1, p1 - miterDirection  , Color.yellow);
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

    public GameObject Cell(List<Vector3> newPoints)
    {

        newPoints = newPoints.Distinct().ToList();

        Vector3 avg = Vector3.zero;
        bool doCubes = false;
         
        for (int a = 0; a < newPoints.Count; a++)
        {
            if (doCubes)
            {
                GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                c.transform.position = newPoints[a];
                c.name = "np";
            }

            //flatten
            newPoints[a] = new Vector3(newPoints[a].x, 0f, newPoints[a].z);

            avg += newPoints[a];
        }

        avg /= newPoints.Count;

        newPoints.Insert(0, avg);

        List<int> trianglesL = new List<int>();

        for (int i = 1; i < newPoints.Count - 1; i++)
        {
            if (i == 0)
                continue;

            trianglesL.Add(0);
            trianglesL.Add(i);
            trianglesL.Add(i + 1);
        }

        //last tri (joining last and first)
        trianglesL.Add(0);
        trianglesL.Add(newPoints.Count - 1);
        trianglesL.Add(1);

        GameObject roomFloor = new GameObject();
        roomFloor.transform.position += Vector3.up * 10;///888test
        roomFloor.name = "RoomFloor";
        roomFloor.AddComponent<MeshRenderer>().sharedMaterial = Resources.Load("Grey") as Material;
        MeshFilter mf = roomFloor.AddComponent<MeshFilter>();
        Mesh mesh = new Mesh();
        mesh.vertices = newPoints.ToArray();
        mesh.triangles = trianglesL.ToArray();

        //mesh = ExtrudeCell.Extrude(mesh, 3f, 1f, true);

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        mf.mesh = mesh;



        

        return roomFloor;

    }
    public static bool LineLineIntersection(out Vector3 intersection, Vector3 linePoint1, Vector3 lineVec1, Vector3 linePoint2, Vector3 lineVec2)
    {
        Debug.DrawLine(linePoint1, linePoint1 + lineVec1, Color.blue);
        Debug.DrawLine(linePoint2, linePoint2 + lineVec2, Color.green);

        Vector3 lineVec3 = linePoint2 - linePoint1;
        Vector3 crossVec1and2 = Vector3.Cross(lineVec1, lineVec2);
        Vector3 crossVec3and2 = Vector3.Cross(lineVec3, lineVec2);

        float planarFactor = Vector3.Dot(lineVec3, crossVec1and2);

        //is coplanar, and not parrallel
        if (Mathf.Abs(planarFactor) < 0.0001f && crossVec1and2.sqrMagnitude > 0.0001f)
        {
            float s = Vector3.Dot(crossVec3and2, crossVec1and2) / crossVec1and2.sqrMagnitude;
            intersection = linePoint1 + (lineVec1 * s);
            return true;
        }
        else
        {
            intersection = Vector3.zero;
            return false;
        }
    }
    // Distance to point (p) from line segment (end points a b)
    float DistanceLineSegmentPoint(Vector3 a, Vector3 b, Vector3 p)
    {
        // If a == b line segment is a point and will cause a divide by zero in the line segment test.
        // Instead return distance from a
        if (a == b)
            return Vector3.Distance(a, p);

        // Line segment to point distance equation
        Vector3 ba = b - a;
        Vector3 pa = a - p;
        return (pa - ba * (Vector3.Dot(pa, ba) / Vector3.Dot(ba, ba))).magnitude;
    }

    
    class IntersectionInfo
    {
        public Vector3 intersection;
        public int cellIndex0;
        public int cellIndex1;
        public int ringIndex0;
        public int ringIndex1;
        public bool thisPointOutside;
        public bool nextPointOutside;

        public IntersectionInfo(Vector3 aIntersection,int aCellIndex0,int aCellIndex1,int aRingIndex0,int aRingIndex1,bool aThisPointOutside, bool aNextPointOutside)
        {
            intersection = aIntersection;
            cellIndex0 = aCellIndex0;
            cellIndex1 = aCellIndex1;
            ringIndex0 = aRingIndex0;
            ringIndex1 = aRingIndex1;
            thisPointOutside = aThisPointOutside;
            nextPointOutside = aNextPointOutside;
        }
    }

    public class CellInfo
    {
        public List<int[]> edges = new List<int[]>();
        public List<List<Vector3>> intersectsForEdges = new List<List<Vector3>>();
        public List<List<int>> intersectIndexesForEdges = new List<List<int>>();
        public int cellNumber;

        public CellInfo(List<int[]> _edges,List<List<Vector3>> _intersectsForEdges,List<List<int>> _intersectIndexesForEdges,int _cellNumber)
        {
            edges = _edges;
            intersectsForEdges = _intersectsForEdges;
            intersectIndexesForEdges = _intersectIndexesForEdges;
            cellNumber = _cellNumber;
        }
    }

    public static bool LineSegmentsIntersection(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4, out Vector2 intersection)
    {
        intersection = Vector2.zero;

        var d = (p2.x - p1.x) * (p4.y - p3.y) - (p2.y - p1.y) * (p4.x - p3.x);

        if (d == 0.0f)
        {
            return false;
        }

        var u = ((p3.x - p1.x) * (p4.y - p3.y) - (p3.y - p1.y) * (p4.x - p3.x)) / d;
        var v = ((p3.x - p1.x) * (p2.y - p1.y) - (p3.y - p1.y) * (p2.x - p1.x)) / d;

        if (u < 0.0f || u > 1.0f || v < 0.0f || v > 1.0f)
        {
            return false;
        }

        intersection.x = p1.x + u * (p2.x - p1.x);
        intersection.y = p1.y + u * (p2.y - p1.y);

        return true;
    }
}
