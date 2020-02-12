using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
public class InteriorAssets : MonoBehaviour
{
    public static GameObject ApartmentDoor(Vector3 centre, Vector3 p0, float doorWidth, float doorHeight,float doorDepth,bool flip) //move to hallways?
    {

        //will hang on hinges for pivot
        GameObject parent = new GameObject();
        
        GameObject door = GameObject.CreatePrimitive(PrimitiveType.Cube);
        door.transform.parent = parent.transform;
        door.name = "Door";

        //zero all y 
        centre = new Vector3(centre.x, 0f, centre.z);
        p0 = new Vector3(p0.x, 0f, p0.z);
        // p1 = new Vector3(p1.x, 0f, p1.y);


        parent.transform.position = centre;
        //look along edge
        parent.transform.LookAt(p0);
        //now make height same as floor
       
        parent.transform.rotation = Quaternion.Euler(0, 90, 0) * door.transform.rotation;
        parent.transform.localScale = new Vector3(doorWidth, doorHeight, doorDepth);

        door.transform.position = parent.transform.position + doorHeight*Vector3.up*.5f;
        door.transform.position -= door.transform.right * doorWidth * .5f;

        if(flip)
        door.transform.position -= door.transform.forward * doorDepth * .5f;
        else
            door.transform.position += door.transform.forward * doorDepth * .5f;






        return parent;
        

        
    }

    public static GameObject ApartmentDoorFrame(out bool flip, Vector3 centre, Vector3 a0,Vector3 a1, Vector3 a2,Vector3 a3, float doorHeight, float doorWidth,float wallDepth, TraditionalSkyscraper tS,Vector3 boundsCentre)//,bool flipDir)
    {
        GameObject wall = new GameObject();
        wall.name = "Door Frame";

        

        //decide which way to build, (towatrds centre of room)
        bool flipTemp = false;

        float angleDir = AngleDir((a2 - a3).normalized, (boundsCentre - centre).normalized, Vector3.up);
        if (angleDir > 0)
            flipTemp = true;

        //ax is bottom
        //px is top


        float ceilingHeight = tS.floorHeight * (1f - tS.spacerHeight * 2);

        Vector3 p0 = a0 + ceilingHeight * Vector3.up; //1f - spacer height 2 because door is already on spacer height and roof is a sapcer height multiple of floor height
        Vector3 p1 = a1 + ceilingHeight * Vector3.up;//top left
        Vector3 p2 = a2 + ceilingHeight * Vector3.up;//top right
        Vector3 p3 = a3 + ceilingHeight * Vector3.up;

        //low
        Vector3 door0Rear = centre + (a2 - a3).normalized * doorWidth*.5f;
        //inside high
        Vector3 door1Rear = door0Rear + Vector3.up * doorHeight;

        //low other side
        Vector3 door2Rear = centre - (a2 - a3).normalized * doorWidth * .5f;
        //high other side
        Vector3 door3Rear = door2Rear + Vector3.up * doorHeight;

        //rear side
        
        int spin = 90;
        if (flipTemp)
            spin = -90;

        Vector3 door0 = door0Rear + Quaternion.Euler(0, spin, 0) * ((a2 - a3).normalized * wallDepth);//take direction then spin and multiply by wall depth
        Vector3 door1 = door1Rear + Quaternion.Euler(0, spin, 0) * ((a2 - a3).normalized * wallDepth);//take direction then spin and multiply by wall depth
        Vector3 door2 = door2Rear + Quaternion.Euler(0, spin, 0) * ((a2 - a3).normalized * wallDepth);//take direction then spin and multiply by wall depth
        Vector3 door3 = door3Rear + Quaternion.Euler(0, spin, 0) * ((a2 - a3).normalized * wallDepth);//take direction then spin and multiply by wall depth
        
        /*
        GameObject    c = GameObject.CreatePrimitive(PrimitiveType.Cube);
            c.transform.position = centre;
            c.transform.localScale *= 0.5f;
            c.name = "a0";

        c = GameObject.CreatePrimitive(PrimitiveType.Cube);
        c.transform.position = a0;
        c.transform.localScale *= 0.5f;
        c.name = "a0";

        c = GameObject.CreatePrimitive(PrimitiveType.Cube);
        c.transform.position = a1;
        c.transform.localScale *= 0.5f;
        c.name = "a1";
        c = GameObject.CreatePrimitive(PrimitiveType.Cube);
        c.transform.position = a2;
        c.transform.localScale *= 0.5f;
        c.name = "a2";
        c = GameObject.CreatePrimitive(PrimitiveType.Cube);
        c.transform.position = a3;
        c.transform.localScale *= 0.5f;
        c.name = "a3";
        */


        //inner
        //from centre to edge multiplied by door width/2 gives us where the door finishes and thus where the frame begins



        Vector3[] verticesFront = new Vector3[]
        {
            door0,p2, door1,
            a2, p2 ,door0,
            //above door
            door1,p2,p3,
            door1,p3,door3,
            //other side
            door3,p3,a3,
            door2,door3,a3,

            //inside door frame
            door1,door0Rear,door0,
            door0Rear,door1,door1Rear,
            door1Rear,door1,door3,
            door3Rear,door1Rear,door3,
            door3Rear,door3,door2Rear,
            door3,door2,door2Rear,

            //rear
            a0,door0Rear,p0,
            door0Rear,door1Rear,p0,
            door3Rear,p0,door1Rear,
            p1,p0,door3Rear,
            door2Rear,a1,door3Rear,
            p1,door3Rear,a1,

            //top can be removed
            p0,p1,p2,p1,p3,p2
        };

        //now extrude
        //extruding the points around the door is easy, just use the door's rotation
        //however we need to find the intersect along the angled wall to get the correct position for the extruded vertices- we are edge sliding along the wall

        List<Vector3> verticeList = new List<Vector3>(verticesFront);
        Vector3 doorExtrudeDir = Quaternion.Euler(0, -90, 0) * (p0 - centre).normalized;

       // if(!flipDir)
        //    doorExtrudeDir = Quaternion.Euler(0, 90, 0) * (p0 - centre).normalized;

       

        int initialLength = verticesFront.Length;
        for (int i = 0; i < verticesFront.Length; i++)
        {
           

            //outside wall point, extrude with extrudeDir,
            if (verticesFront[i] == p0 || verticesFront[i] == p1)
            {
                //note extrude dir is not normalized as it is found from an intersection test 
             //   verticeList.Add(verticesFront[i] + wallEdgeDir);
              
            }
            else
            {
                //if inside (at door, extrude 90 degrees to door
               // verticeList.Add(verticesFront[i] + doorExtrudeDir * wallDepth);
              
            }
            
        }


        List<int> triangles = new List<int>
        {
            //front
            0, 1, 2, 3, 4, 5, 6, 7, 8,9, 10, 11,12,13,14,15,16,17,

            //inside
            18,19,20,21,22,23,24,25,26,27,28,29,30,31,32,33,34,35,
          
            //rear
            36,37,38,39,40,41,42,43,44,45,46,47,48,49,50,51,52,53,

            //top optional
            54,55,56,57,58,59

        };


        if(!flipTemp)
            triangles.Reverse();

       

        Mesh mesh = new Mesh();
        mesh.vertices = verticeList.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        //need to recentre mesh

        MeshFilter mF = wall.AddComponent<MeshFilter>();
        mF.mesh = mesh;


        flip = flipTemp;
        return wall;
    }


