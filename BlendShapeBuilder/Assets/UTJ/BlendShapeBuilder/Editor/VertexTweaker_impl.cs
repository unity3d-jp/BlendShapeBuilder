using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif


namespace UTJ.BlendShapeBuilder
{
    public partial class VertexTweaker : MonoBehaviour
    {


        bool UpdateBoneMatrices()
        {
            bool ret = false;

            var rootMatrix = GetComponent<Transform>().localToWorldMatrix;
            if (m_npSkinData.root != rootMatrix)
            {
                m_npSkinData.root = rootMatrix;
                ret = true;
            }

            var bones = GetComponent<SkinnedMeshRenderer>().bones;
            for (int i = 0; i < m_boneMatrices.Count; ++i)
            {
                var l2w = bones[i].localToWorldMatrix;
                if (m_boneMatrices[i] != l2w)
                {
                    m_boneMatrices[i] = l2w;
                    ret = true;
                }
            }
            return ret;
        }

        void UpdateTransform()
        {
            m_npModelData.transform = GetComponent<Transform>().localToWorldMatrix;

            if (m_skinned && UpdateBoneMatrices())
            {
                npApplySkinning(ref m_npSkinData,
                    m_pointsPredeformed, m_normalsPredeformed, m_tangentsPredeformed,
                    m_points, m_normals, m_tangents);
                npApplySkinning(ref m_npSkinData,
                    IntPtr.Zero, m_normalsBasePredeformed, m_tangentsBasePredeformed,
                    IntPtr.Zero, m_normalsBase, m_tangentsBase);

                if (m_cbPoints != null) m_cbPoints.SetData(m_points);
                if (m_cbNormals != null) m_cbNormals.SetData(m_normals);
                if (m_cbBaseNormals != null) m_cbBaseNormals.SetData(m_normalsBase);
                if (m_cbTangents != null) m_cbTangents.SetData(m_tangents);
                if (m_cbBaseTangents != null) m_cbBaseTangents.SetData(m_tangentsBase);
            }
        }

        public void UpdateNormals(bool mirror = true)
        {
            if (m_meshTarget == null) return;

            if (m_skinned)
            {
                UpdateBoneMatrices();
                npApplyReverseSkinning(ref m_npSkinData,
                    IntPtr.Zero, m_normals, IntPtr.Zero,
                    IntPtr.Zero, m_normalsPredeformed, IntPtr.Zero);
                if (mirror)
                {
                    ApplyMirroringInternal();
                    npApplySkinning(ref m_npSkinData,
                        IntPtr.Zero, m_normalsPredeformed, IntPtr.Zero,
                        IntPtr.Zero, m_normals, IntPtr.Zero);
                }
                m_meshTarget.SetNormals(m_normalsPredeformed);
            }
            else
            {
                if (mirror)
                    ApplyMirroringInternal();
                m_meshTarget.SetNormals(m_normals);
            }

            if (m_settings.tangentsMode == TangentsUpdateMode.Realtime)
                RecalculateTangents();

            m_meshTarget.UploadMeshData(false);
            if (m_cbNormals != null)
                m_cbNormals.SetData(m_normals);
        }

        public void UpdateSelection()
        {
            int prevSelected = m_numSelected;

            m_numSelected = npUpdateSelection(ref m_npModelData, ref m_selectionPos, ref m_selectionNormal);

            m_selectionRot = Quaternion.identity;
            if (m_numSelected > 0)
            {
                m_selectionRot = Quaternion.LookRotation(m_selectionNormal);
                m_settings.pivotPos = m_selectionPos;
                m_settings.pivotRot = m_selectionRot;
            }
            else
            {
                m_selectionPos = GetComponent<Transform>().position;
            }

            if (prevSelected == 0 && m_numSelected == 0)
            {
                // no need to upload
            }
            else
            {
                m_cbSelection.SetData(m_selection);
            }
        }

