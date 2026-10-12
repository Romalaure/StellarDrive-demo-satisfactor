using StellarIndustries.Content;
using StellarIndustries.Core;
using StellarIndustries.Logistics;
using StellarIndustries.Production;
using StellarIndustries.Recipes;
using Xunit;

namespace StellarIndustries.Tests
{
    public class PowerTests
    {
        private static float Energy(uint item) => MachineCatalog.FuelEnergy(item);

        [Fact]
        public void FuelValues()
        {
            Assert.Equal(40000f, Energy(GameItems.Wood));
            Assert.Equal(5 * Energy(GameItems.Wood), Energy(ResourceCatalog.BiomassBrick));
            Assert.Equal(0f, Energy(GameItems.Iron));
            // 4 logs make a brick worth 5 logs
            RecipeDef brick = RecipeBook.Get("press.briquette");
            Assert.Equal(4, brick.Inputs[0].Count);
        }

        [Fact]
        public void AnItemLeavesOnlyOnceFullyBurnt()
        {
            var slots = new Stack[10];
            slots[0] = new Stack(GameItems.Iron, 3);
            slots[1] = new Stack(GameItems.Wood, 2);
            float burned = 0f;
            Assert.Equal(80000f, FuelBurner.Available(slots, burned, Energy));
            burned = 39999f;
            Assert.False(FuelBurner.Consume(slots, ref burned, Energy));
            Assert.Equal(2, slots[1].Count);
            Assert.Equal(40001f, FuelBurner.Available(slots, burned, Energy));
            burned = 50000f;
            Assert.True(FuelBurner.Consume(slots, ref burned, Energy));
            Assert.Equal(1, slots[1].Count);
            Assert.Equal(10000f, burned);
            Assert.Equal(3, slots[0].Count);
        }

        [Fact]
        public void EnergyIsConservedOverManyTicks()
        {
            var slots = new Stack[10];
            slots[0] = new Stack(ResourceCatalog.BiomassBrick, 3);
            slots[1] = new Stack(GameItems.Wood, 5);
            float total = FuelBurner.Available(slots, 0f, Energy);
            float burned = 0f, given = 0f;
            // 6 kW for 0.02 s ticks until empty
            for (int i = 0; i < 100000; i++)
            {
                float offer = System.Math.Min(6000f * 0.02f, FuelBurner.Available(slots, burned, Energy));
                if (offer <= 0f)
                    break;
                given += offer;
                burned += offer;
                FuelBurner.Consume(slots, ref burned, Energy);
            }
            Assert.Equal(total, given, 0);
            Assert.All(slots, s => Assert.True(s.IsEmpty));
        }

        [Fact]
        public void GeneratorsTakeFuelOnlyAndSendNothing()
        {
            MachineCatalog.TryGet(MachineCatalog.CombustionGenerator, out MachineDef gen);
            var slots = new Stack[10];
            Assert.True(ConveyorRules.CanAccept(gen, new RecipeDef[0], null, slots, GameItems.Wood, i => 100));
            Assert.True(ConveyorRules.CanAccept(gen, new RecipeDef[0], null, slots, ResourceCatalog.BiomassBrick, i => 100));
            Assert.False(ConveyorRules.CanAccept(gen, new RecipeDef[0], null, slots, GameItems.Iron, i => 100));
            slots[0] = new Stack(GameItems.Wood, 20);
            Assert.False(ConveyorRules.CanAccept(gen, new RecipeDef[0], null, slots, GameItems.Wood, i => 100));
            Assert.False(ConveyorRules.CanSend(gen, new RecipeDef[0], null, GameItems.Wood));
        }
    }
}
