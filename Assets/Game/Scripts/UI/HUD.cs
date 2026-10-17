using UnityEngine;

namespace Game
{
    /// <summary>In-game overlay: objective, markers, chase meter, boss bar, subtitles, health, ammo, prompts.</summary>
    public class HUD : MonoBehaviour
    {
        PlayerController player;
        PlayerShooter shooter;
        PlayerInteractor interactor;
        float hitMarkerUntil, damageFlash, startTime;
        bool hitWasKill;

        void Start() => startTime = Time.time;

        void Bind()
        {
            if (player == PlayerController.Instance && player != null) return;
            player = PlayerController.Instance;
            if (player == null) return;
            shooter = player.GetComponent<PlayerShooter>();
            interactor = player.GetComponent<PlayerInteractor>();
            shooter.HitConfirmed += kill =>
            {
                hitMarkerUntil = Time.time + 0.18f;
                hitWasKill = kill;
            };
            player.Health.Damaged += _ => damageFlash = 1f;
        }

        void Update()
        {
            Bind();
            damageFlash = Mathf.MoveTowards(damageFlash, 0f, Time.deltaTime * 1.6f);
        }

        static string Key(string keyboard, string pad) =>
            GameInput.Instance != null && GameInput.Instance.UsingGamepad ? pad : keyboard;

        void OnGUI()
        {
            GUI.depth = 10;
            if (player == null || GameBootstrap.IsLoading) return;
            if (GameMenus.Instance != null && GameMenus.Instance.IsOpen) return;
            var director = MissionDirector.Instance;
            var cam = Camera.main;
            if (director == null || cam == null) return;
            float W = UIDraw.W, H = UIDraw.H;

            DrawVignette(W, H);
            DrawMarkers(director, cam, W, H);
            DrawHostLabels(cam);
            DrawObjective(director);
            DrawChase(director, W);
            DrawBoss(director, W);
            DrawSubtitles(W, H);
            DrawVitals(H);
            DrawAmmo(W, H);
            DrawCrosshair(W, H);
            DrawPrompt(W, H);
            DrawHelp(W);

            if (!GameInput.HasAnyDevice)
                UIDraw.Text(new Rect(0f, H * 0.5f - 60f, W, 120f), L.Get("warn.input"), 30, UIDraw.Danger, TextAnchor.MiddleCenter, true, true);
        }

        void DrawVignette(float W, float H)
        {
            float low = Mathf.Clamp01((0.4f - player.Health.Normalized) / 0.4f);
            float a = Mathf.Max(damageFlash * 0.45f, low * (0.25f + 0.1f * Mathf.Sin(Time.time * 6f)));
            if (a <= 0.01f) return;
            var c = new Color(0.6f, 0f, 0f, a);
            UIDraw.Box(new Rect(0f, 0f, W, 70f), c);
            UIDraw.Box(new Rect(0f, H - 70f, W, 70f), c);
            UIDraw.Box(new Rect(0f, 0f, 90f, H), c);
            UIDraw.Box(new Rect(W - 90f, 0f, 90f, H), c);
            UIDraw.Box(new Rect(0f, 0f, W, H), new Color(0.5f, 0f, 0f, a * 0.25f));
        }

        void DrawObjective(MissionDirector d)
        {
            var text = d.ObjectiveText;
            if (string.IsNullOrEmpty(text)) return;
            float fresh = Mathf.Clamp01(1f - (Time.time - d.ObjectiveChangedTime) / 3f);
            float h = UIDraw.TextHeight(text, 27, 560f) + 48f;
            UIDraw.Box(new Rect(30f, 30f, 600f, h), new Color(0f, 0f, 0f, 0.4f + fresh * 0.2f));
            UIDraw.Box(new Rect(30f, 30f, 5f, h), Color.Lerp(UIDraw.Accent, Color.white, fresh));
            UIDraw.Text(new Rect(50f, 38f, 560f, 24f), L.Get("hud.objective"), 18, UIDraw.Accent, TextAnchor.UpperLeft, true);
            UIDraw.Text(new Rect(50f, 64f, 560f, h), text, 27, Color.white, TextAnchor.UpperLeft, false, true);
        }

