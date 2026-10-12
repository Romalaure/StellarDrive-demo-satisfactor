using System.Linq;
using StellarIndustries.Content;
using StellarIndustries.Core;
using StellarIndustries.Production;
using StellarIndustries.Recipes;
using Xunit;

namespace StellarIndustries.Tests
{
    public class CraftingTests
    {
        private static int Max20(uint item) => 20;

        // 2 iron -> 1 plate; 1 copper ingot -> 2 wire
        private static readonly RecipeDef Plate = RecipeBook.Get("workshop.iron_plate_raw");
        private static readonly RecipeDef Wire = RecipeBook.Get("workshop.copper_wire");

        private static Stack[] Slots(params Stack[] first)
        {
            var slots = new Stack[10];
            for (int i = 0; i < first.Length; i++)
                slots[i] = first[i];
            return slots;
        }

        private static int Total(Stack[] slots) => slots.Sum(s => s.Count);

        [Fact]
        public void ValidRecipeConsumesInputsAndGivesOutputs()
        {
            Stack[] slots = Slots(new Stack(GameItems.Iron, 5));
            Assert.Equal(CraftResult.Ok, Crafting.TryCraft(slots, Plate, Max20, out Stack[] after));
            Assert.Equal(3, Crafting.CountOf(after, GameItems.Iron));
            Assert.Equal(1, Crafting.CountOf(after, ResourceCatalog.IronPlate));
            // Outputs go to the last slot
            Assert.Equal(ResourceCatalog.IronPlate, after[9].Item);
        }

        [Fact]
        public void TheInputArrayIsNeverModified()
        {
            Stack[] slots = Slots(new Stack(GameItems.Iron, 2));
            Crafting.TryCraft(slots, Plate, Max20, out _);
            Assert.Equal(2, slots[0].Count);
            Assert.True(slots[9].IsEmpty);
        }

        [Fact]
        public void MissingInputsDoNotRun()
        {
            Stack[] slots = Slots(new Stack(GameItems.Iron, 1));
            Assert.Equal(CraftResult.MissingInputs, Crafting.TryCraft(slots, Plate, Max20, out Stack[] after));
            Assert.Null(after);
        }

        [Fact]
        public void EmptyMachineDoesNotRun()
        {
            Assert.Equal(CraftResult.MissingInputs, Crafting.Check(new Stack[10], Plate, Max20));
        }

        [Fact]
        public void BlockedOutputKeepsInputs()
        {
            // Every slot full of iron that cannot be freed below the stack limit... except the first stack
            var slots = new Stack[10];
            slots[0] = new Stack(GameItems.Iron, 20);
            for (int i = 1; i < 10; i++)
                slots[i] = new Stack(GameItems.Copper, 20);
            Assert.Equal(CraftResult.OutputBlocked, Crafting.TryCraft(slots, Plate, Max20, out Stack[] after));
            Assert.Null(after);
            Assert.Equal(20, slots[0].Count);
        }

        [Fact]
        public void TakingTheLastInputFreesItsSlotForTheOutput()
        {
            var slots = new Stack[10];
            slots[0] = new Stack(GameItems.Iron, 2);
            for (int i = 1; i < 10; i++)
                slots[i] = new Stack(GameItems.Copper, 20);
            Assert.Equal(CraftResult.Ok, Crafting.TryCraft(slots, Plate, Max20, out Stack[] after));
            Assert.Equal(new Stack(ResourceCatalog.IronPlate, 1), after[0]);
        }

        [Fact]
        public void OutputsJoinAnExistingStackFirst()
        {
            Stack[] slots = Slots(new Stack(ResourceCatalog.CopperIngot, 3), new Stack(ResourceCatalog.CopperWire, 5));
            Assert.Equal(CraftResult.Ok, Crafting.TryCraft(slots, Wire, Max20, out Stack[] after));
            Assert.Equal(7, after[1].Count);
            Assert.True(after[9].IsEmpty);
        }

        [Fact]
        public void StackLimitSplitsOutputs()
        {
            Stack[] slots = Slots(new Stack(ResourceCatalog.CopperIngot, 3), new Stack(ResourceCatalog.CopperWire, 19));
            Assert.Equal(CraftResult.Ok, Crafting.TryCraft(slots, Wire, Max20, out Stack[] after));
            Assert.Equal(20, after[1].Count);
            Assert.Equal(new Stack(ResourceCatalog.CopperWire, 1), after[9]);
        }

