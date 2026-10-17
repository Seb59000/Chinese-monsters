using UnityEngine;

namespace Game
{
    /// <summary>
    /// Immediate-mode drawing helpers in a virtual 1080-pixel-high space,
    /// scaled to the real screen so the HUD stays crisp at any resolution.
    /// </summary>
    public static class UIDraw
    {
        public const float H = 1080f;
        public static float S => Screen.height / H;
        public static float W => Screen.width / S;

        public static readonly Color Accent = new Color(1f, 0.82f, 0.3f);
        public static readonly Color Panel = new Color(0f, 0f, 0f, 0.55f);
        public static readonly Color Danger = new Color(1f, 0.25f, 0.2f);

        static GUIStyle style;

        public static Rect ToScreen(Rect v) => new Rect(v.x * S, v.y * S, v.width * S, v.height * S);

        /// <summary>World point to virtual UI coordinates. Returns false if behind the camera.</summary>
        public static bool WorldToUI(Camera cam, Vector3 world, out Vector2 ui)
        {
            var sp = cam.WorldToScreenPoint(world);
            ui = new Vector2(sp.x / S, (Screen.height - sp.y) / S);
            return sp.z > 0f;
        }

        public static void Box(Rect v, Color c)
        {
            var old = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(ToScreen(v), Texture2D.whiteTexture);
            GUI.color = old;
        }

        public static void Frame(Rect v, Color c, float t = 2f)
        {
            Box(new Rect(v.x, v.y, v.width, t), c);
            Box(new Rect(v.x, v.yMax - t, v.width, t), c);
            Box(new Rect(v.x, v.y, t, v.height), c);
            Box(new Rect(v.xMax - t, v.y, t, v.height), c);
        }

        public static void Bar(Rect v, float fill, Color fg, Color? bg = null)
        {
            Box(v, bg ?? new Color(0f, 0f, 0f, 0.6f));
            Box(new Rect(v.x, v.y, v.width * Mathf.Clamp01(fill), v.height), fg);
        }

        static GUIStyle Style(int size, TextAnchor anchor, bool bold, bool wrap)
        {
            if (style == null)
            {
                style = new GUIStyle(GUI.skin.label) { richText = false, clipping = TextClipping.Overflow };
            }
            style.fontSize = Mathf.Max(8, Mathf.RoundToInt(size * S));
            style.alignment = anchor;
            style.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            style.wordWrap = wrap;
            return style;
        }

        public static void Text(Rect v, string text, int size, Color color, TextAnchor anchor = TextAnchor.UpperLeft,
            bool bold = false, bool wrap = false, bool shadow = true)
        {
            if (string.IsNullOrEmpty(text)) return;
            var st = Style(size, anchor, bold, wrap);
            var r = ToScreen(v);
            if (shadow)
            {
                st.normal.textColor = new Color(0f, 0f, 0f, color.a * 0.85f);
                GUI.Label(new Rect(r.x + 2f * S, r.y + 2f * S, r.width, r.height), text, st);
            }
            st.normal.textColor = color;
            GUI.Label(r, text, st);
        }

        public static float TextHeight(string text, int size, float width, bool bold = false)
        {
            var st = Style(size, TextAnchor.UpperLeft, bold, true);
            return st.CalcHeight(new GUIContent(text), width * S) / S;
        }

        public static float TextWidth(string text, int size, bool bold = false)
        {
            var st = Style(size, TextAnchor.UpperLeft, bold, false);
            return st.CalcSize(new GUIContent(text)).x / S;
        }

        /// <summary>Menu button. Returns true when clicked; reports mouse hover.</summary>
        public static bool Button(Rect v, string text, bool selected, out bool hovered, int size = 32)
        {
            var e = Event.current;
            var r = ToScreen(v);
            hovered = r.Contains(e.mousePosition);
            Box(v, selected ? new Color(1f, 0.82f, 0.3f, 0.22f) : new Color(0f, 0f, 0f, 0.45f));
            if (selected) Box(new Rect(v.x, v.y, 6f, v.height), Accent);
            Text(new Rect(v.x + 28f, v.y, v.width - 40f, v.height), text, size, selected ? Accent : Color.white, TextAnchor.MiddleLeft, selected);
            if (e.type == EventType.MouseDown && e.button == 0 && hovered)
            {
                e.Use();
                return true;
            }
            return false;
        }
    }
}
