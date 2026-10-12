using System.Collections.Generic;
using Core.Services;
using Planets.Objects;
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
    /// A rock gives its own drop item; it is not worn down by an extractor, but if a player mines
    /// it away the extractor stops until another rock is within reach.
    /// </summary>
    internal static class Deposits
    {
        public struct Deposit
        {
            public uint Item;
            public string Name;
            public Vector3 Position;
            public float Distance;
        }

        private const float ScanInterval = 3f;

        private static readonly List<(uint Item, string Name, Vector3 Position)> Rocks = new List<(uint, string, Vector3)>();
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

        private static void Scan()
        {
            float now = Time.unscaledTime;
            if (now < _nextScan)
                return;
            _nextScan = now + ScanInterval;
            Rocks.Clear();
            Cache.Clear();
            foreach (MinablePlanetObject rock in Object.FindObjectsByType<MinablePlanetObject>(FindObjectsSortMode.None))
                Add(rock);
        }

        /// <summary>Scans again at the next request (the in-game test after placing a rock).</summary>
        public static void Rescan() => _nextScan = 0f;

        private static void Add(MinablePlanetObject rock)
        {
            if (rock == null || rock.dropRate.item == null)
                return;
            uint item = rock.dropRate.item.id;
            if (!Extractable(item))
                return;
            Rocks.Add((item, string.IsNullOrEmpty(rock.Label) ? rock.dropRate.item.itemName : rock.Label, rock.transform.position));
        }

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
                foreach (var rock in Rocks)
                {
                    float d = Vector3.Distance(at, rock.Position);
                    if (d > best)
                        continue;
                    best = d;
                    found = new Deposit { Item = rock.Item, Name = rock.Name, Position = rock.Position, Distance = d };
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