        void DrawChase(MissionDirector d, float W)
        {
            if (!d.ChaseActive || d.Suspect == null) return;
            float dist = d.Suspect.DistanceToPlayer;
            var r = new Rect(W * 0.5f - 230f, 34f, 460f, 30f);
            UIDraw.Text(r, L.Format("hud.suspect_distance", Mathf.RoundToInt(dist)), 26, Color.white, TextAnchor.MiddleCenter, true);
            float close = 1f - Mathf.Clamp01(dist / MissionDirector.LostDistance);
            var col = Color.Lerp(UIDraw.Danger, new Color(0.3f, 1f, 0.5f), close);
            UIDraw.Bar(new Rect(W * 0.5f - 230f, 70f, 460f, 8f), close, col);
            if (d.LostTimer > 0.1f)
            {
                int left = Mathf.CeilToInt(MissionDirector.LostLimit - d.LostTimer);
                UIDraw.Text(new Rect(W * 0.5f - 300f, 86f, 600f, 34f), L.Format("hud.losing", left), 26,
                    Color.Lerp(Color.white, UIDraw.Danger, Mathf.PingPong(Time.time * 3f, 1f)), TextAnchor.MiddleCenter, true);
            }
            else if (d.Suspect.IsWaitingForPlayer)
            {
                UIDraw.Text(new Rect(W * 0.5f - 300f, 86f, 600f, 34f), L.Get("hud.too_far"), 22, UIDraw.Accent, TextAnchor.MiddleCenter);
            }
        }

        void DrawBoss(MissionDirector d, float W)
        {
            var s = d.Suspect;
            if (s == null || (s.Mode != SuspectMode.Fight && s.Mode != SuspectMode.WaitingInArena) || d.Phase != MissionPhase.Fight) return;
            var r = new Rect(W * 0.5f - 380f, 40f, 760f, 30f);
            UIDraw.Text(r, L.Get("hud.boss_name"), 24, new Color(1f, 0.4f, 0.4f), TextAnchor.MiddleCenter, true);
            UIDraw.Bar(new Rect(W * 0.5f - 380f, 74f, 760f, 14f), s.Health.Normalized, new Color(0.75f, 0.1f, 0.2f));
            // The mark where he breaks off and flees.
            UIDraw.Box(new Rect(W * 0.5f - 380f + 760f * s.Health.floor / s.Health.max, 70f, 2f, 22f), Color.white);
        }

        void DrawSubtitles(float W, float H)
        {
            var radio = RadioDialogue.Instance;
            if (radio == null || !radio.HasLine) return;
            var line = radio.Current;
            string speaker = L.Get(line.speakerKey), text = L.Get(line.textKey);
            float width = Mathf.Min(1150f, W - 80f);
            float th = UIDraw.TextHeight(text, 28, width - 40f);
            var r = new Rect(W * 0.5f - width * 0.5f, H - 190f - th, width, th + 58f);
            UIDraw.Box(r, new Color(0f, 0f, 0f, 0.6f));
            UIDraw.Text(new Rect(r.x + 20f, r.y + 8f, width - 40f, 26f), speaker.ToUpperInvariant(), 20, RadioDialogue.SpeakerColor(line.speakerKey), TextAnchor.UpperLeft, true);
            UIDraw.Text(new Rect(r.x + 20f, r.y + 36f, width - 40f, th + 10f), text, 28, Color.white, TextAnchor.UpperLeft, false, true);
            UIDraw.Box(new Rect(r.x, r.yMax - 3f, width * radio.LineProgress01, 3f), RadioDialogue.SpeakerColor(line.speakerKey));
        }

        void DrawVitals(float H)
        {
            UIDraw.Text(new Rect(40f, H - 118f, 300f, 24f), L.Get("hud.health"), 18, new Color(1f, 1f, 1f, 0.8f), TextAnchor.UpperLeft, true);
            float hp = player.Health.Normalized;
            UIDraw.Bar(new Rect(40f, H - 92f, 380f, 16f), hp, Color.Lerp(UIDraw.Danger, new Color(0.9f, 0.95f, 1f), hp));
            UIDraw.Bar(new Rect(40f, H - 68f, 280f, 7f), player.Stamina01, new Color(0.3f, 0.75f, 1f, 0.9f));
        }

