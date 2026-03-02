using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEx
{
    public static class RectTransformExtensions
    {
        private static readonly Vector3[] s_Corners = new Vector3[4];

        // corners[0]=bottom-left, [1]=top-left, [2]=top-right, [3]=bottom-right
        public static float GetTopWorldPosition(this RectTransform rt)
        {
            rt.GetWorldCorners(s_Corners);
            return s_Corners[1].y;
        }

        public static float GetBottomWorldPosition(this RectTransform rt)
        {
            rt.GetWorldCorners(s_Corners);
            return s_Corners[0].y;
        }

        public static float GetLeftWorldPosition(this RectTransform rt)
        {
            rt.GetWorldCorners(s_Corners);
            return s_Corners[0].x;
        }

        public static float GetRightWorldPosition(this RectTransform rt)
        {
            rt.GetWorldCorners(s_Corners);
            return s_Corners[2].x;
        }
    }

    public static class RectTransformListExtensions
    {
        public static float GetMinLeft(this List<RectTransform> rects)
        {
            return rects.Min(r => r.GetLeftWorldPosition());
        }

        public static float GetMaxRight(this List<RectTransform> rects)
        {
            return rects.Max(r => r.GetRightWorldPosition());
        }

        public static float GetMaxTop(this List<RectTransform> rects)
        {
            return rects.Max(r => r.GetTopWorldPosition());
        }

        public static float GetMinBottom(this List<RectTransform> rects)
        {
            return rects.Min(r => r.GetBottomWorldPosition());
        }
    }

    public static class TransformExtensions
    {
        public static Transform FindChildRecursive(this Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name == name)
                    return child;
                var result = child.FindChildRecursive(name);
                if (result != null)
                    return result;
            }
            return null;
        }
    }
}
