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

    public static class bsbAPI
    {
        [DllImport("BlendShapeBuilderCore")] static extern int npRaycast(
            ref npMeshData model, Vector3 pos, Vector3 dir, ref int tindex, ref float distance);

        [DllImport("BlendShapeBuilderCore")] static extern Vector3 npPickNormal(
            ref npMeshData model, Vector3 pos, int ti);

        [DllImport("BlendShapeBuilderCore")] static extern int npSelectSingle(
            ref npMeshData model, ref Matrix4x4 viewproj, Vector2 rmin, Vector2 rmax, Vector3 campos, float strength, bool frontfaceOnly);

        [DllImport("BlendShapeBuilderCore")] static extern int npSelectTriangle(
            ref npMeshData model, Vector3 pos, Vector3 dir, float strength);
        
        [DllImport("BlendShapeBuilderCore")] static extern int npSelectEdge(
            ref npMeshData model, float strength, bool clear, bool mask);

        [DllImport("BlendShapeBuilderCore")] static extern int npSelectHole(
            ref npMeshData model, float strength, bool clear, bool mask);

        [DllImport("BlendShapeBuilderCore")] static extern int npSelectConnected(
            ref npMeshData model, float strength, bool clear);

        [DllImport("BlendShapeBuilderCore")] static extern int npSelectRect(
            ref npMeshData model, ref Matrix4x4 viewproj, Vector2 rmin, Vector2 rmax, Vector3 campos, float strength, bool frontfaceOnly);

        [DllImport("BlendShapeBuilderCore")] static extern int npSelectLasso(
            ref npMeshData model, ref Matrix4x4 viewproj, Vector2[] lasso, int numLassoPoints, Vector3 campos, float strength, bool frontfaceOnly);
        
        [DllImport("BlendShapeBuilderCore")] static extern int npSelectBrush(
            ref npMeshData model,
            Vector3 pos, float radius, float strength, int num_bsamples, IntPtr bsamples);

        [DllImport("BlendShapeBuilderCore")] static extern int npUpdateSelection(
            ref npMeshData model,
            ref Vector3 selection_pos, ref Vector3 selection_normal);


        [DllImport("BlendShapeBuilderCore")] static extern int npBrushReplace(
            ref npMeshData model,
            Vector3 pos, float radius, float strength, int num_bsamples, IntPtr bsamples, Vector3 amount, bool mask);

        [DllImport("BlendShapeBuilderCore")] static extern int npBrushPaint(
            ref npMeshData model,
            Vector3 pos, float radius, float strength, int num_bsamples, IntPtr bsamples, Vector3 baseNormal, int blend_mode, bool mask);

        [DllImport("BlendShapeBuilderCore")] static extern int npBrushSmooth(
            ref npMeshData model,
            Vector3 pos, float radius, float strength, int num_bsamples, IntPtr bsamples, bool mask);

        [DllImport("BlendShapeBuilderCore")] static extern int npBrushProjection(
            ref npMeshData model,
            Vector3 pos, float radius, float strength, int num_bsamples, IntPtr bsamples, bool mask,
            ref npMeshData normal_source, IntPtr ray_dirs);
        [DllImport("BlendShapeBuilderCore")] static extern int npBrushProjection2(
            ref npMeshData model,
            Vector3 pos, float radius, float strength, int num_bsamples, IntPtr bsamples, bool mask,
            ref npMeshData normal_source, Vector3 ray_dir);

        [DllImport("BlendShapeBuilderCore")] static extern int npBrushLerp(
            ref npMeshData model,
            Vector3 pos, float radius, float strength, int num_bsamples, IntPtr bsamples, IntPtr baseNormals, IntPtr normals, bool mask);

        [DllImport("BlendShapeBuilderCore")] static extern int npAssign(
            ref npMeshData model, Vector3 value);
        
        [DllImport("BlendShapeBuilderCore")] static extern int npMove(
            ref npMeshData model, Vector3 amount);
        
        [DllImport("BlendShapeBuilderCore")] static extern int npRotate(
            ref npMeshData model, Quaternion amount, Quaternion pivotRot);

        [DllImport("BlendShapeBuilderCore")] static extern int npRotatePivot(
            ref npMeshData model, Quaternion amount, Vector3 pivotPos, Quaternion pivotRot);

        [DllImport("BlendShapeBuilderCore")] static extern int npScale(
            ref npMeshData model, Vector3 amount, Vector3 pivotPos, Quaternion pivotRot);

        [DllImport("BlendShapeBuilderCore")] static extern int npSmooth(
            ref npMeshData model, float radius, float strength, bool mask);

        [DllImport("BlendShapeBuilderCore")] static extern int npWeld(
            ref npMeshData model, bool smoothing, float weldAngle, bool mask);
        [DllImport("BlendShapeBuilderCore")] static extern int npWeld2(
            ref npMeshData model, int num_targets, npMeshData[] targets,
            int weldMode, float weldAngle, bool mask);

        [DllImport("BlendShapeBuilderCore")] static extern int npBuildMirroringRelation(
            ref npMeshData model, Vector3 plane_normal, float epsilon, IntPtr relation);

        [DllImport("BlendShapeBuilderCore")] static extern void npApplyMirroring(
            int num_vertices, IntPtr relation, Vector3 plane_normal, IntPtr normals);

        [DllImport("BlendShapeBuilderCore")] static extern void npProjectNormals(
            ref npMeshData model, ref npMeshData target, IntPtr ray_dir, bool mask);
        [DllImport("BlendShapeBuilderCore")] static extern void npProjectNormals2(
            ref npMeshData model, ref npMeshData target, Vector3 ray_dir, bool mask);
        [DllImport("BlendShapeBuilderCore")] static extern void npProjectVertices(
            ref npMeshData model, ref npMeshData target, IntPtr ray_dir, npProjectVerticesMode mode, float max_distance);

        [DllImport("BlendShapeBuilderCore")] static extern void npApplySkinning(
            ref npSkinData skin,
            IntPtr ipoints, IntPtr inormals, IntPtr itangents,
            IntPtr opoints, IntPtr onormals, IntPtr otangents);

        [DllImport("BlendShapeBuilderCore")] static extern void npApplyReverseSkinning(
            ref npSkinData skin,
            IntPtr ipoints, IntPtr inormals, IntPtr itangents,
            IntPtr opoints, IntPtr onormals, IntPtr otangents);
        
        [DllImport("BlendShapeBuilderCore")] static extern int npGenerateNormals(
            ref npMeshData model, IntPtr dst);
        [DllImport("BlendShapeBuilderCore")] static extern int npGenerateTangents(
            ref npMeshData model, IntPtr dst);

        [DllImport("BlendShapeBuilderCore")] static extern void npInitializePenInput();
    }
}