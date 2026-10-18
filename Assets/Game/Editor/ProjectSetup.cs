using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Game.EditorTools
{
    /// <summary>
    /// First-open setup: creates the Mission 01 scene (one GameBootstrap object), adds it to the build,
    /// opens it, and saves a lit material so its shader is kept in standalone builds.
    /// </summary>
    [InitializeOnLoad]
    public static class ProjectSetup
    {
        const string SceneFolder = "Assets/Scenes";
        const string ScenePath = "Assets/Scenes/Mission01.unity";
        const string MaterialFolder = "Assets/Game/Resources/GameMaterials";
        const string MaterialPath = MaterialFolder + "/Lit.mat";
        const string EmissivePath = MaterialFolder + "/LitEmissive.mat";

        static ProjectSetup()
        {
            EditorApplication.delayCall += AutoSetup;
        }

        static void AutoSetup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (SessionState.GetBool("cm.setupDone", false)) return;
            SessionState.SetBool("cm.setupDone", true);

            EnsureMaterial();
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                CreateScene();
                return;
            }
            EnsureBuildSettings();
            var active = SceneManager.GetActiveScene();
            if (string.IsNullOrEmpty(active.path) && !active.isDirty)
                EditorSceneManager.OpenScene(ScenePath);
        }

        [MenuItem("Chinese Monsters/Ouvrir la Mission 01 (Open Mission 01)", priority = 0)]
        public static void OpenScene()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                CreateScene();
                return;
            }
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(ScenePath);
        }

        [MenuItem("Chinese Monsters/Jouer (Play)", priority = 1)]
        public static void Play()
        {
            OpenScene();
            EditorApplication.isPlaying = true;
        }

        [MenuItem("Chinese Monsters/Recréer la scène (Rebuild scene)", priority = 20)]
        public static void CreateScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (!AssetDatabase.IsValidFolder(SceneFolder)) AssetDatabase.CreateFolder("Assets", "Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("GameBootstrap").AddComponent<GameBootstrap>();
            // Fog is enabled at runtime; enabling it in the scene keeps the fog shader variants in builds.
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.011f;
            RenderSettings.fogColor = new Color(0.035f, 0.04f, 0.06f);
            EditorSceneManager.SaveScene(scene, ScenePath);
            EnsureBuildSettings();
            EnsureMaterial();
            Debug.Log("[Chinese Monsters] Scene created: " + ScenePath + ". Press Play.");
        }

        static void EnsureBuildSettings()
        {
            foreach (var s in EditorBuildSettings.scenes)
                if (s.path == ScenePath) return;
            var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            list.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = list.ToArray();
        }

        static void EnsureMaterial()
        {
            var shader = GraphicsSettings.currentRenderPipeline != null ? Shader.Find("Universal Render Pipeline/Lit") : Shader.Find("Standard");
            if (shader == null) return;
            if (!AssetDatabase.IsValidFolder(MaterialFolder)) AssetDatabase.CreateFolder("Assets/Game/Resources", "GameMaterials");
            EnsureMaterialAsset(MaterialPath, shader, false);
            EnsureMaterialAsset(EmissivePath, shader, true);
            AssetDatabase.SaveAssets();
        }

        static void EnsureMaterialAsset(string path, Shader shader, bool emissive)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            else if (mat.shader != shader)
            {
                mat.shader = shader;
            }
            if (emissive)
            {
                mat.EnableKeyword("_EMISSION");
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                mat.SetColor("_EmissionColor", Color.white);
            }
            EditorUtility.SetDirty(mat);
        }
    }
}
