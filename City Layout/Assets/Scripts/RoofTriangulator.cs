using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoofTriangulator : MonoBehaviour {

    public static GameObject RoofObject(List<Vector3> ringVertices)
    {

        Vector2[] vertices2D = new Vector2[ringVertices.Count];
        for (int i = 0; i < ringVertices.Count; i++)
        {
            vertices2D[i] = new Vector2(ringVertices[i].x, ringVertices[i].z);
        }

        // Use the triangulator to get indices for creating triangles
        Triangulator tr = new Triangulator(vertices2D);
        int[] indices = tr.Triangulate();

        // Create the Vector3 vertices
        Vector3[] vertices = new Vector3[vertices2D.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i] = ringVertices[i];
        }

        //triangles are facing down, reverse
        for (int i = 0; i < indices.Length / 2; i++)
        {
            int temp = indices[i];
            indices[i] = indices[indices.Length - i - 1];
            indices[indices.Length - i - 1] = indices[temp];
        }

        // Create the mesh
        Mesh msh = new Mesh();
        msh.vertices = vertices;
        msh.triangles = indices;
        msh.RecalculateNormals();
        msh.RecalculateBounds();

        GameObject roof = new GameObject();
        // Set up game object with mesh;
        roof.AddComponent(typeof(MeshRenderer));
        MeshFilter filter = roof.AddComponent(typeof(MeshFilter)) as MeshFilter;
        filter.mesh = msh;

        return roof;
    }

    public static GameObject RoofObjectFromConcavePolygon(List<Vector3> ringVertices, bool flip)
    {
        List<Vector3> tempVertices = new List<Vector3>(ringVertices);
        List<PolygonTester.Triangle> triangles = PolygonTester.TriangulateConcavePolygon(tempVertices);

        List<int> indices = new List<int>();
        List<Vector3> vertices = new List<Vector3>();

        //transform data from triangulation algorithm in to something unity likes
        for (int i = 0; i < triangles.Count; i++)
        {
            vertices.Add(triangles[i].v1.position);            
            vertices.Add(triangles[i].v2.position);            
            vertices.Add(triangles[i].v3.position);

            if (!flip)
            {
                indices.Add(vertices.Count - 3);
                indices.Add(vertices.Count - 2);
                indices.Add(vertices.Count - 1);
            }
            else
            {
                indices.Add(vertices.Count - 3);
                indices.Add(vertices.Count - 1);
                indices.Add(vertices.Count - 2);
                
            }
         
        }

        

        // Create the mesh
        Mesh msh = new Mesh();
        msh.vertices = vertices.ToArray();
        msh.triangles = indices.ToArray();
        msh.RecalculateNormals();
        msh.RecalculateBounds();

        GameObject roof = new GameObject();
        // Set up game object with mesh;
        roof.AddComponent(typeof(MeshRenderer));
        MeshFilter filter = roof.AddComponent(typeof(MeshFilter)) as MeshFilter;
        filter.mesh = msh;

        return roof;
    }
}

