using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace UTJ.BlendShapeBuilder
{
    public class BlendShapeBuilderWindow : EditorWindow
    {
        #region fields
        public static bool isOpen;

        BlendShapeBuilder m_target;
        GameObject m_active;

        Vector2 m_scrollPos;
        bool m_foldBlendShapes = true;
        BlendShapeFrameData m_radialCenterHandle;
        BlendShapeFrameData m_copySource;

        static readonly int indentSize = 16;
        #endregion



        #region callbacks

        [MenuItem("Window/Blend Shape Builder")]
        public static void Open()
        {
            var window = EditorWindow.GetWindow<BlendShapeBuilderWindow>();
            window.titleContent = new GUIContent("BS Builder");
            window.Show();
            window.OnSelectionChange();
        }

        private void OnEnable()
        {
            isOpen = true;

            Undo.undoRedoPerformed += OnUndoRedo;
            SceneView.onSceneGUIDelegate += OnSceneGUI;
        }

        private void OnDisable()
        {
            isOpen = false;

            Undo.undoRedoPerformed -= OnUndoRedo;
            SceneView.onSceneGUIDelegate -= OnSceneGUI;
        }

        private void OnSelectionChange()
        {
            if (m_target != null)
            {
            }

            m_target = null;
            if (Selection.activeGameObject != null)
            {
                m_target = Selection.activeGameObject.GetComponent<BlendShapeBuilder>();
                if (m_target)
                {
                }
                else
                {
                    var activeGameObject = Selection.activeGameObject;
                    if (Selection.activeGameObject.GetComponent<MeshRenderer>() != null ||
                         Selection.activeGameObject.GetComponent<SkinnedMeshRenderer>() != null)
                    {
                        m_active = activeGameObject;
                    }
                }
            }
            Repaint();
        }


        private void OnGUI()
        {

            if (m_target != null)
            {
                if (!m_target.isActiveAndEnabled)
                {
                    EditorGUILayout.LabelField("(Enable " + m_target.name + " to show Vertex Tweaker)");
                }
                else
                {
                    m_scrollPos = EditorGUILayout.BeginScrollView(m_scrollPos);
                    GUILayout.BeginVertical();
                    DrawBlendShapeBuilder();
                    GUILayout.EndVertical();
                    EditorGUILayout.EndScrollView();
                }
            }
            else if (m_active != null)
            {
                if (GUILayout.Button("Add BlendShapeBuilder to " + m_active.name))
                {
                    m_active.AddComponent<BlendShapeBuilder>();
                    OnSelectionChange();
                }
            }
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            bool repaint = false;
            if (m_radialCenterHandle != null && m_radialCenterHandle.projRayDir == ProjectionRayDirection.Radial)
            {
                EditorGUI.BeginChangeCheck();
                var move = Handles.PositionHandle(m_radialCenterHandle.projRadialCenter, Quaternion.identity);
                if (EditorGUI.EndChangeCheck())
                {
                    m_radialCenterHandle.projRadialCenter = move;
                    repaint = true;
                }
            }

            if (repaint)
                Repaint();
        }


        private void OnUndoRedo()
        {
            Repaint();
        }
        #endregion


        #region impl

        public void ModifyBlendShapeData(Action<BlendShapeBuilderData> op)
        {
            if (m_target == null) { return; }
            var m_data = m_target.data;
            if (m_data != null)
            {
                Undo.RecordObject(m_target, "BlendShapeBuilder");
                op(m_data);
            }
        }


        public void DrawBlendShapeBuilder()
        {
            if (m_target == null) { return; }
            var m_data = m_target.data;

            bool repaint = false;

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("Base Mesh", GUILayout.Width(70));
                var rect = EditorGUILayout.GetControlRect();
                var width = rect.width;
                var pos = rect.position;

                EditorGUI.BeginChangeCheck();
                var baseObject = EditorGUI.ObjectField(new Rect(pos, new Vector2(width , 16)), m_data.baseMesh, typeof(UnityEngine.Object), true);
                if (EditorGUI.EndChangeCheck())
                    m_data.baseMesh = baseObject;
            }
            if (GUILayout.Button("Find Targets", GUILayout.Width(80)))
                FindValidTargets();
            GUILayout.EndHorizontal();


            m_foldBlendShapes = EditorGUILayout.Foldout(m_foldBlendShapes, "BlendShapes");
            if (m_foldBlendShapes)
            {
                var bsData = m_data.blendShapeData;
                var evt = Event.current;

                // handle drag & drop
                if (evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform)
                {
                    var dropArea = GUILayoutUtility.GetLastRect();
                    if (dropArea.Contains(evt.mousePosition))
                    {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                        if (evt.type == EventType.DragPerform)
                        {
                            DragAndDrop.AcceptDrag();
                            Undo.RecordObject(m_target, "BlendShapeBuilder");
                            foreach (var obj in DragAndDrop.objectReferences)
                            {
                                var mesh = Utils.ExtractMesh(obj);
                                if (mesh != null)
                                {
                                    var bsd = new BlendShapeData();
                                    bsd.name = mesh.name;
                                    bsd.frames.Add(new BlendShapeFrameData { mesh = obj });
                                    m_data.blendShapeData.Add(bsd);
                                }
                            }
                        }
                        evt.Use();
                    }
                }

                BlendShapeData delBS = null;

                foreach (var data in bsData)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(indentSize);
                    GUILayout.BeginVertical("Box");
                    GUILayout.BeginHorizontal();
                    data.fold = EditorGUILayout.Foldout(data.fold, data.name);
                    if (GUILayout.Button("-", GUILayout.Width(20)))
                    {
                        Undo.RecordObject(m_target, "BlendShapeBuilder");
                        delBS = data;
                    }

                    GUILayout.EndHorizontal();
                    if (data.fold)
                    {
                        // handle drag & drop
                        if (evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform)
                        {
                            var dropArea = GUILayoutUtility.GetLastRect();
                            if (dropArea.Contains(evt.mousePosition))
                            {
                                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                                if (evt.type == EventType.DragPerform)
                                {
                                    DragAndDrop.AcceptDrag();
                                    Undo.RecordObject(m_target, "BlendShapeBuilder");
                                    data.ClearInvalidFrames();
                                    foreach (var obj in DragAndDrop.objectReferences)
                                    {
                                        var mesh = Utils.ExtractMesh(obj);
                                        if(mesh != null)
                                            data.frames.Add(new BlendShapeFrameData { mesh = obj });
                                    }
                                    data.NormalizeWeights();
                                }
                                evt.Use();
                            }
                        }

                        GUILayout.BeginHorizontal();
                        GUILayout.Space(indentSize);
                        GUILayout.BeginVertical();

                        {

                            var rect = EditorGUILayout.GetControlRect();
                            var width = rect.width;
                            var pos = rect.position;

                            EditorGUI.BeginChangeCheck();
                            EditorGUI.LabelField(new Rect(pos, new Vector2(width, 16)), "Name");
                            pos.x += 50; width -= 50;
                            var name = EditorGUI.TextField(new Rect(pos, new Vector2(width, 16)), data.name);
                            if (EditorGUI.EndChangeCheck())
                            {
                                Undo.RecordObject(m_target, "BlendShapeBuilder");
                                data.name = name;
                            }
                        }

                        EditorGUILayout.LabelField(new GUIContent("Frames", "Weight Mesh    Vertex Normal Tangent"));

                        GUILayout.BeginHorizontal();
                        GUILayout.Space(indentSize);
                        GUILayout.BeginVertical();

                        BlendShapeFrameData delFrame = null;
                        int numFrames = data.frames.Count;
                        int numV = 0, numN = 0, numT = 0;
                        foreach (var frame in data.frames)
                        {
                            if (frame.vertex) ++numV;
                            if (frame.normal) ++numN;
                            if (frame.tangent) ++numT;

                            GUILayout.BeginHorizontal();

                            var rect = EditorGUILayout.GetControlRect();
                            var width = rect.width;
                            var pos = rect.position;

                            EditorGUI.BeginChangeCheck();
                            var w = EditorGUI.FloatField(new Rect(pos, new Vector2(36, 16)), frame.weight);
                            if (EditorGUI.EndChangeCheck())
                            {
                                Undo.RecordObject(m_target, "BlendShapeBuilder");
                                frame.weight = w;
                            }

                            pos.x += 40;

                            EditorGUI.BeginChangeCheck();
                            var m = EditorGUI.ObjectField(new Rect(pos, new Vector2(width - 40, 16)), frame.mesh, typeof(UnityEngine.Object), true);
                            if (EditorGUI.EndChangeCheck())
                            {
                                Undo.RecordObject(m_target, "BlendShapeBuilder");
                                frame.mesh = m;
                            }

                            EditorGUI.BeginChangeCheck();
                            var p = GUILayout.Toggle(frame.proj, "Projection", GUILayout.Width(75));
                            if (EditorGUI.EndChangeCheck())
                            {
                                Undo.RecordObject(m_target, "BlendShapeBuilder");
                                frame.proj = p;
                            }


                            EditorGUI.BeginChangeCheck();
                            var v = GUILayout.Toggle(frame.vertex, "V", GUILayout.Width(25));
                            if (EditorGUI.EndChangeCheck())
                            {
                                Undo.RecordObject(m_target, "BlendShapeBuilder");
                                frame.vertex = v;
                            }

                            EditorGUI.BeginChangeCheck();
                            var n = GUILayout.Toggle(frame.normal, "N", GUILayout.Width(25));
                            if (EditorGUI.EndChangeCheck())
                            {
                                Undo.RecordObject(m_target, "BlendShapeBuilder");
                                frame.normal = n;
                            }

                            EditorGUI.BeginChangeCheck();
                            var t = GUILayout.Toggle(frame.tangent, "T", GUILayout.Width(25));
                            if (EditorGUI.EndChangeCheck())
                            {
                                Undo.RecordObject(m_target, "BlendShapeBuilder");
                                frame.tangent = t;
                            }

                            if (GUILayout.Button("-", GUILayout.Width(20)))
                                delFrame = frame;

                            GUILayout.EndHorizontal();

                            if (frame.proj)
                            {
                                GUILayout.BeginHorizontal();
                                GUILayout.Space(indentSize);
                                GUILayout.BeginVertical("Box");
                                frame.projMode = (npProjectVerticesMode)EditorGUILayout.EnumPopup("Projection Mode", frame.projMode);
                                frame.projRayDir = (ProjectionRayDirection)EditorGUILayout.EnumPopup("Ray Direction", frame.projRayDir);
                                if (frame.projRayDir == ProjectionRayDirection.Radial)
                                {
                                    GUILayout.BeginHorizontal();
                                    GUILayout.Space(indentSize);
                                    GUILayout.BeginVertical();

                                    EditorGUI.BeginChangeCheck();
                                    frame.projRadialCenter = EditorGUILayout.Vector3Field("Radial Center", frame.projRadialCenter);
                                    if (EditorGUI.EndChangeCheck())
                                        repaint = true;

                                    EditorGUI.BeginChangeCheck();
                                    bool h = GUILayout.Toggle(m_radialCenterHandle == frame, "Show Handle", "Button", GUILayout.Width(120));
                                    if(EditorGUI.EndChangeCheck())
                                    {
                                        repaint = true;
                                        if (h)
                                        {
                                            m_radialCenterHandle = frame;
                                            Tools.current = Tool.None;
                                        }
                                        else if (m_radialCenterHandle == frame)
                                            m_radialCenterHandle = null;
                                    }
                                    GUILayout.EndVertical();
                                    GUILayout.EndHorizontal();
                                }
                                frame.projMaxRayDistance = EditorGUILayout.FloatField("Max Ray Distance", frame.projMaxRayDistance);

                                GUILayout.BeginHorizontal();
                                if (GUILayout.Button("Copy", GUILayout.Width(60)))
                                {
                                    m_copySource = frame;
                                }
                                if (GUILayout.Button("Paste", GUILayout.Width(60)) && m_copySource != null)
                                {
                                    frame.projMode = m_copySource.projMode;
                                    frame.projRayDir = m_copySource.projRayDir;
                                    frame.projRadialCenter = m_copySource.projRadialCenter;
                                    frame.projMaxRayDistance = m_copySource.projMaxRayDistance;
                                }
                                GUILayout.Space(10);
                                if (GUILayout.Button("Generate Mesh", GUILayout.Width(120)))
                                {
                                    var pmesh = GenerateProjectedMesh(m_data.baseMesh, frame);
                                    var go = Utils.MeshToGameObject(pmesh, Vector3.zero, Utils.ExtractMaterials(m_data.baseMesh));
                                    Selection.activeGameObject = go;
                                }
                                GUILayout.EndHorizontal();

                                GUILayout.EndVertical();
                                GUILayout.EndHorizontal();
                            }
                        }
                        if(delFrame != null)
                        {
                            Undo.RecordObject(m_target, "BlendShapeBuilder");
                            data.frames.Remove(delFrame);
                            data.NormalizeWeights();
                        }

                        GUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();
                        if (numFrames > 0)
                        {
                            bool v = false;
                            EditorGUI.BeginChangeCheck();
                            if (numV == numFrames || numV == 0)
                                v = GUILayout.Toggle(numV == numFrames, "V", GUILayout.Width(25));
                            else
                                v = GUILayout.Toggle(numV == numFrames, "V", "ToggleMixed", GUILayout.Width(25));
                            if (EditorGUI.EndChangeCheck())
                            {
                                Undo.RecordObject(m_target, "BlendShapeBuilder");
                                foreach (var frame in data.frames)
                                    frame.vertex = v;
                            }

                            EditorGUI.BeginChangeCheck();
                            if (numN == numFrames || numN == 0)
                                v = GUILayout.Toggle(numN == numFrames, "N", GUILayout.Width(25));
                            else
                                v = GUILayout.Toggle(numN == numFrames, "N", "ToggleMixed", GUILayout.Width(25));
                            if (EditorGUI.EndChangeCheck())
                            {
                                Undo.RecordObject(m_target, "BlendShapeBuilder");
                                foreach (var frame in data.frames)
                                    frame.normal = v;
                            }

                            EditorGUI.BeginChangeCheck();
                            if (numT == numFrames || numT == 0)
                                v = GUILayout.Toggle(numT == numFrames, "T", GUILayout.Width(25));
                            else
                                v = GUILayout.Toggle(numT == numFrames, "T", "ToggleMixed", GUILayout.Width(25));
                            if (EditorGUI.EndChangeCheck())
                            {
                                Undo.RecordObject(m_target, "BlendShapeBuilder");
                                foreach (var frame in data.frames)
                                    frame.tangent = v;
                            }
                        }
                        if (GUILayout.Button("+", GUILayout.Width(20)))
                        {
                            Undo.RecordObject(m_target, "BlendShapeBuilder");
                            data.frames.Add(new BlendShapeFrameData());
                            data.NormalizeWeights();
                        }
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        if (GUILayout.Button("Normalize Weights", GUILayout.Width(120)))
                        {
                            Undo.RecordObject(m_target, "BlendShapeBuilder");
                            data.NormalizeWeights();
                        }
                        if (GUILayout.Button("Sort By Weights", GUILayout.Width(110)))
                        {
                            Undo.RecordObject(m_target, "BlendShapeBuilder");
                            data.SortByWeights();
                        }
                        if (GUILayout.Button("Clear", GUILayout.Width(60)))
                        {
                            Undo.RecordObject(m_target, "BlendShapeBuilder");
                            data.frames.Clear();
                        }
                        GUILayout.EndHorizontal();

                        GUILayout.EndVertical();
                        GUILayout.EndHorizontal();

                        GUILayout.EndVertical();
                        GUILayout.Space(10);
                        GUILayout.EndHorizontal();
                    }

                    GUILayout.EndVertical();
                    GUILayout.EndHorizontal();
                }

                if (delBS != null)
                    bsData.Remove(delBS);

                GUILayout.BeginHorizontal();
                GUILayout.Space(indentSize);
                GUILayout.BeginHorizontal("Box");
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("+", GUILayout.Width(20)))
                {
                    Undo.RecordObject(m_target, "BlendShapeBuilder");
                    var tmp = new BlendShapeData();
                    tmp.name = "NewBlendShape" + bsData.Count;
                    tmp.frames.Add(new BlendShapeFrameData());
                    bsData.Add(tmp);
                }
                GUILayout.EndHorizontal();
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(8);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Update Mesh", GUILayout.Width(100)))
            {
                Build(true);
            }
            {
                EditorGUI.BeginChangeCheck();
                var v = GUILayout.Toggle(m_data.preserveExistingBlendShapes, "Preserve Existing BlendShapes");
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(m_target, "BlendShapeBuilder");
                    m_data.preserveExistingBlendShapes = v;
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(6);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Generate New Mesh", GUILayout.Width(130)))
            {
                var result = Build();
                if (result != null)
                {
                    var go = Utils.MeshToGameObject(result, Vector3.zero, Utils.ExtractMaterials(m_data.baseMesh));
                    Selection.activeGameObject = go;
                }
            }
            if (GUILayout.Button("Export BaseMesh To .asset", GUILayout.Width(170)))
            {
                var baseMesh = Utils.ExtractMesh(m_data.baseMesh);
                string path = EditorUtility.SaveFilePanel("Export .asset file", "Assets", baseMesh.name, "asset");
                if (path.Length > 0)
                {
                    var dataPath = Application.dataPath;
                    if (!path.StartsWith(dataPath))
                    {
                        Debug.LogError("Invalid path: Path must be under " + dataPath);
                    }
                    else
                    {
                        path = path.Replace(dataPath, "Assets");
                        AssetDatabase.CreateAsset(Instantiate(baseMesh), path);
                        Debug.Log("Asset exported: " + path);
                    }
                }
            }
            GUILayout.EndHorizontal();

            if (repaint)
                RepaintAllViews();
        }

        public void FindValidTargets()
        {
            var m_data = m_target.data;
            if (m_data.baseMesh == null)
            {
                Debug.LogWarning("Base mesh is not set");
                return;
            }

            var baseMesh = Utils.ExtractMesh(m_data.baseMesh);
            if (baseMesh == null)
            {
                Debug.LogWarning("Base mesh has no valid mesh");
                return;
            }

            var vCount = baseMesh.vertexCount;
            var set = new HashSet<UnityEngine.Object>();
            foreach (var cmp in FindObjectsOfType<SkinnedMeshRenderer>())
            {
                var mesh = cmp.sharedMesh;
                if (mesh != null && mesh.vertexCount == vCount)
                    set.Add(cmp.gameObject);
            }
            foreach (var cmp in FindObjectsOfType<MeshFilter>())
            {
                var mesh = cmp.sharedMesh;
                if (mesh != null && mesh.vertexCount == vCount)
                    set.Add(cmp.gameObject);
            }
            set.Remove(m_data.baseMesh);

            if (set.Count == 0)
            {
                Debug.Log("No valid targets in this scene");
            }
            else
            {
                var sel = new List<UnityEngine.Object>(set);
                sel.Sort((a, b) => { return a.name.CompareTo(b.name); });
                Selection.objects = sel.ToArray();
                Debug.Log(sel.Count + " targets found");
            }
        }


        public Mesh Build(bool updateExistingMesh = false)
        {
            var m_data = m_target.data;
            if (m_data.baseMesh == null)
            {
                Debug.LogError("Base mesh is not set");
                return null;
            }

            var baseMesh = Utils.ExtractMesh(m_data.baseMesh);
            if(baseMesh == null)
            {
                Debug.LogError("Base mesh has no valid mesh");
                return null;
            }

            Mesh ret = null;
            if (updateExistingMesh)
            {
                ret = baseMesh;
                EditorUtility.SetDirty(m_data.baseMesh);
            }
            else
            {
                ret = Instantiate(baseMesh);
                ret.name = baseMesh.name;
            }

            var baseVertices = baseMesh.vertices;
            var baseNormals = baseMesh.normals;
            var baseTangents = baseMesh.tangents;

            // generate delta. * this must be before delete existing blend shapes *
            foreach (var shape in m_data.blendShapeData)
            {
                var name = shape.name;

                foreach(var frame in shape.frames)
                {
                    var mesh = frame.proj ? GenerateProjectedMesh(m_data.baseMesh, frame) : Utils.ExtractMesh(frame.mesh);
                    if(mesh == null)
                    {
                        Debug.LogError("Invalid target in " + name + " at weight " + frame.weight);
                    }
                    else if (mesh.vertexCount != baseMesh.vertexCount)
                    {
                        Debug.LogError("Invalid target (vertex count doesn't match) in " + name + " at weight " + frame.weight);
                    }
                    else
                    {
                        frame.AllocateDelta(baseMesh.vertexCount);
                        if (frame.vertex)
                            GenerateDelta(baseVertices, mesh.vertices, frame.deltaVertices);
                        if (frame.normal)
                            GenerateDelta(baseNormals, mesh.normals, frame.deltaNormals);
                        if (frame.tangent)
                            GenerateDelta(baseTangents, mesh.tangents, frame.deltaTangents);
                    }
                }
            }

            // delete or copy existing blend shapes
            if (m_data.preserveExistingBlendShapes)
            {
                var del = new List<string>();
                int numBS = baseMesh.blendShapeCount;
                for (int si = 0; si < numBS; ++si)
                {
                    var name = baseMesh.GetBlendShapeName(si);
                    foreach (var shape in m_data.blendShapeData)
                    {
                        if (shape.name == name)
                        {
                            del.Add(name);
                            break;
                        }
                    }
                }

                if (del.Count > 0)
                {
                    var deltaVertices = new Vector3[baseMesh.vertexCount];
                    var deltaNormals = new Vector3[baseMesh.vertexCount];
                    var deltaTangents = new Vector3[baseMesh.vertexCount];

                    var src = Instantiate(ret);
                    ret.ClearBlendShapes();
                    for (int si = 0; si < numBS; ++si)
                    {
                        var name = src.GetBlendShapeName(si);
                        if (!del.Contains(name))
                        {
                            int numFrames = src.GetBlendShapeFrameCount(si);
                            for (int fi = 0; fi < numFrames; ++fi)
                            {
                                float weight = src.GetBlendShapeFrameWeight(si, fi);
                                src.GetBlendShapeFrameVertices(si, fi, deltaVertices, deltaNormals, deltaTangents);
                                ret.AddBlendShapeFrame(name, weight, deltaVertices, deltaNormals, deltaTangents);
                            }
                        }
                    }
                    DestroyImmediate(src);
                }
            }
            else
            {
                ret.ClearBlendShapes();
            }

            // add new blend shapes
            int numAdded = 0;
            foreach (var shape in m_data.blendShapeData)
            {
                var name = shape.name;
                foreach (var frame in shape.frames)
                {
                    if (frame.deltaVertices != null)
                    {
                        ret.AddBlendShapeFrame(name, frame.weight, frame.deltaVertices, frame.deltaNormals, frame.deltaTangents);
                        frame.ReleaseDelta();
                        ++numAdded;
                    }
                }
            }

            Debug.Log("Done: added " + numAdded + " frames");
            return ret;
        }

        void RepaintAllViews()
        {
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }

        private static void ZeroClear(Vector3[] dst)
        {
            int len = dst.Length;
            var zero = Vector3.zero;
            for (int i = 0; i < len; ++i)
                dst[i] = zero;
        }

        private static void GenerateDelta(Vector3[] from, Vector3[] to, Vector3[] dst)
        {
            var len = from.Length;
            for (int i = 0; i < len; ++i)
                dst[i] = to[i] - from[i];
        }

        private static void GenerateDelta(Vector4[] from, Vector4[] to, Vector3[] dst)
        {
            var len = from.Length;
            for (int i = 0; i < len; ++i)
                dst[i] = to[i] - from[i];
        }

        public static Mesh GenerateProjectedMesh(UnityEngine.Object baseMeshObj, BlendShapeFrameData frame)
        {
            var baseData = new MeshData();
            var targetData = new MeshData();
            if (frame == null || !baseData.Extract(baseMeshObj) || !targetData.Extract(frame.mesh)) { return null; }

            var baseNP = (npMeshData)baseData;
            var targetNP = (npMeshData)targetData;
            if (frame.projRayDir == ProjectionRayDirection.CurrentNormals)
            {
                npProjectVertices(ref baseNP, ref targetNP, baseData.normals, frame.projMode, frame.projMaxRayDistance);
            }
            else if (frame.projRayDir == ProjectionRayDirection.BaseNomals)
            {
                var rayDirs = new PinnedList<Vector3>();
                rayDirs.Resize(baseData.vertexCount);
                npGenerateNormals(ref baseNP, rayDirs);
                npProjectVertices(ref baseNP, ref targetNP, rayDirs, frame.projMode, frame.projMaxRayDistance);
            }
            else if (frame.projRayDir == ProjectionRayDirection.Radial)
            {
                npProjectVerticesRadial(ref baseNP, ref targetNP, frame.projRadialCenter, frame.projMode, frame.projMaxRayDistance);
            }

            Mesh ret;
            var baseMesh = Utils.ExtractMesh(baseMeshObj);
            if(baseMesh)
            {
                ret = Instantiate(baseMesh);
                ret.ClearBlendShapes();
                if (frame.vertex) { ret.SetVertices(baseData.vertices); }
                if (frame.normal) { ret.SetNormals(baseData.normals); }
                if (frame.tangent) { ret.SetTangents(baseData.tangents); }
            }
            else
            {
                // todo
                ret = new Mesh();
                ret.SetVertices(baseData.vertices);
                ret.SetNormals(baseData.normals);
                ret.SetTangents(baseData.tangents);
            }
            return ret;
        }
        [DllImport("BlendShapeBuilderCore")] static extern int npGenerateNormals(
            ref npMeshData model, IntPtr dst);
        [DllImport("BlendShapeBuilderCore")] static extern void npProjectVertices(
            ref npMeshData model, ref npMeshData target, IntPtr ray_dir, npProjectVerticesMode mode, float max_distance);
        [DllImport("BlendShapeBuilderCore")] static extern void npProjectVerticesRadial(
            ref npMeshData model, ref npMeshData target, Vector3 center, npProjectVerticesMode mode, float max_distance);

        #endregion

    }
}
