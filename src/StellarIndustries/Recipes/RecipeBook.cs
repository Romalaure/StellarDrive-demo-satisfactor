using System;
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
        /// <summary>The mod's tech that must be researched before the machine runs it (0: none).</summary>
        public ushort Tech;
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
                Outputs = new[] { new ItemAmount(ResourceCatalog.StellarDust, 3), new ItemAmount(ResourceCatalog.Silica, 1) }
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
            },

            // ---- 0.10: crusher
            new RecipeDef
            {
                Id = "crusher.biomass", Name = "Biomasse", Machine = MachineCatalog.Crusher, Seconds = 3f, Tier = 1,
                Inputs = new[] { new ItemAmount(GameItems.Wood, 1) },
                Outputs = new[] { new ItemAmount(ResourceCatalog.Biomass, 4) }
            },
            new RecipeDef
            {
                Id = "crusher.silica", Name = "Silice", Machine = MachineCatalog.Crusher, Seconds = 3f, Tier = 1,
                Inputs = new[] { new ItemAmount(GameItems.Glass, 1) },
                Outputs = new[] { new ItemAmount(ResourceCatalog.Silica, 2) }
            },
            // ---- Foundry (two inputs)
            new RecipeDef
            {
                Id = "foundry.steel", Name = "Lingot d'acier (charbon de bois)", Machine = MachineCatalog.Foundry, Seconds = 4f, Tier = 2,
                Inputs = new[] { new ItemAmount(GameItems.Iron, 3), new ItemAmount(ResourceCatalog.Charcoal, 1) },
                Outputs = new[] { new ItemAmount(ResourceCatalog.SteelIngot, 3) }
            },
            new RecipeDef
            {
                Id = "foundry.alloy", Name = "Lingot d'alliage", Machine = MachineCatalog.Foundry, Seconds = 4f, Tier = 2,
                Inputs = new[] { new ItemAmount(GameItems.Aluminum, 2), new ItemAmount(ResourceCatalog.CopperIngot, 1) },
                Outputs = new[] { new ItemAmount(ResourceCatalog.AlloyIngot, 2) }
            },
            new RecipeDef
            {
                Id = "foundry.quartz", Name = "Cristal de quartz", Machine = MachineCatalog.Foundry, Seconds = 6f, Tier = 2,
                Inputs = new[] { new ItemAmount(ResourceCatalog.Silica, 3), new ItemAmount(ResourceCatalog.StellarDust, 1) },
                Outputs = new[] { new ItemAmount(ResourceCatalog.QuartzCrystal, 2) }
            },
            // ---- Refinery (biomass chemistry, co-products)
            new RecipeDef
            {
                Id = "refinery.charcoal", Name = "Charbon de bois", Machine = MachineCatalog.Refinery, Seconds = 4f, Tier = 2,
                Inputs = new[] { new ItemAmount(ResourceCatalog.Biomass, 4) },
                Outputs = new[] { new ItemAmount(ResourceCatalog.Charcoal, 1) }
            },
            new RecipeDef
            {
                Id = "refinery.plastic", Name = "Plastique", Machine = MachineCatalog.Refinery, Seconds = 6f, Tier = 2,
                Inputs = new[] { new ItemAmount(ResourceCatalog.Biomass, 6), new ItemAmount(GameItems.Ice, 1) },
                Outputs = new[] { new ItemAmount(ResourceCatalog.Plastic, 2), new ItemAmount(ResourceCatalog.Resin, 1) }
            },
            new RecipeDef
            {
                Id = "refinery.resin", Name = "Résine polymère", Machine = MachineCatalog.Refinery, Seconds = 5f, Tier = 2,
                Inputs = new[] { new ItemAmount(ResourceCatalog.Biomass, 5) },
                Outputs = new[] { new ItemAmount(ResourceCatalog.Resin, 2) }
            },
            new RecipeDef
            {
                Id = "refinery.rubber", Name = "Caoutchouc", Machine = MachineCatalog.Refinery, Seconds = 6f, Tier = 2,
                Inputs = new[] { new ItemAmount(ResourceCatalog.Resin, 3), new ItemAmount(GameItems.Ice, 1) },
                Outputs = new[] { new ItemAmount(ResourceCatalog.Rubber, 2) }
            },
            // ---- Workshop
            new RecipeDef
            {
                Id = "workshop.fine_wire", Name = "Fil fin", Machine = MachineCatalog.Workshop, Seconds = 3f, Tier = 1,
                Inputs = new[] { new ItemAmount(ResourceCatalog.CopperIngot, 1) },
                Outputs = new[] { new ItemAmount(ResourceCatalog.FineWire, 5) }
            },
            new RecipeDef
            {
                Id = "workshop.alloy_sheet", Name = "Tôle d'alliage", Machine = MachineCatalog.Workshop, Seconds = 4f, Tier = 2,
                Inputs = new[] { new ItemAmount(ResourceCatalog.AlloyIngot, 3) },
                Outputs = new[] { new ItemAmount(ResourceCatalog.AlloySheet, 2) }
            },
            new RecipeDef
            {
                Id = "workshop.casing", Name = "Boîtier en aluminium", Machine = MachineCatalog.Workshop, Seconds = 3f, Tier = 2,
                Inputs = new[] { new ItemAmount(ResourceCatalog.AluminumIngot, 3) },
                Outputs = new[] { new ItemAmount(ResourceCatalog.AluminumCasing, 2) }
            },
            // ---- Assembler
            new RecipeDef
            {
                Id = "assembler.encased_beam", Name = "Poutre enrobée", Machine = MachineCatalog.Assembler, Seconds = 10f, Tier = 2,
                Inputs = new[] { new ItemAmount(ResourceCatalog.SteelBeam, 3), new ItemAmount(ResourceCatalog.Concrete, 6) },
                Outputs = new[] { new ItemAmount(ResourceCatalog.EncasedBeam, 1) }
            },
            new RecipeDef
            {
                Id = "assembler.smart_panel", Name = "Panneau intelligent", Machine = MachineCatalog.Assembler, Seconds = 15f, Tier = 2,
                Inputs = new[] { new ItemAmount(ResourceCatalog.ReinforcedPlate, 1), new ItemAmount(ResourceCatalog.Rotor, 1) },
                Outputs = new[] { new ItemAmount(ResourceCatalog.SmartPanel, 1) }
            },
            new RecipeDef
            {
                Id = "assembler.framework", Name = "Ossature polyvalente", Machine = MachineCatalog.Assembler, Seconds = 20f, Tier = 2,
                Inputs = new[] { new ItemAmount(ResourceCatalog.AssemblyFrame, 1), new ItemAmount(ResourceCatalog.SteelBeam, 6) },
                Outputs = new[] { new ItemAmount(ResourceCatalog.Framework, 2) }
            },
            new RecipeDef
            {
                Id = "assembler.harness", Name = "Faisceau de câbles", Machine = MachineCatalog.Assembler, Seconds = 15f, Tier = 2,
                Inputs = new[] { new ItemAmount(ResourceCatalog.Stator, 1), new ItemAmount(ResourceCatalog.Cable, 10) },
                Outputs = new[] { new ItemAmount(ResourceCatalog.WiringHarness, 1) }
            },
            new RecipeDef
            {
                Id = "assembler.oscillator", Name = "Oscillateur à quartz", Machine = MachineCatalog.Assembler, Seconds = 12f, Tier = 3,
                Inputs = new[] { new ItemAmount(ResourceCatalog.QuartzCrystal, 4), new ItemAmount(ResourceCatalog.AluminumCasing, 1) },
                Outputs = new[] { new ItemAmount(ResourceCatalog.Oscillator, 1) }
            },
            new RecipeDef
            {
                Id = "assembler.battery", Name = "Cellule de batterie", Machine = MachineCatalog.Assembler, Seconds = 10f, Tier = 2,
                Inputs = new[] { new ItemAmount(ResourceCatalog.AlloySheet, 2), new ItemAmount(ResourceCatalog.AluminumCasing, 1) },
                Outputs = new[] { new ItemAmount(ResourceCatalog.BatteryCell, 1) }
            },
            // ---- Fabricator
            new RecipeDef
            {
                Id = "fabricator.connector", Name = "Connecteur rapide", Machine = MachineCatalog.Fabricator, Seconds = 15f, Tier = 3,
                Inputs = new[] { new ItemAmount(ResourceCatalog.FineWire, 40), new ItemAmount(ResourceCatalog.Cable, 4), new ItemAmount(ResourceCatalog.Plastic, 2) },
                Outputs = new[] { new ItemAmount(ResourceCatalog.FastConnector, 1) }
            },
            new RecipeDef
            {
                Id = "fabricator.radio", Name = "Module radio", Machine = MachineCatalog.Fabricator, Seconds = 25f, Tier = 3,
                Inputs = new[] { new ItemAmount(ResourceCatalog.Oscillator, 1), new ItemAmount(ResourceCatalog.HeatSink, 2), new ItemAmount(ResourceCatalog.AluminumCasing, 6) },
                Outputs = new[] { new ItemAmount(ResourceCatalog.RadioModule, 1) }
            },
            new RecipeDef
            {
                Id = "fabricator.cooling", Name = "Système de refroidissement", Machine = MachineCatalog.Fabricator, Seconds = 20f, Tier = 3,
                Inputs = new[] { new ItemAmount(ResourceCatalog.HeatSink, 2), new ItemAmount(ResourceCatalog.Rubber, 4), new ItemAmount(GameItems.Ice, 5) },
                Outputs = new[] { new ItemAmount(ResourceCatalog.CoolingSystem, 1) }
            },
            new RecipeDef
            {
                Id = "fabricator.engine_block", Name = "Bloc moteur", Machine = MachineCatalog.Fabricator, Seconds = 30f, Tier = 3,
                Inputs = new[] { new ItemAmount(ResourceCatalog.Motor, 1), new ItemAmount(ResourceCatalog.Rubber, 8), new ItemAmount(ResourceCatalog.SmartPanel, 2) },
                Outputs = new[] { new ItemAmount(ResourceCatalog.EngineBlock, 1) }
            },
            new RecipeDef
            {
                Id = "fabricator.control_unit", Name = "Contrôleur adaptatif", Machine = MachineCatalog.Fabricator, Seconds = 40f, Tier = 3,
                Inputs = new[] { new ItemAmount(ResourceCatalog.WiringHarness, 2), new ItemAmount(ResourceCatalog.Framework, 1), new ItemAmount(ResourceCatalog.Computer, 1) },
                Outputs = new[] { new ItemAmount(ResourceCatalog.ControlUnit, 1) }
            },
            new RecipeDef
            {
                Id = "fabricator.supercomputer", Name = "Supercalculateur", Machine = MachineCatalog.Fabricator, Seconds = 60f, Tier = 3,
                Inputs = new[] { new ItemAmount(ResourceCatalog.Computer, 2), new ItemAmount(ResourceCatalog.FastConnector, 2), new ItemAmount(ResourceCatalog.Plastic, 12) },
                Outputs = new[] { new ItemAmount(ResourceCatalog.Supercomputer, 1) }
            },
            new RecipeDef
            {
                Id = "fabricator.turbo_motor", Name = "Turbomoteur", Machine = MachineCatalog.Fabricator, Seconds = 60f, Tier = 4,
                Inputs = new[] { new ItemAmount(ResourceCatalog.CoolingSystem, 2), new ItemAmount(ResourceCatalog.RadioModule, 2), new ItemAmount(ResourceCatalog.EngineBlock, 1), new ItemAmount(ResourceCatalog.Rubber, 12) },
                Outputs = new[] { new ItemAmount(ResourceCatalog.TurboMotor, 1) }
            },
            // ---- Particle accelerator
            new RecipeDef
            {
                Id = "accelerator.matter", Name = "Matière stellaire condensée", Machine = MachineCatalog.Accelerator, Seconds = 30f, Tier = 4,
                Inputs = new[] { new ItemAmount(ResourceCatalog.StellarDust, 40), new ItemAmount(ResourceCatalog.QuartzCrystal, 4) },
                Outputs = new[] { new ItemAmount(ResourceCatalog.StellarMatter, 1) }
            },
            new RecipeDef
            {
                Id = "accelerator.quantum", Name = "Processeur quantique", Machine = MachineCatalog.Accelerator, Seconds = 60f, Tier = 4,
                Inputs = new[] { new ItemAmount(ResourceCatalog.Supercomputer, 1), new ItemAmount(ResourceCatalog.StellarMatter, 2) },
                Outputs = new[] { new ItemAmount(ResourceCatalog.QuantumProcessor, 1) }
            },
            // ---- 0.12
            new RecipeDef
            {
                Id = "assembler.logic", Name = "Module logique", Machine = MachineCatalog.Assembler, Seconds = 8f, Tier = 2,
                Inputs = new[] { new ItemAmount(ResourceCatalog.FineWire, 10), new ItemAmount(ResourceCatalog.CopperSheet, 1) },
                Outputs = new[] { new ItemAmount(ResourceCatalog.LogicModule, 1) }
            },
            new RecipeDef
            {
                Id = "fabricator.fuel_rod", Name = "Barre de combustible stellaire", Machine = MachineCatalog.Fabricator, Seconds = 40f, Tier = 4,
                Inputs = new[] { new ItemAmount(ResourceCatalog.StellarMatter, 2), new ItemAmount(ResourceCatalog.AluminumCasing, 4), new ItemAmount(ResourceCatalog.EncasedBeam, 1) },
                Outputs = new[] { new ItemAmount(ResourceCatalog.StellarFuelRod, 1) }
            },
            new RecipeDef
            {
                Id = "encoder.time_crystal", Name = "Cristal temporel", Machine = MachineCatalog.QuantumEncoder, Seconds = 30f, Tier = 4,
                Inputs = new[] { new ItemAmount(ResourceCatalog.StellarMatter, 2), new ItemAmount(ResourceCatalog.QuartzCrystal, 6) },
                Outputs = new[] { new ItemAmount(ResourceCatalog.TimeCrystal, 1) }
            },
            new RecipeDef
            {
                Id = "encoder.neuro", Name = "Processeur neuro-quantique", Machine = MachineCatalog.QuantumEncoder, Seconds = 60f, Tier = 4,
                Inputs = new[] { new ItemAmount(ResourceCatalog.QuantumProcessor, 1), new ItemAmount(ResourceCatalog.LogicModule, 4), new ItemAmount(ResourceCatalog.TimeCrystal, 1) },
                Outputs = new[] { new ItemAmount(ResourceCatalog.NeuroProcessor, 1) }
            },
            new RecipeDef
            {
                Id = "encoder.waste", Name = "Matière stellaire (déchets)", Machine = MachineCatalog.QuantumEncoder, Seconds = 20f, Tier = 4,
                Inputs = new[] { new ItemAmount(ResourceCatalog.StellarWaste, 10), new ItemAmount(ResourceCatalog.QuartzCrystal, 2) },
                Outputs = new[] { new ItemAmount(ResourceCatalog.StellarMatter, 1) }
            },
            new RecipeDef
            {
                Id = "converter.copper", Name = "Cuivre (fer converti)", Machine = MachineCatalog.Converter, Seconds = 6f, Tier = 3,
                Inputs = new[] { new ItemAmount(GameItems.Iron, 10), new ItemAmount(ResourceCatalog.StellarDust, 1) },
                Outputs = new[] { new ItemAmount(GameItems.Copper, 10) }
            },
            new RecipeDef
            {
                Id = "converter.aluminum", Name = "Aluminium (cuivre converti)", Machine = MachineCatalog.Converter, Seconds = 8f, Tier = 3,
                Inputs = new[] { new ItemAmount(GameItems.Copper, 10), new ItemAmount(ResourceCatalog.StellarDust, 2) },
                Outputs = new[] { new ItemAmount(GameItems.Aluminum, 6) }
            },
            new RecipeDef
            {
                Id = "converter.shard", Name = "Éclats stellaires (poussière convertie)", Machine = MachineCatalog.Converter, Seconds = 10f, Tier = 3,
                Inputs = new[] { new ItemAmount(ResourceCatalog.StellarDust, 10), new ItemAmount(ResourceCatalog.QuartzCrystal, 1) },
                Outputs = new[] { new ItemAmount(GameItems.StellarShard, 2) }
            },
            new RecipeDef
            {
                Id = "exchange.iron", Name = "Fer", Machine = MachineCatalog.ExchangeCounter, Seconds = 2f, Tier = 2,
                Inputs = new[] { new ItemAmount(ResourceCatalog.Voucher, 2) },
                Outputs = new[] { new ItemAmount(GameItems.Iron, 20) }
            },
            new RecipeDef
            {
                Id = "exchange.copper", Name = "Cuivre", Machine = MachineCatalog.ExchangeCounter, Seconds = 2f, Tier = 2,
                Inputs = new[] { new ItemAmount(ResourceCatalog.Voucher, 2) },
                Outputs = new[] { new ItemAmount(GameItems.Copper, 20) }
            },
            new RecipeDef
            {
                Id = "exchange.aluminum", Name = "Aluminium", Machine = MachineCatalog.ExchangeCounter, Seconds = 2f, Tier = 2,
                Inputs = new[] { new ItemAmount(ResourceCatalog.Voucher, 3) },
                Outputs = new[] { new ItemAmount(GameItems.Aluminum, 10) }
            },
            new RecipeDef
            {
                Id = "exchange.shards", Name = "Éclats stellaires", Machine = MachineCatalog.ExchangeCounter, Seconds = 2f, Tier = 2,
                Inputs = new[] { new ItemAmount(ResourceCatalog.Voucher, 5) },
                Outputs = new[] { new ItemAmount(GameItems.StellarShard, 5) }
            },
            new RecipeDef
            {
                Id = "exchange.matter", Name = "Matière stellaire condensée", Machine = MachineCatalog.ExchangeCounter, Seconds = 4f, Tier = 3,
                Inputs = new[] { new ItemAmount(ResourceCatalog.Voucher, 60) },
                Outputs = new[] { new ItemAmount(ResourceCatalog.StellarMatter, 1) }
            },

            // ---- 0.13: deposit scanners and their modules, each after its research
            new RecipeDef
            {
                Id = "workshop.scanner1", Name = "Scanner de gisements Mk1", Machine = MachineCatalog.Workshop, Seconds = 20f, Tier = 0, Tech = ScanTech1,
                Inputs = new[] { new ItemAmount(ResourceCatalog.IronPlate, 4), new ItemAmount(ResourceCatalog.CopperWire, 6) },
                Outputs = new[] { new ItemAmount(ResourceCatalog.ScannerMk1, 1) }
            },
            new RecipeDef
            {
                Id = "assembler.scanner2", Name = "Scanner de gisements Mk2", Machine = MachineCatalog.Assembler, Seconds = 30f, Tier = 2, Tech = ScanTech2,
                Inputs = new[] { new ItemAmount(ResourceCatalog.ScannerMk1, 1), new ItemAmount(ResourceCatalog.CircuitBoard, 2) },
                Outputs = new[] { new ItemAmount(ResourceCatalog.ScannerMk2, 1) }
            },
            new RecipeDef
            {
                Id = "assembler.range_module", Name = "Module de portée", Machine = MachineCatalog.Assembler, Seconds = 20f, Tier = 2, Tech = ScanTech2,
                Inputs = new[] { new ItemAmount(ResourceCatalog.CircuitBoard, 1), new ItemAmount(ResourceCatalog.CopperSheet, 2) },
                Outputs = new[] { new ItemAmount(ResourceCatalog.RangeModule, 1) }
            },
            new RecipeDef
            {
                Id = "fabricator.scanner3", Name = "Scanner de gisements Mk3", Machine = MachineCatalog.Fabricator, Seconds = 40f, Tier = 3, Tech = ScanTech3,
                Inputs = new[] { new ItemAmount(ResourceCatalog.ScannerMk2, 1), new ItemAmount(ResourceCatalog.Computer, 1), new ItemAmount(ResourceCatalog.QuartzCrystal, 2) },
                Outputs = new[] { new ItemAmount(ResourceCatalog.ScannerMk3, 1) }
            },
            new RecipeDef
            {
                Id = "fabricator.analysis_module", Name = "Module d'analyse", Machine = MachineCatalog.Fabricator, Seconds = 30f, Tier = 3, Tech = ScanTech3,
                Inputs = new[] { new ItemAmount(ResourceCatalog.Computer, 1), new ItemAmount(ResourceCatalog.QuartzCrystal, 4), new ItemAmount(ResourceCatalog.FineWire, 6) },
                Outputs = new[] { new ItemAmount(ResourceCatalog.AnalysisModule, 1) }
            },
            // ---- 0.15: better modules
            new RecipeDef
            {
                Id = "fabricator.range_module2", Name = "Module de portée II", Machine = MachineCatalog.Fabricator, Seconds = 30f, Tier = 3, Tech = ScanTech3,
                Inputs = new[] { new ItemAmount(ResourceCatalog.RangeModule, 1), new ItemAmount(ResourceCatalog.Oscillator, 1), new ItemAmount(ResourceCatalog.FineWire, 10) },
                Outputs = new[] { new ItemAmount(ResourceCatalog.RangeModule2, 1) }
            },
            new RecipeDef
            {
                Id = "accelerator.range_module3", Name = "Module de portée III", Machine = MachineCatalog.Accelerator, Seconds = 40f, Tier = 4, Tech = ScanTech4,
                Inputs = new[] { new ItemAmount(ResourceCatalog.RangeModule2, 1), new ItemAmount(ResourceCatalog.RadioModule, 1), new ItemAmount(ResourceCatalog.StellarMatter, 1) },
                Outputs = new[] { new ItemAmount(ResourceCatalog.RangeModule3, 1) }
            },
            new RecipeDef
            {
                Id = "accelerator.analysis_module2", Name = "Module d'analyse II", Machine = MachineCatalog.Accelerator, Seconds = 40f, Tier = 4, Tech = ScanTech4,
                Inputs = new[] { new ItemAmount(ResourceCatalog.AnalysisModule, 1), new ItemAmount(ResourceCatalog.QuantumProcessor, 1) },
                Outputs = new[] { new ItemAmount(ResourceCatalog.AnalysisModule2, 1) }
            }
        };

        /// <summary>The prospecting techs (see Parts.IndustryResearch) that unlock the scanner recipes.</summary>
        public const ushort ScanTech1 = 7821, ScanTech2 = 7822, ScanTech3 = 7823, ScanTech4 = 7824;

        /// <summary>Whether one of the mod's techs is researched (set by the mod; everything is when nothing is set).</summary>
        public static Func<ushort, bool> TechDone = _ => true;

        /// <summary>The recipes a machine may run now: <see cref="ForMachine"/> without those still waiting for their research.</summary>
        public static RecipeDef[] Available(ushort machine)
        {
            RecipeDef[] all = ForMachine(machine);
            // One bit per gated recipe that is unlocked: the filtered list is only rebuilt when that changes
            int key = 0;
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i].Tech != 0 && TechDone(all[i].Tech))
                    key |= 1 << (i % 31);
                else if (all[i].Tech != 0)
                    key |= 1 << 31;
            }
            if ((key & (1 << 31)) == 0)
                return all;
            if (AvailableCache.TryGetValue(machine, out var cached) && cached.Key == key)
                return cached.Recipes;
            RecipeDef[] open = all.Where(r => r.Tech == 0 || TechDone(r.Tech)).ToArray();
            AvailableCache[machine] = (key, open);
            return open;
        }

        private static readonly Dictionary<ushort, (int Key, RecipeDef[] Recipes)> AvailableCache = new Dictionary<ushort, (int, RecipeDef[])>();

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

        /// <summary>
        /// The recycler's work on one item as a recipe: as many of the item as make at least one
        /// voucher (100 points of value), into vouchers. Made on demand, never listed in <see cref="All"/>.
        /// </summary>
        public static RecipeDef Recycling(ushort machine, uint item, string itemName)
        {
            string id = "sink." + machine + "." + item;
            if (!Extractions.TryGetValue(id, out RecipeDef recipe))
            {
                float value = ItemValues.Of(item);
                int count = Math.Max(1, (int)Math.Ceiling(ItemValues.PointsPerVoucher / Math.Max(0.01f, value)));
                int vouchers = Math.Max(1, (int)Math.Floor(value * count / ItemValues.PointsPerVoucher));
                recipe = new RecipeDef
                {
                    Id = id, Name = "Recyclage : " + itemName, Machine = machine, Seconds = 2f,
                    Inputs = new[] { new ItemAmount(item, count) }, Outputs = new[] { new ItemAmount(ResourceCatalog.Voucher, vouchers) }
                };
                Extractions[id] = recipe;
            }
            return recipe;
        }

        private static readonly Dictionary<string, RecipeDef> Extractions = new Dictionary<string, RecipeDef>();

        /// <summary>
        /// An extractor's work as a recipe without inputs: one item of its deposit per cycle. Made on
        /// demand (deposits are found in the world), never listed in <see cref="All"/>.
        /// </summary>
        public static RecipeDef Extraction(ushort machine, uint item, float seconds, string itemName, string variant = null)
        {
            string id = "extract." + machine + "." + item + (variant != null ? "." + variant : "");
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
