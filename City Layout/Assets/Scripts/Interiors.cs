using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
public class Interiors : MonoBehaviour
{

    public List<int> isolates = new List<int>();
        
    public List<Vector3> ringPoints = new List<Vector3>();
    public List<Vector3> cornerPoints = new List<Vector3>();
    public int corners = 0;

    public bool snip;
    private void Start()
    {
        //
        CreateVoronoi();
        //need to wait for voronoi to finish before snipping


        Invoke("Snip", 0.1f);
    }

    private void Update()
    {
        if(snip)
        {

            //ShowCubesOnRing(gameObject, ringPoints);

            Snip();
            snip = false;

            //now check for line intersections and adjust mesh
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
        


        GameObject interiorObj = new GameObject();
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
        cap = corners;
        //?work needs done for this
        //add random
        RaycastHit hit;
        float limit = (underSide.GetComponent<MeshRenderer>().bounds.extents.x + underSide.GetComponent<MeshRenderer>().bounds.extents.z)*.8f;
        for (int i = 0; i < cap; i++)
        {
            Vector2 modV2 = Random.insideUnitCircle;
            Vector3 modV3 = new Vector3(modV2.x, 0f, modV2.y);
            //create a float with average size of bounds
            modV3 *= limit;
            //GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
            //c.transform.position = shootFrom + modV3;
            if (Physics.Raycast(shootFrom + modV3, Vector3.up, out hit, 20f, LayerMask.GetMask("Roof")))
            {
                //    GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                //  c.transform.position = hit.point;
                Vector3 zeroYHitPoint = hit.point - gameObject.transform.position;
                zeroYHitPoint = new Vector3(zeroYHitPoint.x, 0f, zeroYHitPoint.z);
                mg.yardPoints.Add(zeroYHitPoint);
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
            mg.yardPoints.Add(p + dir*2f);
           mg.yardPoints.Add(p + dir * 3f);
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
    void Snip()
    {
        List<GameObject> interiorCells = transform.Find("Interior").GetComponent<MeshGenerator>().cells;
        List<CellInfo> cellInfos = Intersects(interiorCells);

        CellPoints(interiorCells, cellInfos);

        Debug.Break();
    }

    public void SnipOld()
    {
        //**need to sort this!! missing end loop - fixed?
        //ShowCubesOnRing(gameObject, ringPoints);

        List<GameObject> interiorCells = transform.Find("Interior").GetComponent<MeshGenerator>().cells;
        // Debug.Log(interiorCells.Count);

        GameObject underSide = transform.Find("UnderSide").gameObject;


        //get rid of any cells which are not overlapping
        List<GameObject> cellsToSnip = new List<GameObject>(interiorCells);

        /*
        RaycastHit hit;
        
        for (int i = 0; i < interiorCells.Count; i++)
        {
            //need to make sure an edge isn't on main floor
            int hits = 0;
            Vector3[] vertices = interiorCells[i].GetComponent<MeshFilter>().mesh.vertices;
            for (int j = 1; j < vertices.Length; j++)
            {
                Vector3 shootFrom = vertices[j];
                shootFrom -= Vector3.up;


                if (Physics.Raycast(shootFrom, Vector3.up, out hit, 2f, LayerMask.GetMask("Roof")))
                {
                    hits++;
                    
                }
                else
                {
                    
                }

                if(hits > 0)
                {
                    cellsToSnip.Add(interiorCells[i]);
                    break;
                }
            }
            if (hits == 0)
                interiorCells[i].SetActive(false);
        }
        */

        //find mesh edges which overlap with outside of building floor       
        //  ringPoints.Add(ringPoints[0]);

       // List<List<Vector3>> cellList = new List<List<Vector3>>();

        //for each cell edge look for an intersection with floor area
        //save all intersect info in a list
        List<CellInfo> cellInfos = new List<CellInfo>();
        for (int j = 0; j < cellsToSnip.Count; j++)//cellsToSnip.Count
        {
            //skip for tests
            if (!isolates.Contains(j) && isolates.Count>0)
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
                for (int i = 1; i < ringPoints.Count; i++)//can use last found point instead of 1? **opto
                {

                    //Debug.DrawLine(ringPoints[i - 1], ringPoints[i], Color.yellow);

                    Vector2 p2 = new Vector2(ringPoints[i - 1].x, ringPoints[i - 1].z);
                    Vector2 p3 = new Vector2(ringPoints[i].x, ringPoints[i].z);

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
                        
                        GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        c.transform.position = intersectV3;
                        c.name = "interesect";
                        */
                        //keep track of how many times this edge has been intersected

                        intersects.Add(intersectV3);
                        intersectIndexes.Add(i);


                    }
                }

                if (intersects.Count == 1)
                {
                    // GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    //  c.transform.position = intersects[0];
                    //  c.name = "interesect 0";

                    //  Debug.DrawLine(intersects[0], vertices[next], Color.cyan);


                }
                if (intersects.Count == 2)
                {
                    //Debug.DrawLine(vertices[k], vertices[next], Color.magenta);
                    //  Debug.DrawLine(intersects[0], intersects[1], Color.magenta);

                }


                int[] edge = new int[2] { k, next };
                edges.Add(edge);

                intersectsForEdges.Add(intersects);
                intersectIndexesForEdges.Add(intersectIndexes);

            }

            //save
            cellInfos.Add(new CellInfo(edges, intersectsForEdges, intersectIndexesForEdges,j));

        }
        for (int j = 0; j < cellInfos.Count; j++)//same count as cell infos ( parallel lists - needed for vertices)
        {
            //skip for tests
         //   if (!isolates.Contains(j))
          //      continue;

            List<int[]> edges = cellInfos[j].edges;
            List<List<Vector3>> intersectsForEdges = cellInfos[j].intersectsForEdges;
            List<List<int>> intersectIndexesForEdges = cellInfos[j].intersectIndexesForEdges;

            Vector3[] vertices = cellsToSnip[cellInfos[j].cellNumber].GetComponent<MeshFilter>().mesh.vertices;

            List<Vector3> newPoints = new List<Vector3>();

            
            for (int i = 0; i < edges.Count; i++)
            {


                int[] edge = edges[i];
                List<Vector3> intersects = intersectsForEdges[i];
                List<int> intersectIndexes = intersectIndexesForEdges[i];

                if (intersects.Count == 0 || intersects.Count == 1)
                {
                    RaycastHit hit;
                    Vector3 shootFrom = vertices[edge[0]];
                    shootFrom -= Vector3.up;

                    //find out if we are starting from apoint inside the floor cell or not
                    if (Physics.Raycast(shootFrom, Vector3.up, out hit, 2f, LayerMask.GetMask("Roof")))
                    {
                        if(hit.transform.gameObject != underSide)
                        {
                            //Debug.DrawLine(vertices[edge[0]], vertices[edge[1]], Color.red);//don't add
                        }
                        //starting inside
                        else if (intersects.Count == 0)
                        {
                           // Debug.DrawLine(vertices[edge[0]], vertices[edge[1]], Color.white);//add
                            newPoints.Add(vertices[edge[0]]);
                            newPoints.Add(vertices[edge[1]]);
                        }

                        else if (intersects.Count == 1)
                        {
                            Debug.DrawLine(vertices[edge[0]], intersects[0], Color.cyan);//add to intersection then look for ring points

                            //add intersection
                            newPoints.Add(intersects[0]);

                            //add edge points to next intersection
                            List<Vector3> tempPoints = RingPointsToNextIntersect(j, i,intersectIndexes[0], cellInfos);//** not adding all points

                            for (int a = 0; a < tempPoints.Count; a++)
                            {
                                newPoints.Add(tempPoints[a]);
                            }
                        }
                    }
                    else
                    {
                        //starting outside
                       // if (intersects.Count == 0)
                       //     Debug.DrawLine(vertices[edge[0]], vertices[edge[1]], Color.green);//don't add

                        if (intersects.Count == 1)
                        {
                            Debug.DrawLine(vertices[edge[1]], intersects[0], Color.magenta);//add from intersection

                            
                            newPoints.Add(intersects[0]);
                            newPoints.Add(vertices[edge[1]]);

                        }
                    }
                }

                if (intersects.Count == 2)
                {
                 
                    //still to do, special case correct direction - found points = 0 in addRing method, or, find before that?

                   // newPoints.Add(intersects[0]);
                   // newPoints.Add(intersects[1]);

                    Debug.DrawLine(intersects[0], intersects[1], Color.red);

                    GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    c.transform.position = intersects[0];
                    c.name = "intersect count = 2";
                    c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    c.transform.position = intersects[1];
                    c.name = "intersect count = 2";

                    List<Vector3> tempPoints = RingPointsToNextIntersect(j,i,intersectIndexes[1],cellInfos); //need to figure out if direction needs flipped- one dir = [0], other start at [1]

                    for (int a = 0; a < tempPoints.Count; a++)
                    {
                        newPoints.Add(tempPoints[a]);
                    }

                }

                if(intersects.Count >= 3)
                {
                    Debug.Log("OMG, 3 or over intersects - it exists, brute force for test to find");
                }
            }

            if(newPoints.Count>0)
              Cell(newPoints);

        }

        Debug.Break();

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

    void CellPoints(List<GameObject> cellsToSnip, List<CellInfo> cellInfos)
    {
        bool doCubes = false;//debug

        GameObject underSide = gameObject.transform.Find("UnderSide").gameObject;

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
                                newPoints.Add(vertices[edge[1]]);//?
                            }
                            else
                            {
                                Debug.Log("Remove marginal edge ? sometimes works sometimes doesnt - redo? voronoi pattern is right on ringpoints edge- could do a distance check when placing vor points - distance to ring point");
                                Debug.DrawLine(vertices[edge[0]], vertices[edge[1]], Color.magenta);//add
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



                        for (int k = 0; k < intersects.Count; k++)
                        {
                            GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                            c.transform.position = intersects[k];
                            c.transform.name = k.ToString();
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
                }
                else if (intersects.Count == 4)
                {
                    Debug.DrawLine(vertices[edge[0]], vertices[edge[1]], Color.magenta);
                    Debug.Log("intersects == 4!");
                }
            }

            //make cell mesh and object
            if (newPoints.Count > 0)
                Cell(newPoints);
        }
    }

    List<Vector3> AddRingPointsToNextIntersect(List<Vector3> newPoints, List<CellInfo> cellInfos,int currentCellIndex,int currentEdgeIndex, int start, Vector3 currentIntersect)
    {
        bool doCubes = true; //for debug
        bool found = false;

        //List<Vector3> intersects = cellInfos[currentCellIndex].intersectsForEdges[currentEdgeIndex];

        for (int a = start, z = 0; z < ringPoints.Count; a++, z++)
        {
            if (found)
                break;

            if (a > ringPoints.Count - 1)
                a -= ringPoints.Count;
            int next = a + 1;
            if (next > ringPoints.Count - 1)
                next -= ringPoints.Count - 1;


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
                    c.transform.position = ringPoints[next];
                    c.name = "ring point " + currentEdgeIndex;
                }
            }

          
        }

