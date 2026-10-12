using System;
using System.Collections.Generic;
using Core.Services;
using HarmonyLib;
using Items.Model;
using Ships;
using Ships.Cables.Power;
using Ships.Interface.Model.Parts;
using Ships.Interface.Model.Parts.Common;
using Ships.Interface.Model.Parts.State;
using Ships.Services;
using StellarIndustries.Content;
using StellarIndustries.Core;
using StellarIndustries.Recipes;
using UnityEngine;

namespace StellarIndustries.Production
{
    /// <summary>
    /// Host-side simulation of the production machines, inside the game's own power tick
    /// (PowerServerTracker.UpdatePowerTransfers, run by the server only, on every loaded ship).
    ///
    /// A machine is a part whose state is the game's StorageState: its 10 slots are its input
    /// and output buffers, saved in the world file and sent to every player by the game itself.
    /// Before the game splits power, each machine that can run a recipe asks its network for its
    /// power; right after, it advances in proportion to the power it got. When a cycle completes,
    /// the recipe is applied to the slots as one transaction (see <see cref="Crafting"/>) and
    /// written back in a single state update: inputs never disappear without outputs.
    ///
    /// The cycle progress lives in memory only: after a reload a machine restarts its current
    /// cycle (nothing is lost, since nothing is taken before a cycle completes).
    /// </summary>
    internal static class MachineSimulation
    {
        public const byte PowerSocket = 0;

        internal sealed class Runtime
        {
            public RecipeDef Recipe;
            public float Progress;
            public MachineStatus Status;
            public float Watts;
            public float PowerRate;
            public PowerGroupServerState Group;
            public float LastSeen;
            public int Cycles;
            public string Detail;
            /// <summary>Generator: energy already drawn from the fuel item being burnt, and offered this tick (joules).</summary>
            public float Burned, Offered;
        }

        private struct Entry
        {
            public TrackedShipServer Ship;
            public StatefulPart Part;
            public MachineDef Def;
            public Runtime Runtime;
        }

        private static readonly Dictionary<(uint, ushort), Runtime> Runtimes = new Dictionary<(uint, ushort), Runtime>();
        private static readonly List<Entry> Running = new List<Entry>();
        private static readonly List<Entry> Burning = new List<Entry>();
        private static readonly List<(uint, ushort)> Stale = new List<(uint, ushort)>();
        private static readonly Stack[] Buffer = new Stack[10];
        private static ShipsServerTracker _ships;
        private static float _nextPrune;

        /// <summary>Host only: what a machine is doing now (null if the host does not simulate it).</summary>
        public static Runtime Get(uint shipId, ushort partId) => Runtimes.TryGetValue((shipId, partId), out Runtime rt) ? rt : null;

        /// <summary>The machine's recipe was changed: its cycle starts over (nothing was taken yet).</summary>
        public static void Restart(uint shipId, ushort partId)
        {
            if (Runtimes.TryGetValue((shipId, partId), out Runtime rt))
            {
                rt.Progress = 0f;
                rt.Recipe = null;
            }
        }

        public static void Forget()
        {
            Runtimes.Clear();
            Running.Clear();
            _ships = null;
        }

        public static void BeforeTransfers(PowerServerTracker power, float deltaTime)
        {
            Running.Clear();
            Burning.Clear();
            if (deltaTime <= 0f || !Settings.Enabled.Value || Diagnostics.DevTest.HoldProduction)
                return;
            if (_ships == null)
                _ships = ServiceLocator.GetService<ShipsServerTracker>();
            if (_ships == null)
                return;
            float now = Time.unscaledTime;
            foreach (TrackedShipServer ship in _ships.LoadedShipsWithChildren)
            {
                foreach (StatefulPart part in ship.GetAllStatefulParts<StorageState>())
                {
                    if (!MachineCatalog.TryGet(part.Settings.id, out MachineDef def))
                        continue;
                    if (!Runtimes.TryGetValue((ship.Id, part.Id), out Runtime rt))
                        Runtimes[(ship.Id, part.Id)] = rt = new Runtime();
                    rt.LastSeen = now;
                    rt.Group = null;
                    rt.PowerRate = 0f;
                    if (def.Generates)
                        PrepareGenerator(power, ship, part, def, rt, deltaTime);
                    else
                        Prepare(power, ship, part, def, rt, deltaTime);
                }
            }
            if (now >= _nextPrune)
                Prune(now);
        }

