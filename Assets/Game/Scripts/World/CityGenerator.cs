using UnityEngine;

namespace Game
{
    /// <summary>Builds the night district of Jinhai (Longmen quarter) from code, deterministically.</summary>
    public static class CityGenerator
    {
        const float FacadeTile = 24f; // metres per facade texture repeat (8 windows of 3 m)

        public static CityLayout Generate(Transform root, int seed)
        {
            var rnd = new System.Random(seed);
            var layout = new CityLayout();
            var city = new GameObject("City").transform;
            city.SetParent(root, false);

            var asphalt = MaterialFactory.Lit(new Color(0.06f, 0.06f, 0.07f), 0.6f);
            var pavement = MaterialFactory.Lit(new Color(0.2f, 0.2f, 0.21f), 0.35f);
            var roadLine = MaterialFactory.Emissive(new Color(0.55f, 0.5f, 0.3f), new Color(0.12f, 0.1f, 0.04f));
            var facades = new Material[3];
            var tints = new[] { Color.white, new Color(0.85f, 0.9f, 1f), new Color(1f, 0.92f, 0.85f) };
            for (int f = 0; f < facades.Length; f++)
            {
                MeshUtil.FacadeTextures(seed + f * 97, out var albedo, out var emission);
                facades[f] = MaterialFactory.Textured(albedo, emission, Color.white * 1.4f);
                facades[f].SetColor("_Color", tints[f]);
                facades[f].SetColor("_BaseColor", tints[f]);
            }

            float half = CityLayout.Half;
            float outer = half + 34f;

            MeshUtil.Solid(PrimitiveType.Cube, city, new Vector3(0f, -0.25f, 0f), new Vector3(outer * 2f, 0.5f, outer * 2f), asphalt).name = "Ground";

            for (int k = 0; k <= CityLayout.N; k++)
            {
                float c = CityLayout.StreetCenter(k);
                MeshUtil.Visual(PrimitiveType.Cube, city, new Vector3(c, 0.01f, 0f), new Vector3(0.18f, 0.02f, half * 2f), roadLine);
                MeshUtil.Visual(PrimitiveType.Cube, city, new Vector3(0f, 0.01f, c), new Vector3(half * 2f, 0.02f, 0.18f), roadLine);
            }

            for (int i = 0; i < CityLayout.N; i++)
            for (int j = 0; j < CityLayout.N; j++)
            {
                var b = new Vector2Int(i, j);
                var center = CityLayout.BlockCenterPos(i, j);
                var blockRoot = new GameObject($"Block_{i}_{j}").transform;
                blockRoot.SetParent(city, false);
                MeshUtil.Solid(PrimitiveType.Cube, blockRoot, center + Vector3.up * (CityLayout.Sidewalk * 0.5f),
                    new Vector3(CityLayout.Block, CityLayout.Sidewalk, CityLayout.Block), pavement).name = "Sidewalk";

                if (b == CityLayout.PoliceBlock) CityProps.PoliceStation(blockRoot, center, rnd);
                else if (b == CityLayout.MarketBlock) CityProps.Market(blockRoot, center, rnd);
                else if (b == CityLayout.ArenaBlock) layout.escapePoint = CityProps.Arena(blockRoot, center, rnd);
                else
                {
                    Lots(blockRoot, i, j, rnd, facades);
                    if (b == CityLayout.AlleyBlock || b == CityLayout.HostAlleyBlock || rnd.NextDouble() < 0.35)
                        CityProps.AlleyDressing(blockRoot, center, rnd);
                }
            }

            Boundary(city, half, outer, rnd, facades);
            DefineMission(layout);
            Lamps(city, layout);
            Civilians(layout, rnd);

            layout.bounds = new Bounds(Vector3.up * 40f, new Vector3(outer * 2f, 100f, outer * 2f));
            return layout;
        }

