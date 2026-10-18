using System;
using UnityEngine;

namespace Game
{
    public enum MissionPhase { Patrol = 0, Chase = 1, Fight = 2, Sample = 3, Hosts = 4, Complete = 5 }

    /// <summary>Choices the player made. Kept across missions to drive the endings.</summary>
    [Serializable]
    public class MoralityStats
    {
        public int hostsSaved;
        public int hostsNeutralized;
        public int offspringBorn;
        public int offspringKilled;
        public int civiliansKilled;
        public float missionTime;

        public MoralityStats Clone() => (MoralityStats)MemberwiseClone();

        /// <summary>Positive = protector, negative = executioner.</summary>
        public int Score => hostsSaved * 10 - hostsNeutralized * 8 - civiliansKilled * 15 - offspringBorn * 5;
    }

    /// <summary>State that survives a checkpoint restart (the world itself is rebuilt).</summary>
    public static class GameSession
    {
        public static MissionPhase Checkpoint = MissionPhase.Patrol;
        public static MoralityStats Stats = new MoralityStats();
        static MoralityStats checkpointStats = new MoralityStats();

        /// <summary>Story hosts (bit per host) infected / dead when the checkpoint was reached.</summary>
        public static int StoryInfectedMask, StoryDeadMask;
        public static Vector3? SamplePosition;

        public static float MouseSensitivity = 0.12f;
        public static float StickSensitivity = 160f;
        public static bool InvertY;

        public static event Action StatsChanged;

        public static void SetCheckpoint(MissionPhase phase)
        {
            Checkpoint = phase;
            checkpointStats = Stats.Clone();
        }

        public static void RestoreCheckpointStats() => Stats = checkpointStats.Clone();

        public static void ResetAll()
        {
            Checkpoint = MissionPhase.Patrol;
            Stats = new MoralityStats();
            checkpointStats = new MoralityStats();
            StoryInfectedMask = StoryDeadMask = 0;
            SamplePosition = null;
        }

        public static void NotifyStatsChanged() => StatsChanged?.Invoke();

        public static void LoadSettings()
        {
            MouseSensitivity = PlayerPrefs.GetFloat("cm.mouseSens", MouseSensitivity);
            StickSensitivity = PlayerPrefs.GetFloat("cm.stickSens", StickSensitivity);
            InvertY = PlayerPrefs.GetInt("cm.invertY", 0) == 1;
        }

        public static void SaveSettings()
        {
            PlayerPrefs.SetFloat("cm.mouseSens", MouseSensitivity);
            PlayerPrefs.SetFloat("cm.stickSens", StickSensitivity);
            PlayerPrefs.SetInt("cm.invertY", InvertY ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}
