using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using UTJ.VertexTweaker;
using UTJ.BlendShapeBuilder;

namespace UTJ.BlendShapeBuilderEditor
{
    public class BlendShapeBuilderWindow : EditorWindow
    {
        #region fields
        public static bool isOpen;

        UTJ.BlendShapeBuilder.BlendShapeBuilder m_target;
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
        }

        private void OnDisable()
        {
            isOpen = false;

            Undo.undoRedoPerformed -= OnUndoRedo;
        }

        private void OnSelectionChange()
        {

            m_target = null;
            if (Selection.activeGameObject != null)
            {
                m_target = Selection.activeGameObject.GetComponent<UTJ.BlendShapeBuilder.BlendShapeBuilder>();
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

        private void OnFocus()
        {
            if (m_target == null)
            {
                OnSelectionChange();
            }
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
                    Undo.AddComponent<UTJ.BlendShapeBuilder.BlendShapeBuilder>(m_active);
                    {
                        var mesh = Utils.GetMesh(m_active);
                        if (mesh != null && ((int)mesh.hideFlags & (int)HideFlags.NotEditable) != 0)
                        {
                            var meshClone = Instantiate(mesh);
                            Undo.RegisterCreatedObjectUndo(meshClone, "BlendShapeBuilder");
                            Utils.SetMesh(m_active, meshClone);
                            EditorUtility.SetDirty(m_active);
                            Debug.Log("Mesh \"" + mesh.name + "\" is not editable. A clone is assigned to " + m_active.name);
                        }
                    }
                    OnSelectionChange();
                }
            }
        }


        private void OnUndoRedo()
        {
            Repaint();
        }
        #endregion


        #region impl
        bool IsValidTarget(UnityEngine.Object obj)
        {
            if (obj == null)
                return false;

            var mesh = Utils.GetMesh(obj);
            if (mesh == null)
            {
                Debug.LogWarning(obj.name + " has no mesh");
                return false;
            }
            else if (Utils.GetMesh(m_target.gameObject).vertexCount != mesh.vertexCount)
            {
                Debug.LogWarning(obj.name + ": vertex count doesn't match");
                return false;
            }
            return true;
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

                EditorGUI.ObjectField(new Rect(pos, new Vector2(width , 16)), m_data.baseMesh, typeof(UnityEngine.Object), true);
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
                                if (IsValidTarget(obj))
                                {
                                    var bsd = new BlendShapeData();
                                    bsd.name = obj.name;
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
                                        if (IsValidTarget(obj))
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
                                if (IsValidTarget(m))
                                {
                                    Undo.RecordObject(m_target, "BlendShapeBuilder");
                                    frame.mesh = m;
                                }
                            }

                            if (GUILayout.Button("Edit", GUILayout.Width(50)))
                                OnEditFrame(frame);

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
                            OnAddFrame(data);
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
                    bsData.Add(tmp);
                }
                GUILayout.EndHorizontal();
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(8);
            m_data.preserveExistingBlendShapes = GUILayout.Toggle(m_data.preserveExistingBlendShapes, "Preserve Existing BlendShapes");

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Update Mesh", GUILayout.Width(150)))
            {
                BuildMesh(true);
            }
            if (GUILayout.Button("Generate New Asset", GUILayout.Width(150)))
            {
                var result = BuildMesh();
                string path = EditorUtility.SaveFilePanel("Generate New Asset", "Assets", Utils.SanitizeFileName(m_data.baseMesh.name), "asset");
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
                        AssetDatabase.CreateAsset(result, path);
                        Debug.Log("Asset exported: " + path);
                    }
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(6);

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

            var baseMesh = Utils.GetMesh(m_data.baseMesh);
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


        public Mesh BuildMesh(bool updateExistingMesh = false)
        {
            var m_data = m_target.data;
            if (m_data.baseMesh == null)
            {
                Debug.LogError("Base mesh is not set");
                return null;
            }

            var baseMesh = Utils.GetMesh(m_data.baseMesh);
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

            var deltaVertices = new Vector3[baseMesh.vertexCount];
            var deltaNormals = new Vector3[baseMesh.vertexCount];
            var deltaTangents = new Vector3[baseMesh.vertexCount];

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

            // generate delta. * this must be before delete existing blend shapes *
            foreach (var shape in m_data.blendShapeData)
            {
                var name = shape.name;

                foreach (var frame in shape.frames)
                {
                    var mesh = Utils.GetMesh(frame.mesh);
                    if (mesh == null)
                    {
                        Debug.LogError("Invalid target in " + name + " at weight " + frame.weight);
                    }
                    else if (mesh.vertexCount != baseMesh.vertexCount)
                    {
                        Debug.LogError("Invalid target (vertex count doesn't match) in " + name + " at weight " + frame.weight);
                    }
                    else
                    {
                        if (frame.vertex)
                            GenerateDelta(baseVertices, mesh.vertices, deltaVertices);
                        else
                            ZeroClear(deltaVertices);

                        if (frame.normal)
                            GenerateDelta(baseNormals, mesh.normals, deltaNormals);
                        else
                            ZeroClear(deltaNormals);

                        if (frame.tangent)
                            GenerateDelta(baseTangents, mesh.tangents, deltaTangents);
                        else
                            ZeroClear(deltaTangents);

                        ret.AddBlendShapeFrame(name, frame.weight, deltaVertices, deltaNormals, deltaTangents);
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

        void OnEditFrame(BlendShapeFrameData frame)
        {
            if(frame.mesh == null)
            {
                Debug.LogWarning("Target object is null");
                return;
            }

            var mesh = Utils.GetMesh(frame.mesh);
            if (mesh == null)
            {
                Debug.LogWarning("Target object has no mesh");
                return;
            }

            var go = frame.mesh as GameObject;
            if (go == null)
            {
                go = Utils.MeshToGameObject(mesh, Vector3.zero, Utils.GetMaterials(m_target));
                Undo.RegisterCreatedObjectUndo(go, "BlendShapeBuilder");
            }

            var vt = go.GetComponent<UTJ.VertexTweaker.VertexTweaker>();
            if (vt == null)
                vt = Undo.AddComponent<UTJ.VertexTweaker.VertexTweaker>(go);

            Selection.activeObject = go;
            UTJ.VertexTweakerEditor.VertexTweakerWindow.Open();
            vt.editing = true;
        }

        void OnAddFrame(BlendShapeData bsd)
        {
            Undo.RecordObject(this, "BlendShapeBuilder");
            var frame = new BlendShapeFrameData();
            bsd.frames.Add(frame);
            bsd.NormalizeWeights();

            var meshBase = Utils.GetMesh(m_target.gameObject);
            if (meshBase == null)
                return;

            var meshNew = Instantiate(meshBase);
            meshNew.name = meshNew.name.Replace("(Clone)", ":" + bsd.name + "[" + (bsd.frames.Count-1) + "]");
            frame.mesh = Utils.MeshToGameObject(meshNew, m_target.gameObject);

            Undo.RegisterCreatedObjectUndo(frame.mesh, "BlendShapeBuilder");
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
        #endregion

    }
}
