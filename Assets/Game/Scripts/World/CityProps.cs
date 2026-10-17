using UnityEngine;

namespace Game
{
    /// <summary>Set dressing for the district: stations, market, docks yard, signs, lamps.</summary>
    public static class CityProps
    {
        static readonly Color[] NeonColors =
        {
            new Color(1f, 0.15f, 0.55f), new Color(0.1f, 0.9f, 1f), new Color(1f, 0.2f, 0.15f),
            new Color(1f, 0.65f, 0.1f), new Color(0.3f, 1f, 0.45f), new Color(0.65f, 0.3f, 1f),
        };

        static float R(System.Random rnd, float a, float b) => CityGenerator.Rand(rnd, a, b);

        public static void PoliceStation(Transform parent, Vector3 center, System.Random rnd)
        {
            var wall = MaterialFactory.Lit(new Color(0.32f, 0.33f, 0.36f), 0.3f);
            var stripe = MaterialFactory.Emissive(new Color(0.1f, 0.2f, 0.6f), new Color(0.15f, 0.35f, 1f), 2.2f);
            var white = MaterialFactory.Emissive(Color.white, Color.white, 1.2f);
            float y = CityLayout.Sidewalk;
            var pos = center + new Vector3(-5f, y, -2f);
            var size = new Vector3(26f, 14f, 32f);
            MeshUtil.Solid(PrimitiveType.Cube, parent, pos + Vector3.up * size.y * 0.5f, size, wall).name = "PoliceStation";
            // Blue band and lit sign facing the east street.
            MeshUtil.Visual(PrimitiveType.Cube, parent, pos + new Vector3(size.x * 0.5f + 0.05f, 9f, 0f), new Vector3(0.1f, 0.8f, size.z), stripe);
            MeshUtil.Visual(PrimitiveType.Cube, parent, pos + new Vector3(size.x * 0.5f + 0.15f, 11.5f, 0f), new Vector3(0.2f, 1.6f, 8f), white);
            MeshUtil.Visual(PrimitiveType.Cube, parent, pos + new Vector3(size.x * 0.5f + 0.2f, 11.5f, 0f), new Vector3(0.2f, 1.2f, 7.4f), stripe);
            // Entrance canopy.
            MeshUtil.Visual(PrimitiveType.Cube, parent, pos + new Vector3(size.x * 0.5f + 1.5f, 3.4f, 0f), new Vector3(3f, 0.2f, 6f), wall);
            MeshUtil.Visual(PrimitiveType.Cube, parent, pos + new Vector3(size.x * 0.5f + 0.06f, 1.4f, 0f), new Vector3(0.1f, 2.8f, 3f), white);
        }

        public static GameObject PoliceCar(Transform parent, Vector3 pos, float yaw)
        {
            var root = new GameObject("PoliceCar");
            root.transform.SetParent(parent, false);
            root.transform.position = pos;
            root.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            var body = MaterialFactory.Lit(new Color(0.85f, 0.86f, 0.9f), 0.7f, 0.3f);
            var dark = MaterialFactory.Lit(new Color(0.05f, 0.06f, 0.1f), 0.8f);
            var solid = MeshUtil.Solid(PrimitiveType.Cube, root.transform, Vector3.zero, Vector3.one, body);
            solid.transform.localPosition = new Vector3(0f, 0.65f, 0f);
            solid.transform.localRotation = Quaternion.identity;
            solid.transform.localScale = new Vector3(2f, 0.9f, 4.5f);
            MeshUtil.Visual(PrimitiveType.Cube, root.transform, new Vector3(0f, 1.35f, -0.2f), new Vector3(1.8f, 0.6f, 2.3f), dark);
            MeshUtil.Visual(PrimitiveType.Cube, root.transform, new Vector3(0f, 0.55f, 0f), new Vector3(2.02f, 0.25f, 4.52f), MaterialFactory.Lit(new Color(0.1f, 0.15f, 0.45f)));
            var red = MeshUtil.Visual(PrimitiveType.Cube, root.transform, new Vector3(-0.4f, 1.72f, -0.2f), new Vector3(0.6f, 0.15f, 0.3f), MaterialFactory.EmissiveInstance(Color.red, Color.red * 3f));
            var blue = MeshUtil.Visual(PrimitiveType.Cube, root.transform, new Vector3(0.4f, 1.72f, -0.2f), new Vector3(0.6f, 0.15f, 0.3f), MaterialFactory.EmissiveInstance(Color.blue, Color.blue * 3f));
            var lights = root.AddComponent<PoliceLights>();
            lights.Setup(red.GetComponent<Renderer>(), blue.GetComponent<Renderer>());
            return root;
        }

