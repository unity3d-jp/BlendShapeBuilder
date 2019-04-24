using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif


namespace UTJ.VertexTweaker
{
    [ExecuteInEditMode]
    public partial class VertexTweaker : MonoBehaviour
    {
#if UNITY_EDITOR
        [Serializable]
        public class History
        {
            public int index;
            public Mesh mesh;
            public Vector3[] points;
            public Vector3[] normals;
            public Vector4[] tangents;
        }


        VertexTweakerSettings m_settings;

        // internal resources
        [SerializeField] Mesh m_meshTarget;
        [SerializeField] Mesh m_meshPoint;
        [SerializeField] Mesh m_meshVector;
        [SerializeField] Mesh m_meshLasso;
        [SerializeField] Material m_matVisualize;
        [SerializeField] Material m_matOverlay;

        ComputeBuffer m_cbArgPoints;
        ComputeBuffer m_cbArgVectors;
        ComputeBuffer m_cbPoints;
        ComputeBuffer m_cbNormals;
        ComputeBuffer m_cbTangents;
        ComputeBuffer m_cbSelection;
        Texture2D m_texBrushSamples;
        CommandBuffer m_cmdDraw;
        bool m_cbPointsDirty;
        bool m_cbNormalsDirty;
        bool m_cbTangentsDirty;
        bool m_cbSelectionDirty;

        bool m_skinned;
        PinnedList<Vector3> m_points, m_pointsPredeformed, m_pointsBase, m_pointsBasePredeformed;
        PinnedList<Vector3> m_normals, m_normalsPredeformed, m_normalsBase, m_normalsBasePredeformed;
        PinnedList<Vector4> m_tangents, m_tangentsPredeformed, m_tangentsBase, m_tangentsBasePredeformed;
        PinnedList<Vector2> m_uv;
        PinnedList<int> m_indices;
        PinnedList<int> m_mirrorRelation;
        PinnedList<float> m_selection;

        PinnedList<BoneWeight> m_boneWeights;
        PinnedList<Matrix4x4> m_bindposes;
        PinnedList<Matrix4x4> m_boneMatrices;

        bool m_editing;
        bool m_edited;
        int m_numSelected = 0;
        bool m_rayHit;
        int m_rayHitTriangle = -1;
        int m_rayHitVertex = -1;
        Vector3 m_rayPos;
        Vector3 m_rayVertexPos;
        Vector3 m_selectionPos;
        Vector3 m_selectionNormal;
        Quaternion m_selectionRot;
        Vector2 m_rectStartPoint;
        Vector2 m_rectEndPoint;
        List<Vector2> m_lassoPoints = new List<Vector2>();

        [SerializeField] History m_history = new History();
        int m_historyIndex = 0;

        npMeshData m_npModelData = new npMeshData();
        npSkinData m_npSkinData = new npSkinData();

        public bool editing
        {
            get { return m_editing; }
            set
            {
                if (value && !m_editing) BeginEdit();
                if (!value && m_editing) EndEdit();
            }
        }
        public bool edited
        {
            get { return m_edited; }
            set { m_edited = value; }
        }

        public VertexTweakerSettings settings { get { return m_settings; } }
        public Mesh mesh { get { return m_meshTarget; } }
        public Vector3 selectionPosition { get { return m_selectionPos; } }
        public Vector3 selectionNormal { get { return m_selectionNormal; } }
        public bool skinned { get { return m_skinned; } }

        public float[] selection
        {
            get { return (float[])m_selection.Array.Clone(); }
            set
            {
                if (value != null && value.Length == m_selection.Count)
                {
                    Array.Copy(value, m_selection.Array, m_selection.Count);
                    UpdateSelection();
                }
            }
        }


        Mesh GetTargetMesh()
        {
            var smr = GetComponent<SkinnedMeshRenderer>();
            if (smr) { return smr.sharedMesh; }

            var mf = GetComponent<MeshFilter>();
            if (mf) { return mf.sharedMesh; }

            return null;
        }

