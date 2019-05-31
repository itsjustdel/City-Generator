using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VerticeCubes : MonoBehaviour {

	// Use this for initialization
	void Start () {


        Vector3[] vertices = GetComponent<MeshFilter>().mesh.vertices;
        for (int i = 0; i < vertices.Length; i++)
        {
            GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
            c.transform.position = vertices[i];
            c.name = i.ToString();
        }

        
	}
	
	}
