using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif


namespace UTJ.VertexTweaker
{
#if UNITY_EDITOR
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
            mesh.GetVertices(vertices.List);
            mesh.GetNormals(normals.List);
            mesh.GetUVs(0, uv.List);
            mesh.GetTangents(tangents.List);
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
        [DllImport("VertexTweakerCore")]
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

    public partial class VertexTweaker : MonoBehaviour
    {
        public Vector3 ToWorldVector(Vector3 v, Coordinate c)
        {
            switch (c)
            {
                case Coordinate.Local: return GetComponent<Transform>().localToWorldMatrix.MultiplyVector(v);
                case Coordinate.Pivot: return m_settings.pivotRot * v;
            }
            return v;
        }


        public void ApplyAssign(Vector3 v, Coordinate c, int xyz, bool pushUndo)
        {
            Matrix4x4 trans = Matrix4x4.identity;
            if (c == Coordinate.World)
                trans = GetComponent<Transform>().localToWorldMatrix;

            npAssignVertices(ref m_npModelData, v, trans, xyz, m_numSelected > 0);
            UpdateVertices();
            if (pushUndo) PushUndo();
        }

        public void ApplyMove(Vector3 v, Coordinate c, bool pushUndo)
        {
            v = ToWorldVector(v, c);

            npMoveVertices(ref m_npModelData, v, m_numSelected > 0);
            UpdateVertices();
            if (pushUndo) PushUndo();
        }

        public void ApplyRotatePivot(Quaternion amount, Vector3 pivotPos, Quaternion pivotRot, Coordinate c, bool pushUndo)
        {
            var backup = m_npModelData.transform;
            var t = GetComponent<Transform>();
            switch (c)
            {
                case Coordinate.World:
                    m_npModelData.transform = t.localToWorldMatrix;
                    pivotRot = Quaternion.identity;
                    break;
                case Coordinate.Local:
                    m_npModelData.transform = Matrix4x4.identity;
                    pivotPos = t.worldToLocalMatrix.MultiplyPoint(pivotPos);
                    pivotRot = Quaternion.identity;
                    break;
                case Coordinate.Pivot:
                    m_npModelData.transform = t.localToWorldMatrix;
                    break;
                default: return;
            }

            npRotatePivotVertices(ref m_npModelData, amount, pivotPos, pivotRot, m_numSelected > 0);
            m_npModelData.transform = backup;

            UpdateVertices();
            if (pushUndo) PushUndo();
        }

        public void ApplyScale(Vector3 amount, Vector3 pivotPos, Quaternion pivotRot, Coordinate c, bool pushUndo)
        {
            var backup = m_npModelData.transform;
            var t = GetComponent<Transform>();
            switch (c)
            {
                case Coordinate.World:
                    m_npModelData.transform = t.localToWorldMatrix;
                    pivotRot = Quaternion.identity;
                    break;
                case Coordinate.Local:
                    m_npModelData.transform = Matrix4x4.identity;
                    pivotPos = t.worldToLocalMatrix.MultiplyPoint(pivotPos);
                    pivotRot = Quaternion.identity;
                    break;
                case Coordinate.Pivot:
                    m_npModelData.transform = t.localToWorldMatrix;
                    break;
                default: return;
            }

            npScaleVertices(ref m_npModelData, amount, pivotPos, pivotRot, m_numSelected > 0);
            m_npModelData.transform = backup;

            UpdateVertices();
            if (pushUndo) PushUndo();
        }