        void BeginEdit()
        {
            var tmesh = GetTargetMesh();
            if (tmesh == null)
            {
                Debug.LogWarning("Target mesh is null.");
                return;
            }
            else if (!tmesh.isReadable)
            {
                Debug.LogWarning("Target mesh is not readable.");
                return;
            }

            if (m_settings == null)
            {
                var ds = AssetDatabase.LoadAssetAtPath<VertexTweakerSettings>(AssetDatabase.GUIDToAssetPath("9b3ba32ae87b3a94e9cf70157c96f58a"));
                if (ds != null)
                {
                    m_settings = Instantiate(ds);
                }
                if (m_settings == null)
                {
                    m_settings = ScriptableObject.CreateInstance<VertexTweakerSettings>();
                }
            }

            if (m_meshPoint == null)
            {
                float l = 0.5f;
                var p = new Vector3[] {
                    new Vector3(-l,-l, l),
                    new Vector3( l,-l, l),
                    new Vector3( l,-l,-l),
                    new Vector3(-l,-l,-l),

                    new Vector3(-l, l, l),
                    new Vector3( l, l, l),
                    new Vector3( l, l,-l),
                    new Vector3(-l, l,-l),
                };

                m_meshPoint = new Mesh();
                m_meshPoint.vertices = new Vector3[] {
                    p[0], p[1], p[2], p[3],
                    p[7], p[4], p[0], p[3],
                    p[4], p[5], p[1], p[0],
                    p[6], p[7], p[3], p[2],
                    p[5], p[6], p[2], p[1],
                    p[7], p[6], p[5], p[4],
                };
                m_meshPoint.SetIndices(new int[] {
                    3, 1, 0, 3, 2, 1,
                    7, 5, 4, 7, 6, 5,
                    11, 9, 8, 11, 10, 9,
                    15, 13, 12, 15, 14, 13,
                    19, 17, 16, 19, 18, 17,
                    23, 21, 20, 23, 22, 21,
                }, MeshTopology.Triangles, 0);
                m_meshPoint.UploadMeshData(false);
            }

            if (m_meshVector == null)
            {
                m_meshVector = new Mesh();
                m_meshVector.vertices = new Vector3[2] { Vector3.zero, Vector3.zero };
                m_meshVector.uv = new Vector2[2] { Vector2.zero, Vector2.one };
                m_meshVector.SetIndices(new int[2] { 0, 1 }, MeshTopology.Lines, 0);
                m_meshVector.UploadMeshData(false);
            }

            if (m_meshLasso == null)
            {
                m_meshLasso = new Mesh();
            }

            if (m_matVisualize == null)
                m_matVisualize = new Material(AssetDatabase.LoadAssetAtPath<Shader>(AssetDatabase.GUIDToAssetPath("5786f144ee220ce4ea056f3f5ef4af19")));
            if (m_matOverlay == null)
                m_matOverlay = new Material(AssetDatabase.LoadAssetAtPath<Shader>(AssetDatabase.GUIDToAssetPath("dada189530d1c844588658636810ae94")));

            if (m_meshTarget == null ||
                m_meshTarget != tmesh ||
                (m_points != null && m_meshTarget.vertexCount != m_points.Count))
            {
                m_meshTarget = tmesh;
                m_points = m_pointsBase = m_pointsPredeformed = m_pointsBasePredeformed = null;
                m_normals = m_normalsBase = m_normalsPredeformed = m_normalsBasePredeformed = null;
                m_tangents = m_tangentsBase = m_tangentsPredeformed = m_tangentsBasePredeformed = null;
                m_indices = null;
                m_mirrorRelation = null;
                m_selection = null;

                ReleaseComputeBuffers();
            }

            if (m_meshTarget != null)
            {
                m_points = m_pointsPredeformed = new PinnedList<Vector3>(m_meshTarget.vertices);

                m_uv = new PinnedList<Vector2>(m_meshTarget.uv);

                m_normals = new PinnedList<Vector3>(m_meshTarget.normals);
                if (m_normals.Count == 0)
                {
                    m_meshTarget.RecalculateNormals();
                    m_normals = new PinnedList<Vector3>(m_meshTarget.normals);
                }
                m_normalsPredeformed = m_normals;

                m_tangents = new PinnedList<Vector4>(m_meshTarget.tangents);
                if (m_tangents.Count == 0)
                {
                    m_meshTarget.RecalculateTangents();
                    m_tangents = new PinnedList<Vector4>(m_meshTarget.tangents);
                }
                m_tangentsPredeformed = m_tangents;

                if (m_pointsBase == null)
                {
                    m_pointsBase = m_pointsBasePredeformed = m_points.Clone();
                    m_normalsBase = m_normalsBasePredeformed = m_normals.Clone();
                    m_tangentsBase = m_tangentsBasePredeformed = m_tangents.Clone();
                }

                m_indices = new PinnedList<int>(m_meshTarget.triangles);
                m_selection = new PinnedList<float>(m_points.Count);

                m_cbPointsDirty = true;
                m_cbNormalsDirty = true;
                m_cbTangentsDirty = true;
                m_cbSelectionDirty = true;

                m_npModelData.num_vertices = m_points.Count;
                m_npModelData.num_triangles = m_indices.Count / 3;
                m_npModelData.indices = m_indices;
                m_npModelData.vertices = m_points;
                m_npModelData.normals = m_normals;
                m_npModelData.tangents = m_tangents;
                m_npModelData.uv = m_uv;
                m_npModelData.selection = m_selection;

                var smr = GetComponent<SkinnedMeshRenderer>();
                if (smr != null && smr.bones.Length > 0)
                {
                    m_skinned = true;

                    m_boneWeights = new PinnedList<BoneWeight>(m_meshTarget.boneWeights);
                    m_bindposes = new PinnedList<Matrix4x4>(m_meshTarget.bindposes);
                    m_boneMatrices = new PinnedList<Matrix4x4>(m_bindposes.Count);

                    m_pointsPredeformed = m_points.Clone();
                    m_pointsBasePredeformed = m_pointsBase.Clone();
                    m_normalsPredeformed = m_normals.Clone();
                    m_normalsBasePredeformed = m_normalsBase.Clone();
                    m_tangentsPredeformed = m_tangents.Clone();
                    m_tangentsBasePredeformed = m_tangentsBase.Clone();

                    m_npSkinData.num_vertices = m_boneWeights.Count;
                    m_npSkinData.num_bones = m_bindposes.Count;
                    m_npSkinData.weights = m_boneWeights;
                    m_npSkinData.bindposes = m_bindposes;
                    m_npSkinData.bones = m_boneMatrices;
                }
            }

            m_settings.InitializeBrushData();

            m_forceDisableRecalculation = true;
            UpdateTransform();
            UpdateVertices();
            PushUndo();
            m_forceDisableRecalculation = false;

            Tools.current = Tool.None;
            m_editing = true;
        }

