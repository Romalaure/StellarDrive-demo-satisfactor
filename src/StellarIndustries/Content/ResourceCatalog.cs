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
        Chunk,
        Ingot,
        Screws,
        Powder,
        HeavyPlate,
        Brick
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
        public const uint IronIngot = 7703;
        public const uint CopperIngot = 7704;
        public const uint AluminumIngot = 7705;
        public const uint IronRod = 7706;
        public const uint Screws = 7707;
        public const uint ReinforcedPlate = 7708;
        public const uint AluminumSheet = 7709;
        public const uint Cable = 7710;
        public const uint CrushedIron = 7711;
        public const uint CrushedCopper = 7712;
        public const uint StellarDust = 7713;
        public const uint BiomassBrick = 7714;

        public static readonly ResourceDef[] All =
        {
            // ---- Intermediates (phase A)
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
            },
            // ---- Smelted (phase C)
            new ResourceDef
            {
                Id = IronIngot, Name = "Lingot de fer", Category = ResourceCategory.Intermediate, Mass = 2f, Tier = 0,
                Description = "Fer fondu et coulé. Se travaille mieux que le minerai brut.",
                MaterialFrom = GameItems.Iron, Tint = new[] { 0.7f, 0.72f, 0.78f }, Shape = ResourceShape.Ingot
            },
            new ResourceDef
            {
                Id = CopperIngot, Name = "Lingot de cuivre", Category = ResourceCategory.Intermediate, Mass = 2f, Tier = 0,
                Description = "Cuivre fondu et coulé.",
                MaterialFrom = GameItems.Copper, Tint = new[] { 1.1f, 0.7f, 0.45f }, Shape = ResourceShape.Ingot
            },
            new ResourceDef
            {
                Id = AluminumIngot, Name = "Lingot d'aluminium", Category = ResourceCategory.Intermediate, Mass = 1f, Tier = 1,
                Description = "Aluminium affiné, léger et résistant.",
                MaterialFrom = GameItems.Aluminum, Tint = new[] { 1.05f, 1.05f, 1.1f }, Shape = ResourceShape.Ingot
            },
            // ---- Shaped parts (phase C)
            new ResourceDef
            {
                Id = IronRod, Name = "Tige de fer", Category = ResourceCategory.Intermediate, Mass = 1f, Tier = 0,
                Description = "Barre de fer étirée. Donne les vis et les armatures.",
                MaterialFrom = GameItems.Iron, Tint = new[] { 0.8f, 0.82f, 0.86f }, Shape = ResourceShape.Rod
            },
            new ResourceDef
            {
                Id = Screws, Name = "Vis", Category = ResourceCategory.Component, Mass = 0.1f, Tier = 0, MaxStack = 200,
                Description = "Petites vis d'assemblage, par poignées.",
                MaterialFrom = GameItems.Iron, Tint = new[] { 0.75f, 0.75f, 0.7f }, Shape = ResourceShape.Screws
            },
            new ResourceDef
            {
                Id = Cable, Name = "Câble", Category = ResourceCategory.Component, Mass = 0.8f, Tier = 0,
                Description = "Fils de cuivre torsadés et gainés.",
                MaterialFrom = GameItems.Copper, Tint = new[] { 0.35f, 0.35f, 0.38f }, Shape = ResourceShape.Coil
            },
            new ResourceDef
            {
                Id = AluminumSheet, Name = "Tôle d'aluminium", Category = ResourceCategory.Component, Mass = 1f, Tier = 1,
                Description = "Tôle fine et légère pour les coques et les machines avancées.",
                MaterialFrom = GameItems.Aluminum, Tint = new[] { 1.1f, 1.1f, 1.15f }, Shape = ResourceShape.Plate
            },
            new ResourceDef
            {
                Id = ReinforcedPlate, Name = "Plaque renforcée", Category = ResourceCategory.Component, Mass = 6f, Tier = 1, MaxStack = 50,
                Description = "Plaques de fer vissées en sandwich. Structure des machines lourdes.",
                MaterialFrom = GameItems.Iron, Tint = new[] { 0.6f, 0.62f, 0.68f }, Shape = ResourceShape.HeavyPlate
            },
            // ---- Crushed (phase C)
            new ResourceDef
            {
                Id = CrushedIron, Name = "Fer broyé", Category = ResourceCategory.Ore, Mass = 1f, Tier = 1,
                Description = "Minerai de fer concassé : la fonderie en tire plus de métal.",
                MaterialFrom = GameItems.Iron, Tint = new[] { 0.55f, 0.45f, 0.42f }, Shape = ResourceShape.Powder
            },
            new ResourceDef
            {
                Id = CrushedCopper, Name = "Cuivre broyé", Category = ResourceCategory.Ore, Mass = 1f, Tier = 1,
                Description = "Minerai de cuivre concassé : la fonderie en tire plus de métal.",
                MaterialFrom = GameItems.Copper, Tint = new[] { 0.9f, 0.55f, 0.35f }, Shape = ResourceShape.Powder
            },
            new ResourceDef
            {
                Id = StellarDust, Name = "Poussière stellaire", Category = ResourceCategory.Ore, Mass = 0.3f, Tier = 1, MaxStack = 200,
                Description = "Éclats stellaires réduits en poudre scintillante. Fondue, elle donne du verre.",
                MaterialFrom = GameItems.StellarShard, Tint = new[] { 1f, 1f, 1f }, Shape = ResourceShape.Powder
            },
            // ---- Fuel (phase E)
            new ResourceDef
            {
                Id = BiomassBrick, Name = "Briquette de biomasse", Category = ResourceCategory.Component, Mass = 1.5f, Tier = 1,
                Description = "Bois broyé et pressé. Brûle cinq fois plus longtemps qu'une bûche dans le générateur à combustion.",
                MaterialFrom = GameItems.Wood, Tint = new[] { 0.6f, 0.45f, 0.3f }, Shape = ResourceShape.Brick
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
