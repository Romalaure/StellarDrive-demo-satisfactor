namespace StellarIndustries.Production
{
    internal enum Purity
    {
        Impure,
        Normal,
        Pure
    }

    /// <summary>
    /// How rich a minable rock is: impure (an extractor works at half speed), normal, or pure
    /// (double speed). It is not stored anywhere: every player's game works it out the same way
    /// from the rock's item and its place on its planet (to the half meter), so the scanner of a
    /// client and the extractors run by the host always agree. About 3 rocks in 10 are impure,
    /// 5 normal and 2 pure.
    /// </summary>
    internal static class DepositPurity
    {
        public static Purity Of(uint item, int x, int y, int z)
        {
            unchecked
            {
                uint h = item * 2654435761u ^ (uint)x * 73856093u ^ (uint)y * 19349663u ^ (uint)z * 83492791u;
                h ^= h >> 15;
                h *= 2246822519u;
                h ^= h >> 13;
                uint roll = h % 10u;
                return roll < 3u ? Purity.Impure : roll < 8u ? Purity.Normal : Purity.Pure;
            }
        }

        /// <summary>Extraction speed factor.</summary>
        public static float Factor(Purity purity) => purity == Purity.Impure ? 0.5f : purity == Purity.Pure ? 2f : 1f;
    }
}