        void EndEdit()
        {
            ReleaseComputeBuffers();
            if (Tools.current == Tool.None)
                Tools.current = Tool.Move;
            m_editing = false;
        }

        void ReleaseComputeBuffers()
        {
            if (m_cbArgPoints != null) { m_cbArgPoints.Release(); m_cbArgPoints = null; }
            if (m_cbArgVectors != null) { m_cbArgVectors.Release(); m_cbArgVectors = null; }
            if (m_cbPoints != null) { m_cbPoints.Release(); m_cbPoints = null; }
            if (m_cbNormals != null) { m_cbNormals.Release(); m_cbNormals = null; }
            if (m_cbTangents != null) { m_cbTangents.Release(); m_cbTangents = null; }
            if (m_cbSelection != null) { m_cbSelection.Release(); m_cbSelection = null; }
            if (m_texBrushSamples != null) { DestroyImmediate(m_texBrushSamples); m_texBrushSamples = null; }
            if (m_cmdDraw != null) { m_cmdDraw.Release(); m_cmdDraw = null; }
        }

        void UpdateComputeBuffers()
        {
            if (m_cbPoints == null && m_points != null && m_points.Count > 0)
            {
                m_cbPoints = new ComputeBuffer(m_points.Count, 12);
            }
            if(m_cbPointsDirty)
            {
                m_cbPoints.SetData(m_points.List);
                m_cbPointsDirty = false;
            }

            if (m_cbNormals == null && m_normals != null && m_normals.Count > 0)
            {
                m_cbNormals = new ComputeBuffer(m_normals.Count, 12);
            }
            if (m_cbNormalsDirty)
            {
                m_cbNormals.SetData(m_normals.List);
                m_cbNormalsDirty = false;
            }

            if (m_cbTangents == null && m_tangents != null && m_tangents.Count > 0)
            {
                m_cbTangents = new ComputeBuffer(m_tangents.Count, 16);
            }
            if (m_cbTangentsDirty)
            {
                m_cbTangents.SetData(m_tangents.List);
                m_cbTangentsDirty = false;
            }

            if (m_cbSelection == null && m_selection != null && m_selection.Count > 0)
            {
                m_cbSelection = new ComputeBuffer(m_selection.Count, 4);
            }
            if(m_cbSelectionDirty)
            {
                m_cbSelection.SetData(m_selection.List);
                m_cbSelectionDirty = false;
            }

            if (m_cbArgPoints == null && m_points != null && m_points.Count > 0)
            {
                m_cbArgPoints = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
                m_cbArgPoints.SetData(new uint[5] { m_meshPoint.GetIndexCount(0), (uint)m_points.Count, 0, 0, 0 });

                m_cbArgVectors = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
                m_cbArgVectors.SetData(new uint[5] { m_meshVector.GetIndexCount(0), (uint)m_points.Count, 0, 0, 0 });
            }
        }

        void Start()
        {
            npInitializePenInput();
        }

        void OnEnable()
        {
            Undo.undoRedoPerformed += OnUndoRedo;
        }

        void OnDisable()
        {
            EndEdit();
            Undo.undoRedoPerformed -= OnUndoRedo;
        }

        void LateUpdate()
        {
            if (m_editing)
            {
                UpdateTransform();
            }
        }


        public int OnSceneGUI()
        {
            if (!isActiveAndEnabled || m_points == null)
                return 0;

            var et = Event.current.rawType;
            int ret = 0;
            ret |= HandleEditTools();
            ret |= HandleSelectTools();
            if (et == EventType.Repaint)
                OnRepaint();

            //Debug.Log("" + m_toolState + ", " + et + "(" + Event.current.type + "), " + GUIUtility.hotControl);
            return ret;
        }


        Vector3 m_prevMove;
        Quaternion m_prevRot;
        Vector3 m_prevScale;
        bool m_toolHanding = false;

        enum ToolState
        {
            Neutral,
            AxisMove,
            FreeMove,
            Rotation,
            Scale,
            Projection,
            Selection,
        }
        ToolState m_toolState = ToolState.Neutral;

