#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace UTJ.VertexTweaker
{
    public static class Utils
    {
        public static GameObject MeshToGameObject(Mesh mesh, Vector3 pos, Material[] materials)
        {
            if (materials == null)
                materials = new Material[1] { AssetDatabase.GetBuiltinExtraResource<Material>("Default-Diffuse.mat") };

            var go = new GameObject(mesh.name);
            var smr = go.AddComponent<SkinnedMeshRenderer>();
            smr.sharedMesh = mesh;
            smr.sharedMaterials = materials;
            go.GetComponent<Transform>().position = pos;
            return go;
        }

        public static GameObject MeshToGameObject(Mesh mesh, GameObject from)
        {
            var go = MeshToGameObject(mesh, Vector3.zero, ExtractMaterials(from));
            if (from != null)
                go.transform.localScale = from.transform.localScale;
            return go;
        }


        public static Mesh ExtractMesh(UnityEngine.Object obj)
        {
            Mesh ret = null;
            var go = obj as GameObject;
            if (go != null)
            {
                {
                    var smr = go.GetComponent<SkinnedMeshRenderer>();
                    if (smr != null)
                        ret = smr.sharedMesh;
                }
                if (ret == null)
                {
                    var mf = go.GetComponent<MeshFilter>();
                    if (mf != null)
                        ret = mf.sharedMesh;
                }
            }
            else
            {
                ret = obj as Mesh;
            }
            return ret;
        }

        public static Material[] ExtractMaterials(UnityEngine.Object obj)
        {
            Material[] ret = null;
            var go = obj as GameObject;
            if (go != null)
            {
                {
                    var smr = go.GetComponent<SkinnedMeshRenderer>();
                    if (smr != null)
                        ret = smr.sharedMaterials;
                }
                if (ret == null)
                {
                    var mf = go.GetComponent<MeshRenderer>();
                    if (mf != null)
                        ret = mf.sharedMaterials;
                }
            }
            else
            {
                var mat = obj as Material;
                if(mat != null)
                    ret = new Material[1] { mat };
            }
            return ret;
        }

        public static string SanitizeForFileName(string name)
        {
            var reg = new Regex("[\\/:\\*\\?<>\\|\\\"]");
            return reg.Replace(name, "_");
        }
    }
}
#endif
