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
            // ---- Workshop: shapes metal (ingots first: the raw recipes are the fallback)
            R("workshop.iron_plate", "Plaque de fer", MachineCatalog.Workshop, 4f, In(ResourceCatalog.IronIngot, 3), Out(ResourceCatalog.IronPlate, 2)),
            R("workshop.iron_rod", "Tige de fer", MachineCatalog.Workshop, 3f, In(ResourceCatalog.IronIngot, 1), Out(ResourceCatalog.IronRod, 1)),
            R("workshop.copper_wire", "Fil de cuivre", MachineCatalog.Workshop, 3f, In(ResourceCatalog.CopperIngot, 1), Out(ResourceCatalog.CopperWire, 2)),
            R("workshop.iron_plate_raw", "Plaque de fer (minerai brut)", MachineCatalog.Workshop, 4f, In(GameItems.Iron, 2), Out(ResourceCatalog.IronPlate, 1)),
            R("workshop.copper_wire_raw", "Fil de cuivre (minerai brut)", MachineCatalog.Workshop, 3f, In(GameItems.Copper, 1), Out(ResourceCatalog.CopperWire, 1)),

            R("workshop.steel_beam", "Poutre d'acier", MachineCatalog.Workshop, 4f, In(ResourceCatalog.SteelIngot, 3), Out(ResourceCatalog.SteelBeam, 1), tier: 1),
            R("workshop.steel_pipe", "Tuyau d'acier", MachineCatalog.Workshop, 3f, In(ResourceCatalog.SteelIngot, 1), Out(ResourceCatalog.SteelPipe, 2), tier: 1),

            // ---- Smelter
            R("smelter.iron_ingot", "Lingot de fer", MachineCatalog.Smelter, 2f, In(GameItems.Iron, 1), Out(ResourceCatalog.IronIngot, 1)),
            R("smelter.copper_ingot", "Lingot de cuivre", MachineCatalog.Smelter, 2f, In(GameItems.Copper, 1), Out(ResourceCatalog.CopperIngot, 1)),
            R("smelter.aluminum_ingot", "Lingot d'aluminium", MachineCatalog.Smelter, 4f, In(GameItems.Aluminum, 2), Out(ResourceCatalog.AluminumIngot, 1), tier: 1),
            R("smelter.crushed_iron", "Lingot de fer (minerai broyé)", MachineCatalog.Smelter, 3f, In(ResourceCatalog.CrushedIron, 3), Out(ResourceCatalog.IronIngot, 2), tier: 1),
            R("smelter.crushed_copper", "Lingot de cuivre (minerai broyé)", MachineCatalog.Smelter, 3f, In(ResourceCatalog.CrushedCopper, 3), Out(ResourceCatalog.CopperIngot, 2), tier: 1),
            new RecipeDef
            {
                Id = "smelter.steel", Name = "Lingot d'acier", Machine = MachineCatalog.Smelter, Seconds = 4f, Tier = 1,
                Inputs = new[] { new ItemAmount(GameItems.Iron, 3), new ItemAmount(ResourceCatalog.BiomassBrick, 1) },
                Outputs = new[] { new ItemAmount(ResourceCatalog.SteelIngot, 2) }
            },
            R("smelter.glass", "Verre", MachineCatalog.Smelter, 3f, In(ResourceCatalog.StellarDust, 2), Out(GameItems.Glass, 1), tier: 1),

            // ---- Press
            R("press.screws", "Vis", MachineCatalog.Press, 4f, In(ResourceCatalog.IronRod, 1), Out(ResourceCatalog.Screws, 4), tier: 1),
            R("press.cable", "Câble", MachineCatalog.Press, 4f, In(ResourceCatalog.CopperWire, 2), Out(ResourceCatalog.Cable, 1), tier: 1),
            R("press.briquette", "Briquette de biomasse", MachineCatalog.Press, 6f, In(GameItems.Wood, 4), Out(ResourceCatalog.BiomassBrick, 1), tier: 1),
            R("press.copper_sheet", "Feuille de cuivre", MachineCatalog.Press, 4f, In(ResourceCatalog.CopperIngot, 2), Out(ResourceCatalog.CopperSheet, 1), tier: 1),
            new RecipeDef
            {
                Id = "press.concrete", Name = "Béton", Machine = MachineCatalog.Press, Seconds = 4f, Tier = 1,
                Inputs = new[] { new ItemAmount(ResourceCatalog.StellarDust, 2), new ItemAmount(GameItems.Ice, 1) },
                Outputs = new[] { new ItemAmount(ResourceCatalog.Concrete, 2) }
            },
            R("press.aluminum_sheet", "Tôle d'aluminium", MachineCatalog.Press, 6f, In(ResourceCatalog.AluminumIngot, 2), Out(ResourceCatalog.AluminumSheet, 1), tier: 1),

            // ---- Crusher (the shard gives a co-product)
            R("crusher.iron", "Fer broyé", MachineCatalog.Crusher, 3f, In(GameItems.Iron, 1), Out(ResourceCatalog.CrushedIron, 2), tier: 1),
            R("crusher.copper", "Cuivre broyé", MachineCatalog.Crusher, 3f, In(GameItems.Copper, 1), Out(ResourceCatalog.CrushedCopper, 2), tier: 1),
            new RecipeDef
            {
                Id = "crusher.shard", Name = "Poussière stellaire", Machine = MachineCatalog.Crusher, Seconds = 4f, Tier = 1,
                Inputs = new[] { new ItemAmount(GameItems.StellarShard, 1) },
                Outputs = new[] { new ItemAmount(ResourceCatalog.StellarDust, 3), new ItemAmount(GameItems.Glass, 1) }
            },

            // ---- Assembler (several inputs)
            new RecipeDef
            {
                Id = "assembler.reinforced_plate", Name = "Plaque renforcée", Machine = MachineCatalog.Assembler, Seconds = 12f, Tier = 1,
                Inputs = new[] { new ItemAmount(ResourceCatalog.IronPlate, 4), new ItemAmount(ResourceCatalog.Screws, 8) },
                Outputs = new[] { new ItemAmount(ResourceCatalog.ReinforcedPlate, 1) }
            },
            new RecipeDef
            {
                Id = "assembler.rotor", Name = "Rotor", Machine = MachineCatalog.Assembler, Seconds = 12f, Tier = 2,
                Inputs = new[] { new ItemAmount(ResourceCatalog.IronRod, 5), new ItemAmount(ResourceCatalog.Screws, 20) },
                Outputs = new[] { new ItemAmount(ResourceCatalog.Rotor, 1) }
            },
            new RecipeDef
            {
                Id = "assembler.stator", Name = "Stator", Machine = MachineCatalog.Assembler, Seconds = 10f, Tier = 2,
                Inputs = new[] { new ItemAmount(ResourceCatalog.SteelPipe, 3), new ItemAmount(ResourceCatalog.CopperWire, 8) },
                Outputs = new[] { new ItemAmount(ResourceCatalog.Stator, 1) }
            },
            new RecipeDef
            {
                Id = "assembler.frame", Name = "Cadre d'assemblage", Machine = MachineCatalog.Assembler, Seconds = 30f, Tier = 2,
                Inputs = new[] { new ItemAmount(ResourceCatalog.ReinforcedPlate, 3), new ItemAmount(ResourceCatalog.IronRod, 12) },
                Outputs = new[] { new ItemAmount(ResourceCatalog.AssemblyFrame, 2) }
            },
            new RecipeDef
            {
                Id = "assembler.circuit", Name = "Circuit imprimé", Machine = MachineCatalog.Assembler, Seconds = 8f, Tier = 2,
                Inputs = new[] { new ItemAmount(ResourceCatalog.CopperSheet, 2), new ItemAmount(GameItems.Glass, 1) },
                Outputs = new[] { new ItemAmount(ResourceCatalog.CircuitBoard, 1) }
            },
            new RecipeDef
            {
                Id = "assembler.heat_sink", Name = "Dissipateur thermique", Machine = MachineCatalog.Assembler, Seconds = 8f, Tier = 2,
                Inputs = new[] { new ItemAmount(ResourceCatalog.AluminumSheet, 5), new ItemAmount(ResourceCatalog.CopperSheet, 3) },
                Outputs = new[] { new ItemAmount(ResourceCatalog.HeatSink, 1) }
            },

            // ---- Fabricator (up to four inputs)
            new RecipeDef
            {
                Id = "fabricator.motor", Name = "Moteur électrique", Machine = MachineCatalog.Fabricator, Seconds = 12f, Tier = 2,
                Inputs = new[] { new ItemAmount(ResourceCatalog.Rotor, 2), new ItemAmount(ResourceCatalog.Stator, 2) },
                Outputs = new[] { new ItemAmount(ResourceCatalog.Motor, 1) }
            },
            new RecipeDef
            {
                Id = "fabricator.computer", Name = "Ordinateur", Machine = MachineCatalog.Fabricator, Seconds = 20f, Tier = 3,
                Inputs = new[] { new ItemAmount(ResourceCatalog.CircuitBoard, 4), new ItemAmount(ResourceCatalog.Cable, 8), new ItemAmount(ResourceCatalog.ReinforcedPlate, 2) },
                Outputs = new[] { new ItemAmount(ResourceCatalog.Computer, 1) }
            },
            new RecipeDef
            {
                Id = "fabricator.heavy_frame", Name = "Cadre lourd", Machine = MachineCatalog.Fabricator, Seconds = 30f, Tier = 3,
                Inputs = new[] { new ItemAmount(ResourceCatalog.AssemblyFrame, 4), new ItemAmount(ResourceCatalog.SteelPipe, 10), new ItemAmount(ResourceCatalog.SteelBeam, 4), new ItemAmount(ResourceCatalog.Concrete, 5) },
                Outputs = new[] { new ItemAmount(ResourceCatalog.HeavyFrame, 1) }
            }
        };

        private static ItemAmount[] In(uint item, int count) => new[] { new ItemAmount(item, count) };

        private static ItemAmount[] Out(uint item, int count) => new[] { new ItemAmount(item, count) };

        private static RecipeDef R(string id, string name, ushort machine, float seconds, ItemAmount[] inputs, ItemAmount[] outputs, int tier = 0) =>
            new RecipeDef { Id = id, Name = name, Machine = machine, Seconds = seconds, Inputs = inputs, Outputs = outputs, Tier = tier };

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

        public static RecipeDef Get(string id) => All.FirstOrDefault(r => r.Id == id) ?? (id != null && Extractions.TryGetValue(id, out RecipeDef e) ? e : null);

        private static readonly Dictionary<string, RecipeDef> Extractions = new Dictionary<string, RecipeDef>();

        /// <summary>
        /// An extractor's work as a recipe without inputs: one item of its deposit per cycle. Made on
        /// demand (deposits are found in the world), never listed in <see cref="All"/>.
        /// </summary>
        public static RecipeDef Extraction(ushort machine, uint item, float seconds, string itemName)
        {
            string id = "extract." + machine + "." + item;
            if (!Extractions.TryGetValue(id, out RecipeDef recipe))
            {
                recipe = new RecipeDef
                {
                    Id = id, Name = "Extraction : " + itemName, Machine = machine, Seconds = seconds,
                    Inputs = new ItemAmount[0], Outputs = new[] { new ItemAmount(item, 1) }
                };
                Extractions[id] = recipe;
            }
            return recipe;
        }
    }
}
