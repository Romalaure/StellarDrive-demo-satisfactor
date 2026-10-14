using System.Collections.Generic;
using StellarIndustries.Core;
using StellarIndustries.Production;
using UnityEngine;
using UnityEngine.Rendering;

namespace StellarIndustries.Parts
{
    /// <summary>
    /// Makes the game's minable rocks read as ore deposits, on this player's game only: veins of
    /// their ore on the rock's own facets (built from the rock's mesh, a hair above it),
    /// with a few nuggets, or small crystals for stellar shards, ice and glass. A pure deposit
    /// shows ore on half its facets and many nuggets, an impure one on a few facets. The game draws each rock with a
    /// separate "... visual" object at the rock's place; the veins are added under the rock itself
    /// (they go away when it is mined) at the pose of that visual. Purely visual: no collider,
    /// nothing saved or sent, the rock is untouched. One mesh is shared by every rock of the same
    /// shape, ore and purity.
    /// </summary>
    internal static class DepositDecor
    {
        public const string ChildName = "SI_DepositDecor";

        private static readonly Dictionary<(int, uint, Purity), Mesh> Meshes = new Dictionary<(int, uint, Purity), Mesh>();
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
            var pending = new List<Deposits.Rock>();
            foreach (Deposits.Rock rock in Deposits.Loaded())
            {
                if (rock.Object != null && rock.Object.transform.Find(ChildName) == null)
                    pending.Add(rock);
            }
            if (pending.Count == 0)
                return;
            Dictionary<Vector3Int, List<MeshFilter>> visuals = Visuals();
            foreach (Deposits.Rock rock in pending)
            {
                try
                {
                    MeshFilter visual = Nearest(visuals, rock.Object.transform.position);
                    if (visual != null)
                        Decorate(rock, visual);
                }
                catch (System.Exception e)
                {
                    ErrorLog.Report("deposit look", e);
                }
            }
        }

        private static Vector3Int Key(Vector3 p) => new Vector3Int(Mathf.RoundToInt(p.x), Mathf.RoundToInt(p.y), Mathf.RoundToInt(p.z));

