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
        /// <summary>Extractor without a minable rock within reach.</summary>
        NoDeposit,
        /// <summary>Generator without anything to burn.</summary>
        NoFuel,
        /// <summary>Generator: its network asks for nothing right now.</summary>
        Standby,
        Error
    }

    /// <summary>
    /// Which recipe a machine runs from what it holds: the recipe the player chose for it if any
    /// (and only that one), otherwise the one it already runs while it still can (no flip-flop
    /// between recipes), otherwise the first in the machine's list that can run.
    /// </summary>
    internal static class RecipeSelector
    {
        public static RecipeDef Pick(Stack[] slots, RecipeDef[] recipes, RecipeDef current, RecipeDef chosen, Func<uint, int> maxStack, out MachineStatus status)
        {
            if (chosen != null && Array.IndexOf(recipes, chosen) >= 0)
                return Pick(slots, new[] { chosen }, chosen, maxStack, out status);
            return Pick(slots, recipes, current, maxStack, out status);
        }

        /// <summary>The next choice when the player cycles a machine's recipe: automatic (null), then each recipe in turn.</summary>
        public static RecipeDef Next(RecipeDef[] recipes, RecipeDef chosen, int step = 1)
        {
            if (recipes.Length == 0)
                return null;
            // Index -1 is the automatic mode
            int index = chosen == null ? -1 : Array.IndexOf(recipes, chosen);
            int count = recipes.Length + 1;
            int next = ((index + 1 + step) % count + count) % count - 1;
            return next < 0 ? null : recipes[next];
        }

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
