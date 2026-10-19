using System.Collections.Generic;
using Core.Services;
using Planets.Objects;
using Planets.Objects.Common.Model;
using Ships.Interface.Model.Parts;
using Ships.Services;
using StellarIndustries.Content;
using StellarIndustries.Core;
using UnityEngine;

namespace StellarIndustries.Production
{
    /// <summary>
    /// The minable rocks the extractors draw from: the game's own MinablePlanetObject components
    /// (the rocks and crystals players mine with the laser), read where the host's game has them
    /// loaded, that is near a player. The list is refreshed every few seconds, not every tick.
    /// A rock gives its own drop item at the speed of its purity (<see cref="DepositPurity"/>);
    /// it is not worn down by an extractor, but if a player mines it away the extractor stops
    /// until another rock is within reach.
    /// </summary>
    internal static class Deposits
    {
        public struct Deposit
        {
            public uint Item;
            public string Name;
            public Vector3 Position;
            public float Distance;
            public Purity Purity;
            public bool Node;
        }

        /// <summary>One rock found in the world (also what the scanner and the decorations read).</summary>
        public struct Rock
        {
            public MinablePlanetObject Object;
            public uint Item;
            public string Name;
            public Vector3 Position;
            public Purity Purity;
            /// <summary>Inexhaustible deposit (see <see cref="OreConversion.IsNode"/>).</summary>
            public bool Node;
            /// <summary>The rock's solid shape (its colliders, world box), null if it has none.</summary>
            public Bounds? Shape;
        }

        private const float ScanInterval = 3f;

        private static readonly List<Rock> Rocks = new List<Rock>();
        private static readonly Dictionary<(uint, ushort), (Deposit? Found, Deposit? Nearest)> Cache = new Dictionary<(uint, ushort), (Deposit?, Deposit?)>();
        private static float _nextScan;
        private static ShipsClientTracker _client;

        public static int Count => Rocks.Count;

        public static void Forget()
        {
            Rocks.Clear();
            Cache.Clear();
            Infos.Clear();
            _nextScan = 0f;
            _client = null;
        }

        /// <summary>Items an extractor never takes: trees and tools lying around are not deposits.</summary>
        public static bool Extractable(uint item)
        {
            if (item == GameItems.Wood || item == 0)
                return false;
            Items.Model.ItemSettings settings = IndustrialItems.Find(item);
            return settings == null || (!settings.isFluid && settings.toolSettings == null);
        }

        /// <summary>A rock's purity, the same on every player's game: from its item and its place on its planet.</summary>
        /// <summary>Whether a rock is an inexhaustible deposit (the same on every player's game).</summary>
        public static bool IsNode(MinablePlanetObject rock)
        {
            if (rock == null || rock.dropRate.item == null || !Extractable(rock.dropRate.item.id))
                return false;
            (int a, int b, int c) = KeyOf(rock);
            return OreConversion.IsNode(a, b, c);
        }

        public static string NodeName(bool node) => node ? Lang.T("inépuisable", "inexhaustible") : "";

        public static Purity PurityOf(MinablePlanetObject rock)
        {
            uint item = rock.dropRate.item != null ? rock.dropRate.item.id : 0u;
            (int a, int b, int c) = KeyOf(rock);
            return DepositPurity.Of(item, a, b, c);
        }

        /// <summary>
        /// A rock's key, the same for every player and every session: the game's own id of the
        /// planet object when it has one (planet first), otherwise its place (half meters) in its
        /// parent's frame.
        /// </summary>
        private static (int Planet, int B, int C) KeyOf(MinablePlanetObject rock)
        {
            var reference = rock.GetComponent<Planets.Objects.Common.Model.PlanetObjectReference>();
            switch (reference != null ? reference.Id : null)
            {
                case Planets.Objects.Common.Model.DenseObjectId dense:
                    return (Fold(dense.PlanetId), Fold(dense.PlacedObjectId.ChunkHash) ^ dense.DenseConfigId * 7919, (int)dense.PlacedObjectId.IdWithinChunk);
                case Planets.Objects.Common.Model.StaticObjectId fixedId:
                    return (Fold(fixedId.PlanetId), 1, fixedId.Index);
                case Planets.Objects.Common.Model.HotspotObjectId hotspot:
                    return (Fold(hotspot.PlanetId), Fold(hotspot.PlacedObjectId.ChunkHash) ^ 0x5f3759df, (int)hotspot.PlacedObjectId.IdWithinChunk);
            }
            Transform t = rock.transform;
            Vector3 p = t.parent != null ? t.localPosition : t.position;
            return (Mathf.RoundToInt(p.x * 2f), Mathf.RoundToInt(p.y * 2f), Mathf.RoundToInt(p.z * 2f));
        }

