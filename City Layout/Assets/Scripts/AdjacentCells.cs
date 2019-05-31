using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AdjacentCells : MonoBehaviour {

    public List<GameObject> adjacentCells = new List<GameObject>();
    public List<GameObject> controlledAdjacents = new List<GameObject>();

    public bool edgeCell;
    public float targetY = 1f;
    public int controlledBy = -1;
    public bool frontlineCell = false;

    public bool beingMadeTransparent = false;
    //attach to gameobject to store adjacent cells 

    private void Start()
    {
        

    }

    private void Update()
    {
        if (beingMadeTransparent)
            return;
        
        if (frontlineCell)
        {
            if (controlledBy == 0)
                GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("Disputed") as Material;//unsure
            else if (controlledBy == 1)
                GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("Disputed") as Material;
            else
                GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("Disputed") as Material;
        }
        else
        
        { 
            
            if (controlledBy == 0)
                GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("Materials/Cyan2") as Material;
            else if (controlledBy == 1)
                GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("Materials/Orange2") as Material;
            else if (controlledBy == 2)
                GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("Materials/Green2") as Material;
            else if (controlledBy == 3)
                GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("Materials/DeepBlue2") as Material;
        }
    }
}
