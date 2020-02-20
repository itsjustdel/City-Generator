﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Hallways : MonoBehaviour
{
    public int iterations = 0;
    public bool makeHalls = false;
    public bool reCentreMesh = false;
    public List<Vector3> interiorsRingPoints = new List<Vector3>();
    public List<Vector3> targetPoints = new List<Vector3>();
    public List<Vector3> tempList = new List<Vector3>();
    List<Vector3> ringPoints = new List<Vector3>();

    public List<Vector3> apartmentDoorPositions = new List<Vector3>();

    //saves two lists used to make walls for the end of halls - "bookends"
    public List<List<Vector3>> bookendPoints = new List<List<Vector3>>();

    public float hallSize = 1f;

    public float wallDepth = 0.3f;
    public float doorDepth = 0.2f;

    // Start is called before the first frame update
    TraditionalSkyscraper tS;
    public void Start()
    {
        if(iterations == 0)
            tS = transform.parent.parent.parent.GetComponent<TraditionalSkyscraper>();
        else if (iterations ==1)
            tS = transform.parent.parent.parent.parent.parent.GetComponent<TraditionalSkyscraper>();

        //make local  - we can make changes to this without overwriting original info
        ringPoints = new List<Vector3>(interiorsRingPoints);
       
        if (reCentreMesh == false)
        {
            //we need to puit the points back in world space to work out
            for (int i = 0; i < ringPoints.Count; i++)
            {
                ringPoints[i] += new Vector3(transform.position.x, 0f, transform.position.z);
            }
        }

        for (int i = 0; i < targetPoints.Count; i++)
        {

          //  GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
          //  c.transform.position = targetPoints[i];
           // c.name = "target";

        }


        AlterMeshSimple();
    }

    private void Update()
    {
        if (makeHalls)
        {
            ringPoints = new List<Vector3>(interiorsRingPoints);

            Start();
            makeHalls = false;
        }
    }


    void AlterMeshSimple()
    {
        
         tempList = new List<Vector3>();

        int safety = 0;
       // GameObject c;
       

        List<Vector3> blockList = new List<Vector3>();
        //gather hall edges and centre points(where it meets the edge and internal halls)
        List<Vector3> hallPoints = new List<Vector3>();
        //where the positions came from -needed for door positions
        List<Vector3> originalPositions = new List<Vector3>();
        //exteriors
        List<Vector3> exteriors = new List<Vector3>();
        //we will store the points which get removed so we can build exteriro walls with them or bookends
        List<Vector3> bookendPointsA = new List<Vector3>();
        List<Vector3> bookendPointsB = new List<Vector3>();

        //make space for halls by pulling in vertices
        for (int i = 0; i < ringPoints.Count; i++)
        {
            safety++;
            if (safety > 100)
            {
                Debug.Log("safety");
                break;

            }

           // c = GameObject.CreatePrimitive(PrimitiveType.Cube);
         //   c.transform.position = ringPoints[i];
         //   c.name = i.ToString();



            int prev = i - 1;
            if (prev < 0)
                prev += ringPoints.Count;

            int next = i + 1;
            if (next > ringPoints.Count - 1)
                next -= ringPoints.Count;


            //we are looking for changes in the y co-ord. When making the points, the y co-ord is 0f for an outside point, and 1f, for an inside (a hall)

            //all edge, add, no problem
            if (ringPoints[prev].y == 0f && ringPoints[i].y == 0f && ringPoints[next].y == 0f)
            {
                //add this first
                if (!blockList.Contains(ringPoints[i]))
                {
                    tempList.Add(ringPoints[i]);
                    originalPositions.Add(ringPoints[i]);
                    

                    if (IsDoorExterior(i))
                        exteriors.Add(ringPoints[i]);
                    
                }
            }
            else if (ringPoints[prev].y == 0f && ringPoints[i].y > 0f && ringPoints[next].y == 0f)
            {
                
                /*
                c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                c.transform.position = ringPoints[prev];
                c.name = "non,hall , non";

                c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                c.transform.position = ringPoints[i];
                c.name = i.ToString();

                c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                c.transform.position = ringPoints[next];
                c.name = next.ToString();
                */

                Vector3 miterDir = MiterDirection(ringPoints[prev], ringPoints[i], ringPoints[next],hallSize);
                tempList.Add(ringPoints[i] - miterDir);
                

                //add halls //iteration?
                hallPoints.Add(ringPoints[prev]);
                hallPoints.Add(ringPoints[i]);
                hallPoints.Add(ringPoints[next]);

                //find door positions for exterior
                if (!IsDoorExterior(i))
                {
                   // GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                   // c.transform.position = ringPoints[i] - miterDir;
                   // c.name = "Interior 1";

                    originalPositions.Add(ringPoints[i]);
                }
                else
                {
                    exteriors.Add(ringPoints[i]);
                    originalPositions.Add(ringPoints[i]);
                }


            }
            else if (ringPoints[prev].y > 0f && ringPoints[i].y == 0f && ringPoints[next].y == 0f)
            {



                //find interect on outer ring/target points
                Vector3 foundIntersect = Vector3.zero;

                //if target points equal ring?
                int indexFoundAt = 0;
                if (IntersectNext(out foundIntersect,out indexFoundAt,ringPoints,ref blockList, hallSize,prev,i,next,i,true,false,false))
                {
                    tempList.Add(foundIntersect);

                    //these are the points which we clipped - we still need them for the wall at the end of the hall
                    GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    c.transform.position = ringPoints[prev];
                    c.name = "missing start 1 0 0";
                    bookendPointsA.Add(ringPoints[prev]);
                    for (int a = i; a <= indexFoundAt; a++)
                    {
                        int thisTemp = a;
                        if (thisTemp > ringPoints.Count - 1)
                            thisTemp -= ringPoints.Count;

                        c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                         c.transform.position = ringPoints[thisTemp];
                         c.name = "missing";

                        bookendPointsA.Add(ringPoints[thisTemp]);
                    }

                    c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    c.transform.position = foundIntersect;
                    c.name = "missing end";
                    bookendPointsA.Add(foundIntersect);


                    i = indexFoundAt;

                    //find door positions for exterior
                    if (!IsDoorExterior(i))
                    {
                        originalPositions.Add(ringPoints[i]);
                    }
                    else
                    {
                        exteriors.Add(foundIntersect);//maybe found intersect?like below
                        originalPositions.Add(ringPoints[i]);
                    }

                }
                
            }
            else if (ringPoints[prev].y == 0f && ringPoints[i].y == 0f && ringPoints[next].y > 0f)
            {
                /*
                c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                c.transform.position = ringPoints[prev];
                c.name = "non,non  ,hall";

                c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                c.transform.position = ringPoints[i];
                c.name = i.ToString();

                c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                c.transform.position = ringPoints[next];
                c.name = next.ToString();
                */
                //find interect on outer ring/target points

                if (tempList.Count == 0)
                {
                 //   Debug.Log("did this");//working needed?
                  //  ringPoints.Add(ringPoints[i]);
                 //   ringPoints.RemoveAt(i);
                    
                 //   i--;

                 //   continue;                  
                }


                Vector3 foundIntersect = Vector3.zero;
                int indexFoundAt = 0;
                if (IntersectNext(out foundIntersect, out indexFoundAt, ringPoints, ref blockList, hallSize, prev, i, next, i, false, false,true))
                {

                    //these are the points which we clipped - we still need them for the wall at the end of the hall
                    GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    c.transform.position = foundIntersect;
                    c.name = "missing start 0 0 1";
                    bookendPointsB.Add(foundIntersect);

                    for (int a = indexFoundAt+1; a <= ringPoints.Count + indexFoundAt; a++)
                    {
                        int thisTemp = a;
                        if (thisTemp > ringPoints.Count-1)
                            thisTemp -= ringPoints.Count;

                        if (ringPoints[thisTemp] == ringPoints[next])
                            break;

                        c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        c.transform.position = ringPoints[thisTemp];
                        c.name = "missing";

                        bookendPointsB.Add(ringPoints[thisTemp]);

                    }

                    c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    c.transform.position = ringPoints[next];
                    c.name = "missing end 0 0 1";
                    bookendPointsB.Add(ringPoints[next]);


                    tempList.Add(foundIntersect);

                    if (!IsDoorExterior(i))
                    {
                      //  GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                      //  c.transform.position = foundIntersect;
                     //  c.name = "Interior 3";

                        originalPositions.Add(ringPoints[i]);

                       
                    }
                    else
                    {
                        exteriors.Add(foundIntersect);//not tested
                        originalPositions.Add(ringPoints[i]);
                    }
                }


            }
            else if (ringPoints[prev].y == 0f && ringPoints[i].y > 0f && ringPoints[next].y > 0f)
            {
                //need to do
                Debug.Log("0 higher higher");
            }
            else if (ringPoints[prev].y > 0f && ringPoints[i].y > 0f && ringPoints[next].y > 0f)
            {
                //need to do
                Debug.Log("ALL HALL");
            }
            else if (ringPoints[prev].y > 0f && ringPoints[i].y == 0f && ringPoints[next].y > 0f)
            {
                //need to do
                Debug.Log("up down up");
            }

        }

        //save points for end of halls - each hallways script will have one and interiors script will gather all these points together to build wals
        bookendPoints = new List<List<Vector3>> { bookendPointsA, bookendPointsB };

        //tidy up andy points that didn't amke it
        for (int i = tempList.Count-1; i >= 0; i--)
        {
            if(blockList.Contains( tempList[i]))
            {

               // GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
               // c.transform.position = tempList[i];
               // c.name = "Removed";

                originalPositions.Remove(tempList[i]);//?
                exteriors.Remove(tempList[i]);//?
                tempList.RemoveAt(i);
            }
        }

        


        if (tempList.Count > 3)
        {
            GameObject cell = Cell(tempList, originalPositions, exteriors);


            //split up again?
            if (iterations < 1)
            {
                Interiors interiors = cell.AddComponent<Interiors>();
                //flatten
                for (int i = 0; i < tempList.Count; i++)
                {
                    tempList[i] = new Vector3(tempList[i].x, 0f, tempList[i].z);
                }
                for (int i = 0; i < targetPoints.Count; i++)
                {
                    targetPoints[i] = new Vector3(targetPoints[i].x, 0f, targetPoints[i].z);
                }

                interiors.ringPoints = tempList;
                interiors.targetPoints = hallPoints;
                interiors.cornerPoints = tempList;
                interiors.corners = 3;
                interiors.iterations = iterations + 1;
                //interiors.enabled = false;

                GetComponent<MeshRenderer>().enabled = false;
            }
        }
        else
        {
            Debug.Log("Room has less than 4 ring points");
            GetComponent<MeshRenderer>().enabled = false;
            transform.name = "Skipped";
        }

    }

    bool IsDoorExterior(int i)
    {
        //two potential doors here, previous point, and next. Check to see if these points are between ring points for the whole floor
        //3 parents up if 2nd iteration
        GameObject c = null;
        bool exterior = false;
        if (iterations == 1)
        {
            List<Vector3> parentsRingPoints = transform.parent.parent.parent.GetComponent<Interiors>().ringPoints;
            parentsRingPoints = parentsRingPoints.Distinct().ToList();

           
            for (int j = 0; j < parentsRingPoints.Count; j++)
            {               
                int parentNext = j + 1;
                if (parentNext > parentsRingPoints.Count - 1)
                    parentNext -= parentsRingPoints.Count;

                //check to see if previous point or next point is between parent ring points
               // float d0 = Vector3.Distance(parentsRingPoints[j], parentsRingPoints[parentNext]);              
               // float d1 = Vector3.Distance(ringPoints[next], parentsRingPoints[j]);
               // float d2 = Vector3.Distance(ringPoints[next], parentsRingPoints[parentNext]);

                if(DistanceLineSegmentPoint(parentsRingPoints[j],parentsRingPoints[parentNext],ringPoints[i]) < 0.01f)
                {


                    exterior = true;
                }
            }
        }

        return exterior;
    }

    // Distance to point (p) from line segment (end points a b)
    float DistanceLineSegmentPoint(Vector3 a, Vector3 b, Vector3 p)
    {
        //https://forum.unity.com/threads/how-to-check-a-vector3-position-is-between-two-other-vector3-along-a-line.461474/

        // If a == b line segment is a point and will cause a divide by zero in the line segment test.
        // Instead return distance from a
        if (a == b)
            return Vector3.Distance(a, p);

        // Line segment to point distance equation
        Vector3 ba = b - a;
        Vector3 pa = a - p;
        return (pa - ba * (Vector3.Dot(pa, ba) / Vector3.Dot(ba, ba))).magnitude;
    }

    static bool IntersectNext(out Vector3 foundIntersect,out int indexFoundAt, List<Vector3> ringPoints, ref List<Vector3> blockList,  float hallSize, int prev, int i, int next,int targetStart, bool flipDir, bool onlyLookForHalls,bool addToBlock)
    {

        bool foundTemp = false;
        indexFoundAt = 0;
        foundIntersect = Vector3.zero;
        //find where a miter point plus a direction intersects with target points

        Vector3 miter = MiterDirection(ringPoints[prev], ringPoints[i], ringPoints[next], hallSize);

        //now project to back to find intersect (we are sliding the whole edge)

        
        Vector3 edgeDir = (ringPoints[i] - ringPoints[next])*1000;//perhaps another function would be better here for line segment check
        if(flipDir)
            edgeDir = (ringPoints[i] - ringPoints[prev]);//other

        Vector3 p2 = ringPoints[i] - miter;
        Vector3 p3 = p2 + edgeDir;

        Debug.DrawLine(p2, p3, Color.magenta);
        Vector2 intersectV2 = Vector2.zero;
        
       // float distance = Mathf.Infinity;

        for (int aa = targetStart + 1, bb = 0; bb < ringPoints.Count; bb++, aa--)
        {
           
            if (aa > ringPoints.Count - 1)
                aa -= ringPoints.Count;

            if (aa < 0)
                aa += ringPoints.Count;

            int aaPrev = aa - 1;
            if (aaPrev < 0)
                aaPrev += ringPoints.Count;

           

            Vector3 p0 = ringPoints[aaPrev];
            Vector3 p1 = ringPoints[aa];

            if (onlyLookForHalls)
            {
                //skip any ring points -only looking for halls
                if (p0.y == 0f & p1.y == 0f)
                    continue;
            }


            //if(tempList.Contains(targetPoints[aa]))
            // {
            //   GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
            //   c.transform.position = ringPoints[aa];
            //  c.name = "to remove";

           // GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
            //   c.transform.position = ringPoints[aa];
            //c.name = "to remove";

            if (addToBlock)
                blockList.Add(ringPoints[aa]);

            
           // }

          //  Debug.DrawLine(p2, p3, Color.yellow);


            Vector2 a0 = new Vector2(p0.x, p0.z);
            Vector2 a1 = new Vector2(p1.x, p1.z);
            Vector2 a2 = new Vector2(p2.x, p2.z);
            Vector2 a3 = new Vector2(p3.x, p3.z);

            Vector2 tempV2 = Vector2.zero;
            if (LineSegmentsIntersection(a0, a1, a2, a3, out tempV2))
            {
                // c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                //c.transform.position = new Vector3(tempV2.x,0f,tempV2.y);
               // c.name = "intersect b4 distance check";

             //   Debug.DrawLine(p0, p1, Color.red);
             //   Debug.DrawLine(p2, p3, Color.cyan);

                //we want the closest intersect
                //float tempD = Vector2.Distance(p2, tempV2);
                //if (tempD < distance)
                {
                    intersectV2 = tempV2;
                   // distance = tempD;

                   // c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                   // c.transform.position = new Vector3(tempV2.x, 0f, tempV2.y);
                   // c.name = "intersect after distance check";

                    foundTemp = true;

                    foundIntersect = new Vector3(intersectV2.x, 0f, intersectV2.y);
                    indexFoundAt = aaPrev;
                   // foundIntersects.Add(foundIntersect);

                    break;
                    
                }
            }
        }

        return foundTemp;
    }
  
    public GameObject Cell(List<Vector3> tempList, List<Vector3> originalPositions, List<Vector3> exteriors)
    {
        List<Vector3> newPoints = new List<Vector3>(tempList);

        GetComponent<MeshRenderer>().enabled = false;
       // newPoints = newPoints.Distinct().ToList();//.linq

        Vector3 avg = Vector3.zero;
        bool doCubes = false;

        for (int a = 0; a < newPoints.Count; a++)
        {
            if (doCubes)
            {
                GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                c.transform.position = newPoints[a];
                c.name = "np";
            }

            //flatten - working on THIS
            Vector3 flat = new Vector3(newPoints[a].x, 0f, newPoints[a].z);
            avg += flat;// newPoints[a];
            newPoints[a] = flat;//testing is ok
        }
        avg /= newPoints.Count;


        newPoints.Insert(0, avg);

        if (reCentreMesh)
        {
            //recentre so mesh is local 
            for (int i = 0; i < newPoints.Count; i++)
            {
                newPoints[i] -=  new Vector3(transform.position.x, 0f, transform.position.z);

            }
        }


        List<int> trianglesL = new List<int>();

        for (int i = 1; i < newPoints.Count - 1; i++)
        {
            if (i == 0)
                continue;

            trianglesL.Add(0);
            trianglesL.Add(i);
            trianglesL.Add(i + 1);
        }

        //last tri (joining last and first)
        trianglesL.Add(0);
        trianglesL.Add(newPoints.Count - 1);
        trianglesL.Add(1);

        GameObject roomFloor = new GameObject();
        roomFloor.transform.parent = transform;
        roomFloor.transform.localPosition = Vector3.zero;// transform.parent.position;// new Vector3(avg.x, transform.position.y, avg.z);
       // roomFloor.transform.position += Vector3.up * 10;///888test
        roomFloor.name = "RoomFloor";
        roomFloor.AddComponent<MeshRenderer>().sharedMaterial = Resources.Load("Grey") as Material;
        MeshFilter mf = roomFloor.AddComponent<MeshFilter>();
        Mesh mesh = new Mesh();
        mesh.vertices = newPoints.ToArray();
        mesh.triangles = trianglesL.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        mf.mesh = mesh;


        //apartment walls and door?

        if (iterations == 0)
        {/*

            InteriorWalls iW = roomFloor.AddComponent<InteriorWalls>();
            iW.tempList = tempList;
            iW.apartmentDoorPositions = apartmentDoorPositions;
            iW.tS = tS;
            iW.originalPositions = originalPositions;
            //iW.exteriors = targetPoints;
            //all walls will be built after
            iW.apartmentDoorOnly = true;
            */
        }

        if (iterations == 1)//back in
        {
            
            InteriorWalls iW = roomFloor.AddComponent<InteriorWalls>();
            iW.tempList = tempList;
            iW.apartmentDoorPositions = apartmentDoorPositions;
            iW.tS = tS;
            iW.originalPositions = originalPositions;
            iW.exteriors = exteriors;
            
        }
      
      
       

        return roomFloor;
    }

    //helper functions
    public static Vector3 MiterDirection(Vector3 p0, Vector3 p1, Vector3 p2, float borderSize)
    {
        //flatten miters in this case
        p0 = new Vector3(p0.x, 0f, p0.z);
        p1 = new Vector3(p1.x, 0f, p1.z);
        p2 = new Vector3(p2.x, 0f, p2.z);

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
        float length = borderSize / Vector3.Dot(normal0, miterDirection);
        miterDirection *= length;

        //  Debug.DrawLine(p0, p1, Color.yellow);
        //   Debug.DrawLine(p2, p1, Color.yellow);
        // if(draw)
        Debug.DrawLine(p1, p1 - miterDirection, Color.yellow);
        //Debug.Break();
        //GetComponent<MeshRenderer>().enabled = true;
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
