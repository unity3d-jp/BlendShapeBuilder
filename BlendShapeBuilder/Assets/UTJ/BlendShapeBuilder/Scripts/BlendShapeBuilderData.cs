using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

#if UNITY_EDITOR
namespace UTJ.BlendShapeBuilder
{
    public class MeshData
    {
        public PinnedList<Vector3> vertices = new PinnedList<Vector3>();
        public PinnedList<Vector3> normals = new PinnedList<Vector3>();
        public PinnedList<Vector2> uv = new PinnedList<Vector2>();
        public PinnedList<Vector4> tangents = new PinnedList<Vector4>();
        public PinnedList<int> indices = new PinnedList<int>();
        public Matrix4x4 transform;

        public int vertexCount
        {
            get { return vertices.Count; }
            set
            {
                vertices.Resize(value);
                normals.Resize(value);
                uv.Resize(value);
            }
        }

        public int indexCount
        {
            get { return indices.Count; }
            set
            {
                indices.Resize(value);
            }
        }

        public bool empty { get { return vertices.Count == 0; } }

        public bool Extract(UnityEngine.Object obj, bool bake = true)
        {
            {
                var mesh = obj as Mesh;
                if (mesh != null)
                    return Extract(mesh);
            }

            var go = obj as GameObject;
            if (!go) { return false; }

            var terrain = go.GetComponent<Terrain>();
            if (terrain)
            {
                // terrain doesn't support rotation and scale
                transform = Matrix4x4.TRS(go.GetComponent<Transform>().position, Quaternion.identity, Vector3.one);
                return Extract(terrain);
            }

            transform = go.GetComponent<Transform>().localToWorldMatrix;

            var smr = go.GetComponent<SkinnedMeshRenderer>();
            if (smr != null)
            {
                if (bake)
                {
                    var mesh = new Mesh();
                    smr.BakeMesh(mesh);
                    return Extract(mesh);
                }
                else
                {
                    return Extract(smr.sharedMesh);
                }
            }

            var mf = go.GetComponent<MeshFilter>();
            if (mf != null)
            {
                return Extract(mf.sharedMesh);
            }
            return false;

        }

        public bool Extract(Mesh mesh)
        {
            if (!mesh || !mesh.isReadable) { return false; }

            vertexCount = mesh.vertexCount;
            mesh.GetVertices(vertices);
            mesh.GetNormals(normals);
            mesh.GetUVs(0, uv);
            mesh.GetTangents(tangents);
            indices = new PinnedList<int>(mesh.triangles);
            return true;
        }

        public bool Extract(Terrain terrain)
        {
            if (!terrain) { return false; }

            var tdata = terrain.terrainData;
            var w = tdata.heightmapWidth;
            var h = tdata.heightmapHeight;
            var heightmap = new PinnedArray2D<float>(tdata.GetHeights(0, 0, w, h));

            vertexCount = w * h;
            indexCount = (w - 1) * (h - 1) * 2 * 3;
            npGenerateTerrainMesh(heightmap, w, h, tdata.size,
                vertices, normals, uv, indices);
            return true;
        }
        [DllImport("BlendShapeBuilderCore")]
        static extern void npGenerateTerrainMesh(
            IntPtr heightmap, int width, int height, Vector3 size,
            IntPtr dst_vertices, IntPtr dst_normals, IntPtr dst_uv, IntPtr dst_indices);

        public static implicit operator npMeshData(MeshData v)
        {
            return new npMeshData
            {
                vertices = v.vertices,
                normals = v.normals,
                uv = v.uv,
                indices = v.indices,
                num_vertices = v.vertices.Count,
                num_triangles = v.indices.Count / 3,
                transform = v.transform,
            };
        }
    }

    public enum npProjectVerticesMode
    {
        Forward,
        Backward,
        ForwardAndBackward,
    };

    public struct npMeshData
    {
        public IntPtr indices;
        public IntPtr vertices;
        public IntPtr normals;
        public IntPtr tangents;
        public IntPtr uv;
        public IntPtr selection;
        public int num_vertices;
        public int num_triangles;
        public Matrix4x4 transform;
    }

    public struct npSkinData
    {
        public IntPtr weights;
        public IntPtr bones;
        public IntPtr bindposes;
        public int num_vertices;
        public int num_bones;
        public Matrix4x4 root;
    }

    [Serializable]
    public class BlendShapeFrameData
    {
        public float weight = 100.0f;
        public UnityEngine.Object mesh;
        public bool vertex = true;
        public bool normal = true;
        public bool tangent = true;

        public bool proj = false;
        public npProjectVerticesMode projMode = npProjectVerticesMode.ForwardAndBackward;
        public ProjectionRayDirection projRayDir = ProjectionRayDirection.CurrentNormals;
        public Vector3 projRadialCenter;
        public float projMaxRayDistance = 10.0f;

        public Vector3[] deltaVertices;
        public Vector3[] deltaNormals;
        public Vector3[] deltaTangents;

        public void AllocateDelta(int size)
        {
            deltaVertices = new Vector3[size];
            deltaNormals = new Vector3[size];
            deltaTangents = new Vector3[size];
        }

        public void ReleaseDelta()
        {
            deltaVertices = null;
            deltaNormals = null;
            deltaTangents = null;
        }
    }

    [Serializable]
    public class BlendShapeData
    {
        public bool fold = true;
        public string name = "";
        public List<BlendShapeFrameData> frames = new List<BlendShapeFrameData>();

        public void ClearInvalidFrames()
        {
            frames.RemoveAll(item => { return item.mesh == null; });
        }

        public void NormalizeWeights()
        {
            int n = frames.Count;
            float step = 100.0f / n;
            for (int i = 0; i < n; ++i)
            {
                frames[i].weight = step * (i + 1);
            }
        }

        public void SortByWeights()
        {
            frames.Sort((x, y) => x.weight.CompareTo(y.weight));
        }
    }

    [Serializable]
    public class BlendShapeBuilderData
    {
        public UnityEngine.Object baseMesh;
        public bool preserveExistingBlendShapes = false;
        public List<BlendShapeData> blendShapeData = new List<BlendShapeData>();
    }
}
#endif
