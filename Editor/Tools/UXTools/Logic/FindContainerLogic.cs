#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UITool
{
    public class FindContainerLogic
    {
        /// <summary>
        /// 只选中一个非 Root的节点时,拖动出来的节点应该和该节点同层级
        /// 未选中或者选中 根Canvas 节点，拖动出来的节点都在 根Canvas 子节点层级
        /// 选中多个时 拖动出来的节点在 根Canvas 子节点层级
        /// </summary>
        public static Transform GetObjectParent(GameObject[] selection)
        {
            var prefabStage = PrefabStageUtils.GetCurrentPrefabStage();
            if (prefabStage != null)
            {
                if (selection.Length == 1
                    && !selection[0].name.Equals("Canvas (Environment)")
                    && selection[0].transform != prefabStage.prefabContentsRoot.transform)
                {
                    return selection[0].transform.parent.transform;
                }
                else
                {
                    return prefabStage.prefabContentsRoot.transform;
                }
            }
            else
            {
                if (selection.Length == 1)
                {
                    if (selection[0].transform == selection[0].transform.root)
                    {
                        return selection[0].transform.root;
                    }
                    else
                    {
                        return selection[0].transform.parent.transform;
                    }
                }
                else
                {
#if UNITY_2023_1_OR_NEWER || UNITY_6000_0_OR_NEWER
                    var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
#else
                    var canvases = Object.FindObjectsOfType<Canvas>();
#endif
                    if (canvases.Length == 0)
                        CreateDefaultCanvas();

#if UNITY_2023_1_OR_NEWER || UNITY_6000_0_OR_NEWER
                    canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
#else
                    canvases = Object.FindObjectsOfType<Canvas>();
#endif
                    return canvases[0].transform;
                }
            }
        }

        /// <summary>
        /// 创建与 Unity 默认行为一致的 Canvas + EventSystem
        /// </summary>
        private static void CreateDefaultCanvas()
        {
            var canvasGo = new GameObject("Canvas");
            canvasGo.layer = LayerMask.NameToLayer("UI");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();
            Undo.RegisterCreatedObjectUndo(canvasGo, "Create Canvas");

            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                var esGo = new GameObject("EventSystem");
                esGo.AddComponent<EventSystem>();
                esGo.AddComponent<StandaloneInputModule>();
                Undo.RegisterCreatedObjectUndo(esGo, "Create EventSystem");
            }
        }
    }
}
#endif
