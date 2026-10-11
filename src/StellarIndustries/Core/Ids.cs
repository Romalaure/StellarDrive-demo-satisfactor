namespace StellarIndustries.Core
{
    /// <summary>
    /// Id ranges reserved by Stellar Industries. Other mods of the group already use parts
    /// 7101-7201 (TF), 7301-7395 (TF Advanced), items 7401-7415 and techs 7501-7511; the game's
    /// items are 0-198. Any clash found at load is logged and the clashing content is not added.
    /// </summary>
    internal static class Ids
    {
        /// <summary>Parts (machines, logistics, structure): 7601-7699.</summary>
        public const ushort FirstPart = 7601, LastPart = 7699;

        /// <summary>Items (ores, intermediates, components, fluids): 7701-7799.</summary>
        public const uint FirstItem = 7701, LastItem = 7799;

        /// <summary>Research techs: 7801-7899.</summary>
        public const ushort FirstTech = 7801, LastTech = 7899;
    }

    /// <summary>The game's own items reused by the mod (ids read from the game's item list).</summary>
    internal static class GameItems
    {
        public const uint Iron = 101, Glass = 102, StellarShard = 103, Aluminum = 104, Water = 105, Hydrogen = 106,
            CarbonDioxide = 107, Oxygen = 108, Ice = 109, Wood = 110, Ethanol = 111, Copper = 112, Biofuel = 100;

        /// <summary>Every game item id a recipe may use (checked by the validator).</summary>
        public static readonly uint[] All = { Biofuel, Iron, Glass, StellarShard, Aluminum, Water, Hydrogen, CarbonDioxide, Oxygen, Ice, Wood, Ethanol, Copper };
    }
}
