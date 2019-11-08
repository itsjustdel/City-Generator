using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spawner : MonoBehaviour {

    public bool resetOnTimer;
    float timeStart;
    public float timeForReset;
    public bool reset;
    public GameObject prefab;
    public GameObject instance;

    public bool individually = false;
    public bool simultaneously = false;
    

    public float buildingSpeed = 2f;
    public int citySize = 100;

    
    public int density = 4;//not changin


    // Use this for initialization
    void Start ()
    {
        Place();
	}
	
	// Update is called once per frame
	void Update ()
    {
        if(reset == true)
        {
            Place();
            reset = false;
        }

        else if (resetOnTimer)
        {
            if (Time.time - timeStart > timeForReset)
                Place();
        }
		
    }
    void Place()
    {
        if (instance != null)
          DestroyImmediate(instance);
        
        instance = Instantiate(prefab);
        instance.GetComponent<MeshGenerator>().volume.x = citySize;
        instance.GetComponent<MeshGenerator>().volume.z = citySize;
        instance.GetComponent<MeshGenerator>().density = density;
            
         instance.GetComponent<BuildControl>().individually = individually;
         instance.GetComponent<BuildControl>().simultaneously = simultaneously;


        if (resetOnTimer)
            timeStart = Time.time;
    }
}

