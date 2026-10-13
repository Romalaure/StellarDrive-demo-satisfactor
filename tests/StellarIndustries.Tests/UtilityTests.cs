using System.Linq;
using StellarIndustries.Content;
using StellarIndustries.Core;
using StellarIndustries.Logistics;
using StellarIndustries.Production;
using StellarIndustries.Recipes;
using Xunit;

namespace StellarIndustries.Tests
{
    /// <summary>The 0.12 machines: values and recycling, reactor waste, conveyor rules.</summary>
    public class UtilityTests
    {
        private static int Max100(uint item) => 100;
        private static MachineDef Def(ushort id) { MachineCatalog.TryGet(id, out MachineDef d); return d; }

        [Fact]
        public void ValuesGrowAlongTheChain()
        {
            Assert.Equal(1f, ItemValues.Of(GameItems.Iron));
            Assert.Equal(0f, ItemValues.Of(ResourceCatalog.Voucher));
            float ingot = ItemValues.Of(ResourceCatalog.IronIngot);
            Assert.True(ingot > 1f);
            Assert.True(ItemValues.Of(ResourceCatalog.QuantumProcessor) > ingot);
        }

        [Fact]
        public void RecyclingGivesVouchersAndIsCached()
        {
            RecipeDef a = RecipeBook.Recycling(MachineCatalog.Recycler, GameItems.Iron, "Fer");
            Assert.Same(a, RecipeBook.Recycling(MachineCatalog.Recycler, GameItems.Iron, "Fer"));
            Assert.Equal(100, a.Inputs[0].Count);
            Assert.Equal(ResourceCatalog.Voucher, a.Outputs[0].Item);
            Assert.Equal(1, a.Outputs[0].Count);
            Assert.DoesNotContain(RecipeBook.All, r => r.Id == a.Id);
            RecipeDef q = RecipeBook.Recycling(MachineCatalog.Recycler, ResourceCatalog.QuantumProcessor, "Q");
            Assert.True(q.Inputs[0].Count < 100);
            Assert.True(q.Outputs[0].Count >= 1);
        }

        [Fact]
        public void ReactorBurnsRodsAndLeavesWaste()
        {
            MachineDef reactor = Def(MachineCatalog.StellarReactor);
            Assert.True(MachineCatalog.FuelEnergy(reactor, ResourceCatalog.StellarFuelRod) > 0f);
            Assert.Equal(0f, MachineCatalog.FuelEnergy(reactor, GameItems.Wood));
            Assert.Equal(ResourceCatalog.StellarWaste, reactor.WasteItem);

            var slots = new Stack[3];
            slots[0] = new Stack(ResourceCatalog.StellarFuelRod, 3);
            float rod = MachineCatalog.FuelEnergy(reactor, ResourceCatalog.StellarFuelRod);
            float burned = rod * 2.5f;
            int burnt = FuelBurner.Consume(slots, ref burned, i => MachineCatalog.FuelEnergy(reactor, i), out bool changed);
            Assert.True(changed);
            Assert.Equal(2, burnt);
            Assert.Equal(1, slots[0].Count);
            Assert.Equal(2, FuelBurner.Add(slots, ResourceCatalog.StellarWaste, burnt, 100));
            Assert.Equal(2, Crafting.CountOf(slots, ResourceCatalog.StellarWaste));
            Assert.True(FuelBurner.HasRoom(slots, ResourceCatalog.StellarWaste, 100));
            slots[1] = new Stack(GameItems.Iron, 1);
            slots[2] = new Stack(ResourceCatalog.StellarWaste, 100);
            slots[0] = new Stack(ResourceCatalog.StellarFuelRod, 1);
            Assert.False(FuelBurner.HasRoom(slots, ResourceCatalog.StellarWaste, 100));
            Assert.Equal(0, FuelBurner.Add(slots, ResourceCatalog.StellarWaste, 1, 100));
        }

        [Fact]
        public void NewMachinesConveyorRules()
        {
            var empty = new Stack[10];
            MachineDef reactor = Def(MachineCatalog.StellarReactor);
            Assert.True(ConveyorRules.CanAccept(reactor, new RecipeDef[0], null, empty, ResourceCatalog.StellarFuelRod, Max100));
            Assert.False(ConveyorRules.CanAccept(reactor, new RecipeDef[0], null, empty, GameItems.Wood, Max100));
            Assert.True(ConveyorRules.CanSend(reactor, new RecipeDef[0], null, ResourceCatalog.StellarWaste));
            Assert.False(ConveyorRules.CanSend(reactor, new RecipeDef[0], null, ResourceCatalog.StellarFuelRod));

            MachineDef recycler = Def(MachineCatalog.Recycler);
            Assert.True(ConveyorRules.CanAccept(recycler, new RecipeDef[0], null, empty, GameItems.Iron, Max100));
            Assert.False(ConveyorRules.CanAccept(recycler, new RecipeDef[0], null, empty, ResourceCatalog.Voucher, Max100));
            Assert.True(ConveyorRules.CanSend(recycler, new RecipeDef[0], null, ResourceCatalog.Voucher));
            Assert.False(ConveyorRules.CanSend(recycler, new RecipeDef[0], null, GameItems.Iron));

            MachineDef condenser = Def(MachineCatalog.WaterCondenser);
            Assert.False(ConveyorRules.CanAccept(condenser, new RecipeDef[0], null, empty, GameItems.Iron, Max100));
            Assert.True(ConveyorRules.CanSend(condenser, new RecipeDef[0], null, condenser.FixedOutput));

            MachineDef radar = Def(MachineCatalog.RadarTower);
            Assert.False(ConveyorRules.CanAccept(radar, new RecipeDef[0], null, empty, GameItems.Iron, Max100));
            Assert.False(ConveyorRules.CanSend(radar, new RecipeDef[0], null, GameItems.Iron));

            MachineDef burner = Def(MachineCatalog.BiomassBurner);
            Assert.True(ConveyorRules.CanAccept(burner, new RecipeDef[0], null, empty, GameItems.Wood, Max100));
        }

        [Fact]
        public void NewRecipesUseTheirMachines()
        {
            foreach (string id in new[] { "encoder.time_crystal", "encoder.neuro", "converter.copper", "exchange.iron", "fabricator.fuel_rod", "assembler.logic" })
                Assert.NotNull(RecipeBook.Get(id));
            Assert.All(RecipeBook.ForMachine(MachineCatalog.ExchangeCounter), r => Assert.Contains(r.Inputs, i => i.Item == ResourceCatalog.Voucher));
        }
    }
}