        private static readonly HarmonyLib.AccessTools.FieldRef<MinablePlanetObject, string> Label = HarmonyLib.AccessTools.FieldRefAccess<MinablePlanetObject, string>("label");

        /// <summary>
        /// Turns a metal rock into the mod's ore it holds (see <see cref="OreConversion"/>): its
        /// drop and its name change, on this player's game.
        /// </summary>
        public static void Convert(MinablePlanetObject rock)
        {
            // Checked every time: the game reuses rock objects, and a reused one gets its metal back
            if (rock == null || rock.dropRate.item == null)
                return;
            uint item = rock.dropRate.item.id;
            if (!OreConversion.Converts(item))
                return;
            (int a, int b, int c) = KeyOf(rock);
            uint ore = OreConversion.Of(item, a, b, c);
            Items.Model.ItemSettings settings = ore != item ? IndustrialItems.Find(ore) : null;
            if (settings == null)
                return;
            ItemDropRate drop = rock.dropRate;
            rock.dropRate = new ItemDropRate { item = settings, amountMin = drop.amountMin, amountMax = drop.amountMax };
            Label(rock) = IndustrialItems.NameOf(ore);
        }

        /// <summary>
        /// What the game shows when a rock is aimed at with the laser: its ore, its purity and,
        /// for an inexhaustible deposit, that only extractors draw from it (the laser does nothing
        /// there). Worked out at each look, so no later change of the rock can wipe it.
        /// </summary>
        public static string LabelOf(MinablePlanetObject rock, string label)
        {
            Convert(rock);
            if (rock.dropRate.item == null || !Extractable(rock.dropRate.item.id))
                return label;
            string name = Label(rock);
            if (string.IsNullOrEmpty(name))
                name = IndustrialItems.NameOf(rock.dropRate.item.id);
            name += " (" + PurityName(PurityOf(rock)) + ")";
            if (IsNode(rock))
                name += Lang.T(" : gisement inépuisable, extracteurs seulement", ": inexhaustible deposit, extractors only");
            return name;
        }

        /// <summary>Development: a rock's key and the id type it came from.</summary>
        public static string DescribeKey(MinablePlanetObject rock)
        {
            var reference = rock.GetComponent<Planets.Objects.Common.Model.PlanetObjectReference>();
            (int a, int b, int c) = KeyOf(rock);
            return $"{(reference != null ? reference.Id?.GetType().Name ?? "null id" : "no reference")} {a}/{b}/{c}";
        }

        private static int Fold(ulong v) => unchecked((int)(v ^ (v >> 32)));

        public static string PurityName(Purity purity) =>
            purity == Purity.Impure ? Lang.T("impur", "impure") : purity == Purity.Pure ? Lang.T("pur", "pure") : Lang.T("normal", "normal");

        /// <summary>Rich-text color of a purity in the scanner and the machine panel.</summary>
        public static string PurityColor(Purity purity) => purity == Purity.Impure ? "#e98" : purity == Purity.Pure ? "#7f9" : "#ddd";

        /// <summary>Every extractable rock loaded on this player's game (a fresh search: call it seldom, or use <see cref="Current"/>).</summary>
        public static List<Rock> Loaded()
        {
            var list = new List<Rock>();
            var seen = new HashSet<(uint, Vector3Int)>();
            Dictionary<Vector3Int, List<MeshFilter>> visuals = null;
            foreach (MinablePlanetObject rock in Object.FindObjectsByType<MinablePlanetObject>(FindObjectsSortMode.None))
            {
                if (rock == null)
                    continue;
                Convert(rock);
                if (rock.dropRate.item == null)
                    continue;
                uint item = rock.dropRate.item.id;
                if (!Extractable(item))
                    continue;
                // A rock can carry the component twice (same place): it is one deposit
                Vector3 at = rock.transform.position;
                if (!seen.Add((item, Vector3Int.RoundToInt(at * 10f))))
                    continue;
                Info info = InfoOf(rock, item, at, ref visuals);
                list.Add(new Rock
                {
                    Object = rock, Item = item, Position = at, Purity = info.Purity, Node = info.Node,
                    Name = IndustrialItems.NameOf(item), Shape = info.Shape
                });
            }
            return list;
        }