        static void Lots(Transform parent, int i, int j, System.Random rnd, Material[] facades)
        {
            float lot = (CityLayout.Block - CityLayout.Alley) * 0.5f;
            var cityCenterDistance = CityLayout.BlockCenterPos(i, j).magnitude / CityLayout.Half;
            for (int lx = 0; lx < 2; lx++)
            for (int lz = 0; lz < 2; lz++)
            {
                float minX = CityLayout.BlockMin(i) + lx * (lot + CityLayout.Alley);
                float minZ = CityLayout.BlockMin(j) + lz * (lot + CityLayout.Alley);
                float inset = 1.2f + (float)rnd.NextDouble() * 1.3f;
                var size = new Vector3(lot - inset * 2f, 0f, lot - inset * 2f);
                float tall = Mathf.Lerp(70f, 16f, Mathf.Clamp01(cityCenterDistance));
                size.y = Mathf.Max(9f, tall * (0.55f + (float)rnd.NextDouble() * 0.7f));
                var basePos = new Vector3(minX + lot * 0.5f, CityLayout.Sidewalk, minZ + lot * 0.5f);
                Building(parent, basePos, size, facades[rnd.Next(facades.Length)]);

                // Neon sign on one street-facing side.
                if (rnd.NextDouble() < 0.7)
                {
                    bool onX = rnd.NextDouble() < 0.5;
                    var normal = onX ? new Vector3(lx == 0 ? -1f : 1f, 0f, 0f) : new Vector3(0f, 0f, lz == 0 ? -1f : 1f);
                    var facePos = basePos + Vector3.Scale(normal, size * 0.5f);
                    var along = Vector3.Cross(Vector3.up, normal);
                    facePos += along * ((float)rnd.NextDouble() - 0.5f) * size.x * 0.6f;
                    facePos.y = CityLayout.Sidewalk + 4f + (float)rnd.NextDouble() * 5f;
                    CityProps.NeonSign(parent, facePos, normal, rnd);
                }

                if (rnd.NextDouble() < 0.5)
                {
                    var roof = basePos + Vector3.up * size.y;
                    var unit = MaterialFactory.Lit(new Color(0.25f, 0.25f, 0.27f));
                    MeshUtil.Visual(PrimitiveType.Cube, parent, roof + new Vector3(1.5f, 0.8f, -1f), new Vector3(3f, 1.6f, 2.2f), unit);
                }
            }
        }

        public static GameObject Building(Transform parent, Vector3 basePos, Vector3 size, Material mat)
        {
            var go = new GameObject("Building");
            go.transform.SetParent(parent, false);
            go.transform.position = basePos;
            go.AddComponent<MeshFilter>().sharedMesh = MeshUtil.FacadeBox(size, FacadeTile);
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            var col = go.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, size.y * 0.5f, 0f);
            col.size = size;
            go.isStatic = true;
            return go;
        }

        static void Boundary(Transform parent, float half, float outer, System.Random rnd, Material[] facades)
        {
            var ring = new GameObject("Boundary").transform;
            ring.SetParent(parent, false);
            const float seg = 28f;
            float depth = outer - half - 2f;
            for (float t = -outer; t < outer; t += seg)
            {
                for (int side = 0; side < 4; side++)
                {
                    float h = 30f + (float)rnd.NextDouble() * 60f;
                    Vector3 pos, size;
                    float mid = t + seg * 0.5f;
                    float d = half + 2f + depth * 0.5f;
                    switch (side)
                    {
                        case 0: pos = new Vector3(mid, 0f, d); size = new Vector3(seg - 1f, h, depth); break;
                        case 1: pos = new Vector3(mid, 0f, -d); size = new Vector3(seg - 1f, h, depth); break;
                        case 2: pos = new Vector3(d, 0f, mid); size = new Vector3(depth, h, seg - 1f); break;
                        default: pos = new Vector3(-d, 0f, mid); size = new Vector3(depth, h, seg - 1f); break;
                    }
                    Building(ring, pos, size, facades[rnd.Next(facades.Length)]);
                }
            }
        }