        public static void Market(Transform parent, Vector3 center, System.Random rnd)
        {
            float y = CityLayout.Sidewalk;
            var tiles = MaterialFactory.Lit(new Color(0.24f, 0.17f, 0.15f), 0.45f);
            MeshUtil.Visual(PrimitiveType.Cube, parent, center + Vector3.up * (y + 0.01f), new Vector3(CityLayout.Block - 1f, 0.02f, CityLayout.Block - 1f), tiles);

            var red = MaterialFactory.Lit(new Color(0.55f, 0.06f, 0.05f), 0.4f);
            var wood = MaterialFactory.Lit(new Color(0.25f, 0.16f, 0.1f));
            Color[] awnings = { new Color(0.7f, 0.1f, 0.1f), new Color(0.1f, 0.35f, 0.6f), new Color(0.8f, 0.6f, 0.1f), new Color(0.2f, 0.5f, 0.25f) };

            // Two rows of stalls either side of the central aisle.
            for (int side = -1; side <= 1; side += 2)
            for (int n = 0; n < 4; n++)
            {
                var p = center + new Vector3(side * 8.5f, y, -14f + n * 9f);
                var stall = MeshUtil.Solid(PrimitiveType.Cube, parent, p + Vector3.up * 0.55f, new Vector3(2.4f, 1.1f, 4f), wood);
                stall.name = "Stall";
                var awning = MaterialFactory.Lit(awnings[rnd.Next(awnings.Length)], 0.2f);
                var aw = MeshUtil.Visual(PrimitiveType.Cube, parent, p + new Vector3(-side * 0.6f, 2.6f, 0f), new Vector3(3.4f, 0.08f, 4.4f), awning);
                aw.transform.rotation = Quaternion.Euler(0f, 0f, side * 12f);
                MeshUtil.Visual(PrimitiveType.Cube, parent, p + new Vector3(side * 1.1f, 1.8f, 1.9f), new Vector3(0.1f, 1.5f, 0.1f), wood);
                MeshUtil.Visual(PrimitiveType.Cube, parent, p + new Vector3(side * 1.1f, 1.8f, -1.9f), new Vector3(0.1f, 1.5f, 0.1f), wood);
                // Goods on the counter.
                for (int g = 0; g < 3; g++)
                    MeshUtil.Visual(PrimitiveType.Cube, parent, p + new Vector3(R(rnd, -0.6f, 0.6f), 1.25f, R(rnd, -1.5f, 1.5f)),
                        Vector3.one * R(rnd, 0.25f, 0.45f), MaterialFactory.Lit(NeonColors[rnd.Next(NeonColors.Length)] * 0.6f));
            }

            // Gates at both ends of the aisle.
            for (int end = -1; end <= 1; end += 2)
            {
                var g = center + new Vector3(0f, y, end * (CityLayout.Block * 0.5f - 0.6f));
                MeshUtil.Solid(PrimitiveType.Cube, parent, g + new Vector3(-3.8f, 2.8f, 0f), new Vector3(0.6f, 5.6f, 0.6f), red);
                MeshUtil.Solid(PrimitiveType.Cube, parent, g + new Vector3(3.8f, 2.8f, 0f), new Vector3(0.6f, 5.6f, 0.6f), red);
                MeshUtil.Visual(PrimitiveType.Cube, parent, g + new Vector3(0f, 5.8f, 0f), new Vector3(10f, 0.5f, 1.2f), red);
                MeshUtil.Visual(PrimitiveType.Cube, parent, g + new Vector3(0f, 6.25f, 0f), new Vector3(11f, 0.25f, 1.8f), MaterialFactory.Lit(new Color(0.1f, 0.1f, 0.1f)));
                MeshUtil.Visual(PrimitiveType.Cube, parent, g + new Vector3(0f, 5.1f, 0f), new Vector3(3f, 0.7f, 0.2f),
                    MaterialFactory.Emissive(new Color(0.8f, 0.6f, 0.1f), new Color(1f, 0.6f, 0.1f), 1.6f));
            }

            // Lantern strings across the aisle.
            for (int n = 0; n < 5; n++)
                LanternString(parent, center + new Vector3(-7f, y + 4.4f, -16f + n * 8f), center + new Vector3(7f, y + 4.4f, -16f + n * 8f), 7, rnd);

            for (int n = 0; n < 3; n++)
                AddLight(parent, center + new Vector3(0f, 3.8f, -12f + n * 12f), new Color(1f, 0.35f, 0.25f), 16f, 2.2f);
        }

