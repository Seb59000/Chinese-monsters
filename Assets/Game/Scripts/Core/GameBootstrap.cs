using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game
{
    /// <summary>
    /// Entry point. Builds the whole slice from code (city, navmesh, characters, UI, mission)
    /// and rebuilds it on restart. Put it on an empty GameObject, or just press Play in an empty scene.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        public static GameBootstrap Instance { get; private set; }
        public static bool IsLoading { get; private set; } = true;

        public int seed = 1987;
        public int civilians = 46;

        Transform world;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoStart()
        {
            // Pressing Play in an unsaved/empty scene still launches the game.
            if (FindAnyObjectByType<GameBootstrap>() != null) return;
            if (!string.IsNullOrEmpty(SceneManager.GetActiveScene().path)) return;
            new GameObject("GameBootstrap").AddComponent<GameBootstrap>();
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            Application.targetFrameRate = 144;
            L.Init();
            GameSession.LoadSettings();
            GameSession.ResetAll();
        }

        void Start() => StartCoroutine(Build(GameSession.Checkpoint));

        public void Restart(bool fromCheckpoint)
        {
            if (IsLoading) return;
            if (fromCheckpoint) GameSession.RestoreCheckpointStats();
            else GameSession.ResetAll();
            StartCoroutine(RestartRoutine());
        }

        IEnumerator RestartRoutine()
        {
            IsLoading = true;
            Time.timeScale = 1f;
            AudioListener.pause = false;
            MissionEvents.Clear();
            if (world != null) Destroy(world.gameObject);
            NavMeshBaker.Clear();
            yield return null;
            yield return Build(GameSession.Checkpoint);
        }

        IEnumerator Build(MissionPhase from)
        {
            IsLoading = true;
            RemoveSceneDefaults();
            yield return null; // let the loading screen draw

            world = new GameObject("World").transform;
            WorldAtmosphere.Setup(world);
            var layout = CityGenerator.Generate(world, seed);
            CityProps.PoliceCar(world, layout.policeCar, 0f);
            yield return null;
            NavMeshBaker.Bake(world, layout.bounds);
            yield return null;

            var systems = new GameObject("Systems").transform;
            systems.SetParent(world, false);
            systems.gameObject.AddComponent<GameInput>();
            systems.gameObject.AddComponent<AudioManager>();
            systems.gameObject.AddComponent<RadioDialogue>();

            var characters = new GameObject("Characters").transform;
            characters.SetParent(world, false);

            var camGo = new GameObject("MainCamera") { tag = "MainCamera" };
            camGo.transform.SetParent(world, false);
            var cam = camGo.AddComponent<Camera>();
            camGo.AddComponent<AudioListener>();
            WorldAtmosphere.ConfigureCamera(cam);
            var tpc = camGo.AddComponent<ThirdPersonCamera>();
            WorldAtmosphere.CreateRain(camGo.transform);

            SpawnPoint(layout, from, out var spawn, out var yaw);
            var player = CharacterFactory.SpawnPlayer(characters, spawn, yaw);
            tpc.target = player.transform;
            tpc.SnapBehind(yaw);
            camGo.transform.position = spawn + Quaternion.Euler(12f, yaw, 0f) * new Vector3(0.5f, 1.6f, -4f);

            var rnd = new System.Random(seed + 3);
            for (int i = 0; i < civilians && layout.civilianSpawns.Count > 0; i++)
            {
                var p = layout.civilianSpawns[rnd.Next(layout.civilianSpawns.Count)];
                CharacterFactory.SpawnCivilian(characters, p + new Vector3(CityGenerator.Rand(rnd, -1f, 1f), 0f, CityGenerator.Rand(rnd, -1f, 1f)), rnd);
            }

            var director = systems.gameObject.AddComponent<MissionDirector>();
            director.Layout = layout;
            director.Characters = characters;
            director.Player = player;
            director.Cam = tpc;

            systems.gameObject.AddComponent<HUD>();
            systems.gameObject.AddComponent<GameMenus>();
            AudioManager.Instance.StartAmbience(camGo.transform);

            director.Begin(from, seed);
            IsLoading = false;
        }

        static void SpawnPoint(CityLayout layout, MissionPhase from, out Vector3 position, out float yaw)
        {
            switch (from)
            {
                case MissionPhase.Chase:
                    position = layout.chaseCheckpointPlayer; yaw = 0f; break;
                case MissionPhase.Fight:
                    position = layout.fightCheckpointPlayer; yaw = 90f; break;
                case MissionPhase.Sample:
                case MissionPhase.Hosts:
                    position = layout.arenaCenter + new Vector3(-6f, 0f, 0f); yaw = 90f; break;
                default:
                    position = layout.playerStart; yaw = layout.playerStartYaw; break;
            }
        }

        /// <summary>The default camera and light of a new scene would fight with ours.</summary>
        void RemoveSceneDefaults()
        {
            foreach (var c in FindObjectsByType<Camera>(FindObjectsSortMode.None))
                if (world == null || !c.transform.IsChildOf(world)) Destroy(c.gameObject);
            foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (l.type == LightType.Directional && (world == null || !l.transform.IsChildOf(world))) Destroy(l.gameObject);
        }

        void OnGUI()
        {
            if (!IsLoading) return;
            GUI.depth = -10;
            UIDraw.Box(new Rect(0f, 0f, UIDraw.W, UIDraw.H), Color.black);
            UIDraw.Text(new Rect(0f, UIDraw.H * 0.5f - 30f, UIDraw.W, 60f), L.Get("loading"), 30, new Color(1f, 1f, 1f, 0.8f), TextAnchor.MiddleCenter);
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
                IsLoading = true;
                NavMeshBaker.Clear();
                MissionEvents.Clear();
            }
        }
    }
}