        return newPoints;
    }

    List<Vector3> RingPointsToNextIntersectForTwo(int currentCellIndex, int currentEdgeIndex, int start, List<CellInfo> cellInfos)
    {
        //look for intersects

        List<Vector3> tempPoints = new List<Vector3>();

        List<List<int>> intersectIndexesForEdges = cellInfos[currentCellIndex].intersectIndexesForEdges;
        List<List<Vector3>> intersectsForEdges = cellInfos[currentCellIndex].intersectsForEdges;

        //add the intersect where we are working from before adding ring points (mental count-1)
        //tempPoints.Add(cellInfos[currentCellIndex].intersectsForEdges[currentEdgeIndex][cellInfos[currentCellIndex].intersectsForEdges[currentEdgeIndex].Count-1]);
        //find next point
        int totalFound = 0;


        /*
                    GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    c.transform.position = ringPoints[thisRingIndex];
                    c.transform.name = "ring point A " + thisRingIndex;
                    c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    c.transform.position = ringPoints[nextRingIndex];
                    c.transform.name = "ring point B " + nextRingIndex;

                    //look for any other intersect(s)
                    List<Vector3> foundPoints = new List<Vector3>();
                    */

        for (int a = 0; a < ringPoints.Count; a++)
        {

            //do -1 to check the edge we are on first 
            int thisRingIndex = a + start - 1;

            if (thisRingIndex > ringPoints.Count - 1)
                thisRingIndex -= ringPoints.Count;



            if (thisRingIndex < 0)
                thisRingIndex += ringPoints.Count;

            int nextRingIndex = thisRingIndex + 1;
            if (nextRingIndex > ringPoints.Count - 1)
                nextRingIndex -= ringPoints.Count;
           // GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
           //  c.transform.position = ringPoints[thisRingIndex];
           //   c.transform.name = "ring point A " + thisRingIndex;

            for (int i = 0; i < intersectIndexesForEdges.Count; i++)
            {

                if (i != currentEdgeIndex)
                    continue;

                List<int> intersectIndexes = intersectIndexesForEdges[i];
                List<Vector3> intersects = intersectsForEdges[i];

                
                for (int j = 0; j < intersectIndexes.Count; j++)
                {
                
                 

                    if (totalFound % 2 != 0)
                    {
                       // GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                       // c.transform.position = ringPoints[thisRingIndex];
                      //  c.transform.name = "ring point A " + thisRingIndex;
                    }

                    if (thisRingIndex == intersectIndexes[j])
                    {
                        // c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        //c.transform.position = ringPoints[intersectIndexes[j]];
                        //c.transform.name = " intersect index 0 " + nextRingIndex;

                        GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        c.transform.position = intersects[j];
                        c.transform.name = " intersect v3 ";

                        
                    }


                }


            }

        }

        //if we never found any other intersections, it means the only edge is this one joined by a curve
        if (totalFound == 0)
        {
            Debug.Log("i = " + currentEdgeIndex + ", total found = " + totalFound);
            //send to its own special function to work out which way the points should run round the edge points
            //tempPoints = EndSection(currentCellIndex,currentEdgeIndex,cellInfos, false);
        }

        return tempPoints;
    }


    List<Vector3> RingPointsToNextIntersect(int currentCellIndex, int currentEdgeIndex, int start, List<CellInfo> cellInfos)
    {
        //look for intersects

        List<Vector3> tempPoints = new List<Vector3>();

        List<List<int>> intersectIndexesForEdges = cellInfos[currentCellIndex].intersectIndexesForEdges;
        List<List<Vector3>> intersectsForEdges = cellInfos[currentCellIndex].intersectsForEdges;

        //add the intersect where we are working from before adding ring points (mental count-1)
        //tempPoints.Add(cellInfos[currentCellIndex].intersectsForEdges[currentEdgeIndex][cellInfos[currentCellIndex].intersectsForEdges[currentEdgeIndex].Count-1]);
        //find next point
        int totalFound = 0;
        for (int a = 0; a < ringPoints.Count; a++)
        {
            //do -1 to check the edge we are on first 
            int thisRingIndex = a + start -1;
          
            if (thisRingIndex > ringPoints.Count - 1)
                thisRingIndex -= ringPoints.Count;

            

            if (thisRingIndex < 0)
                thisRingIndex += ringPoints.Count;

            int nextRingIndex = thisRingIndex + 1;
            if (nextRingIndex > ringPoints.Count - 1)
                nextRingIndex -= ringPoints.Count;

            
           
           //  c =GameObject.CreatePrimitive(PrimitiveType.Cube);
           // c.transform.position = ringPoints[nextRingIndex];
          //  c.transform.name = "ring point B " + nextRingIndex;
            
            //look for any other intersect(s)
            List<Vector3> foundPoints = new List<Vector3>();
            for (int z = 0; z < intersectIndexesForEdges.Count; z++)
            {
               // if (z == currentEdgeIndex)//looking for other edges
                //    continue;

                List<int> intersectIndexes = intersectIndexesForEdges[z];
                List<Vector3> intersects = intersectsForEdges[z];

                for (int w = 0; w < intersectIndexes.Count; w++)
                {
                    if (nextRingIndex == intersectIndexes[w] )
                    {
                        GameObject  c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        c.transform.position = intersects[w];
                        c.transform.name = "Found - ringpoints to next intersect " + foundPoints.Count.ToString();

                        //if (!foundPoints.Contains(intersectsForEdges[z][w]))
                        {
                            tempPoints.Add(intersects[w]);
                            totalFound++;
                        }
                    }
                }
            }

            if (totalFound % 2 == 0)
            {
                GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                c.transform.position = ringPoints[nextRingIndex];
                c.transform.name = "ring point A " + nextRingIndex + ", total found = " + totalFound;
                tempPoints.Add(ringPoints[nextRingIndex]);
            }
            /*
            if (foundPoints.Count > 0)
            {
                
                //add the last found point in the list
                tempPoints.Add(foundPoints[foundPoints.Count - 1]);
                break;
            }
            else
            {
                Debug.DrawLine(ringPoints[thisRingIndex], ringPoints[nextRingIndex], Color.yellow);
                tempPoints.Add(ringPoints[nextRingIndex]);

                c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                c.transform.position = ringPoints[nextRingIndex];
                c.transform.name = "ring point - 1 intersect";
            }
            */
        }

        //if we never found any other intersections, it means the only edge is this one joined by a curve
        if (totalFound == 0)
        {
            Debug.Log("i = " + currentEdgeIndex + ", total found = " + totalFound);
            //send to its own special function to work out which way the points should run round the edge points
           // tempPoints = EndSection(currentCellIndex,currentEdgeIndex,cellInfos, false);
        }

        return tempPoints;
    }

    public List<Vector3> EndSection(int currentCellIndex, int currentEdgeIndex, List<CellInfo> cellInfos, bool flipDirection)
    {
        //look for edges

        List<Vector3> tempPoints = new List<Vector3>();

        //Aim of the game is to find which direction to run round the edge points so that we don't create a new cell over already planned/craeted cells
        //We are looking for duplicates
        
        Vector3 startPos = cellInfos[currentCellIndex].intersectsForEdges[currentEdgeIndex][0];
        Vector3 endPos = cellInfos[currentCellIndex].intersectsForEdges[currentEdgeIndex][1];

        int start = cellInfos[currentCellIndex].intersectIndexesForEdges[currentEdgeIndex][0];
        int end = cellInfos[currentCellIndex].intersectIndexesForEdges[currentEdgeIndex][1];

        if (flipDirection)
        {
            start = cellInfos[currentCellIndex].intersectIndexesForEdges[currentEdgeIndex][1];
            end = cellInfos[currentCellIndex].intersectIndexesForEdges[currentEdgeIndex][0];

            startPos = cellInfos[currentCellIndex].intersectsForEdges[currentEdgeIndex][1];
            endPos = cellInfos[currentCellIndex].intersectsForEdges[currentEdgeIndex][0];
        }

        tempPoints.Add(startPos);

        GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
        c.transform.position = cellInfos[currentCellIndex].intersectsForEdges[currentEdgeIndex][0];
        c.transform.name = "End Section - Start " + "Flipped = " + flipDirection;

        //int found = 0;
        List<Vector3> found = new List<Vector3>();
        //check to see how many intersects we run over when we travel round the ring points
        for (int a = 0; a < ringPoints.Count; a++)
        {
            
            int thisRingIndex = a + start;

            if (thisRingIndex > ringPoints.Count - 1)
                thisRingIndex -= ringPoints.Count;

            if (thisRingIndex < 0)
                thisRingIndex += ringPoints.Count;

            int nextRingIndex = thisRingIndex + 1;
            if (nextRingIndex > ringPoints.Count - 1)
                nextRingIndex -= ringPoints.Count;

            //look for other intersect
            if(thisRingIndex == end)
            {
                c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                c.transform.position = cellInfos[currentCellIndex].intersectsForEdges[currentEdgeIndex][1];
                if(flipDirection)
                    c.transform.position = cellInfos[currentCellIndex].intersectsForEdges[currentEdgeIndex][0];

                c.transform.name = "End Section - End " + "Flipped = " + flipDirection;// + foundPoints.Count.ToString();

                tempPoints.Add(endPos);
                break;
            }

            for (int z = 0; z < cellInfos.Count; z++)
            {
                if (z == currentCellIndex)
                    continue;

                List<List<int>> intersectIndexesForCell = cellInfos[z].intersectIndexesForEdges;
                List<List<Vector3>> intersectsForCell = cellInfos[z].intersectsForEdges;

                for (int x = 0; x < intersectIndexesForCell.Count; x++)
                {

                    List<int> intersectIndexes = intersectIndexesForCell[x];
                    List<Vector3> intersects = intersectsForCell[x];
                    

                    for (int q = 0; q < intersectIndexes.Count; q++)
                    {
                        
                        if (intersectIndexes[q] == thisRingIndex)
                        {
                            
                            c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                            c.transform.position = ringPoints[intersectIndexes[q]];
                            c.transform.name = "Found - Endpoints " +", ring = " + thisRingIndex.ToString() + ", cell = " + z + ", intersect index for cell = " + x + " ,intersect index = " + q ;// + foundPoints.Count.ToString();

                            if(!found.Contains(ringPoints[intersectIndexes[q]]))
                                found.Add(ringPoints[intersectIndexes[q]]);
                            //found++;
                            
                        }
                    }
                }
            }

            c = GameObject.CreatePrimitive(PrimitiveType.Cube);
            c.transform.position = ringPoints[thisRingIndex];
            c.transform.name = "ring point";

            tempPoints.Add(ringPoints[thisRingIndex]);
        }

       // tempPoints.Add(endPos);

        if (found.Count > 1 && !flipDirection)
        {
            Debug.Log("Flip");
            //too many - reverse direction and try again
            tempPoints = EndSection(currentCellIndex, currentEdgeIndex,cellInfos, true);
        }
    


        return tempPoints;
    }

    public GameObject Cell(List<Vector3> newPoints)
    {
        
        Vector3 avg = Vector3.zero;
        for (int a = 0; a < newPoints.Count; a++)
        {
             GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
             c.transform.position = newPoints[a];
             c.name = "np";

            avg += newPoints[a];
        }

        avg /= newPoints.Count;

        newPoints.Insert(0, avg);

        List<int> triangles = new List<int>();

        for (int i = 1; i < newPoints.Count - 1; i++)
        {
            if (i == 0)
                continue;

            triangles.Add(0);
            triangles.Add(i);
            triangles.Add(i + 1);
        }

        //last tri (joining last and first)
        triangles.Add(0);
        triangles.Add(newPoints.Count - 1);
        triangles.Add(1);

        GameObject roomFloor = new GameObject();
        roomFloor.transform.position += Vector3.up * 10;///888test
        roomFloor.name = "RoomFloor";
        roomFloor.AddComponent<MeshRenderer>().sharedMaterial = Resources.Load("Grey") as Material;
        MeshFilter mf = roomFloor.AddComponent<MeshFilter>();
        Mesh mesh = new Mesh();
        mesh.vertices = newPoints.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        mf.mesh = mesh;

        return roomFloor;

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