        public void RecalculateTangents()
        {
            RecalculateTangents(m_settings.tangentsPrecision);
        }
        public void RecalculateTangents(TangentsPrecision precision)
        {
            if (precision == TangentsPrecision.Precise)
            {
                m_meshTarget.RecalculateTangents();
                m_tangentsPredeformed.LockList(l =>
                {
                    m_meshTarget.GetTangents(l);
                });

                if (m_skinned)
                    npApplySkinning(ref m_npSkinData,
                        IntPtr.Zero, IntPtr.Zero, m_tangentsPredeformed,
                        IntPtr.Zero, IntPtr.Zero, m_tangents);
            }
            else
            {
                if (m_skinned)
                {
                    npMeshData tmp = m_npModelData;
                    tmp.vertices = m_pointsPredeformed;
                    tmp.normals = m_normalsPredeformed;
                    npGenerateTangents(ref tmp, m_tangentsPredeformed);
                    npApplySkinning(ref m_npSkinData,
                        IntPtr.Zero, IntPtr.Zero, m_tangentsPredeformed,
                        IntPtr.Zero, IntPtr.Zero, m_tangents);
                }
                else
                {
                    npGenerateTangents(ref m_npModelData, m_tangents);
                }
                m_meshTarget.SetTangents(m_tangentsPredeformed);
            }

            if (m_cbTangents != null)
                m_cbTangents.SetData(m_tangents);
        }


        public bool Raycast(Event e, ref Vector3 pos, ref int ti)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            float d = 0.0f;
            if (Raycast(ray, ref ti, ref d))
            {
                pos = ray.origin + ray.direction * d;
                return true;
            }
            return false;
        }

        public bool Raycast(Ray ray, ref int ti, ref float distance)
        {
            bool ret = npRaycast(ref m_npModelData, ray.origin, ray.direction, ref ti, ref distance) > 0;
            return ret;
        }

        public Vector3 PickNormal(Vector3 pos, int ti)
        {
            return npPickNormal(ref m_npModelData, pos, ti);
        }

        public Vector3 PickBaseNormal(Vector3 pos, int ti)
        {
            m_npModelData.normals = m_normalsBase;
            var ret = npPickNormal(ref m_npModelData, pos, ti);
            m_npModelData.normals = m_normals;
            return ret;
        }


        public bool SelectEdge(float strength, bool clear)
        {
            bool mask = m_numSelected > 0;
            return npSelectEdge(ref m_npModelData, strength, clear, mask) > 0;
        }
        public bool SelectHole(float strength, bool clear)
        {
            bool mask = m_numSelected > 0;
            return npSelectHole(ref m_npModelData, strength, clear, mask) > 0;
        }

        public bool SelectConnected(float strength, bool clear)
        {
            if (m_numSelected == 0)
                return SelectAll();
            else
                return npSelectConnected(ref m_npModelData, strength, clear) > 0;
        }

        public bool SelectAll()
        {
            for (int i = 0; i < m_selection.Count; ++i)
                m_selection[i] = 1.0f;
            return m_selection.Count > 0;
        }

        public bool InvertSelection()
        {
            for (int i = 0; i < m_selection.Count; ++i)
                m_selection[i] = 1.0f - m_selection[i];
            return m_selection.Count > 0;
        }

        public bool ClearSelection()
        {
            System.Array.Clear(m_selection, 0, m_selection.Count);
            return m_selection.Count > 0;
        }

        public static Vector2 ScreenCoord11(Vector2 v)
        {
            var cam = SceneView.lastActiveSceneView.camera;
            var pixelRect = cam.pixelRect;
            var rect = cam.rect;
            return new Vector2(
                    v.x / pixelRect.width * rect.width * 2.0f - 1.0f,
                    (v.y / pixelRect.height * rect.height * 2.0f - 1.0f) * -1.0f);
        }

        public bool SelectVertex(Event e, float strength, bool frontFaceOnly)
        {
            var center = e.mousePosition;
            var size = new Vector2(15.0f, 15.0f);
            var r1 = center - size;
            var r2 = center + size;
            return SelectVertex(r1, r2, strength, frontFaceOnly);
        }
        public bool SelectVertex(Vector2 r1, Vector2 r2, float strength, bool frontFaceOnly)
        {
            var cam = SceneView.lastActiveSceneView.camera;
            if (cam == null) { return false; }

            var campos = cam.GetComponent<Transform>().position;
            var trans = GetComponent<Transform>().localToWorldMatrix;
            var mvp = GL.GetGPUProjectionMatrix(cam.projectionMatrix, false) * cam.worldToCameraMatrix * trans;
            r1 = ScreenCoord11(r1);
            r2 = ScreenCoord11(r2);
            var rmin = new Vector2(Math.Min(r1.x, r2.x), Math.Min(r1.y, r2.y));
            var rmax = new Vector2(Math.Max(r1.x, r2.x), Math.Max(r1.y, r2.y));

            return npSelectSingle(ref m_npModelData, ref mvp, rmin, rmax, campos, strength, frontFaceOnly) > 0;
        }