        int HandleEditTools()
        {
            Event e = Event.current;
            var et = e.type;
            if (et == EventType.Ignore)
            {
                // handle out-of-window mouse move
                if ((e.rawType == EventType.MouseDrag || e.rawType == EventType.MouseUp) && e.button == 0)
                    et = e.rawType;
                else
                    return 0;
            }
            else if (!e.isMouse && et != EventType.Layout && et != EventType.Repaint)
                return 0;
            if (e.isMouse && m_toolState == ToolState.Neutral && (e.shift || e.control || e.alt))
                return 0;
            if (m_toolState == ToolState.Selection)
                return 0;

            // check if model has been changed
            if (m_meshTarget != GetTargetMesh())
                BeginEdit();

            float pickRectSize = 15.0f;

            var editMode = m_settings.editMode;
            bool mouseDown = et == EventType.MouseDown;
            bool mouseUp = et == EventType.MouseUp;
            bool mouseDrag = et == EventType.MouseDrag;
            bool mouseMove = et == EventType.MouseMove;
            bool isMouse = e.isMouse;

            int ret = 0;
            bool handled = false;
            var t = GetComponent<Transform>();

            Action searchNearestVertex = () => {
                if (PickVertex(e, pickRectSize, true, ref m_rayHitVertex, ref m_rayVertexPos)) { }
                else { m_rayHitVertex = -1; }

                bool prevRayHit = m_rayHit;
                m_rayHit = Raycast(e, ref m_rayPos, ref m_rayHitTriangle);
                if (m_rayHit || prevRayHit)
                    ret |= (int)SceneGUIState.Repaint;
            };
            Action selectVertexAtMousePosition = () => {
                if (m_settings.softOp)
                {
                    ClearSelection();
                    if (m_rayHitVertex != -1)
                    {
                        var bd = m_settings.activeBrush;
                        SelectBrush(m_rayVertexPos, bd.radius, bd.strength, bd.samples);
                    }
                    UpdateSelection();
                }
                else
                {
                    if (m_rayHitVertex != -1)
                    {
                        if (m_selection[m_rayHitVertex] == 0.0f)
                        {
                            ClearSelection();
                            m_selection[m_rayHitVertex] = 1.0f;
                            UpdateSelection();
                        }
                    }
                    else
                    {
                        ClearSelection();
                        UpdateSelection();
                    }
                }
            };

            if (editMode == EditMode.Move)
            {
                if ((m_toolState == ToolState.Neutral && (mouseMove || mouseDrag)) ||
                    (m_toolState == ToolState.FreeMove && !m_settings.softOp))
                {
                    searchNearestVertex();
                }

                var move = Vector3.zero;

                // 3 axis handles
                if (m_numSelected > 0 && (m_toolState == ToolState.Neutral || m_toolState == ToolState.AxisMove))
                {
                    var pivotRot = Quaternion.identity;
                    switch (m_settings.coordinate)
                    {
                        case Coordinate.Pivot:
                            pivotRot = m_settings.pivotRot;
                            break;
                        case Coordinate.Local:
                            pivotRot = t.rotation;
                            break;
                    }

                    EditorGUI.BeginChangeCheck();
                    move = VertexHandles.AxisMoveHandle(m_selectionPos, pivotRot);
                    if (EditorGUI.EndChangeCheck())
                        handled = true;
                    if (VertexHandles.axisMoveHandleGainedControl)
                    {
                        m_toolState = ToolState.AxisMove;
                        m_prevMove = m_selectionPos;
                    }
                }

                // select vertex
                if (mouseDown && e.button == 0 && m_toolState == ToolState.Neutral)
                    selectVertexAtMousePosition();

                // free move handle
                if ((m_toolState == ToolState.Neutral && !VertexHandles.axisMoveHandleNear) || m_toolState == ToolState.FreeMove)
                {
                    if ((m_rayHitVertex != -1 || m_settings.softOp) || m_toolState == ToolState.FreeMove)
                    {
                        Vector3 handlePos = m_rayVertexPos;

                        EditorGUI.BeginChangeCheck();
                        move = VertexHandles.FreeMoveHandle(handlePos, pickRectSize);
                        if (EditorGUI.EndChangeCheck())
                            handled = true;
                        if (VertexHandles.freeMoveHandleGainedControl)
                        {
                            m_toolState = ToolState.FreeMove;
                            m_prevMove = handlePos;
                        }
                    }
                }

                if (handled)
                {
                    var diff = move - m_prevMove;
                    m_prevMove = move;
                    if (m_numSelected > 0)
                        ApplyMove(diff * 1.0f, Coordinate.World, false);
                }
                if (m_toolState != ToolState.Neutral && (!VertexHandles.axisMoveHandleHasControl && !VertexHandles.freeMoveHandleHasControl))
                {
                    m_toolState = ToolState.Neutral;
                    PushUndo();
                }
            }
            else if (editMode == EditMode.Rotate)
            {
                if (m_toolState == ToolState.Neutral && m_settings.softOp && (mouseMove || mouseDrag) && !VertexHandles.rotationHandleNear)
                {
                    searchNearestVertex();
                }

                if (m_numSelected > 0 || m_settings.softOp)
                {
                    var pivotRot = Quaternion.identity;
                    switch (m_settings.coordinate)
                    {
                        case Coordinate.Pivot:
                            pivotRot = m_settings.pivotRot;
                            break;
                        case Coordinate.Local:
                            pivotRot = t.rotation;
                            break;
                    }

                    var handlePos = m_settings.softOp ? m_rayVertexPos : m_settings.pivotPos;

                    EditorGUI.BeginChangeCheck();
                    var rot = VertexHandles.RotationHandle(pivotRot, handlePos);
                    if (EditorGUI.EndChangeCheck())
                    {
                        handled = true;
                        var diff = VertexHandles.freeRotating ? rot : (Quaternion.Inverse(m_prevRot) * rot);
                        m_prevRot = rot;
                        if (m_numSelected > 0)
                            ApplyRotatePivot(Quaternion.Inverse(diff), handlePos, pivotRot, Coordinate.Pivot, false);
                    }
                    if (VertexHandles.rotationHandleGainedControl)
                    {
                        m_toolState = ToolState.Rotation;
                        m_prevRot = pivotRot;
                        if (m_settings.softOp)
                        {
                            ClearSelection();
                            var bd = m_settings.activeBrush;
                            SelectBrush(handlePos, bd.radius, bd.strength, bd.samples);
                            UpdateSelection();
                        }
                    }
                }
                if (m_toolState != ToolState.Neutral && !VertexHandles.rotationHandleHasControl)
                {
                    PushUndo();
                    m_toolState = ToolState.Neutral;
                }
            }
            else if (editMode == EditMode.Scale)
            {
                if (m_toolState == ToolState.Neutral && m_settings.softOp && (mouseMove || mouseDrag) && !VertexHandles.scaleHandleNear)
                {
                    searchNearestVertex();
                }

                if (m_numSelected > 0 || m_settings.softOp)
                {
                    var pivotRot = Quaternion.identity;
                    switch (m_settings.coordinate)
                    {
                        case Coordinate.Pivot:
                            pivotRot = m_settings.pivotRot;
                            break;
                        case Coordinate.Local:
                            pivotRot = t.rotation;
                            break;
                    }

                    var handlePos = m_settings.softOp ? m_rayVertexPos : m_settings.pivotPos;

                    EditorGUI.BeginChangeCheck();
                    var scale = VertexHandles.ScaleHandle(Vector3.one, handlePos, pivotRot);
                    if (EditorGUI.EndChangeCheck())
                    {
                        handled = true;
                        var diff = scale - m_prevScale;
                        m_prevScale = scale;
                        if (m_numSelected > 0)
                            ApplyScale(Vector3.one + diff, handlePos, pivotRot, Coordinate.Pivot, false);
                    }
                    if (VertexHandles.scaleHandleGainedControl)
                    {
                        m_toolState = ToolState.Scale;
                        m_prevScale = Vector3.one;
                        if (m_settings.softOp)
                        {
                            ClearSelection();
                            var bd = m_settings.activeBrush;
                            SelectBrush(handlePos, bd.radius, bd.strength, bd.samples);
                            UpdateSelection();
                        }
                    }
                }
                if (m_toolState != ToolState.Neutral && !VertexHandles.scaleHandleHasControl)
                {
                    PushUndo();
                    m_toolState = ToolState.Neutral;
                }
            }
            else if (editMode == EditMode.Projection)
            {
                if (m_toolState == ToolState.Neutral && (mouseMove || mouseDrag) && !VertexHandles.rotationHandleNear)
                {
                    searchNearestVertex();
                }

                if (m_settings.projRayDir == ProjectionRayDirection.Radial)
                {
                    EditorGUI.BeginChangeCheck();
                    var center = VertexHandles.AxisMoveHandle(m_settings.projRadialCenter, t.rotation);
                    if (EditorGUI.EndChangeCheck())
                    {
                        handled = true;
                        m_settings.projRadialCenter = center;
                    }
                }
                if (m_settings.projRayDir == ProjectionRayDirection.Directional)
                {
                    EditorGUI.BeginChangeCheck();
                    var rot = VertexHandles.RotationHandle(Quaternion.identity, m_rayVertexPos);
                    if (EditorGUI.EndChangeCheck())
                    {
                        var diff = VertexHandles.freeRotating ? rot : (Quaternion.Inverse(m_prevRot) * rot);
                        m_prevRot = rot;
                        m_settings.projDirection = (diff * m_settings.projDirection).normalized;
                        handled = true;
                    }
                    if (mouseDown && m_toolState == ToolState.Neutral && VertexHandles.rotationHandleHasControl)
                    {
                        m_toolState = ToolState.Projection;
                        m_prevRot = Quaternion.identity;
                    }
                }
                if (mouseUp)
                    m_toolState = ToolState.Neutral;
            }

            if (m_toolState != ToolState.Neutral && isMouse && e.button == 0)
                e.Use();

            if (handled)
            {
                m_toolHanding = true;
                ret |= (int)SceneGUIState.Repaint;
            }
            else if (m_toolHanding && mouseUp)
            {
                m_toolHanding = false;
                ret |= (int)SceneGUIState.Repaint;
            }

            return ret;
        }


