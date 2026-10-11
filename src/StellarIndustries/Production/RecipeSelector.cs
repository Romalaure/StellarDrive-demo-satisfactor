using System;
using StellarIndustries.Recipes;

namespace StellarIndustries.Production
{
    internal enum MachineStatus
    {
        /// <summary>Empty, nothing to do.</summary>
        Idle,
        /// <summary>Holds items, but not enough for any recipe.</summary>
        WaitingInputs,
        /// <summary>A recipe has its inputs but its outputs have no room.</summary>
        OutputBlocked,
        /// <summary>No power cable on its power socket.</summary>
        Unpowered,
        /// <summary>Connected, but its network gives no power.</summary>
        NoPower,
        Producing,
        /// <summary>Producing slower: its network gives part of what it needs.</summary>
        Underpowered,
        Error
    }

    /// <summary>
    /// Which recipe a machine runs from what it holds: the one it already runs while it still can
    /// (no flip-flop between recipes), otherwise the first in the machine's list that can run.
    /// </summary>
    internal static class RecipeSelector
    {
        public static RecipeDef Pick(Stack[] slots, RecipeDef[] recipes, RecipeDef current, Func<uint, int> maxStack, out MachineStatus status)
        {
            if (current != null && Array.IndexOf(recipes, current) >= 0 && Crafting.Check(slots, current, maxStack) == CraftResult.Ok)
            {
                status = MachineStatus.Producing;
                return current;
            }
            bool blocked = false;
            foreach (RecipeDef recipe in recipes)
            {
                CraftResult result = Crafting.Check(slots, recipe, maxStack);
                if (result == CraftResult.Ok)
                {
                    status = MachineStatus.Producing;
                    return recipe;
                }
                if (result == CraftResult.OutputBlocked)
                    blocked = true;
            }
            bool empty = true;
            foreach (Stack s in slots)
                empty &= s.IsEmpty;
            status = blocked ? MachineStatus.OutputBlocked : empty ? MachineStatus.Idle : MachineStatus.WaitingInputs;
            return null;
        }
    }
}
