using StellarIndustries.Core;
using StellarIndustries.Parts;
using UnityEngine;

namespace StellarIndustries.Content
{
    /// <summary>
    /// The items' 3D models (about the size of the game's ore pieces, 0.3 m), built by the mod
    /// on its texture atlas: each item has its own shape and materials (ingots with bevels, plates
    /// with bolts, wire spools, screw piles, sacks of powder, a rotor with copper windings...).
    /// The game renders the inventory icons from these models.
    /// </summary>
    internal static partial class ItemModels
    {
        private static MeshBuilder _m;

        private static void Box(Tile t, Vector3 c, Vector3 s, Quaternion? r = null)
        {
            _m.Panel = IndustrialSkin.Uv(t);
            _m.Box(c, s, r);
        }

        private static void Cyl(Tile t, Vector3 a, Vector3 b, float ra, float rb, int sides = 14, bool capA = true, bool capB = true)
        {
            _m.Panel = IndustrialSkin.Uv(t);
            _m.Cylinder(a, b, ra, rb, sides, capA, capB);
        }

        private static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);

        /// <summary>The model of an item, or false if it has none (falls back to a plain shape).</summary>
        public static bool Build(MeshBuilder m, uint id)
        {
            _m = m;
            switch (id)
            {
                case ResourceCatalog.IronPlate: Plates(Tile.Steel, Tile.BoltHeads); return true;
                case ResourceCatalog.CopperWire: Spool(Tile.Winding, Tile.PaintedOrange); return true;
                case ResourceCatalog.IronIngot: Ingots(Tile.IngotIron); return true;
                case ResourceCatalog.CopperIngot: Ingots(Tile.IngotCopper); return true;
                case ResourceCatalog.AluminumIngot: Ingots(Tile.IngotAluminum); return true;
                case ResourceCatalog.SteelIngot: Ingots(Tile.IngotSteel); return true;
                case ResourceCatalog.IronRod: Rods(Tile.Brushed); return true;
                case ResourceCatalog.Screws: ScrewPile(); return true;
                case ResourceCatalog.Cable: Spool(Tile.CableJacket, Tile.PaintedDark); return true;
                case ResourceCatalog.AluminumSheet: Sheets(Tile.Aluminum); return true;
                case ResourceCatalog.CopperSheet: Sheets(Tile.Copper); return true;
                case ResourceCatalog.ReinforcedPlate: Reinforced(); return true;
                case ResourceCatalog.CrushedIron: Sack(Tile.CrushedIron); return true;
                case ResourceCatalog.CrushedCopper: Sack(Tile.CrushedCopper); return true;
                case ResourceCatalog.StellarDust: Sack(Tile.StellarDust); return true;
                case ResourceCatalog.BiomassBrick: Bricks(); return true;
                case ResourceCatalog.SteelBeam: Beam(); return true;
                case ResourceCatalog.SteelPipe: Pipes(); return true;
                case ResourceCatalog.Concrete: ConcreteBlock(); return true;
                case ResourceCatalog.Rotor: RotorModel(); return true;
                case ResourceCatalog.Stator: StatorModel(); return true;
                case ResourceCatalog.Motor: MotorModel(); return true;
                case ResourceCatalog.AssemblyFrame: FrameModel(); return true;
                case ResourceCatalog.CircuitBoard: Circuit(); return true;
                case ResourceCatalog.Computer: ComputerModel(); return true;
                case ResourceCatalog.HeatSink: HeatSinkModel(); return true;
                case ResourceCatalog.HeavyFrame: HeavyFrameModel(); return true;
                default: return BuildAdvanced(id);
            }
        }

        // Two plates, the top one turned, bolt holes at the corners
        private static void Plates(Tile metal, Tile bolts)
        {
            Box(metal, V(0f, 0.012f, 0f), V(0.32f, 0.024f, 0.24f));
            Box(metal, V(0.01f, 0.036f, 0.004f), V(0.3f, 0.024f, 0.22f), Quaternion.Euler(0f, 8f, 0f));
            foreach (float x in new[] { -0.12f, 0.12f })
            {
                foreach (float z in new[] { -0.08f, 0.08f })
                    Cyl(bolts, V(x, 0.048f, z), V(x, 0.052f, z), 0.012f, 0.012f, 6);
            }
        }

