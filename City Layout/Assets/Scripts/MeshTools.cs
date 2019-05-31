using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshTools : MonoBehaviour
{
    private void Start()
    {
        Mesh mesh = GetComponent<MeshFilter>().mesh;
        mesh = UniqueVertices(mesh);
        GetComponent<MeshFilter>().mesh = mesh;
    }

    public static Mesh UniqueVertices(Mesh mesh)
    {

        //Process the triangles
        Vector3[] oldVerts = mesh.vertices;
        int[] triangles = mesh.triangles;
        Vector3[] vertices = new Vector3[triangles.Length];
        for (int i = 0; i < triangles.Length; i++)
        {
            vertices[i] = oldVerts[triangles[i]];
            triangles[i] = i;
        }
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        mesh.name = "Unique Verts";

        return mesh;
    }

}
