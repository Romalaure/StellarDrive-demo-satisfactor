using System.Linq;
using StellarIndustries.Content;
using StellarIndustries.Core;
using StellarIndustries.Logistics;
using StellarIndustries.Production;
using StellarIndustries.Recipes;
using Xunit;

namespace StellarIndustries.Tests
{
    public class TransferTests
    {
        private static int Max20(uint item) => 20;
        private static int Total(Stack[] a, Stack[] b) => a.Sum(s => s.Count) + b.Sum(s => s.Count);

        [Fact]
        public void MovesUpToTheBudgetAndKeepsTheTotal()
        {
            var from = new Stack[10];
            var to = new Stack[10];
            from[0] = new Stack(GameItems.Iron, 7);
            from[3] = new Stack(GameItems.Copper, 5);
            int total = Total(from, to);
            Assert.Equal(4, ItemTransfer.Move(from, to, 4, Max20));
            Assert.Equal(3, from[0].Count);
            Assert.Equal(new Stack(GameItems.Iron, 4), to[0]);
            Assert.Equal(total, Total(from, to));
            Assert.Equal(8, ItemTransfer.Move(from, to, 100, Max20));
            Assert.Equal(total, Total(from, to));
            Assert.All(from, s => Assert.True(s.IsEmpty));
        }

        [Fact]
        public void JoinsStacksBeforeUsingEmptySlotsAndRespectsLimits()
        {
            var from = new Stack[10];
            var to = new Stack[10];
            from[0] = new Stack(GameItems.Iron, 15);
            to[4] = new Stack(GameItems.Iron, 18);
            Assert.Equal(15, ItemTransfer.Move(from, to, 100, Max20));
            Assert.Equal(20, to[4].Count);
            Assert.Equal(new Stack(GameItems.Iron, 13), to[0]);
        }

        [Fact]
        public void FullDestinationMovesNothing()
        {
            var from = new Stack[10];
            var to = new Stack[10];
            from[0] = new Stack(GameItems.Iron, 5);
            for (int i = 0; i < 10; i++)
                to[i] = new Stack(GameItems.Copper, 20);
            Assert.Equal(0, ItemTransfer.Move(from, to, 100, Max20));
            Assert.Equal(5, from[0].Count);
        }

        [Fact]
        public void LogisticsPartsTakeAndGiveAnything()
        {
            MachineCatalog.TryGet(MachineCatalog.IndustrialContainer, out MachineDef box);
            MachineCatalog.TryGet(MachineCatalog.LoadingDock, out MachineDef dock);
            foreach (MachineDef d in new[] { box, dock })
            {
                Assert.True(d.IsLogistics);
                Assert.True(ConveyorRules.CanSend(d, new RecipeDef[0], null, GameItems.Wood));
                Assert.True(ConveyorRules.CanAccept(d, new RecipeDef[0], null, new Stack[10], ResourceCatalog.Screws, Max20));
            }
        }
    }
}
