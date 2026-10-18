using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Game
{
    public class CivilianAI : MonoBehaviour
    {
        public static readonly List<CivilianAI> All = new List<CivilianAI>();

        public string DisplayName;
        public bool IsFemale;
        /// <summary>Story civilians stay where they were placed and cower instead of fleeing.</summary>
        public bool StoryLocked;

        public HumanoidRig Rig { get; set; }
        public Health Health { get; private set; }
        public NavMeshAgent Agent { get; private set; }
        public InfectedHost Host { get; private set; }
        public bool IsInfected => Host != null;

        Vector3 home;
        float nextWander, fleeUntil, cowerUntil;
        Vector3 threat;
        bool recovered;

        void Awake()
        {
            Health = GetComponent<Health>();
            Agent = GetComponent<NavMeshAgent>();
            All.Add(this);
        }

        void Start()
        {
            home = transform.position;
            Health.Died += OnDied;
            nextWander = Time.time + Random.Range(0f, 3f);
        }

        void OnDestroy() => All.Remove(this);

        public static void Alert(Vector3 position, float radius, Vector3 threatPosition)
        {
            foreach (var c in All)
                if (c != null && !c.Health.IsDead && (c.transform.position - position).sqrMagnitude < radius * radius)
                    c.Flee(threatPosition);
        }

        public void Flee(Vector3 from)
        {
            if (IsInfected && !recovered) return;
            threat = from;
            if (StoryLocked)
            {
                cowerUntil = Time.time + 4f;
                return;
            }
            if (Time.time > fleeUntil) PickFleePoint();
            fleeUntil = Time.time + Random.Range(5f, 8f);
        }

        void PickFleePoint()
        {
            var away = transform.position - threat;
            away.y = 0f;
            if (away.sqrMagnitude < 0.01f) away = Random.insideUnitSphere;
            away.y = 0f;
            var target = transform.position + away.normalized * 18f + Random.insideUnitSphere * 5f;
            if (NavMeshBaker.Snap(target, out var p, 6f) && Agent.isOnNavMesh) Agent.SetDestination(p);
        }

        public void Cower(float seconds) => cowerUntil = Time.time + seconds;

        public InfectedHost Infect()
        {
            if (Host != null) return Host;
            Host = gameObject.AddComponent<InfectedHost>();
            Host.Civilian = this;
            StoryLocked = true;
            if (Agent.isOnNavMesh) Agent.ResetPath();
            return Host;
        }

        /// <summary>Called after a successful extraction: the person gets up and leaves.</summary>
        public void Recover()
        {
            recovered = true;
            StoryLocked = false;
            home = transform.position;
            Rig.SetPose(HumanoidPose.Standing);
            if (PlayerController.Instance != null) Flee(PlayerController.Instance.transform.position);
        }

        void OnDied(DamageInfo info)
        {
            if (Agent.isOnNavMesh) Agent.ResetPath();
            Agent.enabled = false;
            Rig.SetPose(HumanoidPose.Lying);
            if (!IsInfected && info.sourceFaction == Faction.Player)
            {
                GameSession.Stats.civiliansKilled++;
                GameSession.NotifyStatsChanged();
                MissionEvents.RaiseCivilianKilled(this);
            }
            Alert(transform.position, 25f, info.source != null ? info.source.transform.position : transform.position);
        }

        void Update()
        {
            if (Rig == null) return;
            if (Health.IsDead || !Agent.enabled)
            {
                Rig.Animate(0f);
                return;
            }

            if (IsInfected && !recovered)
            {
                // InfectedHost drives the pose.
                Agent.speed = 0f;
                Rig.Animate(0f);
                return;
            }

            if (Time.time < cowerUntil)
            {
                Rig.SetPose(HumanoidPose.Kneeling);
                if (Agent.isOnNavMesh) Agent.ResetPath();
            }
            else if (Time.time < fleeUntil)
            {
                Rig.SetPose(HumanoidPose.Standing);
                Agent.speed = 4.8f;
                if (Agent.isOnNavMesh && !Agent.pathPending && Agent.remainingDistance < 1.5f) PickFleePoint();
            }
            else
            {
                Rig.SetPose(HumanoidPose.Standing);
                Agent.speed = StoryLocked ? 0.8f : 1.3f;
                if (Agent.isOnNavMesh && !Agent.pathPending && Agent.remainingDistance < 0.6f && Time.time > nextWander)
                {
                    float radius = StoryLocked ? 1.5f : 24f;
                    Agent.SetDestination(NavMeshBaker.RandomPointNear(home, radius));
                    nextWander = Time.time + Random.Range(2f, 7f);
                }
            }

            Rig.Animate(Agent.velocity.magnitude);
        }
    }
}
