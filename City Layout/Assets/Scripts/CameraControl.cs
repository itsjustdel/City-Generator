using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraControl : MonoBehaviour {
    // How many units should we keep from the players

   // public bool focusOnSoloBuilding = true;


    public float zoomFactor = 1.5f;
    public float zoomForSolo = 60f;
    public float zoomDampener = 1f;
    public float followTimeDelta = 0.8f;
    public float nearBumpStop = 10f;


    public bool bumped;
   public  float startingRotX;
    public float rotateForTransparentCells = 10;
    public float extraSpace = 5f;
    public float rotSpeed = 1f;
    public MeshGenerator mg;
    BuildControl buildControl;

    public GameObject activeBuilding;

    Vector3 localPosStart;


    public float shadowMod = 1.5f;

    

    // Use this for initialization
    public void Start () {
    
        startingRotX = transform.localRotation.eulerAngles.x;

        localPosStart = transform.localPosition;

        
       
	}
	
	// Update is called once per frame
	void FixedUpdate ()
    {

        mg = GameObject.FindGameObjectWithTag("Code").GetComponent<MeshGenerator>();
        buildControl = GameObject.FindGameObjectWithTag("Code").GetComponent<BuildControl>();


        RotateCam();

        //if cells havent been created, wait
        if (mg.cells.Count == 0)
            return;

        Vector3 centre = Vector3.zero;
        float highest = 0f;

        if (buildControl.building && buildControl.individually)
        {
            
            transform.parent.transform.position = Vector3.Lerp(transform.parent.transform.position, activeBuilding.transform.position,followTimeDelta);

            Vector3 target = activeBuilding.transform.position + (activeBuilding.GetComponent<TraditionalSkyscraper>().totalHeight * 0.66f) * Vector3.up;
            target -= transform.forward * zoomForSolo;            

            transform.position = Vector3.Slerp(transform.position, target, followTimeDelta);
        }
        else if(!buildControl.building && buildControl.individually)
        {
            
            for (int i = 0; i < mg.cells.Count; i++)
            {
                centre += mg.cells[i].transform.position;

                if (mg.cells[i].GetComponent<TraditionalSkyscraper>() != null)
                    if (mg.cells[i].GetComponent<TraditionalSkyscraper>().totalHeight > highest)
                        highest = mg.cells[i].GetComponent<TraditionalSkyscraper>().totalHeight;

            }

            centre /= mg.cells.Count;

            transform.parent.transform.position = Vector3.Lerp(transform.parent.transform.position, centre, followTimeDelta);

            Vector3 target = Vector3.zero;
            target = centre - transform.forward * zoomFactor * mg.volume.x;

            //add if finished building

            target += (highest * 0.66f) * Vector3.up;

            transform.position = Vector3.Slerp(transform.position, target, followTimeDelta);
        }
        else
        {
            
            if (mg.cells.Count > 0)
            {
               
                for (int i = 0; i < mg.cells.Count; i++)
                {
                    centre += mg.cells[i].transform.position;

                    if (mg.cells[i].GetComponent<TraditionalSkyscraper>() != null)
                        if (mg.cells[i].GetComponent<TraditionalSkyscraper>().totalHeight > highest)
                            highest = mg.cells[i].GetComponent<TraditionalSkyscraper>().totalHeight;

                }

                centre /= mg.cells.Count;

                transform.parent.transform.position = Vector3.Lerp(transform.parent.transform.position, centre, followTimeDelta);
                
            }

            //zoom
            
            Vector3 target = centre - transform.forward * zoomFactor * mg.volume.x;

            //add if finished building
            if(buildControl.finishedSimultaneous)
                target += (highest * 0.66f) * Vector3.up;

            transform.position = Vector3.Slerp(transform.position, target, followTimeDelta);
        }

        //shadows
        QualitySettings.shadowDistance = Vector3.Distance(transform.position, Vector3.zero) * shadowMod;
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