        [Fact]
        public void QuantitiesStayConsistentOverManyCycles()
        {
            Stack[] slots = Slots(new Stack(GameItems.Iron, 20), new Stack(GameItems.Iron, 20));
            int cycles = 0;
            while (Crafting.TryCraft(slots, Plate, Max20, out Stack[] after) == CraftResult.Ok)
            {
                slots = after;
                cycles++;
            }
            Assert.Equal(20, cycles);
            Assert.Equal(0, Crafting.CountOf(slots, GameItems.Iron));
            Assert.Equal(20, Crafting.CountOf(slots, ResourceCatalog.IronPlate));
            Assert.Equal(20, Total(slots));
        }

        [Fact]
        public void RecipeWithNoOutputIsRejected()
        {
            var bad = new RecipeDef { Id = "x", Machine = MachineCatalog.Workshop, Seconds = 1f, Inputs = new[] { new ItemAmount(GameItems.Iron, 1) }, Outputs = new ItemAmount[0] };
            Assert.Equal(CraftResult.InvalidRecipe, Crafting.Check(Slots(new Stack(GameItems.Iron, 5)), bad, Max20));
        }

        [Fact]
        public void NegativeQuantityIsRejected()
        {
            var bad = new RecipeDef { Id = "x", Machine = MachineCatalog.Workshop, Seconds = 1f, Inputs = new[] { new ItemAmount(GameItems.Iron, -1) }, Outputs = new[] { new ItemAmount(ResourceCatalog.IronPlate, 1) } };
            Assert.Equal(CraftResult.InvalidRecipe, Crafting.Check(Slots(new Stack(GameItems.Iron, 5)), bad, Max20));
        }

        [Fact]
        public void CoProductsAreAllStoredOrNothingRuns()
        {
            var twoOutputs = new RecipeDef
            {
                Id = "x", Machine = MachineCatalog.Workshop, Seconds = 1f,
                Inputs = new[] { new ItemAmount(GameItems.Iron, 1) },
                Outputs = new[] { new ItemAmount(ResourceCatalog.IronPlate, 1), new ItemAmount(ResourceCatalog.CopperWire, 1) }
            };
            // Room for one output only: the main one fits, the co-product does not
            var slots = new Stack[10];
            slots[0] = new Stack(GameItems.Iron, 5);
            for (int i = 1; i < 9; i++)
                slots[i] = new Stack(GameItems.Copper, 20);
            Assert.Equal(CraftResult.OutputBlocked, Crafting.TryCraft(slots, twoOutputs, Max20, out _));
            slots[9] = Stack.Empty;
            slots[8] = Stack.Empty;
            Assert.Equal(CraftResult.Ok, Crafting.TryCraft(slots, twoOutputs, Max20, out Stack[] after));
            Assert.Equal(1, Crafting.CountOf(after, ResourceCatalog.IronPlate));
            Assert.Equal(1, Crafting.CountOf(after, ResourceCatalog.CopperWire));
        }
    }

    public class RecipeSelectorTests
    {
        private static int Max20(uint item) => 20;
        private static readonly RecipeDef[] Recipes = RecipeBook.ForMachine(MachineCatalog.Workshop);

        [Fact]
        public void EmptyMachineIsIdle()
        {
            Assert.Null(RecipeSelector.Pick(new Stack[10], Recipes, null, Max20, out MachineStatus status));
            Assert.Equal(MachineStatus.Idle, status);
        }

        [Fact]
        public void NotEnoughIsWaiting()
        {
            var slots = new Stack[10];
            slots[0] = new Stack(GameItems.Iron, 1);
            Assert.Null(RecipeSelector.Pick(slots, Recipes, null, Max20, out MachineStatus status));
            Assert.Equal(MachineStatus.WaitingInputs, status);
        }

        [Fact]
        public void UnknownItemsAreWaitingNotError()
        {
            var slots = new Stack[10];
            slots[0] = new Stack(GameItems.Wood, 10);
            Assert.Null(RecipeSelector.Pick(slots, Recipes, null, Max20, out MachineStatus status));
            Assert.Equal(MachineStatus.WaitingInputs, status);
        }

        [Fact]
        public void KeepsItsCurrentRecipeWhileItCanRun()
        {
            var slots = new Stack[10];
            slots[0] = new Stack(GameItems.Iron, 10);
            slots[1] = new Stack(GameItems.Copper, 10);
            RecipeDef wire = RecipeBook.Get("workshop.copper_wire_raw");
            Assert.Same(wire, RecipeSelector.Pick(slots, Recipes, wire, Max20, out _));
            Assert.Same(RecipeBook.Get("workshop.iron_plate_raw"), RecipeSelector.Pick(slots, Recipes, null, Max20, out _));
        }

