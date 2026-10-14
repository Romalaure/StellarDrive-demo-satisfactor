using StellarIndustries.Parts;
using UnityEngine;

namespace StellarIndustries.Content
{
    /// <summary>Models of the 0.10 items: chemistry, alloys, electronics and top-tier parts.</summary>
    internal static partial class ItemModels
    {
        private static bool BuildAdvanced(uint id)
        {
            switch (id)
            {
                case ResourceCatalog.Charcoal: Lumps(Tile.Coal, 7); return true;
                case ResourceCatalog.Biomass: Sack(Tile.Biomass); return true;
                case ResourceCatalog.Silica: Sack(Tile.Concrete); return true;
                case ResourceCatalog.QuartzCrystal: Crystals(); return true;
                case ResourceCatalog.Plastic: Sheets(Tile.Plastic); return true;
                case ResourceCatalog.Rubber: RubberRoll(); return true;
                case ResourceCatalog.Resin: Canister(Tile.PaintedYellow, Tile.LabelWarning); return true;
                case ResourceCatalog.AlloyIngot: Ingots(Tile.IngotAluminum); Box(Tile.Copper, V(0.01f, 0.112f, 0f), V(0.18f, 0.005f, 0.05f)); return true;
                case ResourceCatalog.AlloySheet: Sheets(Tile.Aluminum); Box(Tile.Copper, V(0f, 0.052f, 0f), V(0.3f, 0.004f, 0.03f)); return true;
                case ResourceCatalog.AluminumCasing: Casings(); return true;
                case ResourceCatalog.FineWire: Spool(Tile.Copper, Tile.Plastic); return true;
                case ResourceCatalog.EncasedBeam: EncasedBeamModel(); return true;
                case ResourceCatalog.SmartPanel: SmartPanelModel(); return true;
                case ResourceCatalog.Framework: FrameworkModel(); return true;
                case ResourceCatalog.WiringHarness: HarnessModel(); return true;
                case ResourceCatalog.Oscillator: OscillatorModel(); return true;
                case ResourceCatalog.FastConnector: ConnectorModel(); return true;
                case ResourceCatalog.RadioModule: RadioModel(); return true;
                case ResourceCatalog.BatteryCell: BatteryModel(); return true;
                case ResourceCatalog.CoolingSystem: CoolingModel(); return true;
                case ResourceCatalog.EngineBlock: EngineBlockModel(); return true;
                case ResourceCatalog.ControlUnit: ControlUnitModel(); return true;
                case ResourceCatalog.Supercomputer: SupercomputerModel(); return true;
                case ResourceCatalog.TurboMotor: TurboModel(); return true;
                case ResourceCatalog.QuantumProcessor: QuantumModel(); return true;
                case ResourceCatalog.StellarMatter: MatterModel(); return true;
                case ResourceCatalog.ScannerMk1: ScannerModel(Tile.PaintedOrange, 1); return true;
                case ResourceCatalog.ScannerMk2: ScannerModel(Tile.PaintedBlue, 2); return true;
                case ResourceCatalog.ScannerMk3: ScannerModel(Tile.PaintedGreen, 3); return true;
                case ResourceCatalog.RangeModule: ModuleModel(Tile.Copper); return true;
                case ResourceCatalog.AnalysisModule: ModuleModel(Tile.Gold); return true;
                default: return false;
            }
        }

        // A handheld scanner: grip, body with its screen, an antenna per tier
        private static void ScannerModel(Tile paint, int tier)
        {
            Box(paint, V(0f, 0.035f, 0.02f), V(0.16f, 0.07f, 0.2f));
            Box(Tile.Screen, V(0f, 0.072f, 0.03f), V(0.12f, 0.006f, 0.12f));
            Box(Tile.Rubber, V(0f, 0.03f, -0.12f), V(0.07f, 0.05f, 0.1f));
            Box(Tile.ControlPanel, V(0f, 0.072f, -0.055f), V(0.1f, 0.006f, 0.03f));
            for (int i = 0; i < tier; i++)
            {
                float x = (i - (tier - 1) * 0.5f) * 0.045f;
                Cyl(Tile.Brushed, V(x, 0.05f, 0.11f), V(x, 0.05f + 0.07f + i * 0.02f, 0.14f), 0.006f, 0.004f, 6);
                Cyl(Tile.LedRed, V(x, 0.12f + i * 0.02f, 0.142f), V(x, 0.13f + i * 0.02f, 0.144f), 0.01f, 0.01f, 6);
            }
        }

