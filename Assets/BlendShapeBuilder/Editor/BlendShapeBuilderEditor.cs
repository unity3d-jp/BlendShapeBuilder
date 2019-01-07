using UnityEngine;
using UnityEditor;
using System.Collections;
using UTJ.BlendShapeBuilder;

namespace UTJ.BlendShapeBuilderEditor
{
    [CustomEditor(typeof(UTJ.BlendShapeBuilder.BlendShapeBuilder))]
    public class BlendShapeBuilderEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            if (GUILayout.Button("Open Window"))
                BlendShapeBuilderWindow.Open();

            EditorGUILayout.Space();
        }
    }
}
