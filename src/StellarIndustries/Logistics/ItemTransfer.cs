using System;
using StellarIndustries.Production;

namespace StellarIndustries.Logistics
{
    /// <summary>
    /// Moves items between two inventories, independent of the game's types: from the source's
    /// first slots, onto stacks of the same item in the destination first, then into its empty
    /// slots, never above an item's stack limit. Both arrays are changed together; the caller
    /// writes both back in the same tick, so an item is never in both places nor in neither.
    /// </summary>
    internal static class ItemTransfer
    {
        public static int Move(Stack[] source, Stack[] dest, int budget, Func<uint, int> maxStack)
        {
            int moved = 0;
            for (int i = 0; i < source.Length && budget > 0; i++)
            {
                if (source[i].IsEmpty)
                    continue;
                uint item = source[i].Item;
                int max = Math.Max(1, Math.Min(255, maxStack(item)));
                // Same item first, then empty slots
                for (int pass = 0; pass < 2 && budget > 0 && !source[i].IsEmpty; pass++)
                {
                    for (int j = 0; j < dest.Length && budget > 0 && !source[i].IsEmpty; j++)
                    {
                        bool fits = pass == 0 ? !dest[j].IsEmpty && dest[j].Item == item && dest[j].Count < max : dest[j].IsEmpty;
                        if (!fits)
                            continue;
                        int room = max - (dest[j].IsEmpty ? 0 : dest[j].Count);
                        int n = Math.Min(Math.Min(room, source[i].Count), budget);
                        dest[j] = new Stack(item, (dest[j].IsEmpty ? 0 : dest[j].Count) + n);
                        source[i].Count -= n;
                        if (source[i].Count <= 0)
                            source[i] = Stack.Empty;
                        budget -= n;
                        moved += n;
                    }
                }
            }
            return moved;
        }
    }
}
