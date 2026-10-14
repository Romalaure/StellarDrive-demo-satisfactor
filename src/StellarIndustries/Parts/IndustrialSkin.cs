using System;
using System.IO;
using MelonLoader.Utils;
using StellarIndustries.Core;
using UnityEngine;

namespace StellarIndustries.Parts
{
    /// <summary>One square of the mod's texture atlas (8 x 8 tiles).</summary>
    internal enum Tile
    {
        PaintedLight, PaintedDark, PaintedOrange, PaintedYellow, PaintedBlue, PaintedGreen, Brushed, DarkSteel,
        Hazard, Grille, Tread, Rivets, Pipe, Rubber, Rust, Copper,
        CopperDark, Aluminum, Steel, Gold, Concrete, Circuit, Biomass, Coal,
        Screen, ScreenAmber, LedGreen, LedRed, Furnace, Gauge, LabelWarning, LabelPower,
        Fan, Glass, IngotIron, IngotCopper, IngotAluminum, IngotSteel, CrushedIron, CrushedCopper,
        StellarDust, Sack, CableJacket, Plastic, ArrowLabel, ControlPanel, BoltHeads, Winding,
        Belt, Stone, VeinIron, VeinCopper, VeinAluminum, StellarCrystal, Ice,
        IconSmelt, IconShape, IconAssemble, IconExtract, IconPower, IconLogistics, IconQuantum, IconChem
    }

    /// <summary>
    /// The mod's own textures, drawn by code (no image from another game): an atlas of 8 x 8
    /// tiles in the hand-drawn style of StellarDrive's parts (flat colors, dark outline, light
    /// bevel) with an albedo, a normal map (grooves, rivets, slots, treads) and an emission map
    /// (screens, lights, furnace mouths). They are worn with the game's own part shader
    /// (Custom/PlanetObject: _Albedo, _Normal, _EmissionMap), so the parts are lit like the game's.
    /// Generated once, then read back from UserData/StellarIndustries/cache.
    /// </summary>
    internal static class IndustrialSkin
    {
        private const int Version = 4;
        /// <summary>Border of each tile repeating its edge, so mipmaps never mix two tiles (fraction of a tile).</summary>
        private const float Gutter = 8f / 256f;
        private const int Grid = 8;
        private const int AlbedoSize = 2048, NormalSize = 1024, EmissionSize = 512;

        private static Texture2D _albedo, _normal, _emission;
        private static Material _material, _items;

        public static bool Ready => _material != null;

        /// <summary>The atlas rectangle of a tile, a little inside its edges (no bleeding from neighbors).</summary>
        public static Rect Uv(Tile tile)
        {
            int i = (int)tile;
            float size = 1f / Grid, inset = size * (Gutter + 0.004f);
            float x = (i % Grid) * size, y = 1f - (i / Grid + 1) * size;
            return new Rect(x + inset, y + inset, size - 2f * inset, size - 2f * inset);
        }

        /// <summary>Part material: the game's part shader and palette with the mod's atlas.</summary>
        public static Material Material
        {
            get
            {
                if (_material == null)
                    _material = Make("SI_Industrial", GameSkin.Material);
                return _material;
            }
        }

        /// <summary>Item material (same atlas, on the material of a game item).</summary>
        public static Material ItemMaterial(Material donor)
        {
            if (_items == null)
                _items = Make("SI_IndustrialItems", donor ?? GameSkin.Material);
            return _items;
        }

        private static Material Make(string name, Material from)
        {
            Build();
            Material material;
            if (from != null)
                material = new Material(from) { name = name };
            else
                material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard")) { name = name };
            material.hideFlags = HideFlags.DontUnloadUnusedAsset;
            if (material.HasProperty("_Albedo"))
                material.SetTexture("_Albedo", _albedo);
            else
                material.mainTexture = _albedo;
            if (material.HasProperty("_Color"))
                material.SetColor("_Color", Color.white);
            if (material.HasProperty("_Normal"))
            {
                material.SetTexture("_Normal", _normal);
                material.EnableKeyword("_NORMALMAP");
            }
            if (material.HasProperty("_EmissionMap"))
            {
                material.SetTexture("_EmissionMap", _emission);
                material.SetColor("_EmissionColor", new Color(1.6f, 1.6f, 1.6f));
                material.EnableKeyword("_EMISSION");
            }
            return material;
        }

        // ---- Generation and cache

        private static void Build()
        {
            if (_albedo != null)
                return;
            string folder = Path.Combine(MelonEnvironment.UserDataDirectory, "StellarIndustries", "cache");
            string a = Path.Combine(folder, $"atlas_albedo_v{Version}.png"), n = Path.Combine(folder, $"atlas_normal_v{Version}.png"), e = Path.Combine(folder, $"atlas_emission_v{Version}.png");
            var watch = System.Diagnostics.Stopwatch.StartNew();
            if (File.Exists(a) && File.Exists(n) && File.Exists(e))
            {
                _albedo = Load(a, false);
                _normal = Load(n, true);
                _emission = Load(e, false);
                if (_albedo != null && _normal != null && _emission != null)
                {
                    Finish();
                    IndustriesMod.Log.Msg($"skin: atlas read from cache in {watch.ElapsedMilliseconds} ms");
                    return;
                }
            }
            _albedo = Render(AlbedoSize, false, (tile, u, v) => Paint(tile, u, v).Albedo);
            _normal = RenderNormal(NormalSize);
            _emission = Render(EmissionSize, false, (tile, u, v) => Paint(tile, u, v).Glow);
            try
            {
                Directory.CreateDirectory(folder);
                File.WriteAllBytes(a, _albedo.EncodeToPNG());
                File.WriteAllBytes(n, _normal.EncodeToPNG());
                File.WriteAllBytes(e, _emission.EncodeToPNG());
            }
            catch (Exception ex)
            {
                IndustriesMod.Log.Warning("skin: cache not written: " + ex.Message);
            }
            Finish();
            IndustriesMod.Log.Msg($"skin: atlas drawn in {watch.ElapsedMilliseconds} ms");
        }

