#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Reflection;
using System;
using System.IO;
using Object = UnityEngine.Object;
using System.Linq;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#if UNITY_2021_3_OR_NEWER
using PrefabStageUtility = UnityEditor.SceneManagement.PrefabStageUtility;
#else
using PrefabStageUtility = UnityEditor.Experimental.SceneManagement.PrefabStageUtility;
#endif

namespace UITool
{
    public static partial class Utils
    {
#if !UNITY_2021_3_OR_NEWER
        private static readonly float m_WindowToolbarHeight = 21f;
        private static readonly float m_StageHandlingFixedHeight = 25f;
        private static readonly float m_TabHeight = 19f;
#endif

        #region Reflection
        //反射方法 性能消耗较大 调用后尽量缓存结果
        /// <summary>
        /// 有可能引起window重新生成实例 暂时废弃
        /// </summary>
        /// <returns></returns>
        [Obsolete]
        public static EditorWindow GetGameView()
        {
            var type = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
            if (type == null) return null;
            var gameview = EditorWindow.GetWindow(type);
            return gameview;
        }
        /// <summary>
        /// 获取GameView对象
        /// </summary>
        /// <returns></returns>
        public static object GetMainPlayModeView()
        {
            var playModeViewType = System.Type.GetType("UnityEditor.PlayModeView,UnityEditor");
            if (playModeViewType == null)
            {
                Debug.LogWarning("[UXTools] GetMainPlayModeView: UnityEditor.PlayModeView type not found (Unity internals may have changed).");
                return null;
            }
            var method = playModeViewType.GetMethod("GetMainPlayModeView", BindingFlags.NonPublic | BindingFlags.Static);
            if (method == null)
            {
                Debug.LogWarning("[UXTools] GetMainPlayModeView: method not found (Unity internals may have changed).");
                return null;
            }
            return method.Invoke(null, null);
        }
        /// <summary>
        /// 获取所有的GameView对象
        /// </summary>
        /// <returns></returns>
        public static Object[] GetPlayViews()
        {
            Assembly assembly = typeof(EditorWindow).Assembly;
            Type type = assembly.GetType("UnityEditor.GameView");
            if (type == null)
            {
#if UNITY_6000_0_OR_NEWER
                type = assembly.GetType("UnityEditor.PlayModeView");
#endif
            }
            if (type == null) return new Object[0];
            return UnityEngine.Resources.FindObjectsOfTypeAll(type);
        }

        public static EditorWindow GetHierarchyWindow()
        {
            var HierarchyViewType = System.Type.GetType("UnityEditor.SceneHierarchyWindow,UnityEditor");
            if (HierarchyViewType == null)
            {
                Debug.LogWarning("[UXTools] GetHierarchyWindow: UnityEditor.SceneHierarchyWindow type not found.");
                return null;
            }

            try
            {
                object hierarchyWindow = GetPropertyValue(HierarchyViewType, "lastInteractedHierarchyWindow");
                return hierarchyWindow as EditorWindow;
            }
            catch (Exception)
            {
                // lastInteractedHierarchyWindow 属性在 Unity 6 中可能已改名，降级为直接获取已打开窗口
                try
                {
                    return EditorWindow.GetWindow(HierarchyViewType, false, null, false);
                }
                catch (Exception e2)
                {
                    Debug.LogWarning($"[UXTools] GetHierarchyWindow fallback failed: {e2.Message}");
                    return null;
                }
            }
        }

        public static SceneView GetSceneView()
        {
            return SceneView.currentDrawingSceneView;
        }

        public static EditorWindow GetEditorWindow(string EditorWindowClassName)
        {
            var assembly = typeof(EditorWindow).Assembly;
            var type = assembly.GetType(EditorWindowClassName);
            if (type == null)
            {
                Debug.LogWarning($"[UXTools] GetEditorWindow: type '{EditorWindowClassName}' not found (Unity internals may have changed).");
                return null;
            }
            return EditorWindow.GetWindow(type);
        }

        public static Editor GetEditor(Object[] targets, string EditorClassName)
        {
            var assembly = typeof(Editor).Assembly;
            var type = assembly.GetType(EditorClassName);
            if (type == null)
            {
                Debug.LogWarning($"[UXTools] GetEditor: type '{EditorClassName}' not found (Unity internals may have changed).");
                return null;
            }
            return Editor.CreateEditor(targets, type);
        }

