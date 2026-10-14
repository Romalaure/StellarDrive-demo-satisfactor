using System.Globalization;
using StellarIndustries.Production;
using UnityEngine;

namespace StellarIndustries.Parts
{
    /// <summary>
    /// The conveyors' models (0.13). Items go towards +z: the belt's chevrons point that way.
    /// While a conveyor moves items, crates ride along its belt (or buckets up the lift).
    /// </summary>
    internal static partial class MachineModels
    {
        private static string F(float v) => v.ToString("0.###", CultureInfo.InvariantCulture);

        private static Tile TierPaint(MachineDef def) =>
            def.PartId == MachineCatalog.BeltMk3 ? Tile.PaintedGreen : def.PartId == MachineCatalog.BeltMk2 ? Tile.PaintedBlue : Tile.PaintedOrange;

        // Belt: a frame on four legs, a rubber belt over two rollers, side rails in the tier's color
        private static void Belt(ModelKit k, float w, float d, MachineDef def)
        {
            float top = 0.3f, half = d * 0.5f, side = w * 0.5f - 0.05f;
            Tile paint = TierPaint(def);
            foreach (float x in new[] { -side, side })
            {
                // Side frame, a rail on top, two legs with feet
                k.Box(paint, P(x, 0.2f, 0f), new Vector3(0.06f, 0.16f, d));
                k.Box(Tile.Brushed, P(x, top + 0.03f, 0f), new Vector3(0.05f, 0.025f, d));
                foreach (float z in new[] { -half + 0.12f, half - 0.12f })
                {
                    k.Box(Tile.DarkSteel, P(x, 0.065f, z), new Vector3(0.05f, 0.13f, 0.05f));
                    k.Box(Tile.Rubber, P(x, 0.01f, z), new Vector3(0.09f, 0.02f, 0.09f));
                }
            }
            // Cross members under the belt
            foreach (float z in new[] { -half + 0.12f, 0f, half - 0.12f })
                k.Box(Tile.DarkSteel, P(0f, 0.15f, z), new Vector3(w - 0.14f, 0.04f, 0.05f));
            // The belt (chevrons along +z) and its return run underneath
            k.Box(Tile.Belt, P(0f, top - 0.02f, 0f), new Vector3(w - 0.16f, 0.03f, d - 0.02f));
            k.Box(Tile.Rubber, P(0f, 0.2f, 0f), new Vector3(w - 0.18f, 0.02f, d - 0.12f));
            // End rollers, turning while it runs
            foreach (float z in new[] { -half + 0.05f, half - 0.05f })
            {
                Vector3 axle = P(0f, top - 0.04f, z);
                k.Moving("kind=spin axis=1,0,0 speed=" + F(def.ConveyorRate * 120f), axle)
                    .Cylinder(Tile.Steel, axle + new Vector3(-(w - 0.17f) * 0.5f, 0f, 0f), axle + new Vector3((w - 0.17f) * 0.5f, 0f, 0f), 0.035f, 0.035f, 10);
            }
            Crates(k, P(0f, top + 0.06f, -half + 0.08f), d * 0.5f, Mathf.Min(1.4f, 0.35f + def.ConveyorRate * 0.2f));
        }

        /// <summary>Two crates riding the belt from <paramref name="start"/>, half a belt apart, looping.</summary>
        private static void Crates(ModelKit k, Vector3 start, float spacing, float speed)
        {
            ModelKit ride = k.Status(true).Moving($"kind=slide axis=0,0,1 speed={F(speed)} amp={F(spacing)}", start);
            for (int i = 0; i < 2; i++)
            {
                Vector3 at = start + new Vector3(0f, 0f, i * spacing);
                ride.Box(Tile.Sack, at, new Vector3(0.16f, 0.1f, 0.14f));
                ride.Box(Tile.CableJacket, at + new Vector3(0f, 0.051f, 0f), new Vector3(0.03f, 0.004f, 0.142f));
            }
        }

