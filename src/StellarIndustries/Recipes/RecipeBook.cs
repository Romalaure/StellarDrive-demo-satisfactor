using System.Collections.Generic;
using System.Linq;
using StellarIndustries.Content;
using StellarIndustries.Core;
using StellarIndustries.Production;

namespace StellarIndustries.Recipes
{
    internal struct ItemAmount
    {
        public uint Item;
        public int Count;

        public ItemAmount(uint item, int count)
        {
            Item = item;
            Count = count;
        }

        public override string ToString() => Count + " x " + Item;
    }

    /// <summary>
    /// A recipe: inputs taken and outputs given at once when a cycle completes. Every output is
    /// a real item (co-products included); a cycle that cannot store all its outputs does not run.
    /// </summary>
    internal sealed class RecipeDef
    {
        public string Id;
        public string Name;
        public ushort Machine;
        public ItemAmount[] Inputs;
        public ItemAmount[] Outputs;
        /// <summary>Duration of one cycle at speed 1 (seconds).</summary>
        public float Seconds;
        /// <summary>Power while producing; 0 = the machine's own.</summary>
        public float Watts;
        public int Tier;
    }

    /// <summary>The recipes of every machine, in the order a machine tries them.</summary>
    internal static class RecipeBook
    {
        public static readonly RecipeDef[] All =
        {
            new RecipeDef
            {
                Id = "workshop.iron_plate", Name = "Plaque de fer", Machine = MachineCatalog.Workshop, Seconds = 4f,
                Inputs = new[] { new ItemAmount(GameItems.Iron, 2) },
                Outputs = new[] { new ItemAmount(ResourceCatalog.IronPlate, 1) }
            },
            new RecipeDef
            {
                Id = "workshop.copper_wire", Name = "Fil de cuivre", Machine = MachineCatalog.Workshop, Seconds = 3f,
                Inputs = new[] { new ItemAmount(GameItems.Copper, 1) },
                Outputs = new[] { new ItemAmount(ResourceCatalog.CopperWire, 2) }
            }
        };

        private static Dictionary<ushort, RecipeDef[]> _byMachine;
        private static HashSet<string> _disabled = new HashSet<string>();

        /// <summary>Recipes the validator rejected: the machines never run them.</summary>
        public static void Disable(IEnumerable<string> ids)
        {
            _disabled = new HashSet<string>(ids.Where(id => id != null));
            _byMachine = null;
        }

        public static bool IsDisabled(string id) => _disabled.Contains(id);

        /// <summary>The valid recipes of a machine, in the order it tries them.</summary>
        public static RecipeDef[] ForMachine(ushort machine)
        {
            if (_byMachine == null)
                _byMachine = All.Where(r => !_disabled.Contains(r.Id)).GroupBy(r => r.Machine).ToDictionary(g => g.Key, g => g.ToArray());
            return _byMachine.TryGetValue(machine, out RecipeDef[] recipes) ? recipes : new RecipeDef[0];
        }

        public static RecipeDef Get(string id) => All.FirstOrDefault(r => r.Id == id);
    }
}
