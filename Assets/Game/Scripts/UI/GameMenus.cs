using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game
{
    /// <summary>Pause, failure and debrief screens, plus the mission title card. Mouse, keyboard and gamepad.</summary>
    public class GameMenus : MonoBehaviour
    {
        public static GameMenus Instance { get; private set; }

        enum MenuScreen { None, Pause, Failed, Debrief }

        class Item
        {
            public Func<string> label;
            public Action submit;
            public Action<int> adjust; // left/right for value items
        }

        MenuScreen screen = MenuScreen.None;
        readonly List<Item> items = new List<Item>();
        int selected;
        float navRepeatAt;
        Vector2 lastNav;
        float openedAt, titleStart;

        public bool IsOpen => screen != MenuScreen.None;

        void Awake() => Instance = this;

        void Start()
        {
            titleStart = Time.unscaledTime;
            var d = MissionDirector.Instance;
            if (d != null)
            {
                d.OnFailed += () => Open(MenuScreen.Failed);
                d.OnCompleted += () => Open(MenuScreen.Debrief);
            }
            ApplyCursor();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            Time.timeScale = 1f;
            AudioListener.pause = false;
        }

        void Open(MenuScreen s)
        {
            screen = s;
            selected = 0;
            openedAt = Time.unscaledTime;
            items.Clear();
            switch (s)
            {
                case MenuScreen.Pause:
                    items.Add(new Item { label = () => L.Get("menu.resume"), submit = Close });
                    items.Add(new Item { label = () => L.Get("menu.restart_checkpoint"), submit = () => Restart(true) });
                    items.Add(new Item { label = () => L.Get("menu.restart_mission"), submit = () => Restart(false) });
                    items.Add(new Item { label = () => L.Get("menu.language") + " : " + L.Get("lang.name"), submit = L.ToggleLanguage, adjust = _ => L.ToggleLanguage() });
                    items.Add(new Item
                    {
                        label = () => L.Get("menu.sensitivity") + " : " + Mathf.RoundToInt(GameSession.MouseSensitivity * 100f),
                        adjust = dir =>
                        {
                            GameSession.MouseSensitivity = Mathf.Clamp(GameSession.MouseSensitivity + dir * 0.01f, 0.02f, 0.5f);
                            GameSession.StickSensitivity = Mathf.Clamp(GameSession.StickSensitivity + dir * 10f, 40f, 400f);
                            GameSession.SaveSettings();
                        },
                    });
                    items.Add(new Item
                    {
                        label = () => L.Get("menu.invert_y") + " : " + L.Get(GameSession.InvertY ? "menu.on" : "menu.off"),
                        submit = () => { GameSession.InvertY = !GameSession.InvertY; GameSession.SaveSettings(); },
                        adjust = _ => { GameSession.InvertY = !GameSession.InvertY; GameSession.SaveSettings(); },
                    });
                    items.Add(new Item { label = () => L.Get("menu.quit"), submit = Quit });
                    Time.timeScale = 0f;
                    AudioListener.pause = true;
                    break;
                case MenuScreen.Failed:
                    items.Add(new Item { label = () => L.Get("menu.restart_checkpoint"), submit = () => Restart(true) });
                    items.Add(new Item { label = () => L.Get("menu.restart_mission"), submit = () => Restart(false) });
                    items.Add(new Item { label = () => L.Get("menu.quit"), submit = Quit });
                    break;
                case MenuScreen.Debrief:
                    items.Add(new Item { label = () => L.Get("menu.replay"), submit = () => Restart(false) });
                    items.Add(new Item { label = () => L.Get("menu.language") + " : " + L.Get("lang.name"), submit = L.ToggleLanguage, adjust = _ => L.ToggleLanguage() });
                    items.Add(new Item { label = () => L.Get("menu.quit"), submit = Quit });
                    break;
            }
            ApplyCursor();
        }

        void Close()
        {
            screen = MenuScreen.None;
            Time.timeScale = 1f;
            AudioListener.pause = false;
            ApplyCursor();
        }

        void ApplyCursor()
        {
            bool menu = IsOpen;
            Cursor.lockState = menu ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = menu;
            if (GameInput.Instance != null) GameInput.Instance.GameplayEnabled = !menu;
        }

        static void Restart(bool checkpoint)
        {
            AudioManager.Play2D(SoundBank.Click);
            GameBootstrap.Instance.Restart(checkpoint);
        }

        static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        void Update()
        {
            var input = GameInput.Instance;
            if (input == null) return;

            if (input.Pause.WasPressedThisFrame())
            {
                if (screen == MenuScreen.None && !GameBootstrap.IsLoading) Open(MenuScreen.Pause);
                else if (screen == MenuScreen.Pause) Close();
            }

            // Clicking the game view re-captures the mouse (the editor releases it on focus loss).
            if (screen == MenuScreen.None && Cursor.lockState != CursorLockMode.Locked && input.Fire.WasPressedThisFrame())
                ApplyCursor();

            if (!IsOpen || items.Count == 0) return;
            if (screen == MenuScreen.Pause && input.Cancel.WasPressedThisFrame()) { Close(); return; }

            var nav = input.Navigate.ReadValue<Vector2>();
            int vertical = Mathf.Abs(nav.y) > 0.5f ? (nav.y > 0f ? -1 : 1) : 0;
            int horizontal = Mathf.Abs(nav.x) > 0.5f ? (nav.x > 0f ? 1 : -1) : 0;
            bool fresh = (Mathf.Abs(lastNav.y) <= 0.5f && vertical != 0) || (Mathf.Abs(lastNav.x) <= 0.5f && horizontal != 0);
            if (fresh || ((vertical != 0 || horizontal != 0) && Time.unscaledTime > navRepeatAt))
            {
                navRepeatAt = Time.unscaledTime + (fresh ? 0.4f : 0.12f);
                if (vertical != 0)
                {
                    selected = (selected + vertical + items.Count) % items.Count;
                    AudioManager.Play2D(SoundBank.Click, 0.5f);
                }
                else if (horizontal != 0 && items[selected].adjust != null)
                {
                    items[selected].adjust(horizontal);
                    AudioManager.Play2D(SoundBank.Click, 0.5f);
                }
            }
            lastNav = nav;

            // Ignore the press that opened the menu.
            if (Time.unscaledTime - openedAt > 0.15f && input.Submit.WasPressedThisFrame())
                Activate(selected);
        }

        void Activate(int index)
        {
            var item = items[index];
            AudioManager.Play2D(SoundBank.Click, 0.8f);
            if (item.submit != null) item.submit();
            else item.adjust?.Invoke(1);
        }

        void OnGUI()
        {
            GUI.depth = 0;
            float W = UIDraw.W, H = UIDraw.H;
            DrawTitleCard(W, H);
            if (!IsOpen) return;

            UIDraw.Box(new Rect(0f, 0f, W, H), new Color(0f, 0f, 0f, screen == MenuScreen.Pause ? 0.6f : 0.75f));
            float x = Mathf.Max(80f, W * 0.5f - 560f);
            float y = 120f;

            switch (screen)
            {
                case MenuScreen.Pause:
                    UIDraw.Text(new Rect(x, y, 900f, 70f), L.Get("menu.paused"), 56, Color.white, TextAnchor.UpperLeft, true);
                    y += 110f;
                    var help = L.Get(GameInput.Instance != null && GameInput.Instance.UsingGamepad ? "help.pad" : "help.kb");
                    UIDraw.Text(new Rect(x + 560f, y, 520f, 600f), help, 22, new Color(1f, 1f, 1f, 0.75f), TextAnchor.UpperLeft, false, true);
                    break;
                case MenuScreen.Failed:
                    UIDraw.Text(new Rect(x, y, 1100f, 70f), L.Get("fail.title"), 56, UIDraw.Danger, TextAnchor.UpperLeft, true);
                    UIDraw.Text(new Rect(x, y + 80f, 1100f, 40f), L.Get(MissionDirector.Instance != null ? MissionDirector.Instance.FailReasonKey : "fail.dead"), 30, Color.white);
                    y += 170f;
                    break;
                case MenuScreen.Debrief:
                    y = DrawDebrief(x, y);
                    break;
            }

            for (int i = 0; i < items.Count; i++)
            {
                var r = new Rect(x, y + i * 62f, 520f, 52f);
                if (UIDraw.Button(r, items[i].label(), i == selected, out bool hovered))
                {
                    selected = i;
                    Activate(i);
                    break;
                }
                if (hovered && Event.current.type == EventType.MouseMove) selected = i;
            }
        }

        float DrawDebrief(float x, float y)
        {
            var s = GameSession.Stats;
            UIDraw.Text(new Rect(x, y - 40f, 1100f, 40f), L.Get("debrief.mission"), 24, UIDraw.Accent, TextAnchor.UpperLeft, true);
            UIDraw.Text(new Rect(x, y, 1100f, 70f), L.Get("debrief.title"), 54, Color.white, TextAnchor.UpperLeft, true);
            y += 90f;
            int minutes = Mathf.FloorToInt(s.missionTime / 60f), seconds = Mathf.FloorToInt(s.missionTime % 60f);
            string[] rows =
            {
                L.Format("debrief.saved", s.hostsSaved),
                L.Format("debrief.neutralized", s.hostsNeutralized),
                L.Format("debrief.born", s.offspringBorn),
                L.Format("debrief.offspring_killed", s.offspringKilled),
                L.Format("debrief.civilians", s.civiliansKilled),
                L.Format("debrief.time", minutes, seconds.ToString("00")),
            };
            foreach (var row in rows)
            {
                UIDraw.Text(new Rect(x, y, 600f, 34f), row, 28, Color.white);
                y += 38f;
            }

            string verdictKey = s.hostsNeutralized == 0 && s.civiliansKilled == 0 && s.offspringBorn == 0 ? "debrief.verdict.protector"
                : s.hostsNeutralized > 0 || s.civiliansKilled > 0 ? "debrief.verdict.executioner"
                : "debrief.verdict.overwhelmed";
            y += 14f;
            var verdict = L.Get(verdictKey);
            float vh = UIDraw.TextHeight(verdict, 26, 1050f);
            UIDraw.Text(new Rect(x, y, 1050f, vh + 10f), verdict, 26, UIDraw.Accent, TextAnchor.UpperLeft, false, true);
            y += vh + 20f;
            var next = L.Get("debrief.next");
            float nh = UIDraw.TextHeight(next, 22, 1050f);
            UIDraw.Text(new Rect(x, y, 1050f, nh + 10f), next, 22, new Color(1f, 1f, 1f, 0.75f), TextAnchor.UpperLeft, false, true);
            return y + nh + 30f;
        }

        void DrawTitleCard(float W, float H)
        {
            var d = MissionDirector.Instance;
            if (d == null || GameBootstrap.IsLoading) return;
            float t = Time.unscaledTime - titleStart;
            // Fade in from black, then the mission card for a few seconds.
            if (t < 1.5f) UIDraw.Box(new Rect(0f, 0f, W, H), new Color(0f, 0f, 0f, 1f - t / 1.5f));
            if (GameSession.Checkpoint != MissionPhase.Patrol || t > 7f) return;
            float a = Mathf.Clamp01(t - 0.5f) * Mathf.Clamp01((7f - t) / 1.5f);
            UIDraw.Text(new Rect(0f, H * 0.3f, W, 40f), L.Get("title.mission"), 26, new Color(UIDraw.Accent.r, UIDraw.Accent.g, UIDraw.Accent.b, a), TextAnchor.MiddleCenter, true);
            UIDraw.Text(new Rect(0f, H * 0.3f + 44f, W, 80f), L.Get("title.name"), 64, new Color(1f, 1f, 1f, a), TextAnchor.MiddleCenter, true);
            UIDraw.Text(new Rect(0f, H * 0.3f + 130f, W, 40f), L.Get("title.place"), 24, new Color(1f, 1f, 1f, a * 0.8f), TextAnchor.MiddleCenter);
        }
    }
}
