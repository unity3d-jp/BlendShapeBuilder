using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UTJ.VertexTweaker;


namespace UTJ.VertexTweakerEditor
{
    public class VertexTweakerWindow : EditorWindow
    {
        public static bool isOpen;

        Vector2 m_scrollPos;
        UTJ.VertexTweaker.VertexTweaker m_target;
        GameObject m_active;

        bool m_shift;
        bool m_ctrl;


        string tips = "";



        [MenuItem("Window/Vertex Tweaker")]
        public static void Open()
        {
            var window = EditorWindow.GetWindow<VertexTweakerWindow>();
            window.titleContent = new GUIContent("VertexTweaker");
            window.Show();
            window.OnSelectionChange();
        }


        private void OnEnable()
        {
            isOpen = true;
#if UNITY_2019_1_OR_NEWER
            SceneView.duringSceneGui += OnSceneGUI;
#else
            SceneView.onSceneGUIDelegate += OnSceneGUI;
#endif
        }

        private void OnDisable()
        {
#if UNITY_2019_1_OR_NEWER
            SceneView.duringSceneGui -= OnSceneGUI;
#else
            SceneView.onSceneGUIDelegate -= OnSceneGUI;
#endif
            isOpen = false;
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            if (m_target != null && m_target.editing)
            {
                Tools.current = Tool.None;

                if (HandleShortcutKeys() || HandleMouseAction())
                {
                    Event.current.Use();
                    RepaintAllViews();
                }
                else
                {
                    int ret = m_target.OnSceneGUI();
                    if ((ret & (int)SceneGUIState.Repaint) != 0)
                        RepaintAllViews();
                }
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
                    var tooltipHeight = 24;
                    var windowHeight = position.height;
                    bool repaint = false;

                    if (m_target.editing)
                    {
                        if (HandleMouseAction())
                        {
                            Event.current.Use();
                            repaint = true;
                        }
                    }


                    EditorGUILayout.BeginHorizontal();
                    EditorGUI.BeginChangeCheck();
                    m_target.editing = GUILayout.Toggle(m_target.editing, EditorGUIUtility.IconContent("EditCollider"),
                        "Button", GUILayout.Width(33), GUILayout.Height(23));
                    if (EditorGUI.EndChangeCheck())
                    {
                    }
                    GUILayout.Label("Edit Vertices");
                    EditorGUILayout.EndHorizontal();


                    if (m_target.editing)
                    {
                        EditorGUILayout.BeginVertical(GUILayout.Height(windowHeight - tooltipHeight));
                        m_scrollPos = EditorGUILayout.BeginScrollView(m_scrollPos);
                        DrawVertexTweaker();
                        EditorGUILayout.EndScrollView();

                        EditorGUILayout.LabelField(tips);
                        EditorGUILayout.EndVertical();

                        if (HandleShortcutKeys())
                        {
                            Event.current.Use();
                            repaint = true;
                        }
                    }

                    if (repaint)
                        RepaintAllViews();
                }
            }
            else if (m_active != null)
            {
                if (GUILayout.Button("Add Vertex Tweaker to " + m_active.name))
                {
                    m_active.AddComponent<UTJ.VertexTweaker.VertexTweaker>();
                    OnSelectionChange();
                }
            }
        }

