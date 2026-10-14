using System.Collections.Generic;
using StellarIndustries.Core;
using StellarIndustries.Production;
using UnityEngine;
using UnityEngine.Rendering;

namespace StellarIndustries.Parts
{
    /// <summary>
    /// Makes the game's minable rocks read as ore deposits: each gets, on this player's game
    /// only, a model of its ore built by the mod and added under the rock (so it goes away when
    /// the rock is mined): metal ore nodules and nuggets breaking out of the stone, or crystal
    /// clusters for stellar shards, ice and glass, plus scree around its foot. A pure deposit
    /// shows more and bigger ore than a normal one, an impure one only a little. Purely visual:
    /// no collider, nothing saved or sent, the rock itself is untouched. Rocks are looked for
    /// every few seconds; one mesh is shared by all deposits of the same ore, purity and shape.
    /// </summary>
    internal static class DepositDecor
    {
        public const string ChildName = "SI_DepositDecor";
        private const int Variants = 4;

        private static readonly Dictionary<(uint, Purity, int), Mesh> Meshes = new Dictionary<(uint, Purity, int), Mesh>();
        private static float _next;

        /// <summary>Rocks decorated since the scene loaded (development log).</summary>
        public static int Count { get; private set; }

        public static void Forget()
        {
            _next = 0f;
            Count = 0;
        }

        public static void Update()
        {
            if (!Settings.DepositDetails.Value || Time.unscaledTime < _next || !IndustrialSkin.Ready)
                return;
            _next = Time.unscaledTime + 3f;
            foreach (Deposits.Rock rock in Deposits.Loaded())
            {
                try
                {
                    Decorate(rock);
                }
                catch (System.Exception e)
                {
                    ErrorLog.Report("deposit look", e);
                }
            }
        }

        /// <summary>Adds the ore model under one rock (once).</summary>
        public static void Decorate(Deposits.Rock rock)
        {
            Transform t = rock.Object != null ? rock.Object.transform : null;
            if (t == null || t.Find(ChildName) != null)
                return;
            // The game draws the rock elsewhere: its size comes from its collider (the laser's target)
            Bounds world = new Bounds(t.position, Vector3.zero);
            bool any = false;
            foreach (Collider c in t.GetComponentsInChildren<Collider>())
            {
                if (!any)
                    world = c.bounds;
                else
                    world.Encapsulate(c.bounds);
                any = true;
            }
            // A box round a turned rock is larger than the rock: the model is made a little smaller
            float radius = any ? Mathf.Clamp((world.extents.x + world.extents.y + world.extents.z) / 3f * 0.85f, 0.25f, 6f) : 0.8f;
            Vector3 center = any ? world.center : t.position;
            Vector3 up = Up();
            int variant = (int)((uint)(rock.Position.GetHashCode() & 0x7fffffff) % Variants);
            Mesh mesh = MeshFor(rock.Item, rock.Purity, variant);
            var go = new GameObject(ChildName) { layer = t.gameObject.layer };
            go.transform.SetParent(t, false);
            // The model is built for a rock 1 wide each side and 1 high, its foot at 0
            go.transform.position = center - up * radius;
            go.transform.rotation = Quaternion.FromToRotation(Vector3.up, up) * Quaternion.AngleAxis(variant * 90f, Vector3.up);
            Vector3 lossy = t.lossyScale;
            float s = Mathf.Max(0.01f, (Mathf.Abs(lossy.x) + Mathf.Abs(lossy.y) + Mathf.Abs(lossy.z)) / 3f);
            go.transform.localScale = new Vector3(radius, radius * 2f, radius) / s;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = IndustrialSkin.Material;
            renderer.shadowCastingMode = ShadowCastingMode.On;
            Count++;
        }

        /// <summary>Which way is up where the player stands (the rocks around share it).</summary>
        private static Vector3 Up()
        {
            var players = SDModKit.Game.GameRefs.PlayersClient;
            if (players != null && players.LocalPlayer != null)
                return players.LocalPlayer.Rotation * Vector3.up;
            return Vector3.up;
        }

        private enum Style { Metal, Crystal }

        private static (Style Style, Tile Ore, Tile Nugget) Look(uint item)
        {
            switch (item)
            {
                case GameItems.Iron: return (Style.Metal, Tile.VeinIron, Tile.IngotIron);
                case GameItems.Copper: return (Style.Metal, Tile.VeinCopper, Tile.Copper);
                case GameItems.Aluminum: return (Style.Metal, Tile.VeinAluminum, Tile.Aluminum);
                case GameItems.StellarShard: return (Style.Crystal, Tile.StellarCrystal, Tile.StellarDust);
                case GameItems.Ice: return (Style.Crystal, Tile.Ice, Tile.Ice);
                case GameItems.Glass: return (Style.Crystal, Tile.Glass, Tile.Glass);
                default: return (Style.Metal, Tile.Stone, Tile.Gold);
            }
        }

