using System.Collections.Generic;
using UnityEngine;

namespace Game
{
    /// <summary>
    /// Grid of N x N city blocks separated by streets. Every regular block is split
    /// in four lots by a cross-shaped alley. Also stores the mission anchor points.
    /// </summary>
    public class CityLayout
    {
        public const int N = 6;
        public const float Block = 40f;
        public const float Street = 12f;
        public const float Alley = 5f;
        public const float Sidewalk = 0.15f;

        public static float Half => (N * Block + (N + 1) * Street) * 0.5f;
        public static float StreetCenter(int k) => -Half + k * (Block + Street) + Street * 0.5f;
        public static float BlockMin(int i) => StreetCenter(i) + Street * 0.5f;
        public static float BlockMax(int i) => BlockMin(i) + Block;
        public static float BlockCenter(int i) => BlockMin(i) + Block * 0.5f;
        public static Vector3 BlockCenterPos(int i, int j) => new Vector3(BlockCenter(i), 0f, BlockCenter(j));

        // Special blocks.
        public static readonly Vector2Int PoliceBlock = new Vector2Int(0, 0);
        public static readonly Vector2Int AlleyBlock = new Vector2Int(1, 1);
        public static readonly Vector2Int MarketBlock = new Vector2Int(2, 3);
        public static readonly Vector2Int HostAlleyBlock = new Vector2Int(4, 4);
        public static readonly Vector2Int ArenaBlock = new Vector2Int(5, 5);

        public Vector3 playerStart;
        public float playerStartYaw;
        public Vector3 policeCar;
        public Vector3 patrolPoint;
        public Vector3 suspectStart;

        /// <summary>Points the suspect runs through, in order. Indices below mark scripted infections.</summary>
        public readonly List<Vector3> chaseRoute = new List<Vector3>();
        public int[] infectionWaypoints = new int[0];

        public Vector3 chaseCheckpointPlayer;
        public Vector3 arenaCenter;
        public Vector3 arenaEntrance;
        public Vector3 fightCheckpointPlayer;
        public Vector3 escapePoint;

        /// <summary>A = alley victim, B/C = market, D = second alley. Index matches HostDefinition order.</summary>
        public Vector3[] hostPositions = new Vector3[0];

        public readonly List<Vector3> civilianSpawns = new List<Vector3>();
        public readonly List<Vector3> lampPositions = new List<Vector3>();

        public Bounds bounds;
    }
}
