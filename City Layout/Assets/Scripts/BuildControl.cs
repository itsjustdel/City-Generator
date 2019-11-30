using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildControl : MonoBehaviour {

    // Use this for initialization
    public List<GameObject> cells = new List<GameObject>();
    
    public bool building = false;

    public int builtSoFar = 0;

    public bool export = false;

    private bool soloBuildings = false;//not working
    public bool individually = false;
    public bool simultaneously = false;

    public bool finishedSimultaneous;

    public bool readyToBuild = false;
    bool cellsSetInactive = false;
    bool waitingForReset = false;
    public float secondsToWaitTilNext = 5f;

    public bool respawnWhenDone = true;

    
    private void Start()
    {
        //make acopy
       // cells =new List<GameObject>( GetComponent<MeshGenerator>().cells);
    }
    // Update is called once per frame
    void Update ()
    {
        if (readyToBuild == false)
            return;

        if (individually)
        {
            if (cells.Count > 0 && building == false)
            {
                //start cell build -- look fro traditional
                TraditionalSkyscraper tss = cells[0].GetComponent<TraditionalSkyscraper>();
                tss.enabled = true;

                //tell camera what cell we are building on, if user hasnt already taken control
                if(!Camera.main.GetComponent<CameraControl>().focusOnClicked)
                    Camera.main.GetComponent<CameraControl>().activeBuilding = cells[0];

                //keep track from here
                building = true;
                
            }
            else if (building)
            {
                //check if cell has finsihed
                TraditionalSkyscraper tss = cells[0].GetComponent<TraditionalSkyscraper>();
                if (tss.finishedBuilding)
                {

                    //StartCoroutine("BuildCo");

                    if (export)
                    {
                        for (int i = 0; i < cells[0].transform.childCount; i++)
                        {

                            string exportPath = "C:/Users/Derrick Wells/Documents/Exports/" + builtSoFar.ToString() + " " + i.ToString() + ".obj";
                            OBJExporter.StartExport(exportPath, cells[0].transform.GetChild(i).gameObject);
                            GameObject cell = cells[0].transform.GetChild(i).gameObject;
                            cell.name = i.ToString();
                        }
                    }

                    cells.RemoveAt(0);

                    building = false;

                    builtSoFar++;

                }
            }
        }
        else if(simultaneously)
        {
            
            foreach (GameObject cell in cells)
                cell.GetComponent<TraditionalSkyscraper>().enabled = true;

            bool allDone = true;
            foreach (GameObject cell in cells)
                if (!cell.GetComponent<TraditionalSkyscraper>().finishedBuilding)
                    allDone = false;

            if (allDone)
            {
                //GameObject.Find("Spawner").GetComponent<Spawner>().reset = true;
                finishedSimultaneous = true;
            }

        }
        else if(soloBuildings)
        {
            //shows one cell at a time, deletes and moves on

            if (!cellsSetInactive)
            {
                for (int i = 0; i < cells.Count; i++)
                {
                    cells[i].SetActive(false);
                    //cells[i].GetComponent<ExtrudeCell>().extrudedCell.SetActive(false);
                    cells[i].transform.GetChild(0).gameObject.SetActive(false);

                    cells[i].transform.position = Vector3.zero;
                    //cells[i].GetComponent<ExtrudeCell>().extrudedCell.transform.position = Vector3.zero;
                }

                cellsSetInactive = true;
            }

            //all inactive, now activate first cell
            if(cells.Count > 0 && !building)
            {

                cells[0].SetActive(true);
                //cells[0].GetComponent<ExtrudeCell>().extrudedCell.SetActive(true);
                cells[0].transform.GetChild(0).gameObject.SetActive(true);
                //start cell build -- look fro traditional
                TraditionalSkyscraper tss = cells[0].GetComponent<TraditionalSkyscraper>();
                tss.enabled = true;

                //keep track from here
                building = true;

                //when building is finsihed, the buildign script will reset the flag on this script
               
            }
            else if (building)
            {
                TraditionalSkyscraper tss = cells[0].GetComponent<TraditionalSkyscraper>();
                if (tss.finishedBuilding)
                {



                    //  cells[0].SetActive(false);
                    //cells[0].GetComponent<ExtrudeCell>().extrudedCell.SetActive(false);

                    //remove from working list
                    //cells.RemoveAt(0);
                    if (!waitingForReset)
                    {
                        waitingForReset = true;
                        //Invoke("ResetFlags", secondsToWaitTilNext);
                    }

                }
            }
        }

	}

    void ResetFlags()
    {
        building = false;
        cells[0].SetActive(false);
        //cells[0].GetComponent<ExtrudeCell>().extrudedCell.SetActive(false);
        cells[0].transform.GetChild(0).gameObject.SetActive(false);

        //remove from working list
        cells.RemoveAt(0);

        waitingForReset = false;

    }
}
