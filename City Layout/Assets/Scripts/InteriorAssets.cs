using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
public class InteriorAssets : MonoBehaviour
{
    public static GameObject ApartmentDoor(Transform transform, Vector3 centre, Vector3 p0, float doorWidth, float doorHeight,float doorDepth,Material material) //move to hallways?
    {

        
        GameObject door = GameObject.CreatePrimitive(PrimitiveType.Cube);
        //zero all y 
        centre = new Vector3(centre.x, 0f, centre.z);
        p0 = new Vector3(p0.x, 0f, p0.z);
        // p1 = new Vector3(p1.x, 0f, p1.y);


        door.transform.position = centre;
        //look along edge
        door.transform.LookAt(p0);
        //now make height same as floor
       
        door.transform.rotation = Quaternion.Euler(0, 90, 0) * door.transform.rotation;

        //now alter door position and scale
        
     
        

        door.transform.localScale = new Vector3(doorWidth, doorHeight, doorDepth);
       
       


        door.GetComponent<MeshRenderer>().sharedMaterial = material;//randomise?
         

        door.name = "Door";



        return door;
        

        
    }

    public static GameObject ApartmentDoorFrame(Vector3 centre, Vector3 p0,Vector3 extrudeDir, float doorHeight, float doorWidth, float wallDepth,TraditionalSkyscraper tS,bool flipDir)
    {
        GameObject wall = new GameObject();
        wall.name = "Door Frame";

        //pX is out frame points
        //aX is inside


        float ceilingHeight = tS.floorHeight * (1f - tS.spacerHeight * 2);

        Vector3 p1 = p0 + ceilingHeight*Vector3.up; //1f - spacer height 2 because door is already on spacer height and roof is a sapcer height multiple of floor height
                                                                                      

        Vector3 p2 = centre + ceilingHeight * Vector3.up;

        //    c = GameObject.CreatePrimitive(PrimitiveType.Cube);
        //    c.transform.position = p2;
        //    c.transform.localScale *= 0.5f;
        //    c.name = "p2";



        //inner
        //from centre to edge multiplied by door width/2 gives us where the door finishes and thus where the frame begins
        Vector3 a0 = centre + (p0 - centre).normalized * doorWidth * .5f;

        Vector3 a1 = a0 + doorHeight* Vector3.up;

        Vector3 a2 = centre + doorHeight * Vector3.up;


        Vector3[] verticesFront = new Vector3[]
        {
            p0, p1, a0,
            a0, p1 ,a1,
            p1, p2, a1,
            a1, p2, a2
        };

        //now extrude
        //extruding the points around the door is easy, just use the door's rotation
        //however we need to find the intersect along the angled wall to get the correct position for the extruded vertices- we are edge sliding along the wall

        List<Vector3> verticeList = new List<Vector3>(verticesFront);
        Vector3 doorExtrudeDir = Quaternion.Euler(0, -90, 0) * (p0 - centre).normalized;

        if(!flipDir)
            doorExtrudeDir = Quaternion.Euler(0, 90, 0) * (p0 - centre).normalized;

        Vector3 centreExtruded = centre + doorExtrudeDir * wallDepth;
        Vector3 intersectLine =centre +  (p0 - centre).normalized * 100;
        Vector3 corner = p0;
        Vector3 dirFromCorner = p0 + extrudeDir*100;

        Vector2 q0 = new Vector2(centreExtruded.x, centreExtruded.z);
        Vector2 q1 = new Vector2(intersectLine.x, intersectLine.z);
        Vector2 q2 = new Vector2(corner.x, corner.z);
        Vector2 q3 = new Vector2(dirFromCorner.x, dirFromCorner.z);

        Vector2 intersect;

        Debug.DrawLine(centreExtruded, intersectLine);
        Debug.DrawLine(corner, dirFromCorner,Color.blue);
        if (Hallways.LineSegmentsIntersection(q0,q1,q2,q3,out intersect))
        {
           // GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
           // c.transform.position = new Vector3(intersect.x, 0f, intersect.y);
          //  c.name = "interz";
        }
        Vector3 wallEdgeDir = new Vector3(intersect.x, 0f, intersect.y) - corner;

        int initialLength = verticesFront.Length;
        for (int i = 0; i < verticesFront.Length; i++)
        {
           

            //outside wall point, extrude with extrudeDir,
            if (verticesFront[i] == p0 || verticesFront[i] == p1)
            {
                //note extrude dir is not normalized as it is found from an intersection test 
                verticeList.Add(verticesFront[i] + wallEdgeDir);
              
            }
            else
            {
                //if inside (at door, extrude 90 degrees to door
                verticeList.Add(verticesFront[i] + doorExtrudeDir * wallDepth);
              
            }
            
        }

        //add inside door frame verts
        verticeList.Add(a0);
        verticeList.Add(a1);
        verticeList.Add(a0 + doorExtrudeDir*wallDepth);
        verticeList.Add(a1 + doorExtrudeDir * wallDepth);
        verticeList.Add(a2);
        verticeList.Add(a2 + doorExtrudeDir * wallDepth);
        //now add verts for insie

        List<int> triangles = new List<int>
        {
            0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, //front

            23,22,21,20,19,18,17,16,15,14,13,12,//back reverse

            //inside
            24,25,26,27,26,25,

            //top inside
            25,28,27,27,28,29

        };


        if(flipDir)
            triangles.Reverse();

       

        Mesh mesh = new Mesh();
        mesh.vertices = verticeList.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        //need to recentre mesh

        MeshFilter mF = wall.AddComponent<MeshFilter>();
        mF.mesh = mesh;
        

        return wall;
    }

