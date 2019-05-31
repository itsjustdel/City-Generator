using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[AddComponentMenu("Mesh/Combine Children")]
public class CombineChildren : MonoBehaviour {
	public bool receiveShadows = true;
	public bool castShadows = true;
    public bool disableColliders = false;
    public bool addAutoWeld = false;
    public bool addMeshCollider = false;
    public bool tagAsField = false;
    public bool addFindEdges = false;
    public bool renderer = false;
    public bool houseCell = false;
    public bool postOffice = false;
    public bool gardenCentre = false;
    public bool fieldCell = false;
    public bool ignoreDisabledRenderers = false;
    public bool reAlignCell;
    public bool addLod = false;
    
    public int LodLevels = 4;//always

	void Start()
	{
		Matrix4x4 myTransform = transform.worldToLocalMatrix;
		Dictionary<Material, List<CombineInstance>> combines = new Dictionary<Material, List<CombineInstance>>();
		MeshRenderer[] meshRenderers = GetComponentsInChildren<MeshRenderer>();
		foreach (var meshRenderer in meshRenderers)
		{
			foreach (var material in meshRenderer.sharedMaterials)
				if (material != null && !combines.ContainsKey(material))
					combines.Add(material, new List<CombineInstance>());
		}
		
		MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>();
		foreach(var filter in meshFilters)
		{
			if (filter.sharedMesh == null)
				continue;

            if (ignoreDisabledRenderers)
            {
                if(filter.GetComponent<MeshRenderer>() != null)
                    if (filter.GetComponent<MeshRenderer>().enabled == false)
                        continue;
            }

			CombineInstance ci = new CombineInstance();
			ci.mesh = filter.sharedMesh;
           // ci.mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            ci.transform = myTransform * filter.transform.localToWorldMatrix;
            if (filter.GetComponent<MeshRenderer>() != null)
                combines[filter.GetComponent<Renderer>().sharedMaterial].Add(ci);

            if (filter.GetComponent<MeshRenderer>() != null)
                if (!renderer)
			        filter.GetComponent<Renderer>().enabled = false;

            //if the public bool is true, disable the box colliders
            if (disableColliders)
                filter.GetComponent<BoxCollider>().enabled = false;
        }
		
		foreach(Material m in combines.Keys)
		{
			var go = new GameObject("Combined mesh");

         
			go.transform.parent = transform;
			go.transform.localPosition = Vector3.zero;
			go.transform.localRotation = Quaternion.identity;
			go.transform.localScale = Vector3.one;
			
			var filter = go.AddComponent<MeshFilter>();
			filter.mesh.CombineMeshes(combines[m].ToArray(), true, true);
			
			var renderer = go.AddComponent<MeshRenderer>();
			renderer.material = m;


            StartCoroutine("AddToGo", go);
		
        }
		//this.gameObject.AddComponent<MeshControl>();
	}

    IEnumerator AddToGo(GameObject go)
    {
        yield return new WaitForEndOfFrame();

        var renderer = go.GetComponent<MeshRenderer>();

        if (!receiveShadows)
            renderer.receiveShadows = false;

        if (!castShadows)
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        if (addAutoWeld)
        {
            AutoWeld aw = go.AddComponent<AutoWeld>();
            aw.addFindEdges = true;
        }


        //realign before adding mesh collider
        if (reAlignCell)
        {
            Realign();
        }

        if (addMeshCollider)
            go.AddComponent<MeshCollider>();


      
        
     

        yield break;
    }

    void Realign()
    {

        //makes the transform position the centre of the mesh and moves the mesh vertices so the stay the same in world space
        Mesh mesh = transform.Find("Combined mesh").GetComponent<MeshFilter>().mesh;

        //find the Y offset


        transform.position = mesh.bounds.center;

        Vector3[] verts = mesh.vertices;
        List<Vector3> vertsList = new List<Vector3>();

        for (int i = 0; i < verts.Length; i++)
        {
            vertsList.Add(verts[i] - transform.position);
        }



        mesh.vertices = vertsList.ToArray();



    }
}