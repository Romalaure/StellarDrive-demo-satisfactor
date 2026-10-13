using UnityEngine;

namespace StellarIndustries.Parts
{
    /// <summary>Models of the advanced machines (0.10): blast furnace, refinery, accumulator, particle accelerator.</summary>
    internal static partial class MachineModels
    {
        /// <summary>A ring of tube segments around <paramref name="axis"/> (an approximated torus).</summary>
        private static void Ring(ModelKit k, Tile tile, Vector3 center, Vector3 axis, float radius, float tube, int segments)
        {
            Vector3 n = axis.normalized;
            Vector3 u = Vector3.Cross(n, Mathf.Abs(n.y) < 0.9f ? Vector3.up : Vector3.right).normalized, v = Vector3.Cross(n, u);
            for (int i = 0; i < segments; i++)
            {
                float a0 = i * Mathf.PI * 2f / segments, a1 = (i + 1) * Mathf.PI * 2f / segments;
                Vector3 p0 = center + (u * Mathf.Cos(a0) + v * Mathf.Sin(a0)) * radius, p1 = center + (u * Mathf.Cos(a1) + v * Mathf.Sin(a1)) * radius;
                // Segments overlap a little so the joints show no notch
                Vector3 along = (p1 - p0).normalized * tube * 0.6f;
                k.Tube(tile, p0 - along, p1 + along, tube, 8);
            }
        }

        // ---- Foundry: a banded blast furnace, two charging hoppers (its two ingredients), a tapping spout, a ladle

        private static void Foundry(ModelKit k, float w, float d)
        {
            Skid(k, w, d);
            k.Cylinder(Tile.Rust, P(-0.05f, 0.12f, 0f), P(-0.05f, 1.3f, 0f), 0.5f, 0.38f, 12);
            for (int i = 0; i < 4; i++)
                k.Cylinder(Tile.Rivets, P(-0.05f, 0.3f + i * 0.28f, 0f), P(-0.05f, 0.35f + i * 0.28f, 0f), 0.52f - i * 0.03f, 0.52f - i * 0.03f, 12);
            k.Cylinder(Tile.DarkSteel, P(-0.05f, 1.3f, 0f), P(-0.05f, 1.45f, 0f), 0.4f, 0.32f, 12);
            k.Tube(Tile.Pipe, P(-0.05f, 1.45f, 0f), P(-0.05f, 2.05f, 0f), 0.12f, 12);
            k.Cylinder(Tile.DarkSteel, P(-0.05f, 2.0f, 0f), P(-0.05f, 2.08f, 0f), 0.16f, 0.16f, 12);
            foreach (float x in new[] { -0.45f, 0.35f })
            {
                k.Cylinder(Tile.PaintedYellow, P(x, 1.35f, -0.25f), P(x, 1.7f, -0.25f), 0.08f, 0.2f, 4, capA: true, capB: false);
                k.Pipe(Tile.Pipe, 0.05f, P(x, 1.35f, -0.25f), P(x * 0.5f, 1.25f, -0.12f));
            }
            k.Frame(Tile.DarkSteel, P(-0.05f, 0.42f, 0.47f), new Vector2(0.34f, 0.28f), 0.05f, Vector3.forward, 0.06f);
            k.Moving("kind=glow freq=1.1", P(-0.05f, 0.42f, 0.46f)).Box(Tile.Furnace, P(-0.05f, 0.42f, 0.455f), new Vector3(0.24f, 0.18f, 0.02f));
            k.Box(Tile.DarkSteel, P(-0.05f, 0.33f, 0.52f), new Vector3(0.12f, 0.05f, 0.14f), Quaternion.Euler(20f, 0f, 0f));
            k.Box(Tile.PaintedOrange, P(0.5f, 0.6f, 0.3f), new Vector3(0.06f, 1.0f, 0.06f));
            ModelKit ladle = k.Moving("kind=swing axis=0,1,0 amp=25 freq=0.2", P(0.5f, 0.9f, 0.3f));
            ladle.Box(Tile.DarkSteel, P(0.5f, 0.92f, 0.37f), new Vector3(0.03f, 0.03f, 0.16f));
            ladle.Tube(Tile.DarkSteel, P(0.5f, 0.92f, 0.44f), P(0.5f, 0.58f, 0.44f), 0.015f, 6);
            ladle.Cylinder(Tile.Rust, P(0.5f, 0.4f, 0.44f), P(0.5f, 0.56f, 0.44f), 0.07f, 0.1f, 10);
            Console(k, P(-0.5f, 0.75f, 0.38f), 0.26f, Tile.ScreenAmber);
            k.Cylinder(Tile.Gauge, P(-0.5f, 1.02f, 0.38f), P(-0.5f, 1.02f, 0.44f), 0.06f, 0.06f, 16);
        }

