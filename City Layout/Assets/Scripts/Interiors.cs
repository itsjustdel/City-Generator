using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Interiors : MonoBehaviour
{
    public List<Vector3> ringPoints = new List<Vector3>();
    public List<Vector3> cornerPoints = new List<Vector3>();

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
            c.transform.position = ringPoints[i]- gameObject.transform.position;
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
        int cap = 5;
        //find random positions within floor

        Vector3 shootFrom = gameObject.transform.position - 10f * Vector3.up;
        //GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
        //c.transform.position = shootFrom;
        GameObject underSide = gameObject.transform.Find("UnderSide").gameObject;
        underSide.AddComponent<MeshCollider>();

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
    public void Snip()
    {
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

        List<List<Vector3>> cellList = new List<List<Vector3>>();

        //for each cell edge look for an intersection with floor area
        for (int j = 0; j < cellsToSnip.Count; j++)//cellsToSnip.Count
        {
            //if (j != 5)// && j !=7)
           //     continue;

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

                if(intersects.Count == 1)
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

                    if (Physics.Raycast(shootFrom, Vector3.up, out hit, 2f, LayerMask.GetMask("Roof")))
                    {
                        if (intersects.Count == 0)
                        {
                            Debug.DrawLine(vertices[edge[0]], vertices[edge[1]], Color.white);//add
                            newPoints.Add(vertices[edge[0]]);
                            newPoints.Add(vertices[edge[1]]);
                        }

                        if (intersects.Count == 1)
                        {
                            bool flipDirection = false;

                            List<Vector3> tempPoints = IntersectedOnce(vertices, edge, intersects, intersectIndexes, edges, intersectIndexesForEdges,flipDirection);//FIND FLIP DIRECTION EXAMPLE AND SOLVE********

                            for (int a = 0; a < tempPoints.Count; a++)
                            {
                                newPoints.Add(tempPoints[a]);
                            }
                        }


                    }
                    else
                    {
                        if (intersects.Count == 0)
                            Debug.DrawLine(vertices[edge[0]], vertices[edge[1]], Color.green);//don't add

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
                    //an edge with two intersects can be a special case where this is the only two intersects in the whole cell
                    //if this is so there are two ways in which to build the cell. A circle split in half has two sides
                    //to determine we travel the correct way around the circle/curve, check that we are not stepping over duplicated points found in other intersection/edges
                    bool flipDirection = false;
                  

                    //get points to be added for this edge
                    List<Vector3> tempPoints = IntersectedTwice(intersectIndexes, intersects, intersectIndexesForEdges,flipDirection);

                    for (int a = 0; a < tempPoints.Count; a++)
                    {
                        newPoints.Add(tempPoints[a]);
                    }
                    
                }
            }

            if(newPoints.Count>0)
              Cell(newPoints);

        }

        Debug.Break();

    }

    List<Vector3> IntersectedOnce(Vector3[] vertices,int[] edge, List<Vector3> intersects, List<int> intersectIndexes, List<int[]> edges, List<List<int>> intersectIndexesForEdges,bool flipDirection)
    {
        List<Vector3> tempPoints = new List<Vector3>();


        Debug.DrawLine(vertices[edge[0]], intersects[0], Color.cyan);//add to intersection then look for ring points

        int first = intersectIndexes[0];
        //int second = intersectIndexes[1];

        if (flipDirection)
        {
            first = intersectIndexes[1];
          //  second = intersectIndexes[0];
        }

        if (!flipDirection)
        {
            tempPoints.Add(vertices[edge[0]]);
            tempPoints.Add(intersects[0]);
        }
        else
        {
            //flip em
            tempPoints.Add(intersects[0]);
            tempPoints.Add(vertices[edge[0]]);
            
        }

        //whats the next intersect's index
        //from
        

       // bool found = false;
        int duplicates = 0;
        for (int a = 0; a < ringPoints.Count; a++)
        {
            bool found = false;
            //if (found)
            //    break;

            int thisRingIndex = a + first;
            if (thisRingIndex > ringPoints.Count - 1)
                thisRingIndex -= ringPoints.Count;

            int nextRingIndex = a + first + 1;
            if (nextRingIndex > ringPoints.Count - 1)
                nextRingIndex -= ringPoints.Count;


            GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
            c.transform.position = ringPoints[thisRingIndex];
            c.transform.name = "ring point";

            tempPoints.Add(ringPoints[thisRingIndex]);

           
            //look for any other intersect
            for (int z = 0; z < intersectIndexesForEdges.Count; z++)
            {
                
                List<int> intersectIndexesForTest = intersectIndexesForEdges[z];
                for (int w = 0; w < intersectIndexesForTest.Count; w++)
                {
                    if (nextRingIndex == intersectIndexesForTest[w])
                    {
                        c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                            c.transform.position = ringPoints[nextRingIndex];
                            c.transform.name = "Found";

                        //newPoints.Add(ringPoints[nextRingIndex]);

                        //  newPoints.Add(intersects[z]);
                        //found = true;

                        duplicates++;
                        found = true;
                        break;
                    }
                    if (found)//TESTING THIS - TWO INTERSECTS BETWEEN ONE RING POINT CAUSES PROBLEMS?
                        break;
                }
                if (found)//TESTING THIS - TWO INTERSECTS BETWEEN ONE RING POINT CAUSES PROBLEMS?
                    break;
            }
            if (found)//TESTING THIS - TWO INTERSECTS BETWEEN ONE RING POINT CAUSES PROBLEMS?
                break;


            Debug.DrawLine(ringPoints[thisRingIndex], ringPoints[nextRingIndex], Color.yellow);
            
        }
        Debug.Log("dups " + duplicates);
        if(duplicates >= 2 && !flipDirection )
        {
            Debug.Log("should flip direction");
            flipDirection = true;
           // tempPoints = IntersectedOnce(vertices, edge, intersects, intersectIndexes, edges, intersectIndexesForEdges,flipDirection);
        }

        return tempPoints;
    }

    List<Vector3> IntersectedTwice(List<int> intersectIndexes,List<Vector3> intersects,List<List<int>> intersectIndexesForEdges,bool flipDirection)
    {
        List<Vector3> tempPoints = new List<Vector3>();


        int first = intersectIndexes[0];
       // int second = intersectIndexes[1];

        if(flipDirection)
        {
            first = intersectIndexes[1];
           // second = intersectIndexes[0];
        }

        Debug.DrawLine(intersects[0], intersects[1], Color.red);//add both points and go to ring points until next point

       

        if (!flipDirection)
        {
            tempPoints.Add(intersects[1]);
            tempPoints.Add(intersects[0]);
            


         //   GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
         //   c.transform.position = intersects[0];
         //   c.transform.name = "intersect0 - start";

        }
        else
        {
           
            
            tempPoints.Add(intersects[0]);
            tempPoints.Add(intersects[1]);

           // GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
          //  c.transform.position = intersects[1];
           // c.transform.name = "intersect1 - start";
        }

        //run back round to intitial point unless we find an intersect
        //bool duplicate = false;
        int duplicates = 0;
        for (int a = first, b = 0; b < ringPoints.Count; a++, b++)
        {
            //if (duplicate)
            //    break;

            int index = a;

            if (index > ringPoints.Count - 1)
                index -= ringPoints.Count;
            int nextIndex = index + 1;
            if (nextIndex > ringPoints.Count - 1)
                nextIndex -= ringPoints.Count;


            //check to see if any other cell has the same ring points in use
            //if so, it means we are travelling round the long way, passed cells which will be built - flip direction
            
            for (int q = 0; q < intersectIndexesForEdges.Count; q++)
            {
                //if (duplicates > 0)
                //    break;

                List<int> intersectIndexesForTest = intersectIndexesForEdges[q];
                for (int w = 0; w < intersectIndexesForEdges[q].Count; w++)
                {

                    if (nextIndex == intersectIndexesForTest[w])
                    {
                        GameObject ca = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        ca.transform.position = ringPoints[nextIndex];
                        ca.transform.name = "Duplicate ";

                       // duplicate = true; // should we use this to skip 2 intersects in the same ring point?

                        duplicates++;
                      //  break;
                    }
                }
            }

            if(duplicates < 1)
                tempPoints.Add(ringPoints[index]);

          //  GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
          //  c.transform.position = ringPoints[index];
          //  c.transform.name = "ring point ";
        }

        //if we have more than two duplicates (one for each slice edge) we have travelled round the long way. Flip direction and start again ovewrwriting any previouslypoints
        if (duplicates == 2 && flipDirection == false)
        {
            Debug.Log("Flipping direction");
            flipDirection = true;
            tempPoints = IntersectedTwice(intersectIndexes, intersects, intersectIndexesForEdges, flipDirection);
        }


        return tempPoints;
    }

    public void SnipOld3()
    {
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

        List<List<Vector3>> cellList = new List<List<Vector3>>();


        for (int i = 1; i < ringPoints.Count ; i++)//can use last found point instead of 1? **opto
        {
            //for each cell edge look for an intersection with floor area
            for (int j = 0; j < cellsToSnip.Count; j++)//cellsToSnip.Count
            {
                List<Vector3> newPoints = new List<Vector3>();


                //disable snipped
                cellsToSnip[j].SetActive(false);



                Vector3[] vertices = cellsToSnip[j].GetComponent<MeshFilter>().mesh.vertices;

                //gather all intersections between cell and ring points
                List<IntersectionInfo> intersectionInfos = new List<IntersectionInfo>();
                //start at 1 to skip central vertice            


           
                Debug.DrawLine(ringPoints[i - 1], ringPoints[i], Color.yellow);

                Vector2 p2 = new Vector2(ringPoints[i - 1].x, ringPoints[i - 1].z);
                Vector2 p3 = new Vector2(ringPoints[i].x, ringPoints[i].z);

               
                for (int k = 1; k < vertices.Length; k++)
                {

                    int next = k + 1;
                    if (next >= vertices.Length)
                        next -= vertices.Length - 1; //was 1

                    Vector2 p0 = new Vector2(vertices[k].x, vertices[k].z);
                    Vector2 p1 = new Vector2(vertices[next].x, vertices[next].z);

                    Debug.DrawLine(vertices[k], vertices[next], Color.white);
                    /*
                    GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    c.transform.position = vertices[k];
                    c.name = "vertices k";

                    c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    c.transform.position = vertices[next];
                    c.name = "vertices next";
                    */
                    Vector2 intersection = Vector2.zero;
                    //check for curve vs cell intersection
                    bool secondIntersectFound = false;
                    if (LineSegmentsIntersection(p0, p1, p2, p3, out intersection))
                    {
                        Vector3 intersectV3 = new Vector3(intersection.x, 0f, intersection.y);

                      GameObject   c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        c.transform.position = intersectV3;
                        c.name = "first interesect (added)";

                        //now check for second intersect from this intersect to next cell point
                        p0 = intersection;
                        
                        //for loop around ring points //start where we found first intersect
                        
                        for (int b = i + 1; b < ringPoints.Count + i; b++)
                        {
                            int curveIndex = b;

                            //Debug.Log("count = " + ringPoints.Count);
                            // Debug.Log("index before = " + curveIndex);

                            if (curveIndex > ringPoints.Count - 1)
                                curveIndex -= ringPoints.Count;

                            int prevCurveIndex = curveIndex - 1;
                            if (prevCurveIndex < 0)
                                prevCurveIndex += ringPoints.Count - 1;

                            //Debug.Log("index after = " + curveIndex);


                            Debug.DrawLine(ringPoints[curveIndex], ringPoints[prevCurveIndex], Color.cyan);

                            p2 = new Vector2(ringPoints[curveIndex].x, ringPoints[curveIndex].z);
                            p3 = new Vector2(ringPoints[prevCurveIndex].x, ringPoints[prevCurveIndex].z);


                            if (LineSegmentsIntersection(p0, p1, p2, p3, out intersection)) //**opto raycast for next point outside and if inside skip this check?
                            {
                                intersectV3 = new Vector3(intersection.x, 0f, intersection.y);

                                secondIntersectFound = true;

                                //stop looking
                                break;
                            }
                        }

                        if(!secondIntersectFound)
                        {
                            c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                            c.transform.position = vertices[next];
                            c.name = "cell point (added)";
                        }
                        else
                        {
                            c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                            c.transform.position = new Vector3(intersection.x,0f,intersection.y);
                            c.name = "second intersect (added)";
                          
                        }


                        if (secondIntersectFound)
                            break;

                        //found second intersect above, check if cell points get added correctly ( !secondintersectfounf) - above


                        /*
                        c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        c.transform.position = vertices[k];
                        c.name = "vertices k";

                        c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        c.transform.position = vertices[next];
                        c.name = "vertices next";
                        */



                        //start going round vertice from next vertice - only try and go round once ( q < vertices length)
                        for (int a = next, q = 0; q < vertices.Length-1; a++,q++)
                        {
                            int index = a;
                            int nextIndex = index + 1;

                            if (index > vertices.Length - 1)
                                index -= vertices.Length - 1;

                            if (index == 0)
                                continue;

                          //  Debug.Log("Next = " + nextIndex + ", length = " + vertices.Length);
                            if (nextIndex > vertices.Length - 1)
                                nextIndex -= vertices.Length -1;

                            if (nextIndex == 0)
                                nextIndex++;

                            /*

                            Debug.Log("After" +" - Next = " + nextIndex + ", length = " + vertices.Length);
                            
                            c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                            c.transform.position = vertices[index];
                            c.name = "index after intersect " + index  + ", Length = " + vertices.Length;

                            c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                            c.transform.position = vertices[nextIndex];
                            c.name = "next index after intersect" + nextIndex;
                            */
                            
                            p0 = new Vector2(vertices[index].x, vertices[index].z);
                            p1 = new Vector2(vertices[nextIndex].x, vertices[nextIndex].z);

                            //search from where we foud our intersect (this search stops one vefore this intersect)
                            //look to add cell point or add intersect

                            bool intersectFound = false;
                            for (int b = i + 1; b < ringPoints.Count + i; b++)
                            {
                                int curveIndex = b;

                                //Debug.Log("count = " + ringPoints.Count);
                                // Debug.Log("index before = " + curveIndex);

                                if (curveIndex > ringPoints.Count - 1)
                                    curveIndex -= ringPoints.Count;

                                int prevCurveIndex = curveIndex - 1;
                                if (prevCurveIndex < 0)
                                    prevCurveIndex += ringPoints.Count - 1;

                                //Debug.Log("index after = " + curveIndex);


                                Debug.DrawLine(ringPoints[curveIndex], ringPoints[prevCurveIndex], Color.red);

                                p2 = new Vector2(ringPoints[curveIndex].x, ringPoints[curveIndex].z);
                                p3 = new Vector2(ringPoints[prevCurveIndex].x, ringPoints[prevCurveIndex].z);


                                if (LineSegmentsIntersection(p0, p1, p2, p3, out intersection)) //**opto raycast for next point outside and if inside skip this check?
                                {
                                    intersectV3 = new Vector3(intersection.x, 0f, intersection.y);

                                    

                                    intersectFound = true;

                                    //skip vertice to this intersection point
                                    k = index;
                                    //jump out of this vertices loop
                                    q = vertices.Length + 1;
                                    break;
                                }
                            }

                            if(intersectFound)
                            {
                                c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                                c.transform.position = intersectV3;
                                c.name = "intersect added";
                            }
                            else
                            {
                                //add cell vertice
                                c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                                c.transform.position = vertices[nextIndex];
                                c.name = "cell vertice added";
                            }
                        }
                    }
                }                
            }
        }

        Debug.Break();

    }

    public GameObject Cell(List<Vector3> newPoints)
    {

        Vector3 avg = Vector3.zero;
        for (int a = 0; a < newPoints.Count; a++)
        {
           //  GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
           //  c.transform.position = newPoints[a];
           //  c.name = "np";

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

    public void SnipOld2()
    {
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
        ringPoints.Add(ringPoints[0]);

        List<List<Vector3>> cellList = new List<List<Vector3>>();

        //Debug.DrawLine(ringPoints[i], ringPoints[i - 1]);///0y                

        List<Vector3> tempPoints = new List<Vector3>();

       
        //for each cell edge look for an intersection with floor area
        for (int j = 0; j < cellsToSnip.Count; j++)//cellsToSnip.Count
        {
           
            //disable snipped
            cellsToSnip[j].SetActive(false);

            

            Vector3[] vertices = cellsToSnip[j].GetComponent<MeshFilter>().mesh.vertices;

            //gather all intersections between cell and ring points
            List<IntersectionInfo> intersectionInfos = new List<IntersectionInfo>();
            //start at 1 to skip central vertice            
           

            for (int i = 1; i < ringPoints.Count - 1; i++)//can use last found point instead of 1? **opto
            {
                Debug.DrawLine(ringPoints[i - 1], ringPoints[i], Color.yellow);

                Vector2 p2 = new Vector2(ringPoints[i - 1].x, ringPoints[i - 1].z);
                Vector2 p3 = new Vector2(ringPoints[i].x, ringPoints[i].z);
                for (int k = 1; k < vertices.Length; k++)
                {

                    int next = k + 1;
                    if (next >= vertices.Length)
                        next -= vertices.Length-1; //was 1

                    Vector2 p0 = new Vector2(vertices[k].x, vertices[k].z);
                    Vector2 p1 = new Vector2(vertices[next].x, vertices[next].z);

                    Debug.DrawLine(vertices[k], vertices[next], Color.white);

                   

                    Vector2 intersection = Vector2.zero;
                    //check for curve vs cell intersection
                    if (LineSegmentsIntersection(p0, p1, p2, p3, out intersection))
                    {
                        Vector3 intersectionV3 = new Vector3(intersection.x, 0f, intersection.y);
                      
                        //check to see if intersection is exiting or entering the ring points area
                        RaycastHit hit;
                        bool thisPointOutside = false;
                        bool nextPointOutside = false;
                        if (Physics.Raycast(vertices[k] - Vector3.up, Vector3.up, out hit, 2f, LayerMask.GetMask("Roof")))
                        {

                        }
                        else
                        {
                            thisPointOutside = true;
                        }
                        if (Physics.Raycast(vertices[next] - Vector3.up, Vector3.up, out hit, 2f, LayerMask.GetMask("Roof")))
                        {

                        }
                        else
                        {
                            nextPointOutside = true;
                        }

                        IntersectionInfo iI = new IntersectionInfo(intersectionV3, k, next, i - 1, i, thisPointOutside, nextPointOutside);
                        intersectionInfos.Add(iI);

                    }
                }
            }

            //create ring points with intersections
            List<Vector3> newPoints = new List<Vector3>();
            
            for (int i = 0; i < intersectionInfos.Count; i++)
            {
                GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                c.transform.position = intersectionInfos[i].intersection;
                c.name = "intersection " + j.ToString() + " this outside = " + intersectionInfos[i].thisPointOutside + ", next outside = " + intersectionInfos[i].nextPointOutside;

                int nextIntersectionInfo = i + 1;
                if (nextIntersectionInfo > intersectionInfos.Count - 1)
                    nextIntersectionInfo -= intersectionInfos.Count;


                //add intersection
                newPoints.Add(intersectionInfos[i].intersection);

                // 3 possible situation
                //this point inside and next point outside
                //this point outside and next point inside
                //this point outside and next point outside   

              


                if (!intersectionInfos[i].thisPointOutside && intersectionInfos[i].nextPointOutside)
                {
                    //add til next intersection
                    for (int a = 0; a < ringPoints.Count; a++)
                    {
                        //clamp (using for loop for safety)
                        int ringIndex = intersectionInfos[i].ringIndex1;
                        ringIndex += a;
                        if (ringIndex > ringPoints.Count - 1)
                            ringIndex-= ringPoints.Count;
                        
                       
                        //look for next intersection

                        if (ringIndex == intersectionInfos[nextIntersectionInfo].ringIndex1)
                        {
                            //stop adding ring
                            break;
                        }
                        else
                        {
                            c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                            c.transform.position = ringPoints[ringIndex];
                            c.name =", Ring index this = " + ringIndex + ", target index = " + intersectionInfos[nextIntersectionInfo].ringIndex1;

                            newPoints.Add(ringPoints[ringIndex]);
                        }
                    }

                    
                }

                //if going inside
                else if(intersectionInfos[i].thisPointOutside && !intersectionInfos[i].nextPointOutside)
                {
                    //add vertices til next intersection
                    //start at 1 skips central vertice
                    for (int a = 0; a < vertices.Length; a++)
                    {
                        //clamp
                        int verticeIndex = intersectionInfos[i].cellIndex1;
                        verticeIndex += a;
                        if (verticeIndex > vertices.Length - 1)
                            verticeIndex -= vertices.Length -1;

                        if(verticeIndex == intersectionInfos[nextIntersectionInfo].cellIndex1)
                        {
                            //stop
                            break;
                        }
                        else
                        {
                            c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                            c.transform.position = vertices[verticeIndex];
                            c.name = "verticeIndex = " + verticeIndex + ", vertices length = " + vertices.Length + "target index = " + intersectionInfos[nextIntersectionInfo].cellIndex1;

                            newPoints.Add(vertices[verticeIndex]);
                        }
                    }
                }   
                else if(intersectionInfos[i].thisPointOutside && intersectionInfos[i].nextPointOutside)
                {

                    //not working, 
                    //cases - only 2 intersects and one curve part
                    //check next intersection is on the same edge                  

                    if (intersectionInfos.Count == 2)
                    {

                        Debug.Log("count = 2");
                        if (i == 0)
                        {
                            Debug.Log("i = 0");

                            //use temp list and check length? or test for duplicates (more thorough)


                            for (int a = 0; a < ringPoints.Count; a++)//can go wrong way, just shorter one? or look for other ring points fomr other cells? but what if this is the first to be made?
                            {
                                //clamp (using for loop for safety)
                                int ringIndex = intersectionInfos[i].ringIndex1;
                                ringIndex += a;
                                if (ringIndex > ringPoints.Count - 1)
                                    ringIndex -= ringPoints.Count;


                                //look for next intersection
                                

                                if (ringIndex == intersectionInfos[nextIntersectionInfo].ringIndex1)
                                {
                                    //stop adding ring
                                    Debug.Log("Both outside break");
                                    break;
                                }
                                else
                                {
                                    c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                                    c.transform.position = ringPoints[ringIndex];
                                    c.name = "BOTH OUTSIDE, two intersects ring point = " + ringIndex + "target index = " + intersectionInfos[nextIntersectionInfo].ringIndex1;

                                    newPoints.Add(ringPoints[ringIndex]);
                                }
                            }

                            //newPoints = temp;
                            //check to see if we have went the ong way round - ITS POSSIBLE WE CAN HAVE A LONG "EAR" - if seeing this wil need to check afterwards for duplicates with other cells
                           // if(temp.Count > ringPoints.Count/2)
                            {
                             //   continue;
                            }
                           // else
                            //    newPoints

                        }
                       // else
                            //add intersect and finish
                           // newPoints.Add(intersectionInfos[i].intersection);

                    }
                    else
                    {
                        //clamp
                        int prevIntersectInfo = i - 1;
                        if (prevIntersectInfo < 0)
                            prevIntersectInfo += intersectionInfos.Count;

                        //check for double intersect on same edge - changes behaviour
                        if (intersectionInfos[i].cellIndex0 == intersectionInfos[prevIntersectInfo].cellIndex0 && intersectionInfos[i].cellIndex1 == intersectionInfos[prevIntersectInfo].cellIndex1)
                        {
                            Debug.Log("shared");
                            
                           // newPoints.Add(intersectionInfos[nextIntersectionInfo].intersection);
                     

                            c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                            c.transform.position = vertices[intersectionInfos[i].cellIndex0];
                            c.name = "same edge 0";

                            c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                            c.transform.position = vertices[intersectionInfos[i].cellIndex1];
                            c.name = "same edge 1";

                            //add til next intersection
                            for (int a = 0; a < ringPoints.Count; a++)
                            {
                                //clamp (using for loop for safety)
                                int ringIndex = intersectionInfos[i].ringIndex1;
                                ringIndex += a;
                                if (ringIndex > ringPoints.Count - 1)
                                    ringIndex -= ringPoints.Count;


                                //look for next intersection

                                if (ringIndex == intersectionInfos[nextIntersectionInfo].ringIndex1)
                                {
                                    //stop adding ring
                                    break;
                                }
                                else
                                {
                                    c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                                    c.transform.position = ringPoints[ringIndex];
                                    c.name = "Same edge adding til next";

                                    newPoints.Add(ringPoints[ringIndex]);
                                }
                            }
                        }


                        Debug.Log("Here?");
                    }
                    //case2
                    //intersect to intersect but missing a cell edge

                //** TRY AGAIN YO ASS GOT SACKED


                }
                else
                {
                    Debug.Log("unexpected");
                }

                
            }


            if (newPoints.Count == 0)
                continue;

            Vector3 avg = Vector3.zero;
            for (int a = 0; a < newPoints.Count; a++)
            {
               // GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
               // c.transform.position = newPoints[a];
               // c.name = "np";

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
            roomFloor.name = "RoomFloor " + j.ToString();
            roomFloor.AddComponent<MeshRenderer>().sharedMaterial = Resources.Load("Grey") as Material;
            MeshFilter mf = roomFloor.AddComponent<MeshFilter>();
            Mesh mesh = new Mesh();
            mesh.vertices = newPoints.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mf.mesh = mesh;

            
        }
        for (int i = 0; i < cellsToSnip.Count; i++)
        {


            Vector3[] vertices = cellsToSnip[i].GetComponent<MeshFilter>().mesh.vertices;
            Color color = Color.blue;
            if (i == 1)
                color = Color.red;
            if (i == 2)
                color = Color.green;
            if (i == 3)
                color = Color.yellow;
            if (i == 4)
                color = Color.cyan;
            if (i == 5)
                color = Color.magenta;
            if (i == 6)
                color = Color.yellow;


            for (int j = 1; j < vertices.Length; j++)
            {
                //wrap
                int next = j + 1;
                if (next >= vertices.Length)
                    next = 1;
                // Debug.DrawLine(vertices[j], vertices[next], color);



            }
        }
        Debug.Break();





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

    public void SnipOld()
    {
        List<GameObject> interiorCells = transform.Find("Interior").GetComponent<MeshGenerator>().cells;
        Debug.Log(interiorCells.Count);
        
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
        ringPoints.Add(ringPoints[0]);



        List<List<Vector3>> cellList = new List<List<Vector3>>();
       
            //Debug.DrawLine(ringPoints[i], ringPoints[i - 1]);///0y                

            List<Vector3> tempPoints = new List<Vector3>();

        //for each cell edge look for an intersection with floor area
        for (int j = 0; j < cellsToSnip.Count; j++)//cellsToSnip.Count
        {
            //disable snipped
            cellsToSnip[j].SetActive(false);

            List<Vector3> newPoints = new List<Vector3>();

            Vector3[] vertices = cellsToSnip[j].GetComponent<MeshFilter>().mesh.vertices;
            //start at 1 to skip central vertice
            bool intersectFound = false;
            bool intersect2Found = false;
            int intersect0index = 0;
            bool onceRound = false;
            for (int k = 1; k < vertices.Length; k++)
            {
                

                int next = k + 1;
                if (next >= vertices.Length)
                    next = 1;

                //if this point is outside andnext point is outside of floor space, skip
                bool thisPointOutside = false;
                bool nextPointOutside = false;
                RaycastHit hit;
                if (Physics.Raycast(vertices[k] - Vector3.up, Vector3.up, out hit, 2f, LayerMask.GetMask("Roof")))
                {

                }else
                {
                    thisPointOutside = true;
                }
                if (Physics.Raycast(vertices[next] - Vector3.up, Vector3.up, out hit, 2f, LayerMask.GetMask("Roof")))
                {

                }
                else
                {
                    nextPointOutside = true;
                }
                Debug.DrawLine(vertices[k], vertices[next]);
                //if both are outside, there will be no intersect and it will be the are of the cell we are not interested in
                if (thisPointOutside && nextPointOutside )
                    continue;

                //looking to start from outside coming in
                if (!intersectFound && !thisPointOutside)
                    continue;

                if (intersectFound && !thisPointOutside)
                    newPoints.Add(vertices[k]);


                for (int i = 1; i < ringPoints.Count - 1; i++)//can use last found point instead of 1? **opto
                {

                    Debug.DrawLine(ringPoints[i-1], ringPoints[i], Color.yellow);

                    Vector2 p2 = new Vector2(vertices[k].x, vertices[k].z);
                    Vector2 p3 = new Vector2(vertices[next].x, vertices[next].z);

                    Vector2 intersection = Vector2.zero;

                    Vector2 p0 = new Vector2(ringPoints[i - 1].x, ringPoints[i - 1].z);
                    Vector2 p1 = new Vector2(ringPoints[i].x, ringPoints[i].z);

            
                    List<Vector3> newVertices = new List<Vector3>();
                   

                    //check for curve vs cell intersection
                    if (LineSegmentsIntersection(p0, p1, p2, p3, out intersection))
                    {
                        Vector3 intersectionV3 = new Vector3(intersection.x, 0f, intersection.y);
                        GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        c.transform.position = intersectionV3;
                        c.name = "intersection " + i.ToString();


                        //check for second intersection on line too*****************

                        newPoints.Add(intersectionV3);


                        if(!intersectFound)
                        {
                            //remember where the first intersect was
                            intersectFound = true;
                            intersect0index = i;

                            
                        }
                        else if (intersectFound)
                        {
                            intersectFound = !intersectFound;

                            //run through run points adding to list back to first intersect point

                            //**FIND A WAY TO KNOW HOW MANY INTERSECTS THERE ARE FIRST THEN RUN IN A LOOP INSTEAD OF MANUALLY WRITING OUT LIKE THIS BELOW
                            

                            for (int a = i+1,b =0; b < ringPoints.Count + i; a++,b++)
                            {
                                Vector2 thirdIntersect = Vector2.zero;
                                //check for thrid intersection against any other edge
                                for (int n = 0; n < vertices.Length; n++)
                                {
                                    next = n + 1;
                                    if (next >= vertices.Length)
                                        next = 1;

                                    p2 = new Vector2(vertices[n].x, vertices[n].z);
                                    p3 = new Vector2(vertices[next].x, vertices[next].z);

                                    p0 = new Vector2(ringPoints[a - 1].x, ringPoints[a - 1].z);
                                    p1 = new Vector2(ringPoints[a].x, ringPoints[a].z);

                                    if (LineSegmentsIntersection(p0, p1, p2, p3, out thirdIntersect))
                                    {
                                        c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                                        c.transform.position = new Vector3(thirdIntersect.x, 0f, thirdIntersect.y);
                                        c.name = "third intersect";
                                    }
                                }


                                if (a > ringPoints.Count - 1)                                
                                    a -= ringPoints.Count;


                                if (a == intersect0index)
                                {
                                    intersect2Found = true;
                                    break;
                                }
                                else
                                    newPoints.Add(ringPoints[a]);
                            }


                        }

                        break;
                    }
                }

                if (k == vertices.Length - 1)
                {
                    if (!intersect2Found && !onceRound)
                    {
                        //restart in search for second intersect
                        onceRound = true;
                        k = 0;
                    }
                }
            }

           if(newPoints.Count == 0)
            {
                //remove any outlying cells
                Vector3 shootFrom = vertices[0] - Vector3.up;
                RaycastHit hit;
                //but keeep any fully enclosed cells that had no intersections
                if (Physics.Raycast(shootFrom, Vector3.up, out hit, 2f, LayerMask.GetMask("Roof")))
                {
                    cellsToSnip[j].SetActive(true);
                    cellsToSnip[j].name = "RoomFloor (enclosed)" + j.ToString();
                    cellsToSnip[j].GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("Grey") as Material;

                }
                else
                    cellsToSnip[j].SetActive(false);


                continue;
            }

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

            for (int i = 1; i < newPoints.Count-1; i++)
            {
                if (i == 0)
                    continue;

                triangles.Add(0);
                triangles.Add(i);
                triangles.Add(i+1);                
            }

            //last tri (joining last and first)
            triangles.Add(0);
            triangles.Add(newPoints.Count-1);
            triangles.Add(1);

            GameObject roomFloor = new GameObject();
            roomFloor.transform.position += Vector3.up * 10;///888test
            roomFloor.name = "RoomFloor " + j.ToString();
            roomFloor.AddComponent<MeshRenderer>().sharedMaterial = Resources.Load("Grey") as Material;
            MeshFilter mf = roomFloor.AddComponent<MeshFilter>();
            Mesh mesh = new Mesh();
            mesh.vertices = newPoints.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mf.mesh = mesh;

          
        }
  
        for (int i = 0; i < cellsToSnip.Count; i++)
        {
            

            Vector3[] vertices = cellsToSnip[i].GetComponent<MeshFilter>().mesh.vertices;
            Color color = Color.blue;
            if (i == 1)
                color = Color.red;
            if (i == 2)
                color = Color.green;
            if (i == 3)
                color = Color.yellow;
            if (i == 4)
                color = Color.cyan;
            if (i == 5)
                color = Color.magenta;
            if (i == 6)
                color = Color.yellow;
            

            for (int j = 1; j < vertices.Length; j++)
            {
                //wrap
                int next = j + 1;
                if (next >= vertices.Length)
                    next = 1;
               // Debug.DrawLine(vertices[j], vertices[next], color);



            }
        }
        Debug.Break();
    




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