        // An upgrade card: a board with a chip and gold contacts
        private static void ModuleModel(Tile contacts)
        {
            Box(Tile.Circuit, V(0f, 0.008f, 0f), V(0.16f, 0.016f, 0.1f));
            Box(Tile.PaintedDark, V(0.01f, 0.022f, 0f), V(0.06f, 0.012f, 0.05f));
            Box(contacts, V(-0.07f, 0.017f, 0f), V(0.02f, 0.003f, 0.09f));
            Box(Tile.LedGreen, V(0.055f, 0.02f, 0.035f), V(0.012f, 0.006f, 0.012f));
        }

        private static void Lumps(Tile tile, int count)
        {
            var rnd = new System.Random(11);
            for (int i = 0; i < count; i++)
            {
                var at = V((float)rnd.NextDouble() * 0.2f - 0.1f, 0.04f + (i / 4) * 0.05f, (float)rnd.NextDouble() * 0.2f - 0.1f);
                Box(tile, at, V(0.08f, 0.06f, 0.07f), Quaternion.Euler(rnd.Next(0, 60), rnd.Next(0, 360), rnd.Next(0, 60)));
            }
        }

        private static void Crystals()
        {
            Cyl(Tile.Glass, V(0f, 0f, 0f), V(0f, 0.2f, 0f), 0.05f, 0.005f, 6);
            Cyl(Tile.Glass, V(0.06f, 0f, 0.03f), V(0.1f, 0.14f, 0.05f), 0.04f, 0.004f, 6);
            Cyl(Tile.Glass, V(-0.06f, 0f, 0.02f), V(-0.11f, 0.12f, 0.03f), 0.035f, 0.004f, 6);
            Cyl(Tile.StellarDust, V(0f, -0.01f, 0f), V(0f, 0.02f, 0f), 0.12f, 0.1f, 8);
        }

        private static void RubberRoll()
        {
            Cyl(Tile.Rubber, V(0f, 0f, 0f), V(0f, 0.12f, 0f), 0.13f, 0.13f, 18);
            Cyl(Tile.DarkSteel, V(0f, 0.12f, 0f), V(0f, 0.125f, 0f), 0.04f, 0.04f, 10);
            Box(Tile.Rubber, V(0.15f, 0.005f, 0f), V(0.12f, 0.01f, 0.12f));
        }

        private static void Canister(Tile body, Tile label)
        {
            Box(body, V(0f, 0.12f, 0f), V(0.18f, 0.24f, 0.1f));
            Box(label, V(0f, 0.12f, 0.051f), V(0.1f, 0.1f, 0.004f));
            Cyl(Tile.DarkSteel, V(0.05f, 0.24f, 0f), V(0.05f, 0.28f, 0f), 0.025f, 0.025f, 8);
            Box(Tile.DarkSteel, V(-0.03f, 0.26f, 0f), V(0.08f, 0.02f, 0.03f));
        }

        private static void Casings()
        {
            foreach (float x in new[] { -0.07f, 0.07f })
            {
                Cyl(Tile.Aluminum, V(x, 0f, 0f), V(x, 0.18f, 0f), 0.065f, 0.065f, 14, capA: true, capB: false);
                Cyl(Tile.DarkSteel, V(x, 0.17f, 0f), V(x, 0.18f, 0f), 0.068f, 0.068f, 14);
            }
        }