        public static Editor GetEditor(Object target, string EditorClassName)
        {
            var assembly = typeof(Editor).Assembly;
            var type = assembly.GetType(EditorClassName);
            if (type == null)
            {
                Debug.LogWarning($"[UXTools] GetEditor: type '{EditorClassName}' not found (Unity internals may have changed).");
                return null;
            }
            return Editor.CreateEditor(target, type);
        }

        /// <summary>
        /// 获取Editor中的类方法
        /// </summary>
        /// <param name="editorClassType"></param>
        /// <param name="methodName"></param>
        /// <param name="paraCount">要获取的method的参数个数,用于区分重载方法</param>
        /// <returns></returns>
        public static List<MethodInfo> GetEditorMethod(Type editorClassType, string methodName, int paraCount = -1)
        {
            List<MethodInfo> m = new List<MethodInfo>();

            MethodInfo[] methods = editorClassType.GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
            foreach (var method in methods)
            {
                if (method.Name == methodName && method.GetParameters().Length == paraCount)
                {
                    m.Add(method);
                }
            }
            return m;
        }
        /// <summary>
        /// 获取没有重载的Editor方法
        /// </summary>
        /// <param name="editorClassType"></param>
        /// <param name="methodName"></param>
        /// <returns></returns>
        public static MethodInfo GetEditorMethod(Type editorClassType, string methodName)
        {
            MethodInfo[] methods = editorClassType.GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
            foreach (var method in methods)
            {
                if (method.Name == methodName)
                {
                    return method;
                }
            }
            return null;
        }