        public static void LanternString(Transform parent, Vector3 a, Vector3 b, int count, System.Random rnd)
        {
            var wire = MaterialFactory.Lit(new Color(0.05f, 0.05f, 0.05f));
            var mid = (a + b) * 0.5f;
            var wireGo = MeshUtil.Visual(PrimitiveType.Cube, parent, mid, new Vector3(0.03f, 0.03f, Vector3.Distance(a, b)), wire);
            wireGo.transform.rotation = Quaternion.LookRotation(b - a);
            var lantern = MaterialFactory.Emissive(new Color(0.8f, 0.1f, 0.05f), new Color(1f, 0.15f, 0.05f), 2.5f);
            for (int i = 0; i < count; i++)
            {
                float t = (i + 0.5f) / count;
                var p = Vector3.Lerp(a, b, t) + Vector3.down * (0.35f + Mathf.Sin(t * Mathf.PI) * 0.35f);
                MeshUtil.Visual(PrimitiveType.Sphere, parent, p, new Vector3(0.45f, 0.6f, 0.45f), lantern);
            }
        }

        /// <summary>Docks yard: container walls with a gate on the west side. Returns the suspect's escape roof point.</summary>
        public static Vector3 Arena(Transform parent, Vector3 center, System.Random rnd)
        {
            float y = CityLayout.Sidewalk;
            var concrete = MaterialFactory.Lit(new Color(0.18f, 0.18f, 0.17f), 0.25f);
            MeshUtil.Visual(PrimitiveType.Cube, parent, center + Vector3.up * (y + 0.01f), new Vector3(CityLayout.Block - 0.5f, 0.02f, CityLayout.Block - 0.5f), concrete);
            Color[] paint = { new Color(0.45f, 0.12f, 0.08f), new Color(0.1f, 0.25f, 0.4f), new Color(0.15f, 0.35f, 0.2f), new Color(0.5f, 0.4f, 0.1f) };
            float edge = CityLayout.Block * 0.5f - 1.5f;
            const float len = 6f, height = 2.6f, depth = 2.4f;

            void Container(Vector3 p, bool alongX, int stack)
            {
                for (int s = 0; s < stack; s++)
                {
                    var size = alongX ? new Vector3(len, height, depth) : new Vector3(depth, height, len);
                    var c = MeshUtil.Solid(PrimitiveType.Cube, parent, p + Vector3.up * (y + height * 0.5f + s * height), size,
                        MaterialFactory.Lit(paint[rnd.Next(paint.Length)], 0.3f, 0.4f));
                    c.name = "Container";
                }
            }

            for (float t = -edge + len * 0.5f; t <= edge - len * 0.5f + 0.01f; t += len + 0.2f)
            {
                Container(center + new Vector3(t, 0f, -edge), true, 2);
                if (t < 6f) Container(center + new Vector3(t, 0f, edge), true, 2);
                if (t < 6f) Container(center + new Vector3(edge, 0f, t), false, 2);
                if (Mathf.Abs(t) > 7f) Container(center + new Vector3(-edge, 0f, t), false, 2); // west gate gap
            }

            // Warehouse in the north-east corner: the suspect escapes over its roof.
            var whSize = new Vector3(14f, 20f, 14f);
            var whPos = center + new Vector3(edge - whSize.x * 0.5f + 1.2f, y, edge - whSize.z * 0.5f + 1.2f);
            MeshUtil.Solid(PrimitiveType.Cube, parent, whPos + Vector3.up * whSize.y * 0.5f, whSize, MaterialFactory.Lit(new Color(0.22f, 0.24f, 0.26f), 0.2f, 0.5f)).name = "Warehouse";

            // Cover inside the yard.
            Vector3[] cover = { new Vector3(-8f, 0f, 8f), new Vector3(7f, 0f, -9f), new Vector3(-10f, 0f, -7f), new Vector3(9f, 0f, 4f), new Vector3(-2f, 0f, -13f) };
            foreach (var c in cover) Container(center + c, rnd.NextDouble() < 0.5, 1);

            // Floodlights.
            Vector3[] corners = { new Vector3(-edge + 2f, 0f, -edge + 2f), new Vector3(edge - 2f, 0f, -edge + 2f), new Vector3(-edge + 2f, 0f, edge - 2f) };
            var pole = MaterialFactory.Lit(new Color(0.3f, 0.3f, 0.3f), 0.5f, 0.8f);
            var head = MaterialFactory.Emissive(Color.white, new Color(1f, 0.95f, 0.85f), 3f);
            foreach (var c in corners)
            {
                var p = center + c;
                MeshUtil.Solid(PrimitiveType.Cube, parent, p + Vector3.up * 5f, new Vector3(0.3f, 10f, 0.3f), pole);
                MeshUtil.Visual(PrimitiveType.Cube, parent, p + Vector3.up * 10.2f, new Vector3(1.2f, 0.4f, 1.2f), head);
                var go = new GameObject("Floodlight");
                go.transform.SetParent(parent, false);
                go.transform.position = p + Vector3.up * 10f;
                go.transform.LookAt(center);
                var l = go.AddComponent<Light>();
                l.type = LightType.Spot;
                l.spotAngle = 75f;
                l.range = 45f;
                l.intensity = 2.2f;
                l.color = new Color(0.85f, 0.9f, 1f);
                l.shadows = LightShadows.Soft;
            }

            return whPos + new Vector3(0f, whSize.y, 0f);
        }

