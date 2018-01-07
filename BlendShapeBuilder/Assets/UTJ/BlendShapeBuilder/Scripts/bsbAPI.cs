using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

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

        public bool Extract(UnityEngine.Object obj)
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

            var smi = go.GetComponent<SkinnedMeshRenderer>();
            if (smi != null)
            {
                var mesh = new Mesh();
                smi.BakeMesh(mesh);
                return Extract(mesh);
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
}