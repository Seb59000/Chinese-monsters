using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game
{
    public enum Language { French, English }

    /// <summary>
    /// Text lookup. All strings live in Resources/Localization/strings.json
    /// as { "key", "fr", "en" } entries, so translations can be edited without code.
    /// </summary>
    public static class L
    {
        [Serializable] class Entry { public string key; public string fr; public string en; }
        [Serializable] class Table { public Entry[] entries; }

        static Dictionary<string, Entry> table;
        public static Language Current { get; private set; } = Language.French;
        public static event Action Changed;

        public static void Init()
        {
            if (table != null) return;
            table = new Dictionary<string, Entry>();
            var asset = Resources.Load<TextAsset>("Localization/strings");
            if (asset == null)
            {
                Debug.LogError("Localization file Resources/Localization/strings.json is missing.");
                return;
            }
            var parsed = JsonUtility.FromJson<Table>(asset.text);
            foreach (var e in parsed.entries)
                table[e.key] = e;

            Current = (Language)PlayerPrefs.GetInt("cm.language",
                Application.systemLanguage == SystemLanguage.French ? 0 : 1);
        }

        public static void SetLanguage(Language lang)
        {
            Current = lang;
            PlayerPrefs.SetInt("cm.language", (int)lang);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        public static void ToggleLanguage() =>
            SetLanguage(Current == Language.French ? Language.English : Language.French);

        public static string Get(string key)
        {
            Init();
            if (!table.TryGetValue(key, out var e)) return "#" + key;
            var text = Current == Language.French ? e.fr : e.en;
            return string.IsNullOrEmpty(text) ? e.fr : text;
        }

        public static string Format(string key, params object[] args) => string.Format(Get(key), args);
    }
}
