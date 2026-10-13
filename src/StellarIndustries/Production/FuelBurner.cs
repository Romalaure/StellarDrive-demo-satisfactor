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

        /// <summary>Removes the fuel items whose energy was fully drawn; returns how many were burnt.</summary>
        public static int Consume(Stack[] slots, ref float burned, Func<uint, float> energy, out bool changed)
        {
            int before = 0, after = 0;
            foreach (Stack s in slots)
                before += s.IsEmpty || energy(s.Item) <= 0f ? 0 : s.Count;
            changed = Consume(slots, ref burned, energy);
            foreach (Stack s in slots)
                after += s.IsEmpty || energy(s.Item) <= 0f ? 0 : s.Count;
            return before - after;
        }

        /// <summary>Whether one more <paramref name="item"/> fits (a stack with room, or an empty slot).</summary>
        public static bool HasRoom(Stack[] slots, uint item, int max)
        {
            foreach (Stack s in slots)
            {
                if (s.IsEmpty || s.Item == item && s.Count < max)
                    return true;
            }
            return false;
        }

        /// <summary>Puts items into the slots (same item first, then empty slots); returns how many fitted.</summary>
        public static int Add(Stack[] slots, uint item, int count, int max)
        {
            int added = 0;
            for (int pass = 0; pass < 2 && count > 0; pass++)
            {
                for (int i = slots.Length - 1; i >= 0 && count > 0; i--)
                {
                    bool fits = pass == 0 ? !slots[i].IsEmpty && slots[i].Item == item && slots[i].Count < max : slots[i].IsEmpty;
                    if (!fits)
                        continue;
                    int n = Math.Min(count, max - (slots[i].IsEmpty ? 0 : slots[i].Count));
                    slots[i] = new Stack(item, (slots[i].IsEmpty ? 0 : slots[i].Count) + n);
                    count -= n;
                    added += n;
                }
            }
            return added;
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
