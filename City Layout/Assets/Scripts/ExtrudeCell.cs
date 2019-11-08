using UnityEngine;
using System.Collections;
using System.Collections.Generic;
public class ExtrudeCell : MonoBehaviour {

    public float depth = 1f;
    public float scale = .7f;
    public float pavementDepth = 3f;//size in metres
    public bool uniqueVertices = false;
    public bool curveEdges = true;
    public bool onlyScale = false;
    public Vector3 centroid;
    public GameObject extrudedCell;//save for tidy up later, skyscraper script moves postions around
    public bool doExtrudeAnimation = true;
    public float totalTimeForAnimation = 2f;
    bool finishedAnimating = false;
    bool showTints = false;

    public Mesh originalMesh;
    private void Awake()
    {
        enabled = false;

    }
    // Use this for initialization
    public void Start()
    {

        originalMesh = new Mesh();
        originalMesh.vertices = GetComponent<MeshFilter>().mesh.vertices;
        originalMesh.triangles = GetComponent<MeshFilter>().mesh.triangles;

        Realign();

        if (scale < 1f) 
            ScaleToMiter();
        //ScaleToCenter();
        GetComponent<MeshRenderer>().enabled = false;

        if (onlyScale)
            return;

        if (doExtrudeAnimation && !curveEdges)
        {

            //make new object for extruded cell
            extrudedCell = new GameObject();
            extrudedCell.name = "Extruded Cell";
            extrudedCell.transform.position = transform.position;
            extrudedCell.transform.parent = transform;
            extrudedCell.AddComponent<MeshRenderer>().sharedMaterial = Resources.Load("Ground") as Material;

            extrudedCell.AddComponent<MeshFilter>().mesh = Extrude(GetComponent<MeshFilter>().mesh, depth, scale, uniqueVertices);
            //extrudedCell.transform.localScale = Vector3.zero;
            StartCoroutine(ScaleWithTime(extrudedCell.transform, totalTimeForAnimation));
        }

        if(curveEdges && doExtrudeAnimation)//no option for non animated curved!
        {
           // extrudedCell.GetComponent<MeshRenderer>().enabled = false;

            Pavement pavement = gameObject.AddComponent<Pavement>();
            pavement.depth = depth;
            
            pavement.Start();
            //animate new curved edge cell
            
            StartCoroutine(ScaleWithTime(pavement.pavement.transform, totalTimeForAnimation));
        }

        if(showTints)
            ShowTints();


        Vector3[] vertices = GetComponent<MeshFilter>().mesh.vertices;
        for (int i = 0; i < vertices.Length; i++)
        {
         //   GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
          //  c.transform.position = vertices[i] + transform.position;
          //  c.name = i.ToString();

        }

        //FixMeshCollider(extrudedCell);//needed?
        
    }
    public IEnumerator ScaleWithTime(Transform extrudedCell, float timeToMove)
    {
      //  Debug.Log("here");
        var t = 0f;
        while (t < 1)
        {
            t += Time.deltaTime / timeToMove;
            float eased = Easings.BounceEaseOut(t);

            Vector3 targetScale = Vector3.one;
            extrudedCell.localScale = Vector3.Lerp(Vector3.zero, targetScale, eased);
            yield return null;
        }
        
        //once it gets here, set true
        finishedAnimating = true;
        //let skyscraper script we already re aligned thisc ell
        //GetComponent<SkyscraperFromVoronoiCell>().cellReAligned = true;
        //alos, set parent cell at height of extruded cell
        //transform.position += Vector3.up *depth;//was mucking up splines
        //start build control

        //wait a little before building
        yield return new WaitForSeconds(0.5f);
        GameObject.FindGameObjectWithTag("Code").GetComponent<BuildControl>().readyToBuild = true;

        if (showTints)
            ShowTints();

    }

   
    void ShowTints()
    {
        //build extrusions for shades and tines - visualistion of template


        int scaleForCylindersX = 10;
        int scaleForCylindersY = 1;
        if (showTints)
        {


            PaletteInfo pI = GetComponent<PaletteInfo>();
            for (int a = 0; a < pI.palette.Count; a++)
            {

                //change this to mini stacks rotate around centre, plinth for main colour
                Vector3 arm = Quaternion.Euler(0, a * (360f / pI.palette.Count), 0) * Vector3.right * scaleForCylindersX * 1.33f;


                //for each tint in main colour's pallete
                //do shades first
                
                for (int i = 0; i < pI.palette[a].shades.Count; i++)
                {
                    int f = i + 1;
                
                    GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    cylinder.transform.position = transform.position + arm + (Vector3.up * i * scaleForCylindersY * 2) + (Vector3.up * (depth + scaleForCylindersY));
                    cylinder.transform.localScale = new Vector3(scaleForCylindersX, scaleForCylindersY, scaleForCylindersX);

                    //show shades                    
                    
                    cylinder.GetComponent<MeshRenderer>().sharedMaterial = pI.palette[a].shades[pI.palette[a].shades.Count - i - 1];
                    cylinder.name = "Shade " + i.ToString();
                    cylinder.transform.parent = extrudedCell.transform;
                }

                //place main colour in middle
                GameObject cylinderM = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                cylinderM.transform.position = transform.position + arm + (Vector3.up * pI.palette[a].shades.Count * scaleForCylindersY * 2) + (Vector3.up * (depth + scaleForCylindersY));
                cylinderM.transform.localScale = new Vector3(scaleForCylindersX, scaleForCylindersY, scaleForCylindersX);
                cylinderM.GetComponent<MeshRenderer>().sharedMaterial = pI.palette[a].material;
                cylinderM.name = "Shade Main";
                cylinderM.transform.parent = extrudedCell.transform;

                for (int i = 0; i < pI.palette[a].tints.Count; i++)
                {
                    GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    cylinder.transform.position = transform.position + arm + (Vector3.up * (i + pI.palette[a].shades.Count + 1) * scaleForCylindersY * 2) + (Vector3.up * (depth + scaleForCylindersY));
                    cylinder.transform.localScale = new Vector3(scaleForCylindersX, scaleForCylindersY, scaleForCylindersX);

                    //show tints
                    cylinder.GetComponent<MeshRenderer>().sharedMaterial = pI.palette[a].tints[pI.palette[a].tints.Count - i - 1];
                    cylinder.name = "Shade " + i.ToString();
                    cylinder.transform.parent = extrudedCell.transform;
                }
            }
        }
    }

