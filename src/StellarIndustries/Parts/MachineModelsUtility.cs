using UnityEngine;

namespace StellarIndustries.Parts
{
    /// <summary>Models of the 0.12 machines: burner, condenser, reactor, encoder, converter, recycler, exchange counter, radar.</summary>
    internal static partial class MachineModels
    {
        /// <summary>A sphere-like ball of stacked rings (no native sphere in the kit).</summary>
        private static void Ball(ModelKit k, Tile tile, Vector3 center, float radius, int bands = 6)
        {
            for (int i = 0; i < bands; i++)
            {
                float a0 = -Mathf.PI * 0.5f + Mathf.PI * i / bands, a1 = -Mathf.PI * 0.5f + Mathf.PI * (i + 1) / bands;
                k.Cylinder(tile, center + Vector3.up * Mathf.Sin(a0) * radius, center + Vector3.up * Mathf.Sin(a1) * radius,
                    Mathf.Cos(a0) * radius + 0.001f, Mathf.Cos(a1) * radius + 0.001f, 16, capA: i == 0, capB: i == bands - 1);
            }
        }

        // ---- Biomass burner: a squat round stove, glowing door, fuel hatch, chimney

        private static void BiomassBurner(ModelKit k, float w, float d)
        {
            Skid(k, w, d);
            k.Cylinder(Tile.Rust, P(0f, 0.1f, 0.02f), P(0f, 0.85f, 0.02f), 0.36f, 0.33f, 16);
            k.Cylinder(Tile.DarkSteel, P(0f, 0.85f, 0.02f), P(0f, 0.95f, 0.02f), 0.36f, 0.2f, 16);
            for (int i = 0; i < 2; i++)
                k.Cylinder(Tile.Rivets, P(0f, 0.25f + i * 0.42f, 0.02f), P(0f, 0.3f + i * 0.42f, 0.02f), 0.375f - i * 0.02f, 0.375f - i * 0.02f, 16);
            k.Frame(Tile.DarkSteel, P(0f, 0.42f, 0.36f), new Vector2(0.3f, 0.26f), 0.04f, Vector3.forward, 0.05f);
            k.Moving("kind=glow freq=1.8", P(0f, 0.42f, 0.35f)).Box(Tile.Furnace, P(0f, 0.42f, 0.355f), new Vector3(0.22f, 0.18f, 0.02f));
            k.Box(Tile.DarkSteel, P(0f, 0.17f, 0.4f), new Vector3(0.3f, 0.08f, 0.12f));
            // Chimney with its cap, gauge on the side
            k.Tube(Tile.Pipe, P(0f, 0.95f, 0.02f), P(0f, 1.45f, 0.02f), 0.08f, 12);
            k.Cylinder(Tile.DarkSteel, P(0f, 1.45f, 0.02f), P(0f, 1.5f, 0.02f), 0.13f, 0.06f, 12);
            k.Cylinder(Tile.Gauge, P(0.3f, 0.7f, 0.18f), P(0.33f, 0.71f, 0.2f), 0.06f, 0.06f, 14);
            // Fuel hatch on the left with its lid
            k.Box(Tile.DarkSteel, P(-0.38f, 0.62f, 0.02f), new Vector3(0.12f, 0.2f, 0.22f));
            k.Box(Tile.Hazard, P(-0.38f, 0.73f, 0.02f), new Vector3(0.14f, 0.03f, 0.24f));
            StatusLight(k, P(0.25f, 0.95f, 0.26f), new Vector3(0.04f, 0.04f, 0.02f));
        }

        // ---- Water condenser: a frosted tank with cooling fins, a fan on top, an ice tray below