        void DrawAmmo(float W, float H)
        {
            if (shooter == null) return;
            UIDraw.Text(new Rect(W - 300f, H - 130f, 260f, 60f), shooter.Ammo + " / ∞", 48, Color.white, TextAnchor.LowerRight, true);
            UIDraw.Text(new Rect(W - 300f, H - 72f, 260f, 26f), L.Get("hud.pistol"), 18, new Color(1f, 1f, 1f, 0.7f), TextAnchor.UpperRight, true);
            if (shooter.Reloading)
            {
                UIDraw.Text(new Rect(W - 300f, H - 170f, 260f, 30f), L.Get("hud.reloading"), 20, UIDraw.Accent, TextAnchor.LowerRight, true);
                UIDraw.Bar(new Rect(W - 240f, H - 138f, 200f, 5f), shooter.ReloadProgress01, UIDraw.Accent);
            }
            else if (shooter.Ammo == 0)
            {
                UIDraw.Text(new Rect(W - 400f, H - 170f, 360f, 30f), L.Format("hud.reload_hint", Key("R", "Y")), 20, UIDraw.Danger, TextAnchor.LowerRight, true);
            }
        }

        void DrawCrosshair(float W, float H)
        {
            if (shooter == null || interactor.Current != HostAction.None || !player.ControlsEnabled) return;
            float cx = W * 0.5f, cy = H * 0.5f;
            float gap = 6f + shooter.SpreadDegrees * 6f;
            var c = new Color(1f, 1f, 1f, shooter.IsAiming ? 0.95f : 0.6f);
            UIDraw.Box(new Rect(cx - 1f, cy - 1f, 2f, 2f), c);
            UIDraw.Box(new Rect(cx - gap - 10f, cy - 1f, 10f, 2f), c);
            UIDraw.Box(new Rect(cx + gap, cy - 1f, 10f, 2f), c);
            UIDraw.Box(new Rect(cx - 1f, cy - gap - 10f, 2f, 10f), c);
            UIDraw.Box(new Rect(cx - 1f, cy + gap, 2f, 10f), c);
            if (Time.time < hitMarkerUntil)
            {
                var hc = hitWasKill ? UIDraw.Danger : Color.white;
                for (int i = 0; i < 4; i++)
                {
                    float sx = i % 2 == 0 ? -1f : 1f, sy = i < 2 ? -1f : 1f;
                    UIDraw.Box(new Rect(cx + sx * 12f - 3f, cy + sy * 12f - 3f, 6f, 6f), hc);
                }
            }
        }

        void DrawPrompt(float W, float H)
        {
            if (interactor == null || interactor.Focus == null || interactor.Focus.IsResolved) return;
            var host = interactor.Focus;
            float y = H * 0.5f + 90f;
            var r = new Rect(W * 0.5f - 330f, y, 660f, 150f);
            UIDraw.Box(r, new Color(0f, 0f, 0f, 0.55f));
            UIDraw.Text(new Rect(r.x, r.y + 10f, r.width, 30f), host.Civilian.DisplayName, 26, new Color(0.85f, 0.6f, 1f), TextAnchor.UpperCenter, true);
            UIDraw.Text(new Rect(r.x, r.y + 48f, r.width, 30f), L.Format("prompt.extract", Key("E", "X")), 24, Color.white, TextAnchor.UpperCenter);
            UIDraw.Text(new Rect(r.x, r.y + 80f, r.width, 30f), L.Format("prompt.neutralize", Key("F", "B")), 24, new Color(1f, 0.6f, 0.55f), TextAnchor.UpperCenter);
            if (interactor.Current != HostAction.None)
            {
                bool extract = interactor.Current == HostAction.Extract;
                UIDraw.Bar(new Rect(r.x + 30f, r.yMax - 22f, r.width - 60f, 10f), interactor.Progress01,
                    extract ? new Color(0.6f, 0.9f, 0.6f) : UIDraw.Danger);
            }
            else if (host.Extraction01 > 0.01f)
            {
                UIDraw.Bar(new Rect(r.x + 30f, r.yMax - 22f, r.width - 60f, 10f), host.Extraction01, new Color(0.6f, 0.9f, 0.6f, 0.5f));
            }
        }

