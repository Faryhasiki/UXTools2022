#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;


namespace UITool
{
    public static class WidgetGenerator
    {

        public static GameObject CreateUIObj(string name)
        {
            var obj = new GameObject(name);
            obj.layer = LayerMask.NameToLayer("UI");
            var rectTransform = obj.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(100, 100);
            obj.GetComponent<RectTransform>().localPosition = new Vector3(0, 0, 0);
            return obj;
        }

        public static GameObject CreateUIObj(string name, Vector3 pos, Vector3 size, GameObject[] selection)
        {
            var obj = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(obj, "");
            obj.layer = LayerMask.NameToLayer("UI");
            Transform parent = FindContainerLogic.GetObjectParent(selection);
            Undo.SetTransformParent(obj.transform, parent, "");
            obj.transform.SetParent(parent);
            var rectTransform = Undo.AddComponent<RectTransform>(obj);
            rectTransform.sizeDelta = size;
            obj.transform.localPosition = pos;
            obj.transform.localScale = Vector3.one;
            Undo.SetCurrentGroupName("Create " + name);
            return obj;
        }

    }
}
#endif