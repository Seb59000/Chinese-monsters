using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Game
{
    /// <summary>A newborn mutant ("rejeton"): small, fast, hunts the player.</summary>
    public class OffspringAI : MonoBehaviour
    {
        public static readonly List<OffspringAI> All = new List<OffspringAI>();

        public HumanoidRig Rig;
        public TentacleRig Tentacles;
        public Health Health { get; private set; }
        public bool IsAlive => Health != null && !Health.IsDead;

        NavMeshAgent agent;
        float nextAttack, nextRepath, nextLunge;
        bool busy;

        void Awake()
        {
            Health = GetComponent<Health>();
            agent = GetComponent<NavMeshAgent>();
            All.Add(this);
        }

        void OnDestroy() => All.Remove(this);

        void Start()
        {
            Health.Died += OnDied;
            StartCoroutine(Emerge());
        }

        IEnumerator Emerge()
        {
            busy = true;
            var full = Rig.transform.localScale;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / 1.2f;
                Rig.transform.localScale = full * Mathf.Lerp(0.3f, 1f, t);
                Tentacles.SetRaise(1f - t);
                yield return null;
            }
            AudioManager.PlayAt(SoundBank.Screech, transform.position, 0.7f, 1.7f);
            busy = false;
            nextLunge = Time.time + 2f;
        }

        void Update()
        {
            if (Health.IsDead)
            {
                Rig.Animate(0f);
                return;
            }
            Rig.Animate(agent.enabled ? agent.velocity.magnitude : 3f);
            var player = PlayerController.Instance;
            if (busy || player == null || player.Health.IsDead || !agent.enabled || !agent.isOnNavMesh) return;

            var to = player.transform.position - transform.position;
            float d = to.magnitude;
            if (d < 2f)
            {
                agent.ResetPath();
                to.y = 0f;
                if (to.sqrMagnitude > 0.01f)
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to), Time.deltaTime * 12f);
                if (Time.time > nextAttack) StartCoroutine(Attack());
            }
            else if (d < 8f && Time.time > nextLunge)
            {
                StartCoroutine(Lunge(player.transform.position));
            }
            else if (Time.time > nextRepath)
            {
                agent.SetDestination(player.transform.position);
                nextRepath = Time.time + 0.25f;
            }
        }

        IEnumerator Attack()
        {
            busy = true;
            nextAttack = Time.time + 1.1f;
            Tentacles.SetRaise(0.8f);
            yield return new WaitForSeconds(0.3f);
            var player = PlayerController.Instance;
            if (player != null && Vector3.Distance(player.transform.position, transform.position) < 2.4f)
            {
                Tentacles.Lash(player.transform.position + Vector3.up, 0.05f);
                var dir = (player.transform.position - transform.position).normalized;
                player.Health.TakeDamage(new DamageInfo(10f, player.transform.position, dir, gameObject, Faction.Mutant));
                player.AddImpulse(dir * 4f);
                AudioManager.PlayAt(SoundBank.Hit, player.transform.position, 0.8f, 1.2f);
            }
            Tentacles.SetRaise(0f);
            busy = false;
        }

        IEnumerator Lunge(Vector3 target)
        {
            busy = true;
            nextLunge = Time.time + Random.Range(3f, 5f);
            agent.ResetPath();
            agent.enabled = false;
            var from = transform.position;
            var end = Vector3.Lerp(from, target, 0.8f);
            if (NavMeshBaker.Snap(end, out var snapped, 2f)) end = snapped;
            AudioManager.PlayAt(SoundBank.Whoosh, from, 0.8f, 1.4f);
            float t = 0f;
            while (t < 1f && !Health.IsDead)
            {
                t += Time.deltaTime / 0.45f;
                var p = Vector3.Lerp(from, end, t);
                p.y += Mathf.Sin(t * Mathf.PI) * 1.6f;
                transform.position = p;
                yield return null;
            }
            if (!Health.IsDead)
            {
                agent.enabled = true;
                agent.Warp(end);
            }
            busy = false;
        }

        void OnDied(DamageInfo info)
        {
            StopAllCoroutines();
            if (agent.enabled && agent.isOnNavMesh) agent.ResetPath();
            agent.enabled = false;
            Rig.SetPose(HumanoidPose.Lying);
            Tentacles.SetRaise(0f);
            AudioManager.PlayAt(SoundBank.Screech, transform.position, 0.6f, 0.6f);
            if (info.sourceFaction == Faction.Player)
            {
                GameSession.Stats.offspringKilled++;
                GameSession.NotifyStatsChanged();
            }
            MissionEvents.RaiseOffspringKilled(this);
            GetComponent<Collider>().enabled = false;
        }
    }
}
