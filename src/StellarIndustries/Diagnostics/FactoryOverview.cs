using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Core.Services;
using FishNet;
using FishNet.Connection;
using Ships;
using Ships.Interface.Model.Parts;
using Ships.Interface.Model.Parts.State;
using Ships.Services;
using StellarIndustries.Core;
using StellarIndustries.Production;
using UnityEngine;
using UnityEngine.InputSystem;

namespace StellarIndustries.Diagnostics
{
    /// <summary>
    /// The factory overview (F7 by default): every machine the host simulates, counted by state,
    /// the power they draw and give, the products made in the last minute, the machines that need
    /// a player (no cable, no power, full, no deposit, no fuel), and what the simulation costs per
    /// tick. Built on the host; a client asks the host for the text.
    /// </summary>
    internal static class FactoryOverview
    {
        private static readonly Stopwatch Watch = new Stopwatch();
        private static double _tickMs, _peakMs;
        private static int _ticks;
        private static float _windowStart;
        private static double _windowMs;
        private static int _windowTicks;

        private static readonly Queue<(float Time, int Cycles)> Made = new Queue<(float, int)>();
        private static int _madeTotal;

        private static Key _key = Key.None;
        private static string _keyName;
        private static bool _open;
        private static string _text;
        private static float _nextRefresh;
        private static GUIStyle _style;
        private static Vector2 _scroll;

        /// <summary>Average and peak cost of the simulation per power tick over the last 5 s (milliseconds).</summary>
        public static double AverageMs => _tickMs;
        public static double PeakMs => _peakMs;

        public static void Install()
        {
            SINet.OnServer(SIMessageKind.OverviewRequest, (NetworkConnection sender, uint a, ushort b, string text) =>
            {
                if (sender != null && !sender.IsLocalClient)
                    SINet.SendTo(sender, SIMessageKind.Overview, 0, 0, Build());
            });
            SINet.OnClient(SIMessageKind.Overview, (a, b, text) => _text = text);
        }

        // ---- Cost of the simulation

        public static void BeginTick() => Watch.Restart();

        /// <summary>Between the prefix and the postfix of the power tick, the game's own work is not counted.</summary>
        public static void Pause() => Watch.Stop();

        public static void Resume() => Watch.Start();

        public static void EndTick()
        {
            Watch.Stop();
            double ms = Watch.Elapsed.TotalMilliseconds;
            _windowMs += ms;
            _windowTicks++;
            _peakMs = Math.Max(_peakMs, ms);
            float now = Time.unscaledTime;
            if (now - _windowStart >= 5f)
            {
                _tickMs = _windowTicks > 0 ? _windowMs / _windowTicks : 0;
                _ticks = _windowTicks;
                _windowMs = 0;
                _windowTicks = 0;
                _windowStart = now;
                if (!_open)
                    _peakMs = 0;
            }
        }

        /// <summary>A machine finished cycles (products made, items moved by a dock).</summary>
        public static void Counted(int cycles)
        {
            Made.Enqueue((Time.unscaledTime, cycles));
            _madeTotal += cycles;
        }

        // ---- Overlay

        public static void Update()
        {
            if (_key == Key.None || _keyName != Settings.OverviewKey.Value)
            {
                _keyName = Settings.OverviewKey.Value;
                if (!Enum.TryParse(_keyName?.Trim(), true, out _key) || _key == Key.None)
                    _key = Key.F7;
            }
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard[_key].wasPressedThisFrame)
            {
                _open = !_open;
                _nextRefresh = 0f;
                _peakMs = 0;
            }
            if (!_open || Time.unscaledTime < _nextRefresh)
                return;
            _nextRefresh = Time.unscaledTime + 1f;
            if (InstanceFinder.IsServerStarted)
                _text = Build();
            else
                SINet.SendToServer(SIMessageKind.OverviewRequest, 0, 0, "");
        }

