using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
public class Interiors : MonoBehaviour
{
    public int iterations = 0;
    public List<int> isolates = new List<int>();
    public bool doHalls = true;

    public List<GameObject> areas = new List<GameObject>();
    public List<Vector3> ringPoints = new List<Vector3>();
    public List<Vector3> targetPoints = new List<Vector3>();//where door will try and attach to
    public List<Vector3> cornerPoints = new List<Vector3>();
    //public GameObject underSide;



    
    public int corners = 0;
    public float doorWidth = 1f;
    public float hallWidth = 2f;

    public bool snip;
    public bool fullReset;
    public bool showRingPoints = false;
    GameObject interiorObj;

    public bool apartmentDoorBuilt = false;
    
    int frames = 0;

    public int roomsBuilt = 0;
    public bool allRoomsBuilt = false;
    //used by children components to build halls
    public Vector3 doorEdgePos = Vector3.zero;


    TraditionalSkyscraper tS;

    private void Start()
    {
        if(iterations == 0)
        {
            tS = transform.parent.GetComponentInParent<TraditionalSkyscraper>();
        }
        else if(iterations == 1)
        {
            //5 x parent
            tS = transform.parent.parent.parent.parent.GetComponentInParent<TraditionalSkyscraper>();
        }
            
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
            if(showRingPoints)
                ShowCubesOnRing(gameObject, ringPoints);

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
            apartmentDoorBuilt = false;
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

        interiorObj = new GameObject();
        interiorObj.name = "Interior";
        interiorObj.transform.parent = gameObject.transform;
        interiorObj.transform.position = gameObject.transform.position;

        MeshGenerator mg = interiorObj.AddComponent<MeshGenerator>();

        List<Vector3> pointsInside = new List<Vector3>();

        //find random positions within floor

        // Vector3 shootFrom = gameObject.transform.position - 10f * Vector3.up;
        //GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
        //c.transform.position = shootFrom;
        //GameObject underSide = gameObject.transform.Find("UnderSide").gameObject;//asigned when adding Interiors
        // if(GetComponent<MeshCollider>() == null)
        //    gameObject.AddComponent<MeshCollider>();

        //int cap = (int)((underSide.GetComponent<MeshRenderer>().bounds.extents.x + underSide.GetComponent<MeshRenderer>().bounds.extents.z )*0.5f);// 5;
        int cap = 3;// corners/2 +1;
        //cap = Random.Range(3, 10);
        //?work needs done for this
        //add random
       // RaycastHit hit;
        

        
        //find random points inside triangles of "underside" floorplan
        Vector3[] vertices = GetComponent<MeshFilter>().mesh.vertices;
        int[] triangles = GetComponent<MeshFilter>().mesh.triangles;
        Vector3 zeroGameObj = new Vector3(gameObject.transform.position.x, 0f, gameObject.transform.position.z);
        
        //might need to change if we want more than a cluster of rooms(double halls in merged cell e.g)
        Vector3 boundsCentre = GetComponent<MeshRenderer>().bounds.center;
        boundsCentre = new Vector3(boundsCentre.x, 0f, boundsCentre.z);

        Vector3 avg = Vector3.zero;
        for (int i = 0; i < ringPoints.Count; i++)
        {
            

            avg +=new Vector3( ringPoints[i].x,0f,ringPoints[i].z);
        }

        avg /= ringPoints.Count;

     

        //spin round this
        int start = Random.Range(0, 360 / cap);
        for (int i = start; i < 360 + start; i+=360/cap)
        {
            Vector3 p0 = Quaternion.Euler(0,i,0)* Vector3.right;
            

            Vector3 p1 =p0 * 100;
           // p1 += avg - boundsCentre;
            //mg.yardPoints.Add(p1);

            Vector3 p2 = p0 *101;
           // p2 += avg - boundsCentre;
            //mg.yardPoints.Add(p2);

            p0 += boundsCentre - avg;
            p1 += boundsCentre - avg;
            p2 += boundsCentre - avg;

            mg.yardPoints.Add(p0);
            mg.yardPoints.Add(p1);
            mg.yardPoints.Add(p2);

            // c = GameObject.CreatePrimitive(PrimitiveType.Cube);
            //  c.transform.position = p0 + transform.position;// + avg - boundsCentre;
            //c.name = "voronoi input";
        }
        
        mg.minEdgeSize = hallWidth;
        mg.volume = Vector3.one * 1000;
        mg.lloydIterations = 1;//relax cells
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

        if(iterations == 0)
        {
            for (int i = 0; i < areas.Count; i++)
            {
                //start hallways and build interior walls
                areas[i].GetComponent<Hallways>().Start(); //starting when placed      

                
            }

            BookEnds();
        }

        if (iterations == 1)
        {

            return;//tresting 0 atm
            //create floor plans for individual apartments

            for (int i = 0; i < areas.Count; i++)
            {
                //start hallways and build interior walls
                areas[i].GetComponent<Hallways>().Start(); //starting when placed

            }

            BookEnds();

        }

    }    

