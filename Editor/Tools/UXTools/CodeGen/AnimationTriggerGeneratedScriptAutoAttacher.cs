#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace UIAnimation
{
    /// <summary>
    /// 处理“生成后自动挂载桥接脚本”的延迟流程。
    /// 由于新脚本生成后需要等待 Unity 重新编译，无法在同一帧直接 AddComponent，
    /// 因此这里通过 SessionState 持久化挂载请求，并在脚本编译完成后自动执行。
    /// </summary>
    [InitializeOnLoad]
    public static class AnimationTriggerGeneratedScriptAutoAttacher
    {
        [Serializable]
        private class PendingAttachRequest
        {
            public string GlobalObjectId;
            public string ScriptAssetPath;
        }

        private const string SESSION_KEY = "UIAnimation.PendingAttachRequest";

        static AnimationTriggerGeneratedScriptAutoAttacher()
        {
            EditorApplication.update += TryProcessPendingRequest;
        }

        public static void Register(AnimationTriggerAuthoring authoring, string scriptAssetPath)
        {
            if (authoring == null || string.IsNullOrWhiteSpace(scriptAssetPath))
                return;

            var request = new PendingAttachRequest
            {
                GlobalObjectId = GlobalObjectId.GetGlobalObjectIdSlow(authoring.gameObject).ToString(),
                ScriptAssetPath = scriptAssetPath,
            };

            SessionState.SetString(SESSION_KEY, JsonUtility.ToJson(request));
        }

        private static void TryProcessPendingRequest()
        {
            var json = SessionState.GetString(SESSION_KEY, string.Empty);
            if (string.IsNullOrWhiteSpace(json))
                return;

            var request = JsonUtility.FromJson<PendingAttachRequest>(json);
            if (request == null
                || string.IsNullOrWhiteSpace(request.GlobalObjectId)
                || string.IsNullOrWhiteSpace(request.ScriptAssetPath))
            {
                ClearPending();
                return;
            }

            var script = AssetDatabase.LoadAssetAtPath<MonoScript>(request.ScriptAssetPath);
            var scriptType = script?.GetClass();
            if (scriptType == null)
                return;

            if (!GlobalObjectId.TryParse(request.GlobalObjectId, out var globalId))
            {
                Debug.LogWarning("[UIAnimation] 自动挂载失败：无法解析目标对象。");
                ClearPending();
                return;
            }

            var targetObject = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(globalId) as GameObject;
            if (targetObject == null)
            {
                Debug.LogWarning("[UIAnimation] 自动挂载失败：目标对象已不存在。");
                ClearPending();
                return;
            }

            var authoring = targetObject.GetComponent<AnimationTriggerAuthoring>();
            if (authoring == null)
            {
                Debug.LogWarning("[UIAnimation] 自动挂载失败：目标对象上缺少 AnimationTriggerAuthoring。");
                ClearPending();
                return;
            }

            var component = targetObject.GetComponent(scriptType);
            if (component == null)
                component = Undo.AddComponent(targetObject, scriptType);

            if (component == null)
            {
                Debug.LogWarning($"[UIAnimation] 自动挂载失败：无法添加组件 {scriptType.FullName}。");
                ClearPending();
                return;
            }

            var serializedObject = new SerializedObject(component);
            var authoringProp = serializedObject.FindProperty("_animationTriggerAuthoring");
            if (authoringProp != null)
            {
                authoringProp.objectReferenceValue = authoring;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(component);
            }

            Selection.activeGameObject = targetObject;
            Debug.Log($"[UIAnimation] 已自动挂载桥接组件：{scriptType.FullName}");
            ClearPending();
        }

        private static void ClearPending()
        {
            SessionState.EraseString(SESSION_KEY);
        }
    }
}
#endif
