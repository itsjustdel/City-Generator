using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoundedPolygonMaker : MonoBehaviour {

    //http://mathworld.wolfram.com/RoundedRectangle.html //from this idea


    public static List<Vector3> AddRing(out List<List<int>> trianglesReturned,out int verticesInARing,GameObject gameObject, List<Vector3> vertices,List<List<int>> trianglesList, Vector3 targetDirection,int sides, float size, float segmentHeight,float roundnessSize,float roundnessDetail, float tolerance, int ringsSofar,int spinSegment,bool forLedge,bool glassType,bool addWindows,bool addCornerWindowFrames,bool forSpacer, float floorHeight, int windowSizeX ,int windowFrameSizeX, int layerType)
    {
        
        //layertypes 
        //0 - main outer
        //1 - window
        //2 - spacer

        int y = ringsSofar;
        //how many corners our shape has
        float coolChange = 2f;//just a cool way to make new shapes
        float step = (Mathf.PI * coolChange) / sides;

        //this loop creates points arounda  circle. The half step starting point is to create a an equal pattern throughout the rings as they go upwards

        //keep a count of vertices for a ring- i could work this out but this is easier
        int verticesThisRing = 0;

        List<int> trianglesForWindows = new List<int>();

        for (float i = 0, j = 0; i < Mathf.PI * 2 + tolerance ; i += step, j++) //a circle split in to segments ( + tolerance if pi is divided by ten, it needs this tolerance
        {
            Vector3 point = GetPerpendicularAtAngle(targetDirection, i);
            Vector3 innerPoint = point * (size - roundnessSize);
            //add segment height
            // point += targetDirection * segmentHeight;//not using
            innerPoint += targetDirection * (segmentHeight);

            
            GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
            c.transform.position = point * size;
            c.transform.localScale *= 0.2f;

            c = GameObject.CreatePrimitive(PrimitiveType.Cube);
            c.transform.position = innerPoint;
            c.transform.localScale *= 0.1f;
            
            float roundnessStep = step / (roundnessDetail * 2);

            float endAt = ((Mathf.PI * 2) / sides) / 2;
            //a fraction multiplied by what corner we are on math.pi*2 is a full circle, split it by sides, and multiply by j (keeps track of what cornder we are on)
            float spin = ((Mathf.PI * 2) / sides) * j;

            float stepForWindow = 1f / windowSizeX;
            float stepForFrame = stepForWindow / windowFrameSizeX;

            if (i > 0)
            {
                Vector3 lastPoint = vertices[vertices.Count - 1];

                Vector3 circlePoint = GetPerpendicularAtAngle(targetDirection, -endAt + spin);
                circlePoint *= roundnessSize;


                /*
                GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                c.transform.position = lastPoint;
                c.transform.localScale *= 0.5f;
                c.name = "last point";

                c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                c.transform.position = circlePoint + innerPoint;
                c.transform.localScale *= 0.5f;
                c.name = "next point";
                */

                //float distance = Vector3.Distance(lastPoint, circlePoint + innerPoint);
                //Vector3 dirToNext = ((circlePoint + innerPoint) - lastPoint)/(roundnessSize*2);

              
                //create windows along flat side              

                for (float k = 0; k <= 1f - stepForWindow+tolerance; k += stepForWindow)
                {
                    //first window frame
                    //the first point has laready been put in by previous side                        
                    //remember for material/triangles submesh allocation if 

                    if (addWindows)
                        trianglesForWindows.Add(verticesThisRing);

                    Vector3 p0 = Vector3.Lerp(lastPoint, circlePoint + innerPoint, k + stepForFrame);
                    vertices.Add(p0);
                    verticesThisRing++;

                    /*
                     GameObject c0 = GameObject.CreatePrimitive(PrimitiveType.Cube);
                     c0.transform.position = p0;// - yHeight*targetDirection*a;
                     c0.transform.localScale *= 0.05f;
                     c0.name = "p point 0 " + y.ToString();
                     */
                    //second window frame
                    Vector3 p1 = Vector3.Lerp(lastPoint, circlePoint + innerPoint, k + stepForWindow - stepForFrame);
                    //second window frame

                    vertices.Add(p1);
                    verticesThisRing++;

                        
                    /*
                      GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                      c.transform.position = p1;// - yHeight*targetDirection*a;
                      c.transform.localScale *= 0.05f;
                      c.name = "p point 1 " + y.ToString();
                      */
                    //***************took middle point out for window frames.. needed?
                    //end point

                    //dont put last point, corner loop puts this in ??sometimes works, sometimes doenst, so suplicates might be a thing
                    //if (k == 1f - stepForWindow)
                    //    continue;

                    Vector3 p2 = Vector3.Lerp(lastPoint, circlePoint + innerPoint, k + stepForWindow);
                    /*
                      c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                      c.transform.position = p2;// - yHeight*targetDirection*a;
                      c.transform.localScale *= 0.05f;
                      c.name = "p point 2 " + y.ToString();
                      */  
                    vertices.Add(p2);
                    verticesThisRing++;

                    //if (addWindows)
                    //    trianglesForWindows.Add(verticesThisRing);


                    bool balconyWindows = true;

                    if(balconyWindows && y==2)
                    {
                   //     BalconyWindow(p0, p1,gameObject,tolerance,i);
                    }

                }

                
            }
            //dont do on last
            if (i < Mathf.PI * 2 + tolerance - step)
            {
                Vector3 firstPoint = Vector3.zero;
                for (float k = -endAt + spin; k < endAt + spin + tolerance; k += roundnessStep)
                {
                    //first index
                    if (k == -endAt + spin)
                    {
                        Vector3 circlePoint = GetPerpendicularAtAngle(targetDirection, k);
                        circlePoint *= roundnessSize;
                        firstPoint = circlePoint + innerPoint;


                        /*
                        GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        c.transform.position = innerPoint + circlePoint;
                        c.transform.localScale *= 0.05f;
                        c.name = "p point round first" + y.ToString(); ;
                        */
                        vertices.Add(circlePoint + innerPoint);

                        verticesThisRing++;


                    }
                    else
                    {

                        //work our way towards the next point
                        //if first point in the circle, use the saved point from first index
                        // Vector3 lastPoint = firstPoint;
                        // if(k > -endAt + spin + roundnessStep)//this will be the second in loop - im aware this is stupid
                        Vector3 lastPoint = vertices[vertices.Count - 1];

                        Vector3 circlePoint = GetPerpendicularAtAngle(targetDirection, k);
                        circlePoint *= roundnessSize;
                        float stepForCornerWindow = 1f / 1;
                        float stepForCornerFrame = stepForCornerWindow / windowFrameSizeX;
                       // float start = 1f - stepForCornerWindow;

                        //I'm not sure this is exactly working how I think it is. changin start and points on loop doesn't have expected outcomes
                        for (float a = 0f; a <= 1f - stepForCornerWindow + tolerance; a += stepForCornerWindow)
                        {
                            //first window frame


                            if (addCornerWindowFrames)
                                //if(!forSpacer)
                                trianglesForWindows.Add(verticesThisRing);

                            Vector3 p0 = Vector3.Lerp(lastPoint, circlePoint + innerPoint, a + stepForCornerFrame);


                            vertices.Add(p0);
                            verticesThisRing++;
                            /*
                            GameObject c0 = GameObject.CreatePrimitive(PrimitiveType.Cube);
                            c0.transform.position = p0;// - yHeight*targetDirection*a;
                            c0.transform.localScale *= 0.05f;
                            c0.name = "p point round 0 " + y.ToString();
                            */
                            //second window frame
                            Vector3 p1 = Vector3.Lerp(lastPoint, circlePoint + innerPoint, a + stepForCornerWindow - stepForCornerFrame);
                            //second window frame


                            vertices.Add(p1);
                            verticesThisRing++;
                            //if (addCornerWindowFrames)
                            //    trianglesForWindows.Add(verticesThisRing);


                            Vector3 p2 = Vector3.Lerp(lastPoint, circlePoint + innerPoint, a + stepForCornerWindow);
                            /*
                            GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                            c.transform.position = p2;// - yHeight*targetDirection*a;
                            c.transform.localScale *= 0.05f;
                            c.name = "p point round 1 " + y.ToString();
                            */
                            vertices.Add(p2);
                            verticesThisRing++;

                            //if (addCornerWindowFrames)
                            //   trianglesForWindows.Add(verticesThisRing);

                        }

                    }
                }
            }
            
        }
        
        //triangles, build back the way, from second ring to first
        if (y > 0)
        {
            //move triangle indexes depending on spin
            int moveIndexBy = 0;
            if (spinSegment > 0)                
                moveIndexBy = (verticesThisRing) / (90/ (spinSegment)); //90/spin works perfect for the first spin, after more twists it gets stretched

            for (int a = 0; a < verticesThisRing - 1; a++)
            {
                //lining up with lower ring as best as we can
                int indiceForLower = a + moveIndexBy;

                if (indiceForLower >= verticesThisRing - 1)
                    indiceForLower -= verticesThisRing - 1;

                if (!forLedge)
                    indiceForLower = a;

                int target = layerType;

                if (trianglesForWindows.Contains(a))
                {
                    //set to window material
                    trianglesList[1].Add(a + (y * verticesThisRing));
                    trianglesList[1].Add((indiceForLower + 1) + ((y - 1) * verticesThisRing));
                    trianglesList[1].Add(a + 1 + (y * verticesThisRing));

                    trianglesList[1].Add(a + (y * verticesThisRing));
                    trianglesList[1].Add((indiceForLower) + ((y - 1) * verticesThisRing));
                    trianglesList[1].Add((indiceForLower + 1) + ((y - 1) * verticesThisRing));
                }
                else
                {

                    trianglesList[target].Add(a + (y * verticesThisRing));
                    trianglesList[target].Add((indiceForLower + 1) + ((y - 1) * verticesThisRing));
                    trianglesList[target].Add(a + 1 + (y * verticesThisRing));

                    trianglesList[target].Add(a + (y * verticesThisRing));
                    trianglesList[target].Add((indiceForLower) + ((y - 1) * verticesThisRing));
                    trianglesList[target].Add((indiceForLower + 1) + ((y - 1) * verticesThisRing));
                }
                
            }
        }

        trianglesReturned = trianglesList;
        verticesInARing = verticesThisRing;

        return vertices;
    }

    
    public static List<Vector3> BalconyRing(out List<List<int>> trianglesReturned,out int verticesInARing, List<List<int>> trianglesList,float xSize, float zSize, int sides,float roundnessSize)
    {
        List<Vector3> vertices = new List<Vector3>();
        int verticesThisRing = 0;
        xSize = 5;
        int sidesTest = 6;
        //start at one corner and run round points adding vertices
        float step = 180 / sidesTest;

        List<Vector3> cornerPoints = new List<Vector3>();
        for (float i = 0; i <= 180f; i+=step)
        {
            Vector3 dir = Quaternion.Euler(0, i , 0)*Vector3.right;
            Vector3 p0 = dir * xSize;

            //dir = Quaternion.Euler(0, i + step / 2, 0) * Vector3.right;
            //Vector3 p1 = dir * xSize;


            float detail = 0.2f;
            for (float j = 0; j < 1f; j+=detail)
            {
                //Vector3 p0 = 
            }

            

            cornerPoints.Add(p0);
            
            /*
            GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
            c.transform.position = p0;
           c.transform.localScale *= 0.5f;
            c.name = "p0";
            */
            
        }
        //below helps a square/rectangle balcony have corners where they shoudl be
        float cornerAmount = 1f;
        if (sidesTest == 2)
            cornerAmount = 2f;

        List<Vector3> cornersAndSides = new List<Vector3>();
        for (int i = 0; i < cornerPoints.Count-1; i++)
        {
            Vector3 halfway = Vector3.Lerp(cornerPoints[i], cornerPoints[i + 1], 0.5f);
            Vector3 fromCentreToHalfway = (halfway - Vector3.zero);
            Vector3 p = fromCentreToHalfway * cornerAmount;

          //  GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
          //  c.transform.position = p;
          //  c.transform.localScale *= 0.5f;
          //  c.name = "corner";

            cornersAndSides.Add(cornerPoints[i]);
            cornersAndSides.Add(p);
        }

        cornersAndSides.Add(cornerPoints[cornerPoints.Count - 1]);

        for (int i = 0; i < cornersAndSides.Count-1; i++)
        {
            Debug.DrawLine(cornersAndSides[i], cornersAndSides[i + 1]);

            if (sidesTest == 2)
            {
                if (i % 2 == 0)
                    continue;
            }
            else
            {
                if (i == 0)
                    continue;

                else if (i % 2 != 0)
                    continue;
            }

            //we have made it to a corner
            //lets create a smoothy!
            Vector3 toPrev = (cornersAndSides[i-1] - cornersAndSides[i]).normalized;
            Vector3 toNext = (cornersAndSides[i+1] - cornersAndSides[i]).normalized;
            //roundness size from corner, spin 90 degree. roundness size from there


            Vector3 a = cornersAndSides[i] + toPrev * roundnessSize;
            //rotate arm from here
            Vector3 arm = a - cornersAndSides[i];
            arm = Quaternion.Euler(0, -90, 0) * arm;
            Vector3 b = a + arm;

            Vector3 c = cornersAndSides[i] + toNext * roundnessSize;
            Vector3 arm1 = c - cornersAndSides[i];
            arm1 = Quaternion.Euler(0, 90, 0) * arm1;
            Vector3 d = c + arm1;

            Vector3 mid = Vector3.Lerp(b, d, 0.5f);
            GameObject c0 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            c0.transform.position = b;
            c0.transform.localScale *= 0.05f;
            c0.name = "b";
            c0 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            c0.transform.position = d;
            c0.transform.localScale *= 0.05f;
            c0.name = "d";
         

            for (float j = 0 ; j <= 360; j+=15)
            {
                //spin
                Vector3 p = Quaternion.Euler(0, -j, 0) * (Vector3.right * roundnessSize);                
                //add
                //Vector3 toCentre = (cornersAndSides[i] - Vector3.zero).normalized;
                p += mid;// toCentre * (xSize);// - roundnessSize);
                                        // p -= toCentre * (roundnessSize);
                                        //now move towards prev point
                                        // p += toPrev * roundnessSize;
                                        //p += toNext * roundnessSize;

                //is this just the same as roundness size?

                c0 = GameObject.CreatePrimitive(PrimitiveType.Cube);
                c0.transform.position = p;
                c0.transform.localScale *= 0.05f;
                c0.name = "cornersss";

                
            }

            
        }

        trianglesReturned = trianglesList;
        verticesInARing = verticesThisRing;

        return vertices;
    }

    public static Vector3 GetPerpendicularAtAngle(Vector3 v, float angle)
    {
        //slighlty altered from https://gamedev.stackexchange.com/questions/120980/get-perpendicular-vector-from-another-vector

        // Generate a uniformly-distributed unit vector in the XY plane.
        Vector3 inPlane = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);

        // Rotate the vector into the plane perpendicular to v and return it.
        return Quaternion.LookRotation(v) * inPlane;
    }

    public static void BalconyWindow(Vector3 startOfBalcony, Vector3 endOfBalcony, GameObject gameObject, float tolerance,float startRot)
    {
        //balcony for each window using the same logic as the rounded rectangle for skyscraper

        List<Vector3> vertices = new List<Vector3>();
        List<List<int>> trianglesList = new List<List<int>>();
        List<int> triangles0 = new List<int>();
        List<int> triangles1 = new List<int>();
        List<int> triangles2 = new List<int>();
        trianglesList.Add(triangles0);
        trianglesList.Add(triangles1);
        trianglesList.Add(triangles2);
        
        float balconyHeight = 1f;
        float balconyWidth = .2f;
        int verticesInARing = 0;
        int sides = 4;
        float size = Vector3.Distance(startOfBalcony,endOfBalcony)/2 ;
        float height = 0f;
        float roundnessSize =.2f;
        float roundnessDetail = 1f;
        int ringsSoFar = 0;
        int spinSegment = 0;
        bool forLedge = false;
        bool glassType = false;
        float height1 = 1f;
        int windowsSizeX = 1;
        int windowsFrameSizeX = 10;
        
        startRot *= Mathf.Rad2Deg;
        /// Debug.Log("startrot = " + startRot);

        BalconyRing(out trianglesList,out verticesInARing, trianglesList, size, 1f, sides,roundnessSize);

        return;
        //2, top to bottom, to inside to bottom inside
        for (int i = 0; i <= 6; i++)
        {
            

        

            //stitch first
            if (i == 0 || i == 6)
            {
                for (int j = 0; j < verticesInARing; j++)
                {
                    vertices[vertices.Count-verticesInARing + j] = new Vector3(0f,vertices[j].y, 0f);
                }
            }

            //add height - floor
            if(i==1)                
                height += balconyWidth;
            if (i == 2)
                height += balconyHeight;
            //edge width, move inside
            if (i == 3)
                size -= balconyWidth;
            //drop back to bottom- we have made a wall - im sure we could get more creative here but using a curve
            if (i == 4)
                height -= balconyHeight;

            ringsSoFar++;
        }


        for (int i = 0; i < vertices.Count; i++)
        {
            /*
            GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
            c.transform.position = vertices[i];
            c.transform.localScale *= 0.2f;
            */
            //vertices[i] = Quaternion.Euler(0, -startRot, 0f) * vertices[i];
        }

        //make game object etc

        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.SetTriangles(trianglesList[0], 0);
        mesh.RecalculateNormals();

        GameObject balconyObject = new GameObject();
        //balconyObject.transform.position = Vector3.Lerp(startOfBalcony, endOfBalcony, .5f);
        // balconyObject.transform.rotation = Quaternion.Euler(0, startRot + 180, 0f);
        balconyObject.name = "Balcony";
        MeshFilter mF = balconyObject.AddComponent<MeshFilter>();
        mF.mesh = mesh;

        MeshRenderer mR = balconyObject.AddComponent<MeshRenderer>();
        mR.sharedMaterial = Resources.Load("Post0") as Material;

        balconyObject.transform.parent = gameObject.transform.Find("Additions");

    }
}
