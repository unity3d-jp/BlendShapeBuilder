using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

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
        public static bool axisMoveHandleControling;
        public static bool axisMoveHandleNear;
        public static Vector3 AxisMoveHandle(Vector3 pos, Quaternion rot)
        {
            var size = HandleUtility.GetHandleSize(pos);
            var snap = 0.0f;
            var snap3 = Vector3.zero;

            var et = Event.current.type;
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
                        axisMoveHandleControling = axisMoveHandleGainedControl = true;
                    break;
                case EventType.MouseUp:
                    if (Event.current.button == 0 && axisMoveHandleControling)
                    {
                        axisMoveHandleLostControl = true;
                        axisMoveHandleControling = false;
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
        public static bool freeMoveHandleControling;
        public static bool freeMoveHandleNear;

        public static Vector3 FreeMoveHandle(Vector3 pos, float rectSize, bool forceCapture)
        {
            var size = HandleUtility.GetHandleSize(pos) * (rectSize * 0.01f);
            Vector3 snap = Vector3.one * 0.5f;

            var et = Event.current.type;
            int hcOld = GUIUtility.hotControl;
            int ncOld = HandleUtility.nearestControl;

            pos = Handles.FreeMoveHandle(freeMoveHandleCID, pos, Quaternion.identity, size, snap, Handles.RectangleHandleCap);

            // check handle has control
            freeMoveHandleGainedControl = freeMoveHandleLostControl = false;
            switch (et)
            {
                case EventType.MouseDown:
                    if (Event.current.button == 0 && GUIUtility.hotControl != hcOld)
                        freeMoveHandleControling = freeMoveHandleGainedControl = true;
                    break;
                case EventType.MouseUp:
                    if (Event.current.button == 0 && freeMoveHandleControling)
                    {
                        freeMoveHandleLostControl = true;
                        freeMoveHandleControling = false;
                    }
                    break;
                case EventType.Layout:
                    freeMoveHandleNear = HandleUtility.nearestControl != ncOld;
                    break;
            }
            return pos;
        }


        public static bool rotationHandleGainedControl;
        public static bool rotationHandleLostControl;
        public static bool rotationHandleControling;
        public static bool rotationHandleNear;
        public static Quaternion RotationHandle(Quaternion rot, Vector3 pos)
        {
            var et = Event.current.type;
            int hcOld = GUIUtility.hotControl;
            int ncOld = HandleUtility.nearestControl;

            rot = Handles.RotationHandle(rot, pos);

            // check handle has control
            rotationHandleGainedControl = rotationHandleLostControl = false;
            switch (et)
            {
                case EventType.MouseDown:
                    if (Event.current.button == 0 && GUIUtility.hotControl != hcOld)
                        rotationHandleControling = rotationHandleGainedControl = true;
                    break;
                case EventType.MouseUp:
                    if (Event.current.button == 0 && rotationHandleControling)
                    {
                        rotationHandleLostControl = true;
                        rotationHandleControling = false;
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
        public static bool scaleHandleControling;
        public static bool scaleHandleNear;
        public static Vector3 ScaleHandle(Vector3 scale, Vector3 pos, Quaternion rot)
        {
            var et = Event.current.type;
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
                        scaleHandleControling = scaleHandleGainedControl = true;
                    break;
                case EventType.MouseUp:
                    if (Event.current.button == 0 && scaleHandleControling)
                    {
                        scaleHandleLostControl = true;
                        scaleHandleControling = false;
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
