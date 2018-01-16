#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;


public class BlendShapeBuilderPackaging
{
    [MenuItem("Assets/Make BlendShapeBuilder.unitypackage")]
    public static void MakePackage()
    {
        string[] files = new string[]
        {
            "Assets/UTJ/BlendShapeBuilder",
            "Assets/UTJ/VertexTweaker",
        };
        AssetDatabase.ExportPackage(files, "BlendShapeBuilder.unitypackage", ExportPackageOptions.Recurse);
    }

}
#endif // UNITY_EDITOR
