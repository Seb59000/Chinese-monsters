using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game
{
    /// <summary>Mission 1 "The Chase": patrol, pursuit, fight in the docks, sample, infected hosts.</summary>
    public class MissionDirector : MonoBehaviour
    {
        public static MissionDirector Instance { get; private set; }

        public CityLayout Layout;
        public Transform Characters;
        public PlayerController Player;
        public ThirdPersonCamera Cam;

        public MissionPhase Phase { get; private set; }
        public string ObjectiveKey { get; private set; } = "";
        public object[] ObjectiveArgs { get; private set; } = new object[0];
        public readonly List<Marker> Markers = new List<Marker>();

        public bool ChaseActive { get; private set; }
        public float LostTimer { get; private set; }
        public const float LostLimit = 12f;
        public const float LostDistance = 70f;

        public bool Failed { get; private set; }
        public string FailReasonKey { get; private set; }
        public bool Complete { get; private set; }
        public SuspectAI Suspect { get; private set; }
        public float ObjectiveChangedTime { get; private set; }

        public event Action OnFailed, OnCompleted;

        // A = alley victim, B and C = market, D = second alley.
        readonly CivilianAI[] storyHosts = new CivilianAI[4];
        readonly float[] incubationTimes = { 150f, 105f, 120f, 75f };
        readonly bool[] storyFemale = { true, true, false, true };
        GameObject sample;
        System.Random rnd;
        bool firstNeutralize = true, firstSave = true, firstHatch = true, firstCivilian = true;

        RadioDialogue Radio => RadioDialogue.Instance;

        void Awake() => Instance = this;

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Begin(MissionPhase from, int seed)
        {
            rnd = new System.Random(seed + 7);
            Player.Health.Died += _ => StartCoroutine(FailAfter("fail.dead", 1.6f));
            MissionEvents.HostResolved += OnHostResolved;
            MissionEvents.CivilianKilled += OnCivilianKilled;
            StartCoroutine(Run(from));
        }

        void Update()
        {
            if (!Failed && !Complete) GameSession.Stats.missionTime += Time.deltaTime;
        }

        // ---------------------------------------------------------------- flow

        IEnumerator Run(MissionPhase from)
        {
            SpawnStoryHosts(from);
            if (from <= MissionPhase.Chase)
            {
                Suspect = CharacterFactory.SpawnSuspect(Characters, Layout.suspectStart);
                Suspect.InfectionTarget = waypoint => waypoint == 5 ? storyHosts[1] : waypoint == 6 ? storyHosts[2] : waypoint == 10 ? storyHosts[3] : null;
                if (storyHosts[0] != null)
                {
                    storyHosts[0].Cower(9999f);
                    Suspect.HoldVictim(storyHosts[0]);
                    Suspect.transform.rotation = Quaternion.LookRotation(storyHosts[0].transform.position - Suspect.transform.position);
                }
            }

            if (from == MissionPhase.Patrol) yield return Patrol();
            if (from <= MissionPhase.Chase) yield return Chase();
            if (Failed) yield break;
            if (from <= MissionPhase.Fight) yield return Fight(from == MissionPhase.Fight);
            if (Failed) yield break;
            if (from <= MissionPhase.Sample) yield return Sample();
            if (Failed) yield break;
            yield return Hosts();
            if (Failed) yield break;
            yield return Finish();
        }

        IEnumerator Patrol()
        {
            Phase = MissionPhase.Patrol;
            GameSession.SetCheckpoint(MissionPhase.Patrol);
            SetObjective("obj.patrol");
            AddMarker(null, Layout.patrolPoint, "marker.alley");
            yield return new WaitForSeconds(1.5f);
            Radio.Say("spk.lin", "line.patrol1");
            Radio.Say("spk.you", "line.patrol2");
            Radio.Say("spk.lin", "line.patrol3");
            // Arriving at the alley, getting close, or shooting at him all trigger the confrontation.
            while (DistanceToPlayer(Layout.patrolPoint) > 7f && DistanceToPlayer(Suspect.transform.position) > 17f &&
                   Suspect.Health.Current >= Suspect.Health.max)
                yield return null;
        }

        IEnumerator Chase()
        {
            Phase = MissionPhase.Chase;
            Checkpoint(MissionPhase.Chase);
            Radio.Clear();
            ClearMarkers();
            yield return Radio.Play("spk.you", "line.confront");
            Suspect.StartChase(Layout.chaseRoute, Layout.infectionWaypoints);
            SetObjective("obj.chase");
            AddMarker(Suspect.transform, Vector3.zero, "marker.suspect").color = new Color(1f, 0.3f, 0.25f);
            ChaseActive = true;
            AudioManager.Instance.SetTension(0.8f);

            Radio.Say("spk.lin", "line.chase1");
            bool saidFast = false, saidMarket = false, saidTentacles = false, saidDocks = false;
            while (Suspect.Mode == SuspectMode.Chase)
            {
                float d = Suspect.DistanceToPlayer;
                LostTimer = d > LostDistance ? LostTimer + Time.deltaTime : Mathf.Max(0f, LostTimer - Time.deltaTime * 2f);
                if (LostTimer > LostLimit)
                {
                    ChaseActive = false;
                    yield return FailAfter("fail.lost", 0f);
                    yield break;
                }

                var p = Suspect.transform.position;
                if (!saidFast && DistanceTo(p, Layout.chaseRoute[2]) < 6f) { saidFast = true; Radio.Say("spk.you", "line.chase2"); }
                if (!saidMarket && DistanceTo(p, Layout.chaseRoute[4]) < 10f) { saidMarket = true; Radio.Say("spk.lin", "line.market"); }
                if (!saidTentacles && storyHosts[1] != null && storyHosts[1].IsInfected) { saidTentacles = true; Radio.Say("spk.you", "line.tentacles"); }
                if (!saidDocks && DistanceTo(p, Layout.chaseRoute[11]) < 8f) { saidDocks = true; Radio.Say("spk.lin", "line.docks"); }
                yield return null;
            }
            ChaseActive = false;
            LostTimer = 0f;

            SetObjective("obj.enter_docks");
            while (DistanceToPlayer(Layout.arenaCenter) > 22f) yield return null;
        }

        IEnumerator Fight(bool fromCheckpoint)
        {
            Phase = MissionPhase.Fight;
            if (fromCheckpoint)
            {
                Suspect = CharacterFactory.SpawnSuspect(Characters, Layout.arenaCenter);
            }
            Checkpoint(MissionPhase.Fight);
            ClearMarkers();
            SetObjective("obj.fight");
            AudioManager.Instance.SetTension(1f);

            Vector3? drop = null;
            bool gone = false;
            Suspect.EscapeStarted += pos => drop = pos;
            Suspect.Escaped += () => gone = true;
            Suspect.FightStage += s => Radio.Say(s == 1 ? "spk.lin" : "spk.t07", s == 1 ? "line.alone" : "line.others");

            yield return Radio.Play("spk.you", "line.fight1", "spk.t07", "line.fight2");
            Suspect.BeginFight(Layout.escapePoint);

            while (drop == null) yield return null;
            Radio.Clear();
            Radio.Say("spk.you", "line.escape1");
            Radio.Say("spk.lin", "line.escape2");
            while (!gone) yield return null;
            GameSession.SamplePosition = drop.Value;
            AudioManager.Instance.SetTension(0.4f);
        }

        IEnumerator Sample()
        {
            Phase = MissionPhase.Sample;
            Checkpoint(MissionPhase.Sample);
            var pos = GameSession.SamplePosition ?? Layout.arenaCenter;
            if (NavMeshBaker.Snap(pos, out var snapped, 3f)) pos = snapped;
            sample = CreateSample(pos);
            ClearMarkers();
            SetObjective("obj.sample");
            AddMarker(sample.transform, Vector3.zero, "marker.sample").offset = Vector3.up * 1.2f;

            while (DistanceToPlayer(sample.transform.position) > 1.8f) yield return null;
            AudioManager.Play2D(SoundBank.Click, 1f);
            Destroy(sample);
            ClearMarkers();
            SetObjective("obj.listen");

            yield return Radio.Play(
                "spk.shen", "line.shen1",
                "spk.shen", "line.shen2",
                "spk.shen", "line.shen3",
                "spk.zhao", "line.zhao1",
                "spk.shen", "line.shen4",
                "spk.shen", "line.shen5",
                "spk.shen", "line.shen6",
                "spk.zhao", "line.zhao2");
        }

        IEnumerator Hosts()
        {
            Phase = MissionPhase.Hosts;
            Checkpoint(MissionPhase.Hosts);
            Radio.Say("spk.shen", "line.shen7");
            AudioManager.Instance.SetTension(0.7f);

            for (int i = 0; i < storyHosts.Length; i++)
            {
                var c = storyHosts[i];
                if (c != null && c.IsInfected && !c.Health.IsDead && !c.Host.IsResolved)
                    c.Host.BeginIncubation(incubationTimes[i]);
            }

            while (true)
            {
                int hosts = 0, offspring = 0;
                ClearMarkers();
                foreach (var c in storyHosts)
                {
                    if (c == null || !c.IsInfected || c.Host.IsResolved) continue;
                    hosts++;
                    AddMarker(c.transform, Vector3.zero, null).labelText = c.DisplayName;
                    Markers[Markers.Count - 1].color = new Color(0.8f, 0.4f, 1f);
                    Markers[Markers.Count - 1].offset = Vector3.up * 2f;
                }
                foreach (var o in OffspringAI.All)
                {
                    if (o == null || !o.IsAlive) continue;
                    offspring++;
                    AddMarker(o.transform, Vector3.zero, "marker.offspring").color = new Color(1f, 0.25f, 0.2f);
                }

                if (hosts == 0 && offspring == 0) break;
                if (offspring > 0 && hosts > 0) SetObjective("obj.hosts_offspring", hosts, offspring);
                else if (offspring > 0) SetObjective("obj.offspring", offspring);
                else SetObjective("obj.hosts", hosts);
                yield return new WaitForSeconds(0.25f);
            }
            ClearMarkers();
        }

        IEnumerator Finish()
        {
            Phase = MissionPhase.Complete;
            SetObjective("obj.done");
            AudioManager.Instance.SetTension(0.2f);
            yield return Radio.Play("spk.zhao", "line.end1", "spk.shen", "line.end2");
            yield return new WaitForSeconds(1f);
            Complete = true;
            Player.ControlsEnabled = false;
            OnCompleted?.Invoke();
        }

        IEnumerator FailAfter(string reasonKey, float delay)
        {
            if (Failed || Complete) yield break;
            Failed = true;
            FailReasonKey = reasonKey;
            Player.ControlsEnabled = false;
            if (delay > 0f) yield return new WaitForSeconds(delay);
            OnFailed?.Invoke();
        }

        // ---------------------------------------------------------------- reactions

        void OnHostResolved(InfectedHost host)
        {
            if (Phase < MissionPhase.Sample) return;
            switch (host.State)
            {
                case HostState.Neutralized when firstNeutralize:
                    firstNeutralize = false;
                    Radio.Say("spk.lin", "line.neutralized");
                    break;
                case HostState.Saved when firstSave:
                    firstSave = false;
                    Radio.Say("spk.shen", host.Civilian.IsFemale ? "line.saved_f" : "line.saved_m");
                    break;
                case HostState.Hatched when firstHatch:
                    firstHatch = false;
                    Radio.Say("spk.lin", "line.hatched");
                    break;
            }
        }

        void OnCivilianKilled(CivilianAI c)
        {
            if (!firstCivilian) return;
            firstCivilian = false;
            Radio.Say("spk.lin", "line.civilian_killed");
        }

        // ---------------------------------------------------------------- helpers

        void SpawnStoryHosts(MissionPhase from)
        {
            for (int i = 0; i < storyHosts.Length; i++)
            {
                bool restoring = from >= MissionPhase.Fight;
                if (restoring && (GameSession.StoryDeadMask & (1 << i)) != 0) continue;
                var c = CharacterFactory.SpawnCivilian(Characters, Layout.hostPositions[i], rnd, storyFemale[i]);
                c.StoryLocked = true;
                storyHosts[i] = c;
                bool infected = i == 0 ? from >= MissionPhase.Fight : restoring && (GameSession.StoryInfectedMask & (1 << i)) != 0;
                if (infected) c.Infect();
            }
        }

        void Checkpoint(MissionPhase phase)
        {
            int infected = 0, dead = 0;
            for (int i = 0; i < storyHosts.Length; i++)
            {
                var c = storyHosts[i];
                if (c == null || c.Health.IsDead) dead |= 1 << i;
                else if (c.IsInfected) infected |= 1 << i;
            }
            GameSession.StoryInfectedMask = infected;
            GameSession.StoryDeadMask = dead;
            GameSession.SetCheckpoint(phase);
        }

        void SetObjective(string key, params object[] args)
        {
            if (key != ObjectiveKey) ObjectiveChangedTime = Time.time;
            ObjectiveKey = key;
            ObjectiveArgs = args;
        }

        public string ObjectiveText => string.IsNullOrEmpty(ObjectiveKey) ? "" : L.Format(ObjectiveKey, ObjectiveArgs);

        Marker AddMarker(Transform target, Vector3 point, string labelKey)
        {
            var m = new Marker { target = target, point = point, labelKey = labelKey };
            Markers.Add(m);
            return m;
        }

        void ClearMarkers() => Markers.Clear();

        float DistanceToPlayer(Vector3 p) => DistanceTo(Player.transform.position, p);

        static float DistanceTo(Vector3 a, Vector3 b)
        {
            a.y = b.y = 0f;
            return Vector3.Distance(a, b);
        }

        GameObject CreateSample(Vector3 pos)
        {
            var root = new GameObject("TissueSample");
            root.transform.SetParent(Characters, false);
            root.transform.position = pos + Vector3.up * 0.6f;
            var vial = MeshUtil.Visual(PrimitiveType.Capsule, root.transform, Vector3.zero, new Vector3(0.15f, 0.2f, 0.15f),
                MaterialFactory.Emissive(new Color(0.5f, 0.1f, 0.6f), new Color(0.8f, 0.2f, 1f), 3f));
            vial.AddComponent<Spinner>();
            CityProps.AddLight(root.transform, root.transform.position, new Color(0.7f, 0.2f, 1f), 6f, 2f);
            return root;
        }
    }
}
