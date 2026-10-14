using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using Core.Services;
using FishNet;
using FishNet.Connection;
using Ships;
using Ships.Interface.Model.Parts;
using Ships.Interface.Model.Parts.State;
using Ships.Services;
using StellarIndustries.Content;
using StellarIndustries.Core;
using StellarIndustries.Production;
using StellarIndustries.Recipes;
using UnityEngine;
using UnityEngine.InputSystem;

namespace StellarIndustries.Diagnostics
{
    /// <summary>
    /// The factory overview (F7 by default): a panel with the machines the host simulates (how
    /// many, how many work, how many need a player), power drawn and given, the accumulators'
    /// charge, the machines' states as one bar, what was made in the last minute item by item,
    /// the machines by type, the conveyors, the problems to look at, and what the simulation
    /// costs per tick. Built on the host as a small snapshot; a client asks the host for it.
    /// </summary>
    internal static class FactoryOverview
    {
        /// <summary>One look at the factory, sent to clients as text.</summary>
        internal sealed class Snapshot
        {
            public int Machines, Working, Problems, Belts, BeltsMoving, Ticks;
            public float Drawn, Given, Stored, Capacity;
            public double TickMs, PeakMs;
            public bool Enabled = true;
            public readonly Dictionary<MachineStatus, int> ByStatus = new Dictionary<MachineStatus, int>();
            public readonly List<(uint Item, int Count)> Made = new List<(uint, int)>();
            public readonly List<(ushort Part, int Working, int Total)> Types = new List<(ushort, int, int)>();
            public readonly List<string> Issues = new List<string>();

            private static string F(float v) => v.ToString("0.#", CultureInfo.InvariantCulture);
            private static string F(double v) => v.ToString("0.###", CultureInfo.InvariantCulture);

            public string Encode()
            {
                var t = new StringBuilder();
                t.Append($"M|{Machines}|{Working}|{Problems}|{Belts}|{BeltsMoving}\n");
                t.Append($"P|{F(Drawn)}|{F(Given)}|{F(Stored)}|{F(Capacity)}\n");
                t.Append($"C|{F(TickMs)}|{F(PeakMs)}|{Ticks}|{(Enabled ? 1 : 0)}\n");
                foreach (var s in ByStatus)
                    t.Append($"S|{(int)s.Key}|{s.Value}\n");
                foreach (var m in Made)
                    t.Append($"I|{m.Item}|{m.Count}\n");
                foreach (var m in Types)
                    t.Append($"T|{m.Part}|{m.Working}|{m.Total}\n");
                foreach (string i in Issues)
                    t.Append("X|").Append(i.Replace("\n", " ").Replace("|", "/")).Append('\n');
                return t.ToString();
            }

            public static Snapshot Decode(string text)
            {
                var s = new Snapshot();
                foreach (string line in (text ?? "").Split('\n'))
                {
                    string[] f = line.Split('|');
                    int I(int k) => k < f.Length && int.TryParse(f[k], NumberStyles.Integer, CultureInfo.InvariantCulture, out int v) ? v : 0;
                    float R(int k) => k < f.Length && float.TryParse(f[k], NumberStyles.Float, CultureInfo.InvariantCulture, out float v) ? v : 0f;
                    switch (f[0])
                    {
                        case "M": s.Machines = I(1); s.Working = I(2); s.Problems = I(3); s.Belts = I(4); s.BeltsMoving = I(5); break;
                        case "P": s.Drawn = R(1); s.Given = R(2); s.Stored = R(3); s.Capacity = R(4); break;
                        case "C": s.TickMs = R(1); s.PeakMs = R(2); s.Ticks = I(3); s.Enabled = I(4) == 1; break;
                        case "S": s.ByStatus[(MachineStatus)I(1)] = I(2); break;
                        case "I": s.Made.Add(((uint)I(1), I(2))); break;
                        case "T": s.Types.Add(((ushort)I(1), I(2), I(3))); break;
                        case "X": if (f.Length > 1) s.Issues.Add(f[1]); break;
                    }
                }
                return s;
            }
        }

