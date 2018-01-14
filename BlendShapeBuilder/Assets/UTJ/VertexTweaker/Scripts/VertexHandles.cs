using System.Reflection;
using UnityEngine;
using UnityEditor;

namespace UTJ.VertexTweaker
{
    public static class VertexHandles
    {
        // "immutable" control ids
        static readonly int axisMoveHandleFCID = "AxisMoveVertexHandleFHash".GetHashCode();
        static readonly int axisMoveHandleXCID = "AxisMoveVertexHandleXHash".GetHashCode();
        static readonly int axisMoveHandleYCID = "AxisMoveVertexHandleYHash".GetHashCode();
        static readonly int axisMoveHandleZCID = "AxisMoveVertexHandleZHash".GetHashCode();

        public static bool axisMoveHandleGainedControl;
        public static bool axisMoveHandleLostControl;
        public static bool axisMoveHandleHasControl;
        public static bool axisMoveHandleNear;
        public static Vector3 AxisMoveHandle(Vector3 pos, Quaternion rot)
        {
            var size = HandleUtility.GetHandleSize(pos);
            var snap = 0.0f;
            var snap3 = Vector3.zero;

            var et = Event.current.rawType;
            int hcOld = GUIUtility.hotControl;
            int ncOld = HandleUtility.nearestControl;

            pos = Handles.FreeMoveHandle(axisMoveHandleFCID, pos, Quaternion.identity, size * 0.2f, snap3, Handles.RectangleHandleCap);
            Handles.color = Handles.xAxisColor;
            pos = Handles.Slider(axisMoveHandleXCID, pos, rot * Vector3.right, size, Handles.ArrowHandleCap, snap);
            Handles.color = Handles.yAxisColor;
            pos = Handles.Slider(axisMoveHandleYCID, pos, rot * Vector3.up, size, Handles.ArrowHandleCap, snap);
            Handles.color = Handles.zAxisColor;
            pos = Handles.Slider(axisMoveHandleZCID, pos, rot * Vector3.forward, size, Handles.ArrowHandleCap, snap);

            // check handle has control
            axisMoveHandleGainedControl = axisMoveHandleLostControl = false;
            switch (et)
            {
                case EventType.MouseDown:
                    if (Event.current.button == 0 && GUIUtility.hotControl != hcOld)
                        axisMoveHandleHasControl = axisMoveHandleGainedControl = true;
                    break;
                case EventType.MouseUp:
                    if (Event.current.button == 0 && axisMoveHandleHasControl)
                    {
                        axisMoveHandleLostControl = true;
                        axisMoveHandleHasControl = false;
                    }
                    break;
                case EventType.Layout:
                    axisMoveHandleNear = HandleUtility.nearestControl != ncOld;
                    break;
            }

            return pos;
        }


        static readonly int freeMoveHandleCID = "FreeMoveVertexHandleHash".GetHashCode();
        public static bool freeMoveHandleGainedControl;
        public static bool freeMoveHandleLostControl;
        public static bool freeMoveHandleHasControl;
        public static bool freeMoveHandleNear;

        public static Vector3 FreeMoveHandle(Vector3 pos, float rectSize)
        {
            var size = HandleUtility.GetHandleSize(pos) * (rectSize * 0.01f);
            Vector3 snap = Vector3.one * 0.5f;

            var et = Event.current.rawType;
            int hcOld = GUIUtility.hotControl;
            int ncOld = HandleUtility.nearestControl;

            pos = Handles.FreeMoveHandle(freeMoveHandleCID, pos, Quaternion.identity, size, snap, Handles.RectangleHandleCap);

            // check handle has control
            freeMoveHandleGainedControl = freeMoveHandleLostControl = false;
            switch (et)
            {
                case EventType.MouseDown:
                    if (Event.current.button == 0 && GUIUtility.hotControl != hcOld)
                        freeMoveHandleHasControl = freeMoveHandleGainedControl = true;
                    break;
                case EventType.MouseUp:
                    if (Event.current.button == 0 && freeMoveHandleHasControl)
                    {
                        freeMoveHandleLostControl = true;
                        freeMoveHandleHasControl = false;
                    }
                    break;
                case EventType.Layout:
                    freeMoveHandleNear = HandleUtility.nearestControl != ncOld;
                    break;
            }
            return pos;
        }


