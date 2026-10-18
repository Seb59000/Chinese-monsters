using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Game
{
    /// <summary>Bakes the navigation mesh at runtime from the generated city's colliders (core API, no package).</summary>
    public static class NavMeshBaker
    {
        static NavMeshDataInstance instance;

        public static void Bake(Transform root, Bounds bounds)
        {
            Clear();
            var sources = new List<NavMeshBuildSource>();
            var markups = new List<NavMeshBuildMarkup>();
            NavMeshBuilder.CollectSources(root, Layers.WorldMask, NavMeshCollectGeometry.PhysicsColliders, 0, markups, sources);
            var settings = NavMesh.GetSettingsByID(0);
            settings.agentRadius = 0.4f;
            settings.agentHeight = 1.8f;
            settings.agentClimb = 0.4f;
            settings.agentSlope = 40f;
            settings.overrideVoxelSize = true;
            settings.voxelSize = 0.2f;
            var data = NavMeshBuilder.BuildNavMeshData(settings, sources, bounds, Vector3.zero, Quaternion.identity);
            if (data == null)
            {
                Debug.LogError("NavMesh build failed.");
                return;
            }
            instance = NavMesh.AddNavMeshData(data);
        }

        public static void Clear()
        {
            if (instance.valid) NavMesh.RemoveNavMeshData(instance);
            instance = default;
        }

        public static bool Snap(Vector3 position, out Vector3 snapped, float radius = 4f)
        {
            if (NavMesh.SamplePosition(position, out var hit, radius, NavMesh.AllAreas))
            {
                snapped = hit.position;
                return true;
            }
            snapped = position;
            return false;
        }

        public static Vector3 RandomPointNear(Vector3 center, float radius)
        {
            for (int i = 0; i < 8; i++)
            {
                var p = center + new Vector3(Random.Range(-radius, radius), 0f, Random.Range(-radius, radius));
                if (NavMesh.SamplePosition(p, out var hit, 3f, NavMesh.AllAreas) && hit.position.y < 1f)
                    return hit.position;
            }
            return center;
        }
    }
}