        static readonly int s_selectID = 0x7fe8710;

        int HandleSelectTools()
        {
            if (m_toolState != ToolState.Neutral && m_toolState != ToolState.Selection)
                return 0;

            int cid = s_selectID;
            Event e = Event.current;
            var et = e.GetTypeForControl(cid);
            if (et == EventType.Ignore)
            {
                // handle out-of-window mouse move
                if ((e.rawType == EventType.MouseDrag || e.rawType == EventType.MouseUp) && e.button == 0)
                    et = e.rawType;
                else
                    return 0;
            }
            else if (!e.isMouse || e.button != 0 || (e.alt && m_toolState != ToolState.Selection))
                return 0;

            var toolStateOld = m_toolState;
            int ret = 0;

            bool mouseDown = et == EventType.MouseDown;
            bool mouseUp = et == EventType.MouseUp;
            bool mouseDrag = et == EventType.MouseDrag;
            bool mouseMove = et == EventType.MouseMove;

            bool handled = false;
            if (!m_settings.softOp)
            {
                var selectMode = m_settings.selectMode;
                float selectSign = e.control ? -1.0f : 1.0f;
                if (selectMode == SelectMode.Single)
                {
                    if (mouseDown)
                    {
                        m_toolState = ToolState.Selection;
                    }
                    if (m_toolState == ToolState.Selection)
                    {
                        if (mouseDown || mouseDrag)
                        {
                            if (!e.shift && !e.control)
                                ClearSelection();

                            if (settings.selectVertex && SelectVertex(e, selectSign, settings.selectFrontSideOnly))
                                handled = true;
                            else if (settings.selectTriangle && SelectTriangle(e, selectSign))
                                handled = true;
                            else if (m_rayHit)
                                handled = true;
                        }
                        else if (mouseUp)
                        {
                            m_toolState = ToolState.Neutral;
                            handled = true;
                        }
                    }
                }
                else if (selectMode == SelectMode.Rect)
                {
                    if (mouseDown)
                    {
                        m_rectStartPoint = m_rectEndPoint = e.mousePosition;
                        handled = true;
                        m_toolState = ToolState.Selection;
                    }
                    else if (m_toolState == ToolState.Selection)
                    {
                        if (mouseDrag)
                        {
                            m_rectEndPoint = e.mousePosition;
                            handled = true;
                        }
                        else if (mouseUp)
                        {
                            m_toolState = ToolState.Neutral;
                            if (!e.shift && !e.control)
                                ClearSelection();

                            m_rectEndPoint = e.mousePosition;
                            SelectRect(m_rectStartPoint, m_rectEndPoint, selectSign, settings.selectFrontSideOnly);
                            m_rectStartPoint = m_rectEndPoint = -Vector2.one;
                            handled = true;
                        }
                    }
                }
                else if (selectMode == SelectMode.Lasso)
                {
                    if (mouseDown)
                    {
                        m_lassoPoints.Clear();
                        m_meshLasso.Clear();
                        m_toolState = ToolState.Selection;
                    }
                    if (m_toolState == ToolState.Selection)
                    {
                        if (mouseDown || mouseDrag)
                        {
                            m_lassoPoints.Add(ScreenCoord11(e.mousePosition));
                            handled = true;

                            m_meshLasso.Clear();
                            if (m_lassoPoints.Count > 1)
                            {
                                var vertices = new Vector3[m_lassoPoints.Count];
                                var indices = new int[(vertices.Length - 1) * 2];
                                for (int i = 0; i < vertices.Length; ++i)
                                {
                                    vertices[i].x = m_lassoPoints[i].x;
                                    vertices[i].y = m_lassoPoints[i].y;
                                }
                                for (int i = 0; i < vertices.Length - 1; ++i)
                                {
                                    indices[i * 2 + 0] = i;
                                    indices[i * 2 + 1] = i + 1;
                                }
                                m_meshLasso.vertices = vertices;
                                m_meshLasso.SetIndices(indices, MeshTopology.Lines, 0);
                                m_meshLasso.UploadMeshData(false);
                            }
                        }
                        else if (mouseUp)
                        {
                            m_toolState = ToolState.Neutral;
                            if (!e.shift && !e.control)
                                ClearSelection();

                            SelectLasso(m_lassoPoints.ToArray(), selectSign, settings.selectFrontSideOnly);
                            m_lassoPoints.Clear();
                            m_meshLasso.Clear();
                            handled = true;
                        }
                    }
                }
                else if (selectMode == SelectMode.Brush)
                {
                    if (mouseDown || mouseDrag || mouseMove)
                    {
                        bool prevRayHit = m_rayHit;
                        m_rayHit = Raycast(e, ref m_rayPos, ref m_rayHitTriangle);
                        if (m_rayHit || prevRayHit)
                            handled = true;
                    }
                    if (mouseDown)
                    {
                        m_toolState = ToolState.Selection;
                        if (!e.shift && !e.control)
                            ClearSelection();
                    }
                    if (m_toolState == ToolState.Selection)
                    {
                        if (mouseDown || mouseDrag)
                        {
                            if (m_rayHit)
                            {
                                var bd = m_settings.activeBrush;
                                SelectBrush(m_rayPos, bd.radius, bd.strength * selectSign, bd.samples);
                            }
                        }
                        else if (mouseUp)
                        {
                            m_toolState = ToolState.Neutral;
                            handled = true;
                        }
                    }

                }

                UpdateSelection();
            }
            else
            {
                if (mouseDown)
                    m_toolState = ToolState.Selection;
                else if (mouseUp && m_toolState == ToolState.Selection)
                    m_toolState = ToolState.Neutral;
            }

            if (mouseDown && m_toolState == ToolState.Selection)
                GUIUtility.hotControl = cid;
            else if (mouseUp && toolStateOld == ToolState.Selection && m_toolState == ToolState.Neutral)
                GUIUtility.hotControl = 0;
            e.Use();

            if (handled)
            {
                ret |= (int)SceneGUIState.Repaint;
            }
            return ret;
        }


