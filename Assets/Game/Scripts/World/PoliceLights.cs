using UnityEngine;

namespace Game
{
    /// <summary>Alternating red/blue light bar.</summary>
    public class PoliceLights : MonoBehaviour
    {
        Renderer red, blue;
        Light redLight, blueLight;
        public float period = 0.5f;

        public void Setup(Renderer redBar, Renderer blueBar)
        {
            red = redBar;
            blue = blueBar;
            redLight = CityProps.AddLight(transform, redBar.transform.position + Vector3.up * 0.3f, Color.red, 12f, 0f);
            blueLight = CityProps.AddLight(transform, blueBar.transform.position + Vector3.up * 0.3f, new Color(0.2f, 0.3f, 1f), 12f, 0f);
        }

        void Update()
        {
            if (red == null) return;
            bool phase = Mathf.Repeat(Time.time, period) < period * 0.5f;
            float flicker = Mathf.Repeat(Time.time * 8f, 1f) < 0.6f ? 1f : 0.3f;
            MaterialFactory.SetEmission(red.material, Color.red * (phase ? 4f * flicker : 0.2f));
            MaterialFactory.SetEmission(blue.material, new Color(0.2f, 0.3f, 1f) * (!phase ? 4f * flicker : 0.2f));
            redLight.intensity = phase ? 3f * flicker : 0f;
            blueLight.intensity = !phase ? 3.5f * flicker : 0f;
        }
    }
}