        private static Texture2D Load(string path, bool linear)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, true, linear);
            // Called by reflection: the compiler would otherwise weigh the Span overload, which needs netstandard 2.1
            System.Reflection.MethodInfo load = typeof(ImageConversion).GetMethod("LoadImage", new[] { typeof(Texture2D), typeof(byte[]) });
            return load != null && (bool)load.Invoke(null, new object[] { texture, File.ReadAllBytes(path) }) ? texture : null;
        }

        private static void Finish()
        {
            foreach (Texture2D t in new[] { _albedo, _normal, _emission })
            {
                t.wrapMode = TextureWrapMode.Clamp;
                t.filterMode = FilterMode.Trilinear;
                t.anisoLevel = 4;
                t.hideFlags = HideFlags.DontUnloadUnusedAsset;
            }
            _albedo.name = "SI_AtlasAlbedo";
            _normal.name = "SI_AtlasNormal";
            _emission.name = "SI_AtlasEmission";
            _albedo.Apply(true, false);
            _normal.Apply(true, false);
            _emission.Apply(true, false);
        }

        private static Texture2D Render(int size, bool linear, Func<Tile, float, float, Color> sample)
        {
            var pixels = new Color32[size * size];
            int tilePx = size / Grid;
            for (int y = 0; y < size; y++)
            {
                int row = Grid - 1 - y / tilePx;
                float v = Inner((y % tilePx + 0.5f) / tilePx);
                for (int x = 0; x < size; x++)
                {
                    int col = x / tilePx;
                    float u = Inner((x % tilePx + 0.5f) / tilePx);
                    var tile = (Tile)(row * Grid + col);
                    pixels[y * size + x] = (int)tile <= (int)Tile.IconChem ? (Color32)sample(tile, u, v) : (Color32)Color.black;
                }
            }
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, true, linear);
            texture.SetPixels32(pixels);
            return texture;
        }

        /// <summary>A position in a tile, with the gutter clamped to the tile's edge.</summary>
        private static float Inner(float t) => (Mathf.Clamp(t, Gutter, 1f - Gutter) - Gutter) / (1f - 2f * Gutter);

        /// <summary>Normals from the tiles' height (x, y, z in R, G, B and full alpha: read the same whether the shader unpacks RGB or AG).</summary>
        private static Texture2D RenderNormal(int size)
        {
            int tilePx = size / Grid;
            float step = 1f / tilePx;
            return Render(size, true, (tile, u, v) =>
            {
                float h = Paint(tile, u, v).Height;
                float hx = Paint(tile, Mathf.Min(1f, u + step), v).Height;
                float hy = Paint(tile, u, Mathf.Min(1f, v + step)).Height;
                var n = new Vector3((h - hx) * 6f, (h - hy) * 6f, 1f).normalized;
                return new Color(n.x * 0.5f + 0.5f, n.y * 0.5f + 0.5f, n.z * 0.5f + 0.5f, 1f);
            });
        }

        // ---- The tiles

        private struct Texel
        {
            public Color Albedo;
            public float Height;
            public Color Glow;
        }

        private static readonly Color Outline = new Color(0.13f, 0.13f, 0.15f);

        private static Texel Paint(Tile tile, float u, float v)
        {
            var t = new Texel { Glow = Color.black, Height = 0.5f };
            float grain = Noise(u * 40f, v * 40f, (int)tile) * 0.06f - 0.03f;
            switch (tile)
            {
                case Tile.PaintedLight: Panel(ref t, u, v, new Color(0.78f, 0.79f, 0.8f), grain, rivets: true); break;
                case Tile.PaintedDark: Panel(ref t, u, v, new Color(0.3f, 0.32f, 0.35f), grain, rivets: true); break;
                case Tile.PaintedOrange: Panel(ref t, u, v, new Color(0.95f, 0.5f, 0.12f), grain, rivets: false); break;
                case Tile.PaintedYellow: Panel(ref t, u, v, new Color(0.96f, 0.78f, 0.15f), grain, rivets: false); break;
                case Tile.PaintedBlue: Panel(ref t, u, v, new Color(0.2f, 0.42f, 0.7f), grain, rivets: false); break;
                case Tile.PaintedGreen: Panel(ref t, u, v, new Color(0.3f, 0.58f, 0.32f), grain, rivets: false); break;
                case Tile.Brushed:
                    Panel(ref t, u, v, new Color(0.68f, 0.7f, 0.73f) * (0.92f + 0.12f * Noise(u * 3f, v * 180f, 7)), 0f, rivets: false);
                    break;
                case Tile.DarkSteel:
                    Panel(ref t, u, v, new Color(0.24f, 0.25f, 0.27f) * (0.9f + 0.2f * Noise(u * 12f, v * 12f, 9)), grain, rivets: true);
                    break;
                case Tile.Hazard:
                {
                    bool yellow = Mathf.Repeat((u + v) * 4f, 1f) < 0.5f;
                    Panel(ref t, u, v, yellow ? new Color(0.97f, 0.76f, 0.1f) : new Color(0.1f, 0.1f, 0.11f), grain, rivets: false);
                    break;
                }
                case Tile.Grille:
                {
                    Panel(ref t, u, v, new Color(0.34f, 0.35f, 0.38f), grain, rivets: false);
                    float slot = Mathf.Repeat(v * 9f, 1f);
                    if (u > 0.1f && u < 0.9f && v > 0.08f && v < 0.92f && slot > 0.3f && slot < 0.8f)
                    {
                        t.Albedo = new Color(0.05f, 0.05f, 0.06f);
                        t.Height = 0.1f;
                    }
                    break;
                }
                case Tile.Tread:
                {
                    Panel(ref t, u, v, new Color(0.55f, 0.57f, 0.6f), grain, rivets: false);
                    float a = Mathf.Repeat(u * 8f + v * 8f, 1f), b = Mathf.Repeat(u * 8f - v * 8f, 1f);
                    float cell = Mathf.Repeat((Mathf.Floor(u * 8f) + Mathf.Floor(v * 8f)), 2f);
                    float d = cell < 1f ? Mathf.Abs(a - 0.5f) : Mathf.Abs(b - 0.5f);
                    if (d < 0.12f && Inside(u, v, 0.06f))
                    {
                        t.Albedo *= 1.25f;
                        t.Height = 0.85f;
                    }
                    break;
                }
                case Tile.Rivets:
                {
                    Panel(ref t, u, v, new Color(0.6f, 0.62f, 0.65f), grain, rivets: false);
                    float ru = Mathf.Repeat(u * 6f, 1f) - 0.5f, rv = Mathf.Repeat(v * 6f, 1f) - 0.5f;
                    if (ru * ru + rv * rv < 0.025f && Inside(u, v, 0.06f))
                    {
                        t.Albedo = new Color(0.8f, 0.82f, 0.85f);
                        t.Height = 0.9f;
                    }
                    break;
                }
                case Tile.Pipe:
                {
                    t.Albedo = new Color(0.62f, 0.64f, 0.67f) * (0.95f + 0.1f * Noise(u * 60f, v * 2f, 3));
                    // Bands near both ends, along v
                    if (v < 0.08f || v > 0.92f || Mathf.Abs(v - 0.5f) < 0.03f)
                    {
                        t.Albedo = new Color(0.4f, 0.42f, 0.45f);
                        t.Height = 0.75f;
                    }
                    break;
                }
                case Tile.Rubber: Flat(ref t, u, v, new Color(0.08f, 0.08f, 0.09f), grain * 0.5f); break;
                case Tile.Rust:
                {
                    float r = Noise(u * 10f, v * 10f, 21);
                    Panel(ref t, u, v, Color.Lerp(new Color(0.45f, 0.25f, 0.14f), new Color(0.68f, 0.38f, 0.17f), r), grain, rivets: true);
                    break;
                }
                case Tile.Copper: Metal(ref t, u, v, new Color(0.86f, 0.5f, 0.3f), 11); break;
                case Tile.CopperDark: Metal(ref t, u, v, new Color(0.55f, 0.3f, 0.18f), 12); break;
                case Tile.Aluminum: Metal(ref t, u, v, new Color(0.86f, 0.88f, 0.9f), 13); break;
                case Tile.Steel: Metal(ref t, u, v, new Color(0.48f, 0.52f, 0.58f), 14); break;
                case Tile.Gold: Metal(ref t, u, v, new Color(0.9f, 0.72f, 0.25f), 15); break;
                case Tile.Concrete:
                {
                    float s = Noise(u * 50f, v * 50f, 31);
                    Flat(ref t, u, v, new Color(0.66f, 0.65f, 0.62f) * (0.85f + 0.2f * s), 0f);
                    t.Height = 0.4f + 0.2f * s;
                    break;
                }
                case Tile.Circuit:
                {
                    Flat(ref t, u, v, new Color(0.08f, 0.4f, 0.2f), grain);
                    bool trace = Mathf.Abs(Mathf.Repeat(u * 7f + (Noise(v * 3f, 0f, 5) > 0.5f ? 0.5f : 0f), 1f) - 0.5f) < 0.06f
                                 || Mathf.Abs(Mathf.Repeat(v * 5f, 1f) - 0.5f) < 0.04f && Noise(u * 4f, v * 5f, 6) > 0.45f;
                    if (trace)
                        t.Albedo = new Color(0.85f, 0.7f, 0.3f);
                    float cu = Mathf.Repeat(u * 3f, 1f) - 0.5f, cv = Mathf.Repeat(v * 3f, 1f) - 0.5f;
                    if (Mathf.Abs(cu) < 0.18f && Mathf.Abs(cv) < 0.14f)
                    {
                        t.Albedo = new Color(0.1f, 0.1f, 0.12f);
                        t.Height = 0.8f;
                    }
                    Border(ref t, u, v, 0.03f);
                    break;
                }
                case Tile.Biomass:
                {
                    float f = Noise(u * 4f, v * 70f, 41);
                    Flat(ref t, u, v, Color.Lerp(new Color(0.4f, 0.27f, 0.14f), new Color(0.62f, 0.45f, 0.25f), f), 0f);
                    t.Height = 0.3f + 0.4f * f;
                    Border(ref t, u, v, 0.03f);
                    break;
                }
                case Tile.Coal:
                {
                    float g = Noise(u * 30f, v * 30f, 43);
                    Flat(ref t, u, v, new Color(0.1f, 0.1f, 0.11f) * (0.7f + 0.8f * g), 0f);
                    t.Height = g;
                    break;
                }
                case Tile.Screen:
                {
                    Flat(ref t, u, v, new Color(0.04f, 0.08f, 0.14f), 0f);
                    float wave = 0.5f + 0.25f * Mathf.Sin(u * 18f) * Mathf.Sin(u * 5f + 1f);
                    bool line = Mathf.Abs(v - wave) < 0.025f || Mathf.Abs(Mathf.Repeat(v * 6f, 1f)) < 0.04f && u > 0.1f && u < 0.9f;
                    if (u > 0.08f && u < 0.92f && v > 0.08f && v < 0.92f && line)
                    {
                        t.Albedo = new Color(0.3f, 0.9f, 1f);
                        t.Glow = new Color(0.2f, 0.75f, 0.9f);
                    }
                    else if (Inside(u, v, 0.08f))
                    {
                        t.Glow = new Color(0.02f, 0.08f, 0.12f);
                    }
                    Border(ref t, u, v, 0.08f);
                    break;
                }
                case Tile.ScreenAmber:
                {
                    Flat(ref t, u, v, new Color(0.12f, 0.06f, 0.02f), 0f);
                    bool bar = Mathf.Repeat(u * 5f, 1f) < 0.6f && v < 0.2f + 0.6f * Noise(Mathf.Floor(u * 5f), 0f, 51);
                    if (Inside(u, v, 0.12f) && bar)
                    {
                        t.Albedo = new Color(1f, 0.65f, 0.15f);
                        t.Glow = new Color(0.9f, 0.5f, 0.1f);
                    }
                    Border(ref t, u, v, 0.08f);
                    break;
                }
                case Tile.LedGreen: Led(ref t, u, v, new Color(0.3f, 1f, 0.4f)); break;
                case Tile.LedRed: Led(ref t, u, v, new Color(1f, 0.25f, 0.15f)); break;
                case Tile.Furnace:
                {
                    float heat = Mathf.Clamp01(1f - v * 0.8f + 0.25f * Noise(u * 8f, v * 8f, 61));
                    t.Albedo = Color.Lerp(new Color(0.8f, 0.2f, 0.02f), new Color(1f, 0.85f, 0.4f), heat);
                    t.Glow = t.Albedo * 1.2f;
                    break;
                }
                case Tile.Gauge:
                {
                    Flat(ref t, u, v, new Color(0.2f, 0.2f, 0.22f), 0f);
                    float du = u - 0.5f, dv = v - 0.5f, r = Mathf.Sqrt(du * du + dv * dv);
                    if (r < 0.42f)
                        t.Albedo = new Color(0.92f, 0.92f, 0.88f);
                    if (r > 0.36f && r < 0.42f && Mathf.Atan2(dv, du) < -0.3f && Mathf.Atan2(dv, du) > -1.4f)
                        t.Albedo = new Color(0.85f, 0.15f, 0.1f);
                    // Needle towards upper right
                    Vector2 needle = new Vector2(0.7f, 0.7f).normalized;
                    float along = du * needle.x + dv * needle.y, across = Mathf.Abs(-du * needle.y + dv * needle.x);
                    if (along > 0f && along < 0.34f && across < 0.02f)
                        t.Albedo = Outline;
                    if (r > 0.42f && r < 0.47f)
                        t.Height = 0.8f;
                    break;
                }
                case Tile.LabelWarning:
                {
                    Panel(ref t, u, v, new Color(0.97f, 0.8f, 0.15f), grain, rivets: false);
                    float tri = v - 0.18f;
                    if (tri > 0f && tri < 0.6f && Mathf.Abs(u - 0.5f) < (0.6f - tri) * 0.6f)
                        t.Albedo = Outline;
                    if (tri > 0.06f && tri < 0.5f && Mathf.Abs(u - 0.5f) < (0.48f - tri) * 0.55f)
                        t.Albedo = new Color(0.97f, 0.8f, 0.15f);
                    if (Mathf.Abs(u - 0.5f) < 0.03f && (tri > 0.18f && tri < 0.36f || tri > 0.1f && tri < 0.14f))
                        t.Albedo = Outline;
                    break;
                }
                case Tile.LabelPower:
                {
                    bool yellow = Mathf.Repeat((u + v) * 3f, 1f) < 0.5f;
                    Panel(ref t, u, v, yellow ? new Color(0.97f, 0.76f, 0.1f) : new Color(0.12f, 0.12f, 0.13f), grain, rivets: false);
                    if (Inside(u, v, 0.22f))
                    {
                        t.Albedo = new Color(0.12f, 0.12f, 0.13f);
                        // Lightning bolt
                        float x = u - 0.5f, y = v - 0.5f;
                        bool bolt = y > 0f ? Mathf.Abs(x + y * 0.6f - 0.04f) < 0.07f && y < 0.25f : Mathf.Abs(x + y * 0.6f + 0.04f) < 0.07f && y > -0.25f;
                        if (bolt)
                            t.Albedo = new Color(0.98f, 0.82f, 0.15f);
                    }
                    break;
                }
                case Tile.Fan:
                {
                    Flat(ref t, u, v, new Color(0.3f, 0.31f, 0.34f), grain);
                    float du = u - 0.5f, dv = v - 0.5f, r = Mathf.Sqrt(du * du + dv * dv);
                    if (r < 0.44f)
                    {
                        float blades = Mathf.Repeat(Mathf.Atan2(dv, du) / (Mathf.PI * 2f) * 6f + r * 1.5f, 1f);
                        t.Albedo = blades < 0.5f && r > 0.1f ? new Color(0.5f, 0.52f, 0.55f) : new Color(0.06f, 0.06f, 0.07f);
                        t.Height = blades < 0.5f ? 0.7f : 0.2f;
                    }
                    if (r > 0.44f && r < 0.48f)
                        t.Albedo = Outline;
                    break;
                }
                case Tile.Glass:
                {
                    Flat(ref t, u, v, new Color(0.62f, 0.8f, 0.88f), 0f);
                    if (Mathf.Abs(u - v - 0.2f) < 0.05f || Mathf.Abs(u - v + 0.15f) < 0.025f)
                        t.Albedo = new Color(0.9f, 0.97f, 1f);
                    Border(ref t, u, v, 0.03f);
                    break;
                }
                case Tile.IngotIron: Ingot(ref t, u, v, new Color(0.55f, 0.57f, 0.6f), 71); break;
                case Tile.IngotCopper: Ingot(ref t, u, v, new Color(0.85f, 0.45f, 0.25f), 72); break;
                case Tile.IngotAluminum: Ingot(ref t, u, v, new Color(0.85f, 0.87f, 0.9f), 73); break;
                case Tile.IngotSteel: Ingot(ref t, u, v, new Color(0.4f, 0.45f, 0.52f), 74); break;
                case Tile.CrushedIron: Granules(ref t, u, v, new Color(0.45f, 0.33f, 0.3f), 81); break;
                case Tile.CrushedCopper: Granules(ref t, u, v, new Color(0.7f, 0.42f, 0.28f), 82); break;
                case Tile.StellarDust:
                {
                    Granules(ref t, u, v, new Color(0.6f, 0.5f, 0.8f), 83);
                    if (Noise(u * 64f, v * 64f, 84) > 0.86f)
                    {
                        t.Albedo = new Color(1f, 0.95f, 1f);
                        t.Glow = new Color(0.5f, 0.4f, 0.7f);
                    }
                    break;
                }
                case Tile.Sack:
                {
                    float weave = (Mathf.Repeat(u * 40f, 1f) < 0.5f ^ Mathf.Repeat(v * 40f, 1f) < 0.5f) ? 1f : 0.9f;
                    Flat(ref t, u, v, new Color(0.72f, 0.6f, 0.4f) * weave, grain);
                    if (Mathf.Abs(v - 0.85f) < 0.02f)
                        t.Albedo = new Color(0.4f, 0.3f, 0.2f);
                    break;
                }
                case Tile.CableJacket:
                {
                    Flat(ref t, u, v, new Color(0.12f, 0.12f, 0.13f), grain);
                    if (Mathf.Abs(u - 0.5f) < 0.06f)
                        t.Albedo = new Color(0.9f, 0.55f, 0.15f);
                    break;
                }
                case Tile.Plastic: Panel(ref t, u, v, new Color(0.88f, 0.88f, 0.85f), grain, rivets: false); break;
                case Tile.ArrowLabel:
                {
                    Panel(ref t, u, v, new Color(0.15f, 0.16f, 0.18f), grain, rivets: false);
                    float x = u - 0.5f, y = v - 0.5f;
                    bool arrow = (Mathf.Abs(x) < 0.08f && y < 0.12f && y > -0.32f) || (y >= 0.12f && y < 0.34f && Mathf.Abs(x) < (0.34f - y) * 1.1f);
                    if (arrow)
                        t.Albedo = new Color(0.95f, 0.95f, 0.9f);
                    break;
                }
                case Tile.ControlPanel:
                {
                    Panel(ref t, u, v, new Color(0.42f, 0.44f, 0.48f), grain, rivets: false);
                    float bu = Mathf.Repeat(u * 4f, 1f) - 0.5f, bv = Mathf.Repeat(v * 3f, 1f) - 0.5f;
                    if (Inside(u, v, 0.1f) && bu * bu + bv * bv < 0.06f)
                    {
                        int index = (int)(u * 4f) + (int)(v * 3f) * 4;
                        Color c = index % 3 == 0 ? new Color(0.2f, 0.9f, 0.3f) : index % 3 == 1 ? new Color(0.95f, 0.3f, 0.15f) : new Color(0.95f, 0.8f, 0.2f);
                        t.Albedo = c;
                        t.Glow = c * 0.7f;
                        t.Height = 0.75f;
                    }
                    break;
                }
                case Tile.BoltHeads:
                {
                    Flat(ref t, u, v, new Color(0.5f, 0.52f, 0.55f), grain);
                    float bu = Mathf.Repeat(u * 4f, 1f) - 0.5f, bv = Mathf.Repeat(v * 4f, 1f) - 0.5f;
                    float hex = Mathf.Max(Mathf.Abs(bu), Mathf.Abs(bu) * 0.5f + Mathf.Abs(bv) * 0.866f);
                    if (hex < 0.3f)
                    {
                        t.Albedo = new Color(0.72f, 0.74f, 0.77f);
                        t.Height = 0.85f;
                        if (Mathf.Abs(bv) < 0.04f && Mathf.Abs(bu) < 0.2f)
                            t.Albedo = Outline;
                    }
                    break;
                }
                case Tile.Winding:
                {
                    float w = Mathf.Repeat(v * 40f, 1f);
                    t.Albedo = new Color(0.85f, 0.48f, 0.26f) * (0.75f + 0.35f * Mathf.Sin(w * Mathf.PI));
                    t.Height = 0.3f + 0.5f * Mathf.Sin(w * Mathf.PI);
                    break;
                }
                case Tile.Belt:
                {
                    // Rubber belt, its chevrons pointing up the tile (the way the items go)
                    Flat(ref t, u, v, new Color(0.11f, 0.11f, 0.12f), grain * 0.5f);
                    float chevron = Mathf.Repeat(v * 2f + Mathf.Abs(u - 0.5f) * 1.2f, 1f);
                    if (u > 0.14f && u < 0.86f && chevron < 0.16f)
                    {
                        t.Albedo = new Color(0.32f, 0.33f, 0.35f);
                        t.Height = 0.8f;
                    }
                    if (u < 0.07f || u > 0.93f)
                    {
                        t.Albedo = new Color(0.05f, 0.05f, 0.06f);
                        t.Height = 0.3f;
                    }
                    break;
                }
                case Tile.Stone: Stone(ref t, u, v, 91); break;
                case Tile.VeinIron: Vein(ref t, u, v, new Color(0.42f, 0.2f, 0.14f), new Color(0.3f, 0.3f, 0.34f), 92); break;
                case Tile.VeinCopper: Vein(ref t, u, v, new Color(0.85f, 0.48f, 0.25f), new Color(0.25f, 0.62f, 0.5f), 93); break;
                case Tile.VeinAluminum: Vein(ref t, u, v, new Color(0.85f, 0.86f, 0.9f), new Color(0.7f, 0.42f, 0.32f), 94); break;
                case Tile.StellarCrystal:
                {
                    float f = Noise(u * 6f, v * 6f, 95);
                    Flat(ref t, u, v, Color.Lerp(new Color(0.35f, 0.2f, 0.65f), new Color(0.65f, 0.55f, 1f), f), 0f);
                    // Facet edges and a glow from inside
                    float facet = Mathf.Abs(Mathf.Repeat(u * 3f + f, 1f) - 0.5f);
                    if (facet < 0.04f)
                        t.Albedo = new Color(0.85f, 0.8f, 1f);
                    t.Glow = new Color(0.25f, 0.15f, 0.5f) * (0.6f + 0.6f * f);
                    t.Height = 0.4f + 0.3f * f;
                    break;
                }
                case Tile.Ice:
                {
                    float f = Noise(u * 8f, v * 8f, 96);
                    Flat(ref t, u, v, Color.Lerp(new Color(0.62f, 0.8f, 0.92f), new Color(0.88f, 0.95f, 1f), f), 0f);
                    if (Mathf.Abs(Noise(u * 5f, v * 5f, 97) - 0.5f) < 0.025f)
                        t.Albedo = Color.white;
                    t.Height = 0.45f + 0.2f * f;
                    break;
                }
                case Tile.IconSmelt:
                {
                    // Flame over a crucible
                    Badge(ref t, u, v, new Color(0.75f, 0.25f, 0.1f));
                    float x = u - 0.5f, y = v - 0.5f;
                    bool flame = (x * x + (y + 0.02f) * (y + 0.02f) < 0.022f) || (y > -0.02f && y < 0.26f && Mathf.Abs(x + Mathf.Sin(y * 9f) * 0.03f) < (0.26f - y) * 0.55f);
                    bool pot = y < -0.13f && y > -0.25f && Mathf.Abs(x) < 0.2f - (-0.13f - y) * 0.6f;
                    if (flame && y > -0.13f || pot)
                        Symbol(ref t);
                    break;
                }
                case Tile.IconShape:
                {
                    // A ram coming down on a plate
                    Badge(ref t, u, v, new Color(0.3f, 0.35f, 0.42f));
                    float x = u - 0.5f, y = v - 0.5f;
                    bool shaft = Mathf.Abs(x) < 0.05f && y > 0.02f && y < 0.28f;
                    bool head = y <= 0.02f && y > -0.12f && Mathf.Abs(x) < (y + 0.12f) * 1.1f;
                    bool plate = y < -0.16f && y > -0.24f && Mathf.Abs(x) < 0.25f;
                    if (shaft || head || plate)
                        Symbol(ref t);
                    break;
                }
                case Tile.IconAssemble:
                {
                    // A gear
                    Badge(ref t, u, v, new Color(0.2f, 0.42f, 0.7f));
                    float x = u - 0.5f, y = v - 0.5f, r = Mathf.Sqrt(x * x + y * y), a = Mathf.Atan2(y, x);
                    float outer = Mathf.Cos(a * 8f) > 0.2f ? 0.3f : 0.23f;
                    if (r < outer && r > 0.1f)
                        Symbol(ref t);
                    break;
                }
                case Tile.IconExtract:
                {
                    // A drill bit going into the ground
                    Badge(ref t, u, v, new Color(0.5f, 0.32f, 0.16f));
                    float x = u - 0.5f, y = v - 0.5f;
                    bool body = Mathf.Abs(x) < 0.1f && y > 0.08f && y < 0.3f;
                    bool bit = y <= 0.08f && y > -0.2f && Mathf.Abs(x) < (y + 0.2f) * 0.36f && Mathf.Repeat((y - x * 0.6f) * 12f, 1f) < 0.7f;
                    bool ground = y < -0.22f && y > -0.27f && Mathf.Abs(x) < 0.28f;
                    if (body || bit || ground)
                        Symbol(ref t);
                    break;
                }
                case Tile.IconPower:
                {
                    Badge(ref t, u, v, new Color(0.85f, 0.62f, 0.05f));
                    float x = u - 0.5f, y = v - 0.5f;
                    bool bolt = y > 0f ? Mathf.Abs(x + y * 0.6f - 0.04f) < 0.07f && y < 0.28f : Mathf.Abs(x + y * 0.6f + 0.04f) < 0.07f && y > -0.28f;
                    if (bolt)
                        Symbol(ref t);
                    break;
                }
                case Tile.IconLogistics:
                {
                    // Two arrows, one each way
                    Badge(ref t, u, v, new Color(0.25f, 0.55f, 0.3f));
                    float x = u - 0.5f, y = v - 0.5f;
                    bool top = Mathf.Abs(y - 0.12f) < 0.035f && x > -0.25f && x < 0.12f || x >= 0.12f && x < 0.26f && Mathf.Abs(y - 0.12f) < (0.26f - x) * 0.6f;
                    bool bottom = Mathf.Abs(y + 0.12f) < 0.035f && x < 0.25f && x > -0.12f || x <= -0.12f && x > -0.26f && Mathf.Abs(y + 0.12f) < (x + 0.26f) * 0.6f;
                    if (top || bottom)
                        Symbol(ref t);
                    break;
                }
                case Tile.IconQuantum:
                {
                    // Three orbits round a nucleus
                    Badge(ref t, u, v, new Color(0.45f, 0.25f, 0.65f));
                    float x = u - 0.5f, y = v - 0.5f;
                    bool atom = x * x + y * y < 0.0025f;
                    for (int i = 0; i < 3 && !atom; i++)
                    {
                        float a = i * Mathf.PI / 3f, c = Mathf.Cos(a), s = Mathf.Sin(a);
                        float ex = (x * c + y * s) / 0.29f, ey = (-x * s + y * c) / 0.1f;
                        atom = Mathf.Abs(Mathf.Sqrt(ex * ex + ey * ey) - 1f) < 0.16f;
                    }
                    if (atom)
                        Symbol(ref t);
                    break;
                }
                case Tile.IconChem:
                {
                    // A flask
                    Badge(ref t, u, v, new Color(0.15f, 0.55f, 0.55f));
                    float x = u - 0.5f, y = v - 0.5f;
                    bool neck = Mathf.Abs(x) < 0.05f && y > 0.02f && y < 0.26f;
                    bool body = y <= 0.02f && y > -0.24f && Mathf.Abs(x) < 0.05f + (0.02f - y) * 0.75f;
                    bool rim = Mathf.Abs(x) < 0.09f && y > 0.23f && y < 0.28f;
                    if (neck || body || rim)
                        Symbol(ref t);
                    break;
                }
            }
            t.Albedo.a = 1f;
            return t;
        }

        /// <summary>A round sign: colored disc with a white ring on a dark plate.</summary>
        private static void Badge(ref Texel t, float u, float v, Color color)
        {
            Panel(ref t, u, v, new Color(0.16f, 0.17f, 0.19f), 0f, rivets: false);
            float du = u - 0.5f, dv = v - 0.5f, r = Mathf.Sqrt(du * du + dv * dv);
            if (r < 0.42f)
            {
                t.Albedo = color;
                t.Height = 0.6f;
            }
            else if (r < 0.46f)
            {
                t.Albedo = new Color(0.95f, 0.95f, 0.92f);
                t.Height = 0.7f;
            }
        }

        private static void Symbol(ref Texel t)
        {
            t.Albedo = new Color(0.97f, 0.97f, 0.94f);
            t.Height = 0.75f;
        }

        /// <summary>Weathered rock: grey-brown, lumpy, with cracks.</summary>
        private static void Stone(ref Texel t, float u, float v, int seed)
        {
            float big = Noise(u * 5f, v * 5f, seed), small = Noise(u * 28f, v * 28f, seed + 1);
            Flat(ref t, u, v, Color.Lerp(new Color(0.34f, 0.32f, 0.3f), new Color(0.56f, 0.53f, 0.48f), big) * (0.88f + 0.24f * small), 0f);
            t.Height = 0.3f + 0.4f * big + 0.15f * small;
            if (Mathf.Abs(Noise(u * 7f, v * 7f, seed + 2) - 0.5f) < 0.02f)
            {
                t.Albedo *= 0.45f;
                t.Height = 0.1f;
            }
        }

        /// <summary>Rock crossed by veins of ore (main color) with a second mineral at their edges.</summary>
        private static void Vein(ref Texel t, float u, float v, Color ore, Color edge, int seed)
        {
            Stone(ref t, u, v, seed);
            float n = Noise(u * 4f + Noise(u * 9f, v * 9f, seed + 5) * 1.5f, v * 4f, seed + 3);
            float d = Mathf.Abs(n - 0.5f);
            if (d < 0.07f)
            {
                float glint = Noise(u * 60f, v * 60f, seed + 4);
                t.Albedo = ore * (0.8f + 0.4f * glint);
                t.Height = 0.75f;
            }
            else if (d < 0.1f)
            {
                t.Albedo = Color.Lerp(t.Albedo, edge, 0.7f);
            }
        }


        /// <summary>A painted plate: flat color, dark outline, light bevel inside, optional corner rivets.</summary>
        private static void Panel(ref Texel t, float u, float v, Color color, float grain, bool rivets)
        {
            Flat(ref t, u, v, color, grain);
            float edge = Mathf.Min(Mathf.Min(u, 1f - u), Mathf.Min(v, 1f - v));
            if (edge < 0.035f)
            {
                t.Albedo = Outline;
                t.Height = 0.2f;
            }
            else if (edge < 0.06f)
            {
                // Light from above-left: top and left bevels lighter
                bool lit = v > 1f - 0.06f || u < 0.06f;
                t.Albedo = color * (lit ? 1.18f : 0.82f);
                t.Height = 0.62f;
            }
            if (rivets)
            {
                foreach (float cu in new[] { 0.12f, 0.88f })
                {
                    foreach (float cv in new[] { 0.12f, 0.88f })
                    {
                        float du = u - cu, dv = v - cv;
                        float d = du * du + dv * dv;
                        if (d < 0.0016f)
                        {
                            t.Albedo = color * (dv > 0f ? 1.3f : 0.9f);
                            t.Height = 0.9f;
                        }
                        else if (d < 0.0025f)
                        {
                            t.Albedo = Outline;
                        }
                    }
                }
            }
        }

        private static void Flat(ref Texel t, float u, float v, Color color, float grain)
        {
            t.Albedo = new Color(Mathf.Clamp01(color.r + grain), Mathf.Clamp01(color.g + grain), Mathf.Clamp01(color.b + grain));
            t.Height = 0.5f;
        }

        private static void Border(ref Texel t, float u, float v, float width)
        {
            if (Mathf.Min(Mathf.Min(u, 1f - u), Mathf.Min(v, 1f - v)) < width)
            {
                t.Albedo = Outline;
                t.Height = 0.3f;
            }
        }

        private static void Metal(ref Texel t, float u, float v, Color color, int seed)
        {
            float streak = Noise(u * 4f, v * 120f, seed);
            float sheen = 0.85f + 0.3f * Mathf.Clamp01(1f - Mathf.Abs(u + v - 1f) * 1.4f);
            Flat(ref t, u, v, color * (0.9f + 0.15f * streak) * sheen, 0f);
            Border(ref t, u, v, 0.025f);
        }

        private static void Ingot(ref Texel t, float u, float v, Color color, int seed)
        {
            Metal(ref t, u, v, color, seed);
            float edge = Mathf.Min(Mathf.Min(u, 1f - u), Mathf.Min(v, 1f - v));
            if (edge < 0.12f && edge >= 0.025f)
                t.Albedo *= v > 0.5f || u < 0.5f ? 1.2f : 0.8f;
            if (Noise(u * 20f, v * 20f, seed + 1) > 0.82f)
                t.Albedo *= 0.85f;
        }

        private static void Granules(ref Texel t, float u, float v, Color color, int seed)
        {
            float g = Noise(u * 36f, v * 36f, seed);
            float g2 = Noise(u * 90f, v * 90f, seed + 1);
            Flat(ref t, u, v, color * (0.65f + 0.6f * g) * (0.9f + 0.2f * g2), 0f);
            t.Height = 0.2f + 0.7f * g;
        }

        private static void Led(ref Texel t, float u, float v, Color light)
        {
            Flat(ref t, u, v, new Color(0.1f, 0.1f, 0.11f), 0f);
            float du = u - 0.5f, dv = v - 0.5f, r = Mathf.Sqrt(du * du + dv * dv);
            if (r < 0.3f)
            {
                t.Albedo = Color.Lerp(Color.white, light, r / 0.3f);
                t.Glow = light * (1.2f - r * 2f);
                t.Height = 0.8f;
            }
            else if (r < 0.36f)
            {
                t.Albedo = new Color(0.55f, 0.56f, 0.6f);
            }
        }

        private static bool Inside(float u, float v, float margin) => u > margin && u < 1f - margin && v > margin && v < 1f - margin;

        /// <summary>Smooth value noise in 0-1.</summary>
        private static float Noise(float x, float y, int seed)
        {
            int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
            float fx = x - x0, fy = y - y0;
            fx = fx * fx * (3f - 2f * fx);
            fy = fy * fy * (3f - 2f * fy);
            float a = Hash(x0, y0, seed), b = Hash(x0 + 1, y0, seed), c = Hash(x0, y0 + 1, seed), d = Hash(x0 + 1, y0 + 1, seed);
            return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
        }

        private static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393 + y * 668265263 + seed * 1442695041);
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                return (h & 0xFFFFFF) / (float)0xFFFFFF;
            }
        }
    }
}
