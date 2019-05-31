using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildControl : MonoBehaviour {

    // Use this for initialization
    public List<GameObject> cells = new List<GameObject>();
    
    public bool building = false;

    public int builtSoFar = 0;

    public bool export = false;

    public bool soloBuildings = false;
    public bool individually = false;
    public bool simultaneously = true;
    public bool readyToBuild = false;
    bool cellsSetInactive = false;
    bool waitingForReset = false;
    public float secondsToWaitTilNext = 1f;
    

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
        }
        else if(soloBuildings)
        {
            //shows one cell at a time, deletes and moves on

            if (!cellsSetInactive)
            {
                for (int i = 0; i < cells.Count; i++)
                {
                    cells[i].SetActive(false);
                    cells[i].GetComponent<ExtrudeCell>().extrudedCell.SetActive(false);

                    cells[i].transform.position = Vector3.zero;
                    cells[i].GetComponent<ExtrudeCell>().extrudedCell.transform.position = Vector3.zero;
                }

                cellsSetInactive = true;
            }

            //all inactive, now activate first cell
            if(cells.Count > 0 && !building)
            {

                cells[0].SetActive(true);
                cells[0].GetComponent<ExtrudeCell>().extrudedCell.SetActive(true);
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
                        Invoke("ResetFlags", secondsToWaitTilNext);
                    }

                }
            }
        }

	}

    void ResetFlags()
    {
        building = false;
        cells[0].SetActive(false);
        cells[0].GetComponent<ExtrudeCell>().extrudedCell.SetActive(false);

        //remove from working list
        cells.RemoveAt(0);

        waitingForReset = false;

    }
}
