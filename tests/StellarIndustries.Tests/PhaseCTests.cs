using System.Linq;
using StellarIndustries.Content;
using StellarIndustries.Core;
using StellarIndustries.Production;
using StellarIndustries.Recipes;
using Xunit;

namespace StellarIndustries.Tests
{
    public class RecipeChoiceTests
    {
        private static int Max100(uint item) => 100;
        private static readonly RecipeDef[] Workshop = RecipeBook.ForMachine(MachineCatalog.Workshop);

        private static Stack[] Slots(params Stack[] first)
        {
            var slots = new Stack[10];
            for (int i = 0; i < first.Length; i++)
                slots[i] = first[i];
            return slots;
        }

        [Fact]
        public void ChosenRecipeIsTheOnlyOneRun()
        {
            // Ingots could make plates (first in the list), but the player chose rods
            Stack[] slots = Slots(new Stack(ResourceCatalog.IronIngot, 6));
            RecipeDef rod = RecipeBook.Get("workshop.iron_rod");
            Assert.Same(RecipeBook.Get("workshop.iron_plate"), RecipeSelector.Pick(slots, Workshop, null, null, Max100, out _));
            Assert.Same(rod, RecipeSelector.Pick(slots, Workshop, null, rod, Max100, out MachineStatus status));
            Assert.Equal(MachineStatus.Producing, status);
        }

        [Fact]
        public void ChosenRecipeWithoutItsInputsWaits()
        {
            // Iron ore could run the raw plate recipe, but the machine is set to copper wire
            Stack[] slots = Slots(new Stack(GameItems.Iron, 10));
            RecipeDef wire = RecipeBook.Get("workshop.copper_wire");
            Assert.Null(RecipeSelector.Pick(slots, Workshop, null, wire, Max100, out MachineStatus status));
            Assert.Equal(MachineStatus.WaitingInputs, status);
        }

        [Fact]
        public void ChoiceOfAnotherMachineIsIgnored()
        {
            Stack[] slots = Slots(new Stack(GameItems.Iron, 10));
            RecipeDef smelt = RecipeBook.Get("smelter.iron_ingot");
            Assert.Same(RecipeBook.Get("workshop.iron_plate_raw"), RecipeSelector.Pick(slots, Workshop, null, smelt, Max100, out _));
        }

        [Fact]
        public void CyclingGoesThroughAutomaticAndEveryRecipe()
        {
            RecipeDef choice = null;
            var seen = new System.Collections.Generic.List<RecipeDef>();
            for (int i = 0; i <= Workshop.Length; i++)
            {
                choice = RecipeSelector.Next(Workshop, choice);
                seen.Add(choice);
            }
            Assert.Equal(Workshop, seen.Take(Workshop.Length));
            Assert.Null(seen.Last());
            // Backwards from automatic is the last recipe
            Assert.Same(Workshop.Last(), RecipeSelector.Next(Workshop, null, -1));
            Assert.Null(RecipeSelector.Next(Workshop, Workshop[0], -1));
        }
    }

    public class CompanionFileTests
    {
        [Fact]
        public void RoundTrip()
        {
            var a = new MachineChoices();
            a.Set(12, 3, "workshop.iron_rod");
            a.Set(4000000000, 65000, "press.screws");
            var b = new MachineChoices();
            Assert.Equal(0, b.Parse(a.Serialize()));
            Assert.Equal("workshop.iron_rod", b.Get(12, 3));
            Assert.Equal("press.screws", b.Get(4000000000, 65000));
            Assert.Equal(2, b.Count);
        }

        [Fact]
        public void AutomaticRemovesTheLine()
        {
            var c = new MachineChoices();
            c.Set(1, 2, "x");
            c.Changed = false;
            c.Set(1, 2, null);
            Assert.True(c.Changed);
            Assert.Null(c.Get(1, 2));
            Assert.DoesNotContain("1 2", c.Serialize());
        }

