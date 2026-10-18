using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Game
{
    public enum SuspectMode { Holding, Chase, WaitingInArena, Fight, Escape, Gone }

    /// <summary>
    /// Subject T-07: the failed genetic edit. Runs the scripted chase route (rubber-banded to the player),
    /// then fights in the docks yard and escapes over the warehouse roof.
    /// </summary>
    public class SuspectAI : MonoBehaviour
    {
        public static SuspectAI Instance { get; private set; }

        public HumanoidRig Rig;
        public TentacleRig Tentacles;
        public SuspectMode Mode { get; private set; } = SuspectMode.Holding;
        public Health Health { get; private set; }

        /// <summary>Asks the director which civilian to infect at a given route waypoint.</summary>
        public Func<int, CivilianAI> InfectionTarget;
        public event Action ReachedArena;
        public event Action<int> FightStage;
        public event Action<Vector3> EscapeStarted;
        public event Action Escaped;

        public float DistanceToPlayer => PlayerController.Instance == null ? 0f :
            Vector3.Distance(transform.position, PlayerController.Instance.transform.position);
        public bool IsWaitingForPlayer { get; private set; }

        NavMeshAgent agent;
        List<Vector3> route;
        HashSet<int> infectionPoints = new HashSet<int>();
        int routeIndex;
        bool busy;
        CivilianAI held;
        Vector3 escapePoint;
        float staggerUntil, whipReady, leapReady, strikeReady;
        float rage = 1f;
        int stage;
        Vector3 lastPos;
        float speedEstimate;
        bool airborne;
        float nextAlert;

        void Awake()
        {
            Instance = this;
            Health = GetComponent<Health>();
            agent = GetComponent<NavMeshAgent>();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Start()
        {
            Health.Damaged += OnDamaged;
            lastPos = transform.position;
        }

        // ---------- Setup by the mission ----------

        public void HoldVictim(CivilianAI victim)
        {
            held = victim;
            Mode = SuspectMode.Holding;
        }

        public void StartChase(List<Vector3> chaseRoute, int[] infectAt, int startIndex = 0)
        {
            route = chaseRoute;
            infectionPoints = new HashSet<int>(infectAt);
            routeIndex = startIndex;
            Health.floor = Health.max * 0.7f;
            Mode = SuspectMode.Chase;
            if (held != null)
            {
                if (!held.Health.IsDead) held.Infect();
                held = null;
                Tentacles.Hold(transform.position, 0f);
            }
            AudioManager.PlayAt(SoundBank.Screech, transform.position, 1f);
            agent.updateRotation = true;
            GoTo(route[routeIndex]);
        }

        public void BeginFight(Vector3 escapeRoof)
        {
            escapePoint = escapeRoof;
            Mode = SuspectMode.Fight;
            Health.floor = Health.max * 0.25f;
            Health.invulnerable = false;
            agent.updateRotation = false;
            StartCoroutine(FightLoop());
        }

        // ---------- Frame update ----------

        void Update()
        {
            var flat = transform.position - lastPos;
            flat.y = 0f;
            speedEstimate = Mathf.Lerp(speedEstimate, flat.magnitude / Mathf.Max(Time.deltaTime, 0.0001f), 0.2f);
            lastPos = transform.position;
            Rig.Animate(airborne ? 2f : speedEstimate);

            switch (Mode)
            {
                case SuspectMode.Holding:
                    if (held != null)
                    {
                        Face(held.transform.position);
                        Tentacles.Hold(held.transform.position + Vector3.up * 1.0f, 0.85f + Mathf.Sin(Time.time * 3f) * 0.1f);
                    }
                    break;
                case SuspectMode.Chase:
                    UpdateChase();
                    break;
                case SuspectMode.WaitingInArena:
                    if (PlayerController.Instance != null) Face(PlayerController.Instance.transform.position);
                    Tentacles.SetRaise(0.4f + Mathf.Sin(Time.time * 2f) * 0.2f);
                    break;
                case SuspectMode.Fight:
                    if (PlayerController.Instance != null && !airborne) Face(PlayerController.Instance.transform.position);
                    break;
            }
        }

        void UpdateChase()
        {
            if (busy || !agent.enabled || !agent.isOnNavMesh) return;
            float d = DistanceToPlayer;
            if (Time.time > nextAlert)
            {
                CivilianAI.Alert(transform.position, 14f, transform.position);
                nextAlert = Time.time + 0.5f;
            }

            // Rubber band: always faster than a sprinting cop up close, waits when far behind.
            float speed = d < 12f ? 11.5f : d < 26f ? 9.5f : d < 42f ? 7f : 4f;
            if (Time.time < staggerUntil) speed *= 0.35f;
            IsWaitingForPlayer = d > 52f;
            agent.isStopped = IsWaitingForPlayer;
            agent.speed = speed;
            if (IsWaitingForPlayer && PlayerController.Instance != null)
            {
                agent.updateRotation = false;
                Face(PlayerController.Instance.transform.position);
            }
            else agent.updateRotation = true;

            if (!agent.pathPending && agent.remainingDistance < 1.6f)
            {
                if (infectionPoints.Contains(routeIndex)) StartCoroutine(InfectOnTheRun(routeIndex));
                routeIndex++;
                if (routeIndex >= route.Count)
                {
                    Mode = SuspectMode.WaitingInArena;
                    agent.ResetPath();
                    agent.updateRotation = false;
                    ReachedArena?.Invoke();
                    return;
                }
                GoTo(route[routeIndex]);
            }
        }

        IEnumerator InfectOnTheRun(int waypoint)
        {
            var target = InfectionTarget?.Invoke(waypoint);
            if (target == null || target.Health.IsDead || target.IsInfected) yield break;
            busy = true;
            agent.speed = 1.5f;
            AudioManager.PlayAt(SoundBank.Whoosh, transform.position, 1f, 0.7f);
            float t = 0f;
            while (t < 0.8f && target != null)
            {
                t += Time.deltaTime;
                Tentacles.Hold(target.transform.position + Vector3.up * 1.05f, Mathf.Clamp01(t / 0.2f));
                yield return null;
            }
            if (target != null && !target.Health.IsDead)
            {
                target.Infect();
                AudioManager.PlayAt(SoundBank.Squelch, target.transform.position, 0.8f);
            }
            Tentacles.Hold(transform.position, 0f);
            busy = false;
        }

        void OnDamaged(DamageInfo info)
        {
            if (Mode == SuspectMode.Chase)
            {
                staggerUntil = Time.time + 0.35f;
                if (UnityEngine.Random.value < 0.25f) AudioManager.PlayAt(SoundBank.Screech, transform.position, 0.6f, 1.3f);
            }
            if (Mode != SuspectMode.Fight) return;

            float hp = Health.Normalized;
            if (stage == 0 && hp < 0.7f) { stage = 1; rage = 1.2f; FightStage?.Invoke(1); }
            else if (stage == 1 && hp < 0.45f) { stage = 2; rage = 1.4f; FightStage?.Invoke(2); }
        }

        // ---------- Fight ----------

        IEnumerator FightLoop()
        {
            yield return Roar();
            whipReady = Time.time + 2f;
            leapReady = Time.time + 6f;
            while (Mode == SuspectMode.Fight)
            {
                if (Health.Current <= Health.floor + 0.5f)
                {
                    StartCoroutine(EscapeRoutine());
                    yield break;
                }
                var player = PlayerController.Instance;
                if (player == null || player.Health.IsDead)
                {
                    agent.ResetPath();
                    yield return null;
                    continue;
                }

                float d = DistanceToPlayer;
                if (d > 14f) yield return Dash();
                else if (Time.time > leapReady && UnityEngine.Random.value < 0.4f) yield return Leap();
                else if (d < 7f && Time.time > whipReady) yield return Whip();
                else if (d < 2.6f && Time.time > strikeReady) yield return Strike();
                else yield return Strafe(UnityEngine.Random.Range(1.0f, 2.0f) / rage);
            }
        }

        IEnumerator Roar()
        {
            AudioManager.PlayAt(SoundBank.Screech, transform.position, 1f, 0.9f);
            ShakeCamera(0.35f);
            float t = 0f;
            while (t < 1.1f)
            {
                t += Time.deltaTime;
                Tentacles.SetRaise(Mathf.Sin(t / 1.1f * Mathf.PI));
                yield return null;
            }
            Tentacles.SetRaise(0f);
        }

        IEnumerator Strafe(float duration)
        {
            var player = PlayerController.Instance.transform.position;
            var from = transform.position - player;
            from.y = 0f;
            float angle = Mathf.Atan2(from.z, from.x) + UnityEngine.Random.Range(0.6f, 1.2f) * (UnityEngine.Random.value < 0.5f ? -1f : 1f);
            float radius = UnityEngine.Random.Range(6f, 10f);
            var target = player + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
            agent.speed = 8f * rage;
            GoTo(target);
            float end = Time.time + duration;
            while (Time.time < end && Mode == SuspectMode.Fight) yield return null;
        }

        IEnumerator Dash()
        {
            agent.speed = 17f * rage;
            agent.acceleration = 80f;
            AudioManager.PlayAt(SoundBank.Whoosh, transform.position, 1f);
            float end = Time.time + 1.4f;
            while (Time.time < end && DistanceToPlayer > 2.3f)
            {
                GoTo(PlayerController.Instance.transform.position);
                yield return null;
            }
            agent.acceleration = 40f;
            if (DistanceToPlayer < 2.8f) yield return Strike();
        }

        IEnumerator Strike()
        {
            agent.ResetPath();
            Tentacles.SetRaise(0.6f);
            yield return new WaitForSeconds(0.28f / rage);
            var player = PlayerController.Instance;
            if (player != null)
            {
                Tentacles.Lash(player.transform.position + Vector3.up * 1.1f, 0.05f);
                if (DistanceToPlayer < 2.9f) HitPlayer(14f, 6f);
            }
            Tentacles.SetRaise(0f);
            strikeReady = Time.time + 1.1f / rage;
            yield return new WaitForSeconds(0.35f);
        }

        IEnumerator Whip()
        {
            agent.ResetPath();
            AudioManager.PlayAt(SoundBank.Screech, transform.position, 0.8f, 1.2f);
            float t = 0f, tell = 0.7f / rage;
            while (t < tell)
            {
                t += Time.deltaTime;
                Tentacles.SetRaise(t / tell);
                yield return null;
            }
            var player = PlayerController.Instance;
            if (player != null)
            {
                var target = player.transform.position + Vector3.up * 1.1f;
                Tentacles.Lash(target, 0.12f);
                AudioManager.PlayAt(SoundBank.Whoosh, transform.position, 1f, 1.3f);
                var to = player.transform.position - transform.position;
                to.y = 0f;
                if (to.magnitude < 7.5f && Vector3.Angle(transform.forward, to) < 70f) HitPlayer(20f, 9f);
            }
            yield return new WaitForSeconds(0.2f);
            Tentacles.SetRaise(0f);
            whipReady = Time.time + 3.2f / rage;
            yield return new WaitForSeconds(0.45f);
        }

        IEnumerator Leap()
        {
            leapReady = Time.time + 9f / rage;
            var player = PlayerController.Instance.transform.position;
            var landing = player + new Vector3(UnityEngine.Random.Range(-2.5f, 2.5f), 0f, UnityEngine.Random.Range(-2.5f, 2.5f));
            if (NavMeshBaker.Snap(landing, out var snapped, 4f)) landing = snapped;
            var ring = TelegraphRing(landing, 4f);
            AudioManager.PlayAt(SoundBank.Whoosh, transform.position, 1f, 0.8f);
            yield return Arc(landing, 0.9f / Mathf.Sqrt(rage), 6f);
            Destroy(ring);
            AudioManager.PlayAt(SoundBank.Impact, landing, 1f);
            ShakeCamera(0.7f);
            TransientFx.Burst(landing + Vector3.up * 0.2f, new Color(0.4f, 0.38f, 0.35f), 10, 0.3f, 6f);
            var p = PlayerController.Instance;
            if (p != null && Vector3.Distance(p.transform.position, landing) < 4f) HitPlayer(18f, 10f);
            CivilianAI.Alert(landing, 30f, landing);
            yield return new WaitForSeconds(0.6f);
        }

        IEnumerator EscapeRoutine()
        {
            Mode = SuspectMode.Escape;
            Health.invulnerable = true;
            EscapeStarted?.Invoke(transform.position);
            yield return Roar();
            yield return Arc(escapePoint, 1.3f, 9f, false);
            yield return new WaitForSeconds(0.6f);
            var away = escapePoint + new Vector3(25f, 6f, 25f);
            yield return Arc(away, 1.1f, 6f, false);
            Mode = SuspectMode.Gone;
            Escaped?.Invoke();
            gameObject.SetActive(false);
        }

        IEnumerator Arc(Vector3 to, float duration, float height, bool landOnNavMesh = true)
        {
            airborne = true;
            if (agent.enabled && agent.isOnNavMesh) agent.ResetPath();
            agent.enabled = false;
            var from = transform.position;
            var flat = to - from;
            flat.y = 0f;
            if (flat.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(flat);
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / duration;
                var p = Vector3.Lerp(from, to, Mathf.Clamp01(t));
                p.y += Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI) * height;
                transform.position = p;
                Tentacles.SetRaise(0.8f);
                yield return null;
            }
            Tentacles.SetRaise(0f);
            airborne = false;
            if (landOnNavMesh)
            {
                agent.enabled = true;
                if (NavMeshBaker.Snap(to, out var snapped, 3f)) agent.Warp(snapped);
            }
        }

        // ---------- Helpers ----------

        void GoTo(Vector3 target)
        {
            if (agent.enabled && agent.isOnNavMesh)
            {
                agent.isStopped = false;
                agent.SetDestination(target);
            }
        }

        void Face(Vector3 point)
        {
            var to = point - transform.position;
            to.y = 0f;
            if (to.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to), Time.deltaTime * 10f);
        }

        void HitPlayer(float damage, float knockback)
        {
            var player = PlayerController.Instance;
            if (player == null) return;
            var dir = player.transform.position - transform.position;
            dir.y = 0f;
            dir.Normalize();
            player.Health.TakeDamage(new DamageInfo(damage, player.transform.position + Vector3.up, dir, gameObject, Faction.Mutant));
            player.AddImpulse(dir * knockback + Vector3.up * 2f);
            AudioManager.PlayAt(SoundBank.Hit, player.transform.position, 1f, 0.7f);
            ShakeCamera(0.5f);
        }

        static void ShakeCamera(float amount)
        {
            var cam = Camera.main != null ? Camera.main.GetComponent<ThirdPersonCamera>() : null;
            if (cam != null) cam.Shake(amount);
        }

        GameObject TelegraphRing(Vector3 position, float radius)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Destroy(go.GetComponent<Collider>());
            go.name = "LeapWarning";
            go.transform.SetParent(transform.parent, false);
            go.transform.position = position + Vector3.up * 0.05f;
            go.transform.localScale = new Vector3(radius * 2f, 0.01f, radius * 2f);
            go.GetComponent<Renderer>().sharedMaterial = MaterialFactory.Glow(new Color(1f, 0.1f, 0.05f, 0.35f));
            return go;
        }
    }
}
