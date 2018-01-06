using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif


namespace UTJ.BlendShapeBuilder
{
    [ExecuteInEditMode]
    public partial class VertexTweaker : MonoBehaviour
    {
        
        [Serializable]
        public class History
        {
            public int index;
            public Mesh mesh;
            public Vector3[] vertices;
            public Vector3[] normals;
            public Vector4[] tangents;
        }


        VertexTweakerSettings m_settings;

        // internal resources
        [SerializeField] Mesh m_meshTarget;
        [SerializeField] Mesh m_meshCube;
        [SerializeField] Mesh m_meshLine;
        [SerializeField] Mesh m_meshLasso;
        [SerializeField] Material m_matVisualize;

        ComputeBuffer m_cbArg;
        ComputeBuffer m_cbPoints;
        ComputeBuffer m_cbNormals;
        ComputeBuffer m_cbTangents;
        ComputeBuffer m_cbSelection;
        ComputeBuffer m_cbBaseNormals;
        ComputeBuffer m_cbBaseTangents;
        ComputeBuffer m_cbBrushSamples;
        CommandBuffer m_cmdDraw;

        bool m_skinned;
        PinnedList<Vector3> m_points, m_pointsPredeformed;
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
        int m_rayHitTriangle;
        Vector3 m_rayPos;
        Vector3 m_selectionPos;
        Vector3 m_selectionNormal;
        Quaternion m_selectionRot;
        bool m_rectDragging;
        Vector2 m_rectStartPoint;
        Vector2 m_rectEndPoint;
        List<Vector2> m_lassoPoints = new List<Vector2>();
        int m_brushNumPainted = 0;

        [SerializeField] History m_history = new History();
        int m_historyIndex = 0;

        npMeshData m_npModelData = new npMeshData();
        npSkinData m_npSkinData = new npSkinData();


