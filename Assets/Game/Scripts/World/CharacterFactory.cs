using UnityEngine;
using UnityEngine.AI;

namespace Game
{
    public static class CharacterFactory
    {
        static readonly Color[] Clothes =
        {
            new Color(0.5f, 0.1f, 0.1f), new Color(0.15f, 0.2f, 0.35f), new Color(0.35f, 0.33f, 0.3f), new Color(0.6f, 0.55f, 0.45f),
            new Color(0.1f, 0.3f, 0.25f), new Color(0.45f, 0.2f, 0.4f), new Color(0.8f, 0.75f, 0.7f), new Color(0.2f, 0.2f, 0.22f),
            new Color(0.75f, 0.45f, 0.1f), new Color(0.25f, 0.4f, 0.6f),
        };
        static readonly Color[] Skins = { new Color(0.93f, 0.8f, 0.68f), new Color(0.87f, 0.72f, 0.58f), new Color(0.8f, 0.64f, 0.5f), new Color(0.7f, 0.54f, 0.42f) };
        static readonly Color[] Hair = { new Color(0.04f, 0.04f, 0.05f), new Color(0.12f, 0.08f, 0.06f), new Color(0.25f, 0.15f, 0.1f), new Color(0.5f, 0.1f, 0.2f) };

        public static PlayerController SpawnPlayer(Transform parent, Vector3 position, float yaw)
        {
            var go = new GameObject("Player");
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(position + Vector3.up * 0.1f, Quaternion.Euler(0f, yaw, 0f));
            go.tag = "Player";

            var cc = go.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.35f;
            cc.center = new Vector3(0f, 0.92f, 0f);
            cc.stepOffset = 0.4f;
            cc.slopeLimit = 50f;
            cc.skinWidth = 0.05f;

            var health = go.AddComponent<Health>();
            health.faction = Faction.Player;
            health.SetMax(100f);
            health.regenDelay = 5f;
            health.regenRate = 14f;

            var pc = go.AddComponent<PlayerController>();
            pc.Rig = HumanoidRig.Build(go.transform, new HumanoidRig.Look
            {
                clothes = new Color(0.08f, 0.12f, 0.25f), pants = new Color(0.05f, 0.06f, 0.1f),
                skin = Skins[1], hair = Hair[0], height = 1.8f, cap = true,
            });
            // Reflective stripes on the uniform.
            var stripe = MaterialFactory.Emissive(new Color(0.8f, 0.8f, 0.3f), new Color(0.35f, 0.35f, 0.1f));
            MeshUtil.Visual(PrimitiveType.Cube, pc.Rig.torso, new Vector3(0f, 0.1f, 0f), new Vector3(1.02f, 0.12f, 1.02f), stripe);

            Layers.SetRecursive(go, Layers.Player);
            go.AddComponent<PlayerShooter>();
            go.AddComponent<PlayerInteractor>();
            return pc;
        }

        static GameObject NpcBase(string name, Transform parent, Vector3 position, float radius, float height, Faction faction, float hp)
        {
            if (!NavMeshBaker.Snap(position, out var p, 6f)) p = position;
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = p;
            go.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            var col = go.AddComponent<CapsuleCollider>();
            col.radius = radius;
            col.height = height;
            col.center = new Vector3(0f, height * 0.5f, 0f);

            var health = go.AddComponent<Health>();
            health.faction = faction;
            health.SetMax(hp);

            var agent = go.AddComponent<NavMeshAgent>();
            agent.radius = radius;
            agent.height = height;
            agent.angularSpeed = 540f;
            agent.acceleration = 14f;
            agent.stoppingDistance = 0.2f;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
            if (agent.isOnNavMesh) agent.Warp(p);
            return go;
        }

        public static CivilianAI SpawnCivilian(Transform parent, Vector3 position, System.Random rnd, bool? female = null)
        {
            bool isFemale = female ?? rnd.NextDouble() < 0.5;
            var go = NpcBase("Civilian", parent, position, 0.3f, 1.75f, Faction.Civilian, 40f);
            var civ = go.AddComponent<CivilianAI>();
            civ.IsFemale = isFemale;
            civ.DisplayName = NameGenerator.Make(rnd, isFemale);
            civ.Rig = HumanoidRig.Build(go.transform, new HumanoidRig.Look
            {
                clothes = Clothes[rnd.Next(Clothes.Length)],
                pants = Clothes[rnd.Next(Clothes.Length)] * 0.5f,
                skin = Skins[rnd.Next(Skins.Length)],
                hair = Hair[rnd.Next(Hair.Length)],
                height = isFemale ? CityGenerator.Rand(rnd, 1.55f, 1.7f) : CityGenerator.Rand(rnd, 1.65f, 1.84f),
                longHair = isFemale && rnd.NextDouble() < 0.75,
            });
            Layers.SetRecursive(go, Layers.Npc);
            return civ;
        }

        public static SuspectAI SpawnSuspect(Transform parent, Vector3 position)
        {
            var go = NpcBase("Suspect_T07", parent, position, 0.35f, 1.95f, Faction.Mutant, 650f);
            var agent = go.GetComponent<NavMeshAgent>();
            agent.speed = 9f;
            agent.acceleration = 40f;
            agent.angularSpeed = 900f;
            // Scripted survivor until the docks fight lowers this floor.
            go.GetComponent<Health>().floor = 650f * 0.7f;
            var suspect = go.AddComponent<SuspectAI>();
            suspect.Rig = HumanoidRig.Build(go.transform, new HumanoidRig.Look
            {
                clothes = new Color(0.1f, 0.09f, 0.09f), pants = new Color(0.06f, 0.06f, 0.07f),
                skin = new Color(0.66f, 0.64f, 0.66f), hair = new Color(0.08f, 0.08f, 0.09f), height = 1.95f,
                eyes = true, eyeColor = new Color(1f, 0.1f, 0.05f),
            });
            suspect.Tentacles = suspect.Rig.gameObject.AddComponent<TentacleRig>();
            Layers.SetRecursive(go, Layers.Npc);
            return suspect;
        }

        public static OffspringAI SpawnOffspring(Transform parent, Vector3 position)
        {
            var go = NpcBase("Offspring", parent, position, 0.3f, 1.3f, Faction.Mutant, 70f);
            var agent = go.GetComponent<NavMeshAgent>();
            agent.speed = 7.2f;
            agent.acceleration = 30f;
            var skin = new Color(0.42f, 0.36f, 0.48f);
            var o = go.AddComponent<OffspringAI>();
            o.Rig = HumanoidRig.Build(go.transform, new HumanoidRig.Look
            {
                clothes = skin * 0.8f, pants = skin * 0.6f, skin = skin, hair = skin, height = 1.3f,
                eyes = true, eyeColor = new Color(1f, 0.15f, 0.1f),
            });
            var t = o.Rig.gameObject.AddComponent<TentacleRig>();
            t.count = 3;
            t.length = 0.9f;
            t.anchor = new Vector3(0f, 1.3f, -0.15f);
            o.Tentacles = t;
            Layers.SetRecursive(go, Layers.Npc);
            MissionEvents.RaiseOffspringSpawned(o);
            return o;
        }
    }
}
