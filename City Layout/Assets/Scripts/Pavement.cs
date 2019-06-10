using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Pavement : MonoBehaviour {

    public float depth = 3f;
    public float borderSize = 2f;

    public float cornerSizeScaler = .1f;
    public float maxCornerSize = 3f;
    public float curveAccuracyCorners = 100f;//making this lower makes hexagonny style shapes
    public float curveStepSize = .5f;
    BezierSpline bezierForCorners;
    public GameObject pavement;
    // Use this for initialization
    private void Awake()
    {
        enabled = false;
    }

    public void Start ()
    {
        bezierForCorners = gameObject.AddComponent<BezierSpline>();

        List<Vector3> cornerPoints = CornerPoints();

        List<List<Vector3>> edgePoints = EdgePoints(cornerPoints);

        List<List<Vector3>> curvedCornerPoints = RingCornerPoints(edgePoints, cornerPoints);

        //now combine these lists to create a sequential ring around cell
        List<Vector3> ringPoints = RingPoints(edgePoints, curvedCornerPoints);

        //game objects
        pavement = TriangulateRing(ringPoints, false);
        pavement.name = "Curved Edge Cell";
        pavement.transform.parent = transform;
        pavement.transform.position = transform.position + (Vector3.up * depth);

        GameObject extrusion = new GameObject();
        extrusion.transform.parent = pavement.transform;
        extrusion.name = "Extrusion for Curved Edge Cell";
        extrusion.AddComponent<MeshRenderer>().sharedMaterial = Resources.Load("Ground") as Material;
        MeshFilter meshFilter = extrusion.AddComponent<MeshFilter>();
        meshFilter.mesh = Extrude(ringPoints, depth, 1f, true);
        extrusion.transform.position = transform.position;

        //work out miters
        List<Vector3> miters = Miters(cornerPoints, borderSize);
        PositionsBetweenCorners(miters);

    }    

    List<Vector3> PositionsBetweenCorners(List<Vector3> miters)
    {
        List<Vector3> positions = new List<Vector3>();

        //make full loop
        miters.Add(miters[0]);

        // we want to roughly place items by variable step
        float step = 5f;
        //meaning that we should try to fit in as many points as we can every "step" between the miter points
        
        //
        //run between border points and create positions
        //how often a position is made

        for (int i = 0; i < miters.Count; i++)
        {
            if (i == 0)
                continue;

            float distance = Vector3.Distance(miters[i - 1], miters[i]);
            //how many to do
            float amount = (distance / step);
            float evenStep = distance / (int)amount;
            
            Vector3 dir = (miters[i] - miters[i -1]).normalized;
            
            //this loop will place the points and spread them out evenly
            for (float j = 0; j < distance; j+= evenStep)
            {
                Vector3 p = miters[i-1] + dir * (j );

                //   GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                // c.transform.position = p + transform.position + Vector3.up*depth;
                //c.name = "p0";
                //Destroy(c, 5);

                positions.Add(p);
            }

        }
        return positions;
    }

    List<Vector3> Miters(List<Vector3> ringPoints, float borderSize)
    {
        List<Vector3> miters = new List<Vector3>();

        //ringPoints.Distinct();

        for (int i = 0; i < ringPoints.Count - 1; i++)
        {
            int prevInt = i - 1;
            int thisInt = i;
            int nextInt = i + 1;
            int nextNextInt = i + 2;

            if (prevInt < 0)
            {
                //if central point, move to last //last point in list is same as first, so actual last is -2
                prevInt += ringPoints.Count -1;
            }
            if (nextInt >= ringPoints.Count)
            {
                //if next is over vertices length, put to start
                nextInt -= ringPoints.Count; 
            }
            if (nextNextInt >= ringPoints.Count)
            {
                nextNextInt -= ringPoints.Count;
            }

            Vector3 p0 = ringPoints[prevInt];
            Vector3 p1 = ringPoints[thisInt];
            Vector3 p2 = ringPoints[nextInt];
            Vector3 p3 = ringPoints[nextNextInt];

            //so order around cell is previous,p0,p1,next

            Vector3 miterDirection0 = MiterDirection(p0, p1, p2,borderSize);
            Vector3 miterDirection1 = MiterDirection(p1, p2, p3, borderSize);

            
            if(i == 0)
                Debug.DrawLine(p1 + transform.position, p1 + transform.position + miterDirection0 * -borderSize, Color.red);
            else
                Debug.DrawLine(p1 + transform.position, p1 + transform.position + miterDirection0 * -borderSize, Color.blue);


            Vector3 m0 = p1 + miterDirection0 * -borderSize;
            Vector3 m1 = p1 + miterDirection0 * -borderSize;
            miters.Add(m0);
            //miters.Add(m1);

        }

        return miters;
    }

    List<Vector3> CornerPoints()
    {
        List<Vector3> cornerPoints = new List<Vector3>();

        //create points around cell
        Vector3[] vertices = GetComponent<MeshFilter>().mesh.vertices;

      
        for (int i = 0; i < vertices.Length; i++)
        {
            if (i == 0)
                continue;

            int prevIndex = i - 1;
            if (prevIndex == 0)
                prevIndex = vertices.Length- 1;

            if (Vector3.Distance(vertices[i], vertices[prevIndex]) < maxCornerSize)
                continue;

            cornerPoints.Add(vertices[i]);
         
        }

        if (Vector3.Distance(vertices[1], vertices[vertices.Length-1]) >= maxCornerSize)            
            cornerPoints.Add(vertices[1]);

        return cornerPoints;
    }

    List<List<Vector3>> EdgePoints(List<Vector3> cornerPoints)
    {
        List<List<Vector3>> ringPoints = new List<List<Vector3>>();

        //create points around cell

        for (int i = 0; i < cornerPoints.Count - 1; i++)
        {

            List<Vector3> temp = new List<Vector3>();
            //add point a little towards next point

            //work out corner size and clamp if too big
            float distance = Vector3.Distance(cornerPoints[i], cornerPoints[i + 1]);
            float tempCornerSize = distance * cornerSizeScaler;
            if (tempCornerSize > maxCornerSize)
                tempCornerSize = maxCornerSize;


            Vector3 toNext = (cornerPoints[i +1 ] - cornerPoints[i]).normalized;

            Vector3 p0 = cornerPoints[i] + toNext * tempCornerSize;
            //add add a point just shy of next point (by cornersize variable)
            Vector3 p1 = cornerPoints[i + 1] - toNext * tempCornerSize;

            bool showCubes = false;
            if (showCubes)
            {
                GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                c.transform.position = transform.position + p0;
                c.name = "p0";
                Destroy(c, 1);

                c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                c.transform.position = transform.position + p1;
                c.name = "p1";
                Destroy(c, 1);
            }

            temp.Add(p0);
            temp.Add(p1);

            ringPoints.Add(temp);
        }

        return ringPoints;
    }

    List<List<Vector3>> RingCornerPoints(List<List<Vector3>> ringPoints, List<Vector3> cornerPoints)
    {
        bezierForCorners = gameObject.GetComponent<BezierSpline>();
        //save in case we want to remove them later, it adds these beizer's to gameobject
        //        cornerSplines.Add(bezierForCorners);

        List<List<Vector3>> ringPointsCorner = new List<List<Vector3>>();
        for (int i = 0; i < ringPoints.Count; i++)
        {
            int thisIndex = i;
            int nextIndex = i + 1;
            int verticeCell = i + 2;
            //covers last index and loops back to first
            if (nextIndex > ringPoints.Count - 1)
            {
                // Debug.Log("This happened 1");
                nextIndex = 0;
                verticeCell = 1;
            }

            //get last point from this list and first from next
            Vector3 lastPoint = ringPoints[i][ringPoints[i].Count - 1];
            Vector3 firstFromNext = ringPoints[nextIndex][0];
            int indexForCellVertice = i + 1;

            if (indexForCellVertice == cornerPoints.Count)
            {
                // Debug.Log("This happened 2");
                indexForCellVertice = 0;
            }

            Vector3 nearestCellVertice = Vector3.zero;
            // Debug.Log("index = " + indexForCellVertice + ",altered corner points count = " + alteredCornerPoints.Count);
            if (indexForCellVertice < cornerPoints.Count)
            {
                nearestCellVertice = cornerPoints[indexForCellVertice];// cellVertices[verticeCell] + Vector3.up*height;
            }
            else
            {
                Debug.Log("out of indexx");
                //  Debug.Log("index = " + indexForCellVertice + ",altered corner points count = " + alteredCornerPoints.Count);
                Debug.Break();
            }
            //we need to scale vertice cell


            //control points of curve
            //dont normalise, we will use curve control point variable to use a percentage of this size, however .33f looks nice
            float curveControlPointSize = 0.33f;//??? what to use

            Vector3 dirToPrev = (lastPoint - nearestCellVertice);
            Vector3 cp0 = nearestCellVertice + dirToPrev * curveControlPointSize;

            Vector3 dirToNext = (firstFromNext - nearestCellVertice);
            Vector3 cp1 = nearestCellVertice + dirToNext * curveControlPointSize;

            //Debug.DrawLine(nearestCellVertice, cp0, Color.red);
            // Debug.DrawLine(nearestCellVertice, cp1, Color.green);

            //add bookending curve points with control points between them
            Vector3[] bezPoints = new Vector3[4] { lastPoint, cp0, cp1, firstFromNext };

            for (int j = 0; j < bezPoints.Length; j++)
            {
                bezPoints[j] -= transform.position;//spline class takes into account transform position(should go in and modify it)
            }
            bezierForCorners.points = bezPoints;


            float step = 1f / curveAccuracyCorners;
            float distanceSinceLast = 0f;
            Vector3 lastStepPoint = bezierForCorners.GetPoint(0 * step);

            List<Vector3> tempCornerPoints = new List<Vector3>();


            //possible we need to run from middle of curve out to both sides like we did above with flat sides so it is symmetrical?
            for (float k = 0; k < curveAccuracyCorners - step * 0.5f; k += step)//half a step for tolerance
            {
                Vector3 p0 = bezierForCorners.GetPoint(k * step);
                Vector3 d = Quaternion.Euler(0, 90, 0) * bezierForCorners.GetDirection(k) * step;
                // Debug.DrawLine(p0, p0 + d, Color.red);

                if (distanceSinceLast > curveStepSize)
                {
                    Vector3 p1 = bezierForCorners.GetPoint((k + step) * step);
                    tempCornerPoints.Add(p0);
                    distanceSinceLast = 0f;

                }
                else
                {
                    distanceSinceLast += Vector3.Distance(lastStepPoint, p0);
                    lastStepPoint = p0;
                }
            }
            ringPointsCorner.Add(tempCornerPoints);
        }

        bool showCubes = false;
        if (showCubes)
        {
            for (int a = 0; a < ringPointsCorner.Count; a++)
            {
                for (int b = 0; b < ringPointsCorner[a].Count; b++)
                {
                    GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    c.transform.position = ringPointsCorner[a][b] + transform.position;
                    //c.transform.localScale *= 0.1f;
                    Destroy(c, 3);
                }
            }
        }

        return ringPointsCorner;

    }

    List<Vector3> RingPoints(List<List<Vector3>> ringEdgePoints, List<List<Vector3>> ringCornerPoints)
    {
        List<Vector3> finalRingVertices = new List<Vector3>();

        for (int i = 0; i < ringEdgePoints.Count; i++)
        {
            for (int j = 0; j < ringEdgePoints[i].Count; j++)
            {
                finalRingVertices.Add(ringEdgePoints[i][j]);

            }

            for (int j = 0; j < ringCornerPoints[i].Count; j++)
            {
                finalRingVertices.Add(ringCornerPoints[i][j]);
            }
        }

        //make final loop point
        finalRingVertices.Add(finalRingVertices[0]);

        bool showCubes = false;
        if (showCubes)
        {
            for (int a = 0; a < finalRingVertices.Count; a++)
            {

                GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                c.transform.position = finalRingVertices[a] + transform.position;
                c.transform.localScale *= 0.1f;
                Destroy(c, 3);

            }
        }

        return finalRingVertices;
    }

    GameObject TriangulateRing(List<Vector3> ringPoints, bool flip)
    {
        GameObject pavement = RoofTriangulator.RoofObjectFromConcavePolygon(ringPoints, flip);
        pavement.GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("Ground") as Material;

        return pavement;
    }

    public static Mesh Extrude(List<Vector3> ringPoints, float depth, float scale, bool uniqueVertices)
    {

        
        //int vertCountStart = verts.Length;
        List<Vector3> vertsList = new List<Vector3>();
        List<int> trisList = new List<int>();

        for (int i = 0; i < ringPoints.Count- 1; i++)
        { 

            vertsList.Add(ringPoints[i]);
            vertsList.Add(ringPoints[i + 1]);
            vertsList.Add(ringPoints[i + 1] + (Vector3.up * depth));
            vertsList.Add(ringPoints[i] + (Vector3.up * depth));
        }

        //add joining link/ last one
     //   vertsList.Add(ringPoints[ringPoints.Count- 1]);
     //   vertsList.Add(ringPoints[1]);
     //   vertsList.Add(ringPoints[1] + (Vector3.up * depth));
      //  vertsList.Add(ringPoints[ringPoints.Count - 1] + (Vector3.up * depth));


        for (int i = 0; i < vertsList.Count - 2; i += 4)
        {

            trisList.Add(i + 0);
            trisList.Add(i + 1);
            trisList.Add(i + 2);


            trisList.Add(i + 3);
            trisList.Add(i + 0);
            trisList.Add(i + 2);

        }

        bool addTop = false;
        if (addTop)
        {
            //now add a top
            vertsList.Add(ringPoints[0] + Vector3.up * depth);
            //join the last ring all to this central point
            for (int i = 0; i < vertsList.Count - 2; i += 4)
            {
                trisList.Add(i + 2);
                trisList.Add(vertsList.Count - 1);
                trisList.Add(i + 3);
            }
        }

        foreach (Vector3 v3 in vertsList)
        {
            //   GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            //   cube.transform.position = v3;
            //   cube.transform.localScale *= 0.1f;
        }

        Mesh newMesh = new Mesh();
        newMesh.vertices = vertsList.ToArray();
        newMesh.triangles = trisList.ToArray();

        if (uniqueVertices)
        {
            newMesh = MeshTools.UniqueVertices(newMesh);
        }
        else
        {
            newMesh.RecalculateNormals();
            newMesh.RecalculateBounds();
        }


        return newMesh;
    }

    Vector3 MiterDirection(Vector3 p0, Vector3 p1, Vector3 p2,float miterLength)
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
        float length = miterLength / Vector3.Dot(normal0, miterDirection);
        miterDirection *= length;

         // Debug.DrawLine(p0, p1, Color.blue);
         // Debug.DrawLine(p2, p1, Color.blue);
        // if(draw)
           //Debug.DrawLine(p1 + transform.position, p1 + transform.position + miterDirection * -length , Color.red);
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
}
