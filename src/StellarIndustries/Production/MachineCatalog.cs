using System.Collections.Generic;
using System.Linq;
using StellarIndustries.Core;

namespace StellarIndustries.Production
{
    /// <summary>
    /// One production machine. Pure data: the build part is made from it by
    /// <see cref="Parts.MachineParts"/>, its behaviour is <see cref="MachineSimulation"/>.
    /// </summary>
    internal sealed class MachineDef
    {
        public ushort PartId;
        public string Name;
        public string Description;
        /// <summary>Power drawn while producing (watts). The game's wood generator gives 4000 W.</summary>
        public float Watts;
        /// <summary>Recipe durations are divided by this.</summary>
        public float Speed = 1f;
        /// <summary>Inventory slots (the game's storage state: always 10).</summary>
        public int Slots = 10;
        public float Mass;
        public int Tier;
        public (uint Item, int Count)[] BuildCost;
    }

    internal static class MachineCatalog
    {
        public const ushort Workshop = 7601;

        public static readonly MachineDef[] All =
        {
            new MachineDef
            {
                PartId = Workshop, Name = "Atelier industriel", Tier = 0, Watts = 300f, Mass = 120f,
                Description = "Presse et étire les métaux bruts. Mettez le fer ou le cuivre dedans, branchez-le au réseau électrique : " +
                              "il produit tant qu'il a de l'énergie, de la matière et de la place. Les produits s'empilent dans les derniers emplacements.",
                BuildCost = new[] { (GameItems.Iron, 10), (GameItems.Copper, 4), (GameItems.Glass, 2) }
            }
        };

        private static Dictionary<ushort, MachineDef> _byId;

        public static bool TryGet(ushort partId, out MachineDef def)
        {
            if (_byId == null)
                _byId = All.ToDictionary(m => m.PartId);
            return _byId.TryGetValue(partId, out def);
        }
    }
}
