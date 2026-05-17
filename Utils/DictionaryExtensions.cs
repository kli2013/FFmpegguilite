using System.Collections.Generic;

namespace FFLiteGUI.Utils
{
    public static class DictionaryExtensions
    {
        public static TValue GetValueOrDefault<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, TValue defaultValue = default)
        {
            if (dict.TryGetValue(key, out TValue value))
                return value;
            return defaultValue;
        }
    }
}