        public static void AlleyDressing(Transform parent, Vector3 center, System.Random rnd)
        {
            float y = CityLayout.Sidewalk;
            var bins = new[] { MaterialFactory.Lit(new Color(0.1f, 0.25f, 0.15f), 0.4f, 0.3f), MaterialFactory.Lit(new Color(0.12f, 0.15f, 0.3f), 0.4f, 0.3f) };
            var crate = MaterialFactory.Lit(new Color(0.3f, 0.22f, 0.12f));
            for (int n = 0; n < 5; n++)
            {
                bool northSouth = rnd.Next(2) == 0;
                float along = (rnd.Next(2) == 0 ? -1f : 1f) * R(rnd, 6f, 16f);
                float lateral = (rnd.Next(2) == 0 ? -1f : 1f) * 3.2f;
                var p = center + (northSouth ? new Vector3(lateral, 0f, along) : new Vector3(along, 0f, lateral));
                if (rnd.NextDouble() < 0.5)
                    MeshUtil.Solid(PrimitiveType.Cube, parent, p + Vector3.up * (y + 0.65f), new Vector3(1.1f, 1.3f, 1.1f), bins[rnd.Next(2)]).name = "Dumpster";
                else
                {
                    MeshUtil.Solid(PrimitiveType.Cube, parent, p + Vector3.up * (y + 0.4f), Vector3.one * 0.8f, crate).name = "Crate";
                    MeshUtil.Visual(PrimitiveType.Cube, parent, p + new Vector3(0.1f, y + 1.1f, 0.05f), Vector3.one * 0.6f, crate);
                }
            }
            // Lanterns strung across the alley near the crossing.
            float o = R(rnd, 7f, 12f);
            LanternString(parent, center + new Vector3(-3.4f, y + 4.2f, o), center + new Vector3(3.4f, y + 4.2f, o), 4, rnd);
            LanternString(parent, center + new Vector3(o, y + 4.6f, -3.4f), center + new Vector3(o, y + 4.6f, 3.4f), 4, rnd);
            AddLight(parent, center + new Vector3(0f, 3.6f, o), new Color(1f, 0.3f, 0.2f), 11f, 1.6f);
        }