        private static void Condenser(ModelKit k, float w, float d)
        {
            Skid(k, w, d);
            k.Box(Tile.PaintedLight, P(0f, 0.3f, -0.05f), new Vector3(1.0f, 0.4f, 0.9f));
            k.Cylinder(Tile.Aluminum, P(-0.05f, 0.5f, -0.05f), P(-0.05f, 1.5f, -0.05f), 0.33f, 0.33f, 18);
            for (int i = 0; i < 6; i++)
                k.Cylinder(Tile.DarkSteel, P(-0.05f, 0.62f + i * 0.15f, -0.05f), P(-0.05f, 0.65f + i * 0.15f, -0.05f), 0.4f, 0.4f, 18);
            k.Cylinder(Tile.Glass, P(-0.05f, 0.5f, -0.05f), P(-0.05f, 0.58f, -0.05f), 0.345f, 0.345f, 18);
            // Fan housing on top
            k.Cylinder(Tile.PaintedBlue, P(-0.05f, 1.5f, -0.05f), P(-0.05f, 1.65f, -0.05f), 0.36f, 0.36f, 18, capA: true, capB: false);
            k.Moving("kind=spin axis=0,1,0 speed=500", P(-0.05f, 1.62f, -0.05f)).Cylinder(Tile.Fan, P(-0.05f, 1.6f, -0.05f), P(-0.05f, 1.62f, -0.05f), 0.32f, 0.32f, 18);
            k.Cylinder(Tile.Grille, P(-0.05f, 1.65f, -0.05f), P(-0.05f, 1.67f, -0.05f), 0.36f, 0.36f, 18);
            // Ice tray at the front, fed by a drain pipe
            k.Pipe(Tile.Pipe, 0.03f, P(-0.05f, 0.55f, 0.28f), P(-0.05f, 0.55f, 0.4f), P(-0.05f, 0.52f, 0.4f));
            k.Box(Tile.DarkSteel, P(0f, 0.53f, 0.43f), new Vector3(0.5f, 0.06f, 0.22f));
            for (int i = 0; i < 4; i++)
                k.Box(Tile.Glass, P(-0.15f + i * 0.1f, 0.59f, 0.42f + (i % 2) * 0.04f), new Vector3(0.07f, 0.06f, 0.07f), Quaternion.Euler(0f, i * 25f, 10f));
            Console(k, P(0.42f, 0.55f, 0.45f), 0.24f);
        }

        // ---- Stellar reactor: containment vessel with a glowing core window, control rods,
        //      a cooling tower and its pipes, warning ring

