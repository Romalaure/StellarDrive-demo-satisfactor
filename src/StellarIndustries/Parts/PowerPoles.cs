using System.Linq;
using HarmonyLib;
using Ships.Interface.Model.Parts;
using Ships.Interface.Model.Parts.Common;
using Ships.Interface.Settings;
using StellarIndustries.Content;
using StellarIndustries.Core;
using UnityEngine;

namespace StellarIndustries.Parts
{
    /// <summary>
    /// Power poles: tall posts that carry power cables over a base. Each is a clone of the game's
    /// Power Splitter, whose power sockets are relayed to one another by the part itself (the
    /// game joins them into one network), with more sockets around a head at the top of the
    /// post: 4, 8 or 12 cables. Nothing is simulated: the game's own power networks do the rest.
    /// </summary>
    internal static class PowerPoles
    {
        public const ushort PoleMk1 = 7629, PoleMk2 = 7630, PoleMk3 = 7631;

        private const string Donor = "PowerSplitter";
        /// <summary>Half the width of the head the sockets come out of (also the part's box).</summary>
        private const float Head = 0.16f;
        /// <summary>Height of one ring of four sockets on the head.</summary>
        private const float Ring = 0.3f;
        /// <summary>The whole pole (model, box, sockets) is built this many times bigger than its design.</summary>
        private const float Scale = 1.5f;

        internal sealed class Pole
        {
            public ushort Id;
            public string Name, NameEn, Description, DescriptionEn;
            public int Sockets;
            public float Height, Mass;
            public (uint Item, int Count)[] Cost;
        }

        public static readonly Pole[] All =
        {
            new Pole
            {
                Id = PoleMk1, Name = "Poteau électrique Mk1", NameEn = "Power Pole Mk1", Sockets = 4, Height = 2f, Mass = 25f,
                Description = "Un poteau de 3 m qui porte les câbles électriques au-dessus du sol : 4 branchements, tous sur le même réseau. " +
                              "Pour relier des machines éloignées sans câble qui traîne.",
                DescriptionEn = "A 3 m pole that carries power cables above the ground: 4 connections, all on the same grid. " +
                                "To link distant machines without cables lying around.",
                Cost = new[] { (ResourceCatalog.IronRod, 4), (ResourceCatalog.CopperWire, 4) }
            },
            new Pole
            {
                Id = PoleMk2, Name = "Poteau électrique Mk2", NameEn = "Power Pole Mk2", Sockets = 8, Height = 3f, Mass = 40f,
                Description = "Un poteau de 4,5 m à double tête : 8 branchements sur le même réseau.",
                DescriptionEn = "A 4.5 m pole with a double head: 8 connections on the same grid.",
                Cost = new[] { (ResourceCatalog.SteelPipe, 2), (ResourceCatalog.Cable, 4), (ResourceCatalog.Rubber, 2) }
            },
            new Pole
            {
                Id = PoleMk3, Name = "Pylône électrique Mk3", NameEn = "Power Pylon Mk3", Sockets = 12, Height = 4f, Mass = 70f,
                Description = "Un pylône en treillis de 6 m : 12 branchements sur le même réseau, pour les grands réseaux.",
                DescriptionEn = "A 6 m lattice pylon: 12 connections on the same grid, for big power networks.",
                Cost = new[] { (ResourceCatalog.SteelBeam, 4), (ResourceCatalog.Cable, 8), (ResourceCatalog.AlloySheet, 2) }
            }
        };

        public static string NameOf(Pole p) => Lang.T(p.Name, p.NameEn);

        public static string DescriptionOf(Pole p) => Lang.T(p.Description, p.DescriptionEn);

        public static Pole Get(ushort id) => All.FirstOrDefault(p => p.Id == id);

        /// <summary>Only call when SDModKit is loaded.</summary>
        public static void Register()
        {
            foreach (Pole pole in All)
            {
                Pole p = pole;
                PartTools.Register(p.Id, Donor, PartTools.UtilityTab, 0, (settings, prefab) => Configure(p, settings, prefab));
            }
        }

        public static void Rename()
        {
            foreach (Pole p in All)
            {
                PartSettings settings = SDModKit.Game.CustomParts.Get(p.Id);
                if (settings == null)
                    continue;
                settings.fullLabel = NameOf(p);
                settings.description = DescriptionOf(p);
            }
        }

        private static void Configure(Pole pole, PartSettings settings, GameObject prefab)
        {
            PartTools.Describe(settings, NameOf(pole), DescriptionOf(pole), pole.Mass, pole.Cost);
            Bounds donor = PartTools.DonorBounds(prefab);
            GameSkin.CaptureLogicGates();
            float bottom = donor.center.y - donor.size.y * 0.5f;
            var size = new Vector3(Head * 2f, pole.Height, Head * 2f) * Scale;
            var origin = new Vector3(donor.center.x, bottom, donor.center.z);
            Transform visuals = PartTools.Blank(prefab, origin + new Vector3(0f, size.y * 0.5f, 0f), size);
            var kit = new ModelKit("SI_Model", origin);
            Model(kit, pole, origin);
            Transform root = kit.Build(visuals, visuals.gameObject.layer, IndustrialSkin.Material, GameSkin.Ready ? GameSkin.Palette : (Vector2?)null, Vector3.zero);
            root.localScale = Vector3.one * Scale;
            // Four sockets per ring, one on each side of the head, from the top ring down
            Vector3[] sides = { Vector3.forward, Vector3.right, Vector3.back, Vector3.left };
            for (int i = 0; i < pole.Sockets; i++)
            {
                Vector3 side = sides[i % 4];
                float y = pole.Height - 0.2f - (i / 4) * Ring;
                PartTools.GraftSocket(prefab, (byte)i, CableType.Power, origin + (new Vector3(0f, y, 0f) + side * Head) * Scale, side);
            }
            // Every socket relays to all the others: the game makes them one network
            SocketObject[] sockets = prefab.GetComponentsInChildren<SocketObject>(true).Where(s => s.Type == CableType.Power).ToArray();
            var relayed = AccessTools.Field(typeof(SocketObject), "relayedSockets");
            foreach (SocketObject socket in sockets)
                relayed.SetValue(socket, sockets.Where(o => o != socket).ToArray());
            IndustriesMod.Log.Msg($"pole {pole.Id} '{pole.Name}': {sockets.Length} relayed power socket(s), box {size}");
        }

