using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game
{
    /// <summary>Subtitled radio/voice lines, queued. Stores localisation keys so a language switch applies live.</summary>
    public class RadioDialogue : MonoBehaviour
    {
        public struct Line
        {
            public string speakerKey;
            public string textKey;
        }

        public static RadioDialogue Instance { get; private set; }

        readonly Queue<Line> queue = new Queue<Line>();
        public bool HasLine { get; private set; }
        public Line Current { get; private set; }
        public float LineProgress01 => HasLine ? Mathf.Clamp01((Time.time - lineStart) / Mathf.Max(0.01f, lineDuration)) : 0f;
        public bool IsBusy => HasLine || queue.Count > 0;

        float lineStart, lineDuration;

        void Awake() => Instance = this;

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Say(string speakerKey, string textKey) =>
            queue.Enqueue(new Line { speakerKey = speakerKey, textKey = textKey });

        /// <summary>Queues pairs of (speakerKey, textKey) and waits until they have all been shown.</summary>
        public IEnumerator Play(params string[] pairs)
        {
            for (int i = 0; i + 1 < pairs.Length; i += 2) Say(pairs[i], pairs[i + 1]);
            while (IsBusy) yield return null;
        }

        public void Clear()
        {
            queue.Clear();
            HasLine = false;
        }

        void Update()
        {
            if (HasLine && Time.time - lineStart >= lineDuration) HasLine = false;
            if (!HasLine && queue.Count > 0)
            {
                Current = queue.Dequeue();
                HasLine = true;
                lineStart = Time.time;
                lineDuration = 1.8f + L.Get(Current.textKey).Length * 0.055f;
                bool overRadio = Current.speakerKey != "spk.you" && Current.speakerKey != "spk.t07";
                if (overRadio) AudioManager.Play2D(SoundBank.Radio, 0.5f);
            }
        }

        public static Color SpeakerColor(string speakerKey)
        {
            switch (speakerKey)
            {
                case "spk.you": return new Color(0.75f, 0.85f, 1f);
                case "spk.lin": return new Color(0.4f, 0.85f, 1f);
                case "spk.zhao": return new Color(1f, 0.8f, 0.35f);
                case "spk.shen": return new Color(0.55f, 1f, 0.6f);
                case "spk.t07": return new Color(1f, 0.25f, 0.3f);
                default: return Color.white;
            }
        }
    }
}