        private static void Reactor(ModelKit k, float w, float d)
        {
            Skid(k, w, d, 0.14f);
            k.Cylinder(Tile.Concrete, P(-0.2f, 0.12f, 0.02f), P(-0.2f, 0.3f, 0.02f), 0.58f, 0.55f, 20);
            k.Cylinder(Tile.PaintedLight, P(-0.2f, 0.3f, 0.02f), P(-0.2f, 1.25f, 0.02f), 0.48f, 0.48f, 20);
            k.Cylinder(Tile.PaintedLight, P(-0.2f, 1.25f, 0.02f), P(-0.2f, 1.5f, 0.02f), 0.48f, 0.2f, 20);
            k.Cylinder(Tile.Hazard, P(-0.2f, 0.78f, 0.02f), P(-0.2f, 0.86f, 0.02f), 0.49f, 0.49f, 20);
            for (int i = 0; i < 3; i++)
                k.Cylinder(Tile.Rivets, P(-0.2f, 0.45f + i * 0.3f, 0.02f), P(-0.2f, 0.48f + i * 0.3f, 0.02f), 0.5f, 0.5f, 20);
            // Core window
            k.Frame(Tile.DarkSteel, P(-0.2f, 0.62f, 0.49f), new Vector2(0.34f, 0.28f), 0.05f, Vector3.forward, 0.06f);
            k.Moving("kind=glow freq=0.8", P(-0.2f, 0.62f, 0.48f)).Box(Tile.StellarDust, P(-0.2f, 0.62f, 0.485f), new Vector3(0.26f, 0.2f, 0.02f));
            // Control rods going up and down through the head
            k.Cylinder(Tile.DarkSteel, P(-0.2f, 1.5f, 0.02f), P(-0.2f, 1.56f, 0.02f), 0.22f, 0.22f, 16);
            for (int i = 0; i < 4; i++)
            {
                float a = i * Mathf.PI * 0.5f;
                Vector3 at = P(-0.2f + Mathf.Cos(a) * 0.12f, 1.56f, 0.02f + Mathf.Sin(a) * 0.12f);
                k.Moving($"kind=bob axis=0,1,0 amp=0.06 freq=0.25 phase={i * 0.25f:0.00}", at).Tube(Tile.Steel, at, at + Vector3.up * 0.2f, 0.03f, 8);
            }
            // Cooling tower on the right, steam glowing at its mouth
            k.Cylinder(Tile.Concrete, P(0.52f, 0.12f, -0.25f), P(0.52f, 0.95f, -0.25f), 0.26f, 0.17f, 16);
            k.Cylinder(Tile.Concrete, P(0.52f, 0.95f, -0.25f), P(0.52f, 1.4f, -0.25f), 0.17f, 0.22f, 16, capA: false, capB: false);
            k.Moving("kind=glow freq=0.4", P(0.52f, 1.38f, -0.25f)).Cylinder(Tile.Glass, P(0.52f, 1.3f, -0.25f), P(0.52f, 1.32f, -0.25f), 0.19f, 0.19f, 16);
            k.Pipe(Tile.Pipe, 0.05f, P(0.25f, 1.0f, 0.02f), P(0.52f, 1.0f, 0.02f), P(0.52f, 1.0f, -0.08f));
            k.Pipe(Tile.Pipe, 0.05f, P(0.25f, 0.45f, -0.15f), P(0.36f, 0.45f, -0.25f));
            Console(k, P(0.45f, 0.5f, 0.45f), 0.3f);
            k.Box(Tile.LabelWarning, P(-0.6f, 0.95f, 0.3f), new Vector3(0.02f, 0.16f, 0.16f));
        }

        // ---- Quantum encoder: a glowing core inside three counter-rotating rings, server racks around

        private static void Encoder(ModelKit k, float w, float d)
        {
            Skid(k, w, d);
            k.Cylinder(Tile.DarkSteel, P(0f, 0.12f, 0f), P(0f, 0.4f, 0f), 0.5f, 0.45f, 18);
            k.Cylinder(Tile.Brushed, P(0f, 0.4f, 0f), P(0f, 0.55f, 0f), 0.2f, 0.12f, 14);
            Vector3 core = P(0f, 1.0f, 0f);
            k.Moving("kind=glow freq=1.2", core).Cylinder(Tile.StellarDust, P(0f, 0.6f, 0f), P(0f, 1.4f, 0f), 0.1f, 0.1f, 12);
            (float r, float speed, Vector3 axis)[] rings = { (0.32f, 90f, Vector3.up), (0.26f, -140f, Vector3.right), (0.2f, 200f, Vector3.forward) };
            foreach ((float r, float speed, Vector3 axis) in rings)
            {
                ModelKit ring = k.Moving($"kind=spin axis=0,1,0 speed={speed:0}", core);
                Ring(ring, Tile.Gold, core, axis, r, 0.025f, 20);
            }
            k.Cylinder(Tile.DarkSteel, P(0f, 1.4f, 0f), P(0f, 1.55f, 0f), 0.16f, 0.2f, 14);
            k.Tube(Tile.DarkSteel, P(0f, 1.55f, 0f), P(0f, 1.75f, 0f), 0.04f, 8);
            // Server racks left and right, cables to the core base
            foreach (float x in new[] { -0.55f, 0.55f })
            {
                k.Box(Tile.PaintedDark, P(x, 0.75f, -0.1f), new Vector3(0.24f, 1.25f, 0.7f));
                k.Box(Tile.Grille, P(x, 0.85f, 0.255f), new Vector3(0.2f, 0.9f, 0.01f));
                for (int i = 0; i < 4; i++)
                    k.Box(i % 2 == 0 ? Tile.LedGreen : Tile.LedRed, P(x - 0.06f + i * 0.04f, 0.32f, 0.255f), new Vector3(0.02f, 0.02f, 0.01f));
                k.Pipe(Tile.CableJacket, 0.025f, P(x * 0.8f, 0.3f, 0.1f), P(x * 0.5f, 0.3f, 0.1f));
            }
            Console(k, P(0f, 0.3f, 0.5f), 0.36f);
        }

