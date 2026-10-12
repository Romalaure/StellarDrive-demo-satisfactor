using System.Collections.Generic;
using Ships;
using Ships.Interface.Model.Parts;
using Ships.Interface.Model.Parts.State;
using StellarIndustries.Content;
using StellarIndustries.Production;
using StellarIndustries.Core;
using UnityEngine;

namespace StellarIndustries.Logistics
{
    /// <summary>
    /// Host-side transfers between loading docks, once a second. A dock set to "send" empties
    /// itself into the nearest dock set to "receive" within its range, whatever ship that dock is
    /// on (Item Lines' conveyors stay inside one ship; docks join a cargo ship to a base). Both
    /// docks' slots are written in the same tick (see <see cref="ItemTransfer"/>). Positions are
    /// the ships' shown positions on the host, so both ships must be loaded near a player.
    /// </summary>
    internal static class LoadingDocks
    {
        public const string Send = "dock.send", Receive = "dock.receive";

        private struct Dock
        {
            public TrackedShipServer Ship;
            public StatefulPart Part;
            public MachineDef Def;
            public MachineSimulation.Runtime Runtime;
            public string Mode;
            public Vector3? Position;
        }

        private static readonly List<Dock> Seen = new List<Dock>();
        private static readonly Production.Stack[] From = new Production.Stack[10], To = new Production.Stack[10];
        private static float _elapsed;

        public static string ModeName(string mode) => mode == Send ? Lang.T("envoi", "send") : mode == Receive ? Lang.T("réception", "receive") : Lang.T("arrêt", "off");

        /// <summary>Called for every dock found in the tick (before <see cref="Run"/>).</summary>
        public static void Collect(TrackedShipServer ship, StatefulPart part, MachineDef def, MachineSimulation.Runtime rt)
        {
            Seen.Add(new Dock { Ship = ship, Part = part, Def = def, Runtime = rt, Mode = MachineControl.Choices.Get(ship.Id, part.Id) });
        }

        public static void Run(float deltaTime)
        {
            _elapsed += deltaTime;
            if (_elapsed < 1f)
            {
                Seen.Clear();
                return;
            }
            float seconds = Mathf.Min(_elapsed, 2f);
            _elapsed = 0f;
            for (int i = 0; i < Seen.Count; i++)
            {
                Dock d = Seen[i];
                d.Position = d.Mode == Send || d.Mode == Receive ? Deposits.WorldPosition(d.Ship.Id, d.Part) : null;
                Seen[i] = d;
                d.Runtime.Recipe = null;
                d.Runtime.Watts = 0f;
                d.Runtime.Status = d.Mode == Send || d.Mode == Receive ? MachineStatus.Idle : MachineStatus.Idle;
                d.Runtime.Detail = Lang.T("Mode : ", "Mode: ") + ModeName(d.Mode);
            }
            foreach (Dock sender in Seen)
            {
                if (sender.Mode != Send || sender.Position == null)
                    continue;
                Dock? target = null;
                float best = sender.Def.DockRange;
                foreach (Dock other in Seen)
                {
                    if (other.Mode != Receive || other.Position == null || (other.Ship.Id == sender.Ship.Id && other.Part.Id == sender.Part.Id))
                        continue;
                    float d = Vector3.Distance(sender.Position.Value, other.Position.Value);
                    if (d <= best)
                    {
                        best = d;
                        target = other;
                    }
                }
                if (target == null)
                {
                    sender.Runtime.Detail = Lang.T($"Mode : envoi — aucun quai en réception à moins de {sender.Def.DockRange:0} m", $"Mode: send — no receiving dock within {sender.Def.DockRange:0} m");
                    continue;
                }
                Dock to = target.Value;
                if (!(sender.Part.State is StorageState fromState) || !(to.Part.State is StorageState toState))
                    continue;
                MachineSimulation.Read(fromState, From);
                MachineSimulation.Read(toState, To);
                int moved = ItemTransfer.Move(From, To, Mathf.Max(1, Mathf.RoundToInt(sender.Def.DockRate * seconds)), IndustrialItems.MaxStack);
                string where = to.Ship.Id == sender.Ship.Id ? Lang.T("même vaisseau", "same ship") : Lang.T("vaisseau ", "ship ") + to.Ship.Id;
                sender.Runtime.Detail = Lang.T($"Mode : envoi vers le quai {to.Part.Id} ({where}, {best:0.0} m)", $"Mode: sending to dock {to.Part.Id} ({where}, {best:0.0} m)") +
                                        (moved == 0 ? Lang.T(" — rien à envoyer ou quai plein", " — nothing to send or dock full") : Lang.T($" — {moved} objet(s) envoyé(s)", $" — {moved} item(s) sent"));
                string from = to.Ship.Id == sender.Ship.Id ? Lang.T("même vaisseau", "same ship") : Lang.T("vaisseau ", "ship ") + sender.Ship.Id;
                to.Runtime.Detail = Lang.T($"Mode : réception depuis le quai {sender.Part.Id} ({from})", $"Mode: receiving from dock {sender.Part.Id} ({from})");
                if (moved > 0)
                {
                    sender.Runtime.Status = MachineStatus.Producing;
                    to.Runtime.Status = MachineStatus.Producing;
                    sender.Runtime.Cycles += moved;
                    sender.Ship.UpdatePartState(sender.Part.Id, fromState with { InventorySlots = MachineSimulation.Write(From) });
                    to.Ship.UpdatePartState(to.Part.Id, toState with { InventorySlots = MachineSimulation.Write(To) });
                }
            }
            Seen.Clear();
        }

        public static void Forget()
        {
            Seen.Clear();
            _elapsed = 0f;
        }
    }
}
