using StellarIndustries.Content;
using StellarIndustries.Core;
using StellarIndustries.Logistics;
using StellarIndustries.Production;
using StellarIndustries.Recipes;
using Xunit;

namespace StellarIndustries.Tests
{
    public class ConveyorTests
    {
        private static int Max100(uint item) => 100;
        private static MachineDef Def(ushort id) { MachineCatalog.TryGet(id, out MachineDef d); return d; }
        private static readonly MachineDef Smelter = Def(MachineCatalog.Smelter);
        private static readonly MachineDef Workshop = Def(MachineCatalog.Workshop);
        private static readonly MachineDef Extractor = Def(MachineCatalog.Extractor);

        [Fact]
        public void MachinesSendProductsOnly()
        {
            RecipeDef[] recipes = RecipeBook.ForMachine(MachineCatalog.Smelter);
            Assert.True(ConveyorRules.CanSend(Smelter, recipes, null, ResourceCatalog.IronIngot));
            Assert.True(ConveyorRules.CanSend(Smelter, recipes, null, GameItems.Glass));
            Assert.False(ConveyorRules.CanSend(Smelter, recipes, null, GameItems.Iron));
            Assert.False(ConveyorRules.CanSend(Smelter, recipes, null, GameItems.Wood));
        }

        [Fact]
        public void TheRunningRecipesIngredientIsKeptEvenIfItIsAlsoAProduct()
        {
            // The smelter makes glass from dust; glass is an output, never an input here: use the workshop's ingot instead
            RecipeDef[] recipes = RecipeBook.ForMachine(MachineCatalog.Workshop);
            var fake = new RecipeDef { Id = "t", Machine = MachineCatalog.Workshop, Seconds = 1f, Inputs = new[] { new ItemAmount(ResourceCatalog.IronPlate, 1) }, Outputs = new[] { new ItemAmount(ResourceCatalog.IronRod, 1) } };
            Assert.True(ConveyorRules.CanSend(Workshop, recipes, null, ResourceCatalog.IronPlate));
            Assert.False(ConveyorRules.CanSend(Workshop, recipes, fake, ResourceCatalog.IronPlate));
        }

        [Fact]
        public void ExtractorsSendEverythingAndTakeNothing()
        {
            Assert.True(ConveyorRules.CanSend(Extractor, new RecipeDef[0], null, GameItems.Copper));
            Assert.False(ConveyorRules.CanAccept(Extractor, new RecipeDef[0], null, new Stack[10], GameItems.Copper, Max100));
        }

        [Fact]
        public void MachinesTakeIngredientsUpToAFewCycles()
        {
            RecipeDef[] recipes = RecipeBook.ForMachine(MachineCatalog.Workshop);
            var slots = new Stack[10];
            Assert.True(ConveyorRules.CanAccept(Workshop, recipes, null, slots, ResourceCatalog.IronIngot, Max100));
            Assert.False(ConveyorRules.CanAccept(Workshop, recipes, null, slots, ResourceCatalog.Screws, Max100));
            // Plates need 3 ingots: 12 is enough
            slots[0] = new Stack(ResourceCatalog.IronIngot, 11);
            Assert.True(ConveyorRules.CanAccept(Workshop, recipes, null, slots, ResourceCatalog.IronIngot, Max100));
            slots[0] = new Stack(ResourceCatalog.IronIngot, 12);
            Assert.False(ConveyorRules.CanAccept(Workshop, recipes, null, slots, ResourceCatalog.IronIngot, Max100));
        }

        [Fact]
        public void ChosenRecipeLimitsWhatIsTaken()
        {
            RecipeDef[] recipes = RecipeBook.ForMachine(MachineCatalog.Workshop);
            RecipeDef wire = RecipeBook.Get("workshop.copper_wire");
            Assert.False(ConveyorRules.CanAccept(Workshop, recipes, wire, new Stack[10], ResourceCatalog.IronIngot, Max100));
            Assert.True(ConveyorRules.CanAccept(Workshop, recipes, wire, new Stack[10], ResourceCatalog.CopperIngot, Max100));
        }

        [Fact]
        public void TwoSlotsStayFreeForProducts()
        {
            RecipeDef[] recipes = RecipeBook.ForMachine(MachineCatalog.Workshop);
            var slots = new Stack[10];
            for (int i = 0; i < 8; i++)
                slots[i] = new Stack(GameItems.Wood, 1);
            Assert.False(ConveyorRules.CanAccept(Workshop, recipes, null, slots, ResourceCatalog.IronIngot, Max100));
            slots[7] = Stack.Empty;
            Assert.True(ConveyorRules.CanAccept(Workshop, recipes, null, slots, ResourceCatalog.IronIngot, Max100));
        }
    }
}
