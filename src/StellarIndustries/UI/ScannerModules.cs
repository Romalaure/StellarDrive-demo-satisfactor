using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using FishNet;
using FishNet.Connection;
using Items.Model;
using Players;
using SDModKit.Game;
using StellarIndustries.Content;
using StellarIndustries.Core;

namespace StellarIndustries.UI
{
    /// <summary>
    /// The modules fitted into a player's deposit scanner. The game's items are only an id and a
    /// count (an item cannot carry anything), so the fitted modules belong to the player's
    /// account, not to one scanner item: they work with whichever scanner the player carries.
    /// The host keeps them, per Steam account, in a companion file beside the world save; fitting
    /// a module takes it out of the player's inventory, taking it out gives it back. A scanner
    /// has 2 slots (Mk1), 3 (Mk2) or 4 (Mk3); modules beyond the slots of the scanner carried
    /// stay fitted but do nothing until a better scanner is carried.
    /// </summary>
    internal static class ScannerModules
    {
        public const string FileName = "stellar-industries-scanners.txt";

        /// <summary>Every module that fits, with its effect: range bonus and analysis grade.</summary>
        public static readonly (uint Item, float Range, int Analysis)[] Kinds =
        {
            (ResourceCatalog.RangeModule, 0.25f, 0),
            (ResourceCatalog.RangeModule2, 0.5f, 0),
            (ResourceCatalog.RangeModule3, 1f, 0),
            (ResourceCatalog.AnalysisModule, 0f, 1),
            (ResourceCatalog.AnalysisModule2, 0f, 2)
        };

        private static readonly Dictionary<ulong, List<uint>> Fitted = new Dictionary<ulong, List<uint>>();
        private static readonly List<uint> Mine = new List<uint>();
        private static bool _changed, _asked;
        private static float _askAt;

        /// <summary>The modules fitted in this player's scanner (host or client).</summary>
        public static IReadOnlyList<uint> Own => Mine;

        /// <summary>Set when the host refused the last change (no room, or the item is gone).</summary>
        public static bool Refused { get; private set; }

        public static int Slots(int tier) => tier >= 3 ? 4 : tier == 2 ? 3 : tier == 1 ? 2 : 0;

        public static bool IsModule(uint item) => Kinds.Any(k => k.Item == item);

        public static void Install()
        {
            SINet.OnServer(SIMessageKind.ScannerInstall, (sender, a, b, text) => Change(sender, text, true));
            SINet.OnServer(SIMessageKind.ScannerRemove, (sender, a, b, text) => Change(sender, text, false));
            SINet.OnServer(SIMessageKind.ScannerRequest, (sender, a, b, text) => Answer(sender, FindPlayer(sender), false));
            SINet.OnClient(SIMessageKind.ScannerModules, (a, b, text) => Receive(text, a == 1));
        }

        public static void Forget()
        {
            Mine.Clear();
            _asked = false;
            _askAt = 0f;
        }

        /// <summary>Asks the host once a world is up (a client after joining; the host answers itself).</summary>
        public static void Update()
        {
            if (_asked)
                return;
            if (_askAt == 0f)
                _askAt = UnityEngine.Time.unscaledTime + 5f;
            if (UnityEngine.Time.unscaledTime < _askAt)
                return;
            _asked = true;
            SINet.SendToServer(SIMessageKind.ScannerRequest, 0, 0, "");
        }

        public static void RequestInstall(uint item) => SINet.SendToServer(SIMessageKind.ScannerInstall, 0, 0, item.ToString(CultureInfo.InvariantCulture));

        public static void RequestRemove(int slot) => SINet.SendToServer(SIMessageKind.ScannerRemove, 0, 0, slot.ToString(CultureInfo.InvariantCulture));

        private static void Receive(string text, bool refused)
        {
            Mine.Clear();
            Mine.AddRange(Parse(text));
            Refused = refused;
        }

        // ---- Host