        /// <summary>The rocks loaded on this game, from the last search (at most a few seconds old).</summary>
        public static List<Rock> Current
        {
            get
            {
                Scan();
                return Rocks;
            }
        }

        private struct Info
        {
            public Vector3 At;
            public uint Item;
            public Purity Purity;
            public bool Node;
            public Bounds? Shape;
        }

        /// <summary>What is known of each rock object (the game reuses rock objects elsewhere: the place and the item are checked).</summary>
        private static readonly Dictionary<int, Info> Infos = new Dictionary<int, Info>();

        private static Info InfoOf(MinablePlanetObject rock, uint item, Vector3 at, ref Dictionary<Vector3Int, List<MeshFilter>> visuals)
        {
            int id = rock.GetInstanceID();
            if (Infos.TryGetValue(id, out Info known) && known.Item == item && (known.At - at).sqrMagnitude < 0.01f)
                return known;
            var info = new Info { At = at, Item = item, Purity = PurityOf(rock), Node = IsNode(rock), Shape = ShapeOf(rock, at, ref visuals) };
            Infos[id] = info;
            return info;
        }

        /// <summary>
        /// The box around a rock: its solid colliders and its drawn model (some big rocks and
        /// crystals have a small collider and a much bigger look). A big rock's centre can be
        /// meters away from a machine set against it. Triggers and huge volumes are left out.
        /// </summary>
        private static Bounds? ShapeOf(MinablePlanetObject rock, Vector3 at, ref Dictionary<Vector3Int, List<MeshFilter>> visuals)
        {
            Bounds? shape = null;
            void Add(Bounds b)
            {
                if (b.size.magnitude > 150f || (b.center - at).magnitude > 75f)
                    return;
                if (shape == null)
                    shape = b;
                else
                {
                    Bounds all = shape.Value;
                    all.Encapsulate(b);
                    shape = all;
                }
            }
            foreach (Collider c in rock.GetComponentsInChildren<Collider>())
            {
                if (c != null && c.enabled && !c.isTrigger)
                    Add(c.bounds);
            }
            bool drawn = false;
            foreach (Renderer r in rock.GetComponentsInChildren<Renderer>())
            {
                if (r != null && r.name != Parts.DepositDecor.ChildName && !(r is ParticleSystemRenderer))
                {
                    Add(r.bounds);
                    drawn = true;
                }
            }
            if (!drawn)
            {
                // Hotspot rocks are drawn by a separate object at the same place
                visuals = visuals ?? Parts.DepositDecor.Visuals();
                Renderer seen = Parts.DepositDecor.Nearest(visuals, at)?.GetComponent<Renderer>();
                if (seen != null)
                    Add(seen.bounds);
            }
            return shape;
        }

        /// <summary>Distance from a point to a rock's surface (its collider box), or to its centre if it has no collider.</summary>
        public static float DistanceTo(Rock rock, Vector3 at) =>
            rock.Shape != null ? Mathf.Sqrt(rock.Shape.Value.SqrDistance(at)) : Vector3.Distance(at, rock.Position);

        private static void Scan()
        {
            float now = Time.unscaledTime;
            if (now < _nextScan)
                return;
            _nextScan = now + ScanInterval;
            Rocks.Clear();
            Cache.Clear();
            if (Infos.Count > 4000)
                Infos.Clear();
            Rocks.AddRange(Loaded());
        }

        /// <summary>Scans again at the next request (the in-game test after placing a rock).</summary>
        public static void Rescan() => _nextScan = 0f;

        /// <summary>Host: the rock nearest to this extractor within its reach, or null.</summary>
        public static Deposit? Find(uint shipId, StatefulPart part, MachineDef def) => Find(shipId, part, def, out _);