        // ---- Converter: two hoppers feeding a glowing sphere with a spinning ring, output chute

        private static void Converter(ModelKit k, float w, float d)
        {
            Skid(k, w, d);
            k.Box(Tile.PaintedLight, P(0f, 0.3f, -0.05f), new Vector3(1.2f, 0.4f, 0.9f));
            Vector3 center = P(0f, 0.95f, -0.05f);
            Ball(k, Tile.DarkSteel, center, 0.32f);
            k.Moving("kind=glow freq=1.5", center).Cylinder(Tile.StellarDust, center - Vector3.up * 0.05f, center + Vector3.up * 0.05f, 0.335f, 0.335f, 16);
            ModelKit spin = k.Moving("kind=spin axis=0,1,0 speed=160", center);
            Ring(spin, Tile.PaintedOrange, center, Vector3.right, 0.42f, 0.03f, 20);
            k.Cylinder(Tile.DarkSteel, P(0f, 0.5f, -0.05f), P(0f, 0.68f, -0.05f), 0.18f, 0.12f, 12);
            foreach (float x in new[] { -0.45f, 0.45f })
            {
                k.Cylinder(Tile.Rust, P(x, 0.9f, -0.15f), P(x, 1.25f, -0.15f), 0.08f, 0.18f, 4, capA: true, capB: false);
                k.Box(Tile.DarkSteel, P(x, 0.7f, -0.15f), new Vector3(0.1f, 0.4f, 0.1f));
                k.Beam(Tile.Pipe, P(x * 0.9f, 0.9f, -0.15f), P(x * 0.45f, 0.95f, -0.05f), 0.05f);
            }
            k.Box(Tile.Tread, P(0f, 0.55f, 0.42f), new Vector3(0.3f, 0.04f, 0.2f), Quaternion.Euler(-15f, 0f, 0f));
            Console(k, P(0.45f, 0.62f, 0.42f), 0.26f);
        }

        // ---- Recycler: a shredder under a wide hopper, a feed ramp, a voucher printer on the side

        private static void Recycler(ModelKit k, float w, float d)
        {
            Skid(k, w, d);
            k.Box(Tile.PaintedOrange, P(-0.1f, 0.5f, -0.05f), new Vector3(0.9f, 0.8f, 0.85f));
            k.Box(Tile.DarkSteel, P(-0.1f, 0.92f, -0.05f), new Vector3(0.8f, 0.06f, 0.75f));
            foreach ((float z, float speed) in new[] { (-0.18f, 220f), (0.08f, -220f) })
            {
                ModelKit shaft = k.Moving($"kind=spin axis=1,0,0 speed={speed:0}", P(-0.1f, 0.98f, z));
                shaft.Tube(Tile.DarkSteel, P(-0.45f, 0.98f, z), P(0.25f, 0.98f, z), 0.08f, 10);
                for (int i = 0; i < 6; i++)
                    shaft.Box(Tile.Steel, P(-0.4f + i * 0.12f, 0.98f, z), new Vector3(0.03f, 0.22f, 0.06f), Quaternion.Euler(i * 35f, 0f, 0f));
            }
            k.Cylinder(Tile.Rust, P(-0.1f, 1.0f, -0.05f), P(-0.1f, 1.35f, -0.05f), 0.5f, 0.7f, 4, capA: false, capB: false);
            k.Cylinder(Tile.Hazard, P(-0.1f, 1.33f, -0.05f), P(-0.1f, 1.38f, -0.05f), 0.715f, 0.715f, 4, capA: false, capB: false);
            // Voucher printer on the right: screen and glowing slot
            k.Box(Tile.PaintedLight, P(0.52f, 0.55f, 0.1f), new Vector3(0.28f, 0.9f, 0.5f));
            k.Moving("kind=glow freq=0.6", P(0.52f, 0.85f, 0.36f)).Box(Tile.ScreenAmber, P(0.52f, 0.85f, 0.355f), new Vector3(0.2f, 0.16f, 0.01f));
            k.Box(Tile.DarkSteel, P(0.52f, 0.55f, 0.36f), new Vector3(0.16f, 0.03f, 0.03f));
            k.Box(Tile.LedGreen, P(0.52f, 0.51f, 0.355f), new Vector3(0.12f, 0.015f, 0.01f));
            StatusLight(k, P(0.52f, 1.05f, 0.36f), new Vector3(0.05f, 0.05f, 0.02f));
            k.Box(Tile.LabelWarning, P(-0.1f, 0.6f, 0.38f), new Vector3(0.18f, 0.16f, 0.02f));
        }