        public void ApplyProjection(bool pushUndo)
        {
            var target = m_settings.projTarget;
            if (target == null)
            {
                Debug.LogWarning("Projection target is not set.");
                return;
            }
            var targetData = new MeshData();
            if (!targetData.Extract(target))
            {
                Debug.LogWarning("Projection target has no mesh.");
                return;
            }

            int PNT = 0;
            if (m_settings.projP) { PNT |= 1; }
            if (m_settings.projN) { PNT |= 2; }
            if (m_settings.projT) { PNT |= 4; }

            bool mask = m_numSelected > 0;
            var targetNP = (npMeshData)targetData;
            if(m_settings.projRayDir == ProjectionRayDirection.CurrentNormals)
                npProjectVertices(ref m_npModelData, ref targetNP, m_normals, m_settings.projMode, m_settings.projMaxRayDistance, PNT, mask);
            else if (m_settings.projRayDir == ProjectionRayDirection.BaseNomals)
                npProjectVertices(ref m_npModelData, ref targetNP, m_normalsBase, m_settings.projMode, m_settings.projMaxRayDistance, PNT, mask);
            else if (m_settings.projRayDir == ProjectionRayDirection.Radial)
                npProjectVerticesRadial(ref m_npModelData, ref targetNP, m_settings.projRadialCenter, m_settings.projMode, m_settings.projMaxRayDistance, PNT, mask);
            else if (m_settings.projRayDir == ProjectionRayDirection.Directional)
                npProjectVerticesDirectional(ref m_npModelData, ref targetNP, m_settings.projDirection.normalized, m_settings.projMode, m_settings.projMaxRayDistance, PNT, mask);

            UpdateVertices(true);
            if (pushUndo) PushUndo();
        }

