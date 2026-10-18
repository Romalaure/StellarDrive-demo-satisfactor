using System;
using HarmonyLib;
using Items.Model;
using Ships.Interface.Model;
using Ships.Interface.Model.Parts;
using Ships.Interface.Model.Parts.State;
using Ships.Interface.Model.Parts.StateTypes;
using Ships.Interface.Settings;
using StellarIndustries.Core;
using StellarIndustries.Production;

namespace StellarIndustries.Logistics
{
    /// <summary>
    /// The industrial container holds 40 slots instead of the game's 10. The game's StorageState
    /// always says it has 10 slots; here it says it has as many as its array holds, so every
    /// game path that goes through SlotCount (player moves, saving, ship mass, SDModKit's item
    /// moves) sees the 40 slots. Containers get 40 slots when they are built, loaded or written,
    /// and a storage of other than 10 slots is sent over the network with its slot count (a
    /// first byte of 2 or 3 instead of the "in use" flag); 10-slot storages are sent as the game
    /// sends them.
    /// </summary>
    internal static class BigContainer
    {
        public const int Slots = 40;
        private const int GameSlots = 10;

        private static readonly AccessTools.FieldRef<StorageState, InventorySlot[]> Array =
            AccessTools.FieldRefAccess<StorageState, InventorySlot[]>("<InventorySlots>k__BackingField");

        /// <summary>How many slots this storage has (the game's getter may be inlined into code compiled before the patch).</summary>
        public static int Count(StorageState state)
        {
            InventorySlot[] slots = state != null ? Array(state) : null;
            return slots?.Length ?? GameSlots;
        }

        public static bool IsBig(PartSettings settings) => settings != null && settings.id == MachineCatalog.IndustrialContainer;

        /// <summary>The state with at least <see cref="Slots"/> slots (the same state when it already has them).</summary>
        public static StorageState Grown(StorageState state)
        {
            InventorySlot[] slots = Array(state);
            if (slots != null && slots.Length >= Slots)
                return state;
            var grown = new InventorySlot[Slots];
            if (slots != null)
                System.Array.Copy(slots, grown, slots.Length);
            return state with { InventorySlots = grown };
        }

        internal static InventorySlot[] Raw(StorageState state) => Array(state);
    }

    [HarmonyPatch(typeof(StorageState), nameof(StorageState.SlotCount), MethodType.Getter)]
    internal static class StorageSlotCountPatch
    {
        private static bool Prefix(StorageState __instance, ref int __result)
        {
            __result = BigContainer.Count(__instance);
            return false;
        }
    }

    [HarmonyPatch(typeof(StorageState), nameof(StorageState.WithUpdatedSlot))]
    internal static class StorageWithUpdatedSlotPatch
    {
        private static bool Prefix(StorageState __instance, byte slotId, InventorySlot slotData, ref IStoragePart __result)
        {
            InventorySlot[] slots = BigContainer.Raw(__instance);
            int count = slots?.Length ?? 10;
            var copy = new InventorySlot[count];
            if (slots != null)
                Array.Copy(slots, copy, count);
            copy[slotId] = slotData;
            __result = __instance with { InventorySlots = copy };
            return false;
        }
    }

    /// <summary>A container built (or received from the host) starts with 40 slots.</summary>
    [HarmonyPatch(typeof(PartStateFactory), nameof(PartStateFactory.BuildDefaultState))]
    internal static class ContainerDefaultStatePatch
    {
        private static void Postfix(PartSettings partSettings, ref IStatefulPartState __result)
        {
            try
            {
                if (BigContainer.IsBig(partSettings) && __result is StorageState state)
                    __result = BigContainer.Grown(state);
            }
            catch (Exception e)
            {
                ErrorLog.Report("container", e);
            }
        }
    }

    /// <summary>Any state written to a container keeps its 40 slots (a save from before 0.19, a 10-slot state written by code).</summary>
    [HarmonyPatch(typeof(ShipState), nameof(ShipState.SetPartState))]
    internal static class ContainerSetStatePatch
    {
        private static void Prefix(ShipState __instance, ushort id, ref IStatefulPartState state)
        {
            try
            {
                if (state is StorageState storage && BigContainer.Count(storage) < BigContainer.Slots &&
                    __instance.TryGetPart(id, out StatefulPart part) && BigContainer.IsBig(part.Settings))
                    state = BigContainer.Grown(storage);
            }
            catch (Exception e)
            {
                ErrorLog.Report("container", e);
            }
        }
    }

    /// <summary>Containers loaded from a save made before 0.19 get their 40 slots.</summary>
    [HarmonyPatch(typeof(Saving.Services.Converters.ShipPartConverter), nameof(Saving.Services.Converters.ShipPartConverter.ConvertToFloatingPartState))]
    internal static class ContainerLoadPatch
    {
        private static void Postfix(StatefulPart[] __result)
        {
            try
            {
                if (__result == null)
                    return;
                foreach (StatefulPart part in __result)
                {
                    if (part != null && BigContainer.IsBig(part.Settings) && part.State is StorageState state)
                        part.State = BigContainer.Grown(state);
                }
            }
            catch (Exception e)
            {
                ErrorLog.Report("container", e);
            }
        }
    }

    [HarmonyPatch(typeof(Managers.Server.Network.Serialization.StatefulPartSerialization), nameof(Managers.Server.Network.Serialization.StatefulPartSerialization.WriteState))]
    internal static class StorageWritePatch
    {
        private static bool Prefix(FishNet.Serializing.Writer writer, IStatefulPartState state)
        {
            if (!(state is StorageState storage) || BigContainer.Count(storage) == 10)
                return true;
            InventorySlot[] slots = BigContainer.Raw(storage);
            writer.WriteUInt8Unpacked((byte)(storage.IsInUse ? 3 : 2));
            writer.WriteUInt8Unpacked((byte)slots.Length);
            foreach (InventorySlot slot in slots)
                StorageReadPatch.WriteSlot(writer, slot);
            return false;
        }
    }

    [HarmonyPatch(typeof(Managers.Server.Network.Serialization.StatefulPartSerialization), nameof(Managers.Server.Network.Serialization.StatefulPartSerialization.ReadNewState))]
    internal static class StorageReadPatch
    {
        private static bool Prefix(IStatefulPartState originalState, FishNet.Serializing.Reader reader, ref IStatefulPartState __result)
        {
            if (!(originalState is StorageState))
                return true;
            // The game's format: the "in use" flag (0 or 1) then 10 slots; ours: 2 or 3, the count, the slots
            byte first = reader.ReadUInt8Unpacked();
            int count = first >= 2 ? reader.ReadUInt8Unpacked() : 10;
            var slots = new InventorySlot[count];
            for (int i = 0; i < count; i++)
            {
                if (reader.ReadBoolean())
                {
                    slots[i].HasItem = true;
                    slots[i].ItemId = reader.ReadUInt32();
                    slots[i].Quantity = reader.ReadUInt8Unpacked();
                }
            }
            __result = new StorageState { IsInUse = (first & 1) == 1, InventorySlots = slots };
            return false;
        }

        internal static void WriteSlot(FishNet.Serializing.Writer writer, InventorySlot slot)
        {
            writer.WriteBoolean(slot.HasItem);
            if (slot.HasItem)
            {
                writer.WriteUInt32(slot.ItemId);
                writer.WriteUInt8Unpacked(slot.Quantity);
            }
        }
    }
}