        [Fact]
        public void BadLinesAreSkipped()
        {
            var c = new MachineChoices();
            int bad = c.Parse("# comment\n\n1 2 ok\nnot a line\n-1 2 x\n3 99999 y\n5 6 a b\r\n7 8 fine\r\n");
            Assert.Equal(4, bad);
            Assert.Equal("ok", c.Get(1, 2));
            Assert.Equal("fine", c.Get(7, 8));
            Assert.Equal(2, c.Count);
        }

        [Fact]
        public void PruneKeepsUnknownShips()
        {
            var c = new MachineChoices();
            c.Set(1, 1, "a");
            c.Set(2, 1, "b");
            c.Set(3, 1, "c");
            // ship 1: machine still there; ship 2: machine gone; ship 3: not loaded (unknown)
            int pruned = c.Prune((ship, part) => ship == 1 ? true : ship == 2 ? false : (bool?)null);
            Assert.Equal(1, pruned);
            Assert.Equal("a", c.Get(1, 1));
            Assert.Null(c.Get(2, 1));
            Assert.Equal("c", c.Get(3, 1));
        }

        [Fact]
        public void ReportRoundTrip()
        {
            var r = new MachineReport { Status = MachineStatus.Underpowered, RecipeId = "smelter.glass", ChosenId = null, Progress = 0.42f, Watts = 400f, PowerRate = 0.5f, Detail = "a|b" };
            Assert.True(MachineReport.TryDecode(r.Encode(), out MachineReport back));
            Assert.Equal(MachineStatus.Underpowered, back.Status);
            Assert.Equal("smelter.glass", back.RecipeId);
            Assert.Null(back.ChosenId);
            Assert.Equal(0.42f, back.Progress, 3);
            Assert.Equal(400f, back.Watts, 1);
            Assert.Equal(0.5f, back.PowerRate, 3);
            Assert.Equal("a/b", back.Detail);
            Assert.False(MachineReport.TryDecode("garbage", out _));
        }
    }

    public class ProductionChainTests
    {
        private static int MaxOf(uint item) => ResourceCatalog.Get(item)?.MaxStack ?? 50;

        /// <summary>Runs a machine's chosen recipe until it stops; returns the cycles done.</summary>
        private static int RunAll(ref Stack[] slots, string recipeId)
        {
            RecipeDef recipe = RecipeBook.Get(recipeId);
            int cycles = 0;
            while (Crafting.TryCraft(slots, recipe, MaxOf, out Stack[] after) == CraftResult.Ok)
            {
                slots = after;
                cycles++;
            }
            return cycles;
        }

        private static Stack[] Holding(uint item, int count)
        {
            var slots = new Stack[10];
            slots[0] = new Stack(item, count);
            return slots;
        }

        [Fact]
        public void CrushingGivesMoreIngotsThanSmeltingOre()
        {
            Stack[] direct = Holding(GameItems.Iron, 30);
            RunAll(ref direct, "smelter.iron_ingot");
            Stack[] crushed = Holding(GameItems.Iron, 30);
            RunAll(ref crushed, "crusher.iron");
            Assert.Equal(60, Crafting.CountOf(crushed, ResourceCatalog.CrushedIron));
            RunAll(ref crushed, "smelter.crushed_iron");
            Assert.Equal(30, Crafting.CountOf(direct, ResourceCatalog.IronIngot));
            Assert.Equal(40, Crafting.CountOf(crushed, ResourceCatalog.IronIngot));
        }

