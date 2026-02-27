using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThunderFireUnityEx
{
    public static class JsonUtilityEx
    {
        [Serializable]
        private class JsonWrapper<T>
        {
            public List<T> Items;
        }

        public static List<T> FromJsonLegacy<T>(string json)
        {
            string wrappedJson = "{\"Items\":" + json + "}";
            var wrapper = JsonUtility.FromJson<JsonWrapper<T>>(wrappedJson);
            return wrapper?.Items ?? new List<T>();
        }
    }
}
