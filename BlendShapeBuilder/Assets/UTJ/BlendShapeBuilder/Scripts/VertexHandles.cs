using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace UTJ.BlendShapeBuilder
{
    public static class VertexHandles
    {
        static int s_xVertexHandleHash = "xAxisVertexHandleHash".GetHashCode();
        static int s_yVertexHandleHash = "yAxisVertexHandleHash".GetHashCode();
        static int s_zVertexHandleHash = "zAxisVertexHandleHash".GetHashCode();
        static int s_xzVertexHandleHash = "xzAxisVertexHandleHash".GetHashCode();
        static int s_xyVertexHandleHash = "xyAxisVertexHandleHash".GetHashCode();
        static int s_yzVertexHandleHash = "yzAxisVertexHandleHash".GetHashCode();
        static int s_FreeMoveVertexHandleHash = "FreeMoveVertexHandleHash".GetHashCode();
        static int s_FreeMoveVertexHandle2Hash = "FreeMoveVertexHandle2Hash".GetHashCode();
        static bool s_positionHandleControling;
        static bool s_freeMoveHandleControling;


        public static bool positionHandleControling { get { return s_positionHandleControling; } }
        public static bool freeMoveHandleControling { get { return s_freeMoveHandleControling; } }

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
            s_positionHandleControling = cid == cidF || cid == cidX || cid == cidY || cid == cidZ;
            return pos;
        }

        public static Vector3 FreeMoveHandle(Vector3 pos)
        {
            var size = HandleUtility.GetHandleSize(pos) * 0.15f;
            Vector3 snap = Vector3.one * 0.5f;

            int cidF = GUIUtility.GetControlID(s_FreeMoveVertexHandle2Hash, FocusType.Passive);
            pos = Handles.FreeMoveHandle(cidF, pos, Quaternion.identity, size, snap, Handles.RectangleHandleCap);
            s_freeMoveHandleControling = GUIUtility.hotControl == cidF;
            return pos;
        }
    }

}
