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
        Debug.Log(interiorCells.Count);
        
        GameObject underSide = transform.Find("UnderSide").gameObject;
        
        //get rid of any cells which are not overlapping
        List<GameObject> cellsToSnip = new List<GameObject>();
        RaycastHit hit;
        for (int i = 0; i < interiorCells.Count; i++)
        {
            //center point is vertice 0
            Vector3 shootFrom = interiorCells[i].GetComponent<MeshFilter>().mesh.vertices[0] ;
            shootFrom -= Vector3.up;

            
            if (Physics.Raycast(shootFrom, Vector3.up, out hit, 2f, LayerMask.GetMask("Roof")))
            {
                cellsToSnip.Add(interiorCells[i]);
            }
            else
            {
                interiorCells[i].SetActive(false);
            }
        }

        //find mesh edges which overlap with outside of building floor       
        ringPoints.Add(ringPoints[0]);



        List<List<Vector3>> cellList = new List<List<Vector3>>();
       
            //Debug.DrawLine(ringPoints[i], ringPoints[i - 1]);///0y                

            List<Vector3> tempPoints = new List<Vector3>();

        //for each cell edge look for an intersection with floor area
        for (int j = 0; j < cellsToSnip.Count; j++)//cellsToSnip.Count
        {

            List<Vector3> newPoints = new List<Vector3>();

            Vector3[] vertices = cellsToSnip[j].GetComponent<MeshFilter>().mesh.vertices;
            //start at 1 to skip central vertice
            bool intersectFound = false;
            int intersect0index = 0;
            for (int k = 1; k < vertices.Length; k++)
            {
                

                int next = k + 1;
                if (next >= vertices.Length)
                    next = 1;

                //if this point is outside andnext point is outside of floor space, skip
                bool thisPointOutside = false;
                bool nextPointOutside = false;

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

                //if both are outside, there will be no intersect and it will be the are of the cell we are not interested in
                //if (thisPointOutside && nextPointOutside )
                  //  continue;

                Debug.DrawLine(vertices[k], vertices[next]);

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
                        c.name = "intersection";


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

                            //find which way to run?

                            for (int a = i+1,b =0; b < ringPoints.Count + i; a++,b++)
                            {
                                if (a > ringPoints.Count - 1)
                                    a -= ringPoints.Count;

                                if (a == intersect0index)
                                    break;
                                else
                                    newPoints.Add(ringPoints[a]);
                            }


                        }

                        break;
                    }
                }
            }

            for (int a = 0; a < newPoints.Count; a++)
            {
                GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                c.transform.position = newPoints[a];
                c.name = "np";

            }
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
