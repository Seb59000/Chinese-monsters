using UnityEngine;
using UnityEngine.Rendering;

namespace Game
{
    /// <summary>Night, fog, moonlight and rain.</summary>
    public static class WorldAtmosphere
    {
        public static readonly Color FogColor = new Color(0.035f, 0.04f, 0.06f);

        public static void Setup(Transform root)
        {
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.1f, 0.11f, 0.16f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.011f;
            RenderSettings.fogColor = FogColor;
            QualitySettings.shadowDistance = 90f;

            var moon = new GameObject("Moonlight");
            moon.transform.SetParent(root, false);
            moon.transform.rotation = Quaternion.Euler(52f, -35f, 0f);
            var light = moon.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(0.55f, 0.62f, 0.85f);
            light.intensity = 0.35f;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.7f;
            RenderSettings.sun = light;
        }

        public static void ConfigureCamera(Camera cam)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = FogColor;
            cam.farClipPlane = 600f;
            cam.nearClipPlane = 0.1f;
            cam.allowHDR = true;
        }

        /// <summary>Rain volume that follows the camera.</summary>
        public static ParticleSystem CreateRain(Transform cameraTransform)
        {
            var go = new GameObject("Rain");
            go.transform.SetParent(cameraTransform, false);
            go.transform.localPosition = new Vector3(0f, 12f, 8f);
            go.transform.localRotation = Quaternion.identity;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.loop = true;
            main.startLifetime = 1.1f;
            main.startSpeed = 0f;
            main.startSize3D = true;
            main.startSizeX = 0.025f;
            main.startSizeY = 0.7f;
            main.startSizeZ = 0.025f;
            main.startColor = new Color(0.65f, 0.72f, 0.85f, 0.28f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 4000;
            main.gravityModifier = 0f;

            var emission = ps.emission;
            emission.rateOverTime = 2600f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(40f, 1f, 40f);

            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(-1.5f);
            vel.y = new ParticleSystem.MinMaxCurve(-24f);
            vel.z = new ParticleSystem.MinMaxCurve(0.5f);

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.velocityScale = 0.02f;
            renderer.lengthScale = 1f;
            renderer.sharedMaterial = MaterialFactory.Glow(Color.white);
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            ps.Play();
            return ps;
        }
    }
}
