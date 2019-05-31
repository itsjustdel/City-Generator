using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SkyscraperGenerator : MonoBehaviour {


    public bool useUIValues = false;
    public float size = 20f;
    public float maxHeight = 100f;
    [Range(2, 10)]
    public int sides = 4;
    [Range(2, 50)]
    public int roundnessSize = 2;
    [Range(0,6 )]
    public int roundnessDetail = 2;

    [Range(1f, 10f)]
    public float floorHeight = 3f;
    [Range(0, 45)]
    public int spin = 0;

    //public bool addWindows = true;
    public bool addWindowsForCorner = false;


    [Range(2, 10)]
    public int windowSplitTop = 10;
    [Range(2, 10)]
    public int windowSplitBottom = 10;
    [Range(1, 10)]
    public int windowSizeX = 2;
    [Range(2, 30)]
    public int windowFrameSizeX = 10;


    public bool glass = true;
    [Range(0.0f, 1f)]
    public float spacerHeight = 0.1f;
    public float spaceNeededForLedge = 0.1f;

    private float tolerance = 0.001f;//sometimes pi is a little out

    public bool debug = true;
    

    private List<Vector3> curveDebug = new List<Vector3>();
    public int debugFreq = 10;
    private BezierSpline spline;

    public List<GameObject> curveObjects = new List<GameObject>();
    

    public GameObject lineRendChild;
    public GameObject cam;
    public float UiZ = 10f;
    public float UiY = 10f;
    public float UiX = 10f;

    //sliders
    public GameObject sidesSlider;
    public GameObject cornerWindowsSlider;
    public GameObject cornerShapeSlider;
    public GameObject spinSlider;
    public GameObject windowHeightSliderTop;
    public GameObject windowHeightSliderBottom;
    public GameObject windowWidthSlider;
    public GameObject windowFrameSlider;
    public GameObject floorSpacerSlider;
    public GameObject typeSlider;
    public GameObject windowNumber;

    // Use this for initialization
    void Start ()
    {
        spline = gameObject.AddComponent<BezierSpline>();
        
    }

    private void Update()
    {
        Spline();
        Building();

        if (debug)
        {
            float step = 1f / debugFreq;
            for (float i = 0; i < debugFreq; i+=step)
            {
                Vector3 p0 = spline.GetPoint(i * step);
                Vector3 p1 = spline.GetPoint((i +step) * step);

                Debug.DrawLine(p0, p1);
                //GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                //c.transform.position = p0;
            }
        }

        if (useUIValues)
        {

            sides = (int)(sidesSlider.GetComponent<Slider>().value);
            roundnessSize = (int)cornerShapeSlider.GetComponent<Slider>().value;
            int addWindowsForCornerInt = (int)(cornerWindowsSlider.GetComponent<Slider>().value);
            if (addWindowsForCornerInt == 0)
                addWindowsForCorner = false;
            else
                addWindowsForCorner = true;

            int typeInt = (int)(typeSlider.GetComponent<Slider>().value);

            if (typeInt == 0)
                glass = false;
            else
                glass = true;

            windowSplitTop = (int)(windowHeightSliderTop.GetComponent<Slider>().value);
            windowSplitBottom = (int)(windowHeightSliderBottom.GetComponent<Slider>().value);
            windowFrameSizeX = (int)(windowFrameSlider.GetComponent<Slider>().value);
            spacerHeight = floorSpacerSlider.GetComponent<Slider>().value;
            spin = (int)spinSlider.GetComponent<Slider>().value;
            windowSizeX = (int)windowNumber.GetComponent<Slider>().value;
        }

        if (Input.GetKey("escape"))
        {
            Application.Quit();
        }
    }

    void Spline()
    {
       
        //creates a curve which defines the outside shape of the skyscraper
        
        /*
        Vector3 p0 = Vector3.right * size * 0.5f;
        Vector3 p1 = Vector3.right * size * 0.5f + Vector3.up * maxHeight * 0.5f;
        Vector3 p2 = Vector3.up * maxHeight * 0.5f;
        List<Vector3>  splinePoints = new List<Vector3> { p0, p1, p2 };
        */
        List<Vector3> splinePoints = new List<Vector3>();
        //splinePoints = AddControlPointsToList(gameObject, splinePoints);

        foreach(Vector3 v3 in splinePoints)
        {
          //  GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
//            c.transform.position = v3;
        }
        List<BezierControlPointMode> modes = new List<BezierControlPointMode>();
        for (int i = 0; i < curveObjects.Count; i++)
        {
            splinePoints.Add(curveObjects[i].transform.position);
            BezierControlPointMode m = BezierControlPointMode.Aligned;
            modes.Add(m);

        }

        spline.points = splinePoints.ToArray();
        spline.modes = modes.ToArray();

    }

    void Building()
    {
        //reset
        GameObject additions = gameObject.transform.Find("Additions").gameObject;
        int count = additions.transform.childCount;
        for (int i = 0; i < count; i++)
        {
            Destroy(additions.transform.GetChild(i).gameObject);
        }

        Mesh mesh = GetComponent<MeshFilter>().mesh;

        mesh.Clear();
        //use spine as a guide to place levels of tower
        //add rings function needs a size(radius) to create its ring. Size is how far away from the centre the curve is at any given height
        //vertices
        List<Vector3> vertices = new List<Vector3>();
        //triangles and submeshes
        List<List<int>> trianglesList = new List<List<int>>();
        List<int> triangles0 = new List<int>();
        List<int> triangles1 = new List<int>();
        List<int> triangles2 = new List<int>();
        trianglesList.Add(triangles0);
        trianglesList.Add(triangles1);
        trianglesList.Add(triangles2);

        Vector3 targetDirection = Vector3.up;

        //shape defining randoms
       
        int ringsSoFar = 0;
        float step = 1f / debugFreq;
        float accuracy = 0.01f;

        float floorHeightCounter = 0f;
        float lastY = 0f;
        
        float lastRingBuiltYPos = 0f;
        //step through curve and when y co-ordinate is close enough, build a floor

        float ringSizeStart = curveObjects[0].transform.position.x;
        float lastX = ringSizeStart;
        //remember how many vertices in a ring so we can stitch the last built in to a roof
        int verticesInARing = 0;


        List<Vector3> curvePoints = new List<Vector3>();
        if (!glass)
        {
            //add first ring on ground
            //float roundnesSizeStart = ringSizeStart;
            if (roundnessDetail == 0)
            {
              //  roundnesSizeStart = ringSizeStart / roundnessSize;
            }

            //  RoundedPolygonMaker.AddRing(out triangles, vertices, triangles, targetDirection, sides, ringSizeStart, 0f, ringSizeStart / roundnessSize, roundnessDetail, tolerance, ringsSoFar);

            //ringsSoFar++;



            // float ledgeHeightCounter = 0f;

            float previousXSize = ringSizeStart;
            float lastLedgeHeight = 0f;
            int floors = 0;
            //tradition style with straight walls and ledges
            float rowHeightCounter = 0f;
            //which submesh
            int layerType = 0;

            for (float i = 0; i <= debugFreq; i += accuracy)
            {
                Vector3 p0 = spline.GetPoint(i * step);

              
               // Debug.Log("before = " + p0.y);
                //round to to 1dp

                p0.y = (float)(System.Math.Round(p0.y, 4));
              //  Debug.Log("after = " + p0.y);

                if (rowHeightCounter >= floorHeight + spacerHeight)
                {
                    curvePoints.Add(p0);
                    float ringSize = p0.x;
                    //check if ring needs ledge, if it is of roughly the same x value, just, build on top without making a ledge
                    float diffBetweenNextRing = Mathf.Abs(lastX - ringSize);
                   
                    //build vertical wall
                    
                    float ledgeSize = previousXSize;
                    float roundnessForLedge = ledgeSize / roundnessSize;
                    
                    if (debug)
                    {
                        GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        c.transform.position = new Vector3(ledgeSize, lastRingBuiltYPos - spacerHeight, 0);
                    }
                    int spinAmountForSection = ringsSoFar;
                    if (ringsSoFar > 0)
                    {
                        for (int q = 0; q < 4; q++)
                        {
                            //add for this ring?
                            bool addWindowsTemp = false;
                            bool addWindowsCornerTemp = false;
                            float tempHeight = (floorHeight - spacerHeight);
                            float yPos = lastRingBuiltYPos - (tempHeight);
                            bool forSpacer = false;
                            if (q == 0)
                            {
                                //spacer
                                layerType = 2;
                                forSpacer = true;
                                addWindowsTemp = false;
                                addWindowsCornerTemp  = false;
                            }
                            
                            if (q == 1)
                            {
                                yPos = lastRingBuiltYPos - tempHeight + (tempHeight / windowSplitBottom);
                                //main building material
                                layerType = 0;
                                addWindowsTemp = false;
                                addWindowsCornerTemp = false;
                            }
                            if (q == 2)
                            { 
                                yPos = lastRingBuiltYPos - (tempHeight / windowSplitTop);
                                //window
                                layerType = 0;
                                addWindowsTemp = true;
                                addWindowsCornerTemp = addWindowsForCorner;

                                forSpacer = true;
                            }
                            if (q == 3)
                            {
                                addWindowsTemp = false;
                                addWindowsCornerTemp = false;
                                yPos = lastRingBuiltYPos;
                                //main building material
                                layerType = 0;

                            }


                            RoundedPolygonMaker.AddRing(out trianglesList, out verticesInARing,gameObject, vertices, trianglesList, targetDirection, sides, ledgeSize, yPos, roundnessForLedge, roundnessDetail, tolerance, ringsSoFar, spin, false, glass,addWindowsTemp, addWindowsCornerTemp, forSpacer, floorHeight, windowSizeX, windowFrameSizeX,layerType);

                            for (int j = 0; j < verticesInARing; j++)
                            {
                            Vector3 t = vertices[vertices.Count - verticesInARing + j];
                            t = Quaternion.Euler(0, (spinAmountForSection - 1) * spin, 0) * t;
                            vertices[vertices.Count - verticesInARing + j] = t;

                             // GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                             // c.transform.position = t;
                             // c.transform.localScale *= 0.05f;
                            //  c.name = q.ToString();
                            }
                            

                            ringsSoFar++;

                            //  GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                            //  c.transform.position = new Vector3(ledgeSize, yPos, 0);
                            //   c.transform.localScale *= 0.5f;
                               //  c.name = q.ToString();
                            
                        }
                    }
              
                    lastLedgeHeight = lastRingBuiltYPos;

                    //build ledge
                    float height = floors * (floorHeight +spacerHeight) + spacerHeight;

                    
                    if (diffBetweenNextRing < spaceNeededForLedge)
                    {
                        //set next level to be the same as the last
                        ringSize = lastX;
                    }
                    
                    if (debug)
                    {
                        GameObject c1 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        c1.transform.position = new Vector3(ringSize, height, 0);
                    }
                    //spacer 
                    layerType = 2;

                    //spacer with twist of new floor and new ring size
                    RoundedPolygonMaker.AddRing(out trianglesList, out verticesInARing, gameObject, vertices, trianglesList, targetDirection, sides, ringSize, height , ringSize / roundnessSize, roundnessDetail, tolerance, ringsSoFar, spin, true, glass,false,false, true, floorHeight, windowSizeX, windowFrameSizeX,layerType);

                    ringsSoFar++;

                    //spin
                    if (ringsSoFar > 1)
                    {
                        for (int j = 0; j < verticesInARing; j++)
                        {
                            Vector3 t = vertices[vertices.Count - verticesInARing + j];
                            t = Quaternion.Euler(0, (ringsSoFar -1) * spin, 0) * t;
                            vertices[vertices.Count - verticesInARing + j] = t;

                            // GameObject c = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                            // c.transform.position = t;
                            // c.transform.localScale *= 0.05f;
                        }
                    }

                    bool balcony = false;
                    if (previousXSize > ringSize)
                    {
                        balcony = true;
                    }

                    if (balcony)
                    {                     
                        //BalconyFullLedge(verticesInARing,vertices);
                    }


                    floors++;
                    rowHeightCounter = 0f;

                    previousXSize = ringSize;
                    lastRingBuiltYPos = p0.y;
                    lastX = ringSize;
                }
                else
                {
                    rowHeightCounter += p0.y - lastY;
                    lastY = p0.y;
                }

                if (debug)
                {
                 //   Vector3 p1 = spline.GetPoint((i + step) * step);
                   // Debug.DrawLine(p0, p1);
                   // GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                   // c.transform.position = p0;
                }
            }

            if (lastLedgeHeight < lastRingBuiltYPos)
                lastLedgeHeight = lastRingBuiltYPos - floorHeight;
            //add roof
            //gather last vertices and stitch at center
            for (int i = 0; i < verticesInARing; i++)
            {
                Vector3 t = vertices[vertices.Count - verticesInARing + i];
                t = new Vector3(0f, t.y, 0f);
                vertices[vertices.Count - verticesInARing + i] = t;
            }
          // RoundedPolygonMaker.AddRing(out triangles, vertices, triangles, targetDirection, sides, 0.5f, lastLedgeHeight ,0, roundnessDetail, tolerance, ringsSoFar);
        }
        else if (glass)
        {
            //
            //add first ring on ground
            float roundnesSizeStart = ringSizeStart / roundnessSize;

            int layerType = 2;

            bool addWindowsForStart = false;
            RoundedPolygonMaker.AddRing(out trianglesList, out verticesInARing, gameObject, vertices, trianglesList, targetDirection, sides, ringSizeStart, 0f, roundnesSizeStart, roundnessDetail, tolerance, ringsSoFar, spin,false, glass, addWindowsForStart, addWindowsForCorner, false, floorHeight, windowSizeX, windowFrameSizeX,layerType);
            ringsSoFar++;

            bool forSpacer = false;
            for (float i = 0; i <= debugFreq; i += accuracy)
            {
                //counter
                int floors = 0;
                float targetHeight = floorHeight;
                if (forSpacer)
                    targetHeight = spacerHeight;
                if(spacerHeight == 0f)
                {
                    targetHeight = floorHeight;
                    forSpacer = false;
                }

                Vector3 p0 = spline.GetPoint(i * step);
                if (floorHeightCounter >= targetHeight)
                {
                    //we ahve reached the point for a new floor
                    curvePoints.Add(p0);
                    //curve built on x axis, use x co-ordinate
                    float ringSize = p0.x;                    
                    float ringHeight = (float)(System.Math.Round(p0.y, 4));

                    float roundnessSizeThisRing = roundnessSize;

                    roundnessSizeThisRing = ringSize / roundnessSize;
                    //glass
                    float heightThisRing = ringHeight;
                    layerType = 0;
                    //create bools for this step, revert back to global if not a spacer, spacers have no windows
                    bool addWindowsTemp = true;
                    bool addWindowsForCornerTemp = addWindowsForCorner;
                    if (forSpacer)
                    {
                        //alternating between spacer and floor
                        addWindowsTemp = false;
                        addWindowsForCornerTemp = false;
                        layerType = 2;
                    }
                    else
                        addWindowsForCornerTemp = addWindowsForCorner;




                    RoundedPolygonMaker.AddRing(out trianglesList, out verticesInARing, gameObject, vertices, trianglesList, targetDirection, sides, ringSize, ringHeight, roundnessSizeThisRing, roundnessDetail, tolerance, ringsSoFar, spin, false, glass,addWindowsTemp, addWindowsForCornerTemp,forSpacer, floorHeight, windowSizeX, windowFrameSizeX,layerType);

                    ringsSoFar++;

                    //spin
                    if (ringsSoFar > 1)
                    {
                        float spinForThisRing = (ringsSoFar - 1f) * spin;
                        if (forSpacer)
                            spinForThisRing = (ringsSoFar - 2f + (spacerHeight / floorHeight)) * spin;
                        for (int j = 0; j < verticesInARing; j++)
                        {
                            Vector3 t = vertices[vertices.Count - verticesInARing + j];
                            t = Quaternion.Euler(0,spinForThisRing , 0) * t;
                            vertices[vertices.Count - verticesInARing + j] = t;
                        }
                    }
                    /*
                    //add spacer
                    layerType = 2;
                    float heightForSpacer = ringHeight + spacerHeight;
                    RoundedPolygonMaker.AddRing(out trianglesList, out verticesInARing, vertices, trianglesList, targetDirection, sides, ringSize, heightForSpacer, roundnessSizeThisRing, roundnessDetail, tolerance, ringsSoFar, spin, false, glass, floorHeight, windowSizeX, windowFrameSizeX, layerType);

                    ringsSoFar++;

                    

                    //spin
                    if (ringsSoFar > 1)
                    {
                        for (int j = 0; j < verticesInARing; j++)
                        {
                            Vector3 t = vertices[vertices.Count - verticesInARing + j];
                            //spin by previous ring + ratio of spacer
                            t = Quaternion.Euler(0, (ringsSoFar - 2f + (spacerHeight/floorHeight)) * spin, 0) * t;
                            vertices[vertices.Count - verticesInARing + j] = t;
                        }
                    }
                    */

                    //reset height counter
                    floorHeightCounter = 0f;
                    //keep track of total
                    floors++;

                    //saving for roof point
                    lastRingBuiltYPos = ringHeight;
                    //next height is for a spacer
                    forSpacer = !forSpacer;
                }
                else
                {
                    floorHeightCounter += p0.y - lastY;
                    lastY = p0.y;
                }

                if (debug)
                {
                    Vector3 p1 = spline.GetPoint((i + step) * step);
                    Debug.DrawLine(p0, p1);
                    GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    c.transform.position = p0;
                }
            }

            //finish roof
            //curve built on x axis, use x co-ordinate
            float ringSizeRoof = 0f;
            float ringHeightRoof = lastRingBuiltYPos;
            float roundnessSizeThisRingRoof = roundnessSize;

            roundnessSizeThisRingRoof = ringSizeRoof / roundnessSize;
            
            RoundedPolygonMaker.AddRing(out trianglesList, out verticesInARing, gameObject, vertices, trianglesList, targetDirection, sides, ringSizeRoof, ringHeightRoof, roundnessSizeThisRingRoof, roundnessDetail, tolerance, ringsSoFar, spin, false, glass, false,false, forSpacer, floorHeight, windowSizeX, windowFrameSizeX,0);


        }

        
        mesh.vertices = vertices.ToArray();
        mesh.subMeshCount = 3;
        mesh.SetTriangles(trianglesList[0], 0);
        mesh.SetTriangles(trianglesList[1], 1);
        mesh.SetTriangles(trianglesList[2], 2);
        mesh.RecalculateNormals();

        MeshFilter meshFilter = GetComponent<MeshFilter>();
        meshFilter.mesh = mesh;

        for (int i = 0; i < vertices.Count; i++)
        {
          //  GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
          //  c.transform.position = vertices[i];
          //  c.transform.localScale *= 0.05f;

        }

        
        for (int i = 0; i < curvePoints.Count; i++)
        {

            
           // curvePoints[i] = cam.transform.rotation * curvePoints[i];
            curvePoints[i] -= Vector3.right * UiX;
            //curvePoints[i] += Vector3.forward * UiY;
            //  curvePoints[i] = Quaternion.Euler(0, 180, 0) * curvePoints[i];
            //move towards cam
            // curvePoints[i] += cam.transform.position + cam.transform.forward*UiZ - cam.transform.up* UiY;

        }

       lineRendChild.GetComponent<LineRenderer>().positionCount = curvePoints.Count;
       lineRendChild.GetComponent<LineRenderer>().SetPositions(curvePoints.ToArray());

    }

    public void BalconyFullLedge(int verticesInARing, List<Vector3> vertices)
    {
        //build mesh around rim
        float wallHeight = 1f;
        float wallWidth = 0.2f;

        List<Vector3> balconyVertices = new List<Vector3>();
        List<int> balconyTriangles = new List<int>();

        //front, top, back is 3 sides
        for (int i = 0; i < 3; i++)
        {
            
            for (int j = 0; j < verticesInARing; j++)
            {
                //find the previous ring, this will be outside edge of ledge, so multiply vertices in a ring by 2
                Vector3 outerPos = vertices[vertices.Count - verticesInARing * 2 + j];
                //this is the last ring built
                Vector3 innerPos = vertices[vertices.Count - verticesInARing + j];

                

                //if i == 0, front
                Vector3 p0 = outerPos;
                Vector3 p1 = outerPos + Vector3.up * wallHeight;
                if(i==1)
                {
                    //build top ledge
                    Vector3 directionInside = (innerPos - outerPos).normalized;
                    p0 = outerPos + Vector3.up*wallHeight;
                    p1 = outerPos + Vector3.up * wallHeight + directionInside * wallWidth;
                }
                if(i == 2)
                {
                    //build back
                    Vector3 directionInside = (innerPos - outerPos).normalized;
                    p0 = outerPos + Vector3.up * wallHeight + directionInside*wallWidth;
                    p1 = outerPos + directionInside * wallWidth;
                }

                balconyVertices.Add(p0);
                balconyVertices.Add(p1);

                /*
                GameObject c = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                c.transform.position = p0;
                c.transform.localScale *= 0.05f;
                c.name = i.ToString() + " " + j.ToString() + "p0";

                c = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                c.transform.position = p1;
                c.transform.localScale *= 0.05f;
                c.name = i.ToString() + " " + j.ToString() + "p1";
                */
                
            }



            for (int j = 0; j < verticesInARing*2 -2; j += 2)
            {
                balconyTriangles.Add(j + 1 + verticesInARing*2 * i); //*2 because we add two rows of vertices each loop
                balconyTriangles.Add(j + 0 + verticesInARing*2 * i);
                balconyTriangles.Add(j + 2 + verticesInARing*2 * i);

                balconyTriangles.Add(j + 1 + verticesInARing*2 * i);
                balconyTriangles.Add(j + 2 + verticesInARing*2 * i);
                balconyTriangles.Add(j + 3 + verticesInARing*2 * i);

            }

        }

        //new mesh
        Mesh mesh = new Mesh();
        mesh.vertices = balconyVertices.ToArray();
        mesh.SetTriangles(balconyTriangles,0);
        mesh.RecalculateNormals();

        GameObject balconyObject = new GameObject();
        balconyObject.name = "Balcony";
        MeshFilter mF = balconyObject.AddComponent<MeshFilter>();
        mF.mesh = mesh;

        MeshRenderer mR = balconyObject.AddComponent<MeshRenderer>();
        mR.sharedMaterial = Resources.Load("Post0") as Material;

        balconyObject.transform.parent = gameObject.transform.Find("Additions");


    }

   

    //for UI
    public void AdjustSides(int newSides)
    {
        sides = newSides;
    }
    

}
