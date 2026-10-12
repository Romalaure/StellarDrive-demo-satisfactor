using System;
using System.Reflection;
using HarmonyLib;
using SDModKit.Content;
using SDModKit.Game;
using Ships.Interface.Model.Parts;
using Ships.Interface.Model.Parts.State;
using Ships.Interface.Model.State;
using StellarIndustries.Content;
using StellarIndustries.Core;
using StellarIndustries.Production;
using StellarIndustries.Recipes;

namespace StellarIndustries.Logistics
{
    /// <summary>
    /// Conveyors through the "Item Lines" mod (optional, by another author): it gives every part
    /// with an inventory an item port, machines included, and moves items along item cables
    /// through SDModKit's ItemMove.Transfer. This bridge applies <see cref="ConveyorRules"/> to
    /// every such move from or into one of our machines, and lets our machines receive by
    /// default (Item Lines only lets the game's own machines receive unless the player sets it).
    /// Without Item Lines nothing here runs and the machines are filled by hand.
    /// </summary>
    internal static class ItemLinesBridge
    {
        private static readonly Production.Stack[] Buffer = new Production.Stack[10];
        private static bool _tried;

        public static bool Installed { get; private set; }

        /// <summary>Patches Item Lines' port, once it is loaded (call when a world loads).</summary>
        public static void TryInstall(HarmonyLib.Harmony harmony)
        {
            if (_tried)
                return;
            _tried = true;
            Type port = AccessTools.TypeByName("ItemLines.AttachedPortBehaviour");
            MethodInfo canReceive = port != null ? AccessTools.Method(port, "CanReceive", new[] { typeof(ContentPart) }) : null;
            if (canReceive == null)
            {
                IndustriesMod.Log.Msg("conveyors: Item Lines is not installed, machines are filled by hand");
                return;
            }
            harmony.Patch(canReceive, postfix: new HarmonyMethod(typeof(ItemLinesBridge), nameof(CanReceivePostfix)));
            Installed = true;
            IndustriesMod.Log.Msg("conveyors: Item Lines found, machines take their ingredients from item lines and send their products");
        }

        private static void CanReceivePostfix(ContentPart part, ref bool __result)
        {
            try
            {
                if (!__result && part.Part?.Settings != null && MachineCatalog.TryGet(part.Part.Settings.id, out MachineDef def) && !def.Extracts &&
                    part.Config != null && !part.Config.Has("receive"))
                    __result = true;
            }
            catch (Exception e)
            {
                ErrorLog.Report("conveyors", e);
            }
        }

        /// <summary>The filter of one move: the caller's own, then the rules of a machine at either end.</summary>
        public static Func<uint, bool> Filter(IShipStateWrite ship, ushort sourceId, ushort destId, Func<uint, bool> allowed)
        {
            if (ship == null)
                return allowed;
            MachineDef from = null, to = null;
            StatefulPart source = null, dest = null;
            if (ship.TryGetStatefulPart(sourceId, out source) && source?.Settings != null)
                MachineCatalog.TryGet(source.Settings.id, out from);
            if (ship.TryGetStatefulPart(destId, out dest) && dest?.Settings != null)
                MachineCatalog.TryGet(dest.Settings.id, out to);
            if (from == null && to == null)
                return allowed;
            uint shipId = ship.Id;
            return item =>
            {
                try
                {
                    return Allows(ship, shipId, sourceId, destId, from, to, allowed, item);
                }
                catch (Exception e)
                {
                    ErrorLog.Report("conveyors", e);
                    return false;
                }
            };
        }

        private static bool Allows(IShipStateWrite ship, uint shipId, ushort sourceId, ushort destId, MachineDef from, MachineDef to, Func<uint, bool> allowed, uint item)
        {
            {
                if (allowed != null && !allowed(item))
                    return false;
                if (from != null)
                {
                    RecipeDef active = MachineControl.Chosen(shipId, sourceId, from.PartId) ?? MachineSimulation.Get(shipId, sourceId)?.Recipe;
                    if (!ConveyorRules.CanSend(from, RecipeBook.ForMachine(from.PartId), active, item))
                        return false;
                }
                if (to != null)
                {
                    // The destination as it is now (a move of several items asks again for each)
                    if (!ship.TryGetStatefulPart(destId, out StatefulPart now) || !(now.State is StorageState state))
                        return false;
                    MachineSimulation.Read(state, Buffer);
                    if (!ConveyorRules.CanAccept(to, RecipeBook.ForMachine(to.PartId), MachineControl.Chosen(shipId, destId, to.PartId), Buffer, item, IndustrialItems.MaxStack))
                        return false;
                }
                return true;
            }
        }
    }

    /// <summary>Every item move made through SDModKit (Item Lines' conveyors) obeys the machines' rules.</summary>
    [HarmonyPatch(typeof(ItemMove), nameof(ItemMove.Transfer))]
    internal static class ItemMoveTransferPatch
    {
        private static void Prefix(IShipStateWrite ship, ushort sourceId, ushort destId, ref Func<uint, bool> allowed)
        {
            try
            {
                allowed = ItemLinesBridge.Filter(ship, sourceId, destId, allowed);
            }
            catch (Exception e)
            {
                ErrorLog.Report("conveyors", e);
            }
        }
    }
}
