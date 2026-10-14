using System.Linq;
using StellarIndustries.Content;
using StellarIndustries.Core;
using StellarIndustries.Logistics;
using StellarIndustries.Production;
using StellarIndustries.Recipes;
using Xunit;

namespace StellarIndustries.Tests
{
    /// <summary>The 0.13 content: conveyors, deposit purity, scanner recipes behind their research, machine roles.</summary>
    public class LogisticsTests
    {
        private static int Max100(uint item) => 100;
        private static MachineDef Def(ushort id) { MachineCatalog.TryGet(id, out MachineDef d); return d; }

        [Fact]
        public void ConveyorsAreLogisticsWithoutPowerOrRecipes()
        {
            var conveyors = MachineCatalog.All.Where(m => m.IsConveyor).ToList();
            Assert.Equal(6, conveyors.Count);
            foreach (MachineDef c in conveyors)
            {
                Assert.True(c.IsLogistics);
                Assert.True(c.HasNoRecipe);
                Assert.Equal(0f, c.Watts);
                Assert.True(c.ConveyorRate > 0f);
                Assert.True(c.ConveyorHold > 0);
                Assert.Empty(RecipeBook.ForMachine(c.PartId));
            }
            Assert.True(Def(MachineCatalog.BeltMk1).ConveyorRate < Def(MachineCatalog.BeltMk2).ConveyorRate);
            Assert.True(Def(MachineCatalog.BeltMk2).ConveyorRate < Def(MachineCatalog.BeltMk3).ConveyorRate);
            Assert.Equal(ConveyorKind.Splitter, Def(MachineCatalog.Splitter).Conveyor);
            Assert.Equal(ConveyorKind.Merger, Def(MachineCatalog.Merger).Conveyor);
            Assert.Equal(ConveyorKind.Lift, Def(MachineCatalog.ConveyorLift).Conveyor);
        }

        [Fact]
        public void ConveyorsCarryAnythingButMachinesKeepTheirRules()
        {
            var empty = new Stack[10];
            MachineDef belt = Def(MachineCatalog.BeltMk1);
            Assert.True(ConveyorRules.CanAccept(belt, new RecipeDef[0], null, empty, GameItems.Iron, Max100));
            Assert.True(ConveyorRules.CanSend(belt, new RecipeDef[0], null, ResourceCatalog.Screws));
            // A smelter fed by a belt still only takes ore it can melt, and only gives ingots
            MachineDef smelter = Def(MachineCatalog.Smelter);
            Assert.True(ConveyorRules.CanAccept(smelter, RecipeBook.ForMachine(smelter.PartId), null, empty, GameItems.Iron, Max100));
            Assert.False(ConveyorRules.CanAccept(smelter, RecipeBook.ForMachine(smelter.PartId), null, empty, ResourceCatalog.Screws, Max100));
            Assert.True(ConveyorRules.CanSend(smelter, RecipeBook.ForMachine(smelter.PartId), null, ResourceCatalog.IronIngot));
            Assert.False(ConveyorRules.CanSend(smelter, RecipeBook.ForMachine(smelter.PartId), null, GameItems.Iron));
        }

        [Fact]
        public void PurityIsStableAndRoughlyThreeFiveTwo()
        {
            Assert.Equal(DepositPurity.Of(GameItems.Iron, 10, 4, -7), DepositPurity.Of(GameItems.Iron, 10, 4, -7));
            int[] counts = new int[3];
            for (int x = -40; x < 40; x++)
            {
                for (int z = -40; z < 40; z++)
                    counts[(int)DepositPurity.Of(GameItems.Copper, x, 3, z)]++;
            }
            float total = counts.Sum();
            Assert.InRange(counts[(int)Purity.Impure] / total, 0.25f, 0.35f);
            Assert.InRange(counts[(int)Purity.Normal] / total, 0.45f, 0.55f);
            Assert.InRange(counts[(int)Purity.Pure] / total, 0.15f, 0.25f);
            Assert.Equal(0.5f, DepositPurity.Factor(Purity.Impure));
            Assert.Equal(1f, DepositPurity.Factor(Purity.Normal));
            Assert.Equal(2f, DepositPurity.Factor(Purity.Pure));
        }

        [Fact]
        public void PurityChangesTheExtractionRecipe()
        {
            RecipeDef slow = RecipeBook.Extraction(MachineCatalog.Extractor, GameItems.Iron, 12f, "Fer", Purity.Impure.ToString());
            RecipeDef fast = RecipeBook.Extraction(MachineCatalog.Extractor, GameItems.Iron, 3f, "Fer", Purity.Pure.ToString());
            Assert.NotEqual(slow.Id, fast.Id);
            Assert.Equal(12f, slow.Seconds);
            Assert.Equal(3f, fast.Seconds);
            Assert.Same(fast, RecipeBook.Get(fast.Id));
        }

        [Fact]
        public void ScannerRecipesWaitForTheirResearch()
        {
            System.Func<ushort, bool> before = RecipeBook.TechDone;
            try
            {
                RecipeBook.TechDone = _ => false;
                Assert.DoesNotContain(RecipeBook.Available(MachineCatalog.Workshop), r => r.Id == "workshop.scanner1");
                Assert.Contains(RecipeBook.Available(MachineCatalog.Workshop), r => r.Id == "workshop.iron_plate");
                Assert.DoesNotContain(RecipeBook.Available(MachineCatalog.Fabricator), r => r.Id == "fabricator.scanner3");
                RecipeBook.TechDone = id => id == RecipeBook.ScanTech1;
                Assert.Contains(RecipeBook.Available(MachineCatalog.Workshop), r => r.Id == "workshop.scanner1");
                Assert.DoesNotContain(RecipeBook.Available(MachineCatalog.Assembler), r => r.Id == "assembler.scanner2");
                RecipeBook.TechDone = _ => true;
                Assert.Equal(RecipeBook.ForMachine(MachineCatalog.Fabricator).Length, RecipeBook.Available(MachineCatalog.Fabricator).Length);
            }
            finally
            {
                RecipeBook.TechDone = before;
            }
            // Each scanner is made from the one before it
            Assert.Contains(RecipeBook.Get("assembler.scanner2").Inputs, i => i.Item == ResourceCatalog.ScannerMk1);
            Assert.Contains(RecipeBook.Get("fabricator.scanner3").Inputs, i => i.Item == ResourceCatalog.ScannerMk2);
        }

        [Fact]
        public void EveryMachineSaysWhatItIsFor()
        {
            Assert.All(MachineCatalog.All, m => Assert.False(string.IsNullOrWhiteSpace(m.Role), m.Name));
        }
    }
}
