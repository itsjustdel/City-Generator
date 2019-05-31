using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColourPicker : MonoBehaviour {

    
    public bool random = false;
    public bool userControlled = false;
    public bool addContrasting = true;

    //limits colours to 30 degrees around colour wheel
    public bool strictColours = false;

    [Range(1, 10)]
    public int tintsAndShades = 4;
    [Range(0, 180)]
    public int harmonyStep = 30;
    //  GameObject mainColourCube;
    //  GameObject contrastCube;

    List<GameObject> cubesTints = new List<GameObject>();
    List<GameObject> cubesShades = new List<GameObject>();

    List<GameObject> allCubes = new List<GameObject>();


    //using this to grab fomr unity standard object- used ofr duplicating and then creating new material
    Material standardMaterial;

    
    public List<MaterialAndShades> matsAndShades = new List<MaterialAndShades>();

    //used to create a mood over several palletes
    public bool useGlobalSaturation;
    public float globalSaturation;

    [Range(0f, 1f)]
    public float hue;
    [Range(0f,1f)]
    public float saturation;
    [Range(0f, 1f)]
    public float value;
    

    private float contrastHue;//add to list?
    public List<float> hues = new List<float>();

    

    // Use this for initialization
    void Start ()
    {
        //steal the main material froma  primitive
        GameObject tempCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        standardMaterial = tempCube.GetComponent<MeshRenderer>().material;
        Destroy(tempCube);



        //start with a random
        ChooseRandom();
        ApplyToCubes();
	}

    private void Update()
    {
        if(random)
        {
            ChooseRandom();

            matsAndShades = Palette(hue, saturation, value);
            ApplyToCubes();
            random = false;
        }

        else if (userControlled)
        {

           

            matsAndShades = Palette(hue, saturation, value);
            
            ApplyToCubes();
        }

     
    }

 

    void ChooseRandom()
    {

        //The colours in the colour wheel are represented in 360 degrees, red is 0,yellow is 120, green is 180 etc.
        //Main colours are 30 degrees from each other, there are 12 main colours. Divide 360 by 0 to 12 to get a colour to start with
        //Using HSV to represent colour

        //stick to main colours? //add bool when i decide what's best

        //randomly choose hue
        hue = 0f;
        if (strictColours)
        {
            float hueChooser = 30 * Random.Range(0, 12);
            hue = (1f / 360) * hueChooser;
        }
        else if (!strictColours)
        {
            //or use this to create non standard palettes - can movd away from main colours
            hue = 1f / 360 * (Random.Range(0f, 360f));
        }

        if (!useGlobalSaturation)
        {
            //randomly choose saturation
            float satChooser = Random.Range(0.25f, .75f);//still playin with these
            saturation = satChooser;
        }
        else
        {
            saturation = globalSaturation;
        }

        //randomly choose value
        float valChooser = Random.Range(0.33f, .66f);//overide atm
         value = valChooser;
        value = 0.5f;//this really just controls how much lights is looking like it is hitting it, perhaps should be a constant


        harmonyStep = 30 * Random.Range(1, 5);

    }

    List<MaterialAndShades> Palette(float hueP, float saturationP,float valueP)
    {

        //destroy all materials - stops memory leak
        for (int i = 0; i < matsAndShades.Count; i++)
        {
            //main           
            Destroy(matsAndShades[i].material);

            //tints shades
            for (int j = 0; j < matsAndShades[i].tints.Count; j++)
            {
                Destroy(matsAndShades[i].tints[j]);
                Destroy(matsAndShades[i].shades[j]);
            }
        }

        List<MaterialAndShades> pallete = new List<MaterialAndShades>();

        //first main
          
        //6 tones, main, adjacents, contrasting, and contrasting adjacent, this will give a nice amount to randomly select from
        for (int i = 0; i < 6; i++)
        {
            //first coloour, hue no harmony
            int addition = 0;
            //add adjacents
            if (i == 1)
                addition = -harmonyStep;
            if (i == 2)
                addition = harmonyStep;

            //now contrasting
            if (i == 3)
                addition = 180;
            //now contrast adjacents
            if (i == 4)
                addition = 180 - harmonyStep;
            if (i == 5)
                addition = 180 + harmonyStep;

            float hueForHarmony = ((hueP * 360) + addition);
            //clamp so it stays within 360 degrees
            if (hueForHarmony < 0)
                hueForHarmony += 360;

            if (hueForHarmony > 360)
                hueForHarmony -= 360;
            //convert to fraction
            hueForHarmony /= 360;

            //complimentary

            //create materials
            MaterialAndShades mAs = ColourAndShades(hueForHarmony, saturationP, valueP);
            pallete.Add(mAs);
        }

        return pallete;

    }

    MaterialAndShades ColourAndShades(float hueP,float saturationP,float valueP)
    {

        //create material for main hue
        Color color0 = Color.HSVToRGB(hueP, saturationP, valueP);
        Material matMain = new Material(standardMaterial);
        matMain.color = color0;

        //get a list of tints based on the starting hue,value,saturation - tints, lighter colours
        List<Material> tints = Tints(hueP, saturationP, valueP);
        //shades too, darker
        List<Material> shades = Shades(hueP, saturationP, valueP);

        MaterialAndShades mAs = new MaterialAndShades(matMain, tints, shades);

        return mAs;
    }

    List<Material> Tints(float huePassed, float saturationPassed, float valuePassed)
    {

        float fractionForTint = saturationPassed / tintsAndShades;
        //tints // //divind by 0 is not defined, dividing by 1 gives us the base color, so start at 2
        List<Material> tints = new List<Material>();
        for (int i = 0; i < tintsAndShades; i++) //less than or equal to because we are adding the main colour as a small colour too   
        {

            Color colorTint = Color.HSVToRGB(huePassed, fractionForTint * i, valuePassed);
            
            Material mat0 = new Material(standardMaterial);
            mat0.color = colorTint;


            //saving to list
            tints.Add(mat0);

        }

        return tints;
    }

    List<Material> Shades (float huePassed, float saturationPassed,float valuePassed)
    {
        float fractionForValue = valuePassed / tintsAndShades;
        //shades // dividing by one again gives us base colour again
        List<Material> shades = new List<Material>();
        for (int i = tintsAndShades - 1; i >= 0; i--)
        {
            Color colorTint = Color.HSVToRGB(huePassed, saturationPassed, fractionForValue * i);

            Material mat0 = new Material(standardMaterial);
            mat0.color = colorTint;
            //saving to list
            shades.Add(mat0);
        }

        return shades;
    }

    void Contrast(float hue)
    {
        //main contrast is 180 degrees opposed to main hue
        contrastHue = hue + 180;
        //clamp
        if (contrastHue > 360)
            contrastHue -= 360;

       // hues.Add(contrastHue);

    }

    void ApplyToCubes()
    {
        //get rid of if any
        foreach (GameObject go in allCubes)
            Destroy(go);
        allCubes.Clear();

        float smallScale = (1f/tintsAndShades)*.5f;
        for (int i = 0; i < matsAndShades.Count; i++)
        {
        
            GameObject cube0 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube0.transform.parent = transform;
            cube0.transform.position = transform.position;
            cube0.transform.position += Vector3.up * i;
            
            //set main cube
            cube0.GetComponent<MeshRenderer>().sharedMaterial = matsAndShades[i].material;
            allCubes.Add(cube0);

            //now set small tins and shades
            for (int a = 0; a < matsAndShades[i].tints.Count; a++)
            {
                GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
               // cube.transform.parent = transform;
               // cube.transform.parent = cube0.transform;
                cube.transform.localScale *= smallScale;
                allCubes.Add(cube);

                //start at edge of main cube (scale for that is)
                cube.transform.position =transform.position+ (Vector3.right * -0.5f);
                //move over a small cube amount
                cube.transform.position += Vector3.right * (smallScale);// + (Vector3.up * -0.5f) + Vector3.forward * (.5f + smallScale * 0.25f);
                                                                        //step across cube leaving a gap
                cube.transform.position += Vector3.right * (smallScale * a * 1.5f);

                cube.transform.position = new Vector3(cube.transform.position.x, .5f - smallScale + cube0.transform.position.y, cube0.transform.position.z);

                cube.transform.position -= Vector3.forward * (.5f + smallScale * 0.25f);

                cube.GetComponent<MeshRenderer>().sharedMaterial = matsAndShades[i].tints[a];

                cube.transform.parent = cube0.transform;
            }

            for (int a = 0; a < matsAndShades[i].shades.Count; a++)
            {
                GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                // cube.transform.parent = transform;
                // cube.transform.parent = cube0.transform;
                cube.transform.localScale *= smallScale;
                allCubes.Add(cube);

                //start at edge of main cube (scale for that is)
                cube.transform.position = transform.position + (Vector3.right * 0.5f);
                //move over a small cube amount
                cube.transform.position -= Vector3.right * (smallScale);

                cube.transform.position -= Vector3.right * (smallScale * (a) * 1.5f);//

                cube.transform.position = new Vector3(cube.transform.position.x, -.5f + smallScale + cube0.transform.position.y, cube0.transform.position.z);

                cube.transform.position -= Vector3.forward * (.5f + smallScale * 0.25f);

                cube.GetComponent<MeshRenderer>().sharedMaterial = matsAndShades[i].shades[tintsAndShades - a -1]; //placing in reverse

                cube.transform.parent = cube0.transform;
            }
        }

    }

    public class MaterialAndShades
    {
        public Material material;
        public List<Material> tints = new List<Material>();//lighter version
        public List<Material> shades = new List<Material>();//darker version

        public MaterialAndShades(Material aMaterial, List<Material> aTints, List<Material> aShades)
        {
            material = aMaterial;
            tints = aTints;
            shades = aShades;
        }
    }
}