        // A stack of three trapezoid bars
        private static void Ingots(Tile tile)
        {
            void Bar(Vector3 at, float yaw)
            {
                Quaternion r = Quaternion.Euler(0f, yaw, 0f);
                // Trapezoid: wide bottom, narrow top, through Block corners
                var c = new Vector3[8];
                for (int i = 0; i < 8; i++)
                {
                    bool top = (i & 2) != 0;
                    float hx = top ? 0.1f : 0.13f, hz = top ? 0.035f : 0.05f;
                    var local = new Vector3((i & 1) == 0 ? -hx : hx, top ? 0.055f : 0f, (i & 4) == 0 ? -hz : hz);
                    c[i] = at + r * local;
                }
                _m.Panel = IndustrialSkin.Uv(tile);
                _m.Block(c);
            }
            Bar(V(0f, 0f, -0.06f), 0f);
            Bar(V(0f, 0f, 0.06f), 0f);
            Bar(V(0.01f, 0.055f, 0f), 90f);
        }

        private static void Rods(Tile tile)
        {
            for (int i = 0; i < 4; i++)
            {
                float z = -0.07f + (i % 3) * 0.07f, y = i < 3 ? 0.03f : 0.085f;
                Cyl(tile, V(-0.17f, y, i < 3 ? z : 0.035f), V(0.17f, y, i < 3 ? z : 0.035f), 0.03f, 0.03f, 8);
            }
            Box(Tile.PaintedYellow, V(-0.1f, 0.05f, 0f), V(0.02f, 0.11f, 0.21f));
            Box(Tile.PaintedYellow, V(0.1f, 0.05f, 0f), V(0.02f, 0.11f, 0.21f));
        }

        private static void ScrewPile()
        {
            var rnd = new System.Random(7);
            for (int i = 0; i < 9; i++)
            {
                var at = V((float)rnd.NextDouble() * 0.22f - 0.11f, 0.022f + (i / 5) * 0.03f, (float)rnd.NextDouble() * 0.22f - 0.11f);
                Quaternion r = Quaternion.Euler(90f, (float)rnd.NextDouble() * 360f, 0f);
                Vector3 axis = r * Vector3.up;
                Cyl(Tile.Brushed, at - axis * 0.07f, at + axis * 0.06f, 0.016f, 0.016f, 6);
                for (int t = 0; t < 4; t++)
                    Cyl(Tile.DarkSteel, at - axis * (0.06f - t * 0.03f), at - axis * (0.055f - t * 0.03f), 0.021f, 0.021f, 6);
                Cyl(Tile.BoltHeads, at + axis * 0.06f, at + axis * 0.08f, 0.032f, 0.032f, 6);
            }
        }

        // A spool: two flanges, a thick winding
        private static void Spool(Tile winding, Tile flange)
        {
            Cyl(flange, V(0f, 0f, 0f), V(0f, 0.025f, 0f), 0.14f, 0.14f, 16);
            Cyl(winding, V(0f, 0.025f, 0f), V(0f, 0.175f, 0f), 0.115f, 0.115f, 16);
            Cyl(flange, V(0f, 0.175f, 0f), V(0f, 0.2f, 0f), 0.14f, 0.14f, 16);
            Cyl(Tile.DarkSteel, V(0f, 0.2f, 0f), V(0f, 0.21f, 0f), 0.035f, 0.035f, 10);
        }

        private static void Sheets(Tile tile)
        {
            for (int i = 0; i < 4; i++)
                Box(tile, V(0f, 0.006f + i * 0.012f, 0f), V(0.34f, 0.01f, 0.26f), Quaternion.Euler(0f, i * 3f - 4f, 0f));
        }

        // Two plates with a frame of screws between
        private static void Reinforced()
        {
            Box(Tile.Steel, V(0f, 0.015f, 0f), V(0.34f, 0.03f, 0.26f));
            Box(Tile.DarkSteel, V(0f, 0.045f, 0f), V(0.3f, 0.03f, 0.22f));
            Box(Tile.Rivets, V(0f, 0.075f, 0f), V(0.34f, 0.03f, 0.26f));
            foreach (float x in new[] { -0.15f, 0f, 0.15f })
            {
                foreach (float z in new[] { -0.11f, 0.11f })
                    Cyl(Tile.BoltHeads, V(x, 0.09f, z), V(x, 0.1f, z), 0.014f, 0.014f, 6);
            }
        }

        // A tied sack of powder with a heap in front
        private static void Sack(Tile powder)
        {
            Cyl(Tile.Sack, V(0f, 0f, -0.03f), V(0f, 0.2f, -0.03f), 0.12f, 0.1f, 12);
            Cyl(Tile.Sack, V(0f, 0.2f, -0.03f), V(0f, 0.25f, -0.03f), 0.05f, 0.07f, 10);
            Cyl(Tile.Rubber, V(0f, 0.2f, -0.03f), V(0f, 0.215f, -0.03f), 0.052f, 0.052f, 10);
            Cyl(powder, V(0.06f, 0f, 0.1f), V(0.06f, 0.06f, 0.1f), 0.1f, 0.01f, 10);
        }