        // Bucket lift: a boot with a hopper at the back, an open frame with the bucket belt, a head with its motor and a chute to the front
        private static void Lift(ModelKit k, float w, float d, float h)
        {
            float c = 0.22f;
            // Boot and its intake hopper at the back, at floor level
            k.Box(Tile.DarkSteel, P(0f, 0.2f, 0f), new Vector3(0.56f, 0.4f, 0.56f));
            k.Box(Tile.Hazard, P(0f, 0.38f, 0.285f), new Vector3(0.5f, 0.04f, 0.01f));
            k.Cylinder(Tile.Brushed, P(0f, 0.12f, -d * 0.5f + 0.05f), P(0f, 0.38f, -d * 0.5f + 0.12f), 0.08f, 0.17f, 4, capA: true, capB: false);
            k.Cylinder(Tile.Hazard, P(0f, 0.37f, -d * 0.5f + 0.12f), P(0f, 0.4f, -d * 0.5f + 0.12f), 0.175f, 0.175f, 4, capA: false, capB: false);
            // Frame: four corner posts with braces, from the boot to the head
            float frameTop = h - 0.38f;
            foreach (float x in new[] { -c, c })
            {
                foreach (float z in new[] { -c, c })
                    k.Box(Tile.PaintedOrange, P(x, (0.4f + frameTop) * 0.5f, z), new Vector3(0.05f, frameTop - 0.4f, 0.05f));
            }
            for (float y = 0.75f; y < frameTop - 0.2f; y += 0.45f)
            {
                k.Box(Tile.DarkSteel, P(0f, y, c), new Vector3(c * 2f, 0.03f, 0.03f));
                k.Box(Tile.DarkSteel, P(0f, y, -c), new Vector3(c * 2f, 0.03f, 0.03f));
                k.Box(Tile.DarkSteel, P(c, y, 0f), new Vector3(0.03f, 0.03f, c * 2f));
                k.Box(Tile.DarkSteel, P(-c, y, 0f), new Vector3(0.03f, 0.03f, c * 2f));
            }
            // Bucket belt: up at the front, down at the back, buckets going up while it runs
            k.Box(Tile.Rubber, P(0f, (0.4f + frameTop) * 0.5f, 0.06f), new Vector3(0.22f, frameTop - 0.4f, 0.02f));
            k.Box(Tile.Rubber, P(0f, (0.4f + frameTop) * 0.5f, -0.06f), new Vector3(0.22f, frameTop - 0.4f, 0.02f));
            float run = (frameTop - 0.45f) * 0.5f;
            ModelKit buckets = k.Status(true).Moving($"kind=slide axis=0,1,0 speed=0.5 amp={F(run / 2f)}", P(0f, 0.45f, 0.1f));
            for (int i = 0; i < 4; i++)
            {
                Vector3 at = P(0f, 0.45f + i * run / 2f, 0.11f);
                buckets.Box(Tile.PaintedYellow, at, new Vector3(0.2f, 0.07f, 0.07f));
                buckets.Box(Tile.DarkSteel, at + new Vector3(0f, -0.03f, -0.03f), new Vector3(0.2f, 0.015f, 0.02f));
            }
            // Head: housing, drive motor on the side, discharge chute towards the front top
            k.Box(Tile.PaintedOrange, P(0f, h - 0.22f, 0f), new Vector3(0.56f, 0.32f, 0.56f));
            k.Box(Tile.DarkSteel, P(0f, h - 0.05f, 0f), new Vector3(0.6f, 0.03f, 0.6f));
            k.Cylinder(Tile.PaintedBlue, P(0.28f, h - 0.22f, -0.05f), P(0.42f, h - 0.22f, -0.05f), 0.1f, 0.1f, 12);
            k.Cylinder(Tile.Grille, P(0.42f, h - 0.22f, -0.05f), P(0.44f, h - 0.22f, -0.05f), 0.085f, 0.085f, 12);
            k.Box(Tile.DarkSteel, P(0.33f, h - 0.33f, -0.05f), new Vector3(0.14f, 0.03f, 0.18f));
            k.Beam(Tile.Brushed, P(0f, h - 0.2f, 0.25f), P(0f, h - 0.32f, d * 0.5f - 0.03f), 0.24f, 0.08f);
            k.Box(Tile.ArrowLabel, P(0f, h - 0.18f, 0.285f), new Vector3(0.14f, 0.12f, 0.01f));
            // Service ladder on the left side
            k.Ladder(P(-0.32f, 0.02f, 0f), h - 0.4f, 0.22f);
            k.Box(Tile.DarkSteel, P(-0.27f, h - 0.6f, 0f), new Vector3(0.1f, 0.03f, 0.04f));
            k.Box(Tile.DarkSteel, P(-0.27f, 0.6f, 0f), new Vector3(0.1f, 0.03f, 0.04f));
        }

