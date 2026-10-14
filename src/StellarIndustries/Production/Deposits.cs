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
        }

        private const float ScanInterval = 3f;

        private static readonly List<Rock> Rocks = new List<Rock>();
        private static readonly Dictionary<(uint, ushort), (Deposit? Found, float Time)> Cache = new Dictionary<(uint, ushort), (Deposit?, float)>();
        private static float _nextScan;
        private static ShipsClientTracker _client;

        public static int Count => Rocks.Count;

        public static void Forget()
        {
            Rocks.Clear();
            Cache.Clear();
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

        /// <summary>Names an inexhaustible deposit as such (its label, shown when aimed at).</summary>
        private static void MarkNode(MinablePlanetObject rock)
        {
            if (!IsNode(rock))
                return;
            string label = Label(rock);
            string name = string.IsNullOrEmpty(label) ? IndustrialItems.NameOf(rock.dropRate.item.id) : label;
            string mark = Lang.T(" (gisement inépuisable)", " (inexhaustible deposit)");
            if (!name.EndsWith(mark))
                Label(rock) = name + mark;
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

        /// <summary>Every extractable rock loaded on this player's game (a fresh search: call it seldom).</summary>
        public static List<Rock> Loaded()
        {
            var list = new List<Rock>();
            foreach (MinablePlanetObject rock in Object.FindObjectsByType<MinablePlanetObject>(FindObjectsSortMode.None))
            {
                Convert(rock);
                MarkNode(rock);
                if (rock == null || rock.dropRate.item == null)
                    continue;
                uint item = rock.dropRate.item.id;
                if (!Extractable(item))
                    continue;
                // A rock can carry the component twice (same place): it is one deposit
                Vector3 at = rock.transform.position;
                if (list.Exists(r => r.Item == item && (r.Position - at).sqrMagnitude < 0.01f))
                    continue;
                list.Add(new Rock
                {
                    Object = rock, Item = item, Position = rock.transform.position, Purity = PurityOf(rock), Node = IsNode(rock),
                    Name = IndustrialItems.NameOf(item)
                });
            }
            return list;
        }

        private static void Scan()
        {
            float now = Time.unscaledTime;
            if (now < _nextScan)
                return;
            _nextScan = now + ScanInterval;
            Rocks.Clear();
            Cache.Clear();
            Rocks.AddRange(Loaded());
        }

        /// <summary>Scans again at the next request (the in-game test after placing a rock).</summary>
        public static void Rescan() => _nextScan = 0f;

        /// <summary>Host: the rock nearest to this extractor within its reach, or null.</summary>
        public static Deposit? Find(uint shipId, StatefulPart part, MachineDef def)
        {
            Scan();
            if (Cache.TryGetValue((shipId, part.Id), out var cached))
                return cached.Found;
            Deposit? found = null;
            if (_client == null)
                _client = ServiceLocator.GetService<ShipsClientTracker>();
            if (_client != null && _client.TryGetShipVisualTransform(shipId, out Transform ship) && ship != null)
            {
                Vector3 at = ship.TransformPoint(part.LocalPosition);
                float best = def.ExtractRadius;
                foreach (Rock rock in Rocks)
                {
                    // A rock mined away since the last scan
                    if (rock.Object == null)
                        continue;
                    float d = Vector3.Distance(at, rock.Position);
                    if (d > best)
                        continue;
                    best = d;
                    found = new Deposit { Item = rock.Item, Name = rock.Name, Position = rock.Position, Distance = d, Purity = rock.Purity, Node = rock.Node };
                }
            }
            Cache[(shipId, part.Id)] = (found, Time.unscaledTime);
            return found;
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
    [HarmonyLib.HarmonyPatch(typeof(Planets.Objects.MinablePlanetObject), nameof(Planets.Objects.MinablePlanetObject.ReduceHealth))]
    internal static class NodeMiningPatch
    {
        private static readonly HarmonyLib.AccessTools.FieldRef<Planets.Objects.MinablePlanetObject, float> Health =
            HarmonyLib.AccessTools.FieldRefAccess<Planets.Objects.MinablePlanetObject, float>("_currentHealth");

        /// <summary>
        /// On the miner's game: a deposit that would break is whole again instead, and the host is
        /// asked for what breaking it would have given.
        /// </summary>
        private static bool Prefix(Planets.Objects.MinablePlanetObject __instance, float amount)
        {
            try
            {
                if (Health(__instance) <= 0f || Health(__instance) - amount > 0f || !Deposits.IsNode(__instance))
                    return true;
                Health(__instance) = __instance.MaxHealth;
                Items.Model.ItemDrop drop = __instance.GetItemDrop();
                if (drop.Quantity > 0)
                    SINet.SendToServer(SIMessageKind.NodeMined, drop.ItemId, drop.Quantity, "");
                return false;
            }
            catch (System.Exception e)
            {
                ErrorLog.Report("inexhaustible deposit", e);
                return true;
            }
        }

        /// <summary>Host: gives a player what they mined from an inexhaustible deposit (ores only, a few at a time).</summary>
        public static void Install()
        {
            SINet.OnServer(SIMessageKind.NodeMined, (sender, item, count, text) =>
            {
                if (!Deposits.Extractable(item) || count == 0 || count > 12 || IndustrialItems.Find(item) == null)
                    return;
                var players = SDModKit.Game.GameRefs.PlayersServer;
                Players.TrackedPlayerServer player = players == null ? null : sender == null || sender.IsLocalClient
                    ? System.Linq.Enumerable.FirstOrDefault(players.ActivePlayers, p => p != null && p.IsHost)
                    : System.Linq.Enumerable.FirstOrDefault(players.ActivePlayers, p => p != null && p.ConnectionId == sender.ClientId);
                player?.AddItemToInventory(new Items.Model.ItemDrop { ItemId = item, Quantity = (byte)count });
            });
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
