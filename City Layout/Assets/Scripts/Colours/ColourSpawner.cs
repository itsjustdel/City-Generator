using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColourSpawner : MonoBehaviour {

    public int amountX = 10;
    public int amountY = 10;
    public bool reset = false;
    public bool animate = false;
    public bool constantRandoms;
    public int randomsAtOnce = 5;
    List<GameObject> blocks = new List<GameObject>();

    bool buildingFinished = false;
    // Use this for initialization
    void Start ()
    {
       StartCoroutine("Spawn");
	}
	
	// Update is called once per frame
	void Update ()
    {
		if(buildingFinished)
        {
            if(constantRandoms)
            {
                for (int i = 0; i < randomsAtOnce; i++)
                {


                    blocks[Random.Range(0, blocks.Count )].GetComponent<ColourPicker>().random = true;
                }
            }
        }
	}

    IEnumerator Spawn()
    {
        for (int i = 0; i < amountX; i++)
        {
            for (int j = 0; j < amountY; j++)
            {
                GameObject colourPicker = new GameObject();
                colourPicker.transform.position = new Vector3(i, j, 0);
                colourPicker.AddComponent<ColourPicker>();

                blocks.Add(colourPicker);

                if(animate)
                    yield return new WaitForEndOfFrame();
            }
        }
        buildingFinished = true;

        yield break;
    }
}