    void BookEnds()
    {

        //now all hallways have been calculated, match up the missing points to fill in the gaps where the hallway was made
        //we use these to build walls 
        for (int i = 0; i < areas.Count; i++)
        {
            //hallways saved two lists of points, one for each type of intersection with the outer edge
            //a wall will be made of type A and type B

            //so let's find an A and a B which share a point
            //the point on the lists where it interesects the outside building is the point we are looking for
            //B is second last and A is second (because we save the interior hall point at the start and the end of the list so we can get the miter direction for the wall depth)
            List<Vector3> listA = areas[i].GetComponent<Hallways>().bookendPoints[0];
            if (listA.Count == 0)
                continue;


            for (int j = 0; j < areas.Count; j++)
            {
                //look in other areas
                if (i == j)
                    continue;

                List<Vector3> wallPoints = new List<Vector3>();
                List<Vector3> listB = areas[j].GetComponent<Hallways>().bookendPoints[1];

                if (listB.Count == 0)
                    continue;

                if (listA[1] == listB[listB.Count - 2])
                {
                   

                    //great, join these list together, start with b
                    for (int a = 0; a < listB.Count - 1; a++)//dont' add last
                    {
                        wallPoints.Add(listB[a]);

                        GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        c.transform.position = listB[a];
                        c.name = "adding B";
                    }
                    for (int a = 2; a < listA.Count; a++)//dont' add  first two, first point is miter and second B had)
                    {
                        wallPoints.Add(listA[a]);

                        GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        c.transform.position = listA[a];
                        c.name = "adding A";
                    }

                    //we have everything we need to build a wall
                    //we just need to add the miters 
                    Vector3 miterDir = (listA[0] - listA[1]).normalized * tS.exteriorWallThickness;
                    Vector3 endIntersect = listB[0] + miterDir;
                    Vector3 startIntersect = wallPoints[wallPoints.Count - 1] + miterDir;

                    wallPoints.Insert(0, endIntersect);
                    wallPoints.Add(startIntersect);

                    //send to build - NEED TO FIGURE OUT IF EXTERIOR WALL OR INTERIOR ROOM WALL - ALSO NEED TO DECIDE WHICH ONE IS FOR A DOOR*******
                    if(iterations == 0)
                    {
                        //always exteriro wall on first iteration
                        //shall we build a door? - could try and anchor doors to one side? Random just now (first point available
                        //we will try to build a door on a flat wall first
                        //check if we ahve already built a door too
                        if (wallPoints.Count == 5  && !apartmentDoorBuilt)
                        {
                            //door frame
                            bool flipped;
                            Vector3 p0 = wallPoints[1];
                            Vector3 p1 = p0 + (wallPoints[0] - wallPoints[1]).normalized * tS.exteriorWallThickness;
                            Vector3 p2 = wallPoints[3];
                            Vector3 p3 = p2 + (wallPoints[4] - wallPoints[3]).normalized * tS.exteriorWallThickness;
                            Vector3 boundsCentre = tS.GetComponent<MeshRenderer>().bounds.center;
                            GameObject doorFrame = InteriorAssets.ApartmentDoorFrame(out flipped, wallPoints[2],p0,p2,p1,p3,  tS, boundsCentre);
                            doorFrame.AddComponent<MeshRenderer>().sharedMaterial = tS.materials[0];

                            //recentre mesh? // just adjust y atm
                            doorFrame.transform.position = new Vector3(0, transform.position.y,0);                            
                            doorFrame.transform.parent = transform;
                            

                            //door

                            GameObject door = InteriorAssets.ApartmentDoor(wallPoints[2], p0, tS.doorWidth, tS.doorHeight, tS.doorDepth, flipped);

                            door.transform.position = new Vector3(wallPoints[2].x, transform.position.y, wallPoints[2].z) + (wallPoints[2] - wallPoints[1]).normalized * tS.doorWidth * .5f; ;
                            door.transform.parent = transform;

                            door.transform.GetChild(0).GetComponent<MeshRenderer>().sharedMaterial = tS.materials[1];

                            apartmentDoorBuilt = true;

                        }
                        else
                        {
                             InteriorWalls.ExteriorWall(wallPoints, startIntersect, endIntersect, tS, transform);
                        }
                    }

                    else if(iterations == 1)
                    {
                        //do exterior test before building
                    }

                }


                for (int a = 0; a < wallPoints.Count; a++)
                {
                    GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    c.transform.position = wallPoints[a];
                    c.name = "wall point";
                }


            }
        }
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
                if (next > vertices.Length - 1)
                    next = 1;// vertices.Length - 1; //was 1



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

   
        for (int i = 0; i < cellInfos.Count; i++)
        {
            //we need to keep track of possible door positions
            List<Vector3> doorPositions = new List<Vector3>();

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

                Debug.DrawLine(vertices[edge[0]], vertices[edge[1]], Color.white);

                if (doCubes)
                {
                    GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    c.transform.position = vertices[edge[0]];
                    c.name = "vertice 0";
                    c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    c.transform.position = vertices[edge[1]];
                    c.name = "vertice 1";
                }


               // RaycastHit hit;
               // Vector3 shootFrom = vertices[edge[0]];
               // shootFrom -= Vector3.up;

                //find out if we are starting from a point inside the floor cell or not
                bool inside = false;
                Vector3[] underSideVertices = gameObject.GetComponent<MeshFilter>().mesh.vertices;
                int[] underSideTriangles= gameObject.GetComponent<MeshFilter>().mesh.triangles;
                for (int t = 0; t < underSideTriangles.Length; t += 3)
                {
                    Vector3 t0 = underSideVertices[underSideTriangles[t]] + transform.position;
                    Vector3 t1 = underSideVertices[underSideTriangles[t + 1]] + transform.position;
                    Vector3 t2 = underSideVertices[underSideTriangles[t + 2]] + transform.position;
                    /*
                    GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    c.transform.position = t0;
                    c.name = "t0";
                    c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    c.transform.position = t1;
                    c.name = "t1";
                    c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    c.transform.position = t2;
                    c.name = "t2";
                    */
                    //check first edge
                    Vector2 q = new Vector3(vertices[edge[0]].x, vertices[edge[0]].z); //possible y co-prd probs on higher floors? but vector 2?
                    Vector2 q0 = new Vector2(t0.x, t0.z);
                    Vector2 q1 = new Vector2(t1.x, t1.z);
                    Vector2 q2 = new Vector2(t2.x, t2.z);

                    if (PointInTriangle(q, q0, q1, q2))
                    {
                        inside = true;
                        break;
                    }


                    //HERE, LOOK FOR FLOOR WITH 2 CYAN ONLY WILL NOT WORK CORRECTLY
                    //what if edge only is inside with 2nd point? - do we need inside first and inside second checks?
                    q = new Vector3(vertices[edge[1]].x, vertices[edge[1]].z);
                    //if (PointInTriangle(q, q0, q1, q2))
                    {
                        //inside = true;
                        //break;
                    }
                }

                /* //using above inside tri method
                if (Physics.Raycast(shootFrom, Vector3.up, out hit, 2f, LayerMask.GetMask("Roof")))
                {
                    if (hit.transform.gameObject == underSide)
                    {
                        //inside = true;
                    }
                }
                */

                //organise these ifs better

                if (intersects.Count == 0 || intersects.Count == 1)
                {
                   
                    if (inside)
                    {
                        //starting inside
                        if (intersects.Count == 0)// && inside)
                        {
                            //had one case where intersect was marginal and was adding edge even though it was outside? expensive edge case check
                            bool insideSecond = false;
                            /*
                            shootFrom = vertices[edge[1]];
                            shootFrom -= Vector3.up;
                            if (Physics.Raycast(shootFrom, Vector3.up, out hit, 2f, LayerMask.GetMask("Roof")))////*** if ditance check for voronoi placement, remove this
                            {
                                if (hit.transform.gameObject == underSide)
                                {
                                    insideSecond = true;
                                }
                            }
                            */

                            for (int t = 0; t < underSideTriangles.Length; t += 3)
                            {
                                Vector3 t0 = underSideVertices[underSideTriangles[t]] + transform.position;
                                Vector3 t1 = underSideVertices[underSideTriangles[t + 1]] + transform.position;
                                Vector3 t2 = underSideVertices[underSideTriangles[t + 2]] + transform.position;
                              
                                Vector3 q = new Vector3(vertices[edge[1]].x, vertices[edge[1]].z); //possible y co-prd probs on higher floors
                                Vector3 q0 = new Vector2(t0.x, t0.z);
                                Vector3 q1 = new Vector2(t1.x, t1.z);
                                Vector3 q2 = new Vector2(t2.x, t2.z);

                                if (PointInTriangle(q, q0, q1, q2))
                                {
                                    insideSecond = true;
                                    break;
                                }
                            }


                            if (insideSecond)
                            {
                                Debug.DrawLine(vertices[edge[0]], vertices[edge[1]], Color.blue);//add
                                newPoints.Add(vertices[edge[0]]);
                                newPoints.Add(vertices[edge[1]]);//?-- this is a hall way
                                
                                //add to list to work out after
                              //  edgesForHallway.Add(new Vector3[] { vertices[edge[0]], vertices[edge[1]] } );
                            }
                            else
                            {
                                Debug.Log("Remove marginal edge ? sometimes works sometimes doesnt - redo? voronoi pattern is right on ringpoints edge- could do a distance check when placing vor points - distance to ring point");
                                Debug.DrawLine(vertices[edge[0]], vertices[edge[1]], Color.yellow);//add

                               // GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                             //   c.transform.position = vertices[edge[0]];
                             //   c.name = "vertice 0";
                             //   c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                             //   c.transform.position = vertices[edge[1]];
                             //   c.name = "vertice 1";

                              //  newPoints.Add(vertices[edge[0]]);
                              //  newPoints.Add(vertices[edge[1]]);

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
                           // Debug.DrawLine(vertices[edge[1]], intersects[0], Color.magenta);//add from intersection
                       

                            //add 0y
                            Vector3 y0 = new Vector3(intersects[0].x, 0f, intersects[0].z);
                            newPoints.Add(intersects[0]);

                            //make y = 1f to indidcate this is a central (hallway) point
                            Vector3 y1 = new Vector3(vertices[edge[1]].x, 1f, vertices[edge[1]].z);
                            newPoints.Add(y1);

                            //check if this point is on the exterior ring
                            
                            if (!IsDoorExterior(intersects[0]))
                            {
                                doorPositions.Add(intersects[0]);
                               
                               // GameObject door = ApartmentDoor(intersects[0], ringPoints[intersectIndexes[0]]);

                                
                            }

                            /*
                            GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                            c.transform.position = intersects[0];
                            c.name = "intersect 0 " + i.ToString(); ;
                            
                              c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                             c.transform.position = y1;
                             c.name = "edge 1 " + i.ToString(); ;
                             */
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
             
                if (newPoints.Count > 2)//needs 3
                {
                    GameObject area = Cell(newPoints);
                    areas.Add(area);
                    area.name = i.ToString();
                    Hallways hallways = area.AddComponent<Hallways>();
                    hallways.interiorsRingPoints = newPoints;
                    hallways.targetPoints = targetPoints;
                    hallways.enabled = false;//will be triggered by interiors script
                    hallways.reCentreMesh = true;
                    hallways.iterations = iterations;
                    hallways.apartmentDoorPositions = doorPositions;
                    GetComponent<MeshRenderer>().enabled = false;

                }
            }
        }
    }

    bool IsDoorExterior(Vector3 i)
    {
        //two potential doors here, previous point, and next. Check to see if these points are between ring points for the whole floor
        //3 parents up if 2nd iteration

        GameObject c = null;
        bool exterior = false;
        if (iterations == 1)
        {
            List<Vector3> parentsRingPoints = transform.parent.parent.GetComponent<Interiors>().ringPoints;
            parentsRingPoints = parentsRingPoints.Distinct().ToList();


            for (int j = 0; j < parentsRingPoints.Count; j++)
            {
                int parentNext = j + 1;
                if (parentNext > parentsRingPoints.Count - 1)
                    parentNext -= parentsRingPoints.Count;

                if (DistanceLineSegmentPoint(parentsRingPoints[j], parentsRingPoints[parentNext], i) < 0.01f)
                {
                    exterior = true;
                }
            }
        }

        return exterior;
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
        interiors.targetPoints = targetPoints;
        interiors.cornerPoints = cornerPoints;
        interiors.corners = corners;
        interiors.hallWidth = hallWidth;
        interiors.iterations = iterations;

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

           // Debug.Log("count = " + ringPoints.Count());
           // Debug.Log("b4 a = " + a.ToString());
            if (a > ringPoints.Count - 1)
                a -= ringPoints.Count;
            int next = a + 1;
            if (next > ringPoints.Count - 1)
                next -= ringPoints.Count ;

           // Debug.Log("after a = " + a.ToString());


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

            //flatten - working on THIS
            Vector3 flat = new Vector3(newPoints[a].x, 0f, newPoints[a].z);
            avg += flat;// newPoints[a];
           // newPoints[a] = flat;//PUT THIS BACK IN, IS IT OK?!
        }
        avg /= newPoints.Count;

        
        newPoints.Insert(0, avg);

        //recentre so mesh is local 
        for (int i = 0; i < newPoints.Count; i++)
        {
            newPoints[i] -= avg;
        }


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
        roomFloor.transform.parent = transform;
        roomFloor.transform.position = new Vector3( avg.x,transform.position.y,avg.z);
       // roomFloor.transform.position += Vector3.up * 10;///888test
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


    public static bool PointInTriangle(Vector2 p, Vector2 p0, Vector2 p1, Vector2 p2)
    {
        var s = p0.y * p2.x - p0.x * p2.y + (p2.y - p0.y) * p.x + (p0.x - p2.x) * p.y;
        var t = p0.x * p1.y - p0.y * p1.x + (p0.y - p1.y) * p.x + (p1.x - p0.x) * p.y;

        if ((s < 0) != (t < 0))
            return false;

        var A = -p1.y * p2.x + p0.y * (p2.x - p1.x) + p0.x * (p1.y - p2.y) + p1.x * p2.y;

        return A < 0 ?
                (s <= 0 && s + t >= A) :
                (s >= 0 && s + t <= A);
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
