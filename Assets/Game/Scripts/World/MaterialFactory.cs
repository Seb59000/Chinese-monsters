using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game
{
    /// <summary>
    /// Creates materials at runtime for whichever render pipeline the project uses
    /// (Built-in "Standard" or URP "Lit"). Materials are cached by colour.
    /// </summary>
    public static class MaterialFactory
    {
        static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();
        static Shader lit, unlit;

        static bool IsUrp => GraphicsSettings.currentRenderPipeline != null;

        static Shader LitShader
        {
            get
            {
                if (lit != null) return lit;
                // The editor setup saves a material here so the shader is kept in builds.
                var template = Resources.Load<Material>("GameMaterials/Lit");
                if (template != null) lit = template.shader;
                if (lit == null) lit = Shader.Find(IsUrp ? "Universal Render Pipeline/Lit" : "Standard");
                if (lit == null) lit = Shader.Find("Standard");
                return lit;
            }
        }

        static Shader UnlitShader
        {
            get
            {
                if (unlit == null) unlit = Shader.Find("Sprites/Default");
                return unlit;
            }
        }

        public static Material Lit(Color color, float smoothness = 0.2f, float metallic = 0f)
        {
            string key = "L" + color + smoothness + metallic;
            if (cache.TryGetValue(key, out var m) && m != null) return m;
            m = new Material(LitShader);
            SetColor(m, color);
            m.SetFloat("_Glossiness", smoothness);
            m.SetFloat("_Smoothness", smoothness);
            m.SetFloat("_Metallic", metallic);
            cache[key] = m;
            return m;
        }

        public static Material Emissive(Color baseColor, Color emission, float intensity = 1f)
        {
            string key = "E" + baseColor + emission + intensity;
            if (cache.TryGetValue(key, out var m) && m != null) return m;
            m = new Material(LitShader);
            SetColor(m, baseColor);
            EnableEmission(m, emission * intensity, null);
            cache[key] = m;
            return m;
        }

        public static Material Textured(Texture2D albedo, Texture2D emissionMap, Color emission)
        {
            var m = new Material(LitShader);
            SetColor(m, Color.white);
            m.SetTexture("_MainTex", albedo);
            m.SetTexture("_BaseMap", albedo);
            m.SetFloat("_Glossiness", 0.35f);
            m.SetFloat("_Smoothness", 0.35f);
            if (emissionMap != null) EnableEmission(m, emission, emissionMap);
            return m;
        }

        /// <summary>Unlit, vertex-coloured, alpha-blended. Works in every pipeline.</summary>
        public static Material Glow(Color color)
        {
            string key = "G" + color;
            if (cache.TryGetValue(key, out var m) && m != null) return m;
            m = new Material(UnlitShader) { color = color };
            cache[key] = m;
            return m;
        }

        /// <summary>A fresh (uncached) material whose colour can be animated per object.</summary>
        public static Material GlowInstance(Color color) => new Material(UnlitShader) { color = color };

        public static Material EmissiveInstance(Color baseColor, Color emission)
        {
            var m = new Material(LitShader);
            SetColor(m, baseColor);
            EnableEmission(m, emission, null);
            return m;
        }

        public static void SetEmission(Material m, Color emission)
        {
            m.SetColor("_EmissionColor", emission);
        }

        static void SetColor(Material m, Color c)
        {
            m.SetColor("_Color", c);
            m.SetColor("_BaseColor", c);
        }

        static void EnableEmission(Material m, Color emission, Texture2D map)
        {
            m.EnableKeyword("_EMISSION");
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            m.SetColor("_EmissionColor", emission);
            if (map != null) m.SetTexture("_EmissionMap", map);
        }

        public static void ClearCache() => cache.Clear();
    }
}
