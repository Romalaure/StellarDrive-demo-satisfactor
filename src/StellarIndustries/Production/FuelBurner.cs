using System;

namespace StellarIndustries.Production
{
    /// <summary>
    /// A generator's fuel, independent of the game's types. The energy drawn from the fuel item
    /// being burnt is counted aside; the item leaves its slot only once all its energy was given,
    /// so a reload (which forgets that count) never loses fuel, at worst gives part of one item
    /// again.
    /// </summary>
    internal static class FuelBurner
    {
        /// <summary>Energy left in the slots (joules), minus what was already drawn from the item being burnt.</summary>
        public static float Available(Stack[] slots, float burned, Func<uint, float> energy)
        {
            double total = 0;
            foreach (Stack s in slots)
            {
                if (!s.IsEmpty)
                    total += (double)s.Count * energy(s.Item);
            }
            return (float)Math.Max(0, total - burned);
        }

        /// <summary>Removes the fuel items whose energy was fully drawn. Returns true if the slots changed.</summary>
        public static bool Consume(Stack[] slots, ref float burned, Func<uint, float> energy)
        {
            bool changed = false;
            while (true)
            {
                int index = -1;
                for (int i = 0; i < slots.Length; i++)
                {
                    if (!slots[i].IsEmpty && energy(slots[i].Item) > 0f)
                    {
                        index = i;
                        break;
                    }
                }
                if (index < 0)
                {
                    burned = 0f;
                    return changed;
                }
                float e = energy(slots[index].Item);
                if (burned < e)
                    return changed;
                burned -= e;
                slots[index].Count--;
                if (slots[index].Count <= 0)
                    slots[index] = Stack.Empty;
                changed = true;
            }
        }
    }
}
