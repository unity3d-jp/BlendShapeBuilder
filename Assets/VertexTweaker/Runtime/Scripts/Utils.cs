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
            var go = MeshToGameObject(mesh, Vector3.zero, GetMaterials(from));
            if (from != null)
            {
                var srctrans = from.GetComponent<Transform>();
                var dsttrans = go.GetComponent<Transform>();
                dsttrans.position = srctrans.position;
                dsttrans.rotation = srctrans.rotation;
                dsttrans.localScale = srctrans.localScale;

                var srcsmr = from.GetComponent<SkinnedMeshRenderer>();
                var dstsmr = go.GetComponent<SkinnedMeshRenderer>();
                if (srcsmr != null && dstsmr != null)
                {
                    dstsmr.rootBone = srcsmr.rootBone;
                    dstsmr.bones = srcsmr.bones;
                }
            }
            return go;
        }


        public static Mesh GetMesh(UnityEngine.Object obj)
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

        public static Material[] GetMaterials(UnityEngine.Object obj)
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

        public static bool SetMesh(UnityEngine.Object obj, Mesh mesh)
        {
            var go = obj as GameObject;
            if (go != null)
            {
                {
                    var smr = go.GetComponent<SkinnedMeshRenderer>();
                    if (smr != null)
                    {
                        smr.sharedMesh = mesh;
                        return true;
                    }
                }
                {
                    var mf = go.GetComponent<MeshFilter>();
                    if (mf != null)
                    {
                        mf.sharedMesh = mesh;
                        return true;
                    }
                }
            }
            return false;
        }

        public static string SanitizeFileName(string name)
        {
            var reg = new Regex("[\\/:\\*\\?<>\\|\\\"]");
            return reg.Replace(name, "_");
        }
    }
}
#endif