        private static void EncasedBeamModel()
        {
            Box(Tile.Concrete, V(0f, 0.07f, 0f), V(0.38f, 0.14f, 0.16f));
            Box(Tile.Steel, V(0f, 0.07f, 0.081f), V(0.38f, 0.02f, 0.004f));
            Box(Tile.Steel, V(-0.191f, 0.07f, 0f), V(0.004f, 0.1f, 0.08f));
            Box(Tile.Steel, V(0.191f, 0.07f, 0f), V(0.004f, 0.1f, 0.08f));
        }

        private static void SmartPanelModel()
        {
            Box(Tile.Rivets, V(0f, 0.02f, 0f), V(0.32f, 0.04f, 0.26f));
            Cyl(Tile.PaintedBlue, V(0f, 0.04f, -0.06f), V(0f, 0.12f, -0.06f), 0.07f, 0.07f, 12);
            Box(Tile.ControlPanel, V(0.08f, 0.045f, 0.07f), V(0.12f, 0.01f, 0.08f));
            Box(Tile.LedGreen, V(-0.11f, 0.045f, 0.09f), V(0.03f, 0.01f, 0.03f));
        }

        private static void FrameworkModel()
        {
            FrameModel();
            Box(Tile.Steel, V(0f, 0.1f, -0.13f), V(0.36f, 0.04f, 0.04f));
            Box(Tile.Steel, V(0f, 0.1f, 0.13f), V(0.36f, 0.04f, 0.04f));
            foreach (float x in new[] { -0.13f, 0.13f })
            {
                foreach (float z in new[] { -0.13f, 0.13f })
                    Box(Tile.PaintedYellow, V(x, 0.06f, z), V(0.03f, 0.08f, 0.03f));
            }
        }

        private static void HarnessModel()
        {
            StatorModel();
            for (int i = 0; i < 3; i++)
            {
                float z = -0.05f + i * 0.05f;
                Cyl(Tile.CableJacket, V(0.1f, 0.15f, z), V(0.22f, 0.02f, z + 0.04f), 0.012f, 0.012f, 6);
            }
        }

        private static void OscillatorModel()
        {
            Cyl(Tile.Aluminum, V(0f, 0f, 0f), V(0f, 0.06f, 0f), 0.12f, 0.12f, 14);
            Cyl(Tile.Glass, V(0f, 0.06f, 0f), V(0f, 0.18f, 0f), 0.05f, 0.005f, 6);
            Cyl(Tile.Glass, V(0.06f, 0.06f, 0.02f), V(0.07f, 0.14f, 0.02f), 0.03f, 0.004f, 6);
            Box(Tile.Gold, V(0f, 0.062f, 0.09f), V(0.1f, 0.006f, 0.03f));
        }

        private static void ConnectorModel()
        {
            Box(Tile.PaintedDark, V(0f, 0.04f, 0f), V(0.22f, 0.08f, 0.12f));
            for (int i = 0; i < 5; i++)
                Box(Tile.Gold, V(-0.08f + i * 0.04f, 0.04f, 0.062f), V(0.015f, 0.04f, 0.006f));
            Cyl(Tile.CableJacket, V(0f, 0.04f, -0.06f), V(0f, 0.04f, -0.18f), 0.025f, 0.025f, 8);
        }

        private static void RadioModel()
        {
            Box(Tile.PaintedLight, V(0f, 0.08f, 0f), V(0.24f, 0.16f, 0.16f));
            Box(Tile.Grille, V(-0.04f, 0.08f, 0.081f), V(0.12f, 0.1f, 0.004f));
            Box(Tile.Screen, V(0.08f, 0.1f, 0.081f), V(0.06f, 0.05f, 0.004f));
            Cyl(Tile.Brushed, V(0.09f, 0.16f, -0.05f), V(0.09f, 0.34f, -0.05f), 0.008f, 0.004f, 6);
            Cyl(Tile.LedRed, V(0.09f, 0.34f, -0.05f), V(0.09f, 0.35f, -0.05f), 0.012f, 0.012f, 6);
        }

