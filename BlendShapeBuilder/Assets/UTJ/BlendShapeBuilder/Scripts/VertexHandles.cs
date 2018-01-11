using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace UTJ.BlendShapeBuilder
{
    public static class VertexHandles
    {
        #region impl
        static int s_xVertexHandleHash = "xAxisVertexHandleHash".GetHashCode();
        static int s_yVertexHandleHash = "yAxisVertexHandleHash".GetHashCode();
        static int s_zVertexHandleHash = "zAxisVertexHandleHash".GetHashCode();
        static int s_FreeMoveVertexHandleHash = "FreeMoveVertexHandleHash".GetHashCode();
        #endregion

        public static bool positionHandleControling;
        public static Vector3 PositionHandle(Vector3 pos, Quaternion rot)
        {
            var size = HandleUtility.GetHandleSize(pos);
            var snap = 0.0f;
            var snap3 = Vector3.zero;

            int cidF = GUIUtility.GetControlID(s_FreeMoveVertexHandleHash, FocusType.Passive);
            int cidX = GUIUtility.GetControlID(s_xVertexHandleHash, FocusType.Passive);
            int cidY = GUIUtility.GetControlID(s_yVertexHandleHash, FocusType.Passive);
            int cidZ = GUIUtility.GetControlID(s_zVertexHandleHash, FocusType.Passive);

            pos = Handles.FreeMoveHandle(cidF, pos, Quaternion.identity, size * 0.2f, snap3, Handles.RectangleHandleCap);
            Handles.color = Handles.xAxisColor;
            pos = Handles.Slider(cidX, pos, rot * Vector3.right, size, Handles.ArrowHandleCap, snap);
            Handles.color = Handles.yAxisColor;
            pos = Handles.Slider(cidY, pos, rot * Vector3.up, size, Handles.ArrowHandleCap, snap);
            Handles.color = Handles.zAxisColor;
            pos = Handles.Slider(cidZ, pos, rot * Vector3.forward, size, Handles.ArrowHandleCap, snap);

            var cid = GUIUtility.hotControl;
            positionHandleControling = cid == cidF || cid == cidX || cid == cidY || cid == cidZ;
            return pos;
        }


        #region impl
        static int s_FreeMoveVertexHandle2Hash = "FreeMoveVertexHandle2Hash".GetHashCode();
        #endregion

        public static bool freeMoveHandleControling;
        public static Vector3 FreeMoveHandle(Vector3 pos, float rectSize)
        {
            var size = HandleUtility.GetHandleSize(pos) * (rectSize * 0.01f);
            Vector3 snap = Vector3.one * 0.5f;

            int cidF = GUIUtility.GetControlID(s_FreeMoveVertexHandle2Hash, FocusType.Passive);
            var e = Event.current;
            if ((e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && e.button == 0)
                GUIUtility.hotControl = cidF;
            pos = Handles.FreeMoveHandle(cidF, pos, Quaternion.identity, size, snap, Handles.RectangleHandleCap);
            freeMoveHandleControling = GUIUtility.hotControl == cidF;
            return pos;
        }


        public static bool rotationHandleControling;
        public static Quaternion RotationHandle(Quaternion rot, Vector3 pos)
        {
            var hc = GUIUtility.hotControl;
            rot = Handles.RotationHandle(rot, pos);
            rotationHandleControling = GUIUtility.hotControl != hc;
            return rot;
        }


        public static bool scaleHandleControling;
        public static Vector3 ScaleHandle(Vector3 scale, Vector3 pos, Quaternion rot)
        {
            var hc = GUIUtility.hotControl;
            var size = HandleUtility.GetHandleSize(pos);
            scale = Handles.ScaleHandle(scale, pos, rot, size);
            scaleHandleControling = GUIUtility.hotControl != hc;
            return scale;
        }
    }

}
