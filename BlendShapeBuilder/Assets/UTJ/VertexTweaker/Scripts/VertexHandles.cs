using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace UTJ.VertexTweaker
{
    public static class VertexHandles
    {
        #region impl
        static int s_xVertexHandleHash = "xAxisVertexHandleHash".GetHashCode();
        static int s_yVertexHandleHash = "yAxisVertexHandleHash".GetHashCode();
        static int s_zVertexHandleHash = "zAxisVertexHandleHash".GetHashCode();
        static int s_FreeMoveVertexHandleHash = "FreeMoveVertexHandleHash".GetHashCode();
        #endregion

        public static bool axisMoveHandleControling;
        public static bool axisMoveHandleNear;
        public static Vector3 AxisMoveHandle(Vector3 pos, Quaternion rot)
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

            var hc = GUIUtility.hotControl;
            axisMoveHandleControling = hc == cidF || hc == cidX || hc == cidY || hc == cidZ;
            var nc = HandleUtility.nearestControl;
            axisMoveHandleNear = nc == cidF || nc == cidX || nc == cidY || nc == cidZ;
            return pos;
        }


        #region impl
        static int s_FreeMoveVertexHandle2Hash = "FreeMoveVertexHandle2Hash".GetHashCode();
        #endregion

        public static bool freeMoveHandleControling;
        public static Vector3 FreeMoveHandle(Vector3 pos, float rectSize, bool forceCapture)
        {
            var size = HandleUtility.GetHandleSize(pos) * (rectSize * 0.01f);
            Vector3 snap = Vector3.one * 0.5f;

            int cid = GUIUtility.GetControlID(s_FreeMoveVertexHandle2Hash, FocusType.Passive);
            var e = Event.current;
            if (forceCapture && GUIUtility.hotControl != cid &&
                ((e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && e.button == 0))
                GUIUtility.hotControl = 0;
            pos = Handles.FreeMoveHandle(cid, pos, Quaternion.identity, size, snap, Handles.RectangleHandleCap);
            freeMoveHandleControling = GUIUtility.hotControl == cid;
            return pos;
        }


        public static bool rotationHandleControling;
        public static bool rotationHandleNear;
        public static Quaternion RotationHandle(Quaternion rot, Vector3 pos)
        {
            var hc = GUIUtility.hotControl;
            var nc = HandleUtility.nearestControl;

            rot = Handles.RotationHandle(rot, pos);

            rotationHandleControling = GUIUtility.hotControl != hc;
            rotationHandleNear = HandleUtility.nearestControl != nc;
            return rot;
        }


        public static bool scaleHandleControling;
        public static bool scaleHandleNear;
        public static Vector3 ScaleHandle(Vector3 scale, Vector3 pos, Quaternion rot)
        {
            var hc = GUIUtility.hotControl;
            var nc = HandleUtility.nearestControl;
            var size = HandleUtility.GetHandleSize(pos);

            scale = Handles.ScaleHandle(scale, pos, rot, size);
            scaleHandleControling = GUIUtility.hotControl != hc;
            scaleHandleNear = HandleUtility.nearestControl != nc;
            return scale;
        }
    }

}