        public static Mesh MeshFor(uint item, Purity purity, int variant)
        {
            if (Meshes.TryGetValue((item, purity, variant), out Mesh cached) && cached != null)
                return cached;
            var b = new MeshBuilder();
            var rnd = new System.Random((int)item * 31 + (int)purity * 7 + variant);
            float R() => (float)rnd.NextDouble();
            (Style style, Tile ore, Tile nugget) = Look(item);
            int count = purity == Purity.Impure ? 5 : purity == Purity.Normal ? 9 : 14;
            float size = purity == Purity.Impure ? 0.9f : purity == Purity.Pure ? 1.35f : 1.1f;
            float turn = R() * Mathf.PI * 2f;
            var c = new Vector3(0f, 0.5f, 0f);
            for (int i = 0; i < count; i++)
            {
                // Spread round the rock (golden angle), on its upper flanks
                float a = turn + i * 2.39996f;
                float tilt = Mathf.Lerp(0.3f, 1.25f, R());
                var dir = new Vector3(Mathf.Cos(a) * Mathf.Sin(tilt), Mathf.Cos(tilt), Mathf.Sin(a) * Mathf.Sin(tilt));
                // The rock's surface that way (a rock 1 wide each side and 1 high, centre at half height)
                Vector3 surface = c + new Vector3(dir.x, dir.y * 0.5f, dir.z);
                Vector3 normal = new Vector3(dir.x, dir.y * 2f, dir.z).normalized;
                if (style == Style.Crystal)
                {
                    float r = (0.07f + 0.05f * R()) * size, length = (0.35f + 0.3f * R()) * size;
                    Vector3 root = c + (surface - c) * 0.6f;
                    Vector3 tip = surface + normal * length;
                    b.Panel = IndustrialSkin.Uv(ore);
                    b.Cylinder(root, tip, r, r, 6);
                    b.Cylinder(tip, tip + (tip - root).normalized * r * 2.2f, r, 0.004f, 6, capB: false);
                    // A smaller crystal beside it
                    Vector3 side = Vector3.Cross(normal, Vector3.up).normalized * r * 1.7f;
                    Vector3 tip2 = surface + side + normal * length * 0.5f;
                    b.Cylinder(root + side, tip2, r * 0.6f, r * 0.6f, 6);
                    b.Cylinder(tip2, tip2 + normal * r * 1.3f, r * 0.6f, 0.003f, 6, capB: false);
                }
                else
                {
                    // A rounded nodule of ore-bearing rock half sunk in the surface, with metal glints on top
                    Vector3 at = c + (surface - c) * 0.96f;
                    float r = (0.13f + 0.07f * R()) * size, h = (0.06f + 0.04f * R()) * size;
                    b.Panel = IndustrialSkin.Uv(ore);
                    b.Cylinder(at - normal * h, at + normal * h, r, r * 0.72f, 7);
                    b.Cylinder(at + normal * h, at + normal * h * 1.6f, r * 0.72f, r * 0.35f, 7);
                    b.Panel = IndustrialSkin.Uv(nugget);
                    Vector3 side = Vector3.Cross(normal, dir.y > 0.9f ? Vector3.right : Vector3.up).normalized;
                    for (int g = 0; g < 2; g++)
                    {
                        Vector3 glint = at + normal * h * 1.2f + side * r * (g == 0 ? 0.45f : -0.3f);
                        b.Cylinder(glint, glint + normal * r * 0.35f, r * 0.22f, r * 0.08f, 5);
                    }
                }
            }
            // Scree round the foot: stones and ore chips, half sunk
            int scree = 8 + (int)purity * 4;
            for (int i = 0; i < scree; i++)
            {
                float a = turn + i * (Mathf.PI * 2f / scree) + R() * 0.5f;
                float d = 0.86f + 0.16f * R();
                float s = (0.12f + 0.12f * R()) * (i % 3 == 0 ? size : 1f);
                var at = new Vector3(Mathf.Cos(a) * d, 0.06f + s * 0.3f, Mathf.Sin(a) * d);
                b.Panel = IndustrialSkin.Uv(i % 3 == 0 ? (style == Style.Crystal ? ore : nugget) : Tile.Stone);
                if (style == Style.Crystal && i % 3 == 0)
                    b.Cylinder(at - new Vector3(0f, s * 0.3f, 0f), at + new Vector3(0f, s * 1.6f, 0f), s * 0.4f, 0.004f, 6, capB: false);
                else
                    b.Box(at, new Vector3(s * 1.3f, s * 0.8f, s), Quaternion.Euler(R() * 40f, R() * 360f, R() * 40f));
            }
            Mesh mesh = b.Build($"SI_Deposit_{item}_{purity}_{variant}", GameSkin.Ready ? GameSkin.Palette : (Vector2?)null);
            mesh.hideFlags = HideFlags.DontUnloadUnusedAsset;
            Meshes[(item, purity, variant)] = mesh;
            return mesh;
        }
    }
}
