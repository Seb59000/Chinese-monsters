using System;
using UnityEngine;

namespace Game
{
    /// <summary>Service pistol: hitscan from the screen centre, magazine, reload, bloom.</summary>
    public class PlayerShooter : MonoBehaviour
    {
        public float damage = 24f;
        public float fireInterval = 0.2f;
        public float range = 150f;
        public int magazine = 12;
        public float reloadTime = 1.4f;

        public bool WeaponEnabled = true;
        public int Ammo { get; private set; }
        public bool Reloading { get; private set; }
        public float ReloadProgress01 => Reloading ? 1f - Mathf.Clamp01((reloadEnd - Time.time) / reloadTime) : 0f;
        public float SpreadDegrees => (aiming ? 0.4f : 2.0f) + bloom;
        public bool IsAiming => aiming;

        /// <summary>Raised when a bullet hits a character. Argument: true if it killed.</summary>
        public event Action<bool> HitConfirmed;

        PlayerController player;
        ThirdPersonCamera cam;
        Transform muzzle;
        LineRenderer tracer;
        Light flash;
        float nextFire, reloadEnd, tracerOff, lastFire = -10f, bloom;
        bool aiming;

        void Start()
        {
            player = GetComponent<PlayerController>();
            cam = Camera.main != null ? Camera.main.GetComponent<ThirdPersonCamera>() : null;
            Ammo = magazine;

            var hand = player.Rig != null ? player.Rig.rightHand : transform;
            var gunMat = MaterialFactory.Lit(new Color(0.05f, 0.05f, 0.06f), 0.6f, 0.6f);
            var gun = MeshUtil.Visual(PrimitiveType.Cube, hand, new Vector3(0f, -0.08f, 0.05f), new Vector3(0.09f, 0.3f, 0.16f), gunMat);
            gun.name = "Pistol";
            muzzle = new GameObject("Muzzle").transform;
            muzzle.SetParent(hand, false);
            muzzle.localPosition = new Vector3(0f, -0.27f, 0.05f);

            tracer = gameObject.AddComponent<LineRenderer>();
            tracer.positionCount = 2;
            tracer.startWidth = 0.035f;
            tracer.endWidth = 0.015f;
            tracer.sharedMaterial = MaterialFactory.Glow(new Color(1f, 0.85f, 0.5f, 0.9f));
            tracer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            tracer.enabled = false;

            flash = CityProps.AddLight(muzzle, muzzle.position, new Color(1f, 0.75f, 0.4f), 9f, 0f);
        }

        void Update()
        {
            var input = GameInput.Instance;
            if (input == null) return;
            bool canUse = WeaponEnabled && player.ControlsEnabled && !player.InteractionLocked && !player.Health.IsDead;

            aiming = canUse && input.Held(input.Aim);
            player.IsAiming = aiming;
            if (cam != null) cam.IsAiming = aiming;

            bool raised = aiming || Time.time - lastFire < 1.2f;
            if (player.Rig != null) player.Rig.HoldWeapon(raised, cam != null ? cam.Pitch : 0f);

            if (Reloading && Time.time >= reloadEnd)
            {
                Reloading = false;
                Ammo = magazine;
            }

            if (canUse && input.Pressed(input.Reload) && Ammo < magazine && !Reloading) StartReload();

            if (canUse && input.Pressed(input.Fire) && Time.time >= nextFire && !Reloading)
            {
                if (Ammo <= 0) StartReload();
                else Fire();
            }

            bloom = Mathf.MoveTowards(bloom, 0f, Time.deltaTime * 4f);
            if (tracer.enabled && Time.time > tracerOff) tracer.enabled = false;
            flash.intensity = Mathf.MoveTowards(flash.intensity, 0f, Time.deltaTime * 80f);
        }

        void StartReload()
        {
            Reloading = true;
            reloadEnd = Time.time + reloadTime;
            AudioManager.PlayAt(SoundBank.Reload, transform.position, 0.6f);
        }

        void Fire()
        {
            Ammo--;
            nextFire = Time.time + fireInterval;
            lastFire = Time.time;
            player.CancelSprint();
            if (player.Rig != null) player.Rig.HoldWeapon(true, cam != null ? cam.Pitch : 0f);

            var camera = Camera.main;
            var ray = camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            // Start the ray level with the player so nothing behind the character can be hit.
            float skip = Mathf.Max(0f, Vector3.Dot(transform.position - ray.origin, ray.direction));
            var origin = ray.origin + ray.direction * skip;
            var dir = Quaternion.AngleAxis(UnityEngine.Random.Range(0f, 360f), ray.direction) *
                      (Quaternion.AngleAxis(UnityEngine.Random.Range(0f, SpreadDegrees), camera.transform.right) * ray.direction);
            bloom = Mathf.Min(bloom + 1.1f, 4f);

            var end = origin + dir * range;
            if (Physics.Raycast(origin, dir, out var hit, range, Layers.ShootMask, QueryTriggerInteraction.Ignore))
            {
                end = hit.point;
                var health = hit.collider.GetComponentInParent<Health>();
                if (health != null && health.faction != Faction.Player)
                {
                    bool wasDead = health.IsDead;
                    health.TakeDamage(new DamageInfo(damage, hit.point, dir, gameObject, Faction.Player));
                    if (!wasDead) HitConfirmed?.Invoke(health.IsDead);
                    TransientFx.Burst(hit.point, health.faction == Faction.Mutant ? new Color(0.35f, 0.05f, 0.25f) : new Color(0.5f, 0.02f, 0.02f), 4, 0.12f, 2.5f);
                    AudioManager.PlayAt(SoundBank.Hit, hit.point, 0.8f);
                }
                else
                {
                    TransientFx.Burst(hit.point + hit.normal * 0.05f, new Color(1f, 0.8f, 0.4f), 3, 0.08f, 3f);
                }
            }

            tracer.SetPosition(0, muzzle.position);
            tracer.SetPosition(1, end);
            tracer.enabled = true;
            tracerOff = Time.time + 0.04f;
            flash.intensity = 4f;
            AudioManager.PlayAt(SoundBank.Gunshot, muzzle.position, 1f);
            if (cam != null)
            {
                cam.Kick(aiming ? 0.6f : 1.1f);
                cam.Shake(0.15f);
            }
            CivilianAI.Alert(transform.position, 40f, transform.position);
        }

        public void Refill()
        {
            Ammo = magazine;
            Reloading = false;
        }
    }
}