        void DrawMarkers(MissionDirector d, Camera cam, float W, float H)
        {
            foreach (var m in d.Markers)
            {
                if (!m.Valid) continue;
                var world = m.Position;
                float dist = Vector3.Distance(player.transform.position, world);
                if (m.target != null && dist < 25f && m.target.GetComponent<InfectedHost>() != null) continue;
                bool front = UIDraw.WorldToUI(cam, world, out var p);
                bool onScreen = front && p.x > 50f && p.x < W - 50f && p.y > 50f && p.y < H - 50f;
                if (!onScreen)
                {
                    var center = new Vector2(W * 0.5f, H * 0.5f);
                    var dir = p - center;
                    if (!front) dir = -dir;
                    if (dir.sqrMagnitude < 0.01f) dir = Vector2.down;
                    dir.Normalize();
                    float sx = (W * 0.5f - 60f) / Mathf.Max(0.001f, Mathf.Abs(dir.x));
                    float sy = (H * 0.5f - 60f) / Mathf.Max(0.001f, Mathf.Abs(dir.y));
                    p = center + dir * Mathf.Min(sx, sy);
                }
                var c = m.color;
                UIDraw.Box(new Rect(p.x - 9f, p.y - 9f, 18f, 18f), new Color(0f, 0f, 0f, 0.6f));
                UIDraw.Box(new Rect(p.x - 6f, p.y - 6f, 12f, 12f), c);
                string label = m.Label;
                string text = string.IsNullOrEmpty(label) ? Mathf.RoundToInt(dist) + " m" : label + "  " + Mathf.RoundToInt(dist) + " m";
                UIDraw.Text(new Rect(p.x - 200f, p.y + 12f, 400f, 26f), text, 20, c, TextAnchor.UpperCenter, true);
            }
        }

        void DrawHostLabels(Camera cam)
        {
            foreach (var h in InfectedHost.All)
            {
                if (h == null || h.IsResolved) continue;
                var head = h.transform.position + Vector3.up * 2.1f;
                float dist = Vector3.Distance(player.transform.position, head);
                if (dist > 25f) continue;
                if (!UIDraw.WorldToUI(cam, head, out var p)) continue;
                string status = h.State == HostState.Dormant ? L.Get("host.dormant") : L.Format(h.StatusKey, Mathf.FloorToInt(h.Incubation01 * 100f));
                var r = new Rect(p.x - 160f, p.y - 64f, 320f, 64f);
                UIDraw.Box(r, new Color(0f, 0f, 0f, 0.45f));
                UIDraw.Text(new Rect(r.x, r.y + 4f, r.width, 24f), h.Civilian.DisplayName, 20, Color.white, TextAnchor.UpperCenter, true);
                UIDraw.Text(new Rect(r.x, r.y + 28f, r.width, 22f), status, 17, new Color(0.85f, 0.65f, 1f), TextAnchor.UpperCenter);
                if (h.State == HostState.Incubating)
                    UIDraw.Bar(new Rect(r.x + 20f, r.yMax - 10f, r.width - 40f, 5f), h.Incubation01,
                        Color.Lerp(new Color(0.7f, 0.3f, 1f), UIDraw.Danger, h.Incubation01));
            }
        }

        void DrawHelp(float W)
        {
            float age = Time.time - startTime;
            var d = MissionDirector.Instance;
            if (d == null || d.Phase != MissionPhase.Patrol || age > 40f) return;
            float a = Mathf.Clamp01((40f - age) / 3f);
            string help = L.Get(GameInput.Instance != null && GameInput.Instance.UsingGamepad ? "help.pad" : "help.kb");
            float h = UIDraw.TextHeight(help, 20, 360f) + 50f;
            var r = new Rect(W - 430f, 30f, 400f, h);
            UIDraw.Box(r, new Color(0f, 0f, 0f, 0.45f * a));
            UIDraw.Text(new Rect(r.x + 20f, r.y + 10f, 360f, 26f), L.Get("hud.controls"), 18, new Color(UIDraw.Accent.r, UIDraw.Accent.g, UIDraw.Accent.b, a), TextAnchor.UpperLeft, true);
            UIDraw.Text(new Rect(r.x + 20f, r.y + 38f, 360f, h), help, 20, new Color(1f, 1f, 1f, a), TextAnchor.UpperLeft, false, true);
        }
    }
}