        private static Players.TrackedPlayerServer FindPlayer(NetworkConnection sender)
        {
            var players = GameRefs.PlayersServer;
            if (players == null)
                return null;
            if (sender == null || sender.IsLocalClient)
                return players.ActivePlayers.FirstOrDefault(p => p != null && p.IsHost);
            return players.ActivePlayers.FirstOrDefault(p => p != null && p.ConnectionId == sender.ClientId);
        }

        /// <summary>The best scanner a player carries (host).</summary>
        private static int Tier(Players.TrackedPlayerServer player) =>
            ServerTrade.Has(player, ResourceCatalog.ScannerMk3, 1) ? 3 : ServerTrade.Has(player, ResourceCatalog.ScannerMk2, 1) ? 2 : ServerTrade.Has(player, ResourceCatalog.ScannerMk1, 1) ? 1 : 0;

        private static void Change(NetworkConnection sender, string text, bool fit)
        {
            Players.TrackedPlayerServer player = FindPlayer(sender);
            if (player == null)
                return;
            if (!Fitted.TryGetValue(player.SteamUserId, out List<uint> list))
                Fitted[player.SteamUserId] = list = new List<uint>();
            bool ok = false;
            try
            {
                if (fit && uint.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out uint item) && IsModule(item) && list.Count < Slots(Tier(player)))
                {
                    ok = ServerTrade.TryTrade(player, new[] { new ItemDrop { ItemId = item, Quantity = 1 } }, new ItemDrop[0]);
                    if (ok)
                        list.Add(item);
                }
                else if (!fit && int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out int slot) && slot >= 0 && slot < list.Count)
                {
                    ok = ServerTrade.TryTrade(player, new ItemDrop[0], new[] { new ItemDrop { ItemId = list[slot], Quantity = 1 } });
                    if (ok)
                        list.RemoveAt(slot);
                }
            }
            catch (Exception e)
            {
                ErrorLog.Report("scanner modules", e);
            }
            if (ok)
                _changed = true;
            Answer(sender, player, !ok);
        }

        private static void Answer(NetworkConnection sender, Players.TrackedPlayerServer player, bool refused)
        {
            if (player == null)
                return;
            string text = Fitted.TryGetValue(player.SteamUserId, out List<uint> list) ? string.Join(",", list) : "";
            if (sender == null || sender.IsLocalClient)
                Receive(text, refused);
            else
                SINet.SendTo(sender, SIMessageKind.ScannerModules, refused ? 1u : 0u, 0, text);
        }

        private static IEnumerable<uint> Parse(string text) =>
            (text ?? "").Split(',').Select(t => uint.TryParse(t.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out uint id) ? id : 0u).Where(IsModule);

        // ---- Companion file

        public static void Load(string worldDirectory)
        {
            Fitted.Clear();
            Mine.Clear();
            _changed = false;
            _asked = false;
            _askAt = 0f;
            if (string.IsNullOrEmpty(worldDirectory))
                return;
            string path = Path.Combine(worldDirectory, FileName);
            if (!File.Exists(path))
                return;
            foreach (string raw in File.ReadAllLines(path))
            {
                string[] parts = raw.Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 2 && parts[0][0] != '#' && ulong.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out ulong steam))
                    Fitted[steam] = Parse(parts[1]).ToList();
            }
            IndustriesMod.Log.Msg($"scanner modules of {Fitted.Count} player(s) read from {FileName}");
        }

        public static void Save(string worldDirectory)
        {
            if (string.IsNullOrEmpty(worldDirectory) || !SINet.IsServer)
                return;
            string path = Path.Combine(worldDirectory, FileName);
            if (!_changed && File.Exists(path) || Fitted.Count == 0 && !File.Exists(path))
                return;
            var text = new StringBuilder("# Stellar Industries: modules du scanner par compte Steam (compte modules)\n");
            foreach (var pair in Fitted.Where(p => p.Value.Count > 0))
                text.Append(pair.Key.ToString(CultureInfo.InvariantCulture)).Append(' ').Append(string.Join(",", pair.Value)).Append('\n');
            File.WriteAllText(path, text.ToString());
            _changed = false;
        }
    }
}
