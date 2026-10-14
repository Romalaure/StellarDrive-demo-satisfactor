using System.Collections.Generic;
using SDModKit.Game;
using Ships;
using Ships.Interface.Model.Parts;
using Ships.Interface.Model.Parts.StateTypes;
using Ships.Parts.Common;
using StellarIndustries.Core;
using StellarIndustries.Production;
using UnityEngine;

namespace StellarIndustries.Logistics
{
    /// <summary>
    /// Host-side conveyors: belts, lifts, splitters and mergers move items between the parts that
    /// touch their ends, inside one ship, in the game's power tick (no power needed). A conveyor
    /// pulls from the part behind it (a machine gives only its products, a chest anything) and
    /// pushes into the part in front of it (a machine takes only what its recipes need, see
    /// <see cref="ConveyorRules"/>; the rules apply through the move itself, SDModKit's
    /// ItemMove.Transfer, which <see cref="ItemMoveTransferPatch"/> filters). Conveyors never
    /// pull from each other: each one pushes into the next, so a line of belts carries items one
    /// segment after another. A conveyor holds a few items at most; a full one stops the line
    /// behind it, like a real belt. The game saves and sends the items on the belts itself (they
    /// are in the part's slots). The parts at both ends are looked up every two seconds, from the
    /// boxes of the ship's parts.
    /// </summary>
    internal static class Conveyors
    {
        private struct Seen
        {
            public TrackedShipServer Ship;
            public StatefulPart Part;
            public MachineDef Def;
            public MachineSimulation.Runtime Runtime;
        }

        private sealed class Flow
        {
            public float In, Out;
            public int Next;
            public float LastMoved = -10f, LinkedAt = -10f;
            public ushort[] Inputs = new ushort[0], Outputs = new ushort[0];
            public string InName, OutName;
        }

        /// <summary>How far past its end a conveyor looks for the next part (meters).</summary>
        private const float Reach = 0.2f;

        private static readonly List<Seen> Belts = new List<Seen>();
        private static readonly Dictionary<(uint, ushort), Flow> Flows = new Dictionary<(uint, ushort), Flow>();
        private static readonly List<ushort> Found = new List<ushort>();

        public static void Collect(TrackedShipServer ship, StatefulPart part, MachineDef def, MachineSimulation.Runtime rt)
        {
            Belts.Add(new Seen { Ship = ship, Part = part, Def = def, Runtime = rt });
        }

        public static void Forget()
        {
            Belts.Clear();
            Flows.Clear();
        }

        public static void Run(float deltaTime)
        {
            float now = Time.unscaledTime;
            foreach (Seen belt in Belts)
            {
                if (!Flows.TryGetValue((belt.Ship.Id, belt.Part.Id), out Flow flow))
                    Flows[(belt.Ship.Id, belt.Part.Id)] = flow = new Flow();
                if (now - flow.LinkedAt > 2f)
                    Link(belt, flow, now);
                Move(belt, flow, deltaTime, now);
            }
            Belts.Clear();
            if (Flows.Count > 4096)
                Flows.Clear();
        }

        private static void Move(Seen belt, Flow flow, float deltaTime, float now)
        {
            MachineDef def = belt.Def;
            TrackedShipServer ship = belt.Ship;
            ushort self = belt.Part.Id;
            float cap = Mathf.Max(1f, def.ConveyorRate * 2f);
            flow.In = Mathf.Min(flow.In + def.ConveyorRate * deltaTime, cap);
            flow.Out = Mathf.Min(flow.Out + def.ConveyorRate * deltaTime, cap);
            // Pull from the machines and chests behind (other conveyors push by themselves)
            foreach (ushort from in flow.Inputs)
            {
                int budget = Mathf.Min(Mathf.FloorToInt(flow.In), def.ConveyorHold - Carried(ship, self));
                if (budget <= 0)
                    break;
                if (IsConveyor(ship, from))
                    continue;
                int moved = ItemMove.Transfer(ship, from, self, budget, null);
                if (moved > 0)
                {
                    flow.In -= moved;
                    flow.LastMoved = now;
                }
            }
            // Push forward; a splitter hands items out one at a time, in turn, skipping a blocked side
            int left = Mathf.FloorToInt(flow.Out);
            ushort[] outs = flow.Outputs;
            if (outs.Length == 1)
            {
                int moved = Give(ship, self, outs[0], left);
                if (moved > 0)
                {
                    flow.Out -= moved;
                    flow.LastMoved = now;
                }
            }
            else if (outs.Length > 1)
            {
                bool progress = true;
                while (left > 0 && progress)
                {
                    progress = false;
                    for (int k = 0; k < outs.Length && left > 0; k++)
                    {
                        int index = (flow.Next + k) % outs.Length;
                        if (Give(ship, self, outs[index], 1) == 1)
                        {
                            left--;
                            flow.Out -= 1f;
                            flow.LastMoved = now;
                            flow.Next = (index + 1) % outs.Length;
                            progress = true;
                            break;
                        }
                    }
                }
            }
            MachineSimulation.Runtime rt = belt.Runtime;
            rt.Recipe = null;
            rt.Watts = 0f;
            rt.Status = now - flow.LastMoved < 1.5f ? MachineStatus.Producing : MachineStatus.Idle;
            rt.Detail = Lang.T($"Débit : {def.ConveyorRate:0.#} objet(s)/s, {Carried(ship, self)}/{def.ConveyorHold} sur le tapis", $"Rate: {def.ConveyorRate:0.#} item(s)/s, {Carried(ship, self)}/{def.ConveyorHold} on the belt") +
                        "\n" + Lang.T("Entrée : ", "In: ") + (flow.InName ?? Lang.T("<color=#f84>rien</color>", "<color=#f84>nothing</color>")) +
                        "\n" + Lang.T("Sortie : ", "Out: ") + (flow.OutName ?? Lang.T("<color=#f84>rien</color>", "<color=#f84>nothing</color>"));
        }

