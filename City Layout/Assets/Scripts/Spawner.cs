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

        if (resetOnTimer)
            timeStart = Time.time;
    }
}
