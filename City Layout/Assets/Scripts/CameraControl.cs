using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraControl : MonoBehaviour {
    // How many units should we keep from the players
    public float zoomFactor = 1.5f;
    public float zoomDampener = 1f;
    public float followTimeDelta = 0.8f;
    public float nearBumpStop = 10f;
    
    public GameObject winner;
    public bool showWinner = false;
    public bool bumped;
   public  float startingRotX;
    public float rotateForTransparentCells = 10;
    public float extraSpace = 5f;
    public float rotSpeed = 1f;
    public MeshGenerator mg;

    // Use this for initialization
    public void Start () {
    
        startingRotX = transform.localRotation.eulerAngles.x;
        showWinner = false;


        
       
	}
	
	// Update is called once per frame
	void Update ()
    {

        mg = GameObject.FindGameObjectWithTag("Code").GetComponent<MeshGenerator>();

        RotateCam();

        FixedCameraFollowSmooth(GetComponent<Camera>(), mg.cells);
        
        
	}

    void RotateCam()
    {
        transform.parent.rotation *= Quaternion.Euler(0, rotSpeed*Time.deltaTime, 0);
    }
    public void FixedCameraFollowSmooth(Camera cam, List<GameObject> players)
    {
        if (mg == null)
            return;

        if (players.Count == 0)
            return;
        
        float distance = 0f;

        Vector3 avg = Vector3.zero;
        int playersUsed = 0;
        for (int i = 0; i < players.Count; i++)
        {
         
         
         
                avg += players[i].transform.position;
                playersUsed++;

                distance += players[i].transform.position.magnitude;
         
        }
        //anchor to center by adding a player at vector.zero - obz dont need to add zero
        avg /= playersUsed;// + 1;
        // Midpoint we're after
        Vector3 midpoint = avg;// (t1.position + t2.position) / 2f;

        // Distance between objects
        //float distance = (t1.position - t2.position).magnitude;
        float mod = (distance/zoomDampener) * zoomFactor;
        Vector3 cameraDestination = midpoint - cam.transform.forward * mod;// (distance/ zoomDampener) * zoomFactor;
        if (distance < nearBumpStop)///zoomDampener) * zoomFactor)
        {
            bumped = true;
            mod = (nearBumpStop / zoomDampener) * zoomFactor;
            cameraDestination = midpoint - cam.transform.forward * mod;// (distance/ zoomDampener) * zoomFactor;
            cam.transform.parent.position = Vector3.Slerp(cam.transform.parent.position, cameraDestination, followTimeDelta);

            return;
        }
        else bumped = false;
        // Move camera a certain distance
        

        // Adjust ortho size if we're using one of those
        if (cam.orthographic)
        {
            // The camera's forward vector is irrelevant, only this size will matter
            cam.orthographicSize = distance;
        }
        // You specified to use MoveTowards instead of Slerp/if no too close
        
            cam.transform.parent.position = Vector3.Slerp(cam.transform.parent.position, cameraDestination, followTimeDelta);

        // Snap when close enough to prevent annoying slerp behavior
        if ((cameraDestination - cam.transform.position).magnitude <= 0.05f)
            cam.transform.parent.position = cameraDestination;
    }
}