        private void OnSelectionChange()
        {
            if (m_target != null)
            {
                m_target.edited = m_target.editing;
                m_target.editing = false;
            }

            m_target = null;
            m_active = null;
            if (Selection.activeGameObject != null)
            {
                m_target = Selection.activeGameObject.GetComponent<UTJ.VertexTweaker.VertexTweaker>();
                if (m_target)
                {
                    m_target.editing = m_target.edited;
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


        void RepaintAllViews()
        {
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }

        static readonly int indentSize = 18;
        static readonly int spaceSize = 5;
        static readonly int c1Width = 100;

        static readonly string[] strCommands = new string[] {
            "Move [F1]",
            "Rotate [F2]",
            "Scale [F3]",
            "Assign [F4]",
            "Projection [F5]",
            "Reset [F6]",
        };
        static readonly string[] strSelectMode = new string[] {
            "Single [1]",
            "Rect [2]",
            "Lasso [3]",
            "Brush [4]",
        };
        static readonly string[] strCoodinate = new string[] {
            "World",
            "Local",
        };


        void DrawBrushPanel()
        {
            var settings = m_target.settings;

            var brushImages = new Texture[settings.brushData.Length];
            for (int i = 0; i < settings.brushData.Length; ++i)
            {
                brushImages[i] = settings.brushData[i].image;
            }
            settings.brushActiveSlot = GUILayout.SelectionGrid(settings.brushActiveSlot, brushImages, 5);

            EditorGUILayout.Space();

            var bd = settings.activeBrush;
            bd.maxRadius = EditorGUILayout.FloatField("Max Radius", bd.maxRadius);
            bd.radius = EditorGUILayout.Slider("Radius [Shift+Drag]", bd.radius, 0.0f, bd.maxRadius);
            bd.strength = EditorGUILayout.Slider("Strength [Ctrl+Drag]", bd.strength, 0.0f, 1.0f);
            EditorGUI.BeginChangeCheck();
            bd.curve = EditorGUILayout.CurveField("Brush Shape", bd.curve, GUILayout.Width(EditorGUIUtility.labelWidth + 32), GUILayout.Height(32));
            if (EditorGUI.EndChangeCheck())
            {
                bd.UpdateSamples();
            }
        }

        void DrawEditPanel()
        {
            var settings = m_target.settings;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical(GUILayout.Width(indentSize));
            EditorGUILayout.Space();
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(GUILayout.Width(c1Width));
            {
                EditorGUI.BeginChangeCheck();
                settings.editMode = (EditMode)GUILayout.SelectionGrid((int)settings.editMode, strCommands, 1);
                if (EditorGUI.EndChangeCheck())
                {
                    if (settings.editMode != EditMode.Move &&
                        settings.editMode != EditMode.Rotate &&
                        settings.editMode != EditMode.Scale)
                        settings.softOp = false;

                    switch (settings.editMode)
                    {
                        case EditMode.Assign:
                        case EditMode.Move:
                        case EditMode.Rotate:
                        case EditMode.Scale:
                        case EditMode.Reset:
                            tips = "Shift+LB: Add selection, Ctrl+LB: Subtract selection";
                            break;
                    }
                }
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(GUILayout.Width(spaceSize));
            EditorGUILayout.Space();
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical();

            if (settings.editMode == EditMode.Assign)
            {
                bool mx = (settings.assignMask & 1) != 0;
                bool my = (settings.assignMask & 2) != 0;
                bool mz = (settings.assignMask & 4) != 0;

                EditorGUILayout.LabelField("Value");
                GUILayout.BeginHorizontal();
                mx = EditorGUILayout.ToggleLeft("X", mx, GUILayout.Width(30));
                settings.assignValue.x = EditorGUILayout.FloatField(settings.assignValue.x, GUILayout.Width(50));
                my = EditorGUILayout.ToggleLeft("Y", my, GUILayout.Width(30));
                settings.assignValue.y = EditorGUILayout.FloatField(settings.assignValue.y, GUILayout.Width(50));
                mz = EditorGUILayout.ToggleLeft("Z", mz, GUILayout.Width(30));
                settings.assignValue.z = EditorGUILayout.FloatField(settings.assignValue.z, GUILayout.Width(50));
                GUILayout.EndHorizontal();
                settings.assignMask = (mx ? 1 : 0) | (my ? 2 : 0) | (mz ? 4 : 0);

                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Coordinate", GUILayout.Width(EditorGUIUtility.labelWidth));
                EditorGUI.BeginChangeCheck();
                var coordinate = (Coordinate)GUILayout.SelectionGrid((int)settings.assignCoordinate, strCoodinate, strCoodinate.Length);
                if (EditorGUI.EndChangeCheck())
                    SetAssignCoordinate(coordinate);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Copy Selected Position [Shift+C]", GUILayout.Width(200)))
                    SetAssignValue(m_target.selectionPosition);
                GUILayout.EndHorizontal();

                EditorGUILayout.Space();

                if (GUILayout.Button("Assign [Shift+V]"))
                {
                    m_target.ApplyAssign(settings.assignValue, settings.assignCoordinate, settings.assignMask, true);
                }
            }
            else if (settings.editMode == EditMode.Move)
            {
                EditorGUI.BeginChangeCheck();
                settings.softOp = GUILayout.Toggle(settings.softOp, "Soft Move", "Button", GUILayout.Width(120));
                if(EditorGUI.EndChangeCheck())
                {
                    m_target.ClearSelection();
                    m_target.UpdateSelection();
                }
                if (settings.softOp)
                {
                    DrawBrushPanel();
                }
                EditorGUILayout.Space();

                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Coordinate", GUILayout.Width(EditorGUIUtility.labelWidth));
                settings.coordinate = (Coordinate)GUILayout.SelectionGrid((int)settings.coordinate, strCoodinate, strCoodinate.Length);
                GUILayout.EndHorizontal();
                if (settings.coordinate == Coordinate.Pivot)
                {
                    EditorGUILayout.Space();
                    settings.pivotPos = EditorGUILayout.Vector3Field("Pivot Position", settings.pivotPos);
                    settings.pivotRot = Quaternion.Euler(EditorGUILayout.Vector3Field("Pivot Rotation", settings.pivotRot.eulerAngles));
                }
                EditorGUILayout.Space();

                if (!settings.softOp)
                {
                    settings.moveAmount = EditorGUILayout.Vector3Field("Move Amount", settings.moveAmount);
                    if (GUILayout.Button("Apply Move"))
                    {
                        m_target.ApplyMove(settings.moveAmount, settings.coordinate, true);
                    }
                }
            }
            else if (settings.editMode == EditMode.Rotate)
            {
                EditorGUI.BeginChangeCheck();
                settings.softOp = GUILayout.Toggle(settings.softOp, "Soft Rotate", "Button", GUILayout.Width(120));
                if (EditorGUI.EndChangeCheck())
                {
                    m_target.ClearSelection();
                    m_target.UpdateSelection();
                }
                if (settings.softOp)
                {
                    DrawBrushPanel();
                }
                EditorGUILayout.Space();

                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Coordinate", GUILayout.Width(EditorGUIUtility.labelWidth));
                settings.coordinate = (Coordinate)GUILayout.SelectionGrid((int)settings.coordinate, strCoodinate, strCoodinate.Length);
                GUILayout.EndHorizontal();
                EditorGUILayout.Space();

                if (!settings.softOp)
                {
                    settings.pivotPos = EditorGUILayout.Vector3Field("Pivot Position", settings.pivotPos);
                    EditorGUILayout.Space();

                    settings.rotateAmount = EditorGUILayout.Vector3Field("Rotate Amount", settings.rotateAmount);
                    if (GUILayout.Button("Apply Rotate"))
                    {
                        m_target.ApplyRotatePivot(
                            Quaternion.Euler(settings.rotateAmount), settings.pivotPos, settings.pivotRot, settings.coordinate, true);
                    }
                }
            }
            else if (settings.editMode == EditMode.Scale)
            {
                EditorGUI.BeginChangeCheck();
                settings.softOp = GUILayout.Toggle(settings.softOp, "Soft Scale", "Button", GUILayout.Width(120));
                if (EditorGUI.EndChangeCheck())
                {
                    m_target.ClearSelection();
                    m_target.UpdateSelection();
                }
                if (settings.softOp)
                {
                    DrawBrushPanel();
                }
                EditorGUILayout.Space();

                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Coordinate", GUILayout.Width(EditorGUIUtility.labelWidth));
                settings.coordinate = (Coordinate)GUILayout.SelectionGrid((int)settings.coordinate, strCoodinate, strCoodinate.Length);
                GUILayout.EndHorizontal();
                EditorGUILayout.Space();

                if(!settings.softOp)
                {
                    settings.pivotPos = EditorGUILayout.Vector3Field("Pivot Position", settings.pivotPos);
                    EditorGUILayout.Space();

                    settings.scaleAmount = EditorGUILayout.Vector3Field("Scale Amount", settings.scaleAmount);
                    if (GUILayout.Button("Apply Scale"))
                    {
                        m_target.ApplyScale(settings.scaleAmount, settings.pivotPos, settings.pivotRot, settings.coordinate, true);
                    }
                }
            }
            else if (settings.editMode == EditMode.Projection)
            {
                settings.projTarget =
                    (GameObject)EditorGUILayout.ObjectField("Projection Target", settings.projTarget, typeof(GameObject), true);
                settings.projMode = (npProjectVerticesMode)EditorGUILayout.EnumPopup("Projection Mode", settings.projMode);
                EditorGUI.BeginChangeCheck();
                settings.projRayDir = (ProjectionRayDirection)EditorGUILayout.EnumPopup("Ray Direction", settings.projRayDir);
                if(EditorGUI.EndChangeCheck())
                {
                    if (settings.projRayDir == ProjectionRayDirection.Radial)
                        settings.projRadialCenter = m_target.GetComponent<Transform>().position;
                }
                if (settings.projRayDir == ProjectionRayDirection.Radial)
                {
                    EditorGUI.indentLevel++;
                    settings.projRadialCenter = EditorGUILayout.Vector3Field("Center", settings.projRadialCenter);
                    EditorGUI.indentLevel--;
                }
                else if (settings.projRayDir == ProjectionRayDirection.Directional)
                {
                    EditorGUI.indentLevel++;
                    settings.projDirection = EditorGUILayout.Vector3Field("Direction", settings.projDirection).normalized;
                    EditorGUI.indentLevel--;
                }
                settings.projMaxRayDistance = EditorGUILayout.FloatField("Max Ray Distance", settings.projMaxRayDistance);
                EditorGUILayout.Space();
                settings.projP = EditorGUILayout.Toggle("Vertices", settings.projP);
                settings.projN = EditorGUILayout.Toggle("Normals", settings.projN);
                settings.projT = EditorGUILayout.Toggle("Tangents", settings.projT);
                EditorGUILayout.Space();

                if (GUILayout.Button("Apply Projection"))
                {
                    m_target.ApplyProjection(true);
                }
            }
            else if (settings.editMode == EditMode.Reset)
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Reset (Selection)"))
                {
                    m_target.ApplyReset(true, true);
                }
                else if (GUILayout.Button("Reset (All)"))
                {
                    m_target.ApplyReset(false, true);
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }

        void DrawSelectPanel()
        {
            var settings = m_target.settings;
            if (settings.softOp)
                return;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical(GUILayout.Width(indentSize));
            EditorGUILayout.Space();
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(GUILayout.Width(c1Width));
            EditorGUILayout.LabelField("", GUILayout.Width(c1Width));
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(GUILayout.Width(spaceSize));
            EditorGUILayout.Space();
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical();

            settings.selectMode = (SelectMode)GUILayout.SelectionGrid((int)settings.selectMode, strSelectMode, 4);
            if (settings.selectMode == SelectMode.Brush)
            {
                DrawBrushPanel();
            }
            else
            {
                if (settings.selectMode == SelectMode.Single)
                {
                    EditorGUILayout.Space();
                    GUILayout.BeginHorizontal();
                    settings.selectVertex = GUILayout.Toggle(settings.selectVertex, "Vertex", "Button");
                    settings.selectTriangle = GUILayout.Toggle(settings.selectTriangle, "Triangle", "Button");
                    GUILayout.EndHorizontal();
                }
                settings.selectFrontSideOnly = EditorGUILayout.Toggle("Front Side Only", settings.selectFrontSideOnly);
            }

            EditorGUILayout.Space();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Edge [E]", GUILayout.Width(100)))
            {
                m_target.SelectEdge(m_ctrl ? -1.0f : 1.0f, !m_shift && !m_ctrl);
                m_target.UpdateSelection();
            }
            if (GUILayout.Button("Hole [H]", GUILayout.Width(100)))
            {
                m_target.SelectHole(m_ctrl ? -1.0f : 1.0f, !m_shift && !m_ctrl);
                m_target.UpdateSelection();
            }
            if (GUILayout.Button("Connected [C]", GUILayout.Width(100)))
            {
                m_target.SelectConnected(m_ctrl ? -1.0f : 1.0f, !m_shift && !m_ctrl);
                m_target.UpdateSelection();
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("All [A]", GUILayout.Width(100)))
            {
                m_target.SelectAll();
                m_target.UpdateSelection();
            }
            if (GUILayout.Button("Clear [D]", GUILayout.Width(100)))
            {
                m_target.ClearSelection();
                m_target.UpdateSelection();
            }
            if (GUILayout.Button("Invert [I]", GUILayout.Width(100)))
            {
                m_target.InvertSelection();
                m_target.UpdateSelection();
            }
            GUILayout.EndHorizontal();

            //EditorGUILayout.Space();

            //GUILayout.BeginHorizontal();
            //EditorGUILayout.LabelField("Save", GUILayout.Width(50));
            //for (int i = 0; i < 5; ++i)
            //{
            //    if (GUILayout.Button((i + 1).ToString()))
            //        settings.selectionSets[i].selection = m_target.selection;
            //}
            //GUILayout.EndHorizontal();
            //GUILayout.BeginHorizontal();
            //EditorGUILayout.LabelField("Load", GUILayout.Width(50));
            //for (int i = 0; i < 5; ++i)
            //{
            //    if (GUILayout.Button((i + 1).ToString()))
            //        m_target.selection = settings.selectionSets[i].selection;
            //}
            //GUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }

        void DrawMiscPanel()
        {
            var settings = m_target.settings;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical(GUILayout.Width(indentSize));
            EditorGUILayout.Space();
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(GUILayout.Width(c1Width));
            EditorGUILayout.LabelField("", GUILayout.Width(c1Width));
            EditorGUILayout.Space();
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(GUILayout.Width(spaceSize));
            EditorGUILayout.Space();
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical();
            {
                settings.foldMirror = EditorGUILayout.Foldout(settings.foldMirror, "Mirroring");
                if (settings.foldMirror)
                {
                    EditorGUI.indentLevel++;
                    var mirrorMode = settings.mirrorMode;
                    settings.mirrorMode = (MirrorMode)EditorGUILayout.EnumPopup("Direction", settings.mirrorMode);
                    settings.mirrorEpsilon = EditorGUILayout.FloatField("Epsilon", settings.mirrorEpsilon);
                    if (mirrorMode != settings.mirrorMode)
                        m_target.ApplyMirroring(true);
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.Space();
                settings.foldNormals = EditorGUILayout.Foldout(settings.foldNormals, "Normals");
                if (settings.foldNormals)
                {
                    EditorGUI.indentLevel++;
                    settings.normalMode = (RecalculateMode)EditorGUILayout.EnumPopup("Update Mode", settings.normalMode);
                    if (settings.normalMode == RecalculateMode.Manual)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("", GUILayout.Width(indentSize));
                        if (GUILayout.Button("Recalculate [N]", GUILayout.Width(120)))
                            m_target.RecalculateNormals(true);
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUI.indentLevel--;
                }

                settings.foldTangents = EditorGUILayout.Foldout(settings.foldTangents, "Tangents");
                if (settings.foldTangents)
                {
                    EditorGUI.indentLevel++;
                    settings.tangentsPrecision = (TangentsPrecision)EditorGUILayout.EnumPopup("Precision", settings.tangentsPrecision);
                    settings.tangentsMode = (RecalculateMode)EditorGUILayout.EnumPopup("Update Mode", settings.tangentsMode);
                    if (settings.tangentsMode == RecalculateMode.Manual)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("", GUILayout.Width(indentSize));
                        if (GUILayout.Button("Recalculate [T]", GUILayout.Width(120)))
                            m_target.RecalculateTangents(true);
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUI.indentLevel--;
                }

                if (m_target.skinned)
                {
                    EditorGUILayout.Space();
                    if (GUILayout.Button("Reset To Bindpose"))
                        m_target.ResetToBindpose(true);
                }
            }

            EditorGUILayout.Space();
            if (GUILayout.Button("Save Settings"))
                m_target.ExportSettings("Assets/UTJ/VertexTweaker/Data/DefaultSettings.asset");

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }


        static readonly string[] strInExport = new string[] {
            "Duplicate",
            "Export .asset",
            "Export .obj",
        };

        void DrawExportPanel()
        {
            var settings = m_target.settings;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical(GUILayout.Width(indentSize));
            EditorGUILayout.Space();
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(GUILayout.Width(c1Width));
            settings.inexportIndex = GUILayout.SelectionGrid(settings.inexportIndex, strInExport, 1);
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(GUILayout.Width(spaceSize));
            EditorGUILayout.Space();
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical();

            if (settings.inexportIndex == 0)
            {
                if (GUILayout.Button("Duplicate"))
                {
                    var dup = Instantiate(m_target.mesh);
                    Utils.MeshToGameObject(dup, Vector3.zero, Utils.GetMaterials(m_target.gameObject));
                }
            }
            if (settings.inexportIndex == 1)
            {
                if (GUILayout.Button("Export .asset file"))
                {
                    string path = EditorUtility.SaveFilePanel("Export .asset file", "Assets", Utils.SanitizeFileName(m_target.name), "asset");
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
                            AssetDatabase.CreateAsset(Instantiate(m_target.mesh), path);
                            Debug.Log("Asset exported: " + path);
                        }
                    }
                }
            }
            else if (settings.inexportIndex == 2)
            {
                settings.objFlipHandedness = EditorGUILayout.Toggle("Flip Handedness", settings.objFlipHandedness);
                settings.objFlipFaces = EditorGUILayout.Toggle("Flip Faces", settings.objFlipFaces);
                settings.objMakeSubmeshes = EditorGUILayout.Toggle("Make Submeshes", settings.objMakeSubmeshes);
                settings.objApplyTransform = EditorGUILayout.Toggle("Apply Transform", settings.objApplyTransform);
                settings.objIncludeChildren = EditorGUILayout.Toggle("Include Children", settings.objIncludeChildren);
            
                if (GUILayout.Button("Export .obj file"))
                {
                    string path = EditorUtility.SaveFilePanel("Export .obj file", "", Utils.SanitizeFileName(m_target.name), "obj");
                    ObjExporter.Export(m_target.gameObject, path, new ObjExporter.Settings
                    {
                        flipFaces = settings.objFlipFaces,
                        flipHandedness = settings.objFlipHandedness,
                        includeChildren = settings.objIncludeChildren,
                        makeSubmeshes = settings.objMakeSubmeshes,
                        applyTransform = settings.objApplyTransform,
                    });
                }
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }


        static readonly string[] strDisplay = new string[] {
            "Display",
            "Options",
        };


        void DrawDisplayPanel()
        {
            var settings = m_target.settings;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical(GUILayout.Width(indentSize));
            EditorGUILayout.Space();
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(GUILayout.Width(c1Width));
            settings.displayIndex = GUILayout.SelectionGrid(settings.displayIndex, strDisplay, 1);
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(GUILayout.Width(spaceSize));
            EditorGUILayout.Space();
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical();
            if (settings.displayIndex == 0)
            {
                settings.visualize = EditorGUILayout.Toggle("Visualize [Tab]", settings.visualize);
                if (settings.visualize)
                {
                    EditorGUI.indentLevel++;
                    settings.showVertices = EditorGUILayout.Toggle("Vertices", settings.showVertices);
                    settings.showNormals = EditorGUILayout.Toggle("Normals", settings.showNormals);
                    settings.showTangents = EditorGUILayout.Toggle("Tangents", settings.showTangents);
                    settings.showBinormals = EditorGUILayout.Toggle("Binormals", settings.showBinormals);
                    EditorGUILayout.Space();
                    settings.showSelectedOnly = EditorGUILayout.Toggle("Selection Only", settings.showSelectedOnly);
                    EditorGUI.indentLevel--;
                    EditorGUILayout.Space();
                }
                //settings.showSelectedOnly = EditorGUILayout.Toggle("Selected Only", settings.showSelectedOnly);

                settings.modelOverlay = (ModelOverlay)EditorGUILayout.EnumPopup("Overlay", settings.modelOverlay);
                settings.showBrushRange = EditorGUILayout.Toggle("Brush Range", settings.showBrushRange);
            }
            else if (settings.displayIndex == 1)
            {
                settings.vertexSize = EditorGUILayout.Slider("Vertex Size", settings.vertexSize, 0.0f, 0.05f);
                settings.normalSize = EditorGUILayout.Slider("Normal Size", settings.normalSize, 0.0f, 1.00f);
                settings.tangentSize = EditorGUILayout.Slider("Tangent Size", settings.tangentSize, 0.0f, 1.00f);
                settings.binormalSize = EditorGUILayout.Slider("Binormal Size", settings.binormalSize, 0.0f, 1.00f);

                EditorGUILayout.Space();

                settings.vertexColor = EditorGUILayout.ColorField("Vertex Color", settings.vertexColor);
                settings.vertexColor2 = EditorGUILayout.ColorField("Vertex Color (Selected)", settings.vertexColor2);
                settings.vertexColor3 = EditorGUILayout.ColorField("Vertex Color (Highlighted)", settings.vertexColor3);
                settings.normalColor = EditorGUILayout.ColorField("Normal Color", settings.normalColor);
                settings.tangentColor = EditorGUILayout.ColorField("Tangent Color", settings.tangentColor);
                settings.binormalColor = EditorGUILayout.ColorField("Binormal Color", settings.binormalColor);
                if (GUILayout.Button("Reset"))
                {
                    settings.ResetDisplayOptions();
                }
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }


        void DrawVertexTweaker()
        {
            var settings = m_target.settings;

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.Space();

            EditorGUILayout.BeginVertical("Box");
            settings.foldEdit = EditorGUILayout.Foldout(settings.foldEdit, "Edit");
            if (settings.foldEdit)
                DrawEditPanel();
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical("Box");
            settings.foldSelect = EditorGUILayout.Foldout(settings.foldSelect, "Select");
            if (settings.foldSelect)
                DrawSelectPanel();
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical("Box");
            settings.foldMisc = EditorGUILayout.Foldout(settings.foldMisc, "Misc");
            if (settings.foldMisc)
                DrawMiscPanel();
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.BeginVertical("Box");
            settings.foldExport = EditorGUILayout.Foldout(settings.foldExport, "Export");
            if (settings.foldExport)
                DrawExportPanel();
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical("Box");
            settings.foldDisplay = EditorGUILayout.Foldout(settings.foldDisplay, "Display");
            if (settings.foldDisplay)
                DrawDisplayPanel();
            EditorGUILayout.EndVertical();

            if (EditorGUI.EndChangeCheck())
                RepaintAllViews();
        }


        bool HandleShortcutKeys()
        {
            bool handled = false;
            var settings = m_target.settings;
            var e = Event.current;

            m_shift = e.shift;
            m_ctrl = e.control;

            if (e.type == EventType.KeyDown)
            {
                var prevEditMode = settings.editMode;
                switch (e.keyCode)
                {
                    case KeyCode.F1: settings.editMode = EditMode.Move; break;
                    case KeyCode.F2: settings.editMode = EditMode.Rotate; break;
                    case KeyCode.F3: settings.editMode = EditMode.Scale; break;
                    case KeyCode.F4: settings.editMode = EditMode.Assign; break;
                    case KeyCode.F5: settings.editMode = EditMode.Projection; break;
                    case KeyCode.F6: settings.editMode = EditMode.Reset; break;
                }
                if (settings.editMode != prevEditMode)
                    handled = true;

                {
                    var prevSelectMode = settings.selectMode;
                    switch (e.keyCode)
                    {
                        case KeyCode.Alpha1: settings.selectMode = SelectMode.Single; break;
                        case KeyCode.Alpha2: settings.selectMode = SelectMode.Rect; break;
                        case KeyCode.Alpha3: settings.selectMode = SelectMode.Lasso; break;
                        case KeyCode.Alpha4: settings.selectMode = SelectMode.Brush; break;
                    }
                    if (settings.selectMode != prevSelectMode)
                        handled = true;
                }

                if (e.keyCode == KeyCode.C && e.shift)
                {
                    handled = true;
                    tips = "Copy";
                    SetAssignValue(m_target.selectionPosition);
                }
                else if (e.keyCode == KeyCode.V && e.shift)
                {
                    handled = true;
                    tips = "Paste";
                    m_target.ApplyAssign(settings.assignValue, settings.coordinate, settings.assignMask, true);
                }
                else if (e.keyCode == KeyCode.Tab)
                {
                    handled = true;
                    tips = "Toggle Visualization";
                    settings.visualize = !settings.visualize;

                }
                else if (e.keyCode == KeyCode.A)
                {
                    handled = true;
                    tips = "Select All";
                    m_target.SelectAll();
                    m_target.UpdateSelection();
                }
                else if (e.keyCode == KeyCode.I)
                {
                    handled = true;
                    tips = "Invert Selection";
                    m_target.InvertSelection();
                    m_target.UpdateSelection();
                }
                else if (e.keyCode == KeyCode.E)
                {
                    handled = true;
                    tips = "Select Edge";
                    m_target.SelectEdge(m_ctrl ? -1.0f : 1.0f, !m_shift && !m_ctrl);
                    m_target.UpdateSelection();
                }
                else if (e.keyCode == KeyCode.H)
                {
                    handled = true;
                    tips = "Select Hole";
                    m_target.SelectHole(m_ctrl ? -1.0f : 1.0f, !m_shift && !m_ctrl);
                    m_target.UpdateSelection();
                }
                else if (e.keyCode == KeyCode.C)
                {
                    handled = true;
                    tips = "Select Connected";
                    m_target.SelectConnected(m_ctrl ? -1.0f : 1.0f, !m_shift && !m_ctrl);
                    m_target.UpdateSelection();
                }
                else if (e.keyCode == KeyCode.D)
                {
                    handled = true;
                    tips = "Clear Selection";
                    m_target.ClearSelection();
                    m_target.UpdateSelection();
                }
                else if (e.keyCode == KeyCode.N)
                {
                    handled = true;
                    tips = "Recalculate Normals";
                    m_target.RecalculateNormals(true);
                }
                else if (e.keyCode == KeyCode.T)
                {
                    handled = true;
                    tips = "Recalculate Tangents";
                    m_target.RecalculateTangents(true);
                }
            }

            return handled;
        }

        bool HandleMouseAction()
        {
            bool handled = false;
            var settings = m_target.settings;
            var e = Event.current;

            if (e.type == EventType.MouseDrag)
            {
                float amount = e.delta.x - e.delta.y;

                if (((settings.editMode == EditMode.Move || settings.editMode == EditMode.Rotate || settings.editMode == EditMode.Scale) && settings.softOp) ||
                    (settings.selectMode == SelectMode.Brush))
                {
                    var bd = settings.activeBrush;
                    if (e.shift)
                    {
                        bd.radius = Mathf.Clamp(bd.radius + amount * (bd.maxRadius * 0.0025f), 0.0f, bd.maxRadius);
                        handled = true;
                    }
                    else if (e.control)
                    {
                        bd.strength = Mathf.Clamp(bd.strength + amount * 0.0025f, 0.0f, 1.0f);
                        handled = true;
                    }
                }
            }

            return handled;
        }



        void SetAssignCoordinate(Coordinate c)
        {
            var settings = m_target.settings;
            settings.assignCoordinate = c;
            switch (c)
            {
                case Coordinate.World:
                    settings.assignValue = m_target.GetComponent<Transform>().localToWorldMatrix.MultiplyPoint(settings.assignValue);
                    break;
                case Coordinate.Local:
                    settings.assignValue = m_target.GetComponent<Transform>().worldToLocalMatrix.MultiplyPoint(settings.assignValue);
                    break;
            }
        }
        void SetAssignValue(Vector3 worldPos)
        {
            var settings = m_target.settings;
            switch (settings.assignCoordinate)
            {
                case Coordinate.World:
                    settings.assignValue = worldPos;
                    break;
                case Coordinate.Local:
                    settings.assignValue = m_target.GetComponent<Transform>().worldToLocalMatrix.MultiplyPoint(worldPos);
                    break;
            }
        }
    }
}