        static object s_RotationHandleIds;
        static object s_RotationHandleParam;
        static MethodInfo s_DoRotationHandle;
        static int s_XRotationHandleID = 0x7fe8700;
        static int s_YRotationHandleID = 0x7fe8701;
        static int s_ZRotationHandleID = 0x7fe8702;
        static int s_CamRotationHandleID = 0x7fe8703;
        static int s_XYZRotationHandleID = 0x7fe8704;

        public static bool rotationHandleGainedControl;
        public static bool rotationHandleLostControl;
        public static bool rotationHandleHasControl;
        public static bool rotationHandleNear;
        public static bool freeRotating;

        public static Quaternion RotationHandle(Quaternion rot, Vector3 pos)
        {
            // try to call Handles.DoRotationHandle() to distinct free rotation or axis rotation
            if (s_RotationHandleIds == null)
            {
                var RotationHandleIds_t = typeof(UnityEditor.Handles).GetNestedType("RotationHandleIds", BindingFlags.NonPublic);
                if (RotationHandleIds_t != null)
                {
                    var ctor = RotationHandleIds_t.GetConstructor(new System.Type[] { typeof(int), typeof(int), typeof(int), typeof(int), typeof(int) });
                    s_RotationHandleIds = ctor.Invoke(new object[] {
                        s_XRotationHandleID, s_YRotationHandleID, s_ZRotationHandleID, s_CamRotationHandleID, s_XYZRotationHandleID });
                }

                var RotationHandleParam_t = typeof(UnityEditor.Handles).GetNestedType("RotationHandleParam", BindingFlags.NonPublic);
                if (RotationHandleParam_t != null)
                {
                    var pDefault = RotationHandleParam_t.GetProperty("Default", BindingFlags.Public | BindingFlags.Static);
                    if (pDefault != null)
                    {
                        s_RotationHandleParam = pDefault.GetGetMethod().Invoke(null, null);
                    }
                }

                s_DoRotationHandle = typeof(UnityEditor.Handles).GetMethod("DoRotationHandle", BindingFlags.NonPublic | BindingFlags.Static);
            }

            var et = Event.current.rawType;
            int hcOld = GUIUtility.hotControl;
            int ncOld = HandleUtility.nearestControl;

            if (s_RotationHandleIds != null && s_RotationHandleParam != null && s_DoRotationHandle != null)
                rot = (Quaternion)s_DoRotationHandle.Invoke(null, new object[] { s_RotationHandleIds, rot, pos, s_RotationHandleParam });
            else
                rot = Handles.RotationHandle(rot, pos);

            // check handle has control
            rotationHandleGainedControl = rotationHandleLostControl = false;
            switch (et)
            {
                case EventType.MouseDown:
                    if (Event.current.button == 0 && GUIUtility.hotControl != hcOld)
                    {
                        rotationHandleHasControl = rotationHandleGainedControl = true;
                        freeRotating = GUIUtility.hotControl == s_XYZRotationHandleID;
                    }
                    break;
                case EventType.MouseUp:
                    if (Event.current.button == 0 && rotationHandleHasControl)
                    {
                        rotationHandleLostControl = true;
                        rotationHandleHasControl = false;
                        freeRotating = false;
                    }
                    break;
                case EventType.Layout:
                    rotationHandleNear = HandleUtility.nearestControl != ncOld;
                    break;
            }
            return rot;
        }


        public static bool scaleHandleGainedControl;
        public static bool scaleHandleLostControl;
        public static bool scaleHandleHasControl;
        public static bool scaleHandleNear;
        public static Vector3 ScaleHandle(Vector3 scale, Vector3 pos, Quaternion rot)
        {
            var et = Event.current.rawType;
            int hcOld = GUIUtility.hotControl;
            int ncOld = HandleUtility.nearestControl;

            var size = HandleUtility.GetHandleSize(pos);

            scale = Handles.ScaleHandle(scale, pos, rot, size);

            // check handle has control
            scaleHandleGainedControl = scaleHandleLostControl = false;
            switch (et)
            {
                case EventType.MouseDown:
                    if (Event.current.button == 0 && GUIUtility.hotControl != hcOld)
                        scaleHandleHasControl = scaleHandleGainedControl = true;
                    break;
                case EventType.MouseUp:
                    if (Event.current.button == 0 && scaleHandleHasControl)
                    {
                        scaleHandleLostControl = true;
                        scaleHandleHasControl = false;
                    }
                    break;
                case EventType.Layout:
                    scaleHandleNear = HandleUtility.nearestControl != ncOld;
                    break;
            }
            return scale;
        }
    }

}