        public bool SelectTriangle(Event e, float strength)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            return SelectTriangle(ray, strength);
        }
        public bool SelectTriangle(Ray ray, float strength)
        {
            Matrix4x4 trans = GetComponent<Transform>().localToWorldMatrix;
            return npSelectTriangle(ref m_npModelData, ray.origin, ray.direction, strength) > 0;
        }


        public bool SelectRect(Vector2 r1, Vector2 r2, float strength, bool frontFaceOnly)
        {
            var cam = SceneView.lastActiveSceneView.camera;
            if (cam == null) { return false; }

            var campos = cam.GetComponent<Transform>().position;
            var trans = GetComponent<Transform>().localToWorldMatrix;
            var mvp = GL.GetGPUProjectionMatrix(cam.projectionMatrix, false) * cam.worldToCameraMatrix * trans;
            r1 = ScreenCoord11(r1);
            r2 = ScreenCoord11(r2);
            var rmin = new Vector2(Math.Min(r1.x, r2.x), Math.Min(r1.y, r2.y));
            var rmax = new Vector2(Math.Max(r1.x, r2.x), Math.Max(r1.y, r2.y));

            return npSelectRect(ref m_npModelData,
                ref mvp, rmin, rmax, campos, strength, frontFaceOnly) > 0;
        }

        public bool SelectLasso(Vector2[] lasso, float strength, bool frontFaceOnly)
        {
            var cam = SceneView.lastActiveSceneView.camera;
            if (cam == null) { return false; }

            var campos = cam.GetComponent<Transform>().position;
            var trans = GetComponent<Transform>().localToWorldMatrix;
            var mvp = GL.GetGPUProjectionMatrix(cam.projectionMatrix, false) * cam.worldToCameraMatrix * trans;

            return npSelectLasso(ref m_npModelData,
                ref mvp, lasso, lasso.Length, campos, strength, frontFaceOnly) > 0;
        }

        public bool SelectBrush(Vector3 pos, float radius, float strength, PinnedArray<float> bsamples)
        {
            Matrix4x4 trans = GetComponent<Transform>().localToWorldMatrix;
            return npSelectBrush(ref m_npModelData, pos, radius, strength, bsamples.Length, bsamples) > 0;
        }

        public static Vector3 GetMirrorPlane(MirrorMode mirrorMode)
        {
            switch (mirrorMode)
            {
                case MirrorMode.RightToLeft: return Vector3.left;
                case MirrorMode.LeftToRight: return Vector3.right;
                case MirrorMode.ForwardToBack: return Vector3.back;
                case MirrorMode.BackToForward: return Vector3.forward;
                case MirrorMode.UpToDown: return Vector3.down;
                case MirrorMode.DownToUp: return Vector3.up;
            }
            return Vector3.up;
        }

        MirrorMode m_prevMirrorMode;

        bool ApplyMirroringInternal()
        {
            if (m_settings.mirrorMode == MirrorMode.None) return false;

            bool needsSetup = false;
            if (m_mirrorRelation == null || m_mirrorRelation.Count != m_points.Count)
            {
                m_mirrorRelation = new PinnedList<int>(m_points.Count);
                needsSetup = true;
            }
            else if (m_prevMirrorMode != m_settings.mirrorMode)
            {
                m_prevMirrorMode = m_settings.mirrorMode;
                needsSetup = true;
            }

            Vector3 planeNormal = GetMirrorPlane(m_settings.mirrorMode);
            if (needsSetup)
            {
                npMeshData tmp = m_npModelData;
                tmp.vertices = m_pointsPredeformed;
                tmp.normals = m_normalsBasePredeformed;
                if (npBuildMirroringRelation(ref tmp, planeNormal, 0.0001f, m_mirrorRelation) == 0)
                {
                    Debug.LogWarning("NormalEditor: this mesh seems not symmetric");
                    m_mirrorRelation = null;
                    m_settings.mirrorMode = MirrorMode.None;
                    return false;
                }
            }

            npApplyMirroring(m_normals.Count, m_mirrorRelation, planeNormal, m_normalsPredeformed);
            return true;
        }
        public bool ApplyMirroring(bool pushUndo)
        {
            ApplyMirroringInternal();
            UpdateNormals();
            if (pushUndo) PushUndo();
            return true;
        }


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
