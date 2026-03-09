#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.IO;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using UnityEx;
using System.Linq;
#if UNITY_2021_3_OR_NEWER
using PrefabStageUtility = UnityEditor.SceneManagement.PrefabStageUtility;
using PrefabStage = UnityEditor.SceneManagement.PrefabStage;
#else
using PrefabStageUtility = UnityEditor.Experimental.SceneManagement.PrefabStageUtility;
using PrefabStage = UnityEditor.Experimental.SceneManagement.PrefabStage;
#endif

namespace UITool
{

    public enum CreateLineType
    {
        Both,
        Vertical,
        Horizon
    }

    /// <summary>
    /// 辅助线吸附的相关逻辑
    /// </summary>
    public class LocationLineLogic : UXSingleton<LocationLineLogic>
    {
        //所有的辅助线数据
        private LocationLinesData m_LinesData;
        //所有辅助线对象 应该和上面的数据是一致的
        private List<LocationLine> m_LinesList;

        private GameObject m_SelectedObject;

        public bool EnableSnap = false;
        public static float sceneviewOffset;
        public override void Init()
        {
            sceneviewOffset = Utils.GetSceneViewOffest();
            m_LinesList = new List<LocationLine>();
            EditorApplication.update += Opened;

            EditorApplication.update += Judge3D;
            EditorApplication.update += UpdateLinesScreenViewPos;
            SceneView.duringSceneGui += OnSceneGUI;
            if (SwitchSetting.CheckValid(SwitchSetting.SwitchType.AlignSnap))
            {
                EditorApplication.update += SnapToLocationLine;
            }

            Undo.undoRedoPerformed += OnUndoRedoPerformed;

            EditorSceneManager.sceneSaved += SaveLocationLines;
            Selection.selectionChanged += OnSelectionChanged;


            PrefabStage.prefabStageOpened += PrefabStageChange;

            PrefabStage.prefabStageClosing += PrefabStageChange;

            OnSelectionChanged();
            LoadLocationLines();
        }

        private void PrefabStageChange(PrefabStage p)
        {
            EditorApplication.delayCall += () =>
            {
                sceneviewOffset = Utils.GetSceneViewOffest();
                UpdateLinesSize();
            };
        }

        public void InitAfter()
        {
            EditorApplication.update += SnapToFinalPos;
        }

        public void CloseBefore()
        {
            EditorApplication.update -= SnapToFinalPos;
        }
        public void Opened()
        {
            if (m_LinesData == null)
            {
                m_LinesData = JsonAssetManager.GetAssets<LocationLinesData>();
            }
        }

        public override void Close()
        {
            EditorApplication.update -= Judge3D;
            EditorApplication.update -= UpdateLinesScreenViewPos;
            SceneView.duringSceneGui -= OnSceneGUI;
            EditorApplication.update -= SnapToLocationLine;
            EditorSceneManager.sceneSaved -= SaveLocationLines;
            Selection.selectionChanged -= OnSelectionChanged;

            Undo.undoRedoPerformed -= OnUndoRedoPerformed;

            PrefabStage.prefabStageOpened -= PrefabStageChange;

            PrefabStage.prefabStageClosing -= PrefabStageChange;
            EditorApplication.update -= Opened;

            if (m_LinesList != null)
            {
                foreach (var line in m_LinesList)
                {
                    if (SceneView.lastActiveSceneView.rootVisualElement.Contains(line))
                    {
                        SceneView.lastActiveSceneView.rootVisualElement.Remove(line);
                    }
                }
                m_LinesList = null;
            }
            Instance.Release();
        }

        public void RemoveLine(LocationLine line)
        {
            LocationLineCommand cmd = new LocationLineCommand(m_LinesData, "Remove LocationLine");
            cmd.Execute();

            if (m_LinesList.Contains(line))
            {
                if (SceneView.lastActiveSceneView.rootVisualElement.Contains(line))
                {
                    SceneView.lastActiveSceneView.rootVisualElement.Remove(line);
                }
                m_LinesList.Remove(line);
                m_LinesData.Remove(line.id);
            }
        }

        public void ModifyLine(LocationLine line)
        {
            LocationLineCommand cmd = new LocationLineCommand(m_LinesData, "Modify LocationLine");
            cmd.Execute();

            LocationLineData lineData = new LocationLineData()
            {
                Id = line.id,
                Horizontal = line.direction == LocationLineDirection.Horizontal,
                Pos = line.direction == LocationLineDirection.Horizontal ? line.worldPostion.y : line.worldPostion.x,
            };
            m_LinesData.Modify(lineData);
        }