        public static void NeonSign(Transform parent, Vector3 facePos, Vector3 normal, System.Random rnd)
        {
            var root = new GameObject("NeonSign").transform;
            root.SetParent(parent, false);
            root.position = facePos + normal * 0.8f;
            root.rotation = Quaternion.LookRotation(normal);
            int chars = 2 + rnd.Next(3);
            float cell = 1.1f, height = chars * cell + 0.3f;
            var board = MaterialFactory.Lit(new Color(0.04f, 0.04f, 0.05f));
            var color = NeonColors[rnd.Next(NeonColors.Length)];
            var glow = MaterialFactory.Emissive(color * 0.5f, color, 3f);
            MeshUtil.Visual(PrimitiveType.Cube, root, new Vector3(0f, height * 0.5f, 0f), new Vector3(0.18f, height, 1.3f), board);
            // Frame.
            MeshUtil.Visual(PrimitiveType.Cube, root, new Vector3(0f, height, 0f), new Vector3(0.22f, 0.06f, 1.34f), glow);
            MeshUtil.Visual(PrimitiveType.Cube, root, new Vector3(0f, 0f, 0f), new Vector3(0.22f, 0.06f, 1.34f), glow);
            // Abstract glyphs on both faces.
            for (int c = 0; c < chars; c++)
            {
                float cy = 0.15f + c * cell + cell * 0.5f;
                int strokes = 3 + rnd.Next(3);
                for (int s = 0; s < strokes; s++)
                {
                    bool horizontal = rnd.NextDouble() < 0.55;
                    var scale = horizontal ? new Vector3(0.22f, 0.07f, R(rnd, 0.4f, 0.85f)) : new Vector3(0.22f, R(rnd, 0.35f, 0.8f), 0.07f);
                    var pos = new Vector3(0f, cy + R(rnd, -0.3f, 0.3f), R(rnd, -0.3f, 0.3f));
                    MeshUtil.Visual(PrimitiveType.Cube, root, pos, scale, glow);
                }
            }
            if (rnd.NextDouble() < 0.12)
                AddLight(root, root.position + normal * 0.8f + Vector3.up * height * 0.5f, color, 7f, 1.2f);
        }

        public static void StreetLamp(Transform parent, Vector3 pos, bool lit)
        {
            var metal = MaterialFactory.Lit(new Color(0.15f, 0.15f, 0.16f), 0.5f, 0.7f);
            var head = MaterialFactory.Emissive(new Color(1f, 0.8f, 0.5f), new Color(1f, 0.65f, 0.35f), lit ? 3f : 1.2f);
            var poleGo = MeshUtil.Solid(PrimitiveType.Cylinder, parent, pos + Vector3.up * 3.2f, new Vector3(0.18f, 3.2f, 0.18f), metal);
            poleGo.name = "Lamp";
            var dir = new Vector3(-1f, 0f, -1f).normalized;
            MeshUtil.Visual(PrimitiveType.Cube, parent, pos + Vector3.up * 6.3f + dir * 0.8f, new Vector3(0.12f, 0.12f, 1.6f), metal)
                .transform.rotation = Quaternion.LookRotation(dir);
            MeshUtil.Visual(PrimitiveType.Cube, parent, pos + Vector3.up * 6.2f + dir * 1.6f, new Vector3(0.5f, 0.15f, 0.5f), head);
            if (lit) AddLight(parent, pos + Vector3.up * 6f + dir * 1.6f, new Color(1f, 0.7f, 0.42f), 17f, 1.6f);
        }

        public static Light AddLight(Transform parent, Vector3 pos, Color color, float range, float intensity)
        {
            var go = new GameObject("Light");
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = color;
            l.range = range;
            l.intensity = intensity;
            l.shadows = LightShadows.None;
            return l;
        }
    }
}
