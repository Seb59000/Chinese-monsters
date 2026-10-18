using System;
using UnityEngine;

namespace Game
{
    public enum Faction { Player, Civilian, Mutant }

    public struct DamageInfo
    {
        public float amount;
        public Vector3 point;
        public Vector3 direction;
        public GameObject source;
        public Faction sourceFaction;

        public DamageInfo(float amount, Vector3 point, Vector3 direction, GameObject source, Faction sourceFaction)
        {
            this.amount = amount;
            this.point = point;
            this.direction = direction;
            this.source = source;
            this.sourceFaction = sourceFaction;
        }
    }

    public class Health : MonoBehaviour
    {
        public float max = 100f;
        public Faction faction = Faction.Civilian;
        public bool invulnerable;

        /// <summary>Health never drops below this (used to keep scripted characters alive).</summary>
        public float floor;

        [Header("Regeneration")]
        public float regenDelay;
        public float regenRate;

        public float Current { get; private set; }
        public bool IsDead { get; private set; }
        public float Normalized => max > 0f ? Current / max : 0f;
        public float LastDamageTime { get; private set; } = -999f;

        public event Action<DamageInfo> Damaged;
        public event Action<DamageInfo> Died;

        void Awake() => Current = max;

        public void SetMax(float value)
        {
            max = value;
            Current = value;
        }

        public void TakeDamage(DamageInfo info)
        {
            if (IsDead || invulnerable || info.amount <= 0f) return;
            Current = Mathf.Max(floor, Current - info.amount);
            LastDamageTime = Time.time;
            Damaged?.Invoke(info);
            if (Current <= 0f)
            {
                IsDead = true;
                Died?.Invoke(info);
            }
        }

        public void Kill(GameObject source, Faction sourceFaction)
        {
            if (IsDead) return;
            floor = 0f;
            invulnerable = false;
            TakeDamage(new DamageInfo(Current + 1f, transform.position, Vector3.zero, source, sourceFaction));
        }

        void Update()
        {
            if (IsDead || regenRate <= 0f || Current >= max) return;
            if (Time.time - LastDamageTime >= regenDelay)
                Current = Mathf.Min(max, Current + regenRate * Time.deltaTime);
        }
    }
}