        private static void Bricks()
        {
            Box(Tile.Biomass, V(-0.06f, 0.04f, 0f), V(0.11f, 0.08f, 0.22f));
            Box(Tile.Biomass, V(0.06f, 0.04f, 0f), V(0.11f, 0.08f, 0.22f));
            Box(Tile.Biomass, V(0f, 0.12f, 0f), V(0.22f, 0.08f, 0.11f), Quaternion.Euler(0f, 5f, 0f));
        }

        // An I-beam
        private static void Beam()
        {
            Box(Tile.Steel, V(0f, 0.01f, 0f), V(0.38f, 0.02f, 0.12f));
            Box(Tile.Steel, V(0f, 0.07f, 0f), V(0.38f, 0.1f, 0.02f));
            Box(Tile.Steel, V(0f, 0.13f, 0f), V(0.38f, 0.02f, 0.12f));
            Box(Tile.PaintedOrange, V(0.17f, 0.07f, 0.012f), V(0.03f, 0.06f, 0.004f));
        }

        private static void Pipes()
        {
            for (int i = 0; i < 3; i++)
            {
                Vector3 a = V(-0.18f, 0.045f + (i == 2 ? 0.075f : 0f), i == 2 ? 0f : (i == 0 ? -0.045f : 0.045f));
                Cyl(Tile.Pipe, a, a + V(0.36f, 0f, 0f), 0.042f, 0.042f, 12, capA: false, capB: false);
                Cyl(Tile.Rubber, a, a + V(0.005f, 0f, 0f), 0.03f, 0.03f, 12);
                Cyl(Tile.Rubber, a + V(0.355f, 0f, 0f), a + V(0.36f, 0f, 0f), 0.03f, 0.03f, 12);
            }
        }

        private static void ConcreteBlock()
        {
            Box(Tile.Concrete, V(0f, 0.08f, 0f), V(0.3f, 0.16f, 0.2f));
            Box(Tile.Concrete, V(-0.07f, 0.165f, 0f), V(0.08f, 0.01f, 0.12f));
            Box(Tile.Concrete, V(0.07f, 0.165f, 0f), V(0.08f, 0.01f, 0.12f));
        }

        // Shaft, copper core between two end discs
        private static void RotorModel()
        {
            Cyl(Tile.Brushed, V(-0.2f, 0.08f, 0f), V(0.2f, 0.08f, 0f), 0.02f, 0.02f, 10);
            Cyl(Tile.DarkSteel, V(-0.12f, 0.08f, 0f), V(-0.1f, 0.08f, 0f), 0.08f, 0.08f, 16);
            Cyl(Tile.Winding, V(-0.1f, 0.08f, 0f), V(0.1f, 0.08f, 0f), 0.07f, 0.07f, 16);
            Cyl(Tile.DarkSteel, V(0.1f, 0.08f, 0f), V(0.12f, 0.08f, 0f), 0.08f, 0.08f, 16);
            for (int i = 0; i < 6; i++)
                Box(Tile.Steel, V(0f, 0.08f, 0f), V(0.2f, 0.152f, 0.015f), Quaternion.Euler(i * 30f, 0f, 0f));
        }

        // Ring housing with windings visible inside
        private static void StatorModel()
        {
            Cyl(Tile.PaintedBlue, V(0f, 0f, 0f), V(0f, 0.16f, 0f), 0.15f, 0.15f, 18, capA: true, capB: false);
            Cyl(Tile.Winding, V(0f, 0.02f, 0f), V(0f, 0.165f, 0f), 0.12f, 0.12f, 18, capA: false, capB: false);
            Cyl(Tile.DarkSteel, V(0f, 0.16f, 0f), V(0f, 0.17f, 0f), 0.08f, 0.08f, 18);
            for (int i = 0; i < 6; i++)
            {
                float a = i * Mathf.PI / 3f;
                Box(Tile.DarkSteel, V(Mathf.Cos(a) * 0.15f, 0.08f, Mathf.Sin(a) * 0.15f), V(0.02f, 0.16f, 0.02f));
            }
        }