        // ---- Refinery: a distillation column with its ladder, two product tanks, piping, a pump and a flare

        private static void Refinery(ModelKit k, float w, float d)
        {
            Skid(k, w, d);
            k.Cylinder(Tile.Brushed, P(-0.35f, 0.12f, -0.1f), P(-0.35f, 1.85f, -0.1f), 0.22f, 0.22f, 16);
            for (int i = 0; i < 5; i++)
                k.Cylinder(Tile.DarkSteel, P(-0.35f, 0.3f + i * 0.33f, -0.1f), P(-0.35f, 0.33f + i * 0.33f, -0.1f), 0.24f, 0.24f, 16);
            k.Cylinder(Tile.Brushed, P(-0.35f, 1.85f, -0.1f), P(-0.35f, 1.95f, -0.1f), 0.22f, 0.12f, 16);
            k.Ladder(P(-0.35f, 0.12f, 0.15f), 1.75f, 0.26f);
            k.Tube(Tile.Pipe, P(-0.62f, 0.12f, -0.4f), P(-0.62f, 1.95f, -0.4f), 0.04f, 8);
            k.Moving("kind=glow freq=2.4", P(-0.62f, 2.0f, -0.4f)).Cylinder(Tile.Furnace, P(-0.62f, 1.95f, -0.4f), P(-0.62f, 2.05f, -0.4f), 0.05f, 0.01f, 8);
            foreach (float z in new[] { -0.25f, 0.2f })
            {
                k.Cylinder(z < 0f ? Tile.PaintedGreen : Tile.PaintedBlue, P(0.05f, 0.45f, z), P(0.62f, 0.45f, z), 0.17f, 0.17f, 16);
                k.Box(Tile.DarkSteel, P(0.15f, 0.25f, z), new Vector3(0.06f, 0.2f, 0.3f));
                k.Box(Tile.DarkSteel, P(0.52f, 0.25f, z), new Vector3(0.06f, 0.2f, 0.3f));
                k.Bolts(P(0.62f, 0.45f, z), Vector3.right, 0.11f, 6, 0.025f);
            }
            k.Pipe(Tile.Pipe, 0.035f, P(-0.13f, 1.4f, -0.1f), P(0.3f, 1.4f, -0.1f), P(0.3f, 0.62f, -0.25f));
            k.Pipe(Tile.Pipe, 0.035f, P(-0.13f, 0.9f, -0.1f), P(0.25f, 0.9f, -0.1f), P(0.25f, 0.9f, 0.2f), P(0.25f, 0.76f, 0.2f));
            k.Cylinder(Tile.PaintedOrange, P(0.25f, 0.61f, 0.2f), P(0.25f, 0.76f, 0.2f), 0.09f, 0.09f, 12);
            k.Moving("kind=spin axis=0,1,0 speed=300", P(0.25f, 0.78f, 0.2f)).Cylinder(Tile.Fan, P(0.25f, 0.76f, 0.2f), P(0.25f, 0.79f, 0.2f), 0.08f, 0.08f, 12);
            Console(k, P(0.45f, 0.45f, 0.5f), 0.24f);
        }

        // ---- Accumulator: racks of cells behind a frame, terminals and busbars on top, a charge bar

