using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Interiors : MonoBehaviour
{
    public List<Vector3> ringPointsUnique = new List<Vector3>();
    public List<Vector3> cornerPoints = new List<Vector3>();

    public bool snip;
    private void Start()
    {
        //
        CreateVoronoi();
        //need to wait for voronoi to finish before snipping
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
       

        for (int i = 0; i < ringPointsUnique.Count; i++)
        {
            GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
            c.transform.position = ringPointsUnique[i];
            c.transform.localScale *= 0.5f;
        }

        for (int i = 0; i < cellsToSnip.Count; i++)
        {

        }

    }
}