    void ScaleToCenter()
    {
        Mesh mesh = GetComponent<MeshFilter>().mesh;
        Vector3[] verts = mesh.vertices;
        for (int i = 0; i < verts.Length; i++)
        {
            Vector3 p = Vector3.Lerp(verts[0], verts[i], scale);
            verts[i] = p;
        }

        mesh.vertices = verts;
    }
    void ScaleToMiter()
    {
       // GetComponent<MeshRenderer>().enabled = true;

        //miter points (directions) give us a nice halfway point in between a corner
        //we already worked them out when we calculating the edges for the cells - they are saved on adjacent cells script
        List<List<Vector3>> miters = GetComponent<AdjacentCells>().miters;
        //miters list corresponds to edges list
        List<List<int>> edges = GetComponent<AdjacentCells>().edges;
        //edges list corresponds to vertices
        Mesh mesh = GetComponent<MeshFilter>().mesh;
        Vector3[] vertices = mesh.vertices;

        //so, run through each edge  and move the vertices towards the miter direction.
        //This will give us a nicely scaled shape
        for (int i = 0; i < edges.Count; i++)
        {
            Vector3 p0 =vertices[ edges[i][0] ];
            Vector3 miter0 = miters[i][0];

           // Debug.DrawLine(p0 + transform.position, p0 + transform.position + miter0, Color.green);
            //Debug.Break();

            p0 += miter0 * pavementDepth;//var
            vertices[edges[i][0]] = p0;

        }

        mesh.vertices = vertices;
        mesh.RecalculateBounds();
    }
    void Realign()
    {
        
        //makes the transform position the centre of the mesh and moves the mesh vertices so the stay the same in world space
        Mesh mesh = GetComponent<MeshFilter>().mesh;

       
        transform.position = mesh.vertices[0];

        Vector3[] verts = mesh.vertices;
        List<Vector3> vertsList = new List<Vector3>();

        for(int i = 0; i < verts.Length; i++)
        {
            Vector3 point = verts[i] - transform.position;
        //    point.y = 0;
            vertsList.Add(point);
        }

        

        mesh.vertices = vertsList.ToArray();

       

    }

    public static Mesh Extrude(Mesh mesh,float depth,float scale,bool uniqueVertices)
    { 

        Vector3[] verts = mesh.vertices;
        int vertCountStart = verts.Length;
        List<Vector3> vertsList = new List<Vector3>();
        List<int> trisList = new List<int>();

        for (int i = 0; i < verts.Length-1 ; i++)
        {
            if (i == 0)
                continue;

            vertsList.Add(verts[i]);
            vertsList.Add(verts[i + 1]);
            vertsList.Add(verts[i + 1] + (Vector3.up*depth));
            vertsList.Add(verts[i] + (Vector3.up * depth));
        }

        //add joining link/ last one
        vertsList.Add(verts[verts.Length-1]);
        vertsList.Add(verts[1]);
        vertsList.Add(verts[1] + (Vector3.up * depth));
        vertsList.Add(verts[verts.Length - 1] + (Vector3.up * depth));


        for (int i = 0; i < vertsList.Count - 2; i+=4)
        {
            
            trisList.Add(i + 0);
            trisList.Add(i + 1);
            trisList.Add(i + 2);


            trisList.Add(i + 3);
            trisList.Add(i + 0);
            trisList.Add(i + 2);

        }

        //now add a top
        vertsList.Add(verts[0] + Vector3.up * depth);
        //join the last ring all to this central point
        for (int i = 0; i < vertsList.Count-2; i+=4)
        {
            trisList.Add(i + 2);
            trisList.Add(vertsList.Count-1);
            trisList.Add(i + 3);


        }
        foreach (Vector3 v3 in vertsList)
        {
        //   GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
          //  cube.transform.position = v3;
            //cube.transform.localScale *= 0.1f;
        }

        Mesh newMesh = new Mesh();
        newMesh.vertices = vertsList.ToArray();
        newMesh.triangles = trisList.ToArray();

        if (uniqueVertices)
        {
            newMesh = MeshTools.UniqueVertices(newMesh);
        }
        else
        {
            newMesh.RecalculateNormals();
            newMesh.RecalculateBounds();
        }

        
        return newMesh;
	}
	
	void Rotate()
    {
        transform.rotation *= Quaternion.Euler(Random.Range(-5f, 5f), 0f, Random.Range(-5f, 5f));
    }
    void FixMeshCollider(GameObject go)
    {
        go.AddComponent<MeshCollider>();
    }
    void Combine()
    {
        //if not already added combine, do it now
      //  if (transform.parent.GetComponent<CombineChildren>() == null)
      //      transform.parent.gameObject.AddComponent<CombineChildren>();

    }

    float inExp(float x)
    {
        if (x < 0.0f) return 0.0f;
        if (x > 1.0f) return 1.0f;
        return Mathf.Pow(2.0f, 10.0f * (x - 1.0f));
    }
}