        private static void Prepare(PowerServerTracker power, TrackedShipServer ship, StatefulPart part, MachineDef def, Runtime rt, float deltaTime)
        {
            var state = (StorageState)part.State;
            Read(state, Buffer);
            RecipeDef recipe;
            MachineStatus status;
            if (def.Extracts)
            {
                // An extractor's only "recipe" is one item of the rock within its reach
                Deposits.Deposit? deposit = Deposits.Find(ship.Id, part, def);
                if (deposit == null)
                {
                    rt.Recipe = null;
                    rt.Progress = 0f;
                    rt.Watts = 0f;
                    rt.Status = MachineStatus.NoDeposit;
                    rt.Detail = $"Aucun rocher minable à moins de {def.ExtractRadius:0} m";
                    return;
                }
                Deposits.Deposit d = deposit.Value;
                rt.Detail = $"Gisement : {d.Name} à {d.Distance:0.0} m";
                RecipeDef extraction = RecipeBook.Extraction(def.PartId, d.Item, def.ExtractSeconds, IndustrialItems.NameOf(d.Item));
                recipe = RecipeSelector.Pick(Buffer, new[] { extraction }, rt.Recipe, IndustrialItems.MaxStack, out status);
            }
            else
            {
                RecipeDef chosen = MachineControl.Chosen(ship.Id, part.Id, def.PartId);
                recipe = RecipeSelector.Pick(Buffer, RecipeBook.ForMachine(def.PartId), rt.Recipe, chosen, IndustrialItems.MaxStack, out status);
            }
            if (recipe != rt.Recipe)
                rt.Progress = 0f;
            rt.Recipe = recipe;
            rt.Status = status;
            if (recipe == null)
            {
                rt.Watts = 0f;
                return;
            }
            rt.Watts = (recipe.Watts > 0f ? recipe.Watts : def.Watts) * Settings.Power;
            if (Settings.RequirePower.Value && rt.Watts > 0f)
            {
                if (!power.TryGetGroupFromSocket(new EntityPartSocket(ship.Id, part.Id, PowerSocket), out PowerGroupServerState group))
                {
                    rt.Status = MachineStatus.Unpowered;
                    return;
                }
                group.TotalPowerNeeded += rt.Watts * deltaTime;
                rt.Group = group;
            }
            Running.Add(new Entry { Ship = ship, Part = part, Def = def, Runtime = rt });
        }

        // ---- Generators

        private static readonly Func<uint, float> FuelEnergy = MachineCatalog.FuelEnergy;

        /// <summary>A generator offers its network what its fuel can give this tick, up to its rating.</summary>
        private static void PrepareGenerator(PowerServerTracker power, TrackedShipServer ship, StatefulPart part, MachineDef def, Runtime rt, float deltaTime)
        {
            rt.Recipe = null;
            rt.Offered = 0f;
            Read((StorageState)part.State, Buffer);
            float available = FuelBurner.Available(Buffer, rt.Burned, FuelEnergy);
            rt.Detail = FuelText(Buffer, available, def.Watts);
            if (available <= 0f)
            {
                rt.Status = MachineStatus.NoFuel;
                rt.Watts = 0f;
                return;
            }
            if (!power.TryGetGroupFromSocket(new EntityPartSocket(ship.Id, part.Id, PowerSocket), out PowerGroupServerState group))
            {
                rt.Status = MachineStatus.Unpowered;
                rt.Watts = 0f;
                return;
            }
            rt.Offered = Mathf.Min(def.Watts * deltaTime, available);
            group.TotalPowerProductionCapacity += rt.Offered;
            rt.Group = group;
            Burning.Add(new Entry { Ship = ship, Part = part, Def = def, Runtime = rt });
        }

        /// <summary>After the split: the energy the network really took is burnt.</summary>
        private static void AfterGenerators(float deltaTime)
        {
            foreach (Entry entry in Burning)
            {
                Runtime rt = entry.Runtime;
                float delivered = rt.Offered * Mathf.Clamp01(rt.Group.NormalizedGeneratorRate);
                rt.Watts = entry.Def.Watts;
                rt.PowerRate = Mathf.Clamp01(delivered / (entry.Def.Watts * deltaTime));
                rt.Status = delivered > 0f ? MachineStatus.Producing : MachineStatus.Standby;
                if (delivered <= 0f || !(entry.Part.State is StorageState state))
                    continue;
                rt.Burned += delivered;
                Read(state, Buffer);
                if (FuelBurner.Consume(Buffer, ref rt.Burned, FuelEnergy))
                    entry.Ship.UpdatePartState(entry.Part.Id, state with { InventorySlots = Write(Buffer) });
            }
            Burning.Clear();
        }