        [Fact]
        public void BlockedOutputIsReported()
        {
            var slots = new Stack[10];
            slots[0] = new Stack(GameItems.Iron, 20);
            for (int i = 1; i < 10; i++)
                slots[i] = new Stack(GameItems.Iron, 20);
            Assert.Null(RecipeSelector.Pick(slots, Recipes, null, Max20, out MachineStatus status));
            Assert.Equal(MachineStatus.OutputBlocked, status);
        }
    }

    public class ValidatorTests
    {
        [Fact]
        public void ShippedContentHasNoError()
        {
            var issues = RecipeValidator.Validate(RecipeBook.All, MachineCatalog.All, ResourceCatalog.All);
            Assert.DoesNotContain(issues, i => i.IsError);
        }

        [Fact]
        public void DetectsBadRecipes()
        {
            var recipes = new[]
            {
                new RecipeDef { Id = "unknown_item", Machine = MachineCatalog.Workshop, Seconds = 1f, Inputs = new[] { new ItemAmount(99999, 1) }, Outputs = new[] { new ItemAmount(ResourceCatalog.IronPlate, 1) } },
                new RecipeDef { Id = "unknown_machine", Machine = 7699, Seconds = 1f, Inputs = new[] { new ItemAmount(GameItems.Iron, 1) }, Outputs = new[] { new ItemAmount(ResourceCatalog.IronPlate, 1) } },
                new RecipeDef { Id = "no_output", Machine = MachineCatalog.Workshop, Seconds = 1f, Inputs = new[] { new ItemAmount(GameItems.Iron, 1) }, Outputs = new ItemAmount[0] },
                new RecipeDef { Id = "zero_time", Machine = MachineCatalog.Workshop, Seconds = 0f, Inputs = new[] { new ItemAmount(GameItems.Iron, 1) }, Outputs = new[] { new ItemAmount(ResourceCatalog.IronPlate, 1) } },
                new RecipeDef { Id = "negative", Machine = MachineCatalog.Workshop, Seconds = 1f, Inputs = new[] { new ItemAmount(GameItems.Iron, -2) }, Outputs = new[] { new ItemAmount(ResourceCatalog.IronPlate, 1) } },
                new RecipeDef { Id = "from_nothing", Machine = MachineCatalog.Workshop, Seconds = 1f, Inputs = new ItemAmount[0], Outputs = new[] { new ItemAmount(ResourceCatalog.IronPlate, 1) } },
                new RecipeDef { Id = "too_big", Machine = MachineCatalog.Workshop, Seconds = 1f, Inputs = new[] { new ItemAmount(GameItems.Iron, 200) }, Outputs = new[] { new ItemAmount(ResourceCatalog.IronPlate, 1) } },
            };
            var issues = RecipeValidator.Validate(recipes, MachineCatalog.All, ResourceCatalog.All);
            foreach (RecipeDef r in recipes)
                Assert.Contains(issues, i => i.IsError && i.RecipeId == r.Id);
        }

        [Fact]
        public void DetectsBlockedChainsAndLoops()
        {
            var recipes = new[]
            {
                new RecipeDef { Id = "a", Machine = MachineCatalog.Workshop, Seconds = 1f, Inputs = new[] { new ItemAmount(ResourceCatalog.IronPlate, 1) }, Outputs = new[] { new ItemAmount(ResourceCatalog.CopperWire, 1) } },
                new RecipeDef { Id = "b", Machine = MachineCatalog.Workshop, Seconds = 1f, Inputs = new[] { new ItemAmount(ResourceCatalog.CopperWire, 1) }, Outputs = new[] { new ItemAmount(ResourceCatalog.IronPlate, 2) } },
            };
            var issues = RecipeValidator.Validate(recipes, MachineCatalog.All, ResourceCatalog.All);
            Assert.Contains(issues, i => !i.IsError && i.Message.Contains("boucle"));

            var blocked = new[] { recipes[0] };
            issues = RecipeValidator.Validate(blocked, MachineCatalog.All, ResourceCatalog.All);
            Assert.Contains(issues, i => i.IsError && i.Message.Contains("chaîne bloquée"));
        }

        [Fact]
        public void DisabledRecipesAreNotRun()
        {
            try
            {
                RecipeBook.Disable(new[] { "workshop.iron_plate" });
                Assert.DoesNotContain(RecipeBook.ForMachine(MachineCatalog.Workshop), r => r.Id == "workshop.iron_plate");
            }
            finally
            {
                RecipeBook.Disable(new string[0]);
            }
        }
    }
}
