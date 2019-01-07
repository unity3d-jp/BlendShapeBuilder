using UnityEngine;
using UnityEditor;
using System.Collections;
using UTJ.VertexTweaker;

namespace UTJ.VertexTweakerEditor
{
    [CustomEditor(typeof(UTJ.VertexTweaker.VertexTweaker))]
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
