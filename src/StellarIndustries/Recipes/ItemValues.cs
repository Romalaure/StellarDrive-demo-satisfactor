using System;
using System.Collections.Generic;
using System.Linq;
using StellarIndustries.Content;
using StellarIndustries.Production;

namespace StellarIndustries.Recipes
{
    /// <summary>
    /// What an item is worth at the recycler, from its production chain: a game resource is worth
    /// 1, a product is worth what its cheapest recipe puts into it (per item made), plus a quarter
    /// for the work. Vouchers are worth nothing (they cannot be recycled into more vouchers).
    /// </summary>
    internal static class ItemValues
    {
        public const int PointsPerVoucher = 100;

        private static Dictionary<uint, float> _values;

        public static float Of(uint item)
        {
            if (_values == null)
                _values = new Dictionary<uint, float>();
            return Compute(item, new HashSet<uint>());
        }

        private static float Compute(uint item, HashSet<uint> path)
        {
            if (item == ResourceCatalog.Voucher)
                return 0f;
            if (_values.TryGetValue(item, out float known))
                return known;
            var recipes = RecipeBook.All.Where(r => r.Outputs.Any(o => o.Item == item) && !r.Inputs.Any(i => i.Item == ResourceCatalog.Voucher)).ToList();
            if (recipes.Count == 0 || !path.Add(item))
            {
                // A game resource, a special product (ice, waste), or a loop
                float raw = ResourceCatalog.Get(item) == null ? 1f : 2f;
                if (recipes.Count == 0)
                    _values[item] = raw;
                return raw;
            }
            float best = float.MaxValue;
            foreach (RecipeDef r in recipes)
            {
                float cost = r.Inputs.Sum(i => Compute(i.Item, path) * i.Count);
                int made = r.Outputs.Where(o => o.Item == item).Sum(o => o.Count);
                best = Math.Min(best, cost / Math.Max(1, made));
            }
            path.Remove(item);
            float value = (float)Math.Round(best * 1.25f + 0.25f, 2);
            _values[item] = value;
            return value;
        }
    }
}
