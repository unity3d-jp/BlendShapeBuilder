#if UNITY_EDITOR
using System.Reflection;
using UnityEngine;
using UnityEditor;

namespace UTJ.VertexTweaker
{
    public static class VertexHandles
    {
        // "immutable" control ids
        static readonly int s_positionXHandleID = 0x7fe8700;
        static readonly int s_positionYHandleID = 0x7fe8701;
        static readonly int s_positionZHandleID = 0x7fe8702;
        static readonly int s_positionXYHandleID= 0x7fe8703;
        static readonly int s_positionXZHandleID= 0x7fe8704;
        static readonly int s_positionYZHandleID= 0x7fe8705;
        static readonly int s_positionFHandleID = 0x7fe8706;
        static readonly int s_freeMoveHandleID  = 0x7fe8707;
        static readonly int s_rotationXHandleID = 0x7fe8708;
        static readonly int s_rotationYHandleID = 0x7fe8709;
        static readonly int s_rotationZHandleID = 0x7fe870a;
        static readonly int s_rotationCamHandleID = 0x7fe870b;
        static readonly int s_rotationXYZHandleID = 0x7fe870c;

        static object s_positionHandleIds;
        static MethodInfo s_DoPositionHandle;

        public static bool axisMoveHandleGainedControl;
        public static bool axisMoveHandleHasControl;
        public static bool axisMoveHandleNear;
        public static Vector3 AxisMoveHandle(Vector3 pos, Quaternion rot)
        {
            // try to call Handles.DoPositionHandle() to assign immutable ids
            if (s_positionHandleIds == null)
            {
                var PositionHandleIds_t = typeof(UnityEditor.Handles).GetNestedType("PositionHandleIds", BindingFlags.NonPublic);
                if (PositionHandleIds_t != null)
                {
                    var ctor = PositionHandleIds_t.GetConstructor(new System.Type[] { typeof(int), typeof(int), typeof(int), typeof(int), typeof(int), typeof(int), typeof(int) });
                    s_positionHandleIds = ctor.Invoke(new object[] {
                        s_positionXHandleID, s_positionYHandleID, s_positionZHandleID,
                        s_positionXYHandleID, s_positionXZHandleID, s_positionYZHandleID,
                        s_positionFHandleID
                    });
                }
                s_DoPositionHandle = typeof(UnityEditor.Handles).GetMethod("DoPositionHandle", BindingFlags.NonPublic | BindingFlags.Static);
            }


            var et = Event.current.rawType;
            int hcOld = GUIUtility.hotControl;
            int ncOld = HandleUtility.nearestControl;

            if (s_positionHandleIds != null && s_DoPositionHandle != null)
                pos = (Vector3)s_DoPositionHandle.Invoke(null, new object[] { s_positionHandleIds, pos, rot });
            else
                pos = Handles.PositionHandle(pos, rot);

            // check handle has control
            axisMoveHandleGainedControl = false;
            switch (et)
            {
                case EventType.MouseDown:
                    if (Event.current.button == 0 && GUIUtility.hotControl != hcOld)
                        axisMoveHandleHasControl = axisMoveHandleGainedControl = true;
                    break;
                case EventType.MouseUp:
                    if (Event.current.button == 0 && axisMoveHandleHasControl)
                        axisMoveHandleHasControl = false;
                    break;
                case EventType.Layout:
                    axisMoveHandleNear = HandleUtility.nearestControl != ncOld;
                    break;
            }

            return pos;
        }


        public static bool freeMoveHandleGainedControl;
        public static bool freeMoveHandleHasControl;
        public static bool freeMoveHandleNear;