        /// <summary>The visual nearest the rock (within half a metre), looked up in the metre cells around it.</summary>
        private static MeshFilter Nearest(Dictionary<Vector3Int, List<MeshFilter>> visuals, Vector3 at)
        {
            MeshFilter best = null;
            float bestDistance = 0.25f;
            Vector3Int c = Key(at);
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    for (int z = -1; z <= 1; z++)
                    {
                        if (!visuals.TryGetValue(c + new Vector3Int(x, y, z), out List<MeshFilter> list))
                            continue;
                        foreach (MeshFilter f in list)
                        {
                            float d = (f.transform.position - at).sqrMagnitude;
                            if (f != null && d < bestDistance)
                            {
                                bestDistance = d;
                                best = f;
                            }
                        }
                    }
                }
            }
            return best;
        }

        /// <summary>The game's rock visuals ("... visual" objects with a readable mesh), by place.</summary>
        private static Dictionary<Vector3Int, List<MeshFilter>> Visuals()
        {
            var found = new Dictionary<Vector3Int, List<MeshFilter>>();
            foreach (MeshFilter filter in Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
            {
                if (filter.sharedMesh == null || !filter.sharedMesh.isReadable || !filter.name.EndsWith(" visual"))
                    continue;
                Vector3Int key = Key(filter.transform.position);
                if (!found.TryGetValue(key, out List<MeshFilter> list))
                    found[key] = list = new List<MeshFilter>();
                list.Add(filter);
            }
            return found;
        }

        /// <summary>Adds the veins to one rock (in-game test: finds its visual itself). False if the rock has no readable visual.</summary>
        public static bool Decorate(Deposits.Rock rock)
        {
            if (rock.Object == null || rock.Object.transform.Find(ChildName) != null)
                return false;
            MeshFilter visual = Nearest(Visuals(), rock.Object.transform.position);
            return visual != null && Decorate(rock, visual);
        }

        private static bool Decorate(Deposits.Rock rock, MeshFilter visual)
        {
            Transform t = rock.Object.transform;
            Mesh mesh = MeshFor(visual.sharedMesh, rock.Item, rock.Purity);
            if (mesh == null)
                return false;
            var go = new GameObject(ChildName) { layer = visual.gameObject.layer };
            go.transform.SetParent(t, false);
            go.transform.position = visual.transform.position;
            go.transform.rotation = visual.transform.rotation;
            Vector3 v = visual.transform.lossyScale, r = t.lossyScale;
            go.transform.localScale = new Vector3(v.x / Mathf.Max(1e-4f, r.x), v.y / Mathf.Max(1e-4f, r.y), v.z / Mathf.Max(1e-4f, r.z));
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = IndustrialSkin.Material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            Count++;
            return true;
        }

        private static (Tile Vein, Tile Nugget, bool Crystal, int Seed) Look(uint item)
        {
            switch (item)
            {
                case GameItems.Iron: return (Tile.CrushedIron, Tile.IngotIron, false, 1);
                case GameItems.Copper: return (Tile.CrushedCopper, Tile.Copper, false, 2);
                case GameItems.Aluminum: return (Tile.Aluminum, Tile.IngotAluminum, false, 3);
                case GameItems.StellarShard: return (Tile.StellarCrystal, Tile.StellarCrystal, true, 4);
                case GameItems.Ice: return (Tile.Ice, Tile.Ice, true, 5);
                case GameItems.Glass: return (Tile.Glass, Tile.Glass, true, 6);
                default: return (Tile.VeinAluminum, Tile.Gold, false, 7);
            }
        }

        /// <summary>A smooth field over the rock: its level lines are the veins.</summary>
        private static float Field(Vector3 p, int seed) =>
            0.5f + 0.25f * Mathf.Sin(p.x * 1.3f + seed + 1.7f * Mathf.Sin(p.y * 0.9f + p.z * 1.1f + seed))
                 + 0.25f * Mathf.Sin(p.z * 1.7f - seed + 1.3f * Mathf.Sin(p.x * 1.2f - p.y * 0.7f));

        /// <summary>The veins of one rock shape, in the rock mesh's own frame.</summary>
        public static Mesh MeshFor(Mesh rock, uint item, Purity purity)
        {
            if (rock == null || !rock.isReadable)
                return null;
            if (Meshes.TryGetValue((rock.GetInstanceID(), item, purity), out Mesh cached) && cached != null)
                return cached;
            (Tile vein, Tile nugget, bool crystal, int seed) = Look(item);
            Vector3[] vertices = rock.vertices;
            int[] triangles = rock.triangles;
            float size = Mathf.Max(0.1f, rock.bounds.size.magnitude);
            // Whole facets of the rock turn to ore, in patches: more of them when the deposit is rich
            float frequency = 3f / size;
            float threshold = purity == Purity.Impure ? 0.62f : purity == Purity.Normal ? 0.52f : 0.42f;
            float lift = size * 0.004f;
            var b = new MeshBuilder { Panel = IndustrialSkin.Uv(vein) };
            var spots = new List<(Vector3 At, Vector3 Normal)>();
            for (int t = 0; t + 2 < triangles.Length; t += 3)
            {
                Vector3 a = vertices[triangles[t]], c1 = vertices[triangles[t + 1]], c2 = vertices[triangles[t + 2]];
                Vector3 normal = Vector3.Cross(c1 - a, c2 - a);
                if (normal.sqrMagnitude < 1e-10f)
                    continue;
                Vector3 centre = (a + c1 + c2) / 3f;
                // Outwards from the rock's centre
                if (Vector3.Dot(normal, centre - rock.bounds.center) < 0f)
                    normal = -normal;
                normal.Normalize();
                if (Field(centre * frequency, seed) < threshold)
                    continue;
                Vector3 up = normal * lift;
                // Pulled in a little from the facet's edges, so the rock's own outline still shows
                Vector3 Inset(Vector3 p) => centre + (p - centre) * 0.97f + up;
                b.Face(Inset(a), Inset(c1), Inset(c2), normal, new Vector2(0.1f, 0.1f), new Vector2(0.9f, 0.1f), new Vector2(0.5f, 0.9f));
                spots.Add((centre + up, normal));
            }
            // Nuggets (metals) or small crystals (shards, ice, glass) on the veins
            int count = purity == Purity.Impure ? 2 : purity == Purity.Normal ? 5 : 10;
            var rnd = new System.Random(seed * 101 + (int)purity);
            b.Panel = IndustrialSkin.Uv(nugget);
            for (int k = 0; k < count && spots.Count > 0; k++)
            {
                (Vector3 at, Vector3 n) = spots[rnd.Next(spots.Count)];
                float s = size * (0.012f + 0.01f * (float)rnd.NextDouble()) * (purity == Purity.Pure ? 1.4f : 1f);
                if (crystal)
                {
                    Vector3 lean = Vector3.Cross(n, Vector3.up).sqrMagnitude > 0.01f ? Vector3.Cross(n, Vector3.up).normalized : Vector3.right;
                    Vector3 axis = (n + lean * ((float)rnd.NextDouble() - 0.5f) * 0.6f).normalized;
                    Vector3 tip = at + axis * s * 5f;
                    b.Cylinder(at - axis * s, tip, s, s * 0.9f, 6);
                    b.Cylinder(tip, tip + axis * s * 1.6f, s * 0.9f, s * 0.05f, 6, capB: false);
                }
                else
                {
                    b.Cylinder(at - n * s * 0.6f, at + n * s * 0.9f, s * 1.3f, s * 0.7f, 5);
                }
            }
            if (!b.HasGeometry)
                return null;
            Mesh mesh = b.Build($"SI_Veins_{rock.name}_{item}_{purity}", GameSkin.Ready ? GameSkin.Palette : (Vector2?)null);
            mesh.hideFlags = HideFlags.DontUnloadUnusedAsset;
            Meshes[(rock.GetInstanceID(), item, purity)] = mesh;
            return mesh;
        }

    }
}
