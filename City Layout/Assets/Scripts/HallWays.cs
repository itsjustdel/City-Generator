using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using System.Linq;

public class Hallways : MonoBehaviour
{
    public bool makeHalls = false;

    public List<Vector3> ringPoints = new List<Vector3>();
    public List<Vector3> targetPoints = new List<Vector3>();
    public float hallSize = 1f;

    Vector3 doorEdgePos;

    // Start is called before the first frame update
    void Start()
    {

        bool makeExteriorDoor = ExteriorDoor();

        AlterMesh(makeExteriorDoor);
    }

    private void Update()
    {
        if (makeHalls)
        {
            Start();
            makeHalls = false;
        }
    }

    bool ExteriorDoor()
    {
        //check if this cell has an exterior door on one of its edges
        bool exteriorDoor = false;

        doorEdgePos = GetComponentInParent<Interiors>().doorEdgePos;

        for (int i = 0; i < ringPoints.Count; i++)
        {
            if (ringPoints[i] == doorEdgePos)
            {

                GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                c.transform.position = doorEdgePos;
                c.name = "door edge HallWays.cs";

                exteriorDoor = true;
            }
        }




        return exteriorDoor;
    }

    void AlterMesh(bool makeExteriorDoor)
    {
        List<Vector3> tempList = new List<Vector3>();

        int safety = 0;

        //make space for halls by pulling in vertices
        for (int i = 0; i < ringPoints.Count; i++)
        {
            safety++;

          // GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
            //c.transform.position = ringPoints[i];
            //c.name = "rp, y = " + ringPoints[i].y;


            int prev = i - 1;
            if (prev < 0)
                prev = ringPoints.Count - 1;

            int next = i + 1;
            if (next > ringPoints.Count - 1)
                next -= ringPoints.Count;


            //we are looking for changes in the y co-ord. When making the points, the y co-ord is 0f for an outside point, and 1f, for an inside (a hall)

            //all edge, add, no problem
            if (ringPoints[prev].y == 0f && ringPoints[i].y == 0f && ringPoints[next].y == 0f)
            {
                //add this first
                tempList.Add(ringPoints[i]);
            }

            //if coming from non hall and this is non hall but going to hall
            else if (ringPoints[prev].y == 0f && ringPoints[i].y == 0f && ringPoints[next].y > 0f)
            {
                GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                c.transform.position = ringPoints[prev];
                c.name = "rp, prev 0";

                c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                c.transform.position = ringPoints[i];
                c.name = "rp, this 0";

                c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                c.transform.position = ringPoints[next];
                c.name = "rp, next 1";

                //if this cell has been chosen to have an exterior door on it

                if (makeExteriorDoor)
                {
                    //if not a door pos
                    if (doorEdgePos != ringPoints[i])
                    {
                        c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        c.transform.position = ringPoints[i];
                        c.name = "rp, not door";

                        tempList.Add(ringPoints[i]);
                    }
                    //and the next point is a door position
                    else
                    {
                        Vector3 intersectV3 = IntersectNext(prev, i, next,false);
                        tempList.Add(intersectV3);
                    }                    
                }
                else if (!makeExteriorDoor)
                {
                    //not tested
                    c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    c.transform.position = ringPoints[i];
                    c.name = "exterior door = false";

                    tempList.Add(ringPoints[i]);
                }
            }
            else if (ringPoints[prev].y == 0f && ringPoints[i].y > 0f && ringPoints[next].y == 0f)
            {
                GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                c.transform.position = ringPoints[prev];
                c.name = "non,hall , non";

                c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                c.transform.position = ringPoints[i];
                c.name = i.ToString();

                c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                c.transform.position = ringPoints[next];
                c.name = next.ToString();

                if (makeExteriorDoor)
                {
                    //check to see if prev or next point is a an exterior door. we ahve a hall point surrounded by exterior in this case
                    if (doorEdgePos == ringPoints[prev])
                    {

                        //NOT TESTED
                        //use direction of original edge
                        Vector3 edgeDir = (ringPoints[i] - ringPoints[prev]);//?

                        //use previous point - is it just last in templist? //out of index possible?
                        if (tempList.Count == 0)
                        {
                            //in order to work out this point we need the point before it to be have been worked out. let's add this point to the end of the list and wait for the other points to be worked out ( a dangerous game adding to a list we are iterating through)
                            if (safety > 5)
                            {
                                //just in case
                                Debug.Log("Safety kicked in, adding to list as iterating");
                                return;
                            }

                            ringPoints.Add(ringPoints[i]);
                            ringPoints.RemoveAt(i);
                            i--;
                            continue;
                        }

                        Vector3 intersectV3 = IntersectNext(prev, i, next, true);
                        tempList.Add(intersectV3);

                    }
                    else if (doorEdgePos == ringPoints[next])
                    {
                        Vector3 intersectV3 = IntersectNext(prev, i, next,false);
                        tempList.Add(intersectV3);
                    }
                }
                else
                {
                    
                    c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    c.transform.position = ringPoints[i];
                    c.name = "exterior door = false non hall non";

                    Vector3 slide = ringPoints[i] + (ringPoints[prev] - ringPoints[i]).normalized * hallSize;
                    // tempList.Add(slide)

                    Vector3 miter = MiterDirection(ringPoints[prev], ringPoints[i], ringPoints[next], hallSize);

                    c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    c.transform.position =ringPoints[i] - miter.normalized*hallSize;
                    c.name = "exterior door = miter";

                    //WORKIN ON THIS

                    tempList.Add(ringPoints[i]);
                }
            }
            else if (ringPoints[prev].y > 0f && ringPoints[i].y == 0f && ringPoints[next].y == 0f)
            {

                GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                c.transform.position = ringPoints[prev];
                c.name = "hall , non , non";

                c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                c.transform.position = ringPoints[i];
                c.name = i.ToString();

                c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                c.transform.position = ringPoints[next];
                c.name = next.ToString();

                if (makeExteriorDoor)
                {
                    if (doorEdgePos == ringPoints[i])
                    {
                        c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        c.transform.position = ringPoints[i];
                        c.name = "door edge ==";

                        //we need to find where the hall intersects

                        //use direction of original edge
                       

                        //use previous point - is it just last in templist? //out of index possible?
                        if(tempList.Count == 0)
                        {
                            c.name = "list = 0,";//special case
                            //in order to work out this point we need the point before it to be have been worked out. let's add this point to the end of the list and wait for the other points to be worked out ( a dangerous game adding to a list we are iterating through)
                            if (safety > 5)
                            {
                                //just in case
                                Debug.Log("Safety kicked in, adding to list as iterating");
                                return;
                            }                           

                            ringPoints.Add(ringPoints[i]);
                            ringPoints.RemoveAt(i);
                            i--;
                            continue;
                        }


                        Vector3 intersectV3 = IntersectNext(prev, i, next, true);
                        tempList.Add(intersectV3);
                    }
                    else
                    {
                        c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        c.transform.position = ringPoints[i];
                        c.name = "door edge != ";

                        tempList.Add(ringPoints[i]);
                    }
                }
                else
                {
                    //not tested
                    c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    c.transform.position = ringPoints[i];
                    c.name = "exterior door = false";

                    tempList.Add(ringPoints[i]);
                }

            }
        }

        Cell(tempList);
    }

