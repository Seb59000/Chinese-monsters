using System;

namespace Game
{
    /// <summary>Gameplay events the mission director and HUD listen to.</summary>
    public static class MissionEvents
    {
        public static event Action<CivilianAI> CivilianKilled;
        public static event Action<InfectedHost> HostResolved;
        public static event Action<InfectedHost> HostInfected;
        public static event Action<OffspringAI> OffspringSpawned;
        public static event Action<OffspringAI> OffspringKilled;

        public static void RaiseCivilianKilled(CivilianAI c) => CivilianKilled?.Invoke(c);
        public static void RaiseHostResolved(InfectedHost h) => HostResolved?.Invoke(h);
        public static void RaiseHostInfected(InfectedHost h) => HostInfected?.Invoke(h);
        public static void RaiseOffspringSpawned(OffspringAI o) => OffspringSpawned?.Invoke(o);
        public static void RaiseOffspringKilled(OffspringAI o) => OffspringKilled?.Invoke(o);

        /// <summary>Drops every subscriber (called when the world is rebuilt).</summary>
        public static void Clear()
        {
            CivilianKilled = null;
            HostResolved = null;
            HostInfected = null;
            OffspringSpawned = null;
            OffspringKilled = null;
        }
    }
}
