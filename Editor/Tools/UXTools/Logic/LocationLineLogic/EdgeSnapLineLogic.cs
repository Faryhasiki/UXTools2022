#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEx;

namespace UITool
{
    /// <summary>
    /// 处理边缘吸附
    /// </summary>
    public class EdgeSnapLineLogic : UXSingleton<EdgeSnapLineLogic>
    {
        private VisualLineManager m_VisualManager;
        private GameObject m_SelectedObject;
        private List<Rect> m_Rects;

        private struct Rect
        {
            /// <summary>
            /// 长度为6，上中下左中右
            /// </summary>
            public float[] pos;

            public Rect(RectTransform trans)
            {
                pos = new float[6];
                pos[0] = trans.GetTopWorldPosition();
                pos[2] = trans.GetBottomWorldPosition();
                pos[3] = trans.GetLeftWorldPosition();
                pos[5] = trans.GetRightWorldPosition();
                pos[4] = trans.position.x;
                pos[1] = trans.position.y;
            }
        }

        /// <summary>
        /// 判断两个世界坐标是否近似相等，避免浮点误差导致提示线丢失。
        /// </summary>
        /// <param name="a">坐标A。</param>
        /// <param name="b">坐标B。</param>
        /// <returns>在允许误差内返回 true。</returns>
        private static bool IsNearlyEqual(float a, float b)
        {
            return Mathf.Abs(a - b) <= SnapLogic.SnapEpsDistance;
        }

        public override void Init()
        {
            m_VisualManager = new VisualLineManager();
            m_VisualManager.Init();
            m_Rects = new List<Rect>();

            ResetAll();

            EditorApplication.hierarchyChanged += ResetAll;
            Selection.selectionChanged += ResetAll;
            EditorApplication.update += ListenMoving;
        }
        public void InitAfter()
        {
            EditorApplication.update += SnapToFinalPos;
        }

        public void CloseBefore()
        {
            EditorApplication.update -= SnapToFinalPos;
        }
        public override void Close()
        {
            EditorApplication.hierarchyChanged -= ResetAll;
            Selection.selectionChanged -= ResetAll;
            EditorApplication.update -= ListenMoving;

            m_VisualManager?.Close();
            m_VisualManager = null;
            Instance.Release();
        }

        private void ResetAll()
        {
            m_VisualManager.RemoveAll();
            if (Selection.gameObjects.Length == 1 && EditorLogic.ObjectFit(Selection.activeGameObject))
            {
                m_SelectedObject = Selection.activeGameObject;
                m_SelectedObject.transform.hasChanged = false;
            }
            else
            {
                m_SelectedObject = null;
                return;
            }

            m_Rects.Clear();
            RectTransform[] allObjects;
            var prefabStage = PrefabStageUtils.GetCurrentPrefabStage();
            if (prefabStage != null)
            {
                allObjects = prefabStage.prefabContentsRoot.GetComponentsInChildren<RectTransform>();
            }
            else
            {
#if UNITY_2023_1_OR_NEWER || UNITY_6000_0_OR_NEWER
                allObjects = UnityEngine.Object.FindObjectsByType<RectTransform>(FindObjectsSortMode.None);
#else
                allObjects = UnityEngine.Object.FindObjectsOfType<RectTransform>();
#endif
            }
            foreach (RectTransform item in allObjects)
            {
                if (EditorLogic.ObjectFit(item.gameObject) && item.gameObject != m_SelectedObject && !item.IsChildOf(m_SelectedObject.transform))
                {
                    m_Rects.Add(new Rect(item));
                }
            }
        }