        public void ApplyReset(bool useSelection, bool pushUndo)
        {
            if (!useSelection)
            {
                Array.Copy(m_pointsBasePredeformed.Array, m_pointsPredeformed.Array, m_points.Count);
                Array.Copy(m_normalsBasePredeformed.Array, m_normalsPredeformed.Array, m_normals.Count);
                Array.Copy(m_tangentsBasePredeformed.Array, m_tangentsPredeformed.Array, m_tangents.Count);
            }
            else
            {
                for (int i = 0; i < m_points.Count; ++i)
                {
                    float s = m_selection[i];
                    m_pointsPredeformed[i] = Vector3.Lerp(m_pointsPredeformed[i], m_pointsBasePredeformed[i], s);
                    m_normalsPredeformed[i] = Vector3.Lerp(m_normalsPredeformed[i], m_normalsBasePredeformed[i], s);
                    m_tangentsPredeformed[i] = Vector4.Lerp(m_tangentsPredeformed[i], m_tangentsBasePredeformed[i], s);
                }
            }
            if (m_skinned)
            {
                npApplySkinning(ref m_npSkinData,
                    m_pointsPredeformed, m_normalsPredeformed, m_tangentsPredeformed,
                    m_points, m_normals, m_tangents);
            }

            UpdateVertices(true);
            if (pushUndo) PushUndo();
        }



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
                    m_pointsBasePredeformed, m_normalsBasePredeformed, m_tangentsBasePredeformed,
                    m_pointsBase, m_normalsBase, m_tangentsBase);
            }
        }

        public void UpdateVertices(bool flushAll = false)
        {
            if (m_meshTarget == null) return;

            if (m_skinned)
            {
                UpdateBoneMatrices();
                npApplyReverseSkinning(ref m_npSkinData,
                    m_points, IntPtr.Zero, IntPtr.Zero,
                    m_pointsPredeformed, IntPtr.Zero, IntPtr.Zero);
            }
            if (m_settings.mirrorMode != MirrorMode.None)
            {
                PrepareMirror();
                npApplyMirroring(ref m_npModelData, m_mirrorRelation, GetMirrorPlane(m_settings.mirrorMode),
                    m_pointsPredeformed, IntPtr.Zero, IntPtr.Zero);
                if (m_skinned)
                {
                    npApplySkinning(ref m_npSkinData,
                        m_pointsPredeformed, IntPtr.Zero, IntPtr.Zero,
                        m_points, IntPtr.Zero, IntPtr.Zero);
                }
            }
            m_meshTarget.SetVertices(m_pointsPredeformed.List);
            m_cbPointsDirty = true;

            if (flushAll)
            {
                m_meshTarget.SetNormals(m_normalsPredeformed.List);
                m_cbNormalsDirty = true;

                m_meshTarget.SetTangents(m_tangentsPredeformed.List);
                m_cbTangentsDirty = true;
            }
            else
            {
                if (m_settings.normalMode == RecalculateMode.Realtime)
                {
                    RecalculateNormalsInternal();
                    if (m_settings.tangentsMode == RecalculateMode.Realtime)
                        RecalculateTangentsInternal();
                }
            }

            m_meshTarget.UploadMeshData(false);

            var smr = GetComponent<SkinnedMeshRenderer>();
            if (smr != null)
            { // force recalculate
                smr.enabled = false;
                smr.enabled = true;
            }

            UpdateSelectionPos();
        }

        public void UpdateSelectionPos()
        {
            var n = npUpdateSelection(ref m_npModelData, ref m_selectionPos, ref m_selectionNormal);
            m_selectionRot = Quaternion.identity;
            if (n > 0)
                m_selectionRot = Quaternion.LookRotation(m_selectionNormal);
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
                m_cbSelectionDirty = true;
            }
        }


        bool m_forceDisableRecalculation = false;

        public void RecalculateNormals(bool pushUndo)
        {
            RecalculateNormalsInternal();
            if (pushUndo) PushUndo();
        }
        void RecalculateNormalsInternal(bool updateMesh = true)
        {
            if(m_forceDisableRecalculation) { return; }

            npMeshData tmp = m_npModelData;
            tmp.vertices = m_pointsPredeformed;
            npGenerateNormals(ref tmp, m_normalsPredeformed);

            if (m_skinned)
            {
                npApplySkinning(ref m_npSkinData,
                    IntPtr.Zero, m_normalsPredeformed, IntPtr.Zero,
                    IntPtr.Zero, m_normals, IntPtr.Zero);
            }

            if (updateMesh)
                m_meshTarget.SetNormals(m_normalsPredeformed.List);
            m_cbNormalsDirty = true;
        }

        public void RecalculateTangents(bool pushUndo)
        {
            RecalculateTangentsInternal();
            if (pushUndo) PushUndo();
        }
        void RecalculateTangentsInternal(bool updateMesh = true)
        {
            RecalculateTangentsInternal(m_settings.tangentsPrecision);
        }
        void RecalculateTangentsInternal(TangentsPrecision precision, bool updateMesh = true)
        {
            if (m_forceDisableRecalculation) { return; }

            if (precision == TangentsPrecision.Precise)
            {
                m_meshTarget.RecalculateTangents();
                m_tangentsPredeformed.LockList(l =>
                {
                    m_meshTarget.GetTangents(l);
                });
            }
            else
            {
                npMeshData tmp = m_npModelData;
                tmp.vertices = m_pointsPredeformed;
                tmp.normals = m_normalsPredeformed;
                npGenerateTangents(ref tmp, m_tangentsPredeformed);
            }

            if (m_skinned)
            {
                npApplySkinning(ref m_npSkinData,
                    IntPtr.Zero, IntPtr.Zero, m_tangentsPredeformed,
                    IntPtr.Zero, IntPtr.Zero, m_tangents);
            }

            if (updateMesh)
                m_meshTarget.SetTangents(m_tangentsPredeformed.List);
            m_cbTangentsDirty = true;
        }


        public bool Raycast(Event e, ref Vector3 pos, ref int ti)
        {
            var mousePos = e.mousePosition;
            if (Camera.current == null || !Camera.current.pixelRect.Contains(mousePos))
                return false;

            Ray ray = HandleUtility.GUIPointToWorldRay(mousePos);
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
            System.Array.Clear(m_selection.Array, 0, m_selection.Count);
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

        // return vertex index. -1 if not hit
        public bool PickVertex(Event e, float rectsize, bool frontFaceOnly, ref int vi, ref Vector3 vpos)
        {
            var center = e.mousePosition;
            var size = new Vector2(rectsize, rectsize);
            var r1 = center - size;
            var r2 = center + size;
            return PickVertex(r1, r2, frontFaceOnly, ref vi, ref vpos);
        }
        // return vertex index. -1 if not hit
        public bool PickVertex(Vector2 r1, Vector2 r2, bool frontFaceOnly, ref int vi, ref Vector3 vpos)
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

            return npPickVertex(ref m_npModelData, ref mvp, rmin, rmax, campos, frontFaceOnly, ref vi, ref vpos);
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
            var mousePos = e.mousePosition;
            if (Camera.current == null || !Camera.current.pixelRect.Contains(mousePos))
                return false;

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
        void PrepareMirror()
        {
            if (m_settings.mirrorMode == MirrorMode.None) return;

            bool needsSetup = false;
            if (m_mirrorRelation == null || m_mirrorRelation.Count != m_points.Count)
            {
                m_mirrorRelation = new PinnedList<int>(m_points.Count);
                needsSetup = true;
            }
            if (m_prevMirrorMode != m_settings.mirrorMode)
            {
                m_prevMirrorMode = m_settings.mirrorMode;
                needsSetup = true;
            }

            Vector3 planeNormal = GetMirrorPlane(m_settings.mirrorMode);
            if (needsSetup)
            {
                npMeshData tmp = m_npModelData;
                tmp.vertices = m_pointsBasePredeformed;
                tmp.normals = m_normalsBasePredeformed;
                if (npBuildMirroringRelation(ref tmp, planeNormal, m_settings.mirrorEpsilon, m_mirrorRelation) == 0)
                {
                    Debug.LogWarning("This mesh seems not symmetric");
                    m_mirrorRelation = null;
                    m_settings.mirrorMode = MirrorMode.None;
                }
            }
        }

        public bool ApplyMirroring(bool pushUndo)
        {
            UpdateVertices();
            if (pushUndo) PushUndo();
            return true;
        }

        static Rect FromToRect(Vector2 start, Vector2 end)
        {
            Rect r = new Rect(start.x, start.y, end.x - start.x, end.y - start.y);
            if (r.width < 0)
            {
                r.x += r.width;
                r.width = -r.width;
            }
            if (r.height < 0)
            {
                r.y += r.height;
                r.height = -r.height;
            }
            return r;
        }

        public void ResetToBindpose(bool pushUndo)
        {
            var smr = GetComponent<SkinnedMeshRenderer>();
            if (smr == null || smr.bones == null || smr.sharedMesh == null) { return; }

            var bones = smr.bones;
            var bindposes = smr.sharedMesh.bindposes;
            var bindposeMap = new Dictionary<Transform, Matrix4x4>();

            for (int i = 0; i < bones.Length; i++)
            {
                if (!bindposeMap.ContainsKey(bones[i]))
                {
                    bindposeMap.Add(bones[i], bindposes[i]);
                }
            }

            if (pushUndo)
                Undo.RecordObjects(bones, "VertexTweaker");

            foreach (var kvp in bindposeMap)
            {
                var bone = kvp.Key;
                var imatrix = kvp.Value;
                var localMatrix =
                    bindposeMap.ContainsKey(bone.parent) ? (imatrix * bindposeMap[bone.parent].inverse).inverse : imatrix.inverse;

                bone.localPosition = localMatrix.MultiplyPoint(Vector3.zero);
                bone.localRotation = Quaternion.LookRotation(localMatrix.GetColumn(2), localMatrix.GetColumn(1));
                bone.localScale = new Vector3(localMatrix.GetColumn(0).magnitude, localMatrix.GetColumn(1).magnitude, localMatrix.GetColumn(2).magnitude);
            }

            if (pushUndo)
                Undo.FlushUndoRecordObjects();
        }

        public void ExportSettings(string path)
        {
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(Instantiate(m_settings), path);
        }




        #region impl
        [DllImport("VertexTweakerCore")] static extern int npRaycast(
            ref npMeshData model, Vector3 pos, Vector3 dir, ref int tindex, ref float distance);

        [DllImport("VertexTweakerCore")] static extern Vector3 npPickNormal(
            ref npMeshData model, Vector3 pos, int ti);
        [DllImport("VertexTweakerCore")] static extern bool npPickVertex(
            ref npMeshData model, ref Matrix4x4 viewproj, Vector2 rmin, Vector2 rmax, Vector3 campos, bool frontfaceOnly, ref int vi, ref Vector3 vpos);

        [DllImport("VertexTweakerCore")] static extern int npSelectSingle(
            ref npMeshData model, ref Matrix4x4 viewproj, Vector2 rmin, Vector2 rmax, Vector3 campos, float strength, bool frontfaceOnly);

        [DllImport("VertexTweakerCore")] static extern int npSelectTriangle(
            ref npMeshData model, Vector3 pos, Vector3 dir, float strength);
        
        [DllImport("VertexTweakerCore")] static extern int npSelectEdge(
            ref npMeshData model, float strength, bool clear, bool mask);

        [DllImport("VertexTweakerCore")] static extern int npSelectHole(
            ref npMeshData model, float strength, bool clear, bool mask);

        [DllImport("VertexTweakerCore")] static extern int npSelectConnected(
            ref npMeshData model, float strength, bool clear);

        [DllImport("VertexTweakerCore")] static extern int npSelectRect(
            ref npMeshData model, ref Matrix4x4 viewproj, Vector2 rmin, Vector2 rmax, Vector3 campos, float strength, bool frontfaceOnly);

        [DllImport("VertexTweakerCore")] static extern int npSelectLasso(
            ref npMeshData model, ref Matrix4x4 viewproj, Vector2[] lasso, int numLassoPoints, Vector3 campos, float strength, bool frontfaceOnly);
        
        [DllImport("VertexTweakerCore")] static extern int npSelectBrush(
            ref npMeshData model,
            Vector3 pos, float radius, float strength, int num_bsamples, IntPtr bsamples);

        [DllImport("VertexTweakerCore")] static extern int npUpdateSelection(
            ref npMeshData model,
            ref Vector3 selection_pos, ref Vector3 selection_normal);

        [DllImport("VertexTweakerCore")] static extern int npAssignVertices(
            ref npMeshData model, Vector3 value, Matrix4x4 transform, int xyz, bool mask);
        
        [DllImport("VertexTweakerCore")] static extern int npMoveVertices(
            ref npMeshData model, Vector3 amount, bool mask);
        
        [DllImport("VertexTweakerCore")] static extern int npRotatePivotVertices(
            ref npMeshData model, Quaternion amount, Vector3 pivotPos, Quaternion pivotRot, bool mask);

        [DllImport("VertexTweakerCore")] static extern int npScaleVertices(
            ref npMeshData model, Vector3 amount, Vector3 pivotPos, Quaternion pivotRot, bool mask);

        [DllImport("VertexTweakerCore")] static extern int npBuildMirroringRelation(
            ref npMeshData model, Vector3 plane_normal, float epsilon, IntPtr relation);

        [DllImport("VertexTweakerCore")] static extern void npApplyMirroring(
            ref npMeshData model, IntPtr relation, Vector3 plane_normal,
            IntPtr iopoints, IntPtr ionormals, IntPtr iotangents);

        [DllImport("VertexTweakerCore")] static extern void npProjectVertices(
            ref npMeshData model, ref npMeshData target, IntPtr ray_dirs, npProjectVerticesMode mode, float max_distance, int PNT, bool mask);
        [DllImport("VertexTweakerCore")] static extern void npProjectVerticesRadial(
            ref npMeshData model, ref npMeshData target, Vector3 center, npProjectVerticesMode mode, float max_distance, int PNT, bool mask);
        [DllImport("VertexTweakerCore")] static extern void npProjectVerticesDirectional(
            ref npMeshData model, ref npMeshData target, Vector3 ray_dir, npProjectVerticesMode mode, float max_distance, int PNT, bool mask);

        [DllImport("VertexTweakerCore")] static extern void npApplySkinning(
            ref npSkinData skin,
            IntPtr ipoints, IntPtr inormals, IntPtr itangents,
            IntPtr opoints, IntPtr onormals, IntPtr otangents);

        [DllImport("VertexTweakerCore")] static extern void npApplyReverseSkinning(
            ref npSkinData skin,
            IntPtr ipoints, IntPtr inormals, IntPtr itangents,
            IntPtr opoints, IntPtr onormals, IntPtr otangents);
        
        [DllImport("VertexTweakerCore")] static extern int npGenerateNormals(
            ref npMeshData model, IntPtr dst);
        [DllImport("VertexTweakerCore")] static extern int npGenerateTangents(
            ref npMeshData model, IntPtr dst);

        [DllImport("VertexTweakerCore")] static extern void npInitializePenInput();
        #endregion
    }
#endif
}