    public static GameObject BookendWall(Vector3 centre, Vector3 p0, Vector3 extrudeDir ,float wallDepth,float floorHeight, bool flipDir)
    {
        GameObject wall = new GameObject();
        
        //pX is out frame points
        //aX is inside


        Vector3 p1 = p0 + floorHeight* Vector3.up; //1f - spacer height 2 because door is already on spacer height and roof is a sapcer height multiple of floor height


       // Vector3 p2 = centre + (tS.floorHeight * (1f - tS.spacerHeight * 2) * Vector3.up);

        //    c = GameObject.CreatePrimitive(PrimitiveType.Cube);
        //    c.transform.position = p2;
        //    c.transform.localScale *= 0.5f;
        //    c.name = "p2";



        //inner
        //from centre to edge multiplied by door width/2 gives us where the door finishes and thus where the frame begins
        Vector3 a0 = centre;// + (p0 - centre).normalized * wallDepth * .5f;

        Vector3 a1 = a0 + floorHeight * Vector3.up;



        Vector3[] verticesFront = new Vector3[]
        {
            p0, p1, a0,
            a0, p1 ,a1,
           // p1, p2, a1
        };

        //now extrude
        //extruding the points around the door is easy, just use the door's rotation
        //however we need to find the intersect along the angled wall to get the correct position for the extruded vertices- we are edge sliding along the wall

        List<Vector3> verticeList = new List<Vector3>(verticesFront);
        Vector3 doorExtrudeDir = Quaternion.Euler(0, -90, 0) * (p0 - centre).normalized;

        if (!flipDir)
            doorExtrudeDir = Quaternion.Euler(0, 90, 0) * (p0 - centre).normalized;

        Vector3 centreExtruded = centre + doorExtrudeDir * wallDepth;
        Vector3 intersectLine = centre + (p0 - centre).normalized * 100;
        Vector3 corner = p0;
        Vector3 dirFromCorner = p0 + extrudeDir * 100;

        Vector2 q0 = new Vector2(centreExtruded.x, centreExtruded.z);
        Vector2 q1 = new Vector2(intersectLine.x, intersectLine.z);
        Vector2 q2 = new Vector2(corner.x, corner.z);
        Vector2 q3 = new Vector2(dirFromCorner.x, dirFromCorner.z);

        Vector2 intersect;

        Debug.DrawLine(centreExtruded, intersectLine);
        Debug.DrawLine(corner, dirFromCorner, Color.blue);
        if (Hallways.LineSegmentsIntersection(q0, q1, q2, q3, out intersect))
        {
             GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
             c.transform.position = new Vector3(intersect.x, 0f, intersect.y);
              c.name = "interz";
        }
        Vector3 wallEdgeDir = new Vector3(intersect.x, 0f, intersect.y) - corner;
        for (int i = 0; i < verticesFront.Length; i++)
        {
            //outside wall point, extrude with extrudeDir,
            if (verticesFront[i] == p0 || verticesFront[i] == p1)
            {
                //note extrude dir is not normalized as it is found from an intersection test 
                verticeList.Add(verticesFront[i] + wallEdgeDir);
            }
            else
            {
                //if inside (at door, extrude 90 degrees to door
                verticeList.Add(verticesFront[i] + doorExtrudeDir * wallDepth);
            }

        }

        List<int> triangles = new List<int>
        {
            0, 1, 2, 3, 4, 5,//, 6, 7, 8//, 9,// 10, 11, //front
            11,10,9,8,7,6
            //23,22,21,20,19,18,17,16,15,14,13,12//back reverse


            //16 start of door frame bottom
            //17 rear fram bottom
            //13, rear bottom

        };


        if (flipDir)
            triangles.Reverse();



        Mesh mesh = new Mesh();
        mesh.vertices = verticeList.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        //need to recentre mesh

        MeshFilter mF = wall.AddComponent<MeshFilter>();
        mF.mesh = mesh;


        return wall;
    }

    public static GameObject Wall(Vector3 p0, Vector3 p1,Vector3 p2, Vector3 p3,float height)
    {
        GameObject wall = new GameObject();

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
}
