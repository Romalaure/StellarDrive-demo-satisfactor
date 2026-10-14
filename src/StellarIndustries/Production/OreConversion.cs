using StellarIndustries.Content;
using StellarIndustries.Core;

namespace StellarIndustries.Production
{
    /// <summary>
    /// The mod's own ores in the world: part of the game's iron, copper and aluminum rocks hold
    /// coal, sulfur, limestone, raw quartz or gold ore instead. Nothing is stored: every player's
    /// game works it out the same way from the rock's own id (or place), so the host's extractors,
    /// the laser and every scanner agree. Each planet has three of the five ores, and about four
    /// rocks in ten of the metals are one of them.
    /// </summary>
    internal static class OreConversion
    {
        public static readonly uint[] Ores =
        {
            ResourceCatalog.Coal, ResourceCatalog.Sulfur, ResourceCatalog.Limestone, ResourceCatalog.RawQuartz, ResourceCatalog.GoldOre
        };

        /// <summary>Share of the metal rocks that hold one of the mod's ores (percent).</summary>
        public const int Share = 40;

        /// <summary>Share of the rocks that are inexhaustible deposits (percent).</summary>
        public const int NodeShare = 12;

        /// <summary>An extractor on an inexhaustible deposit works this many times faster.</summary>
        public const float NodeBonus = 2.5f;

        /// <summary>Whether this rock is an inexhaustible deposit: the laser never breaks it, an extractor on it works faster.</summary>
        public static bool IsNode(int planet, int b, int c) =>
            Mix((uint)planet * 3266489917u ^ (uint)b * 2654435761u ^ (uint)c * 2246822519u ^ 0x9e3779b9u) % 100u < NodeShare;

        public static bool Converts(uint item) => item == GameItems.Iron || item == GameItems.Copper || item == GameItems.Aluminum;

        /// <summary>The ore a rock of <paramref name="item"/> holds; <paramref name="planet"/> picks the planet's three ores.</summary>
        public static uint Of(uint item, int planet, int b, int c)
        {
            if (!Converts(item))
                return item;
            uint h = Mix((uint)planet * 2246822519u ^ (uint)b * 3266489917u ^ (uint)c * 668265263u ^ item * 374761393u);
            if (h % 100u >= Share)
                return item;
            uint first = Mix((uint)planet * 2654435761u + 97u) % (uint)Ores.Length;
            return Ores[(first + (h / 100u) % 3u) % (uint)Ores.Length];
        }

        private static uint Mix(uint h)
        {
            unchecked
            {
                h ^= h >> 15;
                h *= 2246822519u;
                h ^= h >> 13;
                h *= 3266489917u;
                h ^= h >> 16;
                return h;
            }
        }
    }
}