        void OnDrawGizmosSelected()
        {
            if (!m_editing) { return; }

            if (m_matVisualize == null || m_meshPoint == null || m_meshVector == null)
            {
                Debug.LogWarning("Some resources are missing.\n");
                return;
            }

            bool softOp = (m_settings.softOp && (m_settings.editMode == EditMode.Move || m_settings.editMode == EditMode.Rotate || m_settings.editMode == EditMode.Scale));
            bool brushMode = (m_settings.selectMode == SelectMode.Brush) || softOp;
            var brushPos = softOp ? m_rayVertexPos : m_rayPos;

            var trans = GetComponent<Transform>();
            var matrix = trans.localToWorldMatrix;
            var renderer = GetComponent<Renderer>();

            UpdateComputeBuffers();

            m_matVisualize.SetMatrix("_Transform", matrix);
            m_matVisualize.SetFloat("_VertexSize", m_settings.vertexSize);
            m_matVisualize.SetFloat("_NormalSize", m_settings.normalSize);
            m_matVisualize.SetFloat("_TangentSize", m_settings.tangentSize);
            m_matVisualize.SetFloat("_BinormalSize", m_settings.binormalSize);
            m_matVisualize.SetColor("_VertexColor", m_settings.vertexColor);
            m_matVisualize.SetColor("_VertexColor2", m_settings.vertexColor2);
            m_matVisualize.SetColor("_NormalColor", m_settings.normalColor);
            m_matVisualize.SetColor("_TangentColor", m_settings.tangentColor);
            m_matVisualize.SetColor("_BinormalColor", m_settings.binormalColor);
            m_matVisualize.SetInt("_OnlySelected", m_settings.showSelectedOnly ? 1 : 0);

            if (m_rayHit)
            {
                m_matVisualize.SetColor("_VertexColor3", m_settings.vertexColor3);
                if (brushMode)
                {
                    var bd = m_settings.activeBrush;
                    if (m_texBrushSamples == null)
                    {
                        m_texBrushSamples = new Texture2D(bd.samples.Length, 1, TextureFormat.RFloat, false);
                    }
                    m_texBrushSamples.LoadRawTextureData(bd.samples, bd.samples.Length * 4);
                    m_texBrushSamples.Apply();
                    m_matVisualize.SetVector("_BrushPos", new Vector4(brushPos.x, brushPos.y, brushPos.z, bd.radius));
                    m_matVisualize.SetTexture("_BrushSamples", m_texBrushSamples);
                }
                else
                {
                    m_matVisualize.SetVector("_BrushPos", brushPos);
                }
            }
            else
            {
                m_matVisualize.SetColor("_VertexColor3", Color.black);
            }

            if (m_cbPoints != null) m_matVisualize.SetBuffer("_Points", m_cbPoints);
            if (m_cbNormals != null) m_matVisualize.SetBuffer("_Normals", m_cbNormals);
            if (m_cbTangents != null) m_matVisualize.SetBuffer("_Tangents", m_cbTangents);
            if (m_cbSelection != null) m_matVisualize.SetBuffer("_Selection", m_cbSelection);

            if (m_cmdDraw == null)
            {
                m_cmdDraw = new CommandBuffer();
                m_cmdDraw.name = "VertexTweaker";
            }
            m_cmdDraw.Clear();

            // overlay
            if (m_settings.modelOverlay != ModelOverlay.None)
            {
                if (m_cbPoints != null) m_matOverlay.SetBuffer("_Points", m_cbPoints);
                if (m_cbNormals != null) m_matOverlay.SetBuffer("_Normals", m_cbNormals);
                if (m_cbTangents != null) m_matOverlay.SetBuffer("_Tangents", m_cbTangents);
                if (m_cbSelection != null) m_matOverlay.SetBuffer("_Selection", m_cbSelection);

                int pass = (int)m_settings.modelOverlay - 1;
                for (int si = 0; si < m_meshTarget.subMeshCount; ++si)
                    m_cmdDraw.DrawRenderer(renderer, m_matOverlay, si, pass);
            }

            // visualize brush range
            if (m_settings.showBrushRange && m_rayHit && brushMode)
                m_cmdDraw.DrawRenderer(renderer, m_matVisualize, 0, (int)VisualizeType.BrushRange);

            if (m_settings.visualize)
            {
                // visualize vertices
                if (m_settings.showVertices && m_cbPoints != null)
                    m_cmdDraw.DrawMeshInstancedIndirect(m_meshPoint, 0, m_matVisualize, (int)VisualizeType.Vertices, m_cbArgPoints);

                // visualize binormals
                if (m_settings.showBinormals && m_cbNormals != null && m_cbTangents != null)
                    m_cmdDraw.DrawMeshInstancedIndirect(m_meshVector, 0, m_matVisualize, (int)VisualizeType.Binormals, m_cbArgVectors);

                // visualize tangents
                if (m_settings.showTangents && m_cbTangents != null)
                    m_cmdDraw.DrawMeshInstancedIndirect(m_meshVector, 0, m_matVisualize, (int)VisualizeType.Tangents, m_cbArgVectors);

                // visualize normals
                if (m_settings.showNormals && m_cbNormals != null)
                    m_cmdDraw.DrawMeshInstancedIndirect(m_meshVector, 0, m_matVisualize, (int)VisualizeType.Normals, m_cbArgVectors);
            }

            if (m_settings.showBrushRange && m_rayHit)
            {
                // ray pos
                if (brushMode)
                    m_cmdDraw.DrawMesh(m_meshPoint, Matrix4x4.identity, m_matVisualize, 0, (int)VisualizeType.RayPosition);
            }
            if(m_settings.editMode == EditMode.Projection)
            {
                if (m_settings.projRayDir == ProjectionRayDirection.Directional)
                {
                    m_matVisualize.SetVector("_BrushPos", m_rayVertexPos);
                    m_matVisualize.SetVector("_Direction", m_settings.projDirection);
                    m_cmdDraw.DrawMesh(m_meshPoint, Matrix4x4.identity, m_matVisualize, 0, (int)VisualizeType.RayPosition);
                    m_cmdDraw.DrawMesh(m_meshVector, Matrix4x4.identity, m_matVisualize, 0, (int)VisualizeType.Direction);
                }
            }

            // lasso lines
            if (m_meshLasso.vertexCount > 1)
                m_cmdDraw.DrawMesh(m_meshLasso, Matrix4x4.identity, m_matVisualize, 0, (int)VisualizeType.Lasso);

            Graphics.ExecuteCommandBuffer(m_cmdDraw);
        }

