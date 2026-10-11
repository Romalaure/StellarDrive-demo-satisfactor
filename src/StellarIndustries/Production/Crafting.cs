using System;
using StellarIndustries.Recipes;

namespace StellarIndustries.Production
{
    /// <summary>One inventory slot, independent of the game's types (Count 0 = empty).</summary>
    internal struct Stack
    {
        public uint Item;
        public int Count;

        public Stack(uint item, int count)
        {
            Item = item;
            Count = count;
        }

        public bool IsEmpty => Count <= 0;

        public static readonly Stack Empty = default;

        public override string ToString() => IsEmpty ? "-" : Count + " x " + Item;
    }

    internal enum CraftResult
    {
        Ok,
        MissingInputs,
        OutputBlocked,
        InvalidRecipe
    }

    /// <summary>
    /// Applies a recipe to a machine's slots as one transaction: it works on a copy, and the copy
    /// is only returned when every input was taken and every output found room. The caller writes
    /// the result back in one state update, so nothing is consumed without being produced.
    /// Inputs are taken from the first slots onward; outputs join stacks of the same item from
    /// the last slot backward, then fill empty slots from the last one backward.
    /// </summary>
    internal static class Crafting
    {
        public static CraftResult Check(Stack[] slots, RecipeDef recipe, Func<uint, int> maxStack) =>
            TryCraft(slots, recipe, maxStack, out _);

        public static CraftResult TryCraft(Stack[] slots, RecipeDef recipe, Func<uint, int> maxStack, out Stack[] result)
        {
            result = null;
            if (slots == null || recipe == null || recipe.Inputs == null || recipe.Outputs == null || recipe.Outputs.Length == 0)
                return CraftResult.InvalidRecipe;
            foreach (ItemAmount input in recipe.Inputs)
            {
                if (input.Count <= 0)
                    return CraftResult.InvalidRecipe;
            }
            foreach (ItemAmount output in recipe.Outputs)
            {
                if (output.Count <= 0)
                    return CraftResult.InvalidRecipe;
            }

            var work = (Stack[])slots.Clone();
            foreach (ItemAmount input in recipe.Inputs)
            {
                if (!Take(work, input.Item, input.Count))
                    return CraftResult.MissingInputs;
            }
            foreach (ItemAmount output in recipe.Outputs)
            {
                if (!Put(work, output.Item, output.Count, Math.Max(1, Math.Min(255, maxStack(output.Item)))))
                    return CraftResult.OutputBlocked;
            }
            result = work;
            return CraftResult.Ok;
        }

        public static int CountOf(Stack[] slots, uint item)
        {
            int total = 0;
            foreach (Stack s in slots)
            {
                if (!s.IsEmpty && s.Item == item)
                    total += s.Count;
            }
            return total;
        }

        private static bool Take(Stack[] slots, uint item, int count)
        {
            if (CountOf(slots, item) < count)
                return false;
            for (int i = 0; i < slots.Length && count > 0; i++)
            {
                if (slots[i].IsEmpty || slots[i].Item != item)
                    continue;
                int n = Math.Min(count, slots[i].Count);
                slots[i].Count -= n;
                count -= n;
                if (slots[i].Count <= 0)
                    slots[i] = Stack.Empty;
            }
            return count == 0;
        }

        private static bool Put(Stack[] slots, uint item, int count, int max)
        {
            for (int i = slots.Length - 1; i >= 0 && count > 0; i--)
            {
                if (slots[i].IsEmpty || slots[i].Item != item || slots[i].Count >= max)
                    continue;
                int n = Math.Min(count, max - slots[i].Count);
                slots[i].Count += n;
                count -= n;
            }
            for (int i = slots.Length - 1; i >= 0 && count > 0; i--)
            {
                if (!slots[i].IsEmpty)
                    continue;
                int n = Math.Min(count, max);
                slots[i] = new Stack(item, n);
                count -= n;
            }
            return count == 0;
        }
    }
}
