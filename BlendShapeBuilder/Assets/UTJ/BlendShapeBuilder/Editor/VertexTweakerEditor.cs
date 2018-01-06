using UnityEngine;
using UnityEditor;
using System.Collections;

namespace UTJ.BlendShapeBuilder
{
    [CustomEditor(typeof(VertexTweaker))]
    public class VertexTweakerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            if (GUILayout.Button("Open Window"))
                VertexTweakerWindow.Open();

            EditorGUILayout.Space();
        }
    }
}