        // ---- Exchange counter: a counter with a slanted screen under a canopy, a card slot, shelves

        private static void Exchange(ModelKit k, float w, float d)
        {
            Skid(k, w, d);
            k.Box(Tile.PaintedBlue, P(0f, 0.5f, 0.1f), new Vector3(1.1f, 0.8f, 0.5f));
            k.Box(Tile.Brushed, P(0f, 0.92f, 0.12f), new Vector3(1.16f, 0.04f, 0.58f));
            k.Moving("kind=glow freq=0.5", P(-0.15f, 1.08f, 0.2f)).Box(Tile.Screen, P(-0.15f, 1.08f, 0.2f), new Vector3(0.5f, 0.3f, 0.03f), Quaternion.Euler(-20f, 0f, 0f));
            k.Box(Tile.DarkSteel, P(-0.15f, 0.96f, 0.15f), new Vector3(0.1f, 0.06f, 0.08f));
            k.Box(Tile.ControlPanel, P(0.35f, 0.95f, 0.25f), new Vector3(0.24f, 0.02f, 0.16f));
            k.Box(Tile.LedGreen, P(0.35f, 0.75f, 0.355f), new Vector3(0.12f, 0.02f, 0.01f));
            // Canopy on two posts with the sign
            foreach (float x in new[] { -0.6f, 0.6f })
                k.Box(Tile.DarkSteel, P(x, 0.9f, -0.35f), new Vector3(0.06f, 1.6f, 0.06f));
            k.Box(Tile.PaintedOrange, P(0f, 1.72f, -0.05f), new Vector3(1.32f, 0.06f, 0.75f), Quaternion.Euler(8f, 0f, 0f));
            k.Box(Tile.DarkSteel, P(0f, 1.62f, -0.35f), new Vector3(1.26f, 0.06f, 0.06f));
            k.Box(Tile.ArrowLabel, P(0f, 1.48f, -0.35f), new Vector3(0.4f, 0.2f, 0.02f));
            k.Box(Tile.DarkSteel, P(0f, 1.6f, -0.35f), new Vector3(0.02f, 0.04f, 0.02f));
            // Shelves behind the counter
            for (int i = 0; i < 3; i++)
                k.Box(Tile.DarkSteel, P(0f, 0.45f + i * 0.3f, -0.3f), new Vector3(1.26f, 0.03f, 0.18f));
            k.Box(Tile.IngotIron, P(-0.3f, 0.5f, -0.3f), new Vector3(0.18f, 0.07f, 0.12f));
            k.Box(Tile.IngotCopper, P(0.1f, 0.8f, -0.3f), new Vector3(0.18f, 0.07f, 0.12f));
            k.Box(Tile.StellarDust, P(0.35f, 1.1f, -0.3f), new Vector3(0.12f, 0.1f, 0.12f));
        }

        // ---- Radar tower: a tapering lattice mast, platform, turning dish, beacon on top

