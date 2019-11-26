using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParkLayout : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        if (GetComponent<MeshFilter>().mesh.vertices.Length == 4)
        {
            Debug.Log("Cell has too few vertices");
                
            return;
        }
        List<Vector3> outline = Outline();

        GameObject newCell = RoofTriangulator.RoofObjectFromConcavePolygon(outline,false);
        newCell.GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("Ground") as Material;

        GetComponent<MeshRenderer>().enabled = false;

        Vector3[] newVertices = newCell.GetComponent<MeshFilter>().mesh.vertices;

        //find starting index of largest triangle
        int largestTriangleIndex = LargestTriangleIndex(newCell);

        //now find the longest two edges on this triangle
        EdgesByLength(largestTriangleIndex,newVertices);

    }

    List<Vector3> Outline()
    {
        List<Vector3> outline = new List<Vector3>(GetComponent<MeshFilter>().mesh.vertices);

        outline.RemoveAt(0);

        outline.Add(outline[0]);

        return outline;
    }

    int LargestTriangleIndex(GameObject newCell)
    {
        //calculate area for each triangle in mesh
        int[] triangles = newCell.GetComponent<MeshFilter>().mesh.triangles;
        Vector3[] vertices = newCell.GetComponent<MeshFilter>().mesh.vertices;

        int triangleStartIndex = 0;
        float largest = 0f;
        for (int i = 0; i < triangles.Length; i+=3)
        {
            Vector3 p0 = vertices[triangles[i]];
            Vector3 p1 = vertices[triangles[i+1]];
            Vector3 p2 = vertices[triangles[i+2]];
            
            Vector3 V = Vector3.Cross(p0 - p1, p0 - p2);
            float size = V.magnitude * 0.5f;
            if(size > largest)
            {
                largest = size;
                triangleStartIndex = i;
            }
        }

        for (int i = 0; i < 3; i++)
        {
            GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
            c.transform.position = vertices[triangleStartIndex + i];
            c.name = "lg";
        }

        return triangleStartIndex;
    }

    void EdgesByLength(int largestTriangleIndex,Vector3[] vertices)
    {
        SortedList sl = new SortedList();

        for (int i = 0; i < 3; i++)
        {
            int nextIndex = i + 1;
            if (nextIndex > 2)
                nextIndex = 0;

            float distance = Vector3.Distance(vertices[largestTriangleIndex + i],vertices[largestTriangleIndex + nextIndex]);
            int[] edge = new int[] {largestTriangleIndex + i,largestTriangleIndex + nextIndex };

            sl.Add(distance, edge);
        }


        for (int i = 0; i < sl.Count; i++)
        {
            
            //Debug.Log(sl[i]);
            int[] edge = (int[])sl.GetByIndex(i);
            GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
            c.transform.position = vertices[edge[0]];
            c.name = i.ToString() + " " + sl.GetKey(i);

            c = GameObject.CreatePrimitive(PrimitiveType.Cube);
            c.transform.position = vertices[edge[1]];
            c.name = i.ToString();
        }
    }
}