    public static GameObject WindowFrames(List<int> windowIndexes, List<List<Vector3>> buildingVertices, GameObject parent,TraditionalSkyscraper tS)
    {

        //let's add all frames to one gameobject- will reduce draw calls and we will have better frames per second

        GameObject window = new GameObject();
        window.transform.parent = parent.transform;
        window.name = "Windows";
        MeshFilter meshFilter = window.AddComponent<MeshFilter>();
        

        Mesh mesh = new Mesh();
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        //create 

        for (int i = 0; i < windowIndexes.Count; i+=2)
        {
                /*
                GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                c.transform.position = buildingVertices[windowPoints[i][j]];
                c.transform.localScale *= 0.1f;
                c.name = i.ToString() + " " + j.ToString();
                Destroy(c, 3);
                */

                

                //front panel

                Vector3 thisPoint = buildingVertices[3][windowIndexes[i]];
                Vector3 next = buildingVertices[3][windowIndexes[i+1]];

            GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
            c.transform.position = thisPoint;
            c.transform.localScale *= 0.1f;
            c.name = "window0";

            c = GameObject.CreatePrimitive(PrimitiveType.Cube);
            c.transform.position = next;
            c.transform.localScale *= 0.1f;
            c.name = "window1";

            // Vector3 nextNext = buildingVertices[windowPoints[i][nextNextIndex]];
            //some directions we will need 
            //extrude towards start

            /*
                Vector3 side = ((thisPoint - next).normalized);
                Vector3 forward = Vector3.Cross(backwards, side);
                //bottom of window


                //extrude downards //and back //and forward
                Vector3 p0 = thisPoint + forward * tS.windowFrameDepth;
                Vector3 p1 = next + forward * tS.windowFrameDepth;
                Vector3 p2 = thisPoint + side * tS.windowFrameSize + backwards * tS.windowFrameSize + forward * (tS.windowFrameDepth + tS.windowFrameDepthOuter);
                Vector3 p3 = next + side * tS.windowFrameSize - backwards * tS.windowFrameSize + forward * (tS.windowFrameDepth + tS.windowFrameDepthOuter);

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
                p2 = p0 - forward * (tS.windowFrameDepth + tS.windowFrameDepthOuter);
                p3 = p1 - forward * (tS.windowFrameDepth + tS.windowFrameDepthOuter);

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


            
        */

        }

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        meshFilter.mesh = mesh;

        return window;

    }