        void OnRepaint()
        {
            if (m_toolState == ToolState.Selection && m_settings.selectMode == SelectMode.Rect)
            {
                var selectionRect = typeof(EditorStyles).GetProperty("selectionRect", BindingFlags.NonPublic | BindingFlags.Static);
                if (selectionRect != null)
                {
                    var style = (GUIStyle)selectionRect.GetValue(null, null);
                    Handles.BeginGUI();
                    style.Draw(FromToRect(m_rectStartPoint, m_rectEndPoint), GUIContent.none, false, false, false, false);
                    Handles.EndGUI();
                }
            }
        }


        void PushUndo()
        {
            if (m_settings.normalMode == RecalculateMode.Auto)
                RecalculateNormalsInternal();

            // recalculating tangents require normals. so recalculate only when normals are updated
            if (m_settings.tangentsMode == RecalculateMode.Auto &&
                (m_settings.normalMode == RecalculateMode.Auto || m_settings.normalMode == RecalculateMode.Realtime))
                RecalculateTangentsInternal();

            Undo.IncrementCurrentGroup();
            Undo.RecordObject(this, "VertexTweaker [" + m_history.index + "]");
            m_historyIndex = ++m_history.index;

            if(m_history.points == null || m_history.points.Length != m_points.Count)
            {
                m_history.points = m_pointsPredeformed.Clone().Array;
                m_history.normals = m_normalsPredeformed.Clone().Array;
                m_history.tangents = m_tangentsPredeformed.Clone().Array;
            }
            else
            {
                Array.Copy(m_pointsPredeformed.Array, m_history.points, m_points.Count);
                Array.Copy(m_normalsPredeformed.Array, m_history.normals, m_normals.Count);
                Array.Copy(m_tangentsPredeformed.Array, m_history.tangents, m_tangents.Count);
            }
            m_history.mesh = m_meshTarget;

            Undo.FlushUndoRecordObjects();
            //Debug.Log("PushUndo " + m_historyIndex);
        }

        public void OnUndoRedo()
        {
            if (m_points == null)
                return;
            if (m_historyIndex != m_history.index)
            {
                m_historyIndex = m_history.index;

                if (m_history.mesh != m_meshTarget)
                {
                    m_meshTarget = m_history.mesh;
                    BeginEdit();
                }
                UpdateTransform();

                if (m_history.points != null && m_points != null && m_history.points.Length == m_points.Count)
                    Array.Copy(m_history.points, m_pointsPredeformed.Array, m_points.Count);
                if (m_history.normals != null && m_normals != null && m_history.normals.Length == m_normals.Count)
                    Array.Copy(m_history.normals, m_normalsPredeformed.Array, m_normals.Count);
                if (m_history.tangents != null && m_tangents != null && m_history.tangents.Length == m_tangents.Count)
                    Array.Copy(m_history.tangents, m_tangentsPredeformed.Array, m_tangents.Count);
                if (m_skinned)
                {
                    npApplySkinning(ref m_npSkinData,
                        m_pointsPredeformed, m_normalsPredeformed, m_tangentsPredeformed,
                        m_points, m_normals, m_tangents);
                }
                UpdateVertices(true);
            }
        }
#endif
    }
}