        private void ClearAllLine()
        {
            foreach (var line in m_LinesList)
            {
                SceneView.lastActiveSceneView.rootVisualElement.Remove(line);
            }
            m_LinesList.Clear();
        }

        /// <summary>
        /// 清除所有辅助线（UI + 持久化数据）
        /// </summary>
        public void RemoveAllLines()
        {
            LocationLineCommand cmd = new LocationLineCommand(m_LinesData, "Remove All LocationLines");
            cmd.Execute();

            ClearAllLine();
            m_LinesData.Clear();
            m_LinesData.Save();
        }

        public void UpdateLinesSize()
        {
            float top = Utils.GetSceneViewToolbarHeight();
            foreach (LocationLine line in m_LinesList)
            {
                if (line.direction == LocationLineDirection.Vertical)
                {
                    line.style.top = top;
                }
            }
        }

        public void UpdateLinesScreenViewPos()
        {
            SceneView sceneView = SceneView.lastActiveSceneView;
            if (sceneView == null) return;

            var h = sceneView.camera.pixelHeight / EditorGUIUtility.pixelsPerPoint;
            foreach (LocationLine line in m_LinesList)
            {
                line.UpdateLineScreenViewPos(sceneView);
                if (line.style.bottom.value.value > h - 26)
                {
                    line.style.visibility = Visibility.Hidden;
                }
                else
                {
                    line.style.visibility = Visibility.Visible;
                }
            }
        }

        private void PlaceLinesToSceneView(List<LocationLine> lines)
        {
            SceneView sceneView = SceneView.lastActiveSceneView;
            if (sceneView == null) return;
#if UNITY_2020_3_OR_NEWER
            VisualElement firstChild = sceneView.rootVisualElement.Children().First();
            foreach (LocationLine line in lines)
            {
                sceneView.rootVisualElement.Add(line);
                line.PlaceInFront(firstChild);
                m_LinesList.Add(line);
            }
#else
            VisualElement firstChild = sceneView.rootVisualElement;
            foreach (LocationLine line in lines)
            {
                sceneView.rootVisualElement.Add(line);
                line.SendToBack();
                m_LinesList.Add(line);
            }
#endif

        }

        /// <summary>
        /// 如果场景是在3D模式下，删除所有辅助线
        /// </summary>
        private void Judge3D()
        {
            if (SceneView.lastActiveSceneView == null) return;

            if (!SceneView.lastActiveSceneView.in2DMode)
            {
                foreach (var line in m_LinesList)
                {
                    line.style.visibility = Visibility.Hidden;
                }
            }
            else
            {
                foreach (var line in m_LinesList)
                {
                    line.style.visibility = Visibility.Visible;
                }
            }
        }