        private static void Accumulator(ModelKit k, float w, float d)
        {
            Skid(k, w, d);
            k.Box(Tile.PaintedDark, P(0f, 0.75f, -0.05f), new Vector3(1.2f, 1.25f, 0.85f));
            for (int row = 0; row < 3; row++)
            {
                for (int i = 0; i < 6; i++)
                {
                    Vector3 c = P(-0.45f + i * 0.18f, 0.35f + row * 0.36f, 0.38f);
                    k.Cylinder(Tile.PaintedGreen, c - new Vector3(0f, 0.13f, 0f), c + new Vector3(0f, 0.13f, 0f), 0.065f, 0.065f, 12);
                    k.Cylinder(Tile.Aluminum, c + new Vector3(0f, 0.13f, 0f), c + new Vector3(0f, 0.15f, 0f), 0.03f, 0.03f, 8);
                }
            }
            k.Frame(Tile.PaintedYellow, P(0f, 0.72f, 0.43f), new Vector2(1.2f, 1.15f), 0.05f, Vector3.forward, 0.04f);
            k.Box(Tile.DarkSteel, P(0.66f, 0.75f, 0.2f), new Vector3(0.06f, 0.9f, 0.12f));
            k.Moving("kind=glow freq=0.8", P(0.69f, 0.75f, 0.2f)).Box(Tile.LedGreen, P(0.695f, 0.75f, 0.2f), new Vector3(0.01f, 0.8f, 0.08f));
            foreach (float x in new[] { -0.3f, 0.3f })
            {
                k.Cylinder(x < 0f ? Tile.LedRed : Tile.DarkSteel, P(x, 1.38f, 0f), P(x, 1.48f, 0f), 0.06f, 0.06f, 10);
                k.Box(Tile.Copper, P(x, 1.4f, -0.2f), new Vector3(0.08f, 0.03f, 0.4f));
            }
            for (int i = 0; i < 6; i++)
                k.Box(Tile.Aluminum, P(-0.62f, 0.4f + i * 0.14f, -0.05f), new Vector3(0.05f, 0.02f, 0.7f));
            StatusLight(k, P(0.45f, 1.27f, 0.43f), new Vector3(0.05f, 0.05f, 0.02f));
            k.Box(Tile.LabelPower, P(-0.45f, 1.25f, 0.43f), new Vector3(0.14f, 0.14f, 0.02f));
        }

        // ---- Accelerator: a ring of magnets around a vacuum tube, a particle running round, a glowing core

        private static void Accelerator(ModelKit k, float w, float d)
        {
            Skid(k, w, d, 0.14f);
            Vector3 center = P(0f, 1.15f, 0f);
            for (int i = 0; i < 4; i++)
            {
                float a = i * Mathf.PI * 0.5f + Mathf.PI * 0.25f;
                Vector3 foot = P(Mathf.Cos(a) * 0.62f, 0.12f, Mathf.Sin(a) * 0.55f);
                k.Box(Tile.PaintedBlue, foot + new Vector3(0f, 0.5f, 0f), new Vector3(0.1f, 1.0f, 0.1f));
            }
            Ring(k, Tile.Brushed, center, Vector3.up, 0.7f, 0.06f, 24);
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI * 0.25f;
                Vector3 at = center + new Vector3(Mathf.Cos(a) * 0.7f, 0f, Mathf.Sin(a) * 0.7f);
                Quaternion turn = Quaternion.Euler(0f, -a * Mathf.Rad2Deg, 0f);
                k.Box(Tile.PaintedOrange, at, new Vector3(0.16f, 0.18f, 0.16f), turn);
                k.Box(Tile.Winding, at + new Vector3(0f, 0.1f, 0f), new Vector3(0.1f, 0.03f, 0.1f), turn);
            }
            // The particle: a glowing bead running round the ring
            k.Moving("kind=spin axis=0,1,0 speed=720", center)
                .Cylinder(Tile.StellarDust, center + new Vector3(0.7f, -0.04f, 0f), center + new Vector3(0.7f, 0.04f, 0f), 0.045f, 0.045f, 8);
            k.Cylinder(Tile.DarkSteel, P(0f, 0.12f, 0f), P(0f, 0.85f, 0f), 0.22f, 0.18f, 14);
            k.Moving("kind=glow freq=1.7", center).Cylinder(Tile.StellarDust, P(0f, 0.85f, 0f), P(0f, 1.25f, 0f), 0.16f, 0.16f, 14);
            k.Cylinder(Tile.Glass, P(0f, 1.25f, 0f), P(0f, 1.32f, 0f), 0.2f, 0.14f, 14);
            k.Pipe(Tile.Pipe, 0.035f, P(0.3f, 0.12f, 0.2f), P(0.3f, 0.6f, 0.2f), P(0.5f, 1.1f, 0.42f));
            k.Pipe(Tile.Pipe, 0.035f, P(-0.3f, 0.12f, 0.2f), P(-0.3f, 0.6f, 0.2f), P(-0.5f, 1.1f, 0.42f));
            Console(k, P(0.6f, 0.45f, d * 0.5f - 0.12f), 0.36f);
            // Warning sign on its own post
            k.Box(Tile.DarkSteel, P(-0.55f, 0.27f, d * 0.5f - 0.1f), new Vector3(0.04f, 0.3f, 0.04f));
            k.Box(Tile.LabelWarning, P(-0.55f, 0.45f, d * 0.5f - 0.08f), new Vector3(0.18f, 0.16f, 0.02f));
        }
    }
}