    Vector3 IntersectNext(int prev, int i, int next, bool flipDir)
    {
        //find where a miter point plus a direction intersects with target points

        Vector3 miter = MiterDirection(ringPoints[prev], ringPoints[i], ringPoints[next], hallSize);

        //now project to back to find intersect (we are sliding the whole edge)

        
        Vector3 edgeDir = (ringPoints[i] - ringPoints[next]);
        if(flipDir)
            edgeDir = (ringPoints[i] - ringPoints[prev]);//other

        Vector3 p2 = ringPoints[i] - miter;
        Vector3 p3 = p2 + edgeDir;

        Debug.DrawLine(p2, p3, Color.magenta);
        Vector2 intersectV2 = Vector2.zero;
        for (int aa = i + 1, bb = 0; bb < targetPoints.Count; bb++, aa--)
        {
            if (aa > targetPoints.Count - 1)
                aa -= targetPoints.Count;

            if (aa < 0)
                aa += targetPoints.Count;

            int aaPrev = aa - 1;
            if (aaPrev < 0)
                aaPrev += targetPoints.Count;



            Vector3 p0 = targetPoints[aaPrev];
            Vector3 p1 = targetPoints[aa];

            Debug.DrawLine(p2, p3, Color.yellow);


            Vector2 a0 = new Vector2(p0.x, p0.z);
            Vector2 a1 = new Vector2(p1.x, p1.z);
            Vector2 a2 = new Vector2(p2.x, p2.z);
            Vector2 a3 = new Vector2(p3.x, p3.z);


            if (LineSegmentsIntersection(a0, a1, a2, a3, out intersectV2))
            {
                Vector3 intersectV3 = new Vector3(intersectV2.x, 0f, intersectV2.y);

                return intersectV3;



            }
        }

        return Vector3.zero;
    }

    public GameObject Cell(List<Vector3> newPoints)
    {
        GetComponent<MeshRenderer>().enabled = false;
       // newPoints = newPoints.Distinct().ToList();//.linq

        Vector3 avg = Vector3.zero;
        bool doCubes = true;

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
                        // newPoints[a] = flat;
        }
        avg /= newPoints.Count;


        newPoints.Insert(0, avg);

        //recentre so mesh is local 
        for (int i = 0; i < newPoints.Count; i++)
        {
            newPoints[i] -= transform.position;
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
        roomFloor.transform.position += Vector3.up * 10;///888test
        roomFloor.name = "RoomFloor";
        roomFloor.AddComponent<MeshRenderer>().sharedMaterial = Resources.Load("Grey") as Material;
        MeshFilter mf = roomFloor.AddComponent<MeshFilter>();
        Mesh mesh = new Mesh();
        mesh.vertices = newPoints.ToArray();
        mesh.triangles = trianglesL.ToArray();

        //mesh = ExtrudeCell.Extrude(mesh, 3f, 1f, true);

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        mf.mesh = mesh;







        return roomFloor;

    }

    //helper functions
    Vector3 MiterDirection(Vector3 p0, Vector3 p1, Vector3 p2, float borderSize)
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