        /// <summary>
        /// 辅助线吸附逻辑。在屏幕像素空间做距离判断，兼容所有 Canvas 渲染模式（Overlay / Camera / WorldSpace），
        /// 且不受 SceneView 缩放级别影响。
        /// </summary>
        private void SnapToLocationLine()
        {
            if (m_SelectedObject == null || !EnableSnap) return;
            RectTransform rectTransform = m_SelectedObject.GetComponent<RectTransform>();
            if (rectTransform == null) return;
            if (rectTransform.position == SnapLogic.ObjFinalPos) return;

            var cam = SceneView.lastActiveSceneView?.camera;
            if (cam == null) return;

            SnapLogic.SnapLineDisVert = SnapLogic.SnapLineDisHoriz = Mathf.Infinity;

            float leftEdgeWorld   = rectTransform.GetLeftWorldPosition();
            float rightEdgeWorld  = rectTransform.GetRightWorldPosition();
            float bottomEdgeWorld = rectTransform.GetBottomWorldPosition();
            float topEdgeWorld    = rectTransform.GetTopWorldPosition();

            // 将元素四条边转换到 SceneView 屏幕像素空间
            float leftPx   = cam.WorldToScreenPoint(new Vector3(leftEdgeWorld,   0f, 0f)).x;
            float rightPx  = cam.WorldToScreenPoint(new Vector3(rightEdgeWorld,  0f, 0f)).x;
            float bottomPx = cam.WorldToScreenPoint(new Vector3(0f, bottomEdgeWorld, 0f)).y;
            float topPx    = cam.WorldToScreenPoint(new Vector3(0f, topEdgeWorld,    0f)).y;

            // 记录最近的屏幕像素距离及对应的世界坐标偏移
            Vector2 minDisPx = new Vector2(Mathf.Infinity, Mathf.Infinity);
            Vector2 bestWorldDis = new Vector2(Mathf.Infinity, Mathf.Infinity);

            foreach (var line in m_LinesList)
            {
                // 竖直辅助线 → 对齐左/右边
                if (line.direction == LocationLineDirection.Vertical)
                {
                    float linePx = cam.WorldToScreenPoint(new Vector3(line.worldPostion.x, 0f, 0f)).x;
                    float d1Px = linePx - leftPx;
                    float d2Px = linePx - rightPx;
                    bool useLeft = Mathf.Abs(d1Px) < Mathf.Abs(d2Px);
                    float minPx = useLeft ? d1Px : d2Px;
                    float worldDis = useLeft
                        ? (line.worldPostion.x - leftEdgeWorld)
                        : (line.worldPostion.x - rightEdgeWorld);

                    if (Mathf.Abs(minPx) < Mathf.Abs(minDisPx.x))
                    {
                        minDisPx.x = minPx;
                        bestWorldDis.x = worldDis;
                    }
                }

                // 水平辅助线 → 对齐上/下边
                if (line.direction == LocationLineDirection.Horizontal)
                {
                    float linePx = cam.WorldToScreenPoint(new Vector3(0f, line.worldPostion.y, 0f)).y;
                    float d1Px = linePx - bottomPx;
                    float d2Px = linePx - topPx;
                    bool useBottom = Mathf.Abs(d1Px) < Mathf.Abs(d2Px);
                    float minPx = useBottom ? d1Px : d2Px;
                    float worldDis = useBottom
                        ? (line.worldPostion.y - bottomEdgeWorld)
                        : (line.worldPostion.y - topEdgeWorld);

                    if (Mathf.Abs(minPx) < Mathf.Abs(minDisPx.y))
                    {
                        minDisPx.y = minPx;
                        bestWorldDis.y = worldDis;
                    }
                }
            }

            // 阈值统一用屏幕像素（SnapSceneDistance），与 Canvas 模式和缩放无关
            if (Mathf.Abs(minDisPx.x) < SnapLogic.SnapSceneDistance)
                SnapLogic.SnapLineDisHoriz = bestWorldDis.x;
            if (Mathf.Abs(minDisPx.y) < SnapLogic.SnapSceneDistance)
                SnapLogic.SnapLineDisVert = bestWorldDis.y;
        }

        /// <summary>
        /// 将物体吸附到最终位置
        /// </summary>
        public void SnapToFinalPos()
        {
            if (m_SelectedObject != null && m_SelectedObject.GetComponent<RectTransform>().position != SnapLogic.ObjFinalPos && EnableSnap)
            {
                RectTransform rectTransform = m_SelectedObject.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    Vector3 vec = rectTransform.transform.position;
                    if (SnapLogic.SnapLineDisHoriz != Mathf.Infinity)
                    {
                        if (Math.Abs(SnapLogic.SnapLineDisHoriz) <= Math.Abs(SnapLogic.SnapEdgeDisHoriz) &&
                        Math.Abs(SnapLogic.SnapLineDisHoriz) <= Math.Abs(SnapLogic.SnapIntervalDisHoriz))
                        {
                            vec.x += SnapLogic.SnapLineDisHoriz;
                        }
                    }
                    if (SnapLogic.SnapLineDisVert != Mathf.Infinity)
                    {
                        if (Math.Abs(SnapLogic.SnapLineDisVert) <= Math.Abs(SnapLogic.SnapEdgeDisVert) &&
                        Math.Abs(SnapLogic.SnapLineDisVert) <= Math.Abs(SnapLogic.SnapIntervalDisVert))
                        {
                            vec.y += SnapLogic.SnapLineDisVert;
                        }
                    }

                    rectTransform.transform.position = vec;
                }
            }
        }

