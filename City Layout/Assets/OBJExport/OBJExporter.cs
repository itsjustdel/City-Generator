using UnityEngine;
using System.Collections;
//using UnityEditor;
using System.Text;
using System.Collections.Generic;

/*=============================================================================
 |	    Project:  Unity3D Scene OBJ Exporter
 |
 |		  Notes: Only works with meshes + meshRenderers. No terrain yet
 |
 |       Author:  aaro4130
 |
 |     DO NOT USE PARTS OF THIS CODE, OR THIS CODE AS A WHOLE AND CLAIM IT
 |     AS YOUR OWN WORK. USE OF CODE IS ALLOWED IF I (aaro4130) AM CREDITED
 |     FOR THE USED PARTS OF THE CODE.
 |
 *===========================================================================*/

public class OBJExporter : MonoBehaviour
{
    public bool onlySelectedObjects = false;
    public bool applyPosition = true;
    public bool applyRotation = true;
    public bool applyScale = true;
    public bool generateMaterials = true;
    public bool exportTextures = true;
    public bool splitObjects = true;
    public bool autoMarkTexReadable = false;
    public bool objNameAddIdNum = false;

    //public bool materialsUseTextureName = false;

    private string versionString = "v2.0";
    private string lastExportFolder;


    
   static  Vector3 RotateAroundPoint(Vector3 point, Vector3 pivot, Quaternion angle)
    {
        return angle * (point - pivot) + pivot;
    }
    static Vector3 MultiplyVec3s(Vector3 v1, Vector3 v2)
    {
        return new Vector3(v1.x * v2.x, v1.y * v2.y, v1.z * v2.z);
    }

    private void Start()
    {
        string exportPath = "C:/Users/Derrick Wells/Documents/Exports/test.obj";
        Export(exportPath,gameObject);
    }

    public static void StartExport(string exportPath,GameObject gameObject)
    {
        Export(exportPath,gameObject);


         Mesh mesh = gameObject.GetComponent<MeshFilter>().mesh;
        Destroy(mesh);

        
    }