    public static List<List<int>> TrianglesForExteriorWall(List<List<Vector3>> meshPoints,List<int> windowIndexes)
    {
        //triangles
        List<List<int>> triangles = new List<List<int>>();
        triangles.Add(new List<int>());
        triangles.Add(new List<int>());
        triangles.Add(new List<int>());

        List<int> material0 = new List<int>();
        List<int> material1 = new List<int>();
        List<int> material2 = new List<int>();
        for (int i = 0; i < meshPoints.Count - 1; i++)
        {
            for (int j = 0; j < meshPoints[i].Count-1; j++)//?working?, was +=2
            {
                //go!

                int first = j + i * meshPoints[i].Count;
                int second = j + 1 + i * meshPoints[i].Count;
                int third = j + meshPoints[i].Count + (i * meshPoints[i].Count);
                int fourth = j + 1 + meshPoints[i].Count + (i * meshPoints[i].Count);

                if (i < 2 || i > 7)
                {
                    //spacer mat is 0
                    material0.Add(first);
                    material0.Add(second);
                    material0.Add(third);

                    material0.Add(third);
                    material0.Add(second);
                    material0.Add(fourth);
                }
                else if (i != 4 && i != 5)
                {
                    //main building mat is 1
                    material1.Add(first);
                    material1.Add(second);
                    material1.Add(third);

                    material1.Add(third);
                    material1.Add(second);
                    material1.Add(fourth);
                }
                else
                {
                    if (windowIndexes.Contains(j - 1) && windowIndexes.Contains(j + 1)) //not how i planned but it's working!
                    {
                        //window mat is 2
                        material2.Add(first);
                        material2.Add(second);
                        material2.Add(third);

                        material2.Add(third);
                        material2.Add(second);
                        material2.Add(fourth);
                    }
                    else

                    {
                        //main building mat is 1
                        material1.Add(first);
                        material1.Add(second);
                        material1.Add(third);

                        material1.Add(third);
                        material1.Add(second);
                        material1.Add(fourth);

                    }
                }

                /*
                 GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                  c.transform.position = meshPoints[i][j];
                  c.name = "mesh";
                  c.transform.localScale *= 0.2f;
                  */


            }
        }

        triangles[0].AddRange(material0);
        triangles[1].AddRange(material1);
        triangles[2].AddRange(material2);

        return triangles;
    }
    
    public static GameObject Wall(Vector3 p0, Vector3 p1,Vector3 p2, Vector3 p3,float height,TraditionalSkyscraper tS)
    {
        GameObject wall = new GameObject();

       

        Vector3[] vertices = new Vector3[0];
        List<int> triangles = new List<int>();
       
        
            //just use passed points to create a wall

            Vector3 a0 = p0 + height * Vector3.up;
            Vector3 a1 = p1 + height * Vector3.up;
            Vector3 a2 = p2 + height * Vector3.up;
            Vector3 a3 = p3 + height * Vector3.up;

            vertices = new Vector3[]
            {
                //front
                p0, p1, a0,
                a0, p1 ,a1,

                //rear
                p2, p3, a2,
                a2, p3 ,a3,

                //top optional. looks nicer for cross sections

                a0,a1,a2,a3,a2,a1
            };

           triangles= new List<int>
           {
                0, 1, 2, 3, 4, 5,
                11, 10, 9, 8, 7, 6,
                //top optional
                12,13,14,15,16,17
            };
        
        Mesh mesh = new Mesh();
        mesh.vertices = vertices;// verticeList.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        //need to recentre mesh

        MeshFilter mF = wall.AddComponent<MeshFilter>();
        mF.mesh = mesh;

        return wall;
    }

    public static GameObject WallBookend(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float height,Vector3 boundsCentre)
    {
        GameObject wall = new GameObject();

        bool flip = false;

        float angleDir = AngleDir((p2 - p3).normalized, (boundsCentre - Vector3.Lerp(p2,p3,0.5f)).normalized, Vector3.up);
        if (angleDir < 0)
            flip = true;


        Vector3 a0 = p0 + height * Vector3.up;
        Vector3 a1 = p1 + height * Vector3.up;
        Vector3 a2 = p2 + height * Vector3.up;
        Vector3 a3 = p3 + height * Vector3.up;

        Vector3[] vertices = new Vector3[]
       {
           //front
            p0, p1, a0,
            a0, p1 ,a1,

            //rear
            p2, p3, a2,
            a2, p3 ,a3,

            //top optional. looks nicer for cross sections

            a0,a1,a2,a3,a2,a1
       };

        List<int> triangles = new List<int>
        {
            0, 1, 2, 3, 4, 5,
            11, 10, 9, 8, 7, 6,
           
          
            //top optional
            12,13,14,15,16,17

        };

        if (flip)
            triangles.Reverse();

        Mesh mesh = new Mesh();
        mesh.vertices = vertices;// verticeList.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        //need to recentre mesh

        MeshFilter mF = wall.AddComponent<MeshFilter>();
        mF.mesh = mesh;

        return wall;
    }

    public static float AngleDir(Vector3 fwd, Vector3 targetDir, Vector3 up)
    {
        Vector3 perp = Vector3.Cross(fwd, targetDir);
        float dir = Vector3.Dot(perp, up);

        if (dir > 0.0f)
        {
            return 1.0f;
        }
        else if (dir < 0.0f)
        {
            return -1.0f;
        }
        else
        {
            return 0.0f;
        }
    }
}
