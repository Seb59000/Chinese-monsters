using System.Collections;
using UnityEngine;

namespace Game
{
    /// <summary>Procedural tentacles drawn with LineRenderers, swaying from the back. Can lash out at a point.</summary>
    public class TentacleRig : MonoBehaviour
    {
        public int count = 5;
        public float length = 1.5f;
        public float idleExtension = 0.55f;
        public Color rootColor = new Color(0.25f, 0.02f, 0.12f);
        public Color tipColor = new Color(0.75f, 0.1f, 0.35f);
        public Vector3 anchor = new Vector3(0f, 1.35f, -0.18f);

        const int Segments = 14;
        LineRenderer[] lines;
        Vector3[] points = new Vector3[Segments];
        float[] seeds;
        Vector3 reachTarget;
        float reach, extension, raise;
        Coroutine routine;

        public bool IsReaching => reach > 0.01f;

        void Start()
        {
            lines = new LineRenderer[count];
            seeds = new float[count];
            var mat = MaterialFactory.Glow(Color.white);
            for (int i = 0; i < count; i++)
            {
                var go = new GameObject("Tentacle" + i);
                go.transform.SetParent(transform, false);
                var lr = go.AddComponent<LineRenderer>();
                lr.positionCount = Segments;
                lr.sharedMaterial = mat;
                lr.startWidth = 0.11f;
                lr.endWidth = 0.015f;
                lr.startColor = rootColor;
                lr.endColor = tipColor;
                lr.numCapVertices = 2;
                lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                lines[i] = lr;
                seeds[i] = Random.value * 100f;
            }
            extension = idleExtension;
        }

        /// <summary>0..1 visual menace: raises and spreads the tentacles (used as an attack tell).</summary>
        public void SetRaise(float amount) => raise = amount;
        public void SetExtension(float amount) => extension = amount;

        public void Lash(Vector3 target, float hold = 0.15f)
        {
            if (routine != null) StopCoroutine(routine);
            routine = StartCoroutine(LashRoutine(target, hold));
        }

        IEnumerator LashRoutine(Vector3 target, float hold)
        {
            reachTarget = target;
            while (reach < 1f) { reach = Mathf.MoveTowards(reach, 1f, Time.deltaTime / 0.14f); yield return null; }
            yield return new WaitForSeconds(hold);
            while (reach > 0f) { reach = Mathf.MoveTowards(reach, 0f, Time.deltaTime / 0.3f); yield return null; }
            routine = null;
        }

        /// <summary>Keeps tentacles attached to a moving point (used while infecting someone).</summary>
        public void Hold(Vector3 target, float amount)
        {
            if (routine != null) { StopCoroutine(routine); routine = null; }
            reachTarget = target;
            reach = amount;
        }

        void LateUpdate()
        {
            if (lines == null) return;
            float t = Time.time;
            var basePos = transform.TransformPoint(anchor);
            for (int i = 0; i < count; i++)
            {
                float side = count > 1 ? Mathf.Lerp(-1f, 1f, i / (float)(count - 1)) : 0f;
                float s = seeds[i];
                var localDir = new Vector3(side * (0.9f + raise * 0.6f), 0.5f + raise * 0.9f + Mathf.Sin(t * 1.3f + s) * 0.25f, -1f + raise * 0.4f);
                var idleDir = transform.TransformDirection(localDir.normalized);
                float len = length * Mathf.Lerp(extension, 1f, raise) * (0.85f + 0.15f * Mathf.Sin(t * 2.1f + s));
                var idleTip = basePos + idleDir * len;
                var tip = Vector3.Lerp(idleTip, reachTarget + Random.insideUnitSphere * 0.05f, reach);
                var control = basePos + idleDir * len * 0.5f + Vector3.up * 0.3f * (1f - reach);
                var perp = Vector3.Cross(tip - basePos, Vector3.up).normalized;
                for (int k = 0; k < Segments; k++)
                {
                    float u = k / (float)(Segments - 1);
                    var p = (1f - u) * (1f - u) * basePos + 2f * (1f - u) * u * control + u * u * tip;
                    float wave = Mathf.Sin(u * 7f - t * 6f + s) * 0.12f * u * (1f - reach * 0.6f);
                    points[k] = p + perp * wave + Vector3.up * Mathf.Cos(u * 5f - t * 4f + s) * 0.06f * u;
                }
                lines[i].SetPositions(points);
            }
        }
    }
}
