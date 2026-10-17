using System.Collections.Generic;
using UnityEngine;

namespace Game
{
    public enum HostState { Dormant, Incubating, Saved, Neutralized, Hatched }

    /// <summary>
    /// A civilian carrying the parasite. In women it triggers an accelerated gestation, in men it grows
    /// inside the body; either way a new mutant is born when the incubation completes.
    /// The player can extract it (slow) or neutralise the host (fast).
    /// </summary>
    public class InfectedHost : MonoBehaviour
    {
        public static readonly List<InfectedHost> All = new List<InfectedHost>();

        public CivilianAI Civilian;
        public float incubationDuration = 90f;
        public float extractionDuration = 6f;

        public HostState State { get; private set; } = HostState.Dormant;
        public bool IsResolved => State >= HostState.Saved;
        public float Incubation01 { get; private set; }
        public float Extraction01 { get; private set; }
        public bool BeingExtracted { get; private set; }

        Light glow;
        Transform parasite;
        Material parasiteMat;

        Vector3 ParasitePosition => parasite != null ? parasite.position : transform.position + Vector3.up;

        void Awake() => All.Add(this);
        void OnDestroy() => All.Remove(this);

        void Start()
        {
            Civilian.Health.Died += OnDied;
            Civilian.Rig.SetPose(HumanoidPose.Kneeling);

            // Pulsing growth: low on the abdomen for women (gestation), on the upper back otherwise.
            parasiteMat = MaterialFactory.GlowInstance(new Color(0.75f, 0.2f, 1f, 0.85f));
            var local = Civilian.IsFemale ? new Vector3(0f, 1.0f, 0.13f) : new Vector3(0f, 1.32f, -0.13f);
            var go = MeshUtil.Visual(PrimitiveType.Sphere, Civilian.Rig.visualRoot, local, Vector3.one * 0.3f, parasiteMat);
            go.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            parasite = go.transform;
            glow = CityProps.AddLight(transform, transform.position + Vector3.up * 1.1f, new Color(0.7f, 0.2f, 1f), 5f, 1.2f);
            MissionEvents.RaiseHostInfected(this);
        }

        public void BeginIncubation(float duration, float startProgress = 0f)
        {
            if (IsResolved) return;
            incubationDuration = duration;
            Incubation01 = startProgress;
            State = HostState.Incubating;
        }

        void Update()
        {
            if (IsResolved) return;
            if (State == HostState.Incubating)
            {
                float rate = BeingExtracted ? 0.25f : 1f;
                Incubation01 += Time.deltaTime * rate / incubationDuration;
                if (Incubation01 >= 1f)
                {
                    Hatch();
                    return;
                }
            }
            if (!BeingExtracted) Extraction01 = Mathf.MoveTowards(Extraction01, 0f, Time.deltaTime * 0.04f);

            float speed = 2f + Incubation01 * 8f;
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * speed);
            if (parasite != null) parasite.localScale = Vector3.one * (0.3f + Incubation01 * 0.25f + pulse * 0.06f);
            if (glow != null) glow.intensity = 0.6f + pulse * (0.8f + Incubation01 * 1.5f);
            BeingExtracted = false;
        }

        /// <summary>Advances the extraction; returns progress 0..1.</summary>
        public float ExtractTick(float dt, GameObject by)
        {
            if (IsResolved) return 1f;
            BeingExtracted = true;
            Extraction01 += dt / extractionDuration;
            if (Random.value < dt * 3f)
                TransientFx.Burst(ParasitePosition, new Color(0.6f, 0.15f, 0.8f), 1, 0.06f, 1f);
            if (Extraction01 >= 1f) Save();
            return Extraction01;
        }

        public void CancelExtraction() => BeingExtracted = false;

        public void Neutralize(GameObject by) => Civilian.Health.Kill(by, Faction.Player);

        void OnDied(DamageInfo info)
        {
            if (IsResolved) return;
            Resolve(HostState.Neutralized);
            GameSession.Stats.hostsNeutralized++;
            GameSession.NotifyStatsChanged();
        }

        void Save()
        {
            Extraction01 = 1f;
            TransientFx.Burst(ParasitePosition, new Color(0.4f, 0.05f, 0.5f), 8, 0.12f, 2f);
            AudioManager.PlayAt(SoundBank.Squelch, transform.position, 0.8f, 1.3f);
            Resolve(HostState.Saved);
            GameSession.Stats.hostsSaved++;
            GameSession.NotifyStatsChanged();
            Civilian.Recover();
        }

        void Hatch()
        {
            Incubation01 = 1f;
            TransientFx.Burst(ParasitePosition, new Color(0.35f, 0.02f, 0.2f), 14, 0.18f, 4f);
            AudioManager.PlayAt(SoundBank.Squelch, transform.position, 1f, 0.8f);
            AudioManager.PlayAt(SoundBank.Screech, transform.position, 0.7f, 1.6f);
            Resolve(HostState.Hatched);
            GameSession.Stats.offspringBorn++;
            GameSession.NotifyStatsChanged();

            // The host survives, unconscious; the newborn hunts.
            Civilian.Health.invulnerable = true;
            Civilian.Rig.SetPose(HumanoidPose.Lying);
            var spawn = transform.position + transform.forward * 1.2f;
            CharacterFactory.SpawnOffspring(transform.parent, spawn);
        }

        void Resolve(HostState state)
        {
            State = state;
            BeingExtracted = false;
            if (parasite != null) Destroy(parasite.gameObject);
            if (glow != null) Destroy(glow.gameObject);
            MissionEvents.RaiseHostResolved(this);
        }

        public string StatusKey => Civilian.IsFemale ? "host.gestation" : "host.incubation";
    }
}
