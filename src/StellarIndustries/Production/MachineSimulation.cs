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
            /// <summary>Accumulator: stored energy (joules), whether it was read from the save, and this tick's charge and discharge.</summary>
            public float Stored, Request;
            public bool Loaded;
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
        private static readonly List<Entry> Storing = new List<Entry>();
        private static readonly List<(uint, ushort)> Stale = new List<(uint, ushort)>();
        private static readonly Stack[] Buffer = new Stack[10];
        private static ShipsServerTracker _ships;
        private static float _nextPrune;

        /// <summary>Host only: every simulated machine.</summary>
        public static IEnumerable<KeyValuePair<(uint, ushort), Runtime>> All => Runtimes;

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
            Storing.Clear();
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
                    if (def.Stores)
                        Storing.Add(new Entry { Ship = ship, Part = part, Def = def, Runtime = rt });
                    else if (def.Dock)
                        Logistics.LoadingDocks.Collect(ship, part, def, rt);
                    else if (def.IsConveyor)
                        Logistics.Conveyors.Collect(ship, part, def, rt);
                    else if (def.Container)
                        rt.Status = MachineStatus.Idle;
                    else if (def.Radar)
                        rt.Status = MachineStatus.Producing;
                    else if (def.Generates)
                        PrepareGenerator(power, ship, part, def, rt, deltaTime);
                    else
                        Prepare(power, ship, part, def, rt, deltaTime);
                }
            }
            // Accumulators last: they balance what every consumer and producer of their network asked and offered
            foreach (Entry entry in Storing)
                PrepareAccumulator(power, entry, deltaTime);
            Logistics.LoadingDocks.Run(deltaTime);
            Logistics.Conveyors.Run(deltaTime);
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
                    rt.Detail = Lang.T($"Aucun rocher minable à moins de {def.ExtractRadius:0} m", $"No minable rock within {def.ExtractRadius:0} m");
                    return;
                }
                Deposits.Deposit d = deposit.Value;
                rt.Detail = Lang.T($"Gisement : {d.Name} {Deposits.PurityName(d.Purity)} à {d.Distance:0.0} m ({DepositPurity.Factor(d.Purity):0.#}× la vitesse)",
                    $"Deposit: {Deposits.PurityName(d.Purity)} {d.Name} at {d.Distance:0.0} m ({DepositPurity.Factor(d.Purity):0.#}× speed)");
                RecipeDef extraction = RecipeBook.Extraction(def.PartId, d.Item, def.ExtractSeconds / DepositPurity.Factor(d.Purity), IndustrialItems.NameOf(d.Item), d.Purity.ToString());
                recipe = RecipeSelector.Pick(Buffer, new[] { extraction }, rt.Recipe, IndustrialItems.MaxStack, out status);
            }
            else if (def.FixedOutput != 0)
            {
                // A condenser makes its item from nothing but power
                RecipeDef make = RecipeBook.Extraction(def.PartId, def.FixedOutput, def.ExtractSeconds, IndustrialItems.NameOf(def.FixedOutput));
                recipe = RecipeSelector.Pick(Buffer, new[] { make }, rt.Recipe, IndustrialItems.MaxStack, out status);
            }
            else if (def.Sinks)
            {
                // The recycler takes the items in slot order, vouchers excepted
                var sinks = new List<RecipeDef>();
                foreach (Stack s in Buffer)
                {
                    if (!s.IsEmpty && s.Item != ResourceCatalog.Voucher)
                        sinks.Add(RecipeBook.Recycling(def.PartId, s.Item, IndustrialItems.NameOf(s.Item)));
                }
                recipe = RecipeSelector.Pick(Buffer, sinks.ToArray(), rt.Recipe, IndustrialItems.MaxStack, out status);
                rt.Detail = recipe != null
                    ? Lang.T($"Valeur : {ItemValues.Of(recipe.Inputs[0].Item):0.##} point(s) par objet, 100 points = 1 bon", $"Value: {ItemValues.Of(recipe.Inputs[0].Item):0.##} point(s) per item, 100 points = 1 voucher")
                    : null;
            }
            else
            {
                RecipeDef chosen = MachineControl.Chosen(ship.Id, part.Id, def.PartId);
                recipe = RecipeSelector.Pick(Buffer, RecipeBook.Available(def.PartId), rt.Recipe, chosen, IndustrialItems.MaxStack, out status);
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

        // ---- Accumulators

        public const string ChargePrefix = "charge=";

        /// <summary>Charge (joules) kept for a machine in the companion file, or 0.</summary>
        private static float SavedCharge(uint ship, ushort part)
        {
            string value = MachineControl.Choices.Get(ship, part);
            return value != null && value.StartsWith(ChargePrefix) &&
                   float.TryParse(value.Substring(ChargePrefix.Length), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float j) ? j : 0f;
        }

        /// <summary>Before saving: every accumulator's charge goes into the companion file.</summary>
        public static void KeepCharges()
        {
            foreach (KeyValuePair<(uint, ushort), Runtime> pair in Runtimes)
            {
                if (pair.Value.Loaded)
                    MachineControl.Choices.Set(pair.Key.Item1, pair.Key.Item2, ChargePrefix + Mathf.RoundToInt(pair.Value.Stored).ToString(System.Globalization.CultureInfo.InvariantCulture));
            }
        }

        /// <summary>
        /// An accumulator covers its network's shortfall from its charge, or charges from its spare
        /// capacity (which makes the generators really give more). Energy is conserved.
        /// </summary>
        private static void PrepareAccumulator(PowerServerTracker power, Entry entry, float deltaTime)
        {
            Runtime rt = entry.Runtime;
            MachineDef def = entry.Def;
            if (!rt.Loaded)
            {
                rt.Stored = Mathf.Clamp(SavedCharge(entry.Ship.Id, entry.Part.Id), 0f, def.StoreJoules);
                rt.Loaded = true;
            }
            rt.Recipe = null;
            rt.Offered = rt.Request = 0f;
            rt.Watts = def.Watts;
            rt.Detail = Lang.T("Charge : ", "Charge: ") + $"{rt.Stored / 1e6f:0.0} / {def.StoreJoules / 1e6f:0} MJ ({rt.Stored / def.StoreJoules * 100f:0} %)";
            if (!power.TryGetGroupFromSocket(new EntityPartSocket(entry.Ship.Id, entry.Part.Id, PowerSocket), out PowerGroupServerState group))
            {
                rt.Status = MachineStatus.Unpowered;
                return;
            }
            rt.Group = group;
            float step = def.Watts * deltaTime;
            float balance = group.TotalPowerProductionCapacity - group.TotalPowerNeeded;
            if (balance < 0f && rt.Stored > 0f)
            {
                rt.Offered = Mathf.Min(-balance, step, rt.Stored);
                group.TotalPowerProductionCapacity += rt.Offered;
            }
            else if (balance > 0f && rt.Stored < def.StoreJoules)
            {
                rt.Request = Mathf.Min(balance, step, def.StoreJoules - rt.Stored);
                group.TotalPowerNeeded += rt.Request;
            }
        }

        private static void AfterAccumulators(float deltaTime)
        {
            foreach (Entry entry in Storing)
            {
                Runtime rt = entry.Runtime;
                if (rt.Group == null)
                    continue;
                float given = rt.Offered * Mathf.Clamp01(rt.Group.NormalizedGeneratorRate);
                float taken = rt.Request * Mathf.Clamp01(rt.Group.NormalizedConsumerRate);
                rt.Stored = Mathf.Clamp(rt.Stored + taken - given, 0f, entry.Def.StoreJoules);
                rt.PowerRate = Mathf.Clamp01((given + taken) / (entry.Def.Watts * deltaTime));
                rt.Status = given > 0f || taken > 0f ? MachineStatus.Producing : MachineStatus.Standby;
                rt.Detail = (given > 0f ? Lang.T("Décharge", "Discharging") : taken > 0f ? Lang.T("Charge", "Charging") : Lang.T("En attente", "Idle")) +
                            $" — {rt.Stored / 1e6f:0.0} / {entry.Def.StoreJoules / 1e6f:0} MJ ({rt.Stored / entry.Def.StoreJoules * 100f:0} %)";
            }
        }

        // ---- Generators

        private static readonly Func<uint, float> FuelEnergy = MachineCatalog.FuelEnergy;

        private static Func<uint, float> EnergyOf(MachineDef def) => item => MachineCatalog.FuelEnergy(def, item);

        /// <summary>A generator offers its network what its fuel can give this tick, up to its rating.</summary>
        private static void PrepareGenerator(PowerServerTracker power, TrackedShipServer ship, StatefulPart part, MachineDef def, Runtime rt, float deltaTime)
        {
            rt.Recipe = null;
            rt.Offered = 0f;
            Read((StorageState)part.State, Buffer);
            Func<uint, float> energy = EnergyOf(def);
            float available = FuelBurner.Available(Buffer, rt.Burned, energy);
            rt.Detail = FuelText(def, Buffer, available, def.Watts);
            if (available <= 0f)
            {
                rt.Status = MachineStatus.NoFuel;
                rt.Watts = 0f;
                return;
            }
            // A reactor needs room for the waste of the rod it burns
            if (def.WasteItem != 0 && !FuelBurner.HasRoom(Buffer, def.WasteItem, IndustrialItems.MaxStack(def.WasteItem)))
            {
                rt.Status = MachineStatus.OutputBlocked;
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
                int burnt = FuelBurner.Consume(Buffer, ref rt.Burned, EnergyOf(entry.Def), out bool changed);
                if (burnt > 0 && entry.Def.WasteItem != 0)
                    FuelBurner.Add(Buffer, entry.Def.WasteItem, burnt, IndustrialItems.MaxStack(entry.Def.WasteItem));
                if (changed)
                    entry.Ship.UpdatePartState(entry.Part.Id, state with { InventorySlots = Write(Buffer) });
            }
            Burning.Clear();
        }

        private static string FuelText(MachineDef def, Stack[] slots, float joules, float watts)
        {
            int count = 0;
            foreach (Stack s in slots)
            {
                if (!s.IsEmpty && MachineCatalog.FuelEnergy(def, s.Item) > 0f)
                    count += s.Count;
            }
            if (count == 0)
                return def.OwnFuels != null
                    ? Lang.T("Combustible : aucune barre de combustible stellaire", "Fuel: no stellar fuel rod")
                    : Lang.T("Combustible : aucun (bois, biomasse, briquettes, charbon de bois)", "Fuel: none (logs, biomass, bricks, charcoal)");
            float seconds = watts > 0f ? joules / watts : 0f;
            return Lang.T($"Combustible : {count} objet(s), {Mathf.RoundToInt(joules / 1000f)} kJ (≈ {Mathf.RoundToInt(seconds)} s à pleine charge)",
                $"Fuel: {count} item(s), {Mathf.RoundToInt(joules / 1000f)} kJ (≈ {Mathf.RoundToInt(seconds)} s at full load)");
        }

        public static void AfterTransfers(float deltaTime)
        {
            if (deltaTime <= 0f)
            {
                Burning.Clear();
                return;
            }
            AfterGenerators(deltaTime);
            AfterAccumulators(deltaTime);
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
            Diagnostics.FactoryOverview.Counted(1);
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

        public static InventorySlot[] Write(Stack[] stacks)
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
            Diagnostics.FactoryOverview.BeginTick();
            try
            {
                MachineSimulation.BeforeTransfers(__instance, deltaTime);
            }
            catch (System.Exception e)
            {
                ErrorLog.Report("machines", e);
            }
            Diagnostics.FactoryOverview.Pause();
        }

        private static void Postfix(float deltaTime)
        {
            Diagnostics.FactoryOverview.Resume();
            try
            {
                MachineSimulation.AfterTransfers(deltaTime);
            }
            catch (System.Exception e)
            {
                ErrorLog.Report("machines", e);
            }
            Diagnostics.FactoryOverview.EndTick();
        }
    }
}
