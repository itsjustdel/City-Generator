﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class InteriorWalls : MonoBehaviour
{
    public bool reset = false;
    public bool resetDoor = false;
    //  public List<Vector3> doorEdges;
    public List<Vector3> apartmentDoorPositions;
    public List<Vector3> exteriors; 
    public List<Vector3> tempList;
    public List<Vector3> originalPositions;
    public float roomWallThickness = .2f;
    public float exteriorWallThickness = .4f;//pass down from somewhere
    public TraditionalSkyscraper tS;
    public List<Vector3[]> doorEdges = new List<Vector3[]>();
    public List<Vector3> doorCentres = new List<Vector3>();//parallel list to dooredges

    public bool apartmentDoorBuilt = false;
    float floorHeight;
    float doorHeight;
    Material m0;
    Material m1;
    public bool apartmentDoorOnly =  false;
    // Start is called before the first frame update
    // public bool apartment = false;


    private void Awake()
    {
        //enabled = false;
    }
    private void Update()
    {
        if(reset)
        {
            Start();
            reset = false;
        }

        if(resetDoor)
        {
            Bookend();

        }
    }

    void Start()
    {

        doorCentres.Clear();
        doorEdges.Clear();
            

        floorHeight = tS.floorHeight * (1f - tS.spacerHeight * 2);
        doorHeight = floorHeight * .66f;

        //material
        PaletteInfo pI = tS.GetComponent<PaletteInfo>();

        if (pI.palette != null)//protexts for hot loading
        {
            if (pI.palette.Count > 0)
            {
                //m = pI.palette[0].tints[0];
                //match material of story
                //pI.palette[]
                m0 = tS.materials[0];
                m1 = tS.materials[1];
            }
        }
        else
        {
            m0 = Resources.Load("Grey") as Material;
            m1 = Resources.Load("Grey") as Material;
        }

        // tempList = new List<Vector3>(doorEdges);

        //flatten, door edges ahve some 1 heights in them, used for wokring out floorplans
        //  for (int i = 0; i < tempList.Count; i++)
        {
            //tempList[i] = new Vector3(tempList[i].x, 0f, tempList[i].z);
        }

        //apartment doors are built where a hall meets a hall - what do we have for this?

        //room doors are built on edges that run along a hall - we have this with door edges atm
        if (!apartmentDoorOnly)
            RoomDoorAndWalls();
        else
            ApartmentDoor();

    }

    void ApartmentDoor()
    {

        List<Vector3> ringCornerPoints = new List<Vector3>();//interior side of exterior
        for (int i = 0; i < originalPositions.Count; i++)
        {
            int prevPrev = i - 2;
            if (prevPrev < 0)
                prevPrev += originalPositions.Count;

            int prev = i - 1;
            if (prev < 0)
                prev += originalPositions.Count;
            int next = i + 1;
            if (next > originalPositions.Count - 1)
                next -= originalPositions.Count;

            int nextNext = i + 2;
            if (nextNext > originalPositions.Count - 1)
                nextNext -= originalPositions.Count;

            Vector3 y0PrevPrev = new Vector3(tempList[prevPrev].x, 0f, tempList[prevPrev].z);
            Vector3 y0This = new Vector3(tempList[i].x, 0f, tempList[i].z);
            Vector3 y0Prev = new Vector3(tempList[prev].x, 0f, tempList[prev].z);
            Vector3 y0Next = new Vector3(tempList[next].x, 0f, tempList[next].z);
            Vector3 y0NextNext = new Vector3(tempList[nextNext].x, 0f, tempList[nextNext].z);
          
           

            //templist is passeed flattened, but we can use original position to determine if it is a hall point or not ( > 0f)
            if (originalPositions[prev].y == 0f && originalPositions[i].y == 0f && originalPositions[next].y > 0f)//originalPositions[prev].y == 0f &&  //some rooms only have two door points, cant check prev
            {
                // Debug.DrawLine(tempList[i], tempList[next],Color.blue);//what intersect am i going for?
                // Debug.DrawLine(p0, p1,Color.magenta);
                Vector3 miterThis = Hallways.MiterDirection(y0Prev, y0This, y0Next, exteriorWallThickness);//note, outside wall thickness          
                Vector3 p0 = tempList[i] - miterThis;
                Vector3 edgeDir = (tempList[i] - tempList[prev]).normalized;
                Vector3 p1 = p0 + edgeDir * 100;

                //find intersect 
                Vector3 a0v2 = new Vector2(tempList[i].x, tempList[i].z);
                Vector3 a1v2 = new Vector2(tempList[next].x, tempList[next].z);
                Vector2 p0v2 = new Vector2(p0.x, p0.z);
                Vector2 p1v2 = new Vector2(p1.x, p1.z);

                Vector2 intersectV2 = Vector2.zero;
                if (Hallways.LineSegmentsIntersection(a0v2, a1v2, p0v2, p1v2, out intersectV2))
                {
                    //door stuff, check at end of function if we have enough points to make a door
                    Vector3 intersectV3 = new Vector3(intersectV2.x, 0f, intersectV2.y);
                    doorEdges.Add(new Vector3[] {  intersectV3, tempList[i] });
                    doorCentres.Add(originalPositions[i]);

                    //  GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    //  c.name = "original 0";
                    //  c.transform.position = originalPositions[i];
                }
                else
                    Debug.Log("no intersect for apartment wall");
                
            }
            else if (originalPositions[prev].y > 0f && originalPositions[i].y == 0f && originalPositions[next].y == 0f)
            {

                //Debug.DrawLine(tempList[prev], tempList[i]);
                //Debug.DrawLine(tempList[next], tempList[i]);

                Vector3 miterThis = Hallways.MiterDirection(y0Prev, y0This, y0Next, exteriorWallThickness);//note, outside wall thickness          

                Vector3 p0 = originalPositions[i] - miterThis;
                Vector3 edgeDir = (tempList[next] - tempList[i]).normalized;//**
                Vector3 p1 = p0 + edgeDir * 100;

                //find intersect 
                Vector3 a0v2 = new Vector2(tempList[prev].x, tempList[prev].z);
                Vector3 a1v2 = new Vector2(tempList[i].x, tempList[i].z);
                Vector2 p0v2 = new Vector2(p0.x, p0.z);
                Vector2 p1v2 = new Vector2(p1.x, p1.z);

                //  Debug.DrawLine(tempList[prev], tempList[i]);
                //  Debug.DrawLine(p0, p1);

                Vector2 intersectV2;
                if (Hallways.LineSegmentsIntersection(a0v2, a1v2, p0v2, p1v2, out intersectV2))
                {
                    //door stuff
                    Vector3 intersectV3 = new Vector3(intersectV2.x, 0f, intersectV2.y);
                    doorEdges.Add(new Vector3[] {  intersectV3, tempList[i] });
                    doorCentres.Add(originalPositions[i]);

                    //   GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    //   c.name = "orign 1";
                //    c.transform.position = originalPositions[i];

                }                
            }          

           
        }



        Bookend();
    }


    void RoomDoorAndWalls()
    {

       // for (int i = 0; i < exteriors.Count; i++)
        {
         //   GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
        //    c.transform.position = exteriors[i];
        //    c.name = "ex";
        }

        //edges are a list of vector3s, choose an edge ( longer, shorter)
        float doorSize = transform.parent.GetComponentInParent<Interiors>().doorWidth * 2;
        bool doorBuilt = true;

        List<Vector3> outerWallPoints = new List<Vector3>();
        //we need to know where the first point came from
        Vector3 firstWallPointPrev = Vector3.zero;
        Vector3 firstWallPoint = Vector3.zero;
        Vector3 lastWallPoint = Vector3.zero;
        //and where the last point goes to
        Vector3 lastWallPointNext = Vector3.zero;

        //and some intersects so we dont need to do maths twice
        Vector3 intersectStart = Vector3.zero;
        Vector3 intersectEnd = Vector3.zero;

        for (int i = 0; i < tempList.Count; i++)
        {
            int prevPrev = i - 2;
            if (prevPrev < 0)
                prevPrev += tempList.Count;

            int prev = i - 1;
            if (prev < 0)
                prev += tempList.Count;
            int next = i + 1;
            if (next > tempList.Count - 1)
                next -= tempList.Count;

            int nextNext = i + 2;
            if (nextNext > tempList.Count - 1)
                nextNext -= tempList.Count;


            bool containsPrev = exteriors.Contains(tempList[prev]);
            bool containsThis = exteriors.Contains(tempList[i]);
            bool containsNext = exteriors.Contains(tempList[next]);
            // Debug.DrawLine(tempList[prev], tempList[i], Color.blue);
            Vector3 y0PrevPrev = new Vector3(tempList[prevPrev].x, 0f, tempList[prevPrev].z);
            Vector3 y0This = new Vector3(tempList[i].x, 0f, tempList[i].z);
            Vector3 y0Prev = new Vector3(tempList[prev].x, 0f, tempList[prev].z);
            Vector3 y0Next = new Vector3(tempList[next].x, 0f, tempList[next].z);
            Vector3 y0NextNext = new Vector3(tempList[nextNext].x, 0f, tempList[nextNext].z);

            Vector3 miterPrev = Hallways.MiterDirection(y0PrevPrev, y0Prev, y0This, roomWallThickness);
            Vector3 miterThis = Hallways.MiterDirection(y0Prev, y0This, y0Next, roomWallThickness);
            Vector3 miterNext = Hallways.MiterDirection(y0This, y0Next, y0NextNext, roomWallThickness);

            if (tempList[prev].y == 0f && tempList[i].y == 0f && tempList[next].y  > 0f)//tempList[prev].y == 0f &&  //some rooms only have two door points, cant check prev
            {
                if (!containsThis)//other checks?
                {
                    //0 the y
                    Vector3 a0 = new Vector3(tempList[prev].x, 0f, tempList[prev].z);
                    Vector3 a1 = new Vector3(tempList[i].x, 0f, tempList[i].z);
                    Vector3 a2 = new Vector3(tempList[next].x, 0f, tempList[next].z);
                    Vector3 miter0 = Hallways.MiterDirection(a0, a1, tempList[next], roomWallThickness);

                    Vector3 p0 = tempList[i] - miter0;
                    Vector3 edgeDir = (a1 - a0).normalized;
                    Vector3 p1 = p0 + edgeDir * 100;

                    //find intersect 
                    Vector3 a0v2 = new Vector2(a1.x, a1.z);
                    Vector3 a1v2 = new Vector2(a2.x, a2.z);
                    Vector2 p0v2 = new Vector2(p0.x, p0.z);
                    Vector2 p1v2 = new Vector2(p1.x, p1.z);

                    Vector2 intersectV2 = Vector2.zero;

                    //Debug.DrawLine(a0, a1,Color.blue);
                    ///Debug.DrawLine(p0, p1,Color.yellow);

                    if (Hallways.LineSegmentsIntersection(a0v2, a1v2, p0v2, p1v2, out intersectV2))
                    {
                        //door stuff, check at end of function if we have enough points to make a door
                        Vector3 intersectV3 = new Vector3(intersectV2.x, 0f, intersectV2.y);
                        doorEdges.Add(new Vector3[] { intersectV3, tempList[i] });
                        doorCentres.Add(originalPositions[i]);

                    }
                    else
                        Debug.Log("no intersect for apartment wall");
                }
                
                if (!containsPrev && !containsThis && !containsNext)
                {
                        GameObject wall = WallToNext(y0This, y0Next, miterThis, miterNext);
                        wall.name = "Wall 0 0 1";

                }
                else if(containsPrev && !containsThis && !containsNext)
                {
                  //  Debug.Log("not tested");
                    GameObject wall = WallToNext(y0This, y0Next, miterThis, miterNext);                    
                    wall.name = "Wall no ext 0 0 1";
                    
                }

                else if (containsPrev && containsThis && !containsNext)
                {

                    GameObject wall = WallToNextExteriorPrev(out intersectStart, y0Prev, y0This, y0Next, miterNext);
                    wall.name = "Wall prev ext 0 0 1";

                   // GameObject q = GameObject.CreatePrimitive(PrimitiveType.Cube);
                   // q.transform.position = tempList[i];
                  //  q.name = "ext last";

                    lastWallPoint = tempList[i];
                    lastWallPointNext = tempList[next];
                }
              

                
            }
            else if (tempList[prev].y >  0f && tempList[i].y == 0f && tempList[next].y == 0f)
            {
                
                Vector2 intersectV2;
                Vector3 edgeDir;
                Vector3 a0;
                Vector3 a1;
                Vector3 a2;
                if (!containsThis)
                { 
                
                

                    //0 the y
                    a0 = new Vector3(y0Prev.x, 0f, y0Prev.z);//not needed
                    a1 = new Vector3(tempList[i].x, 0f, tempList[i].z);
                    a2 = new Vector3(tempList[next].x, 0f, tempList[next].z);


                    Vector3 p0 = tempList[i] - miterThis;
                    edgeDir = (a1 - a2).normalized;//**
                    Vector3 p1 = p0 + edgeDir * 100;

                    //find intersect 
                    Vector3 a0v2 = new Vector2(a0.x, a0.z);
                    Vector3 a1v2 = new Vector2(a1.x, a1.z);
                    Vector2 p0v2 = new Vector2(p0.x, p0.z);
                    Vector2 p1v2 = new Vector2(p1.x, p1.z);

               

               // Debug.DrawLine(a0, a1);
               // Debug.DrawLine(p0, p1);

                    if (Hallways.LineSegmentsIntersection(a0v2, a1v2, p0v2, p1v2, out intersectV2))
                    {
                        //door stuff
                        Vector3 intersectV3 = new Vector3(intersectV2.x, 0f, intersectV2.y);
                        doorEdges.Add(new Vector3[] { intersectV3, tempList[i] });
                        doorCentres.Add(originalPositions[i]);



                    }

                }
               
                //wall

                if (!containsPrev && !containsThis && containsNext)
                {
                    GameObject wall = WallToNextExteriorNext(out intersectEnd, y0Prev, y0This, y0Next, y0NextNext);
                    wall.name = "Wall next ext 1 0 0";

                  //  GameObject q = GameObject.CreatePrimitive(PrimitiveType.Cube);
                 //   q.transform.position = tempList[next];
                  //  q.name = "ext first";

                    firstWallPoint = tempList[next];
                    firstWallPointPrev = tempList[i];

                }
                else if(!containsPrev && !containsThis && !containsNext)
                {
                    GameObject wall = WallToNext(y0This, y0Next, miterThis, miterNext);
                    wall.name = "Wall a 1 0 0";
                }
                else if(!containsPrev && containsThis & !containsNext)
                {
                    GameObject wall = WallToNext(y0This, y0Next, miterThis, miterNext);
                    wall.name = "Wall b 1 0 0";


                }


               
            }
            else if (tempList[prev].y == 0f && tempList[i].y >  0f && tempList[next].y == 0f)
            {
                if (containsNext)
                {
                    GameObject wall = WallToNextExteriorNext(out intersectEnd, y0Prev, y0This, y0Next, y0NextNext);
                    wall.name = "Wall next ext 0 1 0";


                  //  GameObject q = GameObject.CreatePrimitive(PrimitiveType.Cube);
                 //   q.transform.position = tempList[next];
                 //   q.name = "ext first";

                    firstWallPoint = tempList[next];
                    firstWallPointPrev = tempList[i];

                }
                else
                {
                    GameObject wall = WallToNext(y0This, y0Next, miterThis, miterNext);
                    wall.name = "Wall 0 1 0";
                }
            }
            else if (tempList[prev].y == 0f && tempList[i].y == 0f && tempList[next].y == 0f)
            {

                if (!containsPrev && !containsThis && !containsNext)
                {
                    GameObject wall = WallToNext(y0This, y0Next, miterThis, miterNext);
                    wall.name = "Wall no ext a 0 0 0";
                }

                if (containsPrev && !containsThis && !containsNext)
                {
                    GameObject wall = WallToNext(y0This, y0Next, miterThis, miterNext);
                    wall.name = "Wall no ext b 0 0 0";
                }

                if (containsPrev && containsThis && !containsNext)
                {
                    GameObject wall = WallToNextExteriorPrev(out intersectStart, y0Prev, y0This, y0Next, miterNext);
                    wall.name = "Wall ext prev 0 0 0";

                  //  GameObject q = GameObject.CreatePrimitive(PrimitiveType.Cube);
                  //  q.transform.position = tempList[i];
                 //   q.name = "ext last";

                    lastWallPoint = tempList[i];
                    lastWallPointNext = tempList[next];
                }
                if (!containsPrev && !containsThis && containsNext)
                {
                    GameObject wall = WallToNextExteriorNext(out intersectEnd, y0Prev, y0This, y0Next, y0NextNext);
                    wall.name = "Wall ext next 0 0 0";

                    GameObject q = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    q.transform.position = tempList[i];
                    q.name = "ext first b";

                    firstWallPoint = tempList[i];
                    firstWallPointPrev = tempList[prev];
                }
                else if (containsPrev & containsThis && containsNext)
                {
                    //exterior

                  //  GameObject q = GameObject.CreatePrimitive(PrimitiveType.Cube);
                  //  q.transform.position = tempList[i];
                  //  q.name = "ext";

                    outerWallPoints.Add(tempList[i]);
                    
                }
            }
        }

        //add first and last points to the list - having to do this becasue of the way intersects are found
        outerWallPoints.Insert(0, firstWallPoint);
        outerWallPoints.Insert(0, firstWallPointPrev);

        outerWallPoints.Insert(outerWallPoints.Count, lastWallPoint);
        outerWallPoints.Insert(outerWallPoints.Count, lastWallPointNext);

        //remove any duplicates
        outerWallPoints = outerWallPoints.Distinct().ToList();

        ExteriorWallInside(outerWallPoints, intersectStart, intersectEnd);

        for (int i = 0; i < outerWallPoints.Count; i++)
        {
         //   GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
          //  c.transform.position= outerWallPoints[i];
         //   c.name = "ordered p";
        }


        if (doorBuilt == false)
        {
            Debug.Log("No wall found for door");
        }

        //check for door
        Bookend();
    }

    void ExteriorWallInside(List<Vector3> outerWallPoints,Vector3 startIntersect,Vector3 endIntersect)
    {

        List<Vector3> points = new List<Vector3>();        
        Vector3 miter = Vector3.zero;

        //make list of miter poitns at wall width
        points.Add(endIntersect);
        for (int i = 1; i < outerWallPoints.Count-1; i++)//start at 1 and end at -1 because we just need those points to work out miters
        {
            miter =outerWallPoints[i] - Hallways.MiterDirection(outerWallPoints[i - 1], outerWallPoints[i], outerWallPoints[i + 1],exteriorWallThickness);
            points.Add(miter);

        }     
        points.Add(startIntersect);

        float minWindowSpace = (tS.windowSpaceX * 2) + tS.windowX;
        //figure out where we can place windows

        List<Vector3> pointsWithWindows = new List<Vector3>();
        //we will remember where we placed windows on each row

        List<int> windowIndexes = new List<int>();

        for (int i = 0; i < points.Count-1; i++)
        {
            //GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
            //c.transform.position = points[i];
            // c.name = "inside";

            float distanceToNext = Vector3.Distance(points[i], points[i + 1]);
            Vector3 dirToNext = (points[i+1] - points[i]).normalized;
            Vector3 halfPoint = Vector3.Lerp(points[i], points[i + 1], 0.5f);
            GameObject c0 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            c0.transform.position = halfPoint;
            c0.name = "half";
            c0.transform.localScale *= 0.5f;
            if (distanceToNext > minWindowSpace)
            {
                //first of all,. add our first point?
                //pointsWithWindows.Add(points[i]);

                //find out how many windows we can fit in
                int windowsTotal = (int)( distanceToNext / minWindowSpace);
                float step = distanceToNext / windowsTotal;
                Debug.Log(windowsTotal);

                List<List<List<Vector3>>> tempsTempsTemps = new List<List<List<Vector3>>>();
                for (int k = 0; k < 2; k++)
                {
                    Vector3 dirToUse = dirToNext;
                    if (k == 1)
                        dirToUse = -dirToNext;

                    List<List<Vector3>> tempsTemps = new List<List<Vector3>>();
                    for (float j = 0; j < (distanceToNext * .5f) - minWindowSpace; j += minWindowSpace)
                    {
                        if (k == 1 && j == 0)
                            continue;//will be a doubler

                        //build frame half way across window X size
                        Vector3 windowCentre = halfPoint + dirToUse * j;
                        //frame
                        Vector3 framEdgeWindowSide0 = windowCentre + (dirToNext * tS.windowX * 0.5f);
                        Vector3 framEdgeWindowSide1 = windowCentre - (dirToNext * tS.windowX * 0.5f);
                        //add frame width
                        Vector3 frameEdgeNextFrameSide0 = framEdgeWindowSide0 + (dirToNext * tS.windowSpaceX);
                        Vector3 frameEdgeNextFrameSide1 = framEdgeWindowSide1 - (dirToNext * tS.windowSpaceX);

                        //now build frame
                        
                        GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        c.transform.position = framEdgeWindowSide0;
                        c.name = "window fr0";
                        c.transform.localScale *= 0.5f;

                        c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        c.transform.position = frameEdgeNextFrameSide0;
                        c.name = "window ff0";
                        c.transform.localScale *= 0.5f;
                        c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        c.transform.position = framEdgeWindowSide1;
                        c.name = "window fr1";
                        c.transform.localScale *= 0.5f;
                        c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        c.transform.position = frameEdgeNextFrameSide1;
                        c.name = "window ff1";
                        c.transform.localScale *= 0.5f;

                        List<Vector3> temps = new List<Vector3>()
                        {
                            frameEdgeNextFrameSide1,//,frameEdgeNextFrameSide1,
                            framEdgeWindowSide1,framEdgeWindowSide1,
                            framEdgeWindowSide0,framEdgeWindowSide0,
                            frameEdgeNextFrameSide0//, frameEdgeNextFrameSide0                            


                        };

                      

                        tempsTemps.Add(temps);
                    }

                    tempsTempsTemps.Add(tempsTemps);

                }
                //we want to reverse the firs half of the list, because we started fromt he middle and worked our way out
                //this loop does this, afterwards it wil be inn order form points[i] to points[i+1]
                pointsWithWindows.Add(points[i]);
                bool first = true;//doube the first point
                for (int j = tempsTempsTemps.Count-1; j >=0 ; j--)
                {
                    for (int k = 0; k < tempsTempsTemps[j].Count; k++)
                    {
                        for (int l = 0; l < tempsTempsTemps[j][k].Count; l++)
                        {
                            pointsWithWindows.Add(tempsTempsTemps[j][k][l]);

                            //remember window points here..
                            if(l==1 || l==3)
                            {
                                GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                                c.transform.position = tempsTempsTemps[j][k][l];
                                c.name = "window point";
                                c.transform.localScale *= 0.5f;

                                //add to list which we will use to add window materials to 
                                windowIndexes.Add(pointsWithWindows.Count - 1);
                            }

                            if (first)
                            {
                                pointsWithWindows.Add(tempsTempsTemps[j][k][l]);
                                first = false;
                            }
                        }
                    }
                }

                //last - double the last
                pointsWithWindows.Add(pointsWithWindows[pointsWithWindows.Count - 1]);
                pointsWithWindows.Add(points[i+1]);


            }
            else
            {
                //no window, just add unique points
                pointsWithWindows.Add(points[i]);                
                pointsWithWindows.Add(points[i + 1]);
            }

        }

        for (int i = 0; i < pointsWithWindows.Count; i++)
        {
            GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
            c.transform.position = pointsWithWindows[i];
            c.transform.name = "aa";
        }


        //now add heights to these points
        List<List<Vector3>> meshPoints = tS.BuildHeightsForFloor(pointsWithWindows);
        //we have an array of arrays with points in a grid now. make triangles and vertices now
        //run nested list in to one list for mesh
        List<Vector3> vertices = tS.Vertices(meshPoints);
        //triangles
        List<List<int>> triangles = new List<List<int>>();
        for (int i = 0; i < meshPoints.Count; i++)
        {
            for (int j = 0; j < meshPoints[i].Count; j++)
            {


                //go!

               // GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
              //  c.transform.position = meshPoints[i][j];
              //  c.name = "mesh";
              //  c.transform.localScale *= 0.2f;



            }
        }

        GameObject wall = new GameObject();
        MeshFilter meshFilter = wall.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = wall.AddComponent<MeshRenderer>();


        meshRenderer.sharedMaterials = tS.materials ;

        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.subMeshCount = 3;
        mesh.SetTriangles(triangles[0], 0);
        mesh.SetTriangles(triangles[1], 1);
        mesh.SetTriangles(triangles[2], 2);
        mesh.RecalculateNormals();

        meshFilter.mesh = mesh;

       
        //recentre mesh? // just adjust y atm
        wall.transform.position = new Vector3(wall.transform.position.x, transform.position.y, wall.transform.position.z);
        wall.transform.parent = transform;

    }

    void BookendForFloor()
    {
        InteriorWalls[] interiorWalls = transform.parent.parent.GetComponentsInChildren<InteriorWalls>();
        //Debug.Log(interiorWalls.Length);
        for (int i = 0; i < interiorWalls.Length; i++)
        {
            if (interiorWalls[i] == this)
                continue;

            //other door centre
            for (int j = 0; j < interiorWalls[i].doorCentres.Count; j++)
            {
                for (int k = 0; k < doorCentres.Count; k++)
                {
                    if (interiorWalls[i].doorCentres[j] == doorCentres[k])
                    {
                        //looking for a matching center point, then using parallel lsit with points for door/wall edge
                        Vector3 a = doorEdges[k][0];
                        Vector3 b = interiorWalls[i].doorEdges[j][0];
                        Vector3 c = doorEdges[k][1];
                        Vector3 d = interiorWalls[i].doorEdges[j][1];

                        GameObject q = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        q.transform.position = a;
                        q = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        q.transform.position = b;
                        q = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        q.transform.position = c;
                        q = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        q.transform.position = d;

                        Vector3 boundsCentre = GetComponent<MeshRenderer>().bounds.center;//used to determine which way a door should face
                        //check if parent has been flagged with a door being built
                        //if (transform.parent.GetComponentInParent<Interiors>().apartmentDoorBuilt)
                        {
                            GameObject wall = WallBookend(a, b, c, d, boundsCentre);
                            wall.name = "Bookend";
                        }
                       // else
                        {
                          

                        }
                    }
                }
            }
        }
    }

    GameObject Bookend()
    {

        //
        float hallSize = GetComponentInParent<Hallways>().hallSize;
        InteriorWalls[] interiorWalls = transform.parent.parent.GetComponentsInChildren<InteriorWalls>();
        //Debug.Log(interiorWalls.Length);
        for (int i = 0; i < interiorWalls.Length; i++)
        {
            if (interiorWalls[i] == this)
                continue;

            //other door centre
            for (int j = 0; j < interiorWalls[i].doorCentres.Count; j++)
            {
                for (int k = 0; k < doorCentres.Count; k++)
                {
                    //point can be removed so we need to check for a small distance
                    if (Vector3.Distance( interiorWalls[i].doorCentres[j] ,doorCentres[k]) <= hallSize)
                    {
                        //looking for a matching center point, then using parallel lsit with points for door/wall edge
                        Vector3 a = doorEdges[k][0];
                        Vector3 b = interiorWalls[i].doorEdges[j][0];
                        Vector3 c = doorEdges[k][1];
                        Vector3 d = interiorWalls[i].doorEdges[j][1];

                        Vector3 boundsCentre = GetComponent<MeshRenderer>().bounds.center;//used to determine which way a door should face
                        //check if parent has been flagged with a door being built
                        if (transform.parent.GetComponentInParent<Interiors>().apartmentDoorBuilt)
                        {
                            GameObject wall = WallBookend(a, b, c, d,boundsCentre);
                            wall.name = "Bookend";//could do one with window for end of floor hall?
                        }
                        else
                        {
                            //we better build a door
                            float doorWidth = transform.parent.GetComponentInParent<Interiors>().doorWidth;
                           

                            float doorDepth = roomWallThickness * .66f;//?
                            //pass the centre of the inside positions so door is aligned for the interior halls, they can be angled and it looks nicer doing it this way
                            Vector3 insideCentre = Vector3.Lerp(a, b, 0.5f);

                         
                            //sometimes we have to flip direction, we will also need to keep this in mind when placing door
                            bool flip = false;
                            float thisDepth = roomWallThickness;
                            if (apartmentDoorOnly)
                                thisDepth = exteriorWallThickness;
                            GameObject frame =  InteriorAssets.ApartmentDoorFrame(out flip,insideCentre, a, b, c, d,doorHeight,doorWidth,thisDepth,tS,boundsCentre);
                            frame.name = "Door Frame";
                            //recentre mesh? // just adjust y atm
                            frame.transform.position = new Vector3(frame.transform.position.x, transform.position.y, frame.transform.position.z);
                            frame.transform.parent = transform;
                            frame.AddComponent<MeshRenderer>().sharedMaterial = m0;

                            GameObject door = InteriorAssets.ApartmentDoor(insideCentre, a, doorWidth, doorHeight, doorDepth, flip);
                            //put door parent on hinges at edge of door
                            door.transform.position = insideCentre + (b - a).normalized * doorWidth * .5f;// 
                            door.transform.position = new Vector3(door.transform.position.x, transform.position.y, door.transform.position.z);
                            door.transform.parent = transform;
                            door.name = "Room Door";
                            door.transform.GetChild(0).GetComponent<MeshRenderer>().sharedMaterial = m1;



                            transform.parent.GetComponentInParent<Interiors>().apartmentDoorBuilt = true;

                        }
                    }
                }                
            }
        }

        


        //transform.parent.GetComponentInParent<Interiors>().apartmentDoorCornersA.Clear();
        //transform.parent.GetComponentInParent<Interiors>().apartmentDoorCornersA.Clear();
        return null;
    }

    GameObject Wall(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
    {

        GameObject wall = InteriorAssets.Wall(a, b, c, d, floorHeight,tS);
        wall.AddComponent<MeshRenderer>().sharedMaterial = m0;

        //recentre mesh? // just adjust y atm
        wall.transform.position = new Vector3(wall.transform.position.x, transform.position.y, wall.transform.position.z);
        wall.transform.parent = transform;


        return wall;
    }

    GameObject WallBookend(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 boundsCentre)
    {

        GameObject wall = InteriorAssets.WallBookend(a, b, c, d, floorHeight,boundsCentre);
        wall.AddComponent<MeshRenderer>().sharedMaterial = m0;

        //recentre mesh? // just adjust y atm
        wall.transform.position = new Vector3(wall.transform.position.x, transform.position.y, wall.transform.position.z);
        wall.transform.parent = transform;


        return wall;
    }

    GameObject WallToNext(Vector3 y0This, Vector3 y0Next,Vector3 miterThis,Vector3 miterNext)
    {
        
        GameObject wall = InteriorAssets.Wall(y0This, y0Next, y0This - miterThis, y0Next - miterNext, floorHeight,tS);
        wall.AddComponent<MeshRenderer>().sharedMaterial = m0;

        //recentre mesh? // just adjust y atm
        wall.transform.position = new Vector3(wall.transform.position.x, transform.position.y, wall.transform.position.z);
        wall.transform.parent = transform;
        

        return wall;
    }

    GameObject WallToNextExteriorPrev(out Vector3 intersect,Vector3 y0Prev, Vector3 y0This, Vector3 y0Next,Vector3 miterNext)
    {
        //make space for outside wall

        Vector3 miterThisForOutside = Hallways.MiterDirection(y0Prev, y0This, y0Next, exteriorWallThickness);

        Vector3 edgeDir = (y0This - y0Prev).normalized * 100;//**
        Vector3 p2 = y0This - miterThisForOutside;
        Vector3 p3 = p2 + edgeDir * 100;


        //find intersect 
        Vector2 a0 = new Vector2(y0This.x, y0This.z);
        Vector2 a1 = new Vector2(y0Next.x, y0Next.z);
        Vector2 a2 = new Vector2(p2.x, p2.z);
        Vector2 a3 = new Vector2(p3.x, p3.z);

        Vector2 intersectV2 = Vector2.zero;

        // Debug.DrawLine(p0, p1);
        //  Debug.DrawLine(p2, p3);

        if (Hallways.LineSegmentsIntersection(a0, a1, a2, a3, out intersectV2))
        {
            //GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
           // c.transform.position = new Vector3(intersectV2.x, 0f, intersectV2.y);
          //  c.name = "intersect b";

            Vector3 intersectV3 = new Vector3(intersectV2.x, 0f, intersectV2.y);
            //now we also need the inside point which is room wall widthness towards the prev point
            Vector3 inside = intersectV3 + (y0Prev - y0This).normalized * roomWallThickness;



            GameObject wall1 = InteriorAssets.Wall(intersectV3, y0Next, inside, y0Next - miterNext, floorHeight,tS);//using intersect instead of edge ring point so vertices match exactly ( won't be a top triangle tho if we want to animate
            wall1.AddComponent<MeshRenderer>().sharedMaterial = m0;

            //recentre mesh? // just adjust y atm
            wall1.transform.position = new Vector3(wall1.transform.position.x, transform.position.y, wall1.transform.position.z);
            wall1.transform.parent = transform;
            wall1.name = "Wall 0b";

            intersect = intersectV3;
            return wall1;
        }
        else
        {
            intersect = Vector3.zero;
            Debug.Log("No intersect");
            return null;
        }

        
        
    }

    GameObject WallToNextExteriorNext(out Vector3 intersect,Vector3 y0Prev, Vector3 y0This,Vector3 y0Next, Vector3 y0NextNext)
    {
        Vector3 miterForExterior = Hallways.MiterDirection(y0This, y0Next, y0NextNext, exteriorWallThickness);

        //if going to an exterior wall wee need to cut the last point so we have space for for the exterior wall
        Vector2 intersectV2;
        Vector2 a0 = new Vector2(y0This.x, y0This.z);
        Vector2 a1 = new Vector2(y0Next.x, y0Next.z);
        Vector3 pointPlusMiter = y0Next - miterForExterior;
        Vector3 a2 = new Vector2(pointPlusMiter.x, pointPlusMiter.z);

        Vector3 edgeDir = pointPlusMiter + (y0Next - y0NextNext).normalized * 100;
        Vector2 a3 = new Vector2(edgeDir.x, edgeDir.z);


        Debug.DrawLine(y0This, y0Next);
        Debug.DrawLine(pointPlusMiter, edgeDir);

        if (Hallways.LineSegmentsIntersection(a0, a1, a2, a3, out intersectV2))
        {
           // GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
           // c.transform.position = new Vector3(intersectV2.x, 0f, intersectV2.y);
          //  c.name = "intersect a";

            // Vector3 y0prevPrev = new Vector3(tempList[prevPrev].x, 0f, tempList[prevPrev].z);
            Vector3 miter0 = Hallways.MiterDirection(y0Prev, y0This,y0Next, roomWallThickness);

            Vector3 intersectV3 = new Vector3(intersectV2.x, 0f, intersectV2.y);
            Vector3 inside = intersectV3 - edgeDir.normalized * roomWallThickness;
            GameObject wall = InteriorAssets.Wall(y0This, intersectV3, y0This - miter0, inside, floorHeight,tS);



            wall.AddComponent<MeshRenderer>().sharedMaterial = m0;

            //recentre mesh? // just adjust y atm
            wall.transform.position = new Vector3(wall.transform.position.x, transform.position.y, wall.transform.position.z);
            wall.transform.parent = transform;
            wall.name = "Room Wall A";

            intersect = intersectV3;
            return wall;
        }
        else
        {
            intersect = Vector3.zero;
            Debug.Log("no intersect here");
            return null;
        }
    }
    
}

