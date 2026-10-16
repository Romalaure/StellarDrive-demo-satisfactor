using System.Collections.Generic;
using System.Globalization;
using SDModKit.Game;
using StellarIndustries.Content;
using StellarIndustries.Core;
using UnityEngine;

namespace StellarIndustries.Production
{
    /// <summary>
    /// Multiplayer: the game only loads the rocks near each player, so the host cannot see the
    /// rocks beside a client's base when it is far from it. Every few seconds each client tells
    /// the host which rock is within reach of the extractors it has loaded around it; the host
    /// uses a fresh report when it sees no rock itself (and checks the item and the reach).
    /// </summary>
    internal static class DepositReports
    {
        private const float Interval = 3f, Lifetime = 8f, Around = 250f;

        private static readonly Dictionary<(uint, ushort), (Deposits.Deposit Deposit, float Time)> Reports = new Dictionary<(uint, ushort), (Deposits.Deposit, float)>();
        private static float _next;

        public static int Count => Reports.Count;

        public static void Forget()
        {
            Reports.Clear();
            _next = 0f;
        }

        /// <summary>Host: accepts the clients' reports.</summary>
        public static void Install()
        {
            SINet.OnServer(SIMessageKind.DepositSeen, (sender, ship, part, text) =>
            {
                if (TryDecode(text, out Deposits.Deposit deposit))
                    Reports[(ship, part)] = (deposit, Time.unscaledTime);
            });
        }

        /// <summary>Host: a client's fresh report of a rock within this extractor's reach, or null.</summary>
        public static Deposits.Deposit? Reported(uint shipId, ushort partId, MachineDef def)
        {
            if (!Reports.TryGetValue((shipId, partId), out var report))
                return null;
            if (Time.unscaledTime - report.Time > Lifetime)
            {
                Reports.Remove((shipId, partId));
                return null;
            }
            Deposits.Deposit d = report.Deposit;
            if (d.Distance > def.ExtractRadius || !Deposits.Extractable(d.Item) || IndustrialItems.Find(d.Item) == null)
                return null;
            d.Name = IndustrialItems.NameOf(d.Item);
            return d;
        }

        /// <summary>Client (not the host): reports the rocks within reach of the extractors around this player.</summary>
        public static void Update()
        {
            if (SINet.IsServer || Time.unscaledTime < _next)
                return;
            _next = Time.unscaledTime + Interval;
            Camera camera = Camera.main;
            var ships = GameRefs.Service<Ships.Services.ShipsClientTracker>();
            if (camera == null || ships == null)
                return;
            Vector3 eye = camera.transform.position;
            foreach (var ship in ships.VisuallyLoadedShip)
            {
                if (ship == null || !ships.TryGetShipVisualTransform(ship.Id, out Transform frame) || frame == null || Vector3.Distance(frame.position, eye) > Around + 100f)
                    continue;
                foreach (var part in ship.GetAllStatefulParts<Ships.Interface.Model.Parts.State.StorageState>())
                {
                    if (part?.Settings == null || !MachineCatalog.TryGet(part.Settings.id, out MachineDef def) || !def.Extracts)
                        continue;
                    Vector3 at = frame.TransformPoint(part.LocalPosition);
                    if (Vector3.Distance(at, eye) > Around)
                        continue;
                    Deposits.Deposit? near = Deposits.NearestTo(at);
                    if (near != null && near.Value.Distance <= def.ExtractRadius)
                        SINet.SendToServer(SIMessageKind.DepositSeen, ship.Id, part.Id, Encode(near.Value));
                }
            }
        }

        public static string Encode(Deposits.Deposit d) =>
            string.Join(";", d.Item.ToString(CultureInfo.InvariantCulture), ((int)d.Purity).ToString(CultureInfo.InvariantCulture), d.Node ? "1" : "0",
                d.Distance.ToString("0.##", CultureInfo.InvariantCulture));

        public static bool TryDecode(string text, out Deposits.Deposit deposit)
        {
            deposit = default;
            string[] p = (text ?? "").Split(';');
            if (p.Length < 4 || !uint.TryParse(p[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out uint item) ||
                !int.TryParse(p[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int purity) || purity < 0 || purity > (int)Purity.Pure ||
                !float.TryParse(p[3], NumberStyles.Float, CultureInfo.InvariantCulture, out float distance) || float.IsNaN(distance) || distance < 0f)
                return false;
            deposit = new Deposits.Deposit { Item = item, Purity = (Purity)purity, Node = p[2] == "1", Distance = distance };
            return true;
        }
    }
}