        /// <summary>
        /// 按钮事件,创建一组横竖辅助线
        /// </summary>
        public void CreateLocationLine(CreateLineType createType)
        {
            LocationLineCommand cmd = new LocationLineCommand(m_LinesData, "Add LocationLine");
            cmd.Execute();

            SceneView sceneView = SceneView.lastActiveSceneView;
            Vector3 worldPostion = sceneView.camera.ScreenToWorldPoint(new Vector3(sceneView.camera.pixelWidth / 2, (sceneView.camera.pixelHeight - 40) / 2, 0));

            int curId = m_LinesData.LastLineId + 1;
            LocationLineData horzLineData = null;
            LocationLineData vertLineData = null;
            LocationLine horzLine = null;
            LocationLine vertLine = null;

            if (createType == CreateLineType.Both || createType == CreateLineType.Horizon)
            {
                horzLineData = new LocationLineData()
                {
                    Id = curId,
                    Horizontal = true,
                    Pos = worldPostion.y
                };
                horzLine = new HorizontalLocationLine
                {
                    id = horzLineData.Id,
                    worldPostion = new Vector3(0, horzLineData.Pos, 0)
                };
            }

            if (createType == CreateLineType.Both || createType == CreateLineType.Vertical)
            {
                vertLineData = new LocationLineData()
                {
                    Id = curId + 1,
                    Horizontal = false,
                    Pos = worldPostion.x,
                };
                vertLine = new VerticalLocationLine
                {
                    id = vertLineData.Id,
                    worldPostion = new Vector3(vertLineData.Pos, 0, 0)
                };
            }

            if (createType == CreateLineType.Both)
            {
                m_LinesData.Add(horzLineData);
                m_LinesData.Add(vertLineData);
                PlaceLinesToSceneView(new List<LocationLine> { horzLine, vertLine });
            }
            else if (createType == CreateLineType.Horizon)
            {
                m_LinesData.Add(horzLineData);
                PlaceLinesToSceneView(new List<LocationLine> { horzLine });
            }
            else if (createType == CreateLineType.Vertical)
            {
                m_LinesData.Add(vertLineData);
                PlaceLinesToSceneView(new List<LocationLine> { vertLine });
            }
        }

        /// <summary>
        /// 打开UXToolsEditor的时候从序列化数据中加载之前保存的辅助线数据
        /// </summary>
        private void LoadLocationLines()
        {
            if (!Directory.Exists(UIToolConfig.ProjectDataPath))
            {
                Directory.CreateDirectory(UIToolConfig.ProjectDataPath);
            }

            m_LinesData = JsonAssetManager.GetAssets<LocationLinesData>();

            RestoreLines();
        }

        private void RestoreLines()
        {
            if (m_LinesData == null) return;
            List<LocationLine> lines = new List<LocationLine>();

            foreach (LocationLineData lineData in m_LinesData.List)
            {
                LocationLine l;
                if (lineData.Horizontal)
                {
                    l = new HorizontalLocationLine
                    {
                        id = lineData.Id,
                        worldPostion = new Vector3(0, lineData.Pos, 0)
                    };
                }
                else
                {
                    l = new VerticalLocationLine
                    {
                        id = lineData.Id,
                        worldPostion = new Vector3(lineData.Pos, 0, 0)
                    };
                }
                lines.Add(l);
            }
            PlaceLinesToSceneView(lines);
        }
        /// <summary>
        /// 新建或修改辅助线之后 保存并序列化
        /// </summary>
        public void SaveLocationLines(Scene useless)
        {
            m_LinesData.Save();
        }

        private void OnSelectionChanged()
        {
            if (Selection.gameObjects.Length == 1 && EditorLogic.ObjectFit(Selection.activeGameObject))
            {
                m_SelectedObject = Selection.activeGameObject;
            }
            else
            {
                m_SelectedObject = null;
            }
        }

        private void OnUndoRedoPerformed()
        {
            ClearAllLine();
            RestoreLines();
        }

        private void OnSceneGUI(SceneView view)
        {
#if !UNITY_2020_1_OR_NEWER
            if (Event.current != null && Event.current.button == 1 && Event.current.type == EventType.MouseUp)
            {
                LocationLine line;
                if (CheckPosInLines(Event.current.mousePosition, out line))
                {
                    var menu = new GenericMenu();
                    menu.AddItem(new GUIContent(EditorLocalization.GetLocalization(EditorLocalizationStorage.Def_删除)), false, () =>
                    {
                        RemoveLine(line);
                    });
                    menu.ShowAsContext();
                    Event.current.Use();
                }
            }
#endif
        }

        public bool CheckPosInLines(Vector2 pos, out LocationLine clickLine)
        {
            if (m_LinesList == null || m_LinesList.Count == 0)
            {
                clickLine = null;
                return false;
            }

            pos.y += sceneviewOffset;
            bool inLines = false;

            foreach (var line in m_LinesList)
            {
                if (line.worldBound.Contains(pos))
                {
                    clickLine = line;
                    return true;
                }
            }
            clickLine = null;
            return inLines;
        }
    }
}
#endif