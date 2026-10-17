using System.Collections.Generic;
using UnityEngine;

namespace Game
{
    public static class MeshUtil
    {
        static readonly Dictionary<Vector3, Mesh> boxCache = new Dictionary<Vector3, Mesh>();

        /// <summary>
        /// Box centred on its base (pivot at the bottom). Side faces get world-scaled UVs
        /// (one UV unit every <paramref name="uvTile"/> metres) so a window texture tiles evenly.
        /// The top and bottom faces map to UV (0,0), a plain wall pixel in the facade texture.
        /// </summary>
        public static Mesh FacadeBox(Vector3 size, float uvTile)
        {
            var key = new Vector3(Mathf.Round(size.x * 10f), Mathf.Round(size.y * 10f), Mathf.Round(size.z * 10f) + uvTile * 100000f);
            if (boxCache.TryGetValue(key, out var cached) && cached != null) return cached;

            var v = new List<Vector3>();
            var uv = new List<Vector2>();
            var tri = new List<int>();
            float hx = size.x * 0.5f, hz = size.z * 0.5f, h = size.y;

            void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float width, bool side)
            {
                int i = v.Count;
                v.Add(a); v.Add(b); v.Add(c); v.Add(d);
                if (side)
                {
                    float u = width / uvTile, w = h / uvTile;
                    uv.Add(new Vector2(0, 0)); uv.Add(new Vector2(u, 0));
                    uv.Add(new Vector2(u, w)); uv.Add(new Vector2(0, w));
                }
                else
                {
                    for (int k = 0; k < 4; k++) uv.Add(new Vector2(0.01f, 0.01f));
                }
                tri.Add(i); tri.Add(i + 2); tri.Add(i + 1);
                tri.Add(i); tri.Add(i + 3); tri.Add(i + 2);
            }

            // Front (-z), back (+z), left (-x), right (+x), top, bottom. Winding faces outward.
            Quad(new Vector3(-hx, 0, -hz), new Vector3(hx, 0, -hz), new Vector3(hx, h, -hz), new Vector3(-hx, h, -hz), size.x, true);
            Quad(new Vector3(hx, 0, hz), new Vector3(-hx, 0, hz), new Vector3(-hx, h, hz), new Vector3(hx, h, hz), size.x, true);
            Quad(new Vector3(-hx, 0, hz), new Vector3(-hx, 0, -hz), new Vector3(-hx, h, -hz), new Vector3(-hx, h, hz), size.z, true);
            Quad(new Vector3(hx, 0, -hz), new Vector3(hx, 0, hz), new Vector3(hx, h, hz), new Vector3(hx, h, -hz), size.z, true);
            Quad(new Vector3(-hx, h, -hz), new Vector3(hx, h, -hz), new Vector3(hx, h, hz), new Vector3(-hx, h, hz), 0, false);
            Quad(new Vector3(-hx, 0, hz), new Vector3(hx, 0, hz), new Vector3(hx, 0, -hz), new Vector3(-hx, 0, -hz), 0, false);

            var mesh = new Mesh { name = "Facade" };
            mesh.SetVertices(v);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(tri, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            boxCache[key] = mesh;
            return mesh;
        }

        /// <summary>Procedural night facade: dark concrete with a grid of randomly lit windows.</summary>
        public static void FacadeTextures(int seed, out Texture2D albedo, out Texture2D emission)
        {
            const int cells = 8, cell = 16, size = cells * cell;
            albedo = new Texture2D(size, size, TextureFormat.RGBA32, true) { name = "FacadeAlbedo", wrapMode = TextureWrapMode.Repeat };
            emission = new Texture2D(size, size, TextureFormat.RGBA32, true) { name = "FacadeEmission", wrapMode = TextureWrapMode.Repeat };
            var rnd = new System.Random(seed);
            var wall = new Color(0.16f, 0.16f, 0.18f);
            var glass = new Color(0.05f, 0.06f, 0.08f);
            Color[] warm = { new Color(1f, 0.75f, 0.4f), new Color(1f, 0.9f, 0.7f), new Color(0.6f, 0.85f, 1f), new Color(1f, 0.45f, 0.6f) };
            var a = new Color[size * size];
            var e = new Color[size * size];
            for (int cy = 0; cy < cells; cy++)
            for (int cx = 0; cx < cells; cx++)
            {
                bool lit = rnd.NextDouble() < 0.32;
                var light = warm[rnd.Next(warm.Length)] * (0.5f + (float)rnd.NextDouble() * 0.6f);
                for (int y = 0; y < cell; y++)
                for (int x = 0; x < cell; x++)
                {
                    int idx = (cy * cell + y) * size + cx * cell + x;
                    bool isWindow = x >= 3 && x <= 12 && y >= 4 && y <= 12;
                    // Keep pixel (0,0) of cell (0,0) as wall: roofs sample it.
                    a[idx] = isWindow ? glass : wall * (0.9f + 0.1f * ((x + y) % 3) / 2f);
                    e[idx] = isWindow && lit ? light : Color.black;
                }
            }
            albedo.SetPixels(a);
            emission.SetPixels(e);
            albedo.Apply(true);
            emission.Apply(true);
        }

        /// <summary>Creates a primitive without its collider (for purely visual parts).</summary>
        public static GameObject Visual(PrimitiveType type, Transform parent, Vector3 localPos, Vector3 localScale, Material mat)
        {
            var go = GameObject.CreatePrimitive(type);
            Object.Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }

        /// <summary>Creates a primitive that keeps its collider (solid world geometry).</summary>
        public static GameObject Solid(PrimitiveType type, Transform parent, Vector3 position, Vector3 scale, Material mat, Quaternion? rotation = null)
        {
            var go = GameObject.CreatePrimitive(type);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.rotation = rotation ?? Quaternion.identity;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            go.isStatic = true;
            return go;
        }
    }
}
