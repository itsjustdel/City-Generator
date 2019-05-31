using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PaletteInfo : MonoBehaviour {

    public List<MaterialAndShades> palette;
    
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

    public static List<MaterialAndShades> Palette(float hueP, float saturationP, float valueP, int harmonyStep, int tintsAndShades, Material standardMaterial)
    {


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
            MaterialAndShades mAs = ColourAndShades(hueForHarmony, saturationP, valueP, standardMaterial, tintsAndShades);
            pallete.Add(mAs);
        }

        return pallete;

    }

    static MaterialAndShades ColourAndShades(float hueP, float saturationP, float valueP,Material standardMaterial,int tintsAndShades)
    {

        //create material for main hue
        Color color0 = Color.HSVToRGB(hueP, saturationP, valueP);
        Material matMain = new Material(standardMaterial);
        matMain.color = color0;

        //get a list of tints based on the starting hue,value,saturation - tints, lighter colours
        List<Material> tints = Tints(hueP, saturationP, valueP,standardMaterial,tintsAndShades);
        //shades too, darker
        List<Material> shades = Shades(hueP, saturationP, valueP, standardMaterial, tintsAndShades);

        MaterialAndShades mAs = new MaterialAndShades(matMain, tints, shades);

        return mAs;
    }


    static List<Material> Tints(float huePassed, float saturationPassed, float valuePassed, Material standardMaterial, int tintsAndShades)
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

    static List<Material> Shades(float huePassed, float saturationPassed, float valuePassed, Material standardMaterial, int tintsAndShades)
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
}