        /// <summary>
        /// 通过反射调用非静态方法
        /// </summary>
        /// <param name="obj">调用对象，若调用静态函数请使用typeof(className)</param>
        /// <param name="methodName">方法名称</param>
        /// <param name="parameters">方法的参数，默认为null，表示不传参</param>
        /// <returns>非静态方法的返回值</returns>
        public static object InvokeMethod(object obj, string methodName, object[] parameters = null)
        {
            if (parameters == null)
            {
                var method = obj.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                if (method == null) { Debug.LogWarning($"[UXTools] InvokeMethod: instance method '{methodName}' not found on {obj.GetType().Name}."); return null; }
                return method.Invoke(obj, null);
            }
            Type[] types = new Type[parameters.Length];
            for (int i = 0; i < parameters.Length; i++) types[i] = parameters[i].GetType();
            var methodWithParams = obj.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, null, types, null);
            if (methodWithParams == null) { Debug.LogWarning($"[UXTools] InvokeMethod: instance method '{methodName}' with {parameters.Length} params not found on {obj.GetType().Name}."); return null; }
            return methodWithParams.Invoke(obj, parameters);
        }
        /// <summary>
        /// 通过反射调用静态方法
        /// </summary>
        /// <param name="type">调用静态函数请使用typeof(className)</param>
        /// <param name="methodName">方法名称</param>
        /// <param name="parameters">方法的参数，默认为null，表示不传参</param>
        /// <returns>静态方法的返回值</returns>
        public static object InvokeMethod(Type type, string methodName, object[] parameters = null)
        {
            if (parameters == null)
            {
                var method = type.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
                if (method == null) { Debug.LogWarning($"[UXTools] InvokeMethod: static method '{methodName}' not found on {type.Name}."); return null; }
                return method.Invoke(null, null);
            }
            Type[] types = new Type[parameters.Length];
            for (int i = 0; i < parameters.Length; i++) types[i] = parameters[i].GetType();
            var methodWithParams = type.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static, null, types, null);
            if (methodWithParams == null) { Debug.LogWarning($"[UXTools] InvokeMethod: static method '{methodName}' with {parameters.Length} params not found on {type.Name}."); return null; }
            return methodWithParams.Invoke(null, parameters);
        }

        /// <summary>
        /// 通过反射获取非静态属性的值
        /// </summary>
        /// <param name="obj">调用对象，若调用静态属性请使用typeof(className)</param>
        /// <param name="propertyName">属性名称</param>
        /// <param name="index">索引属性的参数，默认为null，表示获取非索引属性</param>
        /// <returns>非静态属性的值</returns>
        public static object GetPropertyValue(object obj, string propertyName, object[] index = null)
        {
            if (index == null)
            {
                var prop = obj.GetType().GetProperty(propertyName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                if (prop == null) { Debug.LogWarning($"[UXTools] GetPropertyValue: instance property '{propertyName}' not found on {obj.GetType().Name}."); return null; }
                return prop.GetValue(obj);
            }
            Type[] types = new Type[index.Length];
            for (int i = 0; i < index.Length; i++) types[i] = index[i].GetType();
            var propIndexed = obj.GetType().GetProperty(propertyName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, null, null, types, null);
            if (propIndexed == null) { Debug.LogWarning($"[UXTools] GetPropertyValue: indexed instance property '{propertyName}' not found on {obj.GetType().Name}."); return null; }
            return propIndexed.GetValue(obj, index);
        }
        /// <summary>
        /// 通过反射获取静态属性的值
        /// </summary>
        /// <param name="obj">调用静态属性请使用typeof(className)</param>
        /// <param name="propertyName">属性名称</param>
        /// <param name="index">索引属性的参数，默认为null，表示获取非索引属性</param>
        /// <returns>静态属性的值</returns>
        public static object GetPropertyValue(Type type, string propertyName, object[] index = null)
        {
            if (index == null)
            {
                var prop = type.GetProperty(propertyName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
                if (prop == null) { Debug.LogWarning($"[UXTools] GetPropertyValue: static property '{propertyName}' not found on {type.Name}."); return null; }
                return prop.GetValue(null);
            }
            Type[] types = new Type[index.Length];
            for (int i = 0; i < index.Length; i++) types[i] = index[i].GetType();
            var propIndexed = type.GetProperty(propertyName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static, null, null, types, null);
            if (propIndexed == null) { Debug.LogWarning($"[UXTools] GetPropertyValue: indexed static property '{propertyName}' not found on {type.Name}."); return null; }
            return propIndexed.GetValue(null, index);
        }

        /// <summary>
        /// 通过反射获取非静态变量的值
        /// </summary>
        /// <param name="obj">调用对象，若调用静态变量请使用typeof(className)</param>
        /// <param name="fieldName">变量名称</param>
        /// <returns>非静态变量的值</returns>
        public static object GetFieldValue(object obj, string fieldName)
        {
            var field = obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            if (field == null) { Debug.LogWarning($"[UXTools] GetFieldValue: instance field '{fieldName}' not found on {obj.GetType().Name}."); return null; }
            return field.GetValue(obj);
        }
        /// <summary>
        /// 通过反射获取静态变量的值
        /// </summary>
        /// <param name="obj">调用静态变量请使用typeof(className)</param>
        /// <param name="fieldName">变量名称</param>
        /// <returns>静态变量的值</returns>
        public static object GetFieldValue(Type type, string fieldName)
        {
            var field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
            if (field == null) { Debug.LogWarning($"[UXTools] GetFieldValue: static field '{fieldName}' not found on {type.Name}."); return null; }
            return field.GetValue(null);
        }

        public static Rect GetSceneViewCameraRect()
        {
            var sceneView = SceneView.lastActiveSceneView;
            if (sceneView == null) return Rect.zero;
            var type = typeof(SceneView);
            PropertyInfo info = type.GetProperty("cameraRect", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            if (info == null) return Rect.zero;
            object r = info.GetValue(sceneView, null);
            return r != null ? (Rect)r : Rect.zero;
        }

        public static float GetSceneViewOffest()
        {
#if UNITY_6000_0_OR_NEWER
            return 0;
#elif !UNITY_2021_3_OR_NEWER
            return m_TabHeight + GetSceneViewToolbarHeight();
#else
            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null)
            {
                return 19 + 46;
            }
            else
            {
                return 19 + 25;
            }
#endif
        }

        public static float GetSceneViewToolbarHeight()
        {
#if !UNITY_2021_3_OR_NEWER
            //var sceneviewtype = typeof(SceneView);
            //PropertyInfo toolbarHeightInfo = sceneviewtype.GetProperty("toolbarHeight", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            //float toolbarHeight = (float)toolbarHeightInfo.GetValue(SceneView.lastActiveSceneView, null);
            //return toolbarHeight;
            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null)
            {
                return m_WindowToolbarHeight + m_StageHandlingFixedHeight;
            }
            else
            {
                return m_WindowToolbarHeight;
            }
#else
            return 0;
#endif
        }
        #endregion

        #region GameView
        public static Vector2 GetGameViewSize()
        {
#if UNITY_6000_0_OR_NEWER
            PlayModeWindow.GetRenderingResolution(out uint w, out uint h);
            return new Vector2(w, h);
#else
            MethodInfo GetSizeOfMainGameView = GetEditorMethod(Type.GetType("UnityEditor.GameView,UnityEditor"), "GetSizeOfMainGameView");
            if (GetSizeOfMainGameView != null)
            {
                var Res = GetSizeOfMainGameView.Invoke(null, null);
                return (Vector2)Res;
            }
            return Vector2.zero;
#endif
        }
        #endregion

        #region Prefab
        public static void OpenPrefab(string prefabPath)
        {
            List<MethodInfo> m = GetEditorMethod(typeof(PrefabStageUtility), "OpenPrefab", 1);
            if (m == null || m.Count == 0)
            {
                Debug.LogWarning("[UXTools] OpenPrefab: PrefabStageUtility.OpenPrefab method not found (Unity internals may have changed).");
                return;
            }
            m[0].Invoke(null, new object[] { prefabPath });
        }

        public static void SetCursor(Texture2D texture)
        {
            try
            {
                List<MethodInfo> m = GetEditorMethod(typeof(EditorGUIUtility), "SetCurrentViewCursor", 3);
                if (m != null && m.Count > 0)
                {
                    m[0].Invoke(null, new object[] { texture, new Vector2(16, 16), MouseCursor.CustomCursor });
                }
                else
                {
                    EditorGUIUtility.AddCursorRect(new Rect(0, 0, Screen.width, Screen.height), MouseCursor.CustomCursor);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[UXTools] SetCursor failed (Unity internal API may have changed): {e.Message}");
            }
        }

        public static void ClearCurrentViewCursor()
        {
            try
            {
                List<MethodInfo> m = GetEditorMethod(typeof(EditorGUIUtility), "ClearCurrentViewCursor", 0);
                if (m != null && m.Count > 0)
                {
                    m[0].Invoke(null, null);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[UXTools] ClearCurrentViewCursor failed (Unity internal API may have changed): {e.Message}");
            }
        }

        #endregion

        #region Editor State
        public static void ExitPrefabStage()
        {
            StageUtility.GoToMainStage();
        }

        public static void EnterPlayMode()
        {
            EditorApplication.isPlaying = true;
            PlayerPrefs.SetString("previewStage", "true");
        }

        public static void StopPlayMode()
        {
            EditorApplication.isPlaying = false;
            PlayerPrefs.SetString("previewStage", "false");
        }

        #endregion

        #region Selecion
        /// <summary>
        /// 获得Hierarchy中选中且激活的对象。
        /// </summary>
        public static GameObject selectionActiveGameObject
        {
            get
            {
                if (Selection.activeGameObject == null) return null;
                return Selection.activeGameObject.activeInHierarchy ? Selection.activeGameObject : null;
            }
        }
        /// <summary>
        /// 获取当前选中对象
        /// 若选中超过1个以上的对象返回False
        /// </summary>
        /// <param name="obj">返回选中对象</param>
        /// <returns>是否成功获取</returns>
        public static bool TryGetSelectObject(out GameObject obj)
        {
            obj = null;
            if (selectionActiveGameObject == null || Selection.gameObjects.Length > 1)
                return false;

            obj = selectionActiveGameObject;
            //这里要避免选中Prefab中用于预览的Canvas
            var rect = obj.transform as RectTransform;
            bool isCanvasEnvironment = obj.transform.parent == null && obj.name == "Canvas(Environment)";
            return obj != null && !isCanvasEnvironment;
        }

        /// <summary>
        /// 获取当前选中对象的RectTransform
        /// 若选中超过1个以上的对象返回False
        /// </summary>
        /// <param name="rect">返回选中对象Rect</param>
        /// <returns>是否成功获取</returns>
        public static bool TryGetSelectionRectTransform(out RectTransform rect)
        {
            rect = null;
            if (selectionActiveGameObject == null || Selection.gameObjects.Length > 1)
                return false;
            rect = selectionActiveGameObject.transform as RectTransform;
            //这里要避免选中Prefab中用于预览的Canvas
            bool isCanvasEnvironment = rect.parent == null && rect.name == "CanvasEnvironment";
            return rect != null && !isCanvasEnvironment;
        }

        /// <summary>
        /// 获取全部选中对象RrectTransform
        /// </summary>
        /// <returns>返回列表</returns>
        public static List<RectTransform> GetAllSelectionRectTransform()
        {
            List<RectTransform> rects = new List<RectTransform>();
            GameObject[] objects = Selection.gameObjects;
            foreach (var obj in objects)
            {
                RectTransform rect = obj.GetComponent<RectTransform>();
                if (rect != null)
                    rects.Add(rect);
            }
            return rects;
        }

        /// <summary>
        /// 获取当前选中对象的path
        /// 若选中超过1个以上的对象返回False
        /// </summary>
        /// <returns>是否成功获取</returns>
        public static bool TryGetSelectionPath(out string path)
        {
            path = "";
            if (Selection.assetGUIDs.Length == 1)
            {
                path = AssetDatabase.GUIDToAssetPath(Selection.assetGUIDs[0]);
            }

            return !string.IsNullOrEmpty(path);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static List<string> GetAllSelectionPath()
        {
            return Selection.assetGUIDs.Select(AssetDatabase.GUIDToAssetPath).ToList();
        }
        #endregion

        #region Texutre&Icon
        //生成和缓存 preview图片
        static Dictionary<string, Texture> m_PreviewDict = new Dictionary<string, Texture>();
        static Dictionary<string, Texture2D> m_PreviewDict2D = new Dictionary<string, Texture2D>();

        public static Texture GetAssetsPreviewTexture(string guid, int previewSize = 79)
        {
            if (!File.Exists(AssetDatabase.GUIDToAssetPath(guid))) return null;

            if (!m_PreviewDict.TryGetValue(guid, out var tex))
            {
                tex = GenAssetsPreviewTexture(guid, previewSize);
                if (tex != null)
                {
                    m_PreviewDict[guid] = tex;
                }
            }

            if (tex == null)
            {
                tex = GenAssetsPreviewTexture(guid, previewSize);
                if (tex != null)
                    m_PreviewDict[guid] = tex;
            }
            return tex;
        }

        public static Texture UpdatePreviewTexture(string guid, int previewSize = 79)
        {
            var tex = GenAssetsPreviewTexture(guid, previewSize);
            if (tex != null)
                m_PreviewDict[guid] = tex;

            return tex;
        }

        public static Texture GetAssetsNewPreviewTexture(string guid, int previewSize = 79)
        {

            Texture tex = GenAssetsPreviewTexture(guid, previewSize);
            if (tex != null)
            {
                m_PreviewDict[guid] = tex;
            }

            return tex;
        }

        public static Texture2D GetAssetsPreviewTexture2D(string guid, int previewSize = 79)
        {

            if (!m_PreviewDict2D.TryGetValue(guid, out var tex))
            {
                Texture tex1 = GetAssetsPreviewTexture(guid, previewSize);
                if (tex1 != null)
                {
                    tex = TextureToTexture2D(tex1);
                    m_PreviewDict2D[guid] = tex;
                }
            }
            return tex;
        }
        private static Texture2D TextureToTexture2D(Texture texture)
        {
            Texture2D texture2D = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
            RenderTexture currentRT = RenderTexture.active;
            RenderTexture renderTexture = RenderTexture.GetTemporary(texture.width, texture.height, 32);
            Graphics.Blit(texture, renderTexture);

            RenderTexture.active = renderTexture;
            texture2D.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            texture2D.Apply();

            RenderTexture.active = currentRT;
            RenderTexture.ReleaseTemporary(renderTexture);

            return texture2D;
        }

        /// <summary>
        /// 生成prefab的预览图,
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="previewSize"></param>
        /// <returns></returns>
        public static Texture GenAssetsPreviewTexture(string guid, int previewSize = 79)
        {
            // if (EditorApplication.isPlaying)
            // {
            //     return null;
            // }

            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject obj = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            GameObject canvas = new GameObject("UXRenderCanvas", typeof(Canvas));
            GameObject cameraObj = new GameObject("UXRenderCamera", typeof(Camera));
            canvas.transform.position = new Vector3(10000, 10000, 10000);
            canvas.GetComponent<RectTransform>().sizeDelta = new Vector2(Screen.currentResolution.width, Screen.currentResolution.height);

            GameObject go = GameObject.Instantiate(obj, canvas.transform);

            Bounds bound = GetBounds(go);

            cameraObj.transform.position = new Vector3((bound.max.x + bound.min.x) / 2, (bound.max.y + bound.min.y) / 2, (bound.max.z + bound.min.z) / 2 - 100);
            cameraObj.transform.LookAt(cameraObj.transform.position);

            Camera camera = cameraObj.GetComponent<Camera>();
            camera.cameraType = CameraType.SceneView;
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0, 0, 0, 0f);

            float width = bound.max.x - bound.min.x;
            float height = bound.max.y - bound.min.y;
            float max_camera_size = (width > height ? width : height) + 10;
            camera.orthographicSize = max_camera_size / 2;

            RenderTexture rt = RenderTexture.GetTemporary(previewSize, previewSize, 24);
            camera.targetTexture = rt;
            camera.RenderDontRestore();

            RenderTexture tex = new RenderTexture(previewSize, previewSize, 0, RenderTextureFormat.Default);
            Graphics.Blit(rt, tex);

            //Texture2D tex = new Texture2D(previewSize, previewSize, TextureFormat.ARGB32, false);
            //tex.ReadPixels(new Rect(0, 0, previewSize, previewSize), 0, 0);
            //tex.Apply();

            RenderTexture.active = null;
            camera.targetTexture = null;
            rt.Release();
            RenderTexture.ReleaseTemporary(rt);

            Object.DestroyImmediate(canvas);
            Object.DestroyImmediate(cameraObj);

            return tex;
        }

        public static Bounds GetBounds(GameObject obj)
        {
            Vector3 Min = new Vector3(99999, 99999, 99999);
            Vector3 Max = new Vector3(-99999, -99999, -99999);
            MeshRenderer[] renders = obj.GetComponentsInChildren<MeshRenderer>();
            if (renders.Length > 0)
            {
                for (int i = 0; i < renders.Length; i++)
                {
                    if (renders[i].bounds.min.x < Min.x)
                        Min.x = renders[i].bounds.min.x;
                    if (renders[i].bounds.min.y < Min.y)
                        Min.y = renders[i].bounds.min.y;
                    if (renders[i].bounds.min.z < Min.z)
                        Min.z = renders[i].bounds.min.z;

                    if (renders[i].bounds.max.x > Max.x)
                        Max.x = renders[i].bounds.max.x;
                    if (renders[i].bounds.max.y > Max.y)
                        Max.y = renders[i].bounds.max.y;
                    if (renders[i].bounds.max.z > Max.z)
                        Max.z = renders[i].bounds.max.z;
                }
            }
            else
            {
                RectTransform[] rectTrans = obj.GetComponentsInChildren<RectTransform>();
                Vector3[] corner = new Vector3[4];
                for (int i = 0; i < rectTrans.Length; i++)
                {
                    //获取节点的四个角的世界坐标，分别按顺序为左下左上，右上右下
                    rectTrans[i].GetWorldCorners(corner);
                    if (corner[0].x < Min.x)
                        Min.x = corner[0].x;
                    if (corner[0].y < Min.y)
                        Min.y = corner[0].y;
                    if (corner[0].z < Min.z)
                        Min.z = corner[0].z;

                    if (corner[2].x > Max.x)
                        Max.x = corner[2].x;
                    if (corner[2].y > Max.y)
                        Max.y = corner[2].y;
                    if (corner[2].z > Max.z)
                        Max.z = corner[2].z;
                }
            }

            Vector3 center = (Min + Max) / 2;
            Vector3 size = new Vector3(Max.x - Min.x, Max.y - Min.y, Max.z - Min.z);
            return new Bounds(center, size);
        }

        public static void DrawGreenRect(int instanceID, Rect selectionRect, string text)
        {
            GameObject go = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
            Rect rect = new Rect(selectionRect)
            {
                width = selectionRect.width + (PrefabUtility.IsAnyPrefabInstanceRoot(go) ? 0 : 20)
            };
            EditorGUI.DrawRect(rect, new Color(0.157f, 0.157f, 0.157f, 1f));
            GUI.Label(selectionRect, PrefabUtility.GetIconForGameObject(go));
            GUI.Label(new Rect(selectionRect) { x = selectionRect.x + 20 },
                    text, new GUIStyle() { normal = { textColor = Color.green } });
        }

        #endregion

        #region Panel
        public static string SelectFolder(bool needUnderAssets = true)
        {
            string folderPath = PlayerPrefs.GetString("LastParticleCheckPath");
            string path = EditorUtility.OpenFolderPanel(EditorLocalization.GetLocalization(EditorLocalizationStorage.Def_选择路径), folderPath, "");
            if (path != "")
            {
                int index = path.IndexOf("Assets");
                if (index != -1 || !needUnderAssets)
                {
                    PlayerPrefs.SetString("LastParticleCheckPath", path);
                    if (needUnderAssets)
                    {
                        path = path.Substring(index);
                    }
                    return path + "/";
                }
                else
                {
                    EditorUtility.DisplayDialog("messageBox",
                            EditorLocalization.GetLocalization(EditorLocalizationStorage.Def_目录不在Assets下Tip),
                            EditorLocalization.GetLocalization(EditorLocalizationStorage.Def_确定));
                }
            }
            return null;
        }

        public static string SelectFile()
        {
            string filePath = PlayerPrefs.GetString("LastParticleCheckPath");
            string path = EditorUtility.OpenFilePanel(EditorLocalization.GetLocalization(EditorLocalizationStorage.Def_选择文件), filePath, "");
            if (path != "")
            {
                int index = path.IndexOf("Assets");
                if (index != -1)
                {
                    PlayerPrefs.SetString("LastParticleCheckPath", path);
                    return path.Substring(index);
                }
                else
                {
                    EditorUtility.DisplayDialog("messageBox",
                        EditorLocalization.GetLocalization(EditorLocalizationStorage.Def_目录不在Assets下Tip),
                        EditorLocalization.GetLocalization(EditorLocalizationStorage.Def_确定));
                }
            }
            return null;
        }

        public static string SaveFile(string defaultName = "", string extension = "")
        {
            string filePath = PlayerPrefs.GetString("LastParticleCheckPath");
            string path = EditorUtility.SaveFilePanel(EditorLocalization.GetLocalization(EditorLocalizationStorage.Def_保存), filePath, defaultName, extension);
            if (path != "")
            {
                int index = path.IndexOf("Assets");
                if (index != -1)
                {
                    PlayerPrefs.SetString("LastParticleCheckPath", path);
                    return path.Substring(index);
                }
                else
                {
                    EditorUtility.DisplayDialog("messageBox",
                        EditorLocalization.GetLocalization(EditorLocalizationStorage.Def_目录不在Assets下Tip),
                        EditorLocalization.GetLocalization(EditorLocalizationStorage.Def_确定));
                }
            }
            return null;
        }
        #endregion

        public static int EnumPopupEx(Rect rect, string label, Type type, int enumValueIndex, string[] labels)
        {
            int[] ints = (int[])Enum.GetValues(type);
            string[] strings = Enum.GetNames(type);
            if (labels.Length != ints.Length)
            {
                return EditorGUI.IntPopup(rect, label, enumValueIndex, strings, ints);
            }
            else
            {
                return EditorGUI.IntPopup(rect, label, enumValueIndex, labels, ints);
            }
        }

        public static int EnumPopupLayoutEx(string label, Type type, int enumValueIndex, string[] labels)
        {
            int[] ints = (int[])Enum.GetValues(type);
            string[] strings = Enum.GetNames(type);
            if (labels.Length != ints.Length)
            {
                return EditorGUILayout.IntPopup(label, enumValueIndex, strings, ints);
            }
            else
            {
                return EditorGUILayout.IntPopup(label, enumValueIndex, labels, ints);
            }
        }
    }
}
#endif