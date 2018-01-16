using UnityEngine;

namespace UTJ.BlendShapeBuilder
{
    [ExecuteInEditMode]
    public class BlendShapeBuilder : MonoBehaviour
    {
#if UNITY_EDITOR
        [SerializeField] BlendShapeBuilderData m_data = new BlendShapeBuilderData();


        public BlendShapeBuilderData data { get { return m_data; } }

        void Reset()
        {
            if (m_data == null)
                m_data = new BlendShapeBuilderData();
            if (m_data.baseMesh == null)
                m_data.baseMesh = gameObject;
            if (m_data.blendShapeData.Count == 0)
                m_data.blendShapeData.Add(new BlendShapeData { name = "NewBlendShape0" });

        }
#endif
    }
}