        private static void MotorModel()
        {
            Cyl(Tile.PaintedBlue, V(-0.14f, 0.11f, 0f), V(0.1f, 0.11f, 0f), 0.1f, 0.1f, 18);
            for (int i = 0; i < 5; i++)
                Cyl(Tile.DarkSteel, V(-0.12f + i * 0.05f, 0.11f, 0f), V(-0.11f + i * 0.05f, 0.11f, 0f), 0.11f, 0.11f, 18);
            Cyl(Tile.Fan, V(-0.17f, 0.11f, 0f), V(-0.14f, 0.11f, 0f), 0.1f, 0.1f, 18);
            Cyl(Tile.DarkSteel, V(0.1f, 0.11f, 0f), V(0.13f, 0.11f, 0f), 0.07f, 0.07f, 14);
            Cyl(Tile.Brushed, V(0.13f, 0.11f, 0f), V(0.2f, 0.11f, 0f), 0.02f, 0.02f, 10);
            Box(Tile.DarkSteel, V(-0.02f, 0.015f, 0f), V(0.22f, 0.03f, 0.18f));
            Box(Tile.LabelPower, V(-0.02f, 0.21f, 0f), V(0.08f, 0.02f, 0.06f));
        }

        private static void FrameModel()
        {
            float s = 0.3f, b = 0.04f;
            Box(Tile.Steel, V(0f, b * 0.5f, -s * 0.5f + b * 0.5f), V(s, b, b));
            Box(Tile.Steel, V(0f, b * 0.5f, s * 0.5f - b * 0.5f), V(s, b, b));
            Box(Tile.Steel, V(-s * 0.5f + b * 0.5f, b * 0.5f, 0f), V(b, b, s));
            Box(Tile.Steel, V(s * 0.5f - b * 0.5f, b * 0.5f, 0f), V(b, b, s));
            Box(Tile.DarkSteel, V(0f, b * 0.5f, 0f), V(s * 1.3f, b * 0.6f, b * 0.6f), Quaternion.Euler(0f, 45f, 0f));
            foreach (float x in new[] { -0.13f, 0.13f })
            {
                foreach (float z in new[] { -0.13f, 0.13f })
                    Box(Tile.PaintedOrange, V(x, b + 0.006f, z), V(0.05f, 0.012f, 0.05f));
            }
        }

        private static void Circuit()
        {
            Box(Tile.Circuit, V(0f, 0.006f, 0f), V(0.28f, 0.012f, 0.2f));
            Box(Tile.Rubber, V(-0.04f, 0.022f, 0f), V(0.08f, 0.02f, 0.08f));
            Box(Tile.Rubber, V(0.08f, 0.018f, 0.05f), V(0.05f, 0.012f, 0.04f));
            Cyl(Tile.Aluminum, V(0.08f, 0.012f, -0.05f), V(0.08f, 0.05f, -0.05f), 0.015f, 0.015f, 10);
            Box(Tile.Gold, V(0f, 0.008f, 0.098f), V(0.2f, 0.01f, 0.006f));
        }

        private static void ComputerModel()
        {
            Box(Tile.PaintedDark, V(0f, 0.1f, 0f), V(0.26f, 0.2f, 0.2f));
            Box(Tile.Screen, V(0.0f, 0.12f, 0.101f), V(0.16f, 0.1f, 0.004f));
            Box(Tile.Grille, V(0.131f, 0.1f, 0f), V(0.004f, 0.14f, 0.14f));
            Box(Tile.LedGreen, V(0.1f, 0.04f, 0.101f), V(0.015f, 0.015f, 0.004f));
            Box(Tile.ControlPanel, V(-0.06f, 0.04f, 0.101f), V(0.08f, 0.03f, 0.004f));
        }

        private static void HeatSinkModel()
        {
            Box(Tile.Copper, V(0f, 0.01f, 0f), V(0.24f, 0.02f, 0.18f));
            for (int i = 0; i < 8; i++)
                Box(Tile.Aluminum, V(-0.105f + i * 0.03f, 0.07f, 0f), V(0.008f, 0.1f, 0.17f));
        }

        private static void HeavyFrameModel()
        {
            Box(Tile.Concrete, V(0f, 0.03f, 0f), V(0.34f, 0.06f, 0.34f));
            foreach (float x in new[] { -0.14f, 0.14f })
            {
                foreach (float z in new[] { -0.14f, 0.14f })
                    Box(Tile.Steel, V(x, 0.14f, z), V(0.05f, 0.16f, 0.05f));
            }
            Box(Tile.Steel, V(0f, 0.235f, -0.14f), V(0.34f, 0.03f, 0.05f));
            Box(Tile.Steel, V(0f, 0.235f, 0.14f), V(0.34f, 0.03f, 0.05f));
            Cyl(Tile.Pipe, V(-0.14f, 0.12f, -0.14f), V(0.14f, 0.12f, 0.14f), 0.018f, 0.018f, 8);
            Cyl(Tile.Pipe, V(0.14f, 0.12f, -0.14f), V(-0.14f, 0.12f, 0.14f), 0.018f, 0.018f, 8);
            Box(Tile.Hazard, V(0f, 0.062f, 0.171f), V(0.3f, 0.04f, 0.004f));
        }
    }
}
