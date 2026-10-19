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
    /// with a few rough nuggets, or clusters of crystals for stellar shards, ice, glass, sulfur and quartz. A pure deposit
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

        /// <summary>Decorations shown now (development log).</summary>
        public static int Live => All.Count;

        public static void Forget()
        {
            _next = 0f;
            _nextCheck = 0f;
            Count = 0;
            All.Clear();
            NotFound.Clear();
        }

        /// <summary>One decoration made, and what it was made for.</summary>
        private sealed class Made
        {
            public GameObject Go;
            public Planets.Objects.MinablePlanetObject Rock;
            public MeshFilter Visual;
            public Renderer Look;
            public Mesh Source;
            /// <summary>The decoration's own mesh (an inexhaustible deposit's), destroyed with it.</summary>
            public Mesh Own;
            public uint Item;
            public Vector3 At;
        }

        private static readonly List<Made> All = new List<Made>();
        /// <summary>Rocks whose drawn model was not found, and when to look again.</summary>
        private static readonly Dictionary<int, float> NotFound = new Dictionary<int, float>();
        private static float _nextCheck;

        public static void Update()
        {
            if (!Settings.DepositDetails.Value || !IndustrialSkin.Ready)
                return;
            float now = Time.unscaledTime;
            if (now >= _nextCheck)
            {
                _nextCheck = now + 0.5f;
                Check();
            }
            if (now < _next)
                return;
            _next = now + 3f;
            var pending = new List<Deposits.Rock>();
            foreach (Deposits.Rock rock in Deposits.Current)
            {
                if (rock.Object == null || rock.Object.transform.Find(ChildName) != null)
                    continue;
                // A rock without a model found is looked at again only now and then (a whole scene search)
                if (NotFound.TryGetValue(rock.Object.GetInstanceID(), out float retry) && now < retry)
                    continue;
                pending.Add(rock);
            }
            if (pending.Count == 0)
                return;
            Dictionary<Vector3Int, List<MeshFilter>> visuals = null;
            foreach (Deposits.Rock rock in pending)
            {
                try
                {
                    MeshFilter visual = Own(rock);
                    if (visual == null)
                    {
                        visuals = visuals ?? Visuals();
                        visual = Nearest(visuals, rock.Object.transform.position);
                    }
                    if (visual != null && Decorate(rock, visual))
                        NotFound.Remove(rock.Object.GetInstanceID());
                    else
                        NotFound[rock.Object.GetInstanceID()] = now + 10f;
                }
                catch (System.Exception e)
                {
                    ErrorLog.Report("deposit look", e);
                }
            }
            if (NotFound.Count > 2000)
                NotFound.Clear();
        }

        /// <summary>
        /// Takes a decoration away as soon as it no longer fits its rock: the rock mined, its model
        /// hidden or swapped, or the rock object reused by the game for another rock elsewhere.
        /// Otherwise the ore would float in the air where a rock used to be. The rock gets a new
        /// one at the next pass if it is still there.
        /// </summary>
        private static void Check()
        {
            for (int i = All.Count - 1; i >= 0; i--)
            {
                Made m = All[i];
                bool keep;
                try
                {
                    keep = m.Go != null && m.Rock != null && m.Visual != null && m.Visual.gameObject.activeInHierarchy && (m.Look == null || m.Look.enabled) &&
                           m.Visual.sharedMesh == m.Source && m.Rock.dropRate.item != null && m.Rock.dropRate.item.id == m.Item && m.Rock.CurrentHealth > 0f &&
                           (m.Rock.transform.position - m.At).sqrMagnitude < 0.01f && (m.Visual.transform.position - m.Go.transform.position).sqrMagnitude < 0.01f;
                }
                catch (System.Exception)
                {
                    keep = false;
                }
                if (keep)
                    continue;
                if (m.Go != null)
                {
                    // Out of the rock at once, so that the next pass can make the right one
                    m.Go.transform.SetParent(null, false);
                    Object.Destroy(m.Go);
                }
                if (m.Own != null)
                    Object.Destroy(m.Own);
                All.RemoveAt(i);
            }
        }

        private static Vector3Int Key(Vector3 p) => new Vector3Int(Mathf.RoundToInt(p.x), Mathf.RoundToInt(p.y), Mathf.RoundToInt(p.z));

        /// <summary>The visual nearest the rock (within half a metre), looked up in the metre cells around it.</summary>
        internal static MeshFilter Nearest(Dictionary<Vector3Int, List<MeshFilter>> visuals, Vector3 at)
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
        internal static Dictionary<Vector3Int, List<MeshFilter>> Visuals()
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

        /// <summary>Development: the game's visual of a rock, or null.</summary>
        public static MeshFilter VisualOf(Deposits.Rock rock) =>
            rock.Object == null ? null : Own(rock) ?? Nearest(Visuals(), rock.Object.transform.position);

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
            All.Add(new Made
            {
                Go = go, Rock = rock.Object, Visual = visual, Look = visual.GetComponent<Renderer>(), Source = visual.sharedMesh,
                Own = rock.Node ? mesh : null, Item = rock.Item, At = t.position
            });
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

        /// <summary>How much of the rock its ore covers (the field's threshold: lower is more).</summary>
        private static float Threshold(uint item, Purity purity)
        {
            switch (item)
            {
                // Coal and limestone are beds: most of the rock is the ore itself
                case ResourceCatalog.Coal: return purity == Purity.Impure ? 0.4f : purity == Purity.Normal ? 0.3f : 0.2f;
                case ResourceCatalog.Limestone: return purity == Purity.Impure ? 0.38f : purity == Purity.Normal ? 0.28f : 0.18f;
                // The others are veins in the host rock, told apart by their crystals or nuggets
                case ResourceCatalog.Sulfur:
                case ResourceCatalog.RawQuartz:
                case ResourceCatalog.GoldOre: return purity == Purity.Impure ? 0.58f : purity == Purity.Normal ? 0.5f : 0.42f;
                default: return purity == Purity.Impure ? 0.62f : purity == Purity.Normal ? 0.52f : 0.42f;
            }
        }

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
            float threshold = Threshold(item, purity) - (node ? 0.08f : 0f);
            float lift = size * 0.005f;
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
                // Each corner is pushed out from the rock's centre, the same for every facet that
                // shares it: neighbouring ore facets stay joined (no crack showing the rock's edge line)
                Vector3 Raise(Vector3 p) => p + (p - rock.bounds.center).normalized * lift;
                // Each facet shows its own patch of the ore's texture, from the tile's inside only:
                // the tile's outline never draws lines across the rock
                Vector3 tx = Vector3.Cross(normal, Mathf.Abs(normal.y) < 0.9f ? Vector3.up : Vector3.right).normalized, ty = Vector3.Cross(normal, tx);
                var origin = new Vector2(0.3f + 0.4f * Hash01(centre, 1), 0.3f + 0.4f * Hash01(centre, 2));
                Vector2 Uv(Vector3 p)
                {
                    Vector3 d = p - centre;
                    Vector2 uv = origin + new Vector2(Vector3.Dot(d, tx), Vector3.Dot(d, ty)) * (0.5f / size);
                    return new Vector2(Mathf.Clamp(uv.x, 0.18f, 0.82f), Mathf.Clamp(uv.y, 0.18f, 0.82f));
                }
                b.Face(Raise(a), Raise(c1), Raise(c2), normal, Uv(a), Uv(c1), Uv(c2));
                spots.Add(((Raise(a) + Raise(c1) + Raise(c2)) / 3f, normal));
            }
            // Nuggets (metals, coal, limestone, gold) or clusters of crystals (shards, ice, glass, sulfur, quartz) on the veins
            int count = purity == Purity.Impure ? 3 : purity == Purity.Normal ? 6 : 10;
            var rnd = new System.Random(seed * 101 + (int)purity);
            b.Panel = IndustrialSkin.Uv(nugget);
            for (int k = 0; k < count && spots.Count > 0; k++)
            {
                (Vector3 at, Vector3 n) = spots[rnd.Next(spots.Count)];
                float s = size * (0.014f + 0.012f * (float)rnd.NextDouble()) * (purity == Purity.Pure ? 1.3f : 1f);
                if (crystal)
                    Cluster(b, at, n, s, 2 + rnd.Next(3), rnd);
                else
                    Lump(b, at, n, s * 1.4f, rnd);
            }
            if (node)
                Crown(b, vertices, up, size, crystal, nugget, rnd);
            if (!b.HasGeometry)
                return null;
            Mesh mesh = b.Build($"SI_Veins_{rock.name}_{item}_{purity}{(node ? "_node" : "")}", GameSkin.Ready ? GameSkin.Palette : (Vector2?)null);
            mesh.hideFlags = HideFlags.DontUnloadUnusedAsset;
            if (!node)
                Meshes[(rock.GetInstanceID(), item, purity)] = mesh;
            return mesh;
        }

        /// <summary>A number in 0-1 from a place (the same on every game).</summary>
        private static float Hash01(Vector3 p, int salt)
        {
            unchecked
            {
                uint h = (uint)Mathf.RoundToInt(p.x * 97f) * 73856093u ^ (uint)Mathf.RoundToInt(p.y * 97f) * 19349663u ^ (uint)Mathf.RoundToInt(p.z * 97f) * 83492791u ^ (uint)salt * 2654435761u;
                h ^= h >> 13;
                h *= 1274126177u;
                h ^= h >> 16;
                return (h & 0xffffu) / 65535f;
            }
        }

        /// <summary>
        /// A rough nugget half sunk in the rock: an octahedron with its corners pulled at random,
        /// flat faces like the game's own rocks.
        /// </summary>
        private static void Lump(MeshBuilder b, Vector3 at, Vector3 n, float s, System.Random rnd)
        {
            Vector3 t1 = Vector3.Cross(n, Mathf.Abs(n.y) < 0.9f ? Vector3.up : Vector3.right).normalized, t2 = Vector3.Cross(n, t1);
            float spin = (float)rnd.NextDouble() * Mathf.PI;
            Vector3 u = t1 * Mathf.Cos(spin) + t2 * Mathf.Sin(spin), w = Vector3.Cross(n, u);
            float R() => 0.75f + 0.5f * (float)rnd.NextDouble();
            Vector3 centre = at + n * s * 0.15f;
            Vector3 top = centre + n * s * 0.8f * R(), bottom = centre - n * s * 0.6f;
            Vector3[] ring = { centre + u * s * R(), centre + w * s * R(), centre - u * s * R(), centre - w * s * R() };
            for (int i = 0; i < 4; i++)
            {
                Vector3 p = ring[i], q = ring[(i + 1) % 4];
                FaceOut(b, top, p, q, centre);
                FaceOut(b, bottom, q, p, centre);
            }
        }

        private static void FaceOut(MeshBuilder b, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 inside)
        {
            Vector3 normal = Vector3.Cross(p1 - p0, p2 - p0);
            if (Vector3.Dot(normal, (p0 + p1 + p2) / 3f - inside) < 0f)
                normal = -normal;
            b.Face(p0, p1, p2, normal, new Vector2(0.3f, 0.3f), new Vector2(0.7f, 0.3f), new Vector2(0.5f, 0.7f));
        }

        /// <summary>A few six-sided crystals growing from one spot, leaning apart, of different lengths.</summary>
        private static void Cluster(MeshBuilder b, Vector3 at, Vector3 n, float s, int count, System.Random rnd)
        {
            Vector3 t1 = Vector3.Cross(n, Mathf.Abs(n.y) < 0.9f ? Vector3.up : Vector3.right).normalized, t2 = Vector3.Cross(n, t1);
            for (int i = 0; i < count; i++)
            {
                float angle = (i + (float)rnd.NextDouble() * 0.5f) * Mathf.PI * 2f / count;
                Vector3 lean = t1 * Mathf.Cos(angle) + t2 * Mathf.Sin(angle);
                // The first is the biggest and stands straightest
                float r = s * (i == 0 ? 1f : 0.55f + 0.35f * (float)rnd.NextDouble());
                Vector3 axis = (n + lean * (i == 0 ? 0.15f : 0.35f + 0.35f * (float)rnd.NextDouble())).normalized;
                Vector3 foot = at + lean * s * (i == 0 ? 0f : 0.8f) - axis * r;
                Vector3 shoulder = foot + axis * r * (3.5f + 2f * (float)rnd.NextDouble());
                b.Cylinder(foot, shoulder, r, r, 6, capA: false, capB: false);
                b.Cylinder(shoulder, shoulder + axis * r * 1.4f, r, r * 0.08f, 6, capA: false, capB: false);
            }
        }

        /// <summary>
        /// An inexhaustible deposit's crown: a big outcrop of its ore on the rock's top (crystals
        /// or nuggets), with stellar shards glowing in its cracks, so it is told from an ordinary
        /// rock from afar.
        /// </summary>
        private static void Crown(MeshBuilder b, Vector3[] vertices, Vector3 up, float size, bool crystal, Tile ore, System.Random rnd)
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
                if (tops.Count == 4)
                    break;
            }
            for (int i = 0; i < tops.Count; i++)
            {
                Vector3 at = tops[i];
                Vector3 axis = ((at - centre).normalized * 0.4f + up).normalized;
                if (i == 0 || i == 2)
                {
                    b.Panel = IndustrialSkin.Uv(ore);
                    if (crystal)
                        Cluster(b, at, axis, size * (i == 0 ? 0.035f : 0.025f), i == 0 ? 5 : 3, rnd);
                    else
                    {
                        Lump(b, at, axis, size * (i == 0 ? 0.07f : 0.05f), rnd);
                        Lump(b, at + Vector3.Cross(axis, up.normalized).normalized * size * 0.04f, axis, size * 0.035f, rnd);
                    }
                }
                else
                {
                    b.Panel = IndustrialSkin.Uv(Tile.StellarCrystal);
                    Cluster(b, at, axis, size * 0.018f, 3, rnd);
                }
            }
        }
    }
}
