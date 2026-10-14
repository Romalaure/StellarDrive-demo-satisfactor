using System.Collections.Generic;
using System.Linq;
using StellarIndustries.Content;
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
                    MeshFilter visual = Own(rock) ?? Nearest(visuals, rock.Object.transform.position);
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

        /// <summary>The game's rock visuals (objects with a readable mesh, by the game's naming), by place.</summary>
        private static Dictionary<Vector3Int, List<MeshFilter>> Visuals()
        {
            var found = new Dictionary<Vector3Int, List<MeshFilter>>();
            foreach (MeshFilter filter in Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
            {
                // Hotspot rocks are drawn by "... visual" objects, the planets' fixed rocks by "static object ..." ones
                if (filter.sharedMesh == null || !filter.sharedMesh.isReadable || !(filter.name.EndsWith(" visual") || filter.name.StartsWith("static object")))
                    continue;
                Vector3Int key = Key(filter.transform.position);
                if (!found.TryGetValue(key, out List<MeshFilter> list))
                    found[key] = list = new List<MeshFilter>();
                list.Add(filter);
            }
            return found;
        }

        /// <summary>Development: what the decoration of a rock found.</summary>
        public static string Explain(Deposits.Rock rock)
        {
            Transform t = rock.Object != null ? rock.Object.transform : null;
            if (t == null)
                return "gone";
            Transform decor = t.Find(ChildName);
            MeshFilter visual = Nearest(Visuals(), t.position);
            string mesh = decor != null && decor.GetComponent<MeshFilter>()?.sharedMesh != null ? decor.GetComponent<MeshFilter>().sharedMesh.name + " " + decor.GetComponent<MeshFilter>().sharedMesh.vertexCount : "-";
            float near = float.MaxValue;
            foreach (var list in Visuals().Values)
                foreach (MeshFilter f in list)
                    near = Mathf.Min(near, Vector3.Distance(f.transform.position, t.position));
            var around = new System.Text.StringBuilder();
            foreach (Renderer r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {
                if (r == null || Vector3.Distance(r.bounds.center, t.position) > 2.5f || r.bounds.size.magnitude > 10f || r.name == ChildName)
                    continue;
                MeshFilter f = r.GetComponent<MeshFilter>();
                around.Append($" [{r.GetType().Name} {r.name} parent {(r.transform.parent != null ? r.transform.parent.name : "-")} mesh {(f != null && f.sharedMesh != null ? f.sharedMesh.name + " readable " + f.sharedMesh.isReadable : "-")} at {Vector3.Distance(r.transform.position, t.position):0.00} m]");
            }
            return $"decor {(decor != null ? mesh : "none")}, visual {(visual != null ? visual.name : "none")}, nearest visual {near:0.00} m, renderers around:{around}";
        }

        /// <summary>Adds the veins to one rock (in-game test: finds its visual itself). False if the rock has no readable visual.</summary>
        public static bool Decorate(Deposits.Rock rock)
        {
            if (rock.Object == null || rock.Object.transform.Find(ChildName) != null)
                return false;
            MeshFilter visual = Own(rock) ?? Nearest(Visuals(), rock.Object.transform.position);
            return visual != null && Decorate(rock, visual);
        }

        /// <summary>Some rocks (the planets' static objects) carry their mesh themselves rather than a separate visual.</summary>
        private static MeshFilter Own(Deposits.Rock rock)
        {
            foreach (MeshFilter filter in rock.Object.GetComponentsInChildren<MeshFilter>())
            {
                if (filter.name != ChildName && filter.sharedMesh != null && filter.sharedMesh.isReadable)
                    return filter;
            }
            return null;
        }

        private static bool Decorate(Deposits.Rock rock, MeshFilter visual)
        {
            Transform t = rock.Object.transform;
            // The rock's up, in its mesh's frame: an inexhaustible deposit's crown grows that way
            Vector3 up = visual.transform.InverseTransformDirection(Up()).normalized;
            Mesh mesh = MeshFor(visual.sharedMesh, rock.Item, rock.Purity, rock.Node, up);
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

        /// <summary>Which way is up where the player stands (the rocks around share it).</summary>
        private static Vector3 Up()
        {
            var players = SDModKit.Game.GameRefs.PlayersClient;
            if (players != null && players.LocalPlayer != null)
                return players.LocalPlayer.Rotation * Vector3.up;
            return Vector3.up;
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
                case ResourceCatalog.Coal: return (Tile.Coal, Tile.Coal, false, 8);
                case ResourceCatalog.Sulfur: return (Tile.Sulfur, Tile.Sulfur, true, 9);
                case ResourceCatalog.Limestone: return (Tile.Concrete, Tile.Concrete, false, 10);
                case ResourceCatalog.RawQuartz: return (Tile.Glass, Tile.Glass, true, 11);
                case ResourceCatalog.GoldOre: return (Tile.Gold, Tile.Gold, false, 12);
                default: return (Tile.VeinAluminum, Tile.Gold, false, 7);
            }
        }

        /// <summary>A smooth field over the rock: its level lines are the veins.</summary>
        private static float Field(Vector3 p, int seed) =>
            0.5f + 0.25f * Mathf.Sin(p.x * 1.3f + seed + 1.7f * Mathf.Sin(p.y * 0.9f + p.z * 1.1f + seed))
                 + 0.25f * Mathf.Sin(p.z * 1.7f - seed + 1.3f * Mathf.Sin(p.x * 1.2f - p.y * 0.7f));

        /// <summary>The veins of one rock shape, in the rock mesh's own frame.</summary>
        public static Mesh MeshFor(Mesh rock, uint item, Purity purity, bool node = false, Vector3 up = default)
        {
            if (rock == null || !rock.isReadable)
                return null;
            // Inexhaustible deposits are few: theirs is built for each (its crown depends on how the rock lies)
            if (!node && Meshes.TryGetValue((rock.GetInstanceID(), item, purity), out Mesh cached) && cached != null)
                return cached;
            (Tile vein, Tile nugget, bool crystal, int seed) = Look(item);
            Vector3[] vertices = rock.vertices;
            int[] triangles = rock.triangles;
            float size = Mathf.Max(0.1f, rock.bounds.size.magnitude);
            // Whole facets of the rock turn to ore, in patches: more of them when the deposit is rich
            float frequency = 3f / size;
            float threshold = purity == Purity.Impure ? 0.62f : purity == Purity.Normal ? 0.52f : 0.42f;
            // The mod's ores cover the whole rock (gold most of it), so they never pass for the metal the rock was
            if (ResourceCatalog.Get(item) != null)
                threshold = item == ResourceCatalog.GoldOre ? 0.35f : -1f;
            if (node)
                threshold -= 0.2f;
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
                Vector3 raise = normal * lift;
                // Pulled in a little from the facet's edges, so the rock's own outline still shows
                Vector3 Inset(Vector3 p) => centre + (p - centre) * 0.97f + raise;
                b.Face(Inset(a), Inset(c1), Inset(c2), normal, new Vector2(0.1f, 0.1f), new Vector2(0.9f, 0.1f), new Vector2(0.5f, 0.9f));
                spots.Add((centre + raise, normal));
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
            if (node)
                Crown(b, vertices, up, size, crystal, nugget);
            if (!b.HasGeometry)
                return null;
            Mesh mesh = b.Build($"SI_Veins_{rock.name}_{item}_{purity}{(node ? "_node" : "")}", GameSkin.Ready ? GameSkin.Palette : (Vector2?)null);
            mesh.hideFlags = HideFlags.DontUnloadUnusedAsset;
            if (!node)
                Meshes[(rock.GetInstanceID(), item, purity)] = mesh;
            return mesh;
        }

        /// <summary>
        /// An inexhaustible deposit's crown: big ore points on its top, among glowing stellar
        /// shards, so it is told from an ordinary rock from afar.
        /// </summary>
        private static void Crown(MeshBuilder b, Vector3[] vertices, Vector3 up, float size, bool crystal, Tile ore)
        {
            if (up.sqrMagnitude < 0.5f)
                up = Vector3.up;
            Vector3 centre = Vector3.zero;
            foreach (Vector3 v in vertices)
                centre += v;
            centre /= Mathf.Max(1, vertices.Length);
            // The highest corners of the rock, one per direction
            var tops = new List<Vector3>();
            foreach (Vector3 v in vertices.OrderByDescending(v => Vector3.Dot(v - centre, up)))
            {
                if (tops.All(t => (t - v).sqrMagnitude > size * size * 0.02f))
                    tops.Add(v);
                if (tops.Count == 6)
                    break;
            }
            for (int i = 0; i < tops.Count; i++)
            {
                Vector3 at = tops[i];
                Vector3 axis = ((at - centre).normalized * 0.5f + up).normalized;
                bool glow = i % 2 == 1;
                float r = size * (glow ? 0.025f : 0.04f), length = size * (glow ? 0.18f : 0.24f);
                b.Panel = IndustrialSkin.Uv(glow ? Tile.StellarCrystal : ore);
                Vector3 tip = at + axis * length;
                b.Cylinder(at - axis * r * 2f, tip, r, crystal || glow ? r * 0.9f : r * 0.6f, 6);
                b.Cylinder(tip, tip + axis * r * 2.2f, crystal || glow ? r * 0.9f : r * 0.6f, 0.003f, 6, capB: false);
            }
        }

    }
}