        private static void BatteryModel()
        {
            Cyl(Tile.PaintedGreen, V(0f, 0f, 0f), V(0f, 0.22f, 0f), 0.07f, 0.07f, 14);
            Cyl(Tile.Aluminum, V(0f, 0.22f, 0f), V(0f, 0.25f, 0f), 0.03f, 0.03f, 10);
            Box(Tile.LabelPower, V(0f, 0.11f, 0.068f), V(0.07f, 0.07f, 0.004f));
        }

        private static void CoolingModel()
        {
            HeatSinkModel();
            Cyl(Tile.Pipe, V(-0.12f, 0.14f, -0.06f), V(0.12f, 0.14f, -0.06f), 0.02f, 0.02f, 8);
            Cyl(Tile.Pipe, V(-0.12f, 0.14f, 0.06f), V(0.12f, 0.14f, 0.06f), 0.02f, 0.02f, 8);
            Cyl(Tile.PaintedBlue, V(0.16f, 0.02f, 0f), V(0.16f, 0.18f, 0f), 0.05f, 0.05f, 10);
        }

        private static void EngineBlockModel()
        {
            Box(Tile.Rivets, V(0f, 0.015f, 0f), V(0.36f, 0.03f, 0.24f));
            MotorModel();
            Box(Tile.Rubber, V(0.17f, 0.11f, 0.08f), V(0.02f, 0.18f, 0.03f));
        }

        private static void ControlUnitModel()
        {
            Box(Tile.PaintedOrange, V(0f, 0.09f, 0f), V(0.26f, 0.18f, 0.18f));
            Box(Tile.Screen, V(-0.03f, 0.11f, 0.091f), V(0.14f, 0.08f, 0.004f));
            Box(Tile.ControlPanel, V(0.09f, 0.08f, 0.091f), V(0.06f, 0.1f, 0.004f));
            Box(Tile.Grille, V(0f, 0.181f, 0f), V(0.2f, 0.004f, 0.12f));
        }

        private static void SupercomputerModel()
        {
            foreach (float x in new[] { -0.09f, 0.09f })
            {
                Box(Tile.PaintedDark, V(x, 0.13f, 0f), V(0.16f, 0.26f, 0.18f));
                Box(Tile.Grille, V(x, 0.15f, 0.091f), V(0.12f, 0.18f, 0.004f));
                for (int i = 0; i < 4; i++)
                    Box(i % 2 == 0 ? Tile.LedGreen : Tile.LedRed, V(x - 0.05f + i * 0.03f, 0.03f, 0.091f), V(0.012f, 0.012f, 0.004f));
            }
        }

        private static void TurboModel()
        {
            MotorModel();
            Cyl(Tile.Grille, V(-0.22f, 0.11f, 0f), V(-0.17f, 0.11f, 0f), 0.12f, 0.12f, 16);
            Cyl(Tile.Copper, V(-0.1f, 0.24f, 0f), V(0.08f, 0.24f, 0f), 0.025f, 0.025f, 8);
            Box(Tile.PaintedOrange, V(-0.02f, 0.21f, 0f), V(0.04f, 0.04f, 0.04f));
        }

        private static void QuantumModel()
        {
            Box(Tile.Circuit, V(0f, 0.008f, 0f), V(0.24f, 0.016f, 0.24f));
            Box(Tile.Gold, V(0f, 0.025f, 0f), V(0.12f, 0.02f, 0.12f));
            Cyl(Tile.StellarDust, V(0f, 0.035f, 0f), V(0f, 0.07f, 0f), 0.045f, 0.03f, 10);
        }

        private static void MatterModel()
        {
            Cyl(Tile.Glass, V(0f, 0f, 0f), V(0f, 0.2f, 0f), 0.09f, 0.09f, 14, capA: true, capB: true);
            Cyl(Tile.StellarDust, V(0f, 0.04f, 0f), V(0f, 0.16f, 0f), 0.05f, 0.05f, 10);
            Cyl(Tile.DarkSteel, V(0f, -0.01f, 0f), V(0f, 0.02f, 0f), 0.1f, 0.1f, 14);
            Cyl(Tile.DarkSteel, V(0f, 0.19f, 0f), V(0f, 0.22f, 0f), 0.1f, 0.1f, 14);
        }
    }
}
