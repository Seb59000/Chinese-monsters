using UnityEngine;

namespace Game
{
    /// <summary>Tiny self-destroying glow used for impacts, sparks and blood puffs.</summary>
    public class TransientFx : MonoBehaviour
    {
        float life, age;
        Vector3 startScale, drift;

        public static void Spawn(Vector3 position, Color color, float size, float life, Transform parent = null, Vector3 drift = default)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(go.GetComponent<Collider>());
            go.name = "Fx";
            if (parent != null) go.transform.SetParent(parent, true);
            go.transform.position = position;
            go.transform.localScale = Vector3.one * size;
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = MaterialFactory.Glow(color);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var fx = go.AddComponent<TransientFx>();
            fx.life = life;
            fx.startScale = go.transform.localScale;
            fx.drift = drift;
        }

        public static void Burst(Vector3 position, Color color, int count, float size, float speed)
        {
            for (int i = 0; i < count; i++)
                Spawn(position, color, size * Random.Range(0.5f, 1f), Random.Range(0.2f, 0.45f), null, Random.insideUnitSphere * speed);
        }

        void Update()
        {
            age += Time.deltaTime;
            float t = age / life;
            transform.localScale = startScale * (1f - t);
            transform.position += drift * Time.deltaTime;
            drift += Physics.gravity * 0.3f * Time.deltaTime;
            if (t >= 1f) Destroy(gameObject);
        }
    }
}