        public static string Build()
        {
            float now = Time.unscaledTime;
            while (Made.Count > 0 && now - Made.Peek().Time > 60f)
                _madeTotal -= Made.Dequeue().Cycles;
            var text = new StringBuilder();
            text.Append(Lang.T("<b>Tableau de bord industriel</b>", "<b>Factory overview</b>"));
            ShipsServerTracker ships = ServiceLocator.GetService<ShipsServerTracker>();
            if (ships == null)
                return text.Append(Lang.T("\nIndisponible (pas dans un monde).", "\nNot available (not in a world).")).ToString();
            var byStatus = new Dictionary<MachineStatus, int>();
            var problems = new List<string>();
            int machines = 0;
            float drawn = 0f, given = 0f;
            foreach (TrackedShipServer ship in ships.LoadedShipsWithChildren)
            {
                foreach (StatefulPart part in ship.GetAllStatefulParts<StorageState>())
                {
                    if (part?.Settings == null || !MachineCatalog.TryGet(part.Settings.id, out MachineDef def) || def.IsLogistics)
                        continue;
                    MachineSimulation.Runtime rt = MachineSimulation.Get(ship.Id, part.Id);
                    if (rt == null)
                        continue;
                    machines++;
                    byStatus.TryGetValue(rt.Status, out int n);
                    byStatus[rt.Status] = n + 1;
                    if (def.Generates)
                        given += rt.Watts * rt.PowerRate;
                    else if (rt.Status == MachineStatus.Producing || rt.Status == MachineStatus.Underpowered)
                        drawn += rt.Watts * rt.PowerRate;
                    if (IsProblem(rt.Status) && problems.Count < 12)
                        problems.Add(Lang.T($"{def.Name} (vaisseau {ship.Id}, pièce {part.Id}) : ", $"{Lang.MachineName(def)} (ship {ship.Id}, part {part.Id}): ") + UI.MachineHud.StatusText(rt.Status));
                }
            }
            text.Append(Lang.T("\nMachines simulées : ", "\nMachines running: ")).Append(machines);
            foreach (var pair in byStatus.OrderBy(p => p.Key))
                text.Append("\n  ").Append(UI.MachineHud.StatusText(pair.Key)).Append(" : ").Append(pair.Value);
            text.Append(Lang.T("\nÉnergie consommée par les machines : ", "\nPower used by the machines: ")).Append(Mathf.RoundToInt(drawn)).Append(" W");
            text.Append(Lang.T("\nÉnergie donnée par les générateurs du mod : ", "\nPower given by the mod's generators: ")).Append(Mathf.RoundToInt(given)).Append(" W");
            text.Append(Lang.T("\nCycles terminés (dernière minute) : ", "\nCycles done (last minute): ")).Append(_madeTotal);
            if (problems.Count > 0)
            {
                text.Append(Lang.T("\n<b>À vérifier</b>", "\n<b>Needs attention</b>"));
                foreach (string p in problems)
                    text.Append("\n  ").Append(p);
            }
            text.Append(Lang.T($"\n<color=#aaa>Simulation : {_tickMs:0.000} ms par tick en moyenne ({_ticks} ticks / 5 s), pic {_peakMs:0.000} ms</color>",
                $"\n<color=#aaa>Simulation: {_tickMs:0.000} ms per tick on average ({_ticks} ticks / 5 s), peak {_peakMs:0.000} ms</color>"));
            if (!Settings.Enabled.Value)
                text.Append(Lang.T("\n<color=#f84>Production désactivée (Enabled = false)</color>", "\n<color=#f84>Production turned off (Enabled = false)</color>"));
            text.Append($"\n<color=#aaa>[{(_key == Key.None ? "F7" : _key.ToString())}] {Lang.T("fermer", "close")}</color>");
            return text.ToString();
        }

        private static bool IsProblem(MachineStatus s) =>
            s == MachineStatus.Unpowered || s == MachineStatus.NoPower || s == MachineStatus.OutputBlocked || s == MachineStatus.NoDeposit || s == MachineStatus.NoFuel || s == MachineStatus.Error;

        public static void OnGUI()
        {
            if (!_open || _text == null)
                return;
            if (_style == null)
                _style = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.UpperLeft, richText = true, fontSize = 15, wordWrap = false, padding = new RectOffset(12, 12, 10, 10) };
            var content = new GUIContent(_text);
            Vector2 size = _style.CalcSize(content);
            float height = Mathf.Min(size.y, Screen.height * 0.8f);
            var area = new Rect(Screen.width - size.x - 40f, 60f, size.x + 20f, height);
            GUILayout.BeginArea(area);
            _scroll = GUILayout.BeginScrollView(_scroll);
            GUILayout.Box(content, _style);
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        public static void Forget()
        {
            Made.Clear();
            _madeTotal = 0;
            _text = null;
        }
    }
}