        // ---- Model (bottom centre at o, part frame)

        private static void Model(ModelKit k, Pole pole, Vector3 o)
        {
            Vector3 P(float x, float y, float z) => o + new Vector3(x, y, z);
            float h = pole.Height;
            int rings = (pole.Sockets + 3) / 4;
            float headTop = h - 0.07f, headBottom = headTop - rings * Ring;
            // Footing: a bolted plate
            k.Box(Tile.DarkSteel, P(0f, 0.02f, 0f), new Vector3(Head * 2f, 0.04f, Head * 2f));
            k.Bolts(P(0f, 0.04f, 0f), Vector3.up, 0.11f, 4, 0.03f);
            if (pole.Id == PoleMk3)
            {
                // Lattice: four corner angles braced by diagonals
                float c = Head - 0.03f;
                foreach (float x in new[] { -c, c })
                {
                    foreach (float z in new[] { -c, c })
                        k.Beam(Tile.Steel, P(x, 0.04f, z), P(x * 0.7f, headBottom, z * 0.7f), 0.03f);
                }
                for (float y = 0.04f; y + 0.5f <= headBottom; y += 0.5f)
                {
                    float s0 = Mathf.Lerp(1f, 0.7f, y / headBottom) * c, s1 = Mathf.Lerp(1f, 0.7f, (y + 0.5f) / headBottom) * c;
                    k.Beam(Tile.Steel, P(-s0, y, s0), P(s1, y + 0.5f, s1), 0.018f);
                    k.Beam(Tile.Steel, P(-s0, y, -s0), P(s1, y + 0.5f, -s1), 0.018f);
                    k.Beam(Tile.Steel, P(s0, y, -s0), P(s1, y + 0.5f, s1), 0.018f);
                    k.Beam(Tile.Steel, P(-s0, y, -s0), P(-s1, y + 0.5f, s1), 0.018f);
                    k.Box(Tile.DarkSteel, P(0f, y + 0.5f, 0f), new Vector3(s1 * 2f + 0.03f, 0.025f, s1 * 2f + 0.03f));
                }
            }
            else
            {
                // A tube on a base collar, with a hazard band at knee height
                float r = pole.Id == PoleMk2 ? 0.07f : 0.055f;
                k.Cylinder(Tile.DarkSteel, P(0f, 0.04f, 0f), P(0f, 0.22f, 0f), r + 0.03f, r + 0.01f, 12);
                k.Cylinder(pole.Id == PoleMk2 ? Tile.Steel : Tile.PaintedLight, P(0f, 0.2f, 0f), P(0f, headBottom, 0f), r, r, 12);
                k.Cylinder(Tile.Hazard, P(0f, 0.55f, 0f), P(0f, 0.75f, 0f), r + 0.006f, r + 0.006f, 12);
                // Power sign facing front, clamped to the tube
                k.Box(Tile.DarkSteel, P(0f, 1.35f, r + 0.01f), new Vector3(0.05f, 0.05f, 0.03f));
                k.Box(Tile.LabelPower, P(0f, 1.35f, r + 0.03f), new Vector3(0.14f, 0.14f, 0.012f));
            }
            // Head: one junction block per ring of sockets, separated by insulator collars
            for (int i = 0; i < rings; i++)
            {
                float top = headTop - i * Ring;
                k.Box(Tile.PaintedYellow, P(0f, top - Ring * 0.5f + 0.02f, 0f), new Vector3(Head * 2f - 0.02f, Ring - 0.08f, Head * 2f - 0.02f));
                k.Frame(Tile.DarkSteel, P(0f, top - Ring * 0.5f + 0.02f, Head - 0.005f), new Vector2(Head * 2f - 0.02f, Ring - 0.08f), 0.025f, Vector3.forward, 0.012f);
                k.Cylinder(Tile.Plastic, P(0f, top - Ring + 0.04f, 0f), P(0f, top - Ring + 0.06f, 0f), Head * 0.75f, Head * 0.75f, 12);
                k.Cylinder(Tile.Plastic, P(0f, top - Ring + 0.06f, 0f), P(0f, top - Ring + 0.08f, 0f), Head * 0.6f, Head * 0.6f, 12);
            }
            // Cap with a beacon light
            k.Box(Tile.DarkSteel, P(0f, headTop + 0.01f, 0f), new Vector3(Head * 2f, 0.02f, Head * 2f));
            k.Cylinder(Tile.LedRed, P(0f, headTop + 0.02f, 0f), P(0f, h - 0.005f, 0f), 0.03f, 0.022f, 8);
        }
    }
}