        /// <summary>
        /// 核心逻辑。
        /// </summary>
        /// <param name="eps">eps=0 表示吸附最终位置（需要画提示线）；非 0 表示检测阶段。</param>
        private void FindEdges(float eps)
        {
            if (eps == 0)
            {
                m_VisualManager.RemoveAll();
            }

            var cam = SceneView.lastActiveSceneView?.camera;
            Rect objRect = new Rect(m_SelectedObject.GetComponent<RectTransform>());

            foreach (Rect rect in m_Rects)
            {
                if (eps != 0)
                {
                    // 垂直方向（上/中/下 Y 轴对齐）：在屏幕像素空间比较，兼容所有 Canvas 模式
                    float bestWorldDisVert = Mathf.Infinity;
                    float bestScreenDisVert = Mathf.Infinity;
                    for (int i = 0; i < 3; i++)
                    {
                        for (int j = 0; j < 3; j++)
                        {
                            float wd = rect.pos[i] - objRect.pos[j];
                            if (Mathf.Abs(wd) < Mathf.Abs(bestWorldDisVert))
                            {
                                bestWorldDisVert = wd;
                                if (cam != null)
                                {
                                    float py1 = cam.WorldToScreenPoint(new Vector3(0f, rect.pos[i], 0f)).y;
                                    float py2 = cam.WorldToScreenPoint(new Vector3(0f, objRect.pos[j], 0f)).y;
                                    bestScreenDisVert = py1 - py2;
                                }
                                else
                                {
                                    bestScreenDisVert = wd;
                                }
                            }
                        }
                    }
                    float threshVert = cam != null ? SnapLogic.SnapSceneDistance : eps;
                    if (Mathf.Abs(bestScreenDisVert) < threshVert && Mathf.Abs(bestWorldDisVert) < Mathf.Abs(SnapLogic.SnapEdgeDisVert))
                    {
                        SnapLogic.SnapEdgeDisVert = bestWorldDisVert;
                    }

                    // 水平方向（左/中/右 X 轴对齐）：同上
                    float bestWorldDisHoriz = Mathf.Infinity;
                    float bestScreenDisHoriz = Mathf.Infinity;
                    for (int i = 3; i < 6; i++)
                    {
                        for (int j = 3; j < 6; j++)
                        {
                            float wd = rect.pos[i] - objRect.pos[j];
                            if (Mathf.Abs(wd) < Mathf.Abs(bestWorldDisHoriz))
                            {
                                bestWorldDisHoriz = wd;
                                if (cam != null)
                                {
                                    float px1 = cam.WorldToScreenPoint(new Vector3(rect.pos[i], 0f, 0f)).x;
                                    float px2 = cam.WorldToScreenPoint(new Vector3(objRect.pos[j], 0f, 0f)).x;
                                    bestScreenDisHoriz = px1 - px2;
                                }
                                else
                                {
                                    bestScreenDisHoriz = wd;
                                }
                            }
                        }
                    }
                    float threshHoriz = cam != null ? SnapLogic.SnapSceneDistance : eps;
                    if (Mathf.Abs(bestScreenDisHoriz) < threshHoriz && Mathf.Abs(bestWorldDisHoriz) < Mathf.Abs(SnapLogic.SnapEdgeDisHoriz))
                    {
                        SnapLogic.SnapEdgeDisHoriz = bestWorldDisHoriz;
                    }
                }
                else
                {
                    float minX = Math.Min(rect.pos[3], objRect.pos[3]);
                    float maxX = Math.Max(rect.pos[5], objRect.pos[5]);
                    for (int i = 0; i < 3; i++)
                    {
                        for (int j = 0; j < 3; j++)
                        {
                            if (IsNearlyEqual(rect.pos[i], objRect.pos[j]))
                            {
                                m_VisualManager.AddHorizLine(minX, maxX, rect.pos[i], false);
                            }
                        }
                    }
                    float minY = Math.Min(rect.pos[2], objRect.pos[2]);
                    float maxY = Math.Max(rect.pos[0], objRect.pos[0]);
                    for (int i = 3; i < 6; i++)
                    {
                        for (int j = 3; j < 6; j++)
                        {
                            if (IsNearlyEqual(rect.pos[i], objRect.pos[j]))
                            {
                                m_VisualManager.AddVertLine(rect.pos[i], minY, maxY, false);
                            }
                        }
                    }
                }
            }
        }

        private void ListenMoving()
        {
            if (m_SelectedObject != null && m_SelectedObject.GetComponent<RectTransform>().position != SnapLogic.ObjFinalPos && LocationLineLogic.Instance.EnableSnap)
            {
                SnapLogic.SnapEdgeDisHoriz = SnapLogic.SnapEdgeDisVert = Mathf.Infinity;
                FindEdges(SnapLogic.SnapWorldDistance);
            }
        }

        private void SnapToFinalPos()
        {
            if (m_SelectedObject == null) return;
            RectTransform rectTransform = m_SelectedObject.GetComponent<RectTransform>();
            if (rectTransform == null) return;
            if (rectTransform.position != SnapLogic.ObjFinalPos && LocationLineLogic.Instance.EnableSnap)
            {
                Vector3 vec = rectTransform.position;
                if (Math.Abs(SnapLogic.SnapEdgeDisHoriz) <= Math.Abs(SnapLogic.SnapIntervalDisHoriz) &&
                Math.Abs(SnapLogic.SnapEdgeDisHoriz) < Math.Abs(SnapLogic.SnapLineDisHoriz))
                {
                    vec.x += SnapLogic.SnapEdgeDisHoriz;
                }
                if (Math.Abs(SnapLogic.SnapEdgeDisVert) <= Math.Abs(SnapLogic.SnapIntervalDisVert) &&
                Math.Abs(SnapLogic.SnapEdgeDisVert) < Math.Abs(SnapLogic.SnapLineDisVert))
                {
                    vec.y += SnapLogic.SnapEdgeDisVert;
                }
                rectTransform.position = vec;
                FindEdges(0);
            }
        }
    }
}

#endif