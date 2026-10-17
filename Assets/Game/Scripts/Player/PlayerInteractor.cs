using UnityEngine;

namespace Game
{
    public enum HostAction { None, Extract, Neutralize }

    /// <summary>Lets the player extract the parasite from a host (long hold) or neutralise the host (short hold).</summary>
    public class PlayerInteractor : MonoBehaviour
    {
        public float reach = 2.6f;
        public float neutralizeHold = 0.9f;

        public InfectedHost Focus { get; private set; }
        public HostAction Current { get; private set; }
        public float Progress01 { get; private set; }

        PlayerController player;
        float neutralizeTimer, lockedUntil;

        void Start()
        {
            player = GetComponent<PlayerController>();
            player.Health.Damaged += _ =>
            {
                if (Current == HostAction.Extract) lockedUntil = Time.time + 0.6f;
                Stop();
            };
        }

        void Update()
        {
            var input = GameInput.Instance;
            if (input == null) return;

            if (Current == HostAction.None) Focus = FindFocus();
            bool allowed = Focus != null && player.ControlsEnabled && !player.Health.IsDead && Time.time > lockedUntil && !Focus.IsResolved;
            if (!allowed)
            {
                Stop();
                return;
            }

            if (input.Held(input.Interact) && Current != HostAction.Neutralize)
            {
                Current = HostAction.Extract;
                player.InteractionLocked = true;
                FaceFocus();
                Progress01 = Focus.ExtractTick(Time.deltaTime, gameObject);
                if (Focus.IsResolved) Stop();
            }
            else if (input.Held(input.Neutralize) && Current != HostAction.Extract)
            {
                Current = HostAction.Neutralize;
                neutralizeTimer += Time.deltaTime;
                Progress01 = neutralizeTimer / neutralizeHold;
                if (neutralizeTimer >= neutralizeHold)
                {
                    Focus.Neutralize(gameObject);
                    AudioManager.PlayAt(SoundBank.Gunshot, transform.position, 0.9f, 0.8f);
                    Stop();
                }
            }
            else
            {
                Stop();
            }
        }

        void Stop()
        {
            if (Current == HostAction.Extract && Focus != null) Focus.CancelExtraction();
            Current = HostAction.None;
            Progress01 = 0f;
            neutralizeTimer = 0f;
            if (player != null) player.InteractionLocked = false;
        }

        void FaceFocus()
        {
            var to = Focus.transform.position - transform.position;
            to.y = 0f;
            if (to.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to), Time.deltaTime * 10f);
        }

        InfectedHost FindFocus()
        {
            InfectedHost best = null;
            float bestDist = reach;
            foreach (var h in InfectedHost.All)
            {
                if (h == null || h.IsResolved) continue;
                float d = Vector3.Distance(h.transform.position, transform.position);
                if (d < bestDist)
                {
                    best = h;
                    bestDist = d;
                }
            }
            return best;
        }
    }
}