        [Fact]
        public void ReinforcedPlateFromOre()
        {
            // 24 iron: 24 ingots -> 6 rods (6 ingots) + 12 plates (18 ingots); 6 rods -> 24 screws
            Stack[] smelter = Holding(GameItems.Iron, 24);
            Assert.Equal(24, RunAll(ref smelter, "smelter.iron_ingot"));
            Stack[] rods = Holding(ResourceCatalog.IronIngot, 6);
            Assert.Equal(6, RunAll(ref rods, "workshop.iron_rod"));
            Stack[] plates = Holding(ResourceCatalog.IronIngot, 18);
            Assert.Equal(6, RunAll(ref plates, "workshop.iron_plate"));
            Stack[] screws = Holding(ResourceCatalog.IronRod, 6);
            Assert.Equal(6, RunAll(ref screws, "press.screws"));
            var assembler = new Stack[10];
            assembler[0] = new Stack(ResourceCatalog.IronPlate, Crafting.CountOf(plates, ResourceCatalog.IronPlate));
            assembler[1] = new Stack(ResourceCatalog.Screws, Crafting.CountOf(screws, ResourceCatalog.Screws));
            Assert.Equal(3, RunAll(ref assembler, "assembler.reinforced_plate"));
            Assert.Equal(3, Crafting.CountOf(assembler, ResourceCatalog.ReinforcedPlate));
            Assert.Equal(0, Crafting.CountOf(assembler, ResourceCatalog.IronPlate));
        }

        [Fact]
        public void CrusherCoProductIsKept()
        {
            Stack[] slots = Holding(GameItems.StellarShard, 5);
            Assert.Equal(5, RunAll(ref slots, "crusher.shard"));
            Assert.Equal(15, Crafting.CountOf(slots, ResourceCatalog.StellarDust));
            Assert.Equal(5, Crafting.CountOf(slots, ResourceCatalog.Silica));
        }

        [Fact]
        public void EveryMachineHasRecipesAndEveryItemIsMade()
        {
            // Extractors and generators have no recipe
            foreach (MachineDef m in MachineCatalog.All)
                Assert.True(m.HasNoRecipe ^ RecipeBook.ForMachine(m.PartId).Length > 0, m.Name);
            var made = RecipeBook.All.SelectMany(r => r.Outputs).Select(o => o.Item).ToHashSet();
            // Special machines: the reactor leaves waste, the recycler pays vouchers, the condenser makes ice
            foreach (MachineDef m in MachineCatalog.All)
            {
                if (m.WasteItem != 0)
                    made.Add(m.WasteItem);
                if (m.Sinks)
                    made.Add(ResourceCatalog.Voucher);
            }
            foreach (ResourceDef r in ResourceCatalog.All.Where(x => !x.FoundInWorld))
                Assert.Contains(r.Id, made);
        }
    }

    public class ExtractionTests
    {
        [Fact]
        public void ExtractionFillsTheMachineOneItemPerCycleThenBlocks()
        {
            RecipeDef dig = RecipeBook.Extraction(MachineCatalog.Extractor, GameItems.Iron, 6f, "Iron");
            Assert.Same(dig, RecipeBook.Get(dig.Id));
            Assert.Same(dig, RecipeBook.Extraction(MachineCatalog.Extractor, GameItems.Iron, 6f, "Iron"));
            var slots = new Stack[10];
            int cycles = 0;
            while (Crafting.TryCraft(slots, dig, item => 20, out Stack[] after) == CraftResult.Ok)
            {
                slots = after;
                cycles++;
            }
            Assert.Equal(200, cycles);
            Assert.Equal(CraftResult.OutputBlocked, Crafting.Check(slots, dig, item => 20));
            Assert.Null(RecipeSelector.Pick(slots, new[] { dig }, null, item => 20, out MachineStatus status));
            Assert.Equal(MachineStatus.OutputBlocked, status);
        }

        [Fact]
        public void ExtractionIsNotAListedRecipe()
        {
            RecipeBook.Extraction(MachineCatalog.Extractor, GameItems.Copper, 6f, "Copper");
            Assert.DoesNotContain(RecipeBook.All, r => r.Machine == MachineCatalog.Extractor);
            Assert.Empty(RecipeBook.ForMachine(MachineCatalog.Extractor));
        }
    }
}