        // Splitter and merger: a low hub with a belt stub on each side and a turning paddle on top
        private static void Hub(ModelKit k, float w, float d, bool split)
        {
            float half = d * 0.5f, edge = w * 0.5f;
            // Base on four feet
            k.Box(Tile.DarkSteel, P(0f, 0.08f, 0f), new Vector3(w - 0.1f, 0.1f, d - 0.1f));
            foreach (float x in new[] { -edge + 0.1f, edge - 0.1f })
            {
                foreach (float z in new[] { -half + 0.1f, half - 0.1f })
                    k.Box(Tile.Rubber, P(x, 0.015f, z), new Vector3(0.1f, 0.03f, 0.1f));
            }
            // Belt stubs: front and back carry chevrons (they run along z), the sides plain rubber
            k.Box(Tile.Belt, P(0f, 0.3f, -half * 0.62f), new Vector3(0.5f, 0.03f, half * 0.76f));
            k.Box(Tile.Belt, P(0f, 0.3f, half * 0.62f), new Vector3(0.5f, 0.03f, half * 0.76f));
            k.Box(Tile.Rubber, P(-edge * 0.62f, 0.3f, 0f), new Vector3(edge * 0.76f, 0.03f, 0.5f));
            k.Box(Tile.Rubber, P(edge * 0.62f, 0.3f, 0f), new Vector3(edge * 0.76f, 0.03f, 0.5f));
            foreach (float s in new[] { -1f, 1f })
            {
                // Guide rails along each stub, stopping short of the corners so they never cross
                k.Box(Tile.Brushed, P(s * 0.27f, 0.25f, -half * 0.79f), new Vector3(0.04f, 0.14f, half * 0.42f));
                k.Box(Tile.Brushed, P(s * 0.27f, 0.25f, half * 0.79f), new Vector3(0.04f, 0.14f, half * 0.42f));
                k.Box(Tile.Brushed, P(-edge * 0.79f, 0.25f, s * 0.27f), new Vector3(edge * 0.42f, 0.14f, 0.04f));
                k.Box(Tile.Brushed, P(edge * 0.79f, 0.25f, s * 0.27f), new Vector3(edge * 0.42f, 0.14f, 0.04f));
            }
            // Central drum with the paddle that sends items one way after the other
            Tile paint = split ? Tile.PaintedBlue : Tile.PaintedOrange;
            k.Cylinder(paint, P(0f, 0.13f, 0f), P(0f, 0.5f, 0f), 0.2f, 0.2f, 16);
            k.Cylinder(Tile.DarkSteel, P(0f, 0.5f, 0f), P(0f, 0.53f, 0f), 0.22f, 0.22f, 16);
            Vector3 hub = P(0f, 0.555f, 0f);
            ModelKit paddle = k.Moving("kind=spin axis=0,1,0 speed=" + (split ? "240" : "-240"), hub);
            paddle.Cylinder(Tile.DarkSteel, hub + new Vector3(0f, -0.025f, 0f), hub + new Vector3(0f, 0.03f, 0f), 0.04f, 0.04f, 8);
            paddle.Box(Tile.Hazard, hub, new Vector3(0.36f, 0.03f, 0.05f));
            paddle.Box(Tile.Hazard, hub, new Vector3(0.05f, 0.03f, 0.36f));
            // Arrows on the drum: where the items go
            k.Box(Tile.ArrowLabel, P(0f, 0.38f, 0.2f), new Vector3(0.12f, 0.1f, 0.01f));
            k.Box(Tile.LedGreen, P(0.12f, 0.45f, 0.17f), new Vector3(0.03f, 0.03f, 0.01f));
        }
    }
}
