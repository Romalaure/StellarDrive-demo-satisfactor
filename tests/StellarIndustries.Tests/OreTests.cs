using System.Collections.Generic;
using System.Linq;
using StellarIndustries.Content;
using StellarIndustries.Core;
using StellarIndustries.Production;
using StellarIndustries.Recipes;
using Xunit;

namespace StellarIndustries.Tests
{
    /// <summary>The 0.16 ores: which rocks hold them, and what they are for.</summary>
    public class OreTests
    {
        [Fact]
        public void AboutFourMetalRocksInTenHoldTheModsOres()
        {
            int converted = 0, total = 0;
            for (int b = 0; b < 60; b++)
            {
                for (int c = 0; c < 60; c++)
                {
                    total++;
                    if (OreConversion.Of(GameItems.Iron, 12345, b, c) != GameItems.Iron)
                        converted++;
                }
            }
            Assert.InRange(converted / (float)total, 0.35f, 0.45f);
            Assert.Equal(OreConversion.Of(GameItems.Copper, 7, 8, 9), OreConversion.Of(GameItems.Copper, 7, 8, 9));
        }

        [Fact]
        public void AboutOneRockInEightIsInexhaustible()
        {
            int nodes = 0, total = 0;
            for (int b = 0; b < 50; b++)
            {
                for (int c = 0; c < 50; c++)
                {
                    total++;
                    if (OreConversion.IsNode(777, b * 7919, c))
                        nodes++;
                }
            }
            Assert.InRange(nodes / (float)total, 0.09f, 0.15f);
            Assert.True(OreConversion.NodeBonus > 1f);
        }

        [Fact]
        public void ShardsAndIceStayWhatTheyAre()
        {
            for (int c = 0; c < 200; c++)
            {
                Assert.Equal(GameItems.StellarShard, OreConversion.Of(GameItems.StellarShard, 1, 2, c));
                Assert.Equal(GameItems.Ice, OreConversion.Of(GameItems.Ice, 1, 2, c));
            }
        }

        [Fact]
        public void EachPlanetHasThreeOfTheFiveOres()
        {
            var seen = new HashSet<uint>();
            for (int planet = 0; planet < 40; planet++)
            {
                var ores = new HashSet<uint>();
                for (int c = 0; c < 400; c++)
                {
                    uint ore = OreConversion.Of(GameItems.Aluminum, planet, 3, c);
                    if (ore != GameItems.Aluminum)
                        ores.Add(ore);
                }
                Assert.Equal(3, ores.Count);
                seen.UnionWith(ores);
            }
            Assert.Equal(5, seen.Count);
        }

        [Fact]
        public void EveryNewOreIsUsed()
        {
            foreach (uint ore in OreConversion.Ores)
            {
                Assert.NotNull(ResourceCatalog.Get(ore));
                bool used = RecipeBook.All.Any(r => r.Inputs.Any(i => i.Item == ore)) || MachineCatalog.FuelEnergy(ore) > 0f;
                Assert.True(used, ResourceCatalog.Get(ore).Name);
            }
            Assert.Contains(RecipeBook.All, r => r.Inputs.Any(i => i.Item == ResourceCatalog.GoldIngot));
        }
    }
}