    static void Export(string exportPath,GameObject gameObject)
    {
        bool applyScale = true;
        bool applyPosition = true;
        bool applyRotation = true;
        //init stuff
        Dictionary<string, bool> materialCache = new Dictionary<string, bool>();
        var exportFileInfo = new System.IO.FileInfo(exportPath);
        string lastExportFolder = exportFileInfo.Directory.FullName;
        string baseFileName = System.IO.Path.GetFileNameWithoutExtension(exportPath);
      //  EditorUtility.DisplayProgressBar("Exporting OBJ", "Please wait.. Starting export.", 0);

        //get list of required export things
        MeshFilter[] sceneMeshes;
        /*//exporting individually
        List<MeshFilter> filterList = new List<MeshFilter>();
        for (int i = 0; i < gameObject.transform.childCount; i++)
        {
            filterList.Add(gameObject.transform.GetChild(i).GetComponent<MeshFilter>());
        }
        */
        sceneMeshes = new MeshFilter[1] { gameObject.GetComponent<MeshFilter>() };
       // sceneMeshes = filterList.ToArray();
        
        //work on export
        StringBuilder sb = new StringBuilder();
        StringBuilder sbMaterials = new StringBuilder();
       // sb.AppendLine("# Export of " + Application.loadedLevelName);
        //sb.AppendLine("# from Aaro4130 OBJ Exporter " + versionString);
       // if (generateMaterials) //always do
        {
            sb.AppendLine("mtllib " + baseFileName + ".mtl");
        }
     //   float maxExportProgress = (float)(sceneMeshes.Length + 1);
        int lastIndex = 0;
        for(int i = 0; i < sceneMeshes.Length; i++)
        {
            string meshName = sceneMeshes[i].gameObject.name;
           // float progress = (float)(i + 1) / maxExportProgress;
            //EditorUtility.DisplayProgressBar("Exporting objects... (" + Mathf.Round(progress * 100) + "%)", "Exporting object " + meshName, progress);
            MeshFilter mf = sceneMeshes[i];
            MeshRenderer mr = sceneMeshes[i].gameObject.GetComponent<MeshRenderer>();
            bool splitObjects = true;
            if (splitObjects)
            {
              //  string exportName = meshName;
                /*
                if (objNameAddIdNum)
                {
                    exportName += "_" + i;
                }
                sb.AppendLine("g " + exportName);
                */
            }
            if(mr != null)// && generateMaterials)
            {
                Material[] mats = mr.sharedMaterials;
                for(int j=0; j < mats.Length; j++)
                {
                    Material m = mats[j];
                    if (!materialCache.ContainsKey(m.name))
                    {
                        materialCache[m.name] = true;
                        sbMaterials.Append(MaterialToString(m));
                        sbMaterials.AppendLine();
                    }
                }
            }

            //export the meshhh :3
            Mesh msh = mf.sharedMesh;
            int faceOrder = (int)Mathf.Clamp((mf.gameObject.transform.lossyScale.x * mf.gameObject.transform.lossyScale.z), -1, 1);
            
            //export vector data (FUN :D)!
            foreach (Vector3 vx in msh.vertices)
            {
                Vector3 v = vx;
                if (applyScale)
                {
                    v = MultiplyVec3s(v, mf.gameObject.transform.lossyScale);
                }
                
                if (applyRotation)
                {
  
                    v = RotateAroundPoint(v, Vector3.zero, mf.gameObject.transform.rotation);
                }

                if (applyPosition)
                {
                    v += mf.gameObject.transform.position;
                }
                v.x *= -1;
                sb.AppendLine("v " + v.x + " " + v.y + " " + v.z);
            }
            foreach (Vector3 vx in msh.normals)
            {
                Vector3 v = vx;
                
                if (applyScale)
                {
                    v = MultiplyVec3s(v, mf.gameObject.transform.lossyScale.normalized);
                }
                if (applyRotation)
                {
                    v = RotateAroundPoint(v, Vector3.zero, mf.gameObject.transform.rotation);
                }
                v.x *= -1;
                sb.AppendLine("vn " + v.x + " " + v.y + " " + v.z);

            }
            foreach (Vector2 v in msh.uv)
            {
                sb.AppendLine("vt " + v.x + " " + v.y);
            }

            for (int j=0; j < msh.subMeshCount; j++)
            {
                if(mr != null && j < mr.sharedMaterials.Length)
                {
                    string matName = mr.sharedMaterials[j].name;
                    sb.AppendLine("usemtl " + matName);
                }
                else
                {
                    sb.AppendLine("usemtl " + meshName + "_sm" + j);
                }

                int[] tris = msh.GetTriangles(j);
                for(int t = 0; t < tris.Length; t+= 3)
                {
                    int idx2 = tris[t] + 1 + lastIndex;
                    int idx1 = tris[t + 1] + 1 + lastIndex;
                    int idx0 = tris[t + 2] + 1 + lastIndex;
                    if(faceOrder < 0)
                    {
                        sb.AppendLine("f " + ConstructOBJString(idx2) + " " + ConstructOBJString(idx1) + " " + ConstructOBJString(idx0));
                    }
                    else
                    {
                        sb.AppendLine("f " + ConstructOBJString(idx0) + " " + ConstructOBJString(idx1) + " " + ConstructOBJString(idx2));
                    }
                    
                }
            }

            lastIndex += msh.vertices.Length;
        }

        //write to disk
        System.IO.File.WriteAllText(exportPath, sb.ToString());
       // if (generateMaterials)
        {
            System.IO.File.WriteAllText(exportFileInfo.Directory.FullName + "\\" + baseFileName + ".mtl", sbMaterials.ToString());
        }

        //export complete, close progress dialog
        //EditorUtility.ClearProgressBar();
        
    }

   
  

    static private string ConstructOBJString(int index)
    {
        string idxString = index.ToString();
        return idxString + "/" + idxString + "/" + idxString;
    }
    static string MaterialToString(Material m)
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine("newmtl " + m.name);
        
        //add properties
        if (m.HasProperty("_Color"))
        {
            sb.AppendLine("Kd " + m.color.r.ToString() + " " + m.color.g.ToString() + " " + m.color.b.ToString());
            if (m.color.a < 1.0f)
            {
                //use both implementations of OBJ transparency
                sb.AppendLine("Tr " + (1f - m.color.a).ToString());
                sb.AppendLine("d " + m.color.a.ToString());
            }
        }
        if (m.HasProperty("_SpecColor"))
        {
            Color sc = m.GetColor("_SpecColor");
            sb.AppendLine("Ks " + sc.r.ToString() + " " + sc.g.ToString() + " " + sc.b.ToString());
        }
       
        sb.AppendLine("illum 2");
        return sb.ToString();
    }
   
}
//#endif