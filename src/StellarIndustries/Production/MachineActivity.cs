using System.Collections.Generic;
using System.Text;
using FishNet;
using StellarIndustries.Core;
using UnityEngine;

namespace StellarIndustries.Production
{
    /// <summary>
    /// Which machines are working right now, for their animations and sound. The host reads its
    /// simulation; once a second it sends the list of working machines to the clients (one short
    /// text, only when it changed or every 5 s), so a client animates exactly what the host runs.
    /// </summary>
    internal static class MachineActivity
    {
        private static readonly HashSet<(uint, ushort)> Remote = new HashSet<(uint, ushort)>();
        private static readonly StringBuilder Text = new StringBuilder();
        private static float _nextSend, _lastFull;
        private static string _lastSent;

        public static void Install()
        {
            SINet.OnClient(SIMessageKind.Active, (a, b, text) =>
            {
                Remote.Clear();
                if (string.IsNullOrEmpty(text))
                    return;
                foreach (string entry in text.Split(';'))
                {
                    int colon = entry.IndexOf(':');
                    if (colon > 0 && uint.TryParse(entry.Substring(0, colon), out uint ship) && ushort.TryParse(entry.Substring(colon + 1), out ushort part))
                        Remote.Add((ship, part));
                }
            });
        }

        public static bool IsActive(uint ship, ushort part)
        {
            if (SINet.IsServer)
            {
                MachineSimulation.Runtime rt = MachineSimulation.Get(ship, part);
                return rt != null && Working(rt.Status);
            }
            return Remote.Contains((ship, part));
        }

        public static bool Working(MachineStatus status) => status == MachineStatus.Producing || status == MachineStatus.Underpowered;

        /// <summary>Host: sends the working machines to the clients.</summary>
        public static void Update()
        {
            if (!SINet.IsServer || Time.unscaledTime < _nextSend)
                return;
            _nextSend = Time.unscaledTime + 1f;
            Text.Clear();
            foreach (var pair in MachineSimulation.All)
            {
                if (!Working(pair.Value.Status))
                    continue;
                if (Text.Length > 0)
                    Text.Append(';');
                Text.Append(pair.Key.Item1).Append(':').Append(pair.Key.Item2);
            }
            string text = Text.ToString();
            if (text == _lastSent && Time.unscaledTime - _lastFull < 5f)
                return;
            _lastSent = text;
            _lastFull = Time.unscaledTime;
            SINet.SendToClients(SIMessageKind.Active, 0, 0, text);
        }

        public static void Forget()
        {
            Remote.Clear();
            _lastSent = null;
        }
    }
}
