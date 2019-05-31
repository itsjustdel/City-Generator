using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spawner : MonoBehaviour {

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
		
    }
    void Place()
    {
        if (instance != null)
            Destroy(instance);

        instance = Instantiate(prefab);
    }
}