        /// <summary>
        /// Host: the rock nearest to this extractor within its reach, or null; <paramref name="nearest"/>
        /// is the nearest rock loaded here at any distance. A rock seen only by a client near the
        /// extractor counts too (<see cref="DepositReports"/>).
        /// </summary>
        public static Deposit? Find(uint shipId, StatefulPart part, MachineDef def, out Deposit? nearest)
        {
            Scan();
            if (!Cache.TryGetValue((shipId, part.Id), out var cached))
            {
                Deposit? near = null;
                if (_client == null)
                    _client = ServiceLocator.GetService<ShipsClientTracker>();
                if (_client != null && _client.TryGetShipVisualTransform(shipId, out Transform ship) && ship != null)
                    near = NearestTo(ship.TransformPoint(part.LocalPosition));
                cached = (near != null && near.Value.Distance <= def.ExtractRadius ? near : null, near);
                Cache[(shipId, part.Id)] = cached;
            }
            nearest = cached.Nearest;
            return cached.Found ?? DepositReports.Reported(shipId, part.Id, def);
        }

        /// <summary>The rock loaded on this game nearest to a point (distance to its surface), or null.</summary>
        public static Deposit? NearestTo(Vector3 at)
        {
            Scan();
            Deposit? nearest = null;
            float best = float.MaxValue;
            foreach (Rock rock in Rocks)
            {
                // A rock mined away since the last scan
                if (rock.Object == null)
                    continue;
                float d = DistanceTo(rock, at);
                if (d >= best)
                    continue;
                best = d;
                nearest = new Deposit { Item = rock.Item, Name = rock.Name, Position = rock.Position, Distance = d, Purity = rock.Purity, Node = rock.Node };
            }
            return nearest;
        }

        /// <summary>The machine's world position as the host sees it (null if its ship is not shown).</summary>
        public static Vector3? WorldPosition(uint shipId, StatefulPart part)
        {
            if (_client == null)
                _client = ServiceLocator.GetService<ShipsClientTracker>();
            if (_client != null && _client.TryGetShipVisualTransform(shipId, out Transform ship) && ship != null)
                return ship.TransformPoint(part.LocalPosition);
            return null;
        }
    }
}

namespace StellarIndustries.Production
{
    /// <summary>
    /// Mining a rock with the laser gives as much as its purity says too: half on an impure
    /// rock (at least one), twice as much on a pure one, like the extractors.
    /// </summary>
    /// <summary>
    /// Inexhaustible deposits are for the extractors only: the mining laser does not wear them
    /// down (they would be an endless free mine for a player otherwise).
    /// </summary>
    [HarmonyLib.HarmonyPatch(typeof(Planets.Objects.MinablePlanetObject), nameof(Planets.Objects.MinablePlanetObject.ReduceHealth))]
    internal static class NodeMiningPatch
    {
        private static bool Prefix(Planets.Objects.MinablePlanetObject __instance)
        {
            try
            {
                return !Deposits.IsNode(__instance);
            }
            catch (System.Exception e)
            {
                ErrorLog.Report("inexhaustible deposit", e);
                return true;
            }
        }
    }

    [HarmonyLib.HarmonyPatch(typeof(Planets.Objects.MinablePlanetObject), nameof(Planets.Objects.MinablePlanetObject.Label), HarmonyLib.MethodType.Getter)]
    internal static class RockLabelPatch
    {
        private static void Postfix(Planets.Objects.MinablePlanetObject __instance, ref string __result)
        {
            try
            {
                __result = Deposits.LabelOf(__instance, __result);
            }
            catch (System.Exception e)
            {
                ErrorLog.Report("rock label", e);
            }
        }
    }

    [HarmonyLib.HarmonyPatch(typeof(Planets.Objects.MinablePlanetObject), nameof(Planets.Objects.MinablePlanetObject.GetItemDrop))]
    internal static class MiningPurityPatch
    {
        /// <summary>A rock mined before any scan still gives the mod's ore it holds.</summary>
        private static void Prefix(Planets.Objects.MinablePlanetObject __instance) => Deposits.Convert(__instance);

        private static void Postfix(Planets.Objects.MinablePlanetObject __instance, ref Items.Model.ItemDrop __result)
        {
            try
            {
                if (__result.Quantity == 0 || !Deposits.Extractable(__result.ItemId))
                    return;
                float factor = DepositPurity.Factor(Deposits.PurityOf(__instance));
                __result.Quantity = (byte)Mathf.Clamp(Mathf.RoundToInt(__result.Quantity * factor), 1, 255);
            }
            catch (System.Exception e)
            {
                ErrorLog.Report("mining purity", e);
            }
        }
    }
}
