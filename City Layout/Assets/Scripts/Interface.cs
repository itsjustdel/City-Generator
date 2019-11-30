using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Interface : MonoBehaviour
{
    public Button generateButton;
    public Toggle simultaneousToggle;
    public Slider citySize;
    public Slider speed;

    public Spawner spawner;
    // Start is called before the first frame update
    void Start()
    {
        //buttons
        Button btn0 = generateButton.GetComponent<Button>();
        btn0.onClick.AddListener(GenerateClick);
        Toggle tgl0 = simultaneousToggle.GetComponent<Toggle>();
        
    }

    // Update is called once per frame
    void Update()
    {
        spawner.buildingSpeed = speed.value;
    }


    void GenerateClick()
    {

        Debug.Log("You have clicked the button! Generate");

        spawner.reset = true;
        spawner.citySize = (int)citySize.value;
        if (simultaneousToggle.isOn)
        {
            spawner.simultaneously = true;
            spawner.individually = false;
        }
        else
        {
            spawner.simultaneously = false;
            spawner.individually = true;
        }

        //make cam defaults
        Camera.main.GetComponent<CameraControl>().focusOnClicked = false;
        Camera.main.GetComponent<CameraControl>().zoomForSolo = 60;
        Camera.main.GetComponent<CameraControl>().zoomFactor = 1.5f;
        Camera.main.transform.localEulerAngles = new Vector3( 45,0,0); 
        spawner.buildingSpeed = speed.value;
    }
}
