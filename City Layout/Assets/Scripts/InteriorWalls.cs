using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteriorWalls : MonoBehaviour
{
    public bool reset = false;
  //  public List<Vector3> doorEdges;
    public List<Vector3> apartmentDoorPositions;
    public List<Vector3> exteriors; 
    public List<Vector3> tempList;
    public List<Vector3> originalPositions;
    public float roomWallThickness = .2f;
    public float exteriorWallThickness = .4f;//pass down from somewhere
    public TraditionalSkyscraper tS;

    float floorHeight;

    Material m;
    // Start is called before the first frame update
    // public bool apartment = false;
    public bool room = false;

    private void Awake()
    {
        enabled = false;
    }
    private void Update()
    {
        if(reset)
        {
            Start();
            reset = false;
        }
    }

    void Start()
    {

        floorHeight = tS.floorHeight * (1f - tS.spacerHeight * 2);

        //material
        PaletteInfo pI = tS.GetComponent<PaletteInfo>();
        
        if (pI.palette != null)//protexts for hot loading
        {
            if (pI.palette.Count > 0)
            {
                m = pI.palette[0].tints[0];
                //wall.AddComponent<MeshRenderer>().sharedMaterial = m;  //match to storey outside?
            }
        }
        else
            m = Resources.Load("Grey") as Material;

        // tempList = new List<Vector3>(doorEdges);

        //flatten, door edges ahve some 1 heights in them, used for wokring out floorplans
        //  for (int i = 0; i < tempList.Count; i++)
        {
            //tempList[i] = new Vector3(tempList[i].x, 0f, tempList[i].z);
        }

        //apartment doors are built where a hall meets a hall - what do we have for this?

        //room doors are built on edges that run along a hall - we have this with door edges atm

        RoomDoorAndWalls();

    }


    void RoomDoorAndWalls()
    {

        for (int i = 0; i < exteriors.Count; i++)
        {
           // GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
           // c.transform.position = exteriors[i];
           // c.name = "ex";
        }

      

        //edges are a list of vector3s, choose an edge ( longer, shorter)
        float doorSize = transform.parent.GetComponentInParent<Interiors>().doorWidth * 2;
        bool doorBuilt = true;
      
        
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

            if (tempList[prev].y == 0f && tempList[i].y == 0f && tempList[next].y > 0f)//tempList[prev].y == 0f &&  //some rooms only have two door points, cant check prev
            {

                //Vector3 miterThis = Hallways.MiterDirection(tempList[prev], tempList[i], y0Next, roomWallThickness);///up top
                //if (!containsPrev && !containsThis)
                //do we need to build an aparment door?
                // if (!GetComponentInParent<Hallways>().GetComponentInParent<Interiors>().apartmentDoorBuilt)
                if (!exteriors.Contains(tempList[i]))
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
                        GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        c.transform.position = new Vector3(intersectV2.x, 0f, intersectV2.y);
                        c.name = "intersect";

                        c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        c.transform.position = tempList[i];
                        c.name = "local i";

                        Vector3 intersectV3 = new Vector3(intersectV2.x, 0f, intersectV2.y);
                        Vector3 door0 = originalPositions[i];//testing
                        Vector3 door1 = door0 + (Quaternion.Euler(0, 90, 0) * edgeDir) * roomWallThickness;


                        c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        c.transform.position = door0;
                        c.name = "door 0";



                        c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        c.transform.position = door1;
                        c.name = "door 1";


                        GameObject wall = InteriorAssets.Wall(door1, intersectV3, door0, tempList[i], floorHeight);
                        wall.AddComponent<MeshRenderer>().sharedMaterial = m;
                        //recentre mesh? // just adjust y atm
                        wall.transform.position = new Vector3(wall.transform.position.x, transform.position.y, wall.transform.position.z);
                        wall.transform.parent = transform;
                        wall.name = "Wall (bookend)";
                        //sliding point to edge of bookend 
                        //wallPoint0 = intersectV3;

                        //we need to add these points to a list held in parent object - once we ahve enough points, we can build the door and frame
                        transform.parent.GetComponentInParent<Interiors>().apartmentDoorCornersA.Add(intersectV3);
                        transform.parent.GetComponentInParent<Interiors>().apartmentDoorCornersA.Add(tempList[i]);

                        if (transform.parent.GetComponentInParent<Interiors>().apartmentDoorCornersB.Count == 2)
                        {
                            Bookend();
                        }

                    }
                    else
                        Debug.Log("no intersect for apartment wall");



                }

                if (!containsPrev && !containsThis && !containsNext)
                {
                        GameObject wall = WallToNext(y0This, y0Next, miterThis, miterNext);
                        wall.name = "Wall 0 0 1";
                    //GameObject wall = WallToNextExteriorPrev(y0Prev, y0This, y0Next, miterNext);
                    // wall.name = "Wall 0b";

                }
                else if(containsPrev && !containsThis && !containsNext)
                {
                  //  Debug.Log("not tested");
                    GameObject wall = WallToNext(y0This, y0Next, miterThis, miterNext);                    
                    wall.name = "Wall no ext 0 0 1";
                    
                }

                else if (containsPrev && containsThis && !containsNext)
                {
                    GameObject wall = WallToNextExteriorPrev(y0Prev, y0This, y0Next, miterNext);
                    wall.name = "Wall prev ext 0 0 1";
                }
            }
            else if (tempList[prev].y > 0f && tempList[i].y == 0f && tempList[next].y == 0f)
            {
                
                Vector2 intersectV2;
                Vector3 edgeDir;
                Vector3 a0;
                Vector3 a1;
                Vector3 a2;
                if (!exteriors.Contains(tempList[i]))
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
                        GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        c.transform.position = new Vector3(intersectV2.x, 0f, intersectV2.y);
                        c.name = "intersect x";

                        c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        c.transform.position = tempList[i];
                        c.name = "local i x";

                        Vector3 intersectV3 = new Vector3(intersectV2.x, 0f, intersectV2.y);

                        //use the original point from the previous mesh/ring points //testing
                        Vector3 door0 = originalPositions[i];

                        c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        c.transform.position = door0;
                        c.name = "door 0 x";

                        Vector3 door1 = door0 + (Quaternion.Euler(0, -90, 0) * edgeDir) * roomWallThickness;

                        c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        c.transform.position = door1;
                        c.name = "door 1 x";

                        GameObject wall = InteriorAssets.Wall(door0, tempList[i], door1, intersectV3, floorHeight);

                       

                        wall.AddComponent<MeshRenderer>().sharedMaterial = m;

                        //recentre mesh? // just adjust y atm
                        wall.transform.position = new Vector3(wall.transform.position.x, transform.position.y, wall.transform.position.z);
                        wall.transform.parent = transform;
                        wall.name = "Wall (bookend) 2";

                        //we need to add these points to a list held in parent object - once we ahve enough points, we can build the door and frame
                        transform.parent.GetComponentInParent<Interiors>().apartmentDoorCornersB.Add(intersectV3);
                        transform.parent.GetComponentInParent<Interiors>().apartmentDoorCornersB.Add(tempList[i]);

                        if (transform.parent.GetComponentInParent<Interiors>().apartmentDoorCornersA.Count == 2)
                        {
                            Bookend();
                        }



                    }

                }
                //else needed?
              


                //wall
               
                if (!containsPrev && !containsThis && containsNext)
                {
                    GameObject wall = WallToNextExteriorNext(y0Prev, y0This, y0Next, y0NextNext);
                    wall.name = "Wall next ext 1 0 0";
                  
                }
                else if(!containsPrev && !containsThis)
                {
                    GameObject wall = WallToNext(y0This, y0Next, miterThis, miterNext);
                    wall.name = "Wall 1 0 0";
                }


               
            }
            else if (tempList[prev].y == 0f && tempList[i].y > 0f && tempList[next].y == 0f)
            {
                if (containsNext)
                {
                    GameObject wall = WallToNextExteriorNext(y0Prev, y0This, y0Next, y0NextNext);
                    wall.name = "Wall next ext 0 1 0";
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
                    GameObject wall = WallToNextExteriorPrev(y0Prev, y0This, y0Next, miterNext);
                    wall.name = "Wall ext prev 0 0 0";
                }

            }
        }

        if (doorBuilt == false)
        {
            Debug.Log("No wall found for door");
        }
    }

    GameObject Bookend()
    {
        List<Vector3> pointsA = transform.parent.GetComponentInParent<Interiors>().apartmentDoorCornersA;
        List<Vector3> pointsB = transform.parent.GetComponentInParent<Interiors>().apartmentDoorCornersB;
        //end of a hall

        GameObject wall = Wall(pointsA[0], pointsB[0], pointsA[1], pointsB[1]);
        wall.name = "Booked Simple";


        //transform.parent.GetComponentInParent<Interiors>().apartmentDoorCornersA.Clear();
        //transform.parent.GetComponentInParent<Interiors>().apartmentDoorCornersA.Clear();
        return null;
    }

    GameObject Wall(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
    {

        GameObject wall = InteriorAssets.Wall(a, b, c, d, floorHeight);
        wall.AddComponent<MeshRenderer>().sharedMaterial = m;

        //recentre mesh? // just adjust y atm
        wall.transform.position = new Vector3(wall.transform.position.x, transform.position.y, wall.transform.position.z);
        wall.transform.parent = transform;


        return wall;
    }


    GameObject WallToNext(Vector3 y0This, Vector3 y0Next,Vector3 miterThis,Vector3 miterNext)
    {
        
        GameObject wall = InteriorAssets.Wall(y0This, y0Next, y0This - miterThis, y0Next - miterNext, floorHeight);
        wall.AddComponent<MeshRenderer>().sharedMaterial = m;

        //recentre mesh? // just adjust y atm
        wall.transform.position = new Vector3(wall.transform.position.x, transform.position.y, wall.transform.position.z);
        wall.transform.parent = transform;
        

        return wall;
    }

    GameObject WallToNextExteriorPrev(Vector3 y0Prev, Vector3 y0This, Vector3 y0Next,Vector3 miterNext)
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
            GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
            c.transform.position = new Vector3(intersectV2.x, 0f, intersectV2.y);
            c.name = "intersect b";

            Vector3 intersectV3 = new Vector3(intersectV2.x, 0f, intersectV2.y);
            //now we also need the inside point which is room wall widthness towards the prev point
            Vector3 inside = intersectV3 + (y0Prev - y0This).normalized * roomWallThickness;



            GameObject wall1 = InteriorAssets.Wall(intersectV3, y0Next, inside, y0Next - miterNext, floorHeight);//using intersect instead of edge ring point so vertices match exactly ( won't be a top triangle tho if we want to animate
            wall1.AddComponent<MeshRenderer>().sharedMaterial = m;

            //recentre mesh? // just adjust y atm
            wall1.transform.position = new Vector3(wall1.transform.position.x, transform.position.y, wall1.transform.position.z);
            wall1.transform.parent = transform;
            wall1.name = "Wall 0b";

            return wall1;
        }
        else
        {

            Debug.Log("No intersect");
            return null;
        }

        
        
    }

    GameObject WallToNextExteriorNext(Vector3 y0Prev, Vector3 y0This,Vector3 y0Next, Vector3 y0NextNext)
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
            GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
            c.transform.position = new Vector3(intersectV2.x, 0f, intersectV2.y);
            c.name = "intersect a";

            // Vector3 y0prevPrev = new Vector3(tempList[prevPrev].x, 0f, tempList[prevPrev].z);
            Vector3 miter0 = Hallways.MiterDirection(y0Prev, y0This,y0Next, roomWallThickness);

            Vector3 intersectV3 = new Vector3(intersectV2.x, 0f, intersectV2.y);
            Vector3 inside = intersectV3 - edgeDir.normalized * roomWallThickness;
            GameObject wall = InteriorAssets.Wall(y0This, intersectV3, y0This - miter0, inside, floorHeight);



            wall.AddComponent<MeshRenderer>().sharedMaterial = m;

            //recentre mesh? // just adjust y atm
            wall.transform.position = new Vector3(wall.transform.position.x, transform.position.y, wall.transform.position.z);
            wall.transform.parent = transform;
            wall.name = "Room Wall A";

            return wall;
        }
        else
        {
            Debug.Log("no intersect here");
            return null;
        }
    }
    
}