        /// <summary>Moves up to <paramref name="budget"/> items from a conveyor into the next part (a conveyor only takes what it has room for).</summary>
        private static int Give(TrackedShipServer ship, ushort from, ushort to, int budget)
        {
            if (budget <= 0)
                return 0;
            if (ship.TryGetStatefulPart(to, out StatefulPart next) && next?.Settings != null && MachineCatalog.TryGet(next.Settings.id, out MachineDef nextDef) && nextDef.IsConveyor)
                budget = Mathf.Min(budget, nextDef.ConveyorHold - ItemMove.Total(next.State as IStoragePart));
            return budget > 0 ? ItemMove.Transfer(ship, from, to, budget, null) : 0;
        }

        private static int Carried(TrackedShipServer ship, ushort part) =>
            ship.TryGetStatefulPart(part, out StatefulPart p) ? ItemMove.Total(p.State as IStoragePart) : 0;

        private static bool IsConveyor(TrackedShipServer ship, ushort part) =>
            ship.TryGetStatefulPart(part, out StatefulPart p) && p?.Settings != null && MachineCatalog.TryGet(p.Settings.id, out MachineDef d) && d.IsConveyor;

        // ---- Which parts touch the conveyor's ends

        private static void Link(Seen belt, Flow flow, float now)
        {
            flow.LinkedAt = now;
            PartBox box = PartGeometry.Box(belt.Part.Settings, belt.Part.LocalPosition, belt.Part.LocalRotation, 0.5f);
            Vector3 h = box.Half;
            var ins = new List<Vector3[]>();
            var outs = new List<Vector3[]>();
            Vector3 back = new Vector3(0f, 0f, -h.z - Reach), front = new Vector3(0f, 0f, h.z + Reach);
            Vector3 right = new Vector3(h.x + Reach, 0f, 0f), left = new Vector3(-h.x - Reach, 0f, 0f);
            // Belt-height points sit low in the box (the bottom fifth) so a low belt still finds a machine on the same floor
            Vector3 low = new Vector3(0f, -h.y * 0.5f, 0f);
            switch (belt.Def.Conveyor)
            {
                case ConveyorKind.Lift:
                    // Foot: just above the floor; top: just below and just above the lift's top (a machine, or a belt on the deck above)
                    ins.Add(new[] { back + new Vector3(0f, -h.y + 0.15f, 0f) });
                    outs.Add(new[] { front + new Vector3(0f, h.y + 0.15f, 0f), front + new Vector3(0f, h.y - 0.15f, 0f) });
                    break;
                case ConveyorKind.Splitter:
                    ins.Add(new[] { back + low, back });
                    outs.Add(new[] { front + low, front });
                    outs.Add(new[] { left + low, left });
                    outs.Add(new[] { right + low, right });
                    break;
                case ConveyorKind.Merger:
                    ins.Add(new[] { back + low, back });
                    ins.Add(new[] { left + low, left });
                    ins.Add(new[] { right + low, right });
                    outs.Add(new[] { front + low, front });
                    break;
                default:
                    ins.Add(new[] { back + low * 0.5f, back });
                    outs.Add(new[] { front + low * 0.5f, front });
                    break;
            }
            flow.Inputs = Resolve(belt, box, ins, out flow.InName);
            flow.Outputs = Resolve(belt, box, outs, out flow.OutName);
            if (flow.Next >= flow.Outputs.Length)
                flow.Next = 0;
        }

        private static ushort[] Resolve(Seen belt, PartBox box, List<Vector3[]> sides, out string names)
        {
            Found.Clear();
            var labels = new List<string>();
            foreach (Vector3[] candidates in sides)
            {
                foreach (Vector3 local in candidates)
                {
                    StatefulPart hit = PartAt(belt, box.Center + box.Rotation * local);
                    if (hit == null)
                        continue;
                    if (!Found.Contains(hit.Id))
                    {
                        Found.Add(hit.Id);
                        labels.Add(hit.Settings != null ? hit.Settings.fullLabel : "?");
                    }
                    break;
                }
            }
            names = labels.Count > 0 ? string.Join(", ", labels) : null;
            return Found.ToArray();
        }

        /// <summary>The part with an inventory whose box holds this ship-frame point (the nearest centre if several do).</summary>
        private static StatefulPart PartAt(Seen belt, Vector3 point)
        {
            StatefulPart best = null;
            float bestDistance = float.MaxValue;
            foreach (StatefulPart other in belt.Ship.StatefulParts)
            {
                if (other == null || other.Id == belt.Part.Id || other.Settings == null || !(other.State is IStoragePart))
                    continue;
                PartBox b = PartGeometry.Box(other.Settings, other.LocalPosition, other.LocalRotation, 0.5f);
                if ((point - b.Center).sqrMagnitude > b.Half.sqrMagnitude + 0.01f)
                    continue;
                Vector3 local = Quaternion.Inverse(b.Rotation) * (point - b.Center);
                if (Mathf.Abs(local.x) > b.Half.x + 0.01f || Mathf.Abs(local.y) > b.Half.y + 0.01f || Mathf.Abs(local.z) > b.Half.z + 0.01f)
                    continue;
                float d = (point - b.Center).sqrMagnitude;
                if (d < bestDistance)
                {
                    bestDistance = d;
                    best = other;
                }
            }
            return best;
        }
    }
}
