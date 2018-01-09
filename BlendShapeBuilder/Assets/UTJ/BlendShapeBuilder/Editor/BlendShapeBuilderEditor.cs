using UnityEngine;
using UnityEditor;
using System.Collections;

namespace UTJ.BlendShapeBuilder
{
    [CustomEditor(typeof(BlendShapeBuilder))]
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
