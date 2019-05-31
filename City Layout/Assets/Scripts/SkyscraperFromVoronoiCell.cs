using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkyscraperFromVoronoiCell : MonoBehaviour {

    public bool glassType = true;
    public float scale = .9f;

    [Range(.1f, 1000f)]
    public float windowX = 1f;
   // [Range(0.0f, 1f)]
    public float cornerSize = .5f;
    public float windowFrameX = 0.2f;//**var
    public int windowFractionOfFloorHeight = 6;

    public bool uniformCorners = false;
    [Range(.0f, 1f)]
    public float cornerScaler = 1f;
    public bool coolTop = false;
    public float curveControlPointSize = 0.5f;//??? what to use
    public float curveAccuracyCorners = 10f;
    public float curveAccuracyHeight = 10f;//debug only
    private float curveStepSize = .5f;
    public float smoothAccuracy = .1f;

    public bool capHeight = false;
    public float testHeight = 30f;
    public float totalHeight = 10f;
    public float floorHeight = 3f;
    public float spacerHeight = 0.2f;
    public float segmentHeight = 10f;
    public bool doAnimationForEachFloor = false;
    public bool doAnimationFullBuilding=true;
    public float animationSpeed = 1f;
    float animationTimer = 0f;
    public Material[] materials;

    public bool nextFrameBuild = false;

  //  private GameObject child;
    public List<BezierSpline> splines = new List<BezierSpline>();

    public bool buildingInProgress = false;
    public bool finishedBuilding = false;
    public bool doRandom = true;
    public bool doSpin = true;
    public int spinPlus = 20;

    public int baseWindowAmount = 0;

    private Vector3 centroid = Vector3.zero;

    //opto, wokring out corner window amount
    List<float> cornerLengths = new List<float>();
    List<int> edgesWindowAmount = new List<int>();
    List<Vector3> baseWindowPoints = new List<Vector3>();

    private BezierSpline spline;
    BezierSpline bezierForCorners;
    //used to remember where last curve poitn was when calculating poitns for height
    float lastStepFloorFound =0f;//having it at 0.01 seems to circumvent a glitch with first spacer size
    ExtrudeCell extrudeCell;
    

    public bool cellReAligned = false;

    

    public void Awake()
    {
        enabled = false;
    }

    public void Start()
    {
        //abezier we wil re -use
        bezierForCorners = gameObject.AddComponent<BezierSpline>();
        extrudeCell = GetComponent<ExtrudeCell>();

        if (Random.value >= 0)//forcing floor animation with 0 here. Bounce skyscrapers dont look as cool
        {
            doAnimationForEachFloor = true;
            doAnimationFullBuilding = false;
            animationSpeed = .2f + Random.Range(-.1f,.1f);
        }
        else
        {
            doAnimationForEachFloor = false;
            doAnimationFullBuilding = true;
            animationSpeed = 2f;
        }

        //object for skyscraper mesh
        /*
        child = new GameObject();
        child.transform.parent = gameObject.transform;
        child.transform.position = gameObject.transform.position;
        child.AddComponent<MeshRenderer>();
        child.AddComponent<MeshFilter>();
        */

//        Debug.Log("Start");
        Height();

        if(capHeight)
            totalHeight = testHeight;

        LoadMaterials();

        //splines.Clear();

        if (glassType)
        {
            spline = gameObject.AddComponent<BezierSpline>();

            //work out how many windows a level will have (glass)
            
            BaseData();

            AddSplinesForUniformPoints();
        }
        else
        {
            AddSplines();
        }

        

        if (doRandom)
        {
            RandomiseValues();
        }

        StartCoroutine("BuildByFrame");
        //StartBuild(doRandom);
    }

    public void ReAlignCell()
    {
        //transform is zero and mesh points are world points. change this to local mesh points with transform position correct

        transform.position = centroid; 
        Vector3[] vertices = gameObject.GetComponent<MeshFilter>().mesh.vertices;

        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i] -= transform.position;
        }

        gameObject.GetComponent<MeshFilter>().mesh.vertices = vertices;
        gameObject.GetComponent<MeshFilter>().mesh.RecalculateBounds();
    }        

    public void Update()
    {
        // Mesh skyMesh = SkyscraperMesh();
        //  GetComponent<MeshFilter>().mesh = skyMesh;

        if (nextFrameBuild)
        {
            StartBuild(doRandom);
        }

        bool drawSpline = true;
        if(drawSpline)
        {

            float step = 1f / curveAccuracyHeight;

            for (int i = 0; i < splines.Count; i++)
            {


                for (float j = 0; j < curveAccuracyHeight; j += step)
                {
                    //step through curve until we find the height passed
                    Vector3 p0 = splines[i].GetPoint(j);
                    Vector3 p1 = splines[i].GetPoint(j + step);

                    if(i==0)
                        Debug.DrawLine(p0 , p1 , Color.red);
                    else
                        Debug.DrawLine(p0 , p1 , Color.blue);
                }
            }
        }

    }

    void StartBuild(bool doRandom)
    {
      //  Debug.Log("Start Build");
        //remove any splines - debug
        foreach (var comp in gameObject.GetComponents<BezierSpline>())
        {
            Destroy(comp);
        }

        if (cellReAligned == false)
        {
            //re align cell first of all
            //work out centroid
            Vector3[] vertices = gameObject.GetComponent<MeshFilter>().mesh.vertices;
            List<Vector2> v2List = new List<Vector2>();
            for (int i = 0; i < vertices.Length; i++)
            {
                v2List.Add(new Vector2(vertices[i].x, vertices[i].z));
            }

            Vector2 centroidV2 = GetCentroid(v2List);
            centroid = new Vector3(centroidV2.x, 0f, centroidV2.y) + transform.position;//adding ransform position here for testing if we are computingcentroid again. First time t.position is 0,0,0, next time we have moved it.
            transform.position = centroid;

          //  GameObject c = GameObject.CreatePrimitive(PrimitiveType.Sphere);
          //  c.transform.position = centroid;
          //  c.name = "centroid";

        
            ReAlignCell();
            cellReAligned = true;

            
          
        }

        //now we can scale
        if (!glassType)
        {
            Scale();///not happenign from build control?
        }

        if (glassType)
        {
           // Debug.Log("here");
            if (!doSpin)
                Scale();////scales using bounds? makes diff road size atm
            else
                ScaleForSpin();
        }

        //spline = gameObject.AddComponent<BezierSpline>();

        //work out how many windows a level will have (glass)
       // if (glassType)
       //     BaseData();

       

       
        

        cornerLengths = new List<float>();

        //work out corner size, size is smallest edge, can be scaled
     //   List<Vector3> alteredPoints = new List<Vector3>(GetComponent<MeshFilter>().mesh.vertices);
       // alteredPoints.RemoveAt(0);
        //UniformCornerSize(alteredPoints);

      

        if (!buildingInProgress)
        {
            //destroy any child objects(used for testing)
            for (int i = 0; i < gameObject.transform.childCount; i++)
            {
                if(gameObject.transform.GetChild(i).name != "Extruded Cell")
                    Destroy(gameObject.transform.GetChild(i).gameObject);
            }

            if (doRandom)
            {

                RandomiseValues();
                

            }
            
            LoadMaterials();
            

            // child.GetComponent<MeshFilter>().mesh.Clear();
            // Mesh skyMesh = SkyscraperMesh();
            // child.GetComponent<MeshFilter>().mesh = skyMesh;

            buildingInProgress = true;

           
           // BuildByFrame();
        }
    }

    void RandomiseValues()
    {
       // glassType = Random.value >= 0.8;
        doSpin = Random.value >= 0.8f;
        

        Height();
        if (capHeight)
            totalHeight = testHeight;

     

        windowX = Random.Range(1f, 2f);
        windowFrameX = Random.Range(.05f, windowX*0.5f);
        windowFractionOfFloorHeight = Random.Range(4,10);
        spacerHeight = Random.Range(.1f, 1.5f);        
        floorHeight = Random.Range(2.5f, 3.5f);

        //corner size needs to be bigger or equal than windowX
        //we need to find the smallest edge
        

        uniformCorners = Random.value >= 0.8f;
        cornerScaler =  Random.Range(0.01f, 1f);//how stretched the corners are
        coolTop = Random.value >= 0.8f;

        

        //to work ou segment height, let's consider total height and how many different segments we want it divided in to
        float multiplier = 1.5f;//higher number here means more chunky chance
        float divider = (Random.Range(1f, totalHeight * multiplier));
        if (!glassType)
        {
            segmentHeight = floorHeight * Random.Range(1,20);
        }
        else
            segmentHeight = floorHeight;

        if(doSpin)
        {
            if(glassType)
                spinPlus = Random.Range(-90, 90);
            else
            {
                spinPlus = Random.Range(-5, 5);

            }

        }


        // segmentHeight = floorHeight * Random.Range(1f, segmentHeight);
    }

    IEnumerator BuildByFrame()
    {

        //yield return new WaitForEndOfFrame();

        // Vector3[] cellVertices = gameObject.GetComponent<MeshFilter>().mesh.vertices;
        List<Vector3> vertices = new List<Vector3>();
        //tris
        List<List<int>> trianglesList = new List<List<int>>();
        trianglesList.Add(new List<int>());//0
        trianglesList.Add(new List<int>());//1
        trianglesList.Add(new List<int>());//2

       

        int verticesThisRing = 0;
        int floorsSoFar = 0;
        float spin = 0f;
        //vertices
        
        float lastHeightSize = 0f;

        bool breakFromLoop = false;
        for (float i =0; i < totalHeight - (floorHeight+spacerHeight); i += floorHeight + spacerHeight) //keep sapce for one floor, curve will stop after this
        {
            if (breakFromLoop)
                break;
            //get mesh ring
            //remember which points are windows
            List<List<int>> materialsForTriangles = new List<List<int>>();
            //we build roof inside

            //make a new child every floor and apply mesh to it, too many vertices for one mesh!
            GameObject child = new GameObject();
            
            int moveIndexBy = 0;

            int end = 6;
            if (glassType)
                end = 3;
            float startingHeight = 0f;// GetComponent<ExtrudeCell>().depth - floorsSoFar * (floorHeight + spacerHeight);// 0f;
            float yPos = GetComponent<ExtrudeCell>().depth + (floorHeight + spacerHeight) * floorsSoFar;

            GameObject s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            s.transform.position = Vector3.up * i;

            //different functions for different types of tower
            List<Vector3> thisRingVertices = new List<Vector3>();

            for (int y = 0; y < end; y++)
            {
                int layerType = 0;
                float heightForSize = i;

               // float diffBetweenNextRing = Mathf.Abs(heightForSize - lastHeightSize);
                
               // if (diffBetweenNextRing < segmentHeight)
                //    heightForSize = lastHeightSize;

                //layertype 
                //0 is solid main colour
                //1 is spacer secondary colour
                //2 is windows with main colour between, or all glass depending on which function you call
                
             
                float extrusionHeight = 0f;

                
                if (y == 0)
                {
                    layerType = 1;
                    extrusionHeight = spacerHeight * 0.5f;
                  //  startingHeight = 0f;
                }
                
                if (y == 1)
                {
                    layerType = 0;

                    //building layer
                    extrusionHeight = (floorHeight / 3) - (floorHeight / windowFractionOfFloorHeight);
                   // startingHeight = spacerHeight * 0.5f;//last extrusion

                    if (glassType)
                    {
                        layerType = 2;
                        extrusionHeight = floorHeight;// - spacerHeight*.5f;
                    }
                }
                if (y == 2)
                {
                    layerType = 2;
                    //glass
                    extrusionHeight = (floorHeight / 3) + (floorHeight / windowFractionOfFloorHeight)*2;
                    // startingHeight = spacerHeight * 0.5f + floorHeight / 3;//last height
                    if (glassType)
                    {
                        layerType = 1;
                        startingHeight = (floorHeight+spacerHeight*0.5f);
                        extrusionHeight = spacerHeight*0.5f;
                    }
                }
                if (y == 3)
                {
                    //building layer
                    layerType = 0;
                    extrusionHeight = (floorHeight / 3) - (floorHeight / windowFractionOfFloorHeight);
                    // startingHeight = spacerHeight * 0.5f + floorHeight / 3 + floorHeight / 3;
                }
                if (y == 4)
                {

                    layerType = 1;
                    extrusionHeight = spacerHeight * .5f;
                   // startingHeight = spacerHeight * 0.5f + floorHeight / 3 + floorHeight / 3 + floorHeight / 3;


                }
                if (y == 5)
                {
                    layerType = 1;
                  //  startingHeight = spacerHeight * 0.5f + floorHeight / 3 + floorHeight / 3 + floorHeight / 3;
                    extrusionHeight = floorHeight;
                }

                
                
               // bool spacer = false;//for debug purposes

               

                float heightToCheckWorldSpace = i + startingHeight + extrudeCell.depth;
                if (!glassType && y== 0)
                    thisRingVertices = SkyscraperRingUniform(out materialsForTriangles, heightForSize,heightToCheckWorldSpace , windowX, layerType, floorsSoFar);
               if(glassType)
                    thisRingVertices = SkyscraperRingStretched4(out materialsForTriangles, i + startingHeight, windowX, layerType, floorsSoFar);
               if(y==0)
                    child.name = (startingHeight + i).ToString();

                if (!glassType)
                {
                    //hack to move on top of extruided cell
                    for (int a = 0; a < thisRingVertices.Count; a++)
                    {
                        //moving vertices before they are put in to their own transform so we can animate nicely
                        //thisRingVertices[a] += (floorsSoFar * (floorHeight + spacerHeight)) * Vector3.up;
                    }
                }
                if(glassType)
                {
                    //hack to move on top of extruided cell
                    for (int a = 0; a < thisRingVertices.Count; a++)
                    {
                        //moving vertices before they are put in to their own transform so we can animate nicely
                        Debug.DrawLine(thisRingVertices[a], thisRingVertices[a] + Vector3.up, Color.red);
                        thisRingVertices[a] -= (floorsSoFar * (floorHeight + spacerHeight)) * Vector3.up;
                        //  thisRingVertices[a] -= (GetComponent<ExtrudeCell>().depth*Vector3.up);// - floorsSoFar * (floorHeight + spacerHeight)) * Vector3.up;

                    }
                }

                if (doSpin)
                {

                    if (floorsSoFar > 0)
                    {
                        //if (forSpacer)
                        //  spinForThisRing = (ringsSoFar - 2f + (spacerHeight / floorHeight)) * spin;

                        for (int j = 0; j < thisRingVertices.Count; j++)
                        {
                            Vector3 t = thisRingVertices[j];
                            if (doSpin)
                                t = Quaternion.Euler(0, spin, 0) * t;

                            thisRingVertices[j] = t;
                        }
                    }
                }

                //add first point for last too to make full loop
                thisRingVertices.Add(thisRingVertices[0]);
                verticesThisRing = thisRingVertices.Count;
                vertices.AddRange(thisRingVertices);

                if (y < 5)// && !glassType || y < 2 && glassType))
                {
                    if (!glassType)
                    {                    //do next height- extrude
                        for (int j = 0; j < thisRingVertices.Count; j++)
                        {
                            vertices.Add(thisRingVertices[j] + Vector3.up * extrusionHeight);
                        }
                    }
                    else if(glassType)
                    {

                        thisRingVertices = SkyscraperRingStretched4(out materialsForTriangles,i+startingHeight+ extrusionHeight, windowX, layerType, floorsSoFar);

                        //hack to move on top of extruided cell
                        for (int a = 0; a < thisRingVertices.Count; a++)
                        {
                            //moving vertices before they are put in to their own transform so we can animate nicely
                            Debug.DrawLine(thisRingVertices[a], thisRingVertices[a] + Vector3.up, Color.red);
                            thisRingVertices[a] -= (floorsSoFar * (floorHeight + spacerHeight)) * Vector3.up;

                        }

                        if (floorsSoFar > 0)
                        {
                            //if (forSpacer)
                            //  spinForThisRing = (ringsSoFar - 2f + (spacerHeight / floorHeight)) * spin;

                            for (int j = 0; j < thisRingVertices.Count; j++)
                            {
                                Vector3 t = thisRingVertices[j];
                                if(doSpin)
                                    t = Quaternion.Euler(0, spin, 0) * t;
                                thisRingVertices[j] = t;
                            }
                        }

                        if (thisRingVertices.Count == 0)
                        {
                            //we have reached the top, make roof and exit
                            breakFromLoop = true;
                            break;
                        }
                        //add first point for last too to make full loop
                        thisRingVertices.Add(thisRingVertices[0]);
                        verticesThisRing = thisRingVertices.Count;
                        vertices.AddRange(thisRingVertices);

                    }

                }
                else
                {
                    
                }

                if(glassType & y == 2)
                    AddRoof(child, thisRingVertices,false);
                else if(!glassType && y == 5)
                    AddRoof(child, thisRingVertices,false);


                SetMaterials(verticesThisRing, materialsForTriangles, moveIndexBy, vertices, trianglesList);
                lastHeightSize = heightForSize;

                startingHeight += extrusionHeight;
            }

          
            //
            if(!glassType && doSpin)
                spin += spinPlus;// 0f;



        
            
            
            
            child.transform.parent = gameObject.transform;
          
           // yPos = 0f;
            child.transform.position = new Vector3(gameObject.transform.position.x,3f, gameObject.transform.position.z);
            child.AddComponent<MeshRenderer>();
            child.AddComponent<MeshFilter>();
            child.GetComponent<MeshRenderer>().sharedMaterials = materials;

            

            Mesh mesh = new Mesh();
            mesh.vertices = vertices.ToArray();
            mesh.subMeshCount = 3;
            mesh.SetTriangles(trianglesList[0], 0);
            mesh.SetTriangles(trianglesList[1], 1);
            mesh.SetTriangles(trianglesList[2], 2);
            mesh.RecalculateNormals();

            child.GetComponent<MeshFilter>().mesh = mesh;


            for (int a = 0; a < vertices.Count; a++)
            {
                /*
                GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                c.transform.localScale *= 0.2f;
                c.transform.position = vertices[a] + child.transform.position;
                c.name = i.ToString();
                Destroy(c, 5f);
                */
            }

            //reset lists
            vertices = new List<Vector3>();
            trianglesList = new List<List<int>>();
            trianglesList.Add(new List<int>());//0
            trianglesList.Add(new List<int>());//1
            trianglesList.Add(new List<int>());//2


            floorsSoFar++;
            //if(floorsSoFar % 1 == 0)
            //     yield return new WaitForEndOfFrame();

            if(doAnimationForEachFloor)
            {
                animationTimer = 0f;
                Vector3 startScale = new Vector3(1f, 0.1f, 1f);
                Vector3 targetScale = Vector3.one;
                while (animationTimer < 1)
                {
                    animationTimer += Time.deltaTime / animationSpeed;
                    float eased = Easings.QuarticEaseInOut(animationTimer);

                    
                    child.transform.localScale = Vector3.Lerp(startScale, targetScale, eased);

                    yield return new WaitForFixedUpdate();
                }
            }
           
            //yield return new WaitForSeconds(0.1f);
            
        }
        if (doAnimationFullBuilding)
        {

            transform.localScale = Vector3.zero;
            animationTimer = 0f;
            Vector3 startScale = new Vector3(1f, 0.1f, 1f);
            Vector3 targetScale = Vector3.one;
            while (animationTimer < 1)
            {
                animationTimer += Time.deltaTime / animationSpeed;
                float eased = Easings.BounceEaseOut(animationTimer);
                
                transform.localScale = Vector3.Lerp(startScale, targetScale, eased);

                yield return new WaitForFixedUpdate();
            }
        }

        //Roof(verticesThisRing);//stitching all floors


       

        //child.GetComponent<MeshFilter>().mesh = mesh;

        buildingInProgress = false;
        nextFrameBuild = false;
        finishedBuilding = true;
        yield break;
    }

    GameObject AddRoof(GameObject child, List<Vector3> roofPoints,bool flip)
    {
        GameObject roof = RoofTriangulator.RoofObjectFromConcavePolygon(roofPoints,flip);
        roof.name = "Floor Roof";
        roof.transform.parent = child.transform;
        roof.transform.localPosition = Vector3.zero;
        //roof.transform.position = transform.position + Vector3.up * (startingHeight);
        
        roof.GetComponent<MeshRenderer>().sharedMaterial = materials[1];

        return roof;
    }
        
    public List<List<int>> SetMaterials(int verticesThisRing, List<List<int>> materialsForTriangles,int moveIndexBy,List<Vector3> vertices,List<List<int>> trianglesList)
    {
        for (int a = 0; a < verticesThisRing - 1; a++)
        {
            //lining up with lower ring as best as we can
            int indiceForLower = a + moveIndexBy;

            if (indiceForLower >= verticesThisRing - 1)
                indiceForLower -= verticesThisRing - 1;

            //    if (!forLedge)
            //       indiceForLower = a;

            for (int q = 0; q < materialsForTriangles.Count; q++)
            {
                if (materialsForTriangles[q].Contains(a))
                {
                    trianglesList[q].Add(vertices.Count - verticesThisRing + a);
                    trianglesList[q].Add(vertices.Count - verticesThisRing + a - verticesThisRing);
                    trianglesList[q].Add(vertices.Count - verticesThisRing + a + 1);

                    trianglesList[q].Add(vertices.Count - verticesThisRing + a + 1);
                    trianglesList[q].Add(vertices.Count - verticesThisRing + a - verticesThisRing);
                    trianglesList[q].Add(vertices.Count - verticesThisRing + a - verticesThisRing + 1);

                }
            }
        }

        return trianglesList;
    }

    public List<Vector3> SkyscraperRingUniform(out List<List<int>> trianglesForWindows,float height,float heightForBottomVertice, float windowX, int layerType,int floorsSoFar)
    {
        
        //create skyscraper from a voronoi cell, attach this script to cell
        Vector3[] cellVertices = gameObject.GetComponent<MeshFilter>().mesh.vertices;
        List<List<Vector3>> ringPoints = new List<List<Vector3>>();
        List<Vector3> alteredCornerPoints = new List<Vector3>();



        for (int i = 0; i < cellVertices.Length; i++)
        {
            //0 vertice is central, skip
            if (i == 0)
                continue;

            //run around perimeter
            //create looped indexes
            int thisIndex = i;
            int nextIndex = i + 1;
            if (nextIndex > cellVertices.Length - 1)//because we skip 0
                nextIndex = 1;

            Vector3 thisPoint = cellVertices[thisIndex] + Vector3.up * height;
            Vector3 nextPoint = cellVertices[nextIndex] + Vector3.up * height;

            //SCALE CELL POINTS
            //now we have our target points, check against shape curve and scale accordingly ??
            
            //this pline and next spline - using 2 loop to swap between splines
            for (int x = 0; x < 2; x++)
            {
                BezierSpline thisSpline = splines[thisIndex-1];//checking each spline twice?
                if(x==1)
                    thisSpline = splines[nextIndex-1];

                Vector3 top = thisSpline.GetPoint(totalHeight);
                Debug.DrawLine(top, top + Vector3.right, Color.green);

               // float step =1f/ ( totalHeight/ segmentHeight);//   1f / curveAccuracyHeight;
                float step = 1f / 200f;// (totalHeight / segmentHeight);//   1f / curveAccuracyHeight;

                Vector3 p = Vector3.zero;
                //float startAt = (totalHeight/floorHeight)* floorsSoFar;//optimisation idea
                
                if(i==1)//search for height
                {
                    //Debug.Log("height = " + height);
                    bool found = false;
                    /*//draw curve for debugging
                    for (float j = 0f; j < totalHeight; j += step)//j < curveaccuracyheight                
                    {
                        // Debug.Log(j);
                        //step through curve until we find the height passed
                        p = thisSpline.GetPoint(j);

                        Vector3 nextP = thisSpline.GetPoint(j + step);
                        Debug.Log(p);
                        Debug.DrawLine(p, nextP, Color.red);
                    }
                    */

                    for (float j = lastStepFloorFound; j < totalHeight; j += step)//j < curveaccuracyheight                
                    {
                       // Debug.Log(j);
                        //step through curve until we find the height passed
                        p = thisSpline.GetPoint(j);


                        // Debug.Log(p);


                      //  GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                      //  c.transform.position = p;// + extrudeCell.depth*Vector3.up;
                      //  c.name = "step" + i.ToString() + x.ToString() + j.ToString();
                     //   c.transform.localScale *= 0.5f;
                      //  Destroy(c, 5);

                        if (p.y>= height)
                        {

                            Debug.DrawLine(p, p + Vector3.up, Color.magenta);
                            //find the difference between the orignal voronoi point's x and z position at this curve point
                            p = new Vector3(p.x, heightForBottomVertice, p.z);
                            if (x == 0)
                            {
                                //we need this point for the corner, save it
                                alteredCornerPoints.Add(p);
                                thisPoint = p;
                            }
                            if (x == 1)
                                nextPoint = p;

                            //remember for next floor so we dont need to start at 0 every time
                            lastStepFloorFound = j;

                            found = true;

                            //  c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                            //   c.transform.position = p;// + extrudeCell.depth * Vector3.up;
                            //   Destroy(c, 5);

                            //get out of this loop
                            
                            break;
                        }
                    }
                    if (!found)
                    {
                       // Debug.DrawLine(p, p + Vector3.up, Color.magenta);
                       // Vector3 top = thisSpline.GetPoint(totalHeight);
                      //  Debug.DrawLine(top, top + Vector3.right, Color.green);
                        Debug.Break();
                        Debug.Log("Never found?" + x.ToString());
                        Debug.Log("p.y = " + p.y.ToString());
                    }
                }
                else //use height already found
                {
                   
                    p = thisSpline.GetPoint(lastStepFloorFound);
                    Debug.DrawLine(p, p + Vector3.up, Color.blue);

                    p = new Vector3(p.x, heightForBottomVertice, p.z);
                    if (x == 0)
                    {
                        //we need this point for the corner, save it
                        alteredCornerPoints.Add(p);
                        thisPoint = p;
                    }
                    if (x == 1)
                        nextPoint = p;

                }

                // Debug.Break();
            }
            // if (spacer)
            //     Debug.Log(alteredCornerPoints.Count);

          


            Vector3 halfPoint = Vector3.Lerp(thisPoint, nextPoint, 0.5f);
            Vector3 dirToNext = (nextPoint - thisPoint).normalized;
            Vector3 dirToPrev = (thisPoint - nextPoint).normalized;
            float distanceToNext = Vector3.Distance(thisPoint, nextPoint);


            //work out corner size for upcoming corner loop if we want windows at the top
            if (!coolTop)// && !uniformCorners)//cant ahve both, bugs will appear, not really bothered about uniforomo corners, i cant really notice the difference and i coded it!
            {
                float smallestDistance = Mathf.Infinity;
                for (int j = 0; j < alteredCornerPoints.Count; j++)
                {
                    int thisIndexA = j;
                    int nextIndexA = j + 1;
                    if (nextIndexA > alteredCornerPoints.Count - 1)
                        nextIndexA = 0;

                    float tempD = Vector3.Distance(alteredCornerPoints[thisIndexA], alteredCornerPoints[nextIndexA]);
                    if (tempD < smallestDistance)
                        smallestDistance = tempD;
                }

                cornerSize = smallestDistance * cornerScaler - windowX * .5f;// as large a curve as we can get
                if (cornerSize < windowX * 2)//*2 for safety, can have overlap at edges
                    cornerSize = windowX * 2;
            }

          

            //work out from middle, then spin and come back. This ensures sides are symmetrical
            Vector3 dirToUse = dirToPrev;

            List<Vector3> thisEdgePoints = new List<Vector3>();
            
            for (int j = 0; j < 2; j++)
            {
                List<Vector3> tempPoints = new List<Vector3>();

                //TESTING
                //if distance is short, palce middle point and jump to next, otherwise, below loop will jump out at 0
                //using distance to next point gives us super smooth corners, we can get rid of this with cornerSize value(0f -1f)
                float tempCornerSize = (distanceToNext * 0.5f) * cornerScaler*.5f; 
                if (tempCornerSize < windowX*2)
                    tempCornerSize = windowX*2;

                if (uniformCorners)
                {   
                    //we can clamp all corners to be same size
                    tempCornerSize = cornerSize;

                    if (tempCornerSize < windowX * 2)
                    {                        
                        tempCornerSize = windowX * 2;
                        
                    }
                    
                }
              

             
                
                if (j == 1)
                {
                    dirToUse = dirToNext;
                    //avoid duplicate on second loop
                   // start = windowFrameX;
                }

                //drop a point for middle of side( this does mean this will be a duplicate point, because the other half of this side uses it too ?? removed below?
                if (j == 1)
                {
                    Vector3 p0 = halfPoint;// + dirToUse * k;
                    tempPoints.Add(p0);
                }

                for (float k = windowFrameX; k < distanceToNext * 0.5f - tempCornerSize; k += windowFrameX )
                {

                    Vector3  p = halfPoint + dirToUse * k;
                    tempPoints.Add(p);

                    k += windowX;
                    p = halfPoint + dirToUse * k;
                    tempPoints.Add(p);

                    //add window frame
                    //double check if this will be the last

                    k += windowFrameX;
                    p = halfPoint + dirToUse * k;
                    tempPoints.Add(p);

                }

                //spin first half
                if (j == 0)
                    tempPoints.Reverse();

                //add to this edge points
                thisEdgePoints.AddRange(tempPoints);
            }

            //add to main list
            ringPoints.Add(thisEdgePoints);
           
            
        }
      


        //we have working out our edge points, now we need to make a smooth curve between them for the corners

        
        
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

            if (indexForCellVertice == alteredCornerPoints.Count)
            {
               // Debug.Log("This happened 2");
                indexForCellVertice = 0;
            }

            Vector3 nearestCellVertice = Vector3.zero;
           // Debug.Log("index = " + indexForCellVertice + ",altered corner points count = " + alteredCornerPoints.Count);
            if (indexForCellVertice < alteredCornerPoints.Count)
            {
                nearestCellVertice = alteredCornerPoints[indexForCellVertice];// cellVertices[verticeCell] + Vector3.up*height;
            }
            else
            {
              //  Debug.Log("out of indexx");
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

            List<Vector3> cornerPoints = new List<Vector3>();

           
            //possible we need to run from middle of curve out to both sides like we did above with flat sides so it is symmetrical?
            for (float k = 0; k < curveAccuracyCorners; k+=step)
            {
                Vector3 p0 = bezierForCorners.GetPoint(k * step);
                Vector3 d = Quaternion.Euler(0, 90, 0) * bezierForCorners.GetDirection(k)*step;
               // Debug.DrawLine(p0, p0 + d, Color.red);

                if (distanceSinceLast >= curveStepSize)
                {
                    Vector3 p1 = bezierForCorners.GetPoint((k + step) * step);
                    cornerPoints.Add(p0);
                    distanceSinceLast = 0f;

                }
                else
                {
                    distanceSinceLast += Vector3.Distance(lastStepPoint, p0);
                    lastStepPoint = p0;
                }
            }
            ringPointsCorner.Add(cornerPoints);
        }

    


        List<Vector3> finalRingVertices = new List<Vector3>();
        List<List<int>> windowTrianglesToReturn = new List<List<int>>();

        windowTrianglesToReturn.Add(new List<int>());
        windowTrianglesToReturn.Add(new List<int>());
        windowTrianglesToReturn.Add(new List<int>());

        List<int> material0 = new List<int>();
        List<int> material1 = new List<int>();
        List<int> material2 = new List<int>();


        for (int i = 0; i < ringPoints.Count; i++)
        {
            for (int j = 0; j < ringPoints[i].Count; j++)
            {
                //Debug.DrawLine(ringPoints[i][j], ringPoints[i][j + 1]);

                finalRingVertices.Add(ringPoints[i][j] - transform.position);

                //add vertice number to a material list. this will later be set in the mesh
            
                //this is some maths which figures out to leave a window pain every third vertice
                if( (j +2) % 3 == 0) 
                    material2.Add(finalRingVertices.Count - 1);                        
                else
                    material0.Add(finalRingVertices.Count - 1);
               
                /*
                GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                c.transform.position = ringPoints[i][j];
                c.transform.localScale *= 0.1f;
                c.name = (finalRingVertices.Count-1).ToString();
                */
            }

            for (int j = 0; j < ringPointsCorner[i].Count; j++)
            {
               // Debug.DrawLine(ringPointsCorner[i][j], ringPointsCorner[i][j + 1]);

                finalRingVertices.Add(ringPointsCorner[i][j] - transform.position);

                material0.Add(finalRingVertices.Count - 1);

                /*
                GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                c.transform.position = ringPointsCorner[i][j];
                c.transform.localScale *= 0.1f;
                c.name = " vertice round";
                */
            }
        }


        //now sort materials based on layer/ring type
        if (layerType == 0)
        {
            //all solid, all to first material
            
            windowTrianglesToReturn[0].AddRange(material0);
            windowTrianglesToReturn[0].AddRange(material1);
            windowTrianglesToReturn[0].AddRange(material2);
            
        }
        if (layerType == 1)
        {
            //all spacer, to secondary material
              windowTrianglesToReturn[1].AddRange(material0);
              windowTrianglesToReturn[1].AddRange(material1);
             windowTrianglesToReturn[1].AddRange(material2);
            
        }
        if (layerType == 2)
        {
            
            //with windows and main colour
            windowTrianglesToReturn[0].AddRange(material0);
            windowTrianglesToReturn[0].AddRange(material1);
            windowTrianglesToReturn[2].AddRange(material2);
            
        }


        trianglesForWindows = windowTrianglesToReturn;
        return finalRingVertices;
    }

    public void UniformCornerSize(List<Vector3> alteredPoints)
    {
        //this function works out how many windows will be on each side and on each curve all the way up a glass skyscraper
        List<int> returnList = new List<int>();
        
        //decide on corner size
        Vector3[] cellVertices = gameObject.GetComponent<MeshFilter>().mesh.vertices;
        //use this to work out smallest side
        float smallestEdgeSize = SmallestEdgeSizeList(alteredPoints);
        cornerSize = smallestEdgeSize*.2f; //max .5f

    }

    public List<Vector3> ScaleRing(Vector3[] cellVertices,float height)
    {

        List<Vector3> alteredPoints = new List<Vector3>();
        for (int i = 0; i < cellVertices.Length; i++)
        {
            //0 vertice is central, skip
            if (i == 0)
                continue;

            //run around perimeter
            //create looped indexes
            int thisIndex = i;
            int nextIndex = i + 1;
            if (nextIndex > cellVertices.Length - 1)
                nextIndex = 1;

            

            Vector3 thisPoint = cellVertices[thisIndex] + Vector3.up * height;
            Vector3 nextPoint = cellVertices[nextIndex] + Vector3.up * height;

            Vector3 originalThis = thisPoint;
            Vector3 originalNext = nextPoint;
            // for (int x = 0; x < 2; x++)
            {
                BezierSpline thisSpline = splines[thisIndex];
                //if (x == 1)
                //     thisSpline = splines[nextIndex];

                float step = 1f / curveAccuracyHeight;
                Vector3 p = Vector3.zero;
                //float startAt = (totalHeight/floorHeight)* floorsSoFar;//optimisation idea
                for (float j = 0; j < curveAccuracyHeight; j += step)
                {
                    //step through curve until we find the height passed
                    p = thisSpline.GetPoint(j);

                    // Debug.DrawLine(p, p + Vector3.right, Color.blue);

                    if (p.y >= height)
                    {
                        //Debug.DrawLine(p, p + Vector3.up, Color.magenta);
                        //find the difference between the orignal voronoi point's x and z position at this curve point
                        p = new Vector3(p.x, height, p.z);

                        alteredPoints.Add(p);

                        break;
                    }
                }          
            }
        }

        return alteredPoints;
    }

    public void BaseData()
    {
        //create skyscraper from a voronoi cell, attach this script to cell
        Vector3[] cellVertices = gameObject.GetComponent<MeshFilter>().mesh.vertices;
        List<List<Vector3>> ringPoints = new List<List<Vector3>>();
        List<Vector3> alteredCornerPoints = new List<Vector3>(cellVertices);

        //scale ring. Checks x and z co-ords agaisnt height on the the constructor splines
        //height is 0 for base data
        //alteredCornerPoints = ScaleRing(cellVertices,0f);
        //make all sides the same distance from centre
        
        List<Vector3> verticeList = new List<Vector3>();
        GetComponent<MeshFilter>().mesh.RecalculateBounds();
        //Vector3 center = GetComponent<MeshRenderer>().bounds.center;

       // GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
      //  c.transform.position = center;
       

        //make curve around cell
        for (int i = 1; i < alteredCornerPoints.Count+1; i++)
        {
            int thisIndex = i;
            int nextIndex = i + 1;
            int nextNextIndex = i + 2;
            //int verticeCell = i + 2;
            //covers last index and loops back to first
            if (thisIndex > alteredCornerPoints.Count - 1)
            {
                thisIndex -= alteredCornerPoints.Count-1;
            }
            if (nextIndex > alteredCornerPoints.Count - 1)
            {
                nextIndex -= alteredCornerPoints.Count-1;
            }
            if (nextNextIndex > alteredCornerPoints.Count - 1)
            {
                nextNextIndex -= alteredCornerPoints.Count-1;
            }

            //scaled
            Vector3 thisPoint = alteredCornerPoints[thisIndex];
            Vector3 nextPoint = alteredCornerPoints[nextIndex];
            Vector3 nextNextPoint = alteredCornerPoints[nextNextIndex];

            Vector3 dirToNext = (nextPoint - thisPoint).normalized;
            Vector3 dirToPrev = (thisPoint - nextPoint).normalized;
            Vector3 dirToNextNext = (nextNextPoint - nextPoint).normalized;

            //cornerSize *= 0.5f;
            Vector3 startPoint = thisPoint + dirToNext * cornerSize;
            Vector3 endPoint = nextPoint + dirToPrev * cornerSize;
            Vector3 verticePoint = thisPoint;
            Vector3 nextVerticePoint = alteredCornerPoints[nextIndex];
            Vector3 nextNextVerticePoint = alteredCornerPoints[nextNextIndex];

            Vector3 halfway = Vector3.Lerp(verticePoint, nextVerticePoint, 0.5f);
            Vector3 nextHalfway = Vector3.Lerp(nextVerticePoint, nextNextVerticePoint, 0.5f);

            List<Vector3> thisEdgePoints = new List<Vector3>();

            Vector3 cp0 = nextVerticePoint + dirToPrev * cornerSize * 0.5f;
            Vector3 cp1 = nextVerticePoint + dirToNextNext * cornerSize * 0.5f;
            //thisEdgePoints.Add(verticePoint);//p0

            // if (ringPoints.Count == 0)
            {
                thisEdgePoints.Add(halfway);//p0
                                            //  Debug.DrawLine(halfway, halfway + Vector3.up * 4, Color.white);
            }

            thisEdgePoints.Add(cp0);//
            thisEdgePoints.Add(cp1);//
                                    //thisEdgePoints.Add(nextVerticePoint);

            // Debug.DrawLine(nextVerticePoint, nextVerticePoint + Vector3.up * 2, Color.red);
            // Debug.DrawLine(cp0, cp0+ Vector3.up * 2, Color.magenta);
            // Debug.DrawLine(cp1, cp1+ Vector3.up * 2, Color.blue);
            /*
            GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
            c.transform.position = verticePoint;
            c.transform.localScale *= 0.5f;
            c.name = "vp";

            c = GameObject.CreatePrimitive(PrimitiveType.Cube);
            c.transform.position = startPoint;
            c.transform.localScale *= 0.5f;
            c.name = "sp";

            c = GameObject.CreatePrimitive(PrimitiveType.Cube);
            c.transform.position = endPoint;
            c.transform.localScale *= 0.5f;
            c.name = "ep";
            */
            //add to main list
            ringPoints.Add(thisEdgePoints);
        }

        //BezierSpline spline = gameObject.AddComponent<BezierSpline>(); //moved public
        List<Vector3> splineList = new List<Vector3>();
        for (int i = 0; i < ringPoints.Count; i++)
        {
            splineList.AddRange(ringPoints[i]);
        }
        spline.points = splineList.ToArray();
        // Debug.Log(splineList.Count);
        //add mod points?
        List<Vector3> curveReturn = new List<Vector3>();
        float stepSize = 1f / (curveAccuracyCorners);
        for (float i = 0; i < curveAccuracyCorners; i += stepSize)
        {
            Vector3 p = spline.GetPoint(i * stepSize);
            curveReturn.Add(p);

            /*
            GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
            c.transform.position = p;
            c.transform.localScale *= 0.5f;
            c.name = "p";
            */
        }
        //now make a loop
        curveReturn.Add(spline.GetPoint(0));

        Vector3 lastPlaced = curveReturn[0];
        List<Vector3> spacedPoints = new List<Vector3>();
        float adjuster = 0f;
        for (int x = 0; x < 1000; x++)
        {
            //add first
            spacedPoints.Add(curveReturn[0]);
            // Debug.DrawLine(curveReturn[0], curveReturn[0] + Vector3.up * 4, Color.cyan);
            for (int i = 0; i < curveReturn.Count; i++)
            {
                int nextIndex = i + 1;
                if (nextIndex > curveReturn.Count - 1)
                    nextIndex = 0;

                //Debug.DrawLine(curveReturn[i], curveReturn[nextIndex]);
                // Debug.DrawLine(curveReturn[i], curveReturn[i] + Vector3.up);

                /*
                GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                c.transform.position = curveReturn[i];
                c.transform.localScale *= 0.5f;
                c.name = "p";
                */
                //place a point at every windowX
                if (Vector3.Distance(lastPlaced, curveReturn[i]) >= windowX + adjuster)
                {
                    spacedPoints.Add(curveReturn[i]);
                    //Debug.DrawLine(curveReturn[i], curveReturn[i] + Vector3.up*2,Color.cyan);
                    lastPlaced = curveReturn[i];
                }
            }

            float distanceToFirstFromLast = Vector3.Distance(spacedPoints[0], spacedPoints[spacedPoints.Count - 1]);

            if (distanceToFirstFromLast < 1f / curveAccuracyCorners) //is this a good variable to use?
            {
                // Debug.Log("attempts = " + x.ToString());
                break;
            }
            else
            {
                adjuster += 1f / curveAccuracyCorners;
                lastPlaced = curveReturn[0];
                spacedPoints.Clear();
            }
        }


        bool makeSidesEquidistant = false;
        if (makeSidesEquidistant)
        {

           

            //clamp distance, makes a cirlce

            //find the shortest side
            float distance = Mathf.Infinity;
            for (int i = 0; i < spacedPoints.Count; i++)
            {
                if (i == 0)
                    continue;

                float d = Vector3.Distance(centroid, spacedPoints[i]);
                if (d < distance)
                {
                    distance = d;
                }
            }

            //now clamp all distances by this found smallest size
            for (int i = 0; i < spacedPoints.Count; i++)
            {
                if (i == 0)
                    continue;

                Vector3 d = (spacedPoints[i] - centroid).normalized;
                Vector3 p = centroid + d * distance;
                verticeList.Add(p);

               
            }
            spacedPoints = new List<Vector3>(verticeList);
        }

        //make local to cell

        for (int i = 0; i < spacedPoints.Count; i++)
        {
            spacedPoints[i] -= transform.position;

        }
        for (int i = 0; i < spacedPoints.Count - 1; i++)
        {

            

           // Vector3 d = Quaternion.Euler(0, 90, 0) * ((spacedPoints[i] - spacedPoints[i + 1]).normalized);
            //Debug.DrawLine(spacedPoints[i] + transform.position, spacedPoints[i] + d + transform.position, Color.red);


        }

        //how many windows all floors need cut in to
        baseWindowAmount = spacedPoints.Count;
        baseWindowPoints = spacedPoints;

        //now scale these
        if (doSpin)
        {
            ScaleForSpin();
            //then scale actual cell (just for visual on where road woudl be atm                
            //Scale();
        }
      

    }

    public List<Vector3> SkyscraperRingStretched4(out List<List<int>> trianglesForWindows, float height, float windowX, int layerType, int floorsSoFar)
    {
        //create skyscraper from a voronoi cell, attach this script to cell
        //Vector3[] cellVertices = gameObject.GetComponent<MeshFilter>().mesh.vertices;
        List<Vector3> ringPoints = new List<Vector3>();
        List<Vector3> alteredCornerPoints = new List<Vector3>();

        /*uniform scale unfinshed
        for (int i = 0; i < baseWindowPoints.Count; i++)
        {
            Vector3 p = baseWindowPoints[i] + (height * Vector3.up);

            //scale compared to splines
            ringPoints.Add(p);
        }
        */

        //poplate ring points
        for (int i = 0; i < baseWindowPoints.Count-1; i++)
        {

            //run around perimeter
            //create looped indexes
            int thisIndex = i;
            int nextIndex = i + 1;
            if (nextIndex > baseWindowPoints.Count - 1)
                nextIndex = 1;

            Vector3 thisPoint = baseWindowPoints[thisIndex] + Vector3.up * height;
            Vector3 nextPoint = baseWindowPoints[nextIndex] + Vector3.up * height;

            Vector3 originalThis = thisPoint;
            Vector3 originalNext = nextPoint;




            BezierSpline thisSpline = splines[thisIndex];
            //if (x == 1)
            //     thisSpline = splines[nextIndex];

            float step = 1f / curveAccuracyHeight;
            Vector3 p = Vector3.zero;
            //float startAt = (totalHeight/floorHeight)* floorsSoFar;//optimisation idea
            for (float j = 0; j < curveAccuracyHeight; j += step)
            {
                //step through curve until we find the height passed
                p = thisSpline.GetPoint(j);

                // Debug.DrawLine(p, p + Vector3.right, Color.blue);

                if (p.y >= height)
                {
                    Debug.DrawLine(p, p + Vector3.up, Color.green);
                    //find the difference between the orignal voronoi point's x and z position at this curve point
                    p = new Vector3(p.x, height, p.z);

                    /*
                    GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    c.transform.position = p + transform.position;
                    c.transform.localScale *= 0.5f;
                    Destroy(c, 10f);
                    */
                    ringPoints.Add(p);

                    break;
                }
            }

            // Debug.Break();
        }
           

        

        List<List<int>> windowTrianglesToReturn = new List<List<int>>();

        windowTrianglesToReturn.Add(new List<int>());
        windowTrianglesToReturn.Add(new List<int>());
        windowTrianglesToReturn.Add(new List<int>());

        List<int> material0 = new List<int>();
        List<int> material1 = new List<int>();
        List<int> material2 = new List<int>();


        for (int j = 0; j < ringPoints.Count; j++)
        {
            //Debug.DrawLine(ringPoints[i][j], ringPoints[i][j + 1]);

            //finalRingVertices.Add(ringPoints[i][j]);

            //add vertice number to a material list. this will later be set in the mesh

            //this is some maths which figures out to leave a window pain every third vertice

            //  if ((j + 2) % 3 == 0)
            material2.Add(j);//.Count - 1);
            // else
            //     material0.Add(finalRingVertices.Count - 1);

            /*
            GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
            c.transform.position = ringPoints[j];
            c.transform.localScale *= 0.1f;
            c.name = (j).ToString();
            */
        }


        //now sort materials based on layer/ring type
        if (layerType == 0)
        {
            //all solid, all to first material

            windowTrianglesToReturn[0].AddRange(material0);
            windowTrianglesToReturn[0].AddRange(material1);
            windowTrianglesToReturn[0].AddRange(material2);

        }
        if (layerType == 1)
        {
            //all spacer, to secondary material
            windowTrianglesToReturn[1].AddRange(material0);
            windowTrianglesToReturn[1].AddRange(material1);
            windowTrianglesToReturn[1].AddRange(material2);

        }
        if (layerType == 2)
        {

            //with windows and main colour
            windowTrianglesToReturn[0].AddRange(material0);
            windowTrianglesToReturn[0].AddRange(material1);
            windowTrianglesToReturn[2].AddRange(material2);

        }


        trianglesForWindows = windowTrianglesToReturn;
        return ringPoints;
    }

    void AddSplines()
    {
        splines = new List<BezierSpline>();
        //add a curve for each point on the edge of the cell, the corners
        Mesh mesh = GetComponent<MeshFilter>().mesh;
        Vector3[] vertices = mesh.vertices;

        //the number here definethe shape of the building
        //if we compute the random numbers here we get a more uniform shape        
        float random1 = Random.Range(0.5f, 1f);
        float random2 = Random.Range(0.2f, 1f);
        float random3 = Random.Range(0.0f, 0f);

        float heightRandom1 = Random.Range(0.0f, 0.5f);
        float heightRandom2 = Random.Range(0.5f, 1f);

        //choose a side to sculpt with more random detail
        int randomSide = Random.Range(0, vertices.Length);
        bool allRandom = Random.value >= 0.5f;
        //make staright side type common
        bool straight =  Random.value >= 0.5f;
        //Debug.Log(allRandom);

        for (int i = 1; i < vertices.Length; i++)
        {
           // if (i == 0)
                //skip central vertice- add as a modifier later?
              //  continue;

            BezierSpline splineL = gameObject.AddComponent<BezierSpline>();

            if (straight)
            {
                random1 = 1f;
                random2 = 1f;
                random3 = 1f;
            }
            //if we compute the random numbers here we get a more erratic shape
            else if (i == randomSide || allRandom)
            {
                random1 = Random.Range(0.5f, 1f);
                random2 = Random.Range(0.2f, 1f);
                random3 = Random.Range(0f, 1f);
            }
            
            Vector3 p0 = vertices[i];

            Vector3 p1 = vertices[i] + Vector3.up * totalHeight * heightRandom1;
            p1 = Vector3.Lerp(vertices[0] + Vector3.up * totalHeight, p1,random1 );

            Vector3 p2 = vertices[i] + Vector3.up * totalHeight * heightRandom2;
            p2 = Vector3.Lerp(vertices[0] + Vector3.up * totalHeight, p2, random2);

            Vector3 p3 = vertices[i] + Vector3.up * totalHeight;

            GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
            c.transform.position = Vector3.up * totalHeight + transform.position;

           // p3 = Vector3.Lerp(vertices[0] + Vector3.up * totalHeight, p3, random3); //don't change total height
            List<Vector3> splinePoints = new List<Vector3> { p0, p1, p2, p3 };

            //remove transform position, we will work out positions in local/ mesh space

            for (int a = 0; a < splinePoints.Count; a++)
            {
               // splinePoints[a] -= transform.position;
            }

            splineL.points = splinePoints.ToArray();

            splines.Add(splineL);

          
        }
        
      
    }

    void AddSplinesForUniformPoints()
    {
        List<Vector3> generalShapePoints = new List<Vector3>();
        float step = 1f / curveAccuracyHeight;
        List<float> randoms = new List<float>();
        for (int i = 0; i <= 3; i++)
        {
            float r = Random.Range(0.1f, 1f);
            r = 1f;
            r *= 1f / (i + 1);

            //if spinning we need to clamp, limitations in design of algorithm, clamps to max safe distance which we can spin by so building doesn't hang over road
            if(doSpin)
                r = 1f;

            Vector3 v3 = Vector3.right * r + Vector3.up *( (totalHeight / 3) * i);
            generalShapePoints.Add(v3 - transform.position);

           // Debug.Log(r);            
        }


        BezierSpline generalSpline = gameObject.AddComponent<BezierSpline>();
        generalSpline.points = generalShapePoints.ToArray();
        float generalStep = (1f / 3);

        for (float j = 0; j <3; j += generalStep)
        {
            //step through curve until we find the height passed
            Vector3 p0 = generalSpline.GetPoint(j);
            Vector3 p1 = generalSpline.GetPoint(j + generalStep);

            Debug.DrawLine(p0 + transform.position, p1 + transform.position, Color.red);
        }
      

        //then run through this general shape curve and get widths at 1/segment amount
        int segments = 10;//more for multi spin but then stepped problems happen, we can clamp general shape at 1 if we want to do multi spin (looks like a flump)
        float stepForGeneral = 1f / (segments);
        randoms = new List<float>();
        for (float i = 0; i <= segments; i+=stepForGeneral)
        {
            Vector3 p = generalSpline.GetPoint(i);
            Debug.DrawLine(p + transform.position, p + (Vector3.right * 10) + transform.position, Color.magenta);
            p.y = 0;
           // Debug.Log(p.magnitude);
            randoms.Add(p.magnitude);
            
        }

        splines = new List<BezierSpline>();

        List<List<Vector3>> mainPoints = new List<List<Vector3>>();
        
        int spin = 0;
        if (!doSpin)
            spinPlus = 0;

        for (int i = 0; i < baseWindowPoints.Count; i++)
        {
            int prevIndexI = i - 1;
            int nextIndexI = i + 1;
            int nextNextIndexI = i + 2;
            if (prevIndexI < 0)
                prevIndexI += baseWindowPoints.Count;
            if (nextIndexI >= baseWindowPoints.Count)
                nextIndexI -= baseWindowPoints.Count;
            if (nextNextIndexI >= baseWindowPoints.Count)
                nextNextIndexI -= baseWindowPoints.Count;

            //GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
            //c.transform.position = baseWindowPoints[i];

            List<Vector3> splinePoints = new List<Vector3>();
            float segmentHeight = (totalHeight / segments);
            for (int x = 0; x < segments; x++)//start at - 1 , because we are doung this (to make start curve smooth), we need to also spin the vertices back by a spinPlus
            {
                //int prevIndexX = x - 1;
                int nextIndexX = x + 1;
                int nextNextIndexX = x + 2;
                //if (prevIndexX < 0)
                  //  prevIndexX += segments;
                //if (nextIndexX >= segments)
                  //  nextIndexX -= segments;
                
                float thisSegmentHeight = segmentHeight * x - segmentHeight*0.5f;
                //float prevSegmentStart = segmentHeight * prevIndexX; //can be -1
                float nextSegmentHeight = segmentHeight * nextIndexX - segmentHeight * 0.5f;
                float nextNextSegmentHeight = segmentHeight * nextNextIndexX - segmentHeight * 0.5f;
                //because we start underneath, randoms doesn't start underneath
                int indexForP = x;
                if (indexForP < 0)
                    indexForP = 0;

                Vector3 p = (Quaternion.Euler(0, spin - spinPlus, 0) * baseWindowPoints[i]*randoms[indexForP]) + thisSegmentHeight*Vector3.up;
                int spin0 = spin + spinPlus;
               // if (x <= 0)
               //     spin0 = 0;
               //get direction from this vector spun, get length from next - is this even making a difference? points are all close to each other anyway
                Vector3 pNext = Quaternion.Euler(0, spin0-spinPlus, 0) * (baseWindowPoints[i] * randoms[indexForP]) + nextSegmentHeight * Vector3.up;
                int spin1 = spin + spinPlus + spinPlus;
               // if (x <= 0)
               //     spin1 = 0;
                Vector3 pNextNext = Quaternion.Euler(0, spin1-spinPlus, 0) * (baseWindowPoints[i] * randoms[indexForP]) + nextNextSegmentHeight * Vector3.up;
                Vector3 halfWay = Vector3.Lerp(p, pNext, 0.5f);
                //Vector3 cp0 = Vector3.Lerp(p, pNext, 0.25f);         
                float cpAdjust = 0.33f;//doesnt seeem to change things too much when changing this
                Vector3 cp0 = Vector3.Lerp(halfWay, pNext,1f - cpAdjust);
                Vector3 nextHalfway = Vector3.Lerp(pNext, pNextNext, 0.5f);
                Vector3 cp1 = Vector3.Lerp(pNext, nextHalfway, cpAdjust);

                

                //add this direction to main point
                if (i == 0 && x > 0)
                {
                    //Debug.DrawLine(transform.position + halfWay, splinePoints[splinePoints.Count-1]+ transform.position, Color.red);
                    //Debug.DrawLine(transform.position + halfWay, cp0+ transform.position, Color.green);
                    //Debug.DrawLine(transform.position + p, pNext + transform.position, Color.cyan);
                    //  Debug.DrawLine(transform.position + p, transform.position + cp0, Color.green);
                    //  Debug.DrawLine(transform.position + pNext, transform.position + cp1, Color.red);
                    //Debug.DrawLine(transform.position + p, transform.position + cp1, Color.blue);
                }

                
                splinePoints.Add(halfWay);
                splinePoints.Add(cp0);
                splinePoints.Add(cp1);
                //if(i==0)
                if (i == 0)
                {
                    /*
                     GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                     c.transform.position = halfWay + transform.position;

                    c = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    c.transform.position = cp0 + transform.position;

                    c = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    c.transform.position = cp1+ transform.position;
                    */
                }
                    
               //if(x > 0)
                    spin += spinPlus;
            }
            //add final bookend 
            Vector3 pLast = (Quaternion.Euler(0, spin, 0) * (baseWindowPoints[i] * randoms[segments]) + segmentHeight*(segments) * Vector3.up);
            //Debug.DrawLine((segments*segmentHeight) * Vector3.up + transform.position, pLast + transform.position,Color.blue);

            splinePoints.Add(pLast);

            spin = 0;
            mainPoints.Add(splinePoints);
        }

        //add to spline
        for (int i = 0; i < mainPoints.Count; i++)
        {
            BezierSpline splineL = gameObject.AddComponent<BezierSpline>();
            List<Vector3> splinePoints = new List<Vector3>();
            for (int j = 0; j < mainPoints[i].Count ; j++)
            {
                //we need to remove transform position again because I think the bezier class automaicallly adds it when working out point. Should research this
                splinePoints.Add(mainPoints[i][j] - transform.position);//local         
               
            }
            splineL.points = splinePoints.ToArray();
            splines.Add(splineL);
            
        }
        //float startAt = (totalHeight/floorHeight)* floorsSoFar;//optimisation idea
        BezierSpline thisSpline = splines[0];
        for (float j = 0; j < curveAccuracyHeight; j += step)
        {
            //step through curve until we find the height passed
            Vector3 p0 = thisSpline.GetPoint(j);
            Vector3 p1 = thisSpline.GetPoint(j+step); 

            Debug.DrawLine(p0 +transform.position, p1 + transform.position, Color.blue);
        }
    }

    void Scale()
    {
      //  Debug.Log("Scale");
        Mesh mesh = GetComponent<MeshFilter>().mesh;
        Vector3[] verts = mesh.vertices;

        //find longest arm
        for (int i = 0; i < verts.Length; i++)
        {
            Vector3 p = Vector3.Lerp(verts[0], verts[i], scale);
            verts[i] = p;
        }

        mesh.vertices = verts;
    }

    void ScaleForSpin()
    {
        
        //if we want to spin the building, we need to make sure we dont spin it outwith it's cell limits in to other buildings
        //to do this we must scale the longest side so it matches the shortest side's length, then scale the rest accordingly
        //we have already re aligned cell, so each point's magnitude is it's length from the centroid
        float longestLength = 0f;
        float shortestLength = Mathf.Infinity;


        for (int i = 0; i < baseWindowPoints.Count; i++)
        {
            //skip middle vertice
            if (i == 0)
                continue;

            if (baseWindowPoints[i].magnitude > longestLength)
                longestLength = baseWindowPoints[i].magnitude;

            if (baseWindowPoints[i].magnitude < shortestLength)
                shortestLength = baseWindowPoints[i].magnitude;
        }

        //find ratio
        float d = longestLength / shortestLength;
        //now scale all arms by this amount

        //find longest arm
        for (int i = 0; i < baseWindowPoints.Count; i++)
        {
            Vector3 p = baseWindowPoints[i] / d;
            baseWindowPoints[i] = p;
        }



        //mesh.vertices = verts;
    }

    void LoadMaterials()
    {
        Material mat0 = Resources.Load("Building") as Material;
        Material mat1 = Resources.Load("Ground") as Material;
        Material mat2 = Resources.Load("Glass") as Material;

        materials = new Material[3] { mat0, mat1, mat2 };       
    }

    void Height()
    {
        //work out height depending on closeness to centre
        float distance = transform.position.magnitude;//Vector3.Distance(transform.position, Vector3.zero);// 

        float xSizeOfCity =  GameObject.Find("Layout").GetComponent<MeshGenerator>().volume.x/2;

        totalHeight = xSizeOfCity/ distance;
        totalHeight = inExp( xSizeOfCity / totalHeight);
        totalHeight *= xSizeOfCity*.2f;
        totalHeight += 30;//min

        //make less linear
        totalHeight *= Random.Range(.5f, 1f);
    }

    float inExp(float x)
    {
        if (x < 0.0f) return 0.0f;
        if (x > 1.0f) return 1.0f;
        return Mathf.Pow(2.0f, 10.0f * (x - 1.0f));
    }

    void BaseLengths(List<List<Vector3>> ringPoints,List<Vector3> alteredCornerPoints, BezierSpline bezier)
    {
        for (int q = 0; q < ringPoints.Count; q++)
        {

            //populate curve list, it holds how many windows we will put on each level at each curve/corner
            int thisIndexBase = q;
            int nextIndexBase = q + 1;
            int verticeCellBase = q + 2;
            //covers last index and loops back to first
            if (nextIndexBase > ringPoints.Count - 1)
            {
                nextIndexBase = 0;
                verticeCellBase = 1;
            }

            //get last point from this list and first from next
            Vector3 lastPointBase = ringPoints[q][ringPoints[q].Count - 1];
            Vector3 firstFromNextBase = ringPoints[nextIndexBase][0];
            int indexForCellVerticeBase = q + 1;
            if (indexForCellVerticeBase == alteredCornerPoints.Count)
                indexForCellVerticeBase = 0;
            Vector3 nearestCellVerticeBase = alteredCornerPoints[indexForCellVerticeBase];
            //control points of curve
            //dont normalise, we will use curve control point variable to use a percentage of this size, however .33f looks nice
            Vector3 dirToPrevBase = (lastPointBase - nearestCellVerticeBase).normalized*cornerSize;
            Vector3 cp0Base = nearestCellVerticeBase + dirToPrevBase * curveControlPointSize;

            Vector3 dirToNextBase = (firstFromNextBase - nearestCellVerticeBase).normalized * cornerSize;
            Vector3 cp1Base = nearestCellVerticeBase + dirToNextBase * curveControlPointSize;

            Debug.DrawLine(nearestCellVerticeBase, cp0Base, Color.red);
            Debug.DrawLine(nearestCellVerticeBase, cp1Base, Color.green);

            //add bookending curve points with control points between them
            Vector3[] bezPointsBase = new Vector3[4] { lastPointBase, cp0Base, cp1Base, firstFromNextBase };
            bezier.points = bezPointsBase;

            float stepForBase = 1f / curveAccuracyCorners;

            Vector3 lastStepPointBase = bezier.GetPoint(0 * stepForBase);
            List<Vector3> cornerPointsBase = new List<Vector3>();

            float distanceOfCurve = 0f;
            //possible we need to run from middle of curve out to both sides like we did above with flat sides so it is symmetrical?
            for (float k = 0; k <= curveAccuracyCorners; k += stepForBase)
            {
                Vector3 p0 = bezier.GetPoint(k * stepForBase);
                //Vector3 d = Quaternion.Euler(0, 90, 0) * bezier.GetDirection(k) * stepForBase;

                distanceOfCurve += Vector3.Distance(lastStepPointBase, p0);
                lastStepPointBase = p0;
                //Debug.DrawLine(p0, p0 + Vector3.up, Color.cyan);
            }

            //now we have total distance try and and split up window to target size (it won't be perfect!)

            cornerLengths.Add(distanceOfCurve);
        }
    }

    float SmallestEdgeSizeList(List<Vector3> alteredPoints)
    {
        float smallestDistance = Mathf.Infinity;
        for (int i = 0; i < alteredPoints.Count- 1; i++)
        {
            int thisIndex = i;
            int nextIndex = i + 1;
            if (nextIndex > alteredPoints.Count - 1)
            {
                nextIndex = 0;
            }

            float tempD = Vector3.Distance(alteredPoints[thisIndex], alteredPoints[nextIndex]);
            if (tempD < smallestDistance)
                smallestDistance = tempD;
        }
        

        return smallestDistance;
    }

    float SmallestEdgeSize(Vector3[] cellVertices)
    {
        float smallestDistance = Mathf.Infinity;
        for (int i = 1; i < cellVertices.Length ; i++)
        {
            int thisIndex = i;
            int nextIndex = i + 1;
            if (nextIndex > cellVertices.Length - 1)
            {
                nextIndex = 1;
            }

            float tempD = Vector3.Distance(cellVertices[thisIndex], cellVertices[nextIndex]);
            if (tempD < smallestDistance)
                smallestDistance = tempD;
        }


        return smallestDistance;
    }

    public static Vector2 GetCentroid(List<Vector2> poly)
    {
        //https://stackoverflow.com/questions/9815699/how-to-calculate-centroid - converted to vector2 by me

        float accumulatedArea = 0.0f;
        float centerX = 0.0f;
        float centerY = 0.0f;

        for (int i = 0, j = poly.Count - 1; i < poly.Count; j = i++)
        {
            float temp = poly[i].x * poly[j].y - poly[j].x * poly[i].y;
            accumulatedArea += temp;
            centerX += (poly[i].x + poly[j].x) * temp;
            centerY += (poly[i].y + poly[j].y) * temp;
        }

        if (Mathf.Abs(accumulatedArea) < 1E-7f)
            return Vector2.zero;  // Avoid division by zero

        accumulatedArea *= 3f;
        return new Vector2(centerX / accumulatedArea, centerY / accumulatedArea);
    }

}