        private static void Radar(ModelKit k, float w, float d)
        {
            Skid(k, w, d);
            k.Box(Tile.PaintedDark, P(0.3f, 0.35f, 0.25f), new Vector3(0.4f, 0.5f, 0.4f));
            k.Box(Tile.Grille, P(0.3f, 0.4f, 0.455f), new Vector3(0.3f, 0.3f, 0.01f));
            float top = 1.9f;
            Vector3[] feet = { P(-0.45f, 0.1f, -0.45f), P(0.45f, 0.1f, -0.45f), P(0.45f, 0.1f, 0.45f), P(-0.45f, 0.1f, 0.45f) };
            Vector3[] heads = { P(-0.15f, top, -0.15f), P(0.15f, top, -0.15f), P(0.15f, top, 0.15f), P(-0.15f, top, 0.15f) };
            for (int i = 0; i < 4; i++)
            {
                k.Beam(Tile.PaintedYellow, feet[i], heads[i], 0.06f);
                Vector3 a = Vector3.Lerp(feet[i], heads[i], 0.35f), b = Vector3.Lerp(feet[(i + 1) % 4], heads[(i + 1) % 4], 0.65f);
                k.Beam(Tile.DarkSteel, a, b, 0.03f);
                Vector3 c = Vector3.Lerp(feet[(i + 1) % 4], heads[(i + 1) % 4], 0.35f), e = Vector3.Lerp(feet[i], heads[i], 0.65f);
                k.Beam(Tile.DarkSteel, c, e, 0.03f);
            }
            k.Box(Tile.Tread, P(0f, top + 0.03f, 0f), new Vector3(0.5f, 0.06f, 0.5f));
            foreach (float x in new[] { -0.12f, 0.12f })
                k.Beam(Tile.PaintedYellow, P(x, 0.1f, 0.52f), P(x, top + 0.05f, 0.27f), 0.03f);
            for (int i = 1; i < 9; i++)
            {
                float t = i / 9f;
                Vector3 c = Vector3.Lerp(P(0f, 0.1f, 0.52f), P(0f, top + 0.05f, 0.27f), t);
                k.Tube(Tile.DarkSteel, c + Vector3.left * 0.12f, c + Vector3.right * 0.12f, 0.012f, 6);
            }
            // Dish turning around the mast
            Vector3 hub = P(0f, top + 0.2f, 0f);
            k.Cylinder(Tile.DarkSteel, P(0f, top + 0.06f, 0f), hub, 0.08f, 0.08f, 12);
            ModelKit dish = k.Moving("kind=spin axis=0,1,0 speed=60", hub);
            Vector3 dishFace = hub + new Vector3(0f, 0.25f, 0.12f);
            dish.Beam(Tile.DarkSteel, hub, dishFace, 0.05f);
            dish.Cylinder(Tile.Aluminum, dishFace + new Vector3(0f, -0.05f, -0.08f), dishFace + new Vector3(0f, 0.05f, 0.08f), 0.06f, 0.42f, 18, capA: true, capB: false);
            dish.Beam(Tile.Steel, dishFace + new Vector3(0f, 0.05f, 0.08f), dishFace + new Vector3(0f, 0.2f, 0.32f), 0.02f);
            dish.Beam(Tile.DarkSteel, hub, hub + new Vector3(0f, 0.05f, -0.2f), 0.04f);
            dish.Box(Tile.DarkSteel, hub + new Vector3(0f, 0.05f, -0.25f), new Vector3(0.14f, 0.14f, 0.14f));
            k.Tube(Tile.DarkSteel, P(0.2f, top + 0.06f, 0.2f), P(0.2f, top + 0.5f, 0.2f), 0.015f, 6);
            k.Moving("kind=glow freq=1.0", P(0.2f, top + 0.52f, 0.2f)).Cylinder(Tile.LedRed, P(0.2f, top + 0.5f, 0.2f), P(0.2f, top + 0.56f, 0.2f), 0.03f, 0.03f, 8);
        }
    }
}