        static void DefineMission(CityLayout l)
        {
            float y = CityLayout.Sidewalk;
            float S(int k) => CityLayout.StreetCenter(k);
            float C(int k) => CityLayout.BlockCenter(k);
            float Min(int k) => CityLayout.BlockMin(k);
            float Max(int k) => CityLayout.BlockMax(k);

            l.playerStart = new Vector3(S(1) + 2.5f, 0f, S(0) + 3f);
            l.playerStartYaw = 0f;
            l.policeCar = new Vector3(S(1) - 3.2f, 0f, S(0) + 7f);
            l.patrolPoint = new Vector3(C(1), 0f, S(1));
            l.suspectStart = new Vector3(C(1), y, Min(1) + 12f);
            l.chaseCheckpointPlayer = new Vector3(C(1) - 2f, 0f, S(1) - 4f);

            l.chaseRoute.Clear();
            l.chaseRoute.Add(new Vector3(C(1), y, C(1)));                 // 0  alley crossing
            l.chaseRoute.Add(new Vector3(C(1), 0f, S(2)));                // 1  alley exit
            l.chaseRoute.Add(new Vector3(S(2), 0f, S(2)));                // 2
            l.chaseRoute.Add(new Vector3(S(2), 0f, S(3)));                // 3
            l.chaseRoute.Add(new Vector3(C(2), 0f, S(3)));                // 4  market south gate
            l.chaseRoute.Add(new Vector3(C(2) - 1f, y, Min(3) + 10f));    // 5  infection B
            l.chaseRoute.Add(new Vector3(C(2) + 1f, y, Max(3) - 10f));    // 6  infection C
            l.chaseRoute.Add(new Vector3(C(2), 0f, S(4)));                // 7  market north gate
            l.chaseRoute.Add(new Vector3(S(4), 0f, S(4)));                // 8
            l.chaseRoute.Add(new Vector3(C(4), 0f, S(4)));                // 9
            l.chaseRoute.Add(new Vector3(C(4), y, C(4)));                 // 10 infection D
            l.chaseRoute.Add(new Vector3(C(4), 0f, S(5)));                // 11
            l.chaseRoute.Add(new Vector3(S(5), 0f, S(5)));                // 12
            l.chaseRoute.Add(new Vector3(S(5), 0f, C(5)));                // 13 docks entrance street
            l.chaseRoute.Add(new Vector3(Min(5) + 6f, y, C(5)));          // 14 yard gate
            l.chaseRoute.Add(new Vector3(C(5) + 2f, y, C(5) + 2f));       // 15 yard centre
            l.infectionWaypoints = new[] { 5, 6, 10 };

            l.arenaCenter = new Vector3(C(5), y, C(5));
            l.arenaEntrance = new Vector3(Min(5) + 3f, y, C(5));
            l.fightCheckpointPlayer = new Vector3(S(5), 0f, C(5));

            l.hostPositions = new[]
            {
                new Vector3(C(1) + 0.9f, y, Min(1) + 14f),
                new Vector3(C(2) - 4.6f, y, Min(3) + 10f),
                new Vector3(C(2) + 4.6f, y, Max(3) - 10f),
                new Vector3(C(4) + 1.4f, y, C(4) + 1.4f),
            };
        }

        static void Lamps(Transform parent, CityLayout layout)
        {
            var lamps = new GameObject("Lamps").transform;
            lamps.SetParent(parent, false);
            for (int k = 0; k < CityLayout.N; k++)
            for (int m = 0; m < CityLayout.N; m++)
            {
                var b = new Vector2Int(k, m);
                if (b == CityLayout.MarketBlock || b == CityLayout.ArenaBlock) continue;
                var pos = new Vector3(CityLayout.StreetCenter(k) + 6.8f, CityLayout.Sidewalk, CityLayout.StreetCenter(m) + 6.8f);
                layout.lampPositions.Add(pos);
                bool lit = NearRoute(layout, pos, 22f) || Vector3.Distance(pos, layout.playerStart) < 30f;
                CityProps.StreetLamp(lamps, pos, lit);
            }
        }

        static bool NearRoute(CityLayout layout, Vector3 p, float radius)
        {
            for (int i = 1; i < layout.chaseRoute.Count; i++)
            {
                var a = layout.chaseRoute[i - 1];
                var b = layout.chaseRoute[i];
                var ab = b - a;
                float t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / Mathf.Max(0.01f, ab.sqrMagnitude));
                if (Vector3.Distance(p, a + ab * t) < radius) return true;
            }
            return false;
        }

        static void Civilians(CityLayout layout, System.Random rnd)
        {
            layout.civilianSpawns.Clear();
            for (int i = 0; i < CityLayout.N; i++)
            for (int j = 0; j < CityLayout.N; j++)
            {
                var b = new Vector2Int(i, j);
                if (b == CityLayout.ArenaBlock || b == CityLayout.PoliceBlock) continue;
                var c = CityLayout.BlockCenterPos(i, j);
                if (b == CityLayout.MarketBlock)
                {
                    for (int n = 0; n < 12; n++)
                        layout.civilianSpawns.Add(c + new Vector3(Rand(rnd, -16f, 16f), CityLayout.Sidewalk, Rand(rnd, -16f, 16f)));
                    continue;
                }
                for (int n = 0; n < 2; n++)
                {
                    float edge = CityLayout.Block * 0.5f - 0.8f;
                    float along = Rand(rnd, -edge, edge);
                    Vector3 offset;
                    switch (rnd.Next(4))
                    {
                        case 0: offset = new Vector3(along, 0f, -edge); break;
                        case 1: offset = new Vector3(along, 0f, edge); break;
                        case 2: offset = new Vector3(-edge, 0f, along); break;
                        default: offset = new Vector3(edge, 0f, along); break;
                    }
                    layout.civilianSpawns.Add(c + offset + Vector3.up * CityLayout.Sidewalk);
                }
            }
        }

        public static float Rand(System.Random rnd, float min, float max) => min + (float)rnd.NextDouble() * (max - min);
    }
}
