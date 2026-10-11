using System.Collections.Generic;
using System.Linq;
using StellarIndustries.Core;

namespace StellarIndustries.Content
{
    internal enum ResourceCategory
    {
        Ore,
        Intermediate,
        Component,
        Advanced,
        Fluid
    }

    /// <summary>The look of an item's 3D model (built by the mod, worn with a game item's material).</summary>
    internal enum ResourceShape
    {
        Plate,
        Coil,
        Rod,
        Chunk
    }

    /// <summary>
    /// One industrial resource. Pure data: the game item (ItemSettings) is made from it at load
    /// by <see cref="IndustrialItems"/>. Colors are RGB in 0-1 (tint over the donor material).
    /// </summary>
    internal sealed class ResourceDef
    {
        public uint Id;
        public string Name;
        public string Description;
        public ResourceCategory Category;
        public float Mass = 2f;
        /// <summary>Largest stack in one slot (the game's slots hold at most 255).</summary>
        public int MaxStack = 100;
        /// <summary>The game item whose material (texture, shader) the model wears.</summary>
        public uint MaterialFrom = GameItems.Iron;
        public float[] Tint = { 1f, 1f, 1f };
        public ResourceShape Shape;
        /// <summary>Tech level that makes it obtainable (documentation and validation only for now).</summary>
        public int Tier;
    }

    /// <summary>
    /// The mod's items. Only what a released phase really produces is listed: every entry here
    /// becomes a game item, saved in chests and inventories by the game itself, so an id must
    /// never be reused for another resource once released.
    /// </summary>
    internal static class ResourceCatalog
    {
        public const uint IronPlate = 7701;
        public const uint CopperWire = 7702;

        public static readonly ResourceDef[] All =
        {
            new ResourceDef
            {
                Id = IronPlate, Name = "Plaque de fer", Category = ResourceCategory.Intermediate, Mass = 1.5f, Tier = 0,
                Description = "Fer pressé en plaque. Base des pièces structurelles.",
                MaterialFrom = GameItems.Iron, Tint = new[] { 0.85f, 0.88f, 0.92f }, Shape = ResourceShape.Plate
            },
            new ResourceDef
            {
                Id = CopperWire, Name = "Fil de cuivre", Category = ResourceCategory.Intermediate, Mass = 0.5f, Tier = 0,
                Description = "Cuivre étiré en bobine. Base des composants électriques.",
                MaterialFrom = GameItems.Copper, Tint = new[] { 1f, 0.75f, 0.5f }, Shape = ResourceShape.Coil
            }
        };

        private static Dictionary<uint, ResourceDef> _byId;

        public static ResourceDef Get(uint id)
        {
            if (_byId == null)
                _byId = All.ToDictionary(r => r.Id);
            return _byId.TryGetValue(id, out ResourceDef def) ? def : null;
        }

        /// <summary>Whether a recipe may name this item: one of the mod's or one of the game's.</summary>
        public static bool Exists(uint id) => Get(id) != null || GameItems.All.Contains(id);
    }
}