        public static Vector3 FreeMoveHandle(Vector3 pos, float rectSize)
        {
            var size = HandleUtility.GetHandleSize(pos) * (rectSize * 0.01f);
            Vector3 snap = Vector3.one * 0.5f;

            var et = Event.current.rawType;
            int hcOld = GUIUtility.hotControl;
            int ncOld = HandleUtility.nearestControl;

            pos = Handles.FreeMoveHandle(s_freeMoveHandleID, pos, Quaternion.identity, size, snap, Handles.RectangleHandleCap);

            // check handle has control
            freeMoveHandleGainedControl = false;
            switch (et)
            {
                case EventType.MouseDown:
                    if (Event.current.button == 0 && GUIUtility.hotControl != hcOld)
                        freeMoveHandleHasControl = freeMoveHandleGainedControl = true;
                    break;
                case EventType.MouseUp:
                    if (Event.current.button == 0 && freeMoveHandleHasControl)
                        freeMoveHandleHasControl = false;
                    break;
                case EventType.Layout:
                    freeMoveHandleNear = HandleUtility.nearestControl != ncOld;
                    break;
            }
            return pos;
        }


        static object s_rotationHandleIds;
        static object s_rotationHandleParam;
        static MethodInfo s_DoRotationHandle;

        public static bool rotationHandleGainedControl;
        public static bool rotationHandleHasControl;
        public static bool rotationHandleNear;
        public static bool freeRotating;

        public static Quaternion RotationHandle(Quaternion rot, Vector3 pos)
        {
            // try to call Handles.DoRotationHandle() to distinguish free rotation or axis rotation
            if (s_rotationHandleIds == null)
            {
                var RotationHandleIds_t = typeof(UnityEditor.Handles).GetNestedType("RotationHandleIds", BindingFlags.NonPublic);
                if (RotationHandleIds_t != null)
                {
                    var ctor = RotationHandleIds_t.GetConstructor(new System.Type[] { typeof(int), typeof(int), typeof(int), typeof(int), typeof(int) });
                    s_rotationHandleIds = ctor.Invoke(new object[] {
                        s_rotationXHandleID, s_rotationYHandleID, s_rotationZHandleID, s_rotationCamHandleID, s_rotationXYZHandleID });
                }

                var RotationHandleParam_t = typeof(UnityEditor.Handles).GetNestedType("RotationHandleParam", BindingFlags.NonPublic);
                if (RotationHandleParam_t != null)
                {
                    var pDefault = RotationHandleParam_t.GetProperty("Default", BindingFlags.Public | BindingFlags.Static);
                    if (pDefault != null)
                    {
                        s_rotationHandleParam = pDefault.GetGetMethod().Invoke(null, null);
                    }
                }

                s_DoRotationHandle = typeof(UnityEditor.Handles).GetMethod("DoRotationHandle", BindingFlags.NonPublic | BindingFlags.Static);
            }

            var et = Event.current.rawType;
            int hcOld = GUIUtility.hotControl;
            int ncOld = HandleUtility.nearestControl;

            if (s_rotationHandleIds != null && s_rotationHandleParam != null && s_DoRotationHandle != null)
                rot = (Quaternion)s_DoRotationHandle.Invoke(null, new object[] { s_rotationHandleIds, rot, pos, s_rotationHandleParam });
            else
                rot = Handles.RotationHandle(rot, pos);

            // check handle has control
            rotationHandleGainedControl = false;
            switch (et)
            {
                case EventType.MouseDown:
                    if (Event.current.button == 0 && GUIUtility.hotControl != hcOld)
                    {
                        rotationHandleHasControl = rotationHandleGainedControl = true;
                        freeRotating = GUIUtility.hotControl == s_rotationXYZHandleID;
                    }
                    break;
                case EventType.MouseUp:
                    if (Event.current.button == 0 && rotationHandleHasControl)
                    {
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
            scaleHandleGainedControl = false;
            switch (et)
            {
                case EventType.MouseDown:
                    if (Event.current.button == 0 && GUIUtility.hotControl != hcOld)
                        scaleHandleHasControl = scaleHandleGainedControl = true;
                    break;
                case EventType.MouseUp:
                    if (Event.current.button == 0 && scaleHandleHasControl)
                        scaleHandleHasControl = false;
                    break;
                case EventType.Layout:
                    scaleHandleNear = HandleUtility.nearestControl != ncOld;
                    break;
            }
            return scale;
        }
    }

}
#endif
