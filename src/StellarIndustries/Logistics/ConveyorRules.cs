using System;
using System.Linq;
using StellarIndustries.Content;
using StellarIndustries.Production;
using StellarIndustries.Recipes;

namespace StellarIndustries.Logistics
{
    /// <summary>
    /// What a machine lets an item line take out of it and put into it, so that conveyors feed
    /// inputs and carry products away without emptying a machine of its own ingredients or
    /// filling all its slots with them:
    ///  - out: only the machine's products (outputs of its recipes, every item of an extractor),
    ///    never an ingredient of the recipe it runs or was set to;
    ///  - in: only ingredients of its recipes (of the chosen one if set), up to a few cycles'
    ///    worth, and a new kind of item only while two slots stay free for the products.
    /// </summary>
    internal static class ConveyorRules
    {
        /// <summary>Cycles of ingredients a machine is fed ahead.</summary>
        public const int CyclesAhead = 4;

        /// <summary>Slots kept free for products when a new ingredient arrives.</summary>
        public const int FreeSlotsKept = 2;

        /// <summary>Fuel a generator is fed ahead (items).</summary>
        public const int FuelAhead = 20;

        public static bool CanSend(MachineDef def, RecipeDef[] recipes, RecipeDef active, uint item)
        {
            if (def.Extracts || def.IsLogistics || def.FixedOutput != 0)
                return true;
            if (def.Stores || def.Radar)
                return false;
            if (def.Sinks)
                return item == ResourceCatalog.Voucher;
            if (def.Generates)
                return def.WasteItem != 0 && item == def.WasteItem;
            if (!recipes.Any(r => r.Outputs.Any(o => o.Item == item)))
                return false;
            return active == null || active.Inputs.All(i => i.Item != item);
        }

        public static bool CanAccept(MachineDef def, RecipeDef[] recipes, RecipeDef chosen, Stack[] slots, uint item, Func<uint, int> maxStack)
        {
            if (def.IsLogistics)
                return true;
            if (def.Extracts || def.Stores || def.Radar || def.FixedOutput != 0)
                return false;
            if (def.Generates)
                return MachineCatalog.FuelEnergy(def, item) > 0f && Crafting.CountOf(slots, item) < Math.Min(FuelAhead, maxStack(item));
            if (def.Sinks)
                return item != ResourceCatalog.Voucher && slots.Count(s => s.IsEmpty) > FreeSlotsKept;
            RecipeDef[] candidates = chosen != null ? new[] { chosen } : recipes;
            int need = 0;
            foreach (RecipeDef r in candidates)
            {
                foreach (ItemAmount input in r.Inputs)
                {
                    if (input.Item == item)
                        need = Math.Max(need, input.Count);
                }
            }
            if (need == 0)
                return false;
            int cap = Math.Max(need, Math.Min(need * CyclesAhead, maxStack(item)));
            int have = Crafting.CountOf(slots, item);
            if (have >= cap)
                return false;
            if (have > 0)
                return true;
            int free = slots.Count(s => s.IsEmpty);
            return free > FreeSlotsKept;
        }
    }
}
