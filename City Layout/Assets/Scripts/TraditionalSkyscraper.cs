using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class TraditionalSkyscraper : MonoBehaviour {

    public bool drawLocalSplines;
    public bool reset = false;
    public bool updateEveryFrame = false;
    public bool makeRandomBuildingShape = true;
    public bool makeRandomWindows = true;
    public bool animateBuild = true;
    public bool doAnimationForEachFloor = true;
    [Range(1f, 100f)]
    public float animationTimer = 0f;
   // public float animationSpeed = 5f;

    public float totalHeight = 6f;
    public float floorHeight = 3f;

    public int segments = 5;

    public float spinAmount0 = 0f;
    public float spinAmount1 = 0f;
    public float spinAmount2 = 0f;

    [Range(0f, 1f)]
    public float spacerHeight;

    public bool uniformCorners;
    [Range(0f, 1f)]
    public float cornerScaler = 0.5f;
    [Range(0.5f, 5f)]
    public float windowX = 1f;
    [Range(0.1f, 1f)]
    public float windowSpaceX = 0.2f;

    public float windowFrameSize = 0.1f;
    public float windowFrameDepth = 0.02f;
    public float windowFrameDepthOuter = 0.02f;

    [Range(0f, 1f)]
    public float windowBottomPanelHeight = 0f;
    [Range(0f, 1f)]
    public float windowTopPanelHeight = 1f;

    public float curveAccuracyCorners = 5f;//can do 1 and make hex edge. 5 seems ok, more and we get more vertices- large performance cos(build time)
    private float curveStepSize = .5f;//not looked in to this for opto

    Material[] materials;//materials for main storey
    Material windowFrameMaterial;

    public List<BezierSpline> splines = new List<BezierSpline>();
    //List<BezierSpline> cornerSplines;
    BezierSpline bezierForCorners;// = gameObject.GetComponent<BezierSpline>();

    public List<GameObject> stories = new List<GameObject>();

    //used to optimise height find in curve
    float lastHeightFound = 0f;


   

    List<List<float>> randoms = new List<List<float>>();
    List<List<float>> randomsHeight = new List<List<float>>();

    //used for script which build ther layout
    public bool finishedBuilding = false;

    //material patterns
    //spacer
    bool upwardsSequentialSpacer = false;
    bool downwardsSequentialSpacer = false;
    bool randomSpacerMaterial = false;
    bool constantSpacerMaterial = false;
    int contantSpacerIndex = 0;
    bool darkSpacers = false;

    bool upwardsSequentialBuilding = false;
    bool downwardsSequentialBuilding = false;
    bool randomBuildingMaterial = false;

    bool upwardsSequentialWindows = false;
    bool downwardsSequentialWindows = false;
    bool randomWindowMaterial = false;

    int sequentialSpacerCounter = 0;
    int sequentialBuildingCounter = 0;
    int sequentialWindowsCounter = 0;

    bool inAndOutSpacer = false;//dont reset counter when finished, flip it so colours smoothly oscilate
    //building
    bool darkBuilding = false;
    int buildingShadeIndex = 0;
    //windows
    bool windowConstantMaterial = false;

    //grab material holding class on gameobject
    PaletteInfo pI;//

    private void Awake()
    {
        //enabled form build script
        this.enabled = false;
    }

    void Start ()
    {
       


        //grab material holding class on gameobject
        pI = GetComponent<PaletteInfo>();

        lastHeightFound = 0f;

        StopAllCoroutines();

        //if(materials == null)
        //    LoadMaterials();

        MaterialPatterns();

        if (makeRandomBuildingShape)
        {
            //for multiple test on same cell
            foreach (var comp in gameObject.GetComponents<BezierSpline>())
            {
                Destroy(comp);
            }

            //set values to be used for building, height, window sizes etc

            ValuesBuildingShape();
            
            //add curves to define the building's shape


        }

        if (makeRandomWindows)
            ValuesWindows();

        AddSplines();

        //lopping function to build mesh
        StartCoroutine("BuildFloors");
	}
	
	// Update is called once per frame
	void Update ()
    {
        if (drawLocalSplines)//could add extrude height for cell if we wanted these in correct position - note, worked out in local space
        {

            float step = 1f / 10;

            for (int i = 0; i < splines.Count; i++)
            {


                for (float j = 0; j < 10; j += step)
                {
                    //step through curve until we find the height passed
                    Vector3 p0 = splines[i].GetPoint(j);
                    Vector3 p1 = splines[i].GetPoint(j + step);

                    if (i == 0)
                        Debug.DrawLine(p0, p1, Color.red);
                    else
                        Debug.DrawLine(p0, p1, Color.blue);
                }
            }
        }

        if(reset || updateEveryFrame)
        {
            //destroy previous children
            foreach(GameObject g in stories)
            {
                Destroy(g);
            }

            stories.Clear();

            reset = false;
            Start();

        }

    }

    void ValuesBuildingShape()
    {
        randoms.Clear();
        randomsHeight.Clear();

        //general building shape

        //height = 
        Height();
        floorHeight = Random.Range(2f, 3.5f);
        spacerHeight = Random.Range(0f, 0.2f);
        //choose a side to sculpt with more random detail
        //if this number is over vertices length, it won't sculpt a side , just keep everything more uniform
        Mesh mesh = GetComponent<MeshFilter>().mesh;
        Vector3[] vertices = mesh.vertices;
        

        
      //  bool allRandom = Random.value >= 0.5f;
        //make staright side type common
        bool straight = Random.value >= 0.8f;//straight is alright but a bit boring

        //work out segments (how many floors are clustered together in a vertical block
        int maxFloors = Mathf.RoundToInt(totalHeight / floorHeight);
       // segments = Random.Range(2, maxFloors);
        //this means if segments are going to be less than 3 floors, just make the segments floor size (smooth bullding)
        if (totalHeight / segments < floorHeight * 3)
            segments = maxFloors;

        //make building spin 1 out of 4
        bool spin = Random.value > 0.75f;
        //don't spin a merged cell building

        if (GetComponent<MergeCell>() != null)
            spin = false;

        bool carveOnlyOneSide = Random.value > 0.5f;
        int randomSide = Random.Range(1, vertices.Length);
        //overrdies
        if (spin)//is this straight bool?
        {
            //add spins
            //leavin spin0 at 0
            spinAmount1 = Random.Range(0f,1f );//probably enough, crazy spins just dont look very good
            spinAmount1 *= 360; //multiply here because it gets divided by vertice magnitude when spinning
            //match spin or keep spinning, or reverse?
            float r = Random.value;
            if (r < .4f)
            {
                //match
                spinAmount2 = spinAmount1;
            }
            else if (r >= .4f && r < 0.9f)
            {
                //twist more - keeping at uniform spin
                spinAmount2 = spinAmount1 + spinAmount1;
            }
            else
            {
                //do a crazy one
                spinAmount2 = Random.Range(0, 720);
            }
          

        }
        else
        {
            spinAmount0 = 0f;  //Random.Range(0f, 1f);
            spinAmount1 = 0f; //Random.Range(0f, 1f);
            spinAmount2 = 0f;// Random.Range(0f, 1f);
        }
        

        float straightScaler = Random.Range(0.1f, 0.5f);//how to decide this?
        
        for (int i = 1; i < vertices.Length; i++)
        {

            //heights for each vertice

            float heightRandom1 = Random.Range(0.5f, 0.75f);//make list and have random heights for each curve?
            float heightRandom2 = Random.Range(0.76f, 1f);

            randomsHeight.Add(new List<float> { heightRandom1, heightRandom2});

            //x and z combined - used for lerp from centre vertice

            float random1 = 0f;
            float random2 = 0f;
            float random3 = 0f;

            if (straight)
            {
                random1 = straightScaler;
                random2 = straightScaler;
                random3 = straightScaler;
            }

            //if we compute the random numbers here we get a more erratic shape
            
            else if (carveOnlyOneSide)
            {
                if (i == randomSide)
                {
                    random1 = Random.Range(0f, 1f);
                    random2 = Random.Range(0.0f, 1f);
                    random3 = Random.Range(0f, 1f);
                }
                else
                {
                    random1 = straightScaler;
                    random2 = straightScaler;
                    random3 = straightScaler;
                }
            }
            else //all random
            {
                random1 = Random.Range(0.0f, 1f);
                random2 = Random.Range(0.0f, 1f);
                random3 = Random.Range(0f, 1f);
            }

            List<float> temp = new List<float> { random1, random2, random3 };
            //scale randoms by max pavement size
            //float max = distances[i - 1];// Mathf.Sqrt( GetComponent<MeshRenderer>().bounds.size.magnitude );//testing //use intersect point across with miter dir( halved )
            
            //for (int f =0; f < temp.Count;f++)
              //  temp[f] *= max;

            randoms.Add(temp);
        }



        //windows and corners

    
    }
    bool AreABCOneTheSameLine(Vector3 A, Vector3 B, Vector3 C)
    {
        return Mathf.Approximately(
            Vector3.Project(A - B, A - C).magnitude,
            (A - B).magnitude
        );
    }

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

    void ValuesWindows()
    {


        uniformCorners = Random.value >= 0.5f;
 
        cornerScaler = Random.Range(0f, 1f);

        windowX = Random.Range(0.5f, 3f);

        windowSpaceX = Random.Range(0.1f, 1f);//space between windows

        windowFrameSize = Random.Range(0.1f, .3f);//size of frame

        windowFrameDepth = Random.Range(0.02f, .1f);

        windowFrameDepthOuter = windowFrameDepth * 2f * Random.value;

        windowBottomPanelHeight = Random.Range(0f, .5f);
        windowTopPanelHeight = 1f- spacerHeight;// forcing to spacer height, just doesn't look good with dropped height //old Random.Range(0.66f, 1f);


        if (windowSpaceX < windowFrameSize)
            windowSpaceX = windowFrameSize;
    }

    void MaterialPatterns()
    {
        //this function determines the order materials are placed on the building. The pattern.
        //spacers
        float r = Random.value;
        if (r < 0.25f )
        {
            upwardsSequentialSpacer = true;
        }
        else if(r >=0.25f && r <0.5f)
        {
            downwardsSequentialSpacer = true;
            sequentialSpacerCounter = GetComponent<PaletteInfo>().palette[0].tints.Count - 1;
        }
        else if(r >= 0.5f && r < 0.75f)
        {
            constantSpacerMaterial = true;
            contantSpacerIndex = Random.Range(0, GetComponent<PaletteInfo>().palette[0].tints.Count);
        }
        else
        {
            randomSpacerMaterial = true;
        }

        //oscilate? or reset counter when all tinst have been used
        if (Random.value < 0.5f)
            inAndOutSpacer = true;

        //dark or light theme?
        if (Random.value >= 0.5f)
            darkSpacers = true;

        
        //building
        //keeping this pretty simple and plain - doing crazy colour combos doesn't really look good
        buildingShadeIndex = Random.Range(0, 2);//keeping reasonably conservative for main color
        darkBuilding = Random.value >= 0.8f;

        windowConstantMaterial = true;////***
        //windows
        if(windowConstantMaterial)
        {
            //randomly choose dark or light frames for whole building
            if(Random.value>0.5f)
                windowFrameMaterial = pI.palette[Random.Range(0,pI.palette.Count)].tints[Random.Range(0, pI.palette[0].tints.Count - 1)];
            else
                windowFrameMaterial = pI.palette[Random.Range(0, pI.palette.Count)].shades[Random.Range(0, pI.palette[0].shades.Count - 1)];
        }
    }

    void AddSplines()
    {
        //for corners, re-use the same spline
        if(bezierForCorners == null)
            bezierForCorners = gameObject.AddComponent<BezierSpline>();
        

        for (int i = 0; i < splines.Count; i++)
        {
            Destroy(splines[i]);
        }

        splines = new List<BezierSpline>();
        //add a curve for each point on the edge of the cell, the corners
        //points are made of four points
        //choose a side to sculpt with more random detail
        
        Mesh mesh = GetComponent<MeshFilter>().mesh;
        Vector3[] vertices = mesh.vertices;

         AdjacentCells aJ = GetComponent<AdjacentCells>(); //going back to vertices[0] - but for original cell
        //use to find where to lerp to, saved as were we merging cells
        List<GameObject> previousCells = null;

        if(GetComponent<MergeCell>() != null)
            previousCells = GetComponent<MergeCell>().previousCells;


       // List<float> distances = new List<float>();
        List<Vector3> directions = new List<Vector3>();


        Vector3[] originalVerticesThis = GetComponent<ExtrudeCell>().originalMesh.vertices;

        float minBuildWidth = 3f; //testing

        if (GetComponent<MergeCell>() == null)
        {
            for (int i = 1; i < vertices.Length; i++)
            {
              //  distanceToCenter = Vector3.Distance(vertices[i], vertices[0]);

                Vector3 newCenter = (vertices[i] - vertices[0]).normalized * minBuildWidth;//min edge var?
                directions.Add(newCenter - vertices[i]);

                /*
                GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                c.transform.position = vertices[i] + transform.position;
                c.name = i.ToString();
                */

                bool showPoints = false;
                if (showPoints)
                {
                 
                        Debug.DrawLine(transform.position + vertices[i], transform.position + vertices[i] + directions[directions.Count-1], Color.cyan);
                }
            }
        }
        else
        {
            for (int i = 1; i < originalVerticesThis.Length; i++)
            {
                //we need to find which cell this vertice point used to belong to so we can use the old cell's centre point to drag curve/building in

                //    GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                //   c.transform.position = originalVerticesThis[i];

                List<Vector3> matches = new List<Vector3>();
                for (int a = 0; a < previousCells.Count; a++)
                {
                    Vector3[] originalVerticesPrevious = previousCells[a].GetComponent<ExtrudeCell>().originalMesh.vertices;

                    for (int b = 0; b < originalVerticesPrevious.Length; b++)
                    {
                        if (b == 0)
                            continue;//skip centre

                        if (Vector3.Distance(originalVerticesThis[i], originalVerticesPrevious[b]) < 0.1f)
                        {
                            //add the orig. cell's centre point
                            matches.Add(originalVerticesPrevious[0]);


                            // c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                            //c.transform.position = originalVerticesPrevious[0];
                            //c.name = matches.Count.ToString();
                        }

                        //Debug.Log(matches.Count);
                    }
                }
                //if no point was found, it is a shared vertice between two cells, use the average of all
                Vector3 sharedCenter = Vector3.zero;
                foreach (Vector3 v3 in matches)
                    sharedCenter += v3;

                //do avg.
                sharedCenter /= matches.Count;

                //we were working in world space with the old original meshes, fix this
                sharedCenter -= transform.position;

                //add min building width
                sharedCenter += (vertices[i] - sharedCenter).normalized * minBuildWidth;

                // distanceToCenter = Vector3.Distance(originalCenter, vertices[i]);


                //move "original center" towards the vertice so it is always at least minEdge length
                //Vector3 newCenter = vertices[i] + (sharedCenter - vertices[i]).normalized * minBuildWidth;//min edge var?
               // directions.Add(sharedCenter - vertices[i]);

                List<Vector3> miters = GetComponent<Pavement>().miters;

               // Debug.Log(miters[i]);
                Debug.DrawLine(vertices[i] + miters[i-1] + transform.position, vertices[i] + transform.position, Color.blue);
                //we worked these directions which createsa nice middling line betweeon the two edges when making the pavement
                Vector3 miter = -miters[i - 1]*2; //make negative to point inside polygon
                //we now just need to create the length for this miter direction by measuring the distance between the vertice and the new centre point
                float distance = Vector3.Distance(vertices[i], sharedCenter);
               // miter = miter.normalized * distance;

                directions.Add(miter);
             

                bool showPoints = false;
                if (showPoints)
                {
                    Debug.Break();
                    
                    if (matches.Count < 2)
                        Debug.DrawLine(transform.position + vertices[i], transform.position + vertices[i] + directions[directions.Count - 1], Color.green);
                    else
                        Debug.DrawLine(transform.position + vertices[i], transform.position + vertices[i] + directions[directions.Count - 1], Color.red);
                }
               
            }            
        }
        
        
        for (int i = 1; i < vertices.Length; i++)
        {
            BezierSpline splineL = gameObject.AddComponent<BezierSpline>();

           // Vector3 miter = (aJ.miters[i - 1][0]).normalized;
            //scale but don't spin first point
           // Vector3 p0 = Vector3.Lerp(originalCenter, vertices[i],randoms[i-1][0]); //i -1 beacuse vertices are skipped on [0]
            //use miter for direction inside
           
            Vector3 p0 = vertices[i] + directions[i-1] * randoms[i - 1][0] ;//not, same as random as next point (creating straight start to building) - could add another if we want crazy starts
            //first of all spin vertice
            Vector3 p1 = Quaternion.Euler(0, spinAmount0, 0) * vertices[i];
            //then decide how close te centre we want
            //p1 = Vector3.Lerp(originalCenter, p1, randoms[i-1][0]);
            p1 = p1 + directions[i - 1] * randoms[i - 1][0]  ;//var - randoms?

            //then raise by hight
            p1 += Vector3.up * randomsHeight[i-1][0];

            Vector3 p2 = Quaternion.Euler(0, spinAmount1/vertices[i].magnitude, 0) * vertices[i];
            //then decide how close te centre we want
            //p2 = Vector3.Lerp(originalCenter, p2, randoms[i-1][1]);
            p2 = p2 + directions[i - 1] * randoms[i - 1][1] ;//var
            //then raise by hight
            p2 += Vector3.up * randomsHeight[i - 1][1];

            Vector3 p3 = Quaternion.Euler(0, spinAmount2/vertices[i].magnitude, 0) * vertices[i];
            //then decide how close te centre we want
            //p3 = Vector3.Lerp(originalCenter, p3, randoms[i-1][2]);
            p3 = p3 + directions[i - 1] * randoms[i - 1][2];
            //then raise by hight
            p3 += Vector3.up * totalHeight;//no random on this, just set to total height

            bool showHeightCube = false;
            if (showHeightCube)
            {
                GameObject c0 = GameObject.CreatePrimitive(PrimitiveType.Cube);
                c0.transform.position = Vector3.up * totalHeight + transform.position;
                c0.transform.name = "Total Height";
            }

            // p3 = Vector3.Lerp(vertices[0] + Vector3.up * totalHeight, p3, random3); //don't change total height
            List<Vector3> splinePoints = new List<Vector3> { p0, p1, p2, p3 };

            splineL.points = splinePoints.ToArray();

            splines.Add(splineL);

            
        }


    }

    IEnumerator BuildFloors()
    {
        
        //for each height
        for (float i = 0; i < totalHeight - floorHeight; i += floorHeight)
        {
            //find corner points at height i - note points return will be in world space
            List<Vector3> cornerPoints = CornerPoints(i);

            //work out points for windows along edge between each corner point
            List<List<Vector3>> ringEdgePoints = RingEdgePoints(cornerPoints);

            //work out points curving round corner
            List<List<Vector3>> ringCornerPoints = RingCornerPoints(ringEdgePoints, cornerPoints);

            //make unique points instead of a ring of sequential points - allows triangles their own vertices.
            List<List<Vector3>> uniqueEdgePoints = AddDuplicates(ringEdgePoints);
            List<List<Vector3>> uniqueCornerPoints = AddDuplicates(ringCornerPoints);

            //organise list in to one list travelling round ring
            List<Vector3> ringPointsUnique = RingPoints(uniqueEdgePoints, uniqueCornerPoints);
            
            //now we have an organised ring of points, extrude these upwards and create windows
            List<List<Vector3>> extrudedRings = BuildHeightsForFloor(ringPointsUnique);

            List<Vector3> vertices = Vertices(extrudedRings);

            //we have worked out the vertices, now we can look at triangles and submeshes
            //we can extract the window points for making window frames while we doing this since the method finds where the windows are
            List<List<int>> windowPoints = new List<List<int>>();
            List<List<int>> triangles = TrianglesAndMaterials(out windowPoints, extrudedRings, uniqueEdgePoints, uniqueCornerPoints);

            //set materials - meshes will use these
            materials = ChooseMaterials();

            //Create Gameobject to show these meshes
            GameObject storey = Storey(vertices, triangles);

            //floors and ceilings use triangulation algorithm which triangulates the ring points
            List<GameObject> floorAndCeiling =  FloorAndCeiling(ringEdgePoints, ringCornerPoints, storey);

            //create window frames
            GameObject windowFrames = WindowFrames(windowPoints, vertices, storey);
            

            //all meshes have been built in world space. Convert them to local mesh space and move gameObject which holds the mesh
            //I build in world space so I only have one set of co-ordinates to consider. I find it more confusing factoring in local mesh space.
            float height = i;
            MakeMeshesLocal(storey, floorAndCeiling,windowFrames, i);

            //now place at correct y height (on top of extruded cell)
            storey.transform.position += Vector3.up *  GetComponent<ExtrudeCell>().depth;
               
            //save in list
            stories.Add(storey);

            if (doAnimationForEachFloor)
            {
                animationTimer = 0f;
                Vector3 startScale = new Vector3(1f, 0.1f, 1f);
                Vector3 targetScale = Vector3.one;
                while (animationTimer < 1)
                {
                    animationTimer += Time.deltaTime * GameObject.FindGameObjectWithTag("Spawner").GetComponent<Spawner>().buildingSpeed;// animationSpeed;
                    float eased = Easings.QuarticEaseInOut(animationTimer);


                    storey.transform.localScale = Vector3.Lerp(startScale, targetScale, eased);
                    
                    yield return new WaitForFixedUpdate();
                }
            }

            //interiors
            //drop some mesh data to help later when planning interior
            Interiors interiors = storey.AddComponent<Interiors>();
            interiors.ringPoints = RingPointsForInterior(ringEdgePoints, ringCornerPoints);//orgnaises two lists in two one
            interiors.cornerPoints = cornerPoints;
            interiors.corners = ringCornerPoints.Count;
           

            yield break;



            //else if (animateBuild)     
            yield return new WaitForEndOfFrame();
            
             

        }

        //hooray!
        finishedBuilding = true;
        yield break;
    }

    //helpers

    float CornerSize(List<Vector3> cornerPoints)
    {
        float cornerSize = 0f; //uniform option here?

        float smallestDistance = Mathf.Infinity;
        for (int j = 0; j < cornerPoints.Count; j++)
        {
            //clamp list
            int thisIndexA = j;
            int nextIndexA = j + 1;
            if (nextIndexA > cornerPoints.Count - 1)
                nextIndexA = 0;

            float tempD = Vector3.Distance(cornerPoints[thisIndexA], cornerPoints[nextIndexA]);
            if (tempD < smallestDistance)
                smallestDistance = tempD;
        }

        cornerSize = smallestDistance * cornerScaler - windowX * .5f;// as large a curve as we can get
        if (cornerSize < windowX * 2)//*2 for safety, can have overlap at edges
            cornerSize = windowX * 2;

        return cornerSize;
    }

    List<Vector3> CornerPoints(float height)
    {
        //to create an outline of the floor we need to find the corner points defined by the vertical splines
        List<Vector3> cornerPoints = new List<Vector3>();
        //for each spline
        for (int j = 0; j < splines.Count; j++)
        {
            //find corner points at height defined by i
            //to do this we need to step up curve in small incrementtts until y height is satisfied
            
            float step = 1f / segments;
            for (float k = lastHeightFound; k < segments; k += step)
            {
                Vector3 p = splines[j].GetPoint(k);

                if (p.y >= height)
                {
                    bool showCubes = false;
                    if (showCubes)
                    {
                        GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        c.transform.position = p;
                        c.name = "Unclamped";
                        Destroy(c, 3);
                    }

                    //clamp p height in case just over, remember to apply extrude depth as we get world co-ords back from spline
                    float clampedHeight = height;
                    p = new Vector3(p.x, clampedHeight, p.z);

                    //add to our list
                    //remove any that are too close
                    if (j == 0)
                    {
                        cornerPoints.Add(p);
                    }
                    else if(j < splines.Count-1)
                    {
                        if(Vector3.Distance(p,cornerPoints[ cornerPoints.Count-1]) < 3f)//var
                        {
                            //dont add
                            break;
                        }
                        cornerPoints.Add(p);
                    }
                    else if( j == splines.Count -1)
                    {
                        if (Vector3.Distance(p, cornerPoints[cornerPoints.Count - 1]) < 3f)//var
                        {
                            //dont add
                            break;
                        }

                        //also check to first point
                        if (Vector3.Distance(p, cornerPoints[0]) < 3f)//var
                        {
                            //dont add
                            break;
                        }

                        cornerPoints.Add(p);
                    }

                    if (showCubes)
                    {
                        GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        c.transform.position = p;
                        c.name = "Clamped at " + height.ToString();
                        Destroy(c, 3);
                    }

                    //remember for next time we are doing this, we don't need to start at the bottom
                    lastHeightFound = k;

                    break;
                }
            }
        }

        

        return cornerPoints;
    }

    List<List<Vector3>> RingEdgePoints(List<Vector3> cornerPoints)
    {
        //first if we want uniform corner size we need to find out what the smallest corner size and apply it to all corners
        
        float cornerSizeForUniform = 0f;
        if(uniformCorners)
            cornerSizeForUniform = CornerSize(cornerPoints); 

        //now we know corner size, we can start making our edges. A "ring" will comprise of straight edges followed by curved corners.
        //This ring will be the shape of the floor


        List<List<Vector3>> ringPoints = new List<List<Vector3>>();

        for (int j = 0; j < cornerPoints.Count; j++)
        {
            //clamp list
            int thisIndex = j;
            int nextIndex = j + 1;
            if (nextIndex > cornerPoints.Count - 1)
                nextIndex = 0;

            Vector3 thisPoint = cornerPoints[thisIndex];
            Vector3 nextPoint = cornerPoints[nextIndex];

            bool showDebugCubes = false;
            if (showDebugCubes)
            {

                GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                c.transform.position = thisPoint;
                c.name = "This";

                c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                c.transform.position = nextPoint;
                c.name = "Next";
            }

            Vector3 halfPoint = Vector3.Lerp(thisPoint, nextPoint, 0.5f);
            Vector3 dirToNext = (nextPoint - thisPoint).normalized;
            Vector3 dirToPrev = (thisPoint - nextPoint).normalized;
            float distanceToNext = Vector3.Distance(thisPoint, nextPoint);


            //work out from middle, then spin and come back. This ensures sides are symmetrical


            List<Vector3> thisEdgePoints = new List<Vector3>();

            //using for loop to switch directions
            for (int k = 0; k < 2; k++)
            {
                //using temp ;list because we will reverse them becore entering in to main edge list
                List<Vector3> tempPoints = new List<Vector3>();

                //if non uniform
                float tempCornerSize = (distanceToNext * 0.5f) * cornerScaler * .5f;
                if (tempCornerSize < windowX * 2)
                    tempCornerSize = windowX * 2;

                //else if uniform, use, corner size function?//***************TODO
                if(uniformCorners)
                {
                    tempCornerSize = cornerSizeForUniform;
                }

                Vector3 dirToUse = dirToPrev;

                if (k == 1)
                {
                    dirToUse = dirToNext;
                }

                //drop a point for middle of side( this does mean this will be a duplicate point, because the other half of this side uses it too ?? removed below?
                if (k == 1)
                {
                    Vector3 p0 = halfPoint;// + dirToUse * k;
                    tempPoints.Add(p0);
                }

                for (float a = windowSpaceX; a < distanceToNext * 0.5f - tempCornerSize; a += windowSpaceX)
                {

                    Vector3 p = halfPoint + dirToUse * a;
                    tempPoints.Add(p);

                    a += windowX;
                    p = halfPoint + dirToUse * a;
                    tempPoints.Add(p);

                    //add window frame
                    //double check if this will be the last

                    a += windowSpaceX;
                    p = halfPoint + dirToUse * a;
                    tempPoints.Add(p);

                }

                //spin first half
                if (k == 0)
                    tempPoints.Reverse();

                //add to this edge points
                thisEdgePoints.AddRange(tempPoints);
            }
            //add to main list
            ringPoints.Add(thisEdgePoints);
        }

        bool showCubes = false;
        if (showCubes)
        {
            for (int a = 0; a < ringPoints.Count; a++)
            {
                for (int b = 0; b < ringPoints[a].Count; b++)
                {
                    GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    c.transform.position = ringPoints[a][b];
                    c.transform.localScale *= 0.1f;
                    Destroy(c, 3);
                }
            }
        }

        return ringPoints;
    }

    List<List<Vector3>> AddDuplicates(List<List<Vector3>> points)
    {
        //adds duplicate points for mesh so triangles can have unique vertices
        List<List<Vector3>> uniques = new List<List<Vector3>>();
        for (int i = 0; i < points.Count; i++)
        {
            List<Vector3> temp = new List<Vector3>();
            for (int j = 0; j < points[i].Count; j++)
            {
                temp.Add(points[i][j]);

                //duplicate point if not at start
                if(j > 0)
                    temp.Add(points[i][j]);
            }

            uniques.Add(temp);
        }

        return uniques;
    }

    List<List<Vector3>> RingCornerPoints(List<List<Vector3>> ringPoints,List<Vector3> cornerPoints)
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

            /*
            GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
            c.transform.position = lastPoint;
            c.transform.localScale *= 0.5f;
            c.name = " last point";

            c = GameObject.CreatePrimitive(PrimitiveType.Cube);
            c.transform.position = nearestCellVertice;
            c.transform.localScale *= 0.5f;
            c.name = "vertice cell";

            c = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            c.transform.position = firstFromNext;
            c.transform.localScale *= 0.5f;
            c.name = "first from next";
            */


            //control points of curve
            //dont normalise, we will use curve control point variable to use a percentage of this size, however .33f looks nice
            float curveControlPointSize = 0.33f;//??? what to use

            Vector3 dirToPrev = (lastPoint - nearestCellVertice);
            Vector3 cp0 = nearestCellVertice + dirToPrev * curveControlPointSize;

            Vector3 dirToNext = (firstFromNext - nearestCellVertice);//.normalized;
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
            for (float k = 0; k < curveAccuracyCorners- step*0.5f ; k += step)//half a step for tolerance
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
                    c.transform.position = ringPointsCorner[a][b];
                    c.transform.localScale *= 0.1f;
                    c.name = "ring corner point";
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
                c.transform.position = finalRingVertices[a];
                c.transform.localScale *= 0.1f;
                Destroy(c, 3);
                
            }
        }

        return finalRingVertices;
    }

    List<Vector3> RingPointsForInterior(List<List<Vector3>> ringEdgePoints,List<List<Vector3>> ringCornerPoints)
    {
        List<Vector3> finalRingVertices = new List<Vector3>();

        for (int i = 0; i < ringEdgePoints.Count; i++)
        {
            for (int j = ringEdgePoints[i].Count -1; j < ringEdgePoints[i].Count; j++)//only add last
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
                c.transform.position = finalRingVertices[a];
                c.transform.localScale *= 0.1f;
                Destroy(c, 3);

            }
        }

        return finalRingVertices;
    }

    List<List<Vector3>> BuildHeightsForFloor(List<Vector3> ringPoints)
    {
        List<List<Vector3>> extrudedRings = new List<List<Vector3>>();

        //a floor of a skyscraper comprises of 5 sections, a spacer at the top and bottom, a panel each side of the window, and the window in the middle

        //spacer
        //panel
        //window
        //panel
        //spacer

        //we have been passed the base points (ringPoints)        
        //now we need to extrude (copy and paste them at a higher height) depending on what section we are doing
        //we will make extrusion heights as fractions of total floor


        //move to values/////////***********


        float spacer = spacerHeight;// .2f;
        float tempBottom = windowBottomPanelHeight + spacer;
        float tempTop = 1f - (spacer + (1f - windowTopPanelHeight));
        //clamp to windowFrameX
        if (tempTop > 1f - windowFrameSize * .5f - spacer)
            tempTop = 1f - windowFrameSize * .5f - spacer;

        //clamp to half way
        if (tempTop < 0.5f)
            tempTop = 0.5f;

        //clamp bottom to half way
        if (tempBottom > 0.5f)
            tempBottom = 0.5f;

        //clamp to spacer plus windowframe x for bottom
        if (tempBottom < spacer + windowFrameSize * 0.5f)
            tempBottom = spacer + windowFrameSize * 0.5f;


        //windowTopPanelHeight;// = 1f - (spacer + windowTopPanelHeight); 
        float[] extrusionHeights = new float[6] 
        {
            0f,
            spacer,
            tempBottom,
            tempTop,
            1f - spacer,
            1f
        };

        //determine height        
        for (int i = 0; i < extrusionHeights.Length; i++)
        {
            //we will make the mesh with unique vertices. This helps with lighting and asigning materials properly
            //so if we already populated the list previously, duplicate the entry, this will give us the right amount of vertices
            if (i == 0)
            {
                //don't duplicate on first index, simply place passed points in list that is being returned
                extrudedRings.Add(ringPoints);
                continue;
            }
            if (i > 1)
            {
                //place previous list in as new entry, this will give us the duplicated vertices
                extrudedRings.Add(extrudedRings[extrudedRings.Count - 1]);
            }

            List<Vector3> extrusionPoints = new List<Vector3>();
            for (int j = 0; j < ringPoints.Count; j++)
            {
                //use extrusion heights array to divide floorheight
                Vector3 p = ringPoints[j] + Vector3.up * (floorHeight * extrusionHeights[i]);
                //now add to this extrusion's list
                extrusionPoints.Add(p);
            }

            //add to main list to be returned
            extrudedRings.Add(extrusionPoints);            
        }

        bool showCubes = false;
        if (showCubes)
        {
            for (int i = 0; i < extrudedRings.Count; i++)
            {
                for (int j = 0; j < extrudedRings[i].Count; j++)
                {
                    GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    c.transform.position = extrudedRings[i][j];
                    c.transform.localScale *= 0.1f;
                    Destroy(c, 3);
                }
            }
        }
        return extrudedRings;
    }

    List<List<int>> TrianglesAndMaterials(out List<List<int>> windowPoints, List<List<Vector3>> extrudedRings, List<List<Vector3>> ringPointsEdge,List<List<Vector3>> ringPointsCorner)
    {
        //we will retunr points for windows with this function too - saves us doing the exact same maths on another function
        windowPoints = new List<List<int>>();
            
        List<List<int>> triangles = new List<List<int>>();

        triangles.Add(new List<int>());
        triangles.Add(new List<int>());
        triangles.Add(new List<int>());

        List<int> material0 = new List<int>();
        List<int> material1 = new List<int>();
        List<int> material2 = new List<int>();


        int totalVertsInRing = 0;
        for (int i = 0; i < ringPointsEdge.Count; i++)
        {
            totalVertsInRing += ringPointsEdge[i].Count;
        }
        for (int i = 0; i < ringPointsCorner.Count; i++)
        {
            totalVertsInRing += ringPointsCorner[i].Count;
        }

        //+1 because.. finishing loop extra point?
        totalVertsInRing++;


        //running through each ring and segment in ring(edge or corner) and assigning to lists depending on whcih material they should be
        //materials are selected depending on which ring they are on and how far along the ring( e.g every 3rd vertice is a window)
        for (int i = 0; i < extrudedRings.Count-1; i+=2) // +=2 to jump unique vertice ring
        {
            int edgeCount = 0;
            for (int j = 0; j < extrudedRings[i].Count-1; j++)
            {

                /*
                GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                c.transform.position = extrudedRings[i][j];
                c.transform.localScale *= 0.1f;
                c.name = "vertice round" + i.ToString() + " " + j.ToString(); ;
                Destroy(c, 3);
                */

                int first = j + i * totalVertsInRing;
                int second = j + 1 + i * totalVertsInRing;
                int third = j + totalVertsInRing + (i * totalVertsInRing);
                int fourth = j + 1 + totalVertsInRing + (i * totalVertsInRing);

              

                //how many rings high are we? spacer, or panel, or window?
                if (i < 2 || i > 7)
                {
                    //spacer
                    //add all materials to spacer material
                    material0.Add(first);
                    material0.Add(second);
                    material0.Add(third);

                    material0.Add(third);
                    material0.Add(second);
                    material0.Add(fourth);


                    continue;
                }
                else if (i != 4 && i != 5)
                {
                    //panel
                    material1.Add(first);
                    material1.Add(second);
                    material1.Add(third);

                    material1.Add(third);
                    material1.Add(second);
                    material1.Add(fourth);

                    continue;
                }
                
                int count = 0;
                
                for (int a = 0; a < ringPointsEdge.Count; a++)
                {
                    
                    //add edge
                    
                    if (j >= count && j < ringPointsEdge[a].Count + count)
                    {
                        //Debug.Log("edge count = " + edgeCount);
                        //figure out if we are at a window
                        //6 vertices per window section (window plus frame)
                        //plus move four points in gives us a window every 6 points starting at the fourth point
                        if ((count - 4 - j) % 6 == 0) 
                        {
                            //glass
                            material2.Add(first);
                            material2.Add(second);
                            material2.Add(third);

                            material2.Add(third);
                            material2.Add(second);
                            material2.Add(fourth);

                            //save these points for windows
                            List<int> windowPos = new List<int>() { first, second, third, fourth };
                            windowPoints.Add(windowPos);
                        }
                        else
                        //give to building material
                        {
                            material1.Add(first);
                            material1.Add(second);
                            material1.Add(third);

                            material1.Add(third);
                            material1.Add(second);
                            material1.Add(fourth);
                        }

                        edgeCount++;
                    }
                    //keep a count of how many we added
                    count += ringPointsEdge[a].Count;

                    //add corner
                    if (j >= count && j < ringPointsCorner[a].Count + count)
                    {
                        material1.Add(first);
                        material1.Add(second);
                        material1.Add(third);


                        material1.Add(third);
                        material1.Add(second);
                        material1.Add(fourth);
                    }
                    //keep a count of how many we added
                    count += ringPointsCorner[a].Count;
                }
            }

        }


        //now sort materials based on layer/ring type
        int layerType = 0;
        if (layerType == 0)
        {
            //all solid, all to first material

            triangles[0].AddRange(material0);
            triangles[1].AddRange(material1);
            triangles[2].AddRange(material2);

        }
        if (layerType == 1)
        {
            //all spacer, to secondary material
            triangles[1].AddRange(material0);
            triangles[1].AddRange(material1);
            triangles[1].AddRange(material2);

        }
        if (layerType == 2)
        {

            //with windows and main colour
            triangles[0].AddRange(material0);
            triangles[0].AddRange(material1);
            triangles[2].AddRange(material2);

        }       


        return triangles;

    }

    GameObject WindowFrames(List<List<int>> windowPoints,List<Vector3> buildingVertices,GameObject parent)
    {
        
        //let's add all frames to one gameobject- will reduce draw calls and we will have better frames per second

        GameObject window = new GameObject();
        window.transform.parent = parent.transform;
        window.name = "Windows";
        MeshFilter meshFilter = window.AddComponent<MeshFilter>();
        window.AddComponent<MeshRenderer>().sharedMaterial =  windowFrameMaterial;

        Mesh mesh = new Mesh();
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        
        for (int i = 0; i < windowPoints.Count; i++)
        {

            //make counterclockwise 
            int temp = windowPoints[i][2];
            windowPoints[i][2] = windowPoints[i][3];
            windowPoints[i][3] = temp;
            for (int j = 0; j < windowPoints[i].Count; j++)
            {
                /*
                GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                c.transform.position = buildingVertices[windowPoints[i][j]];
                c.transform.localScale *= 0.1f;
                c.name = i.ToString() + " " + j.ToString();
                Destroy(c, 3);
                */

                //clamp list
                int nextIndex = j + 1;
                if (nextIndex > 3)
                    nextIndex -= 4;

                int nextNextIndex = j + 2;
                if (nextNextIndex > 3)
                    nextNextIndex -= 4;

                //front panel

                Vector3 thisPoint = buildingVertices[windowPoints[i][j]];
                Vector3 next = buildingVertices[windowPoints[i][nextIndex]];
                Vector3 nextNext = buildingVertices[windowPoints[i][nextNextIndex]];
                //some directions we will need 
                //extrude towards start
                Vector3 backwards = (thisPoint - next).normalized;
                Vector3 side = ((next - nextNext).normalized);
                Vector3 forward = Vector3.Cross(backwards, side);
                //bottom of window

                
                //extrude downards //and back //and forward
                Vector3 p0 = thisPoint + forward * windowFrameDepth;
                Vector3 p1 = next + forward * windowFrameDepth;
                Vector3 p2 = thisPoint + side * windowFrameSize + backwards * windowFrameSize + forward *(windowFrameDepth + windowFrameDepthOuter);
                Vector3 p3 = next + side * windowFrameSize - backwards * windowFrameSize + forward * (windowFrameDepth + windowFrameDepthOuter);

                vertices.Add(p0);
                vertices.Add(p1);
                vertices.Add(p2);
                vertices.Add(p3);

                //a dirty way to add triangles

                triangles.Add(vertices.Count - 2);                
                triangles.Add(vertices.Count - 3);
                triangles.Add(vertices.Count - 4);

                triangles.Add(vertices.Count - 1);                
                triangles.Add(vertices.Count - 3);
                triangles.Add(vertices.Count - 2);

                //outer depth
                p0 = p2;
                p1 = p3;
                p2 = p0 - forward* (windowFrameDepth + windowFrameDepthOuter);
                p3 = p1 - forward * (windowFrameDepth + windowFrameDepthOuter);

                vertices.Add(p0);
                vertices.Add(p1);
                vertices.Add(p2);
                vertices.Add(p3);

                //a dirty way to add triangles                
                triangles.Add(vertices.Count - 2);                
                triangles.Add(vertices.Count - 3);
                triangles.Add(vertices.Count - 4);

                triangles.Add(vertices.Count - 1);
                triangles.Add(vertices.Count - 3);                
                triangles.Add(vertices.Count - 2);


                //inner depth
                p2 = thisPoint;
                p3 = next;
                
                vertices.Add(p0);
                vertices.Add(p1);
                vertices.Add(p2);
                vertices.Add(p3);

                //a dirty way to add triangles
                //note - switched oreder
                triangles.Add(vertices.Count - 2);                
                triangles.Add(vertices.Count - 4);
                triangles.Add(vertices.Count - 3);
                                
                triangles.Add(vertices.Count - 3);
                triangles.Add(vertices.Count - 1);
                triangles.Add(vertices.Count - 2);

                
            }
           

        }

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        meshFilter.mesh = mesh;

        return window;

    }

    List<Vector3> Vertices(List<List<Vector3>> extrudedRings)
    {
        List<Vector3> vertices = new List<Vector3>();

        for (int i = 0; i < extrudedRings.Count; i++)
        {
            for (int j = 0; j < extrudedRings[i].Count; j++)
            {
                vertices.Add(extrudedRings[i][j]);
            }
        }

        return vertices;
    }

    GameObject Storey(List<Vector3> vertices, List<List<int>> triangles)
    {
        GameObject storey = new GameObject();
        storey.name = "Storey";
        storey.transform.parent = transform;

        MeshFilter meshFilter = storey.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = storey.AddComponent<MeshRenderer>();


        meshRenderer.sharedMaterials = materials;

        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.subMeshCount = 3;
        mesh.SetTriangles(triangles[0], 0);
        mesh.SetTriangles(triangles[1], 1);
        mesh.SetTriangles(triangles[2], 2);
        mesh.RecalculateNormals();

        meshFilter.mesh = mesh;

        return storey;
    }

    Material[] ChooseMaterials()
    {

        Material[] materials = new Material[3];
                
        Material glass = Resources.Load("Glass") as Material;
     


        materials[0] = SpacerMaterial();
        materials[1] = BuildingMaterial();
        
        materials[2] = glass;
        
        if(!windowConstantMaterial)
            windowFrameMaterial = WindowFrameMaterial();

        return materials;
    }

    Material SpacerMaterial()
    { 
        Material spacerMat = null;
        //set spacer to other tint?
        bool spacerToTint = true;
        if (spacerToTint)
        {
            if (randomSpacerMaterial)
            {
                int r = Random.Range(0, pI.palette[0].tints.Count);
                if (!darkSpacers)
                    spacerMat = pI.palette[0].tints[r];
                else
                    spacerMat = pI.palette[0].shades[r];

            }
            else if (upwardsSequentialSpacer || downwardsSequentialSpacer)
            {
                if (!darkSpacers)
                    spacerMat = pI.palette[0].tints[sequentialSpacerCounter];
                else
                    spacerMat = pI.palette[0].shades[sequentialSpacerCounter];

                if (upwardsSequentialSpacer)
                    sequentialSpacerCounter++;
                else if (downwardsSequentialSpacer)
                    sequentialSpacerCounter--;

                //restart count
                if (sequentialSpacerCounter == pI.palette[0].tints.Count - 1 || sequentialSpacerCounter == 0)
                {
                    if (inAndOutSpacer)
                    {
                        //swap order
                        if (upwardsSequentialSpacer)
                        {
                            upwardsSequentialSpacer = false;
                            downwardsSequentialSpacer = true;
                        }
                        else if (downwardsSequentialSpacer)
                        {
                            downwardsSequentialSpacer = false;
                            upwardsSequentialSpacer = true;
                        }
                    }
                    else
                    {
                        if (upwardsSequentialSpacer)
                            if (!inAndOutSpacer)
                                sequentialSpacerCounter = 0;
                            else
                                sequentialSpacerCounter--;

                        else if (downwardsSequentialSpacer)
                            sequentialSpacerCounter = pI.palette[0].tints.Count - 1;

                    }
                }
            }
            else if (constantSpacerMaterial)
            {
                if (!darkSpacers)
                    spacerMat = pI.palette[0].tints[contantSpacerIndex];
                else
                    spacerMat = pI.palette[0].shades[contantSpacerIndex];
            }
        }

        return spacerMat;
    }

    Material BuildingMaterial()
    {
        Material buildingMat = null;

        if(darkBuilding)
            buildingMat= pI.palette[0].shades[ pI.palette[0].tints.Count - 1 - buildingShadeIndex];
        else
            buildingMat = pI.palette[0].tints[buildingShadeIndex];

        //random
        //buildingMat = pI.palette[0].shades[Random.Range(0, pI.palette[0].tints.Count - 1)];
        //main
        //buildingMat = pI.palette[0].material;

        return buildingMat;
    }

    Material WindowFrameMaterial()
    {
        Material windowMat = null;


        windowMat = windowFrameMaterial;// pI.palette[0].shades[Random.Range( 0,pI.palette[0].shades.Count-1)];


        return windowMat;
    }

    List<GameObject> FloorAndCeiling(List<List<Vector3>> ringEdgePoints, List<List<Vector3>> ringCornerPoints, GameObject parent)
    {
        List<GameObject> floorAndCeiling = new List<GameObject>();

        //ceiling for each floor
        //needs non duplicated ring to do this
        
        List<Vector3> ringPoints = RingPoints(ringEdgePoints, ringCornerPoints);

        //checking for duplicates - debug
        for (int i = 0; i < ringPoints.Count; i++)
        {
            for (int j = 0; j < ringPoints.Count; j++)
            {
                if (i == j)
                    continue;

                if(ringPoints[i] == ringPoints[j])
                {
                   // GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                   // c.transform.position = ringPoints[i];
                }
            }
        }
        //remove final loop point - triangulation algorithm doesn't want duplicates
        ringPoints.RemoveAt(ringPoints.Count - 1);
        //debug checks
        //Debug.Log(ringPoints.Count);
        //ringPoints =  ringPoints.Distinct().ToList();//can use just to be safe
        //Debug.Log(ringPoints.Count + " a");

        bool flip = false;
        GameObject roof = TriangulateRing(ringPoints, flip);
        roof.transform.parent = parent.transform;
        roof.name = "Roof";
        //can do underside of floor too            
        flip = true;
        GameObject underSide = TriangulateRing(ringPoints, flip);
        underSide.transform.parent = parent.transform;
        underSide.name = "UnderSide";
        underSide.layer = LayerMask.NameToLayer("Roof");//for interior planning
        

        floorAndCeiling.Add(roof);
        floorAndCeiling.Add(underSide);


        return floorAndCeiling;
    }

    GameObject TriangulateRing(List<Vector3> roofPoints,bool flip)
    {
        GameObject roof = RoofTriangulator.RoofObjectFromConcavePolygon(roofPoints,flip);
        roof.GetComponent<MeshRenderer>().sharedMaterial = materials[1];

        return roof;
    }

    void MakeMeshesLocal(GameObject storey,List<GameObject> floorAndCeiling,GameObject windowFrames, float height)
    {
        ReAlign(storey, height);

        ReAlign(windowFrames, height);

        //re align roof - height at next floor height
        ReAlign(floorAndCeiling[0], height);
        floorAndCeiling[0].transform.position += Vector3.up * floorHeight;
        //re align underside
        ReAlign(floorAndCeiling[1], height);
    }

    void ReAlign(GameObject storey,float height)
    {
        if (storey == null)
            return;
        //makes the transform position the centre of the mesh and moves the mesh vertices so the stay the same in world space        
        //move to centre of cell we are building from
        storey.transform.position = transform.position + height * Vector3.up;

        Mesh mesh = storey.GetComponent<MeshFilter>().mesh;
        Vector3[] verts = mesh.vertices;
        List<Vector3> vertsList = new List<Vector3>();

        for (int i = 0; i < verts.Length; i++)
        {
            Vector3 point = verts[i] - transform.position;
            point -= height * Vector3.up;
            vertsList.Add(point);
        }
        mesh.vertices = vertsList.ToArray();

        mesh.RecalculateBounds();
        
    }

    void Height()
    {
        //work out height depending on closeness to centre
        float distance = transform.position.magnitude;//Vector3.Distance(transform.position, Vector3.zero);// 
        MeshGenerator mg = GameObject.FindGameObjectWithTag("Code").GetComponent<MeshGenerator>();
        float xSizeOfCity = mg.volume.x * .5f;// * .5f;// / mg.density;

        totalHeight = xSizeOfCity / distance;
        totalHeight = inExp(xSizeOfCity / totalHeight);
        totalHeight *= xSizeOfCity;// * .2f;
        totalHeight += 30;//min

        //make less linear
        totalHeight = 50f;//test
        totalHeight *= Random.Range(.5f, 1f);

        
    }

    float inExp(float x)
    {
        if (x < 0.0f) return 0.0f;
        if (x > 1.0f) return 1.0f;
        return Mathf.Pow(2.0f, 10.0f * (x - 1.0f));
    }
    //Calculate the intersection point of two lines. Returns true if lines intersect, otherwise false.
    //Note that in 3d, two lines do not intersect most of the time. So if the two lines are not in the 
    //same plane, use ClosestPointsOnTwoLines() instead.
    public static bool LineLineIntersection(out Vector3 intersection, Vector3 linePoint1, Vector3 lineVec1, Vector3 linePoint2, Vector3 lineVec2)
    {

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

}
