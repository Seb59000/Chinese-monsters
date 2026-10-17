using UnityEngine;

namespace Game
{
    /// <summary>An objective marker drawn by the HUD (follows a transform or stays at a point).</summary>
    public class Marker
    {
        public Transform target;
        public Vector3 point;
        public Vector3 offset = Vector3.up * 2.3f;
        public string labelKey;
        public string labelText;
        public Color color = new Color(1f, 0.85f, 0.3f);

        public Vector3 Position => target != null ? target.position + offset : point + offset;
        public bool Valid => target == null || target.gameObject.activeInHierarchy;
        public string Label => !string.IsNullOrEmpty(labelText) ? labelText : string.IsNullOrEmpty(labelKey) ? "" : L.Get(labelKey);
    }
}