        private static readonly Stopwatch Watch = new Stopwatch();
        private static double _tickMs, _peakMs;
        private static int _ticks;
        private static float _windowStart;
        private static double _windowMs;
        private static int _windowTicks;

        private static readonly Queue<(float Time, uint Item, int Count)> Made = new Queue<(float, uint, int)>();

        private static Key _key = Key.None;
        private static string _keyName;
        private static bool _open;
        private static Snapshot _shown;
        private static float _nextRefresh;
        private static Vector2 _scroll;
        private static GUIStyle _title, _text, _small, _big, _right;

        /// <summary>Average and peak cost of the simulation per power tick over the last 5 s (milliseconds).</summary>
        public static double AverageMs => _tickMs;
        public static double PeakMs => _peakMs;

        /// <summary>Development: opens or closes the panel (in-game test screenshot).</summary>
        public static bool Shown
        {
            get => _open;
            set
            {
                _open = value;
                _nextRefresh = 0f;
            }
        }

        public static void Install()
        {
            SINet.OnServer(SIMessageKind.OverviewRequest, (NetworkConnection sender, uint a, ushort b, string text) =>
            {
                if (sender != null && !sender.IsLocalClient)
                    SINet.SendTo(sender, SIMessageKind.Overview, 0, 0, Take().Encode());
            });
            SINet.OnClient(SIMessageKind.Overview, (a, b, text) => _shown = Snapshot.Decode(text));
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

        /// <summary>A machine finished a cycle of this recipe: its products are counted for a minute.</summary>
        public static void Counted(RecipeDef recipe)
        {
            if (recipe == null)
                return;
            foreach (ItemAmount output in recipe.Outputs)
                Made.Enqueue((Time.unscaledTime, output.Item, output.Count));
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
                _shown = Take();
            else
                SINet.SendToServer(SIMessageKind.OverviewRequest, 0, 0, "");
        }

        /// <summary>Host: the factory now.</summary>
        public static Snapshot Take()
        {
            float now = Time.unscaledTime;
            while (Made.Count > 0 && now - Made.Peek().Time > 60f)
                Made.Dequeue();
            var s = new Snapshot { TickMs = _tickMs, PeakMs = _peakMs, Ticks = _ticks, Enabled = Settings.Enabled.Value };
            foreach (var group in Made.GroupBy(m => m.Item).OrderByDescending(g => g.Sum(m => m.Count)).Take(10))
                s.Made.Add((group.Key, group.Sum(m => m.Count)));
            ShipsServerTracker ships = ServiceLocator.GetService<ShipsServerTracker>();
            if (ships == null)
                return s;
            var types = new Dictionary<ushort, (int Working, int Total)>();
            foreach (TrackedShipServer ship in ships.LoadedShipsWithChildren)
            {
                foreach (StatefulPart part in ship.GetAllStatefulParts<StorageState>())
                {
                    if (part?.Settings == null || !MachineCatalog.TryGet(part.Settings.id, out MachineDef def))
                        continue;
                    MachineSimulation.Runtime rt = MachineSimulation.Get(ship.Id, part.Id);
                    if (rt == null)
                        continue;
                    if (def.IsConveyor)
                    {
                        s.Belts++;
                        if (rt.Status == MachineStatus.Producing)
                            s.BeltsMoving++;
                        continue;
                    }
                    if (def.IsLogistics)
                        continue;
                    s.Machines++;
                    bool working = MachineActivity.Working(rt.Status);
                    if (working)
                        s.Working++;
                    s.ByStatus.TryGetValue(rt.Status, out int n);
                    s.ByStatus[rt.Status] = n + 1;
                    types.TryGetValue(def.PartId, out var count);
                    types[def.PartId] = (count.Working + (working ? 1 : 0), count.Total + 1);
                    if (def.Generates)
                        s.Given += rt.Watts * rt.PowerRate;
                    else if (working)
                        s.Drawn += rt.Watts * rt.PowerRate;
                    if (def.Stores)
                    {
                        s.Stored += rt.Stored;
                        s.Capacity += def.StoreJoules;
                    }
                    if (IsProblem(rt.Status))
                    {
                        s.Problems++;
                        if (s.Issues.Count < 10)
                            s.Issues.Add($"{part.Settings.id}/{(int)rt.Status}/{ship.Id}/{part.Id}");
                    }
                }
            }
            foreach (var t in types.OrderByDescending(t => t.Value.Total).ThenBy(t => t.Key))
                s.Types.Add((t.Key, t.Value.Working, t.Value.Total));
            return s;
        }

        /// <summary>The overview as one line of text (development log).</summary>
        public static string Build()
        {
            Snapshot s = Take();
            return $"{s.Machines} machine(s), {s.Working} working, {s.Problems} to check, {Mathf.RoundToInt(s.Drawn)} W used, {Mathf.RoundToInt(s.Given)} W given, " +
                   $"{s.Belts} conveyor(s) ({s.BeltsMoving} moving), made last minute: " + string.Join(", ", s.Made.Select(m => m.Count + " " + IndustrialItems.NameOf(m.Item))) +
                   $", {s.TickMs:0.000} ms/tick";
        }

        private static bool IsProblem(MachineStatus s) =>
            s == MachineStatus.Unpowered || s == MachineStatus.NoPower || s == MachineStatus.OutputBlocked || s == MachineStatus.NoDeposit || s == MachineStatus.NoFuel || s == MachineStatus.Error;

        private static Color StatusColor(MachineStatus s)
        {
            switch (s)
            {
                case MachineStatus.Producing: return new Color(0.35f, 0.9f, 0.5f);
                case MachineStatus.Underpowered: return new Color(0.95f, 0.85f, 0.35f);
                case MachineStatus.Standby: return new Color(0.5f, 0.75f, 1f);
                case MachineStatus.WaitingInputs: return new Color(0.95f, 0.75f, 0.4f);
                case MachineStatus.OutputBlocked:
                case MachineStatus.NoDeposit:
                case MachineStatus.NoFuel: return new Color(1f, 0.55f, 0.3f);
                case MachineStatus.Unpowered:
                case MachineStatus.NoPower:
                case MachineStatus.Error: return new Color(1f, 0.35f, 0.35f);
                default: return new Color(0.55f, 0.62f, 0.65f);
            }
        }

        // ---- Drawing

        public static void OnGUI()
        {
            if (!_open || _shown == null)
                return;
            Styles();
            Snapshot s = _shown;
            const float width = 560f;
            var frame = new Rect(Screen.width - width - 30f, 50f, width, Screen.height - 100f);
            Fill(frame, new Color(0.04f, 0.06f, 0.08f, 0.9f));
            Fill(new Rect(frame.x, frame.y, frame.width, 3f), new Color(1f, 0.6f, 0.15f));
            GUI.Label(new Rect(frame.x + 16f, frame.y + 10f, 400f, 26f), Lang.T("TABLEAU DE BORD INDUSTRIEL", "FACTORY OVERVIEW"), _title);
            GUI.Label(new Rect(frame.xMax - 136f, frame.y + 14f, 120f, 20f), $"<color=#9aa>[{_key}] {Lang.T("fermer", "close")}</color>", _right);
            var view = new Rect(frame.x, frame.y + 44f, frame.width, frame.height - 50f);
            float content = 1200f + s.Types.Count * 22f;
            _scroll = GUI.BeginScrollView(view, _scroll, new Rect(0f, 0f, width - 20f, content));
            float x = 16f, y = 4f, w = width - 52f;

            // Tiles
            float tile = (w - 18f) / 4f;
            Tile(new Rect(x, y, tile, 64f), Lang.T("Machines", "Machines"), s.Machines.ToString(), Color.white);
            Tile(new Rect(x + (tile + 6f), y, tile, 64f), Lang.T("En marche", "Working"), s.Machines > 0 ? $"{s.Working} <size=13>({100 * s.Working / Mathf.Max(1, s.Machines)} %)</size>" : "0", new Color(0.35f, 0.9f, 0.5f));
            Tile(new Rect(x + 2f * (tile + 6f), y, tile, 64f), Lang.T("À vérifier", "To check"), s.Problems.ToString(), s.Problems > 0 ? new Color(1f, 0.45f, 0.35f) : new Color(0.55f, 0.62f, 0.65f));
            Tile(new Rect(x + 3f * (tile + 6f), y, tile, 64f), Lang.T("Convoyeurs", "Conveyors"), $"{s.BeltsMoving}<size=13>/{s.Belts}</size>", new Color(0.5f, 0.75f, 1f));
            y += 76f;

            // Power
            Section(ref y, x, w, Lang.T("ÉNERGIE", "POWER"));
            float top = Mathf.Max(1f, Mathf.Max(s.Drawn, s.Given));
            Bar(new Rect(x, y, w, 22f), s.Drawn / top, new Color(1f, 0.6f, 0.2f), Lang.T($"Consommée par les machines : {Watts(s.Drawn)}", $"Used by the machines: {Watts(s.Drawn)}"));
            y += 26f;
            Bar(new Rect(x, y, w, 22f), s.Given / top, new Color(0.35f, 0.9f, 0.5f), Lang.T($"Donnée par les générateurs du mod : {Watts(s.Given)}", $"Given by the mod's generators: {Watts(s.Given)}"));
            y += 26f;
            if (s.Capacity > 0f)
            {
                Bar(new Rect(x, y, w, 22f), s.Stored / s.Capacity, new Color(0.5f, 0.75f, 1f),
                    Lang.T($"Accumulateurs : {s.Stored / 1e6f:0.0} / {s.Capacity / 1e6f:0} MJ", $"Accumulators: {s.Stored / 1e6f:0.0} / {s.Capacity / 1e6f:0} MJ"));
                y += 26f;
            }
            y += 8f;

            // States, as one bar
            Section(ref y, x, w, Lang.T("ÉTAT DES MACHINES", "MACHINE STATES"));
            float cursor = x;
            foreach (var pair in s.ByStatus.OrderBy(p => p.Key))
            {
                float part = w * pair.Value / Mathf.Max(1, s.Machines);
                Fill(new Rect(cursor, y, part, 16f), StatusColor(pair.Key));
                cursor += part;
            }
            y += 22f;
            foreach (var pair in s.ByStatus.OrderByDescending(p => p.Value))
            {
                Fill(new Rect(x, y + 4f, 10f, 10f), StatusColor(pair.Key));
                GUI.Label(new Rect(x + 16f, y, w - 16f, 20f), $"{UI.MachineHud.StatusText(pair.Key)}  <b>{pair.Value}</b>", _text);
                y += 20f;
            }
            y += 10f;

            // Products of the last minute
            Section(ref y, x, w, Lang.T("PRODUCTION (DERNIÈRE MINUTE)", "PRODUCTION (LAST MINUTE)"));
            var thumbnails = ServiceLocator.GetService<Items.Interface.Service.IItemThumbnailProvider>();
            int most = s.Made.Count > 0 ? s.Made.Max(m => m.Count) : 1;
            if (s.Made.Count == 0)
            {
                GUI.Label(new Rect(x, y, w, 20f), Lang.T("<color=#9aa>Rien de produit depuis une minute.</color>", "<color=#9aa>Nothing made in the last minute.</color>"), _text);
                y += 22f;
            }
            foreach (var m in s.Made)
            {
                Texture2D icon = thumbnails?.GetThumbnail(m.Item);
                if (icon != null)
                    GUI.DrawTexture(new Rect(x, y, 22f, 22f), icon);
                Bar(new Rect(x + 28f, y, w - 28f, 22f), (float)m.Count / most, new Color(0.35f, 0.7f, 0.9f), $"{IndustrialItems.NameOf(m.Item)}  <b>{m.Count}/min</b>");
                y += 26f;
            }
            y += 8f;

            // Machines by type
            Section(ref y, x, w, Lang.T("MACHINES PAR TYPE", "MACHINES BY TYPE"));
            foreach (var t in s.Types)
            {
                string name = MachineCatalog.TryGet(t.Part, out MachineDef def) ? Lang.MachineName(def) : t.Part.ToString();
                Bar(new Rect(x, y, w, 20f), (float)t.Working / Mathf.Max(1, t.Total), new Color(0.35f, 0.9f, 0.5f, 0.8f),
                    $"{name}  <b>{t.Working}/{t.Total}</b> {Lang.T("en marche", "working")}");
                y += 22f;
            }
            y += 10f;

            // What needs a player
            if (s.Issues.Count > 0)
            {
                Section(ref y, x, w, Lang.T("À VÉRIFIER", "NEEDS ATTENTION"));
                foreach (string issue in s.Issues)
                {
                    string[] f = issue.Split('/');
                    string line = issue;
                    if (f.Length == 4 && ushort.TryParse(f[0], out ushort id) && int.TryParse(f[1], out int status) && MachineCatalog.TryGet(id, out MachineDef def))
                        line = $"<b>{Lang.MachineName(def)}</b> <color=#9aa>({Lang.T("vaisseau", "ship")} {f[2]}, {Lang.T("pièce", "part")} {f[3]})</color> : {UI.MachineHud.StatusText((MachineStatus)status)}";
                    Fill(new Rect(x, y + 5f, 4f, 12f), new Color(1f, 0.45f, 0.35f));
                    GUI.Label(new Rect(x + 10f, y, w - 10f, 22f), line, _text);
                    y += 22f;
                }
                y += 10f;
            }

            if (!s.Enabled)
            {
                GUI.Label(new Rect(x, y, w, 22f), Lang.T("<color=#f84>Production désactivée (Enabled = false)</color>", "<color=#f84>Production turned off (Enabled = false)</color>"), _text);
                y += 24f;
            }
            GUI.Label(new Rect(x, y, w, 20f), Lang.T($"<color=#9aa>Simulation : {s.TickMs:0.000} ms par tick ({s.Ticks} ticks / 5 s), pic {s.PeakMs:0.000} ms</color>",
                $"<color=#9aa>Simulation: {s.TickMs:0.000} ms per tick ({s.Ticks} ticks / 5 s), peak {s.PeakMs:0.000} ms</color>"), _small);
            GUI.EndScrollView();
        }

        private static string Watts(float w) => w >= 1000f ? $"{w / 1000f:0.0} kW" : $"{Mathf.RoundToInt(w)} W";

        private static void Section(ref float y, float x, float w, string title)
        {
            GUI.Label(new Rect(x, y, w, 20f), title, _small);
            Fill(new Rect(x, y + 19f, w, 1f), new Color(1f, 1f, 1f, 0.12f));
            y += 26f;
        }

        private static void Tile(Rect r, string label, string value, Color color)
        {
            Fill(r, new Color(0.1f, 0.13f, 0.16f, 1f));
            Fill(new Rect(r.x, r.y, 3f, r.height), color);
            GUI.Label(new Rect(r.x + 10f, r.y + 6f, r.width - 12f, 18f), "<color=#9aa>" + label + "</color>", _small);
            GUI.Label(new Rect(r.x + 10f, r.y + 24f, r.width - 12f, 34f), value, _big);
        }

        private static void Bar(Rect r, float fraction, Color color, string label)
        {
            Fill(r, new Color(0.1f, 0.13f, 0.16f, 1f));
            Fill(new Rect(r.x, r.y, r.width * Mathf.Clamp01(fraction), r.height), new Color(color.r, color.g, color.b, 0.45f));
            GUI.Label(new Rect(r.x + 8f, r.y + 1f, r.width - 10f, r.height), label, _text);
        }

        private static void Fill(Rect r, Color c)
        {
            Color previous = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = previous;
        }

        private static void Styles()
        {
            if (_title != null)
                return;
            _title = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold, richText = true, normal = { textColor = new Color(1f, 0.85f, 0.6f) } };
            _text = new GUIStyle(GUI.skin.label) { fontSize = 14, richText = true, wordWrap = false, normal = { textColor = Color.white } };
            _small = new GUIStyle(GUI.skin.label) { fontSize = 12, fontStyle = FontStyle.Bold, richText = true, normal = { textColor = new Color(0.6f, 0.85f, 1f) } };
            _big = new GUIStyle(GUI.skin.label) { fontSize = 24, fontStyle = FontStyle.Bold, richText = true, normal = { textColor = Color.white } };
            _right = new GUIStyle(_text) { alignment = TextAnchor.UpperRight, fontSize = 12 };
        }

        public static void Forget()
        {
            Made.Clear();
            _shown = null;
        }
    }
}