        private static string FuelText(Stack[] slots, float joules, float watts)
        {
            int count = 0;
            foreach (Stack s in slots)
            {
                if (!s.IsEmpty && FuelEnergy(s.Item) > 0f)
                    count += s.Count;
            }
            if (count == 0)
                return "Combustible : aucun (bois ou briquettes)";
            float seconds = watts > 0f ? joules / watts : 0f;
            return $"Combustible : {count} objet(s), {Mathf.RoundToInt(joules / 1000f)} kJ (≈ {Mathf.RoundToInt(seconds)} s à pleine charge)";
        }

        public static void AfterTransfers(float deltaTime)
        {
            if (deltaTime <= 0f)
            {
                Burning.Clear();
                return;
            }
            AfterGenerators(deltaTime);
            foreach (Entry entry in Running)
            {
                Runtime rt = entry.Runtime;
                float rate = rt.Group != null ? Mathf.Clamp01(rt.Group.NormalizedConsumerRate) : 1f;
                rt.PowerRate = rate;
                if (rate <= 0.001f)
                {
                    rt.Status = MachineStatus.NoPower;
                    continue;
                }
                rt.Status = rate < 0.999f ? MachineStatus.Underpowered : MachineStatus.Producing;
                rt.Progress += deltaTime * rate * entry.Def.Speed * Settings.Speed / rt.Recipe.Seconds;
                if (rt.Progress < 1f)
                    continue;
                Complete(entry);
            }
            Running.Clear();
        }

        private static void Complete(Entry entry)
        {
            Runtime rt = entry.Runtime;
            // Read again: a player may have moved items during this tick
            if (!(entry.Part.State is StorageState state))
                return;
            Read(state, Buffer);
            CraftResult result = Crafting.TryCraft(Buffer, rt.Recipe, IndustrialItems.MaxStack, out Stack[] after);
            if (result != CraftResult.Ok)
            {
                rt.Progress = 0f;
                rt.Status = result == CraftResult.OutputBlocked ? MachineStatus.OutputBlocked : MachineStatus.WaitingInputs;
                return;
            }
            // One cycle per tick at most; a partial next cycle is kept
            rt.Progress = Mathf.Min(rt.Progress - 1f, 0.99f);
            rt.Cycles++;
            entry.Ship.UpdatePartState(entry.Part.Id, state with { InventorySlots = Write(after) });
        }

        private static void Prune(float now)
        {
            _nextPrune = now + 5f;
            Stale.Clear();
            foreach (KeyValuePair<(uint, ushort), Runtime> pair in Runtimes)
            {
                if (now - pair.Value.LastSeen > 5f)
                    Stale.Add(pair.Key);
            }
            foreach ((uint, ushort) key in Stale)
                Runtimes.Remove(key);
        }

        public static void Read(StorageState state, Stack[] into)
        {
            for (int i = 0; i < into.Length; i++)
            {
                InventorySlot slot = i < state.SlotCount ? state.GetSlot((byte)i) : default;
                into[i] = slot.HasItem && slot.Quantity > 0 ? new Stack(slot.ItemId, slot.Quantity) : Stack.Empty;
            }
        }

        private static InventorySlot[] Write(Stack[] stacks)
        {
            var slots = new InventorySlot[stacks.Length];
            for (int i = 0; i < stacks.Length; i++)
            {
                if (stacks[i].IsEmpty)
                    continue;
                slots[i] = new InventorySlot { HasItem = true, ItemId = stacks[i].Item, Quantity = (byte)Mathf.Clamp(stacks[i].Count, 1, 255) };
            }
            return slots;
        }
    }

    [HarmonyPatch(typeof(PowerServerTracker), nameof(PowerServerTracker.UpdatePowerTransfers))]
    internal static class MachineSimulationPatch
    {
        private static void Prefix(PowerServerTracker __instance, float deltaTime)
        {
            try
            {
                MachineSimulation.BeforeTransfers(__instance, deltaTime);
            }
            catch (System.Exception e)
            {
                ErrorLog.Report("machines", e);
            }
        }

        private static void Postfix(float deltaTime)
        {
            try
            {
                MachineSimulation.AfterTransfers(deltaTime);
            }
            catch (System.Exception e)
            {
                ErrorLog.Report("machines", e);
            }
        }
    }
}