        public float[] selection
        {
            get { return (float[])m_selection.Array.Clone(); }
            set
            {
                if (value != null && value.Length == m_selection.Count)
                {
                    Array.Copy(value, m_selection, m_selection.Count);
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
                var ds = AssetDatabase.LoadAssetAtPath<VertexTweakerSettings>(AssetDatabase.GUIDToAssetPath("f9fa1a75054c38b439daaed96bc5b424"));
                if (ds != null)
                {
                    m_settings = Instantiate(ds);
                }
                if (m_settings == null)
                {
                    m_settings = ScriptableObject.CreateInstance<VertexTweakerSettings>();
                }
            }

            if (m_meshCube == null)
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

                m_meshCube = new Mesh();
                m_meshCube.vertices = new Vector3[] {
                    p[0], p[1], p[2], p[3],
                    p[7], p[4], p[0], p[3],
                    p[4], p[5], p[1], p[0],
                    p[6], p[7], p[3], p[2],
                    p[5], p[6], p[2], p[1],
                    p[7], p[6], p[5], p[4],
                };
                m_meshCube.SetIndices(new int[] {
                    3, 1, 0, 3, 2, 1,
                    7, 5, 4, 7, 6, 5,
                    11, 9, 8, 11, 10, 9,
                    15, 13, 12, 15, 14, 13,
                    19, 17, 16, 19, 18, 17,
                    23, 21, 20, 23, 22, 21,
                }, MeshTopology.Triangles, 0);
            }

            if (m_meshLine == null)
            {
                m_meshLine = new Mesh();
                m_meshLine.vertices = new Vector3[2] { Vector3.zero, Vector3.zero };
                m_meshLine.uv = new Vector2[2] { Vector2.zero, Vector2.one };
                m_meshLine.SetIndices(new int[2] { 0, 1 }, MeshTopology.Lines, 0);
            }

            if (m_meshLasso == null)
            {
                m_meshLasso = new Mesh();
            }

            if (m_matVisualize == null)
                m_matVisualize = new Material(AssetDatabase.LoadAssetAtPath<Shader>(AssetDatabase.GUIDToAssetPath("03871fa9be0375f4c91cb4842f15b890")));

            if (m_meshTarget == null ||
                m_meshTarget != tmesh ||
                (m_points != null && m_meshTarget.vertexCount != m_points.Count))
            {
                m_meshTarget = tmesh;
                m_points = null;
                m_normals = null;
                m_normalsBase = null;
                m_tangents = null;
                m_indices = null;
                m_mirrorRelation = null;
                m_selection = null;

                ReleaseComputeBuffers();
            }

            if (m_meshTarget != null)
            {
                m_points = new PinnedList<Vector3>(m_meshTarget.vertices);
                m_pointsPredeformed = m_points;

                m_uv = new PinnedList<Vector2>(m_meshTarget.uv);

                m_normals = new PinnedList<Vector3>(m_meshTarget.normals);
                if (m_normals.Count == 0)
                {
                    m_meshTarget.RecalculateNormals();
                    m_normalsBase = m_normals = new PinnedList<Vector3>(m_meshTarget.normals);
                }
                else
                {
                    m_meshTarget.RecalculateNormals();
                    m_normalsBase = new PinnedList<Vector3>(m_meshTarget.normals);
                    m_meshTarget.normals = m_normals;
                }
                m_normalsPredeformed = m_normals;
                m_normalsBasePredeformed = m_normalsBase;

                m_tangents = new PinnedList<Vector4>(m_meshTarget.tangents);
                if (m_tangents.Count == 0)
                {
                    m_meshTarget.RecalculateTangents();
                    m_tangentsBase = m_tangents = new PinnedList<Vector4>(m_meshTarget.tangents);
                }
                else
                {
                    m_meshTarget.RecalculateTangents();
                    m_tangentsBase = new PinnedList<Vector4>(m_meshTarget.tangents);
                    m_meshTarget.tangents = m_tangents;
                }
                m_tangentsPredeformed = m_tangents;
                m_tangentsBasePredeformed = m_tangentsBase;

                m_indices = new PinnedList<int>(m_meshTarget.triangles);
                m_selection = new PinnedList<float>(m_points.Count);

                m_npModelData.num_vertices = m_points.Count;
                m_npModelData.num_triangles = m_indices.Count / 3;
                m_npModelData.indices = m_indices;
                m_npModelData.vertices = m_points;
                m_npModelData.normals = m_normals;
                m_npModelData.tangents = m_tangents;
                m_npModelData.uv = m_uv;
                m_npModelData.selection = m_selection;

                var smr = GetComponent<SkinnedMeshRenderer>();
                if (smr != null)
                {
                    m_skinned = true;

                    m_boneWeights = new PinnedList<BoneWeight>(m_meshTarget.boneWeights);
                    m_bindposes = new PinnedList<Matrix4x4>(m_meshTarget.bindposes);
                    m_boneMatrices = new PinnedList<Matrix4x4>(m_bindposes.Count);

                    m_pointsPredeformed = m_points.Clone();
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

            if (m_cbPoints == null && m_points != null && m_points.Count > 0)
            {
                m_cbPoints = new ComputeBuffer(m_points.Count, 12);
                m_cbPoints.SetData(m_points);
            }
            if (m_cbNormals == null && m_normals != null && m_normals.Count > 0)
            {
                m_cbNormals = new ComputeBuffer(m_normals.Count, 12);
                m_cbNormals.SetData(m_normals);
                m_cbBaseNormals = new ComputeBuffer(m_normalsBase.Count, 12);
                m_cbBaseNormals.SetData(m_normalsBase);
            }
            if (m_cbTangents == null && m_tangents != null && m_tangents.Count > 0)
            {
                m_cbTangents = new ComputeBuffer(m_tangents.Count, 16);
                m_cbTangents.SetData(m_tangents);
                m_cbBaseTangents = new ComputeBuffer(m_tangentsBase.Count, 16);
                m_cbBaseTangents.SetData(m_tangentsBase);
            }
            if (m_cbSelection == null && m_selection != null && m_selection.Count > 0)
            {
                m_cbSelection = new ComputeBuffer(m_selection.Count, 4);
                m_cbSelection.SetData(m_selection);
            }

            if (m_cbArg == null && m_points != null && m_points.Count > 0)
            {
                m_cbArg = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
                m_cbArg.SetData(new uint[5] { m_meshCube.GetIndexCount(0), (uint)m_points.Count, 0, 0, 0 });
            }

            m_settings.InitializeBrushData();

            UpdateTransform();
            UpdateNormals();
            PushUndo();
            m_editing = true;
        }

        void EndEdit()
        {
            ReleaseComputeBuffers();
            if (m_settings) m_settings.projectionNormalSource = null;

            m_editing = false;
        }

        void ReleaseComputeBuffers()
        {
            if (m_cbArg != null) { m_cbArg.Release(); m_cbArg = null; }
            if (m_cbPoints != null) { m_cbPoints.Release(); m_cbPoints = null; }
            if (m_cbNormals != null) { m_cbNormals.Release(); m_cbNormals = null; }
            if (m_cbTangents != null) { m_cbTangents.Release(); m_cbTangents = null; }
            if (m_cbSelection != null) { m_cbSelection.Release(); m_cbSelection = null; }
            if (m_cbBaseNormals != null) { m_cbBaseNormals.Release(); m_cbBaseNormals = null; }
            if (m_cbBaseTangents != null) { m_cbBaseTangents.Release(); m_cbBaseTangents = null; }
            if (m_cbBrushSamples != null) { m_cbBrushSamples.Release(); m_cbBrushSamples = null; }
            if (m_cmdDraw != null) { m_cmdDraw.Release(); m_cmdDraw = null; }
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


        void PushUndo()
        {
            PushUndo(m_normals);
        }

        void PushUndo(Vector3[] normals)
        {
            Undo.IncrementCurrentGroup();
            Undo.RecordObject(this, "NormalEditor [" + m_history.index + "]");
            m_historyIndex = ++m_history.index;

            if (normals == null)
            {
                m_history.normals = null;
            }
            else
            {
                if (m_history.normals != null && m_history.normals.Length == normals.Length)
                    Array.Copy(normals, m_history.normals, normals.Length);
                else
                    m_history.normals = (Vector3[])normals.Clone();

                if (m_settings.tangentsMode == TangentsUpdateMode.Auto)
                    RecalculateTangents();
            }
            m_history.mesh = m_meshTarget;

            Undo.FlushUndoRecordObjects();
        }

        public void OnUndoRedo()
        {
            if (m_historyIndex != m_history.index)
            {
                m_historyIndex = m_history.index;

                if (m_history.mesh != m_meshTarget)
                {
                    m_meshTarget = m_history.mesh;
                    BeginEdit();
                }
                UpdateTransform();
                if (m_history.normals != null && m_normals != null && m_history.normals.Length == m_normals.Count)
                {
                    Array.Copy(m_history.normals, m_normals, m_normals.Count);
                    UpdateNormals(false);

                    if (m_settings.tangentsMode == TangentsUpdateMode.Auto)
                        RecalculateTangents();
                }
            }
        }
    }

}
