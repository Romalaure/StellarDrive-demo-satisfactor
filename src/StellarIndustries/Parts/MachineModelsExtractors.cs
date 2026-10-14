using StellarIndustries.Production;
using UnityEngine;

namespace StellarIndustries.Parts
{
    /// <summary>The 0.17 extractors: the Mk2 rig and the deposit drill (their drill turns and feeds while they work).</summary>
    internal static partial class MachineModels
    {
        // Mk2: an orange A-frame mast with a crown sheave, a blue rotary head on a thick drill string, a cyclone and an ore hopper
        private static void ExtractorMk2(ModelKit k, float w, float d)
        {
            Skid(k, w, d, 0.14f);
            float x0 = -0.15f;
            k.Cylinder(Tile.DarkSteel, P(x0, 0.12f, 0f), P(x0, 0.26f, 0f), 0.28f, 0.22f, 16);
            k.Cylinder(Tile.Hazard, P(x0, 0.26f, 0f), P(x0, 0.3f, 0f), 0.23f, 0.23f, 16);
            // A-frame: two legs a side meeting under the crown
            foreach (float z in new[] { -0.38f, 0.38f })
            {
                k.Beam(Tile.PaintedOrange, P(x0 - 0.45f, 0.14f, z), P(x0, 2.25f, z * 0.3f), 0.08f);
                k.Beam(Tile.PaintedOrange, P(x0 + 0.45f, 0.14f, z), P(x0, 2.25f, z * 0.3f), 0.08f);
                k.Beam(Tile.DarkSteel, P(x0 - 0.3f, 0.8f, z * 0.8f), P(x0 + 0.3f, 0.8f, z * 0.8f), 0.04f);
                k.Beam(Tile.DarkSteel, P(x0 - 0.18f, 1.5f, z * 0.55f), P(x0 + 0.18f, 1.5f, z * 0.55f), 0.04f);
            }
            k.Box(Tile.PaintedOrange, P(x0, 2.28f, 0f), new Vector3(0.36f, 0.1f, 0.4f));
            Vector3 sheave = P(x0, 2.38f, 0f);
            k.Moving("kind=spin axis=0,0,1 speed=200", sheave).Cylinder(Tile.Steel, sheave + new Vector3(0f, 0f, -0.05f), sheave + new Vector3(0f, 0f, 0.05f), 0.12f, 0.12f, 14);
            k.Box(Tile.LedRed, P(x0, 2.52f, 0f), new Vector3(0.05f, 0.05f, 0.05f));
            // Rotary head going up and down, the string and its double bit turning under it
            ModelKit feed = k.Moving("kind=bob axis=0,1,0 amp=0.18 freq=0.3", P(x0, 1.7f, 0f));
            feed.Box(Tile.PaintedBlue, P(x0, 1.75f, 0f), new Vector3(0.36f, 0.34f, 0.34f));
            feed.Box(Tile.Grille, P(x0, 1.75f, 0.175f), new Vector3(0.26f, 0.22f, 0.01f));
            feed.Cylinder(Tile.DarkSteel, P(x0 + 0.18f, 1.75f, 0f), P(x0 + 0.3f, 1.75f, 0f), 0.09f, 0.09f, 12);
            ModelKit drill = feed.Moving("kind=spin axis=0,1,0 speed=520", P(x0, 1f, 0f));
            drill.Tube(Tile.Steel, P(x0, 1.58f, 0f), P(x0, 0.35f, 0f), 0.07f, 12);
            for (int i = 0; i < 7; i++)
                drill.Box(Tile.DarkSteel, P(x0, 0.45f + i * 0.16f, 0f), new Vector3(0.2f, 0.025f, 0.05f), Quaternion.Euler(0f, i * 26f, 18f));
            drill.Cylinder(Tile.Gold, P(x0 - 0.06f, 0.36f, 0f), P(x0 - 0.06f, 0.16f, 0f), 0.1f, 0.02f, 8);
            drill.Cylinder(Tile.Gold, P(x0 + 0.06f, 0.36f, 0f), P(x0 + 0.06f, 0.16f, 0f), 0.1f, 0.02f, 8);
            // Cyclone separator on the right, piped from the well, over the ore hopper
            k.Pipe(Tile.Pipe, 0.045f, P(x0 + 0.18f, 0.28f, -0.2f), P(0.45f, 0.28f, -0.2f), P(0.45f, 1.2f, -0.2f));
            k.Cylinder(Tile.Brushed, P(0.48f, 1.25f, 0.05f), P(0.48f, 1.55f, 0.05f), 0.16f, 0.16f, 14);
            k.Cylinder(Tile.Brushed, P(0.48f, 1.25f, 0.05f), P(0.48f, 0.95f, 0.05f), 0.16f, 0.05f, 14);
            k.Box(Tile.PaintedDark, P(0.48f, 0.5f, 0.05f), new Vector3(0.38f, 0.7f, 0.5f));
            k.Box(Tile.DarkSteel, P(0.48f, 0.9f, 0.05f), new Vector3(0.06f, 0.1f, 0.06f));
            k.Moving("kind=glow freq=0.5", P(0.48f, 0.55f, 0.31f)).Box(Tile.ScreenAmber, P(0.48f, 0.55f, 0.305f), new Vector3(0.26f, 0.22f, 0.01f));
            StatusLight(k, P(0.48f, 0.84f, 0.31f), new Vector3(0.05f, 0.05f, 0.02f));
            Motor(k, P(-0.6f, 0.3f, -0.42f), P(-0.6f, 0.3f, -0.14f), 0.11f);
        }

        // Deposit drill: a lattice derrick over a turning rotary table with three drill rods, a red top drive,
        // a power cabinet with its cooling fan, and a chute down to the ore bin
        private static void DeepDrill(ModelKit k, float w, float d)
        {
            Skid(k, w, d, 0.16f);
            float x0 = -0.2f, c = 0.34f;
            k.Box(Tile.DarkSteel, P(x0, 0.22f, 0f), new Vector3(0.9f, 0.12f, 0.9f));
            k.Box(Tile.Hazard, P(x0, 0.29f, 0.46f), new Vector3(0.9f, 0.03f, 0.02f));
            // Rotary table and its three rods
            Vector3 table = P(x0, 0.3f, 0f);
            ModelKit rotary = k.Moving("kind=spin axis=0,1,0 speed=160", table);
            rotary.Cylinder(Tile.Steel, P(x0, 0.28f, 0f), P(x0, 0.36f, 0f), 0.34f, 0.34f, 20);
            rotary.Bolts(P(x0, 0.36f, 0f), Vector3.up, 0.27f, 10, 0.035f);
            for (int i = 0; i < 3; i++)
            {
                float a = i * Mathf.PI * 2f / 3f;
                Vector3 at = P(x0 + Mathf.Cos(a) * 0.17f, 0f, Mathf.Sin(a) * 0.17f);
                rotary.Tube(Tile.Brushed, at + new Vector3(0f, 0.36f, 0f), at + new Vector3(0f, 1.6f, 0f), 0.04f, 10);
            }
            // Derrick: four corner posts leaning in, braced
            foreach (float x in new[] { -c, c })
            {
                foreach (float z in new[] { -c, c })
                    k.Beam(Tile.Steel, P(x0 + x, 0.28f, z), P(x0 + x * 0.45f, 2.5f, z * 0.45f), 0.06f);
            }
            for (float y = 0.7f; y < 2.4f; y += 0.45f)
            {
                float s = Mathf.Lerp(1f, 0.45f, (y - 0.28f) / 2.22f) * c;
                k.Box(Tile.Hazard, P(x0, y, s), new Vector3(s * 2f + 0.04f, 0.035f, 0.035f));
                k.Box(Tile.Hazard, P(x0, y, -s), new Vector3(s * 2f + 0.04f, 0.035f, 0.035f));
                k.Box(Tile.DarkSteel, P(x0 + s, y, 0f), new Vector3(0.035f, 0.035f, s * 2f + 0.04f));
                k.Box(Tile.DarkSteel, P(x0 - s, y, 0f), new Vector3(0.035f, 0.035f, s * 2f + 0.04f));
            }
            k.Box(Tile.DarkSteel, P(x0, 2.52f, 0f), new Vector3(0.4f, 0.06f, 0.4f));
            k.Box(Tile.LedRed, P(x0, 2.6f, 0f), new Vector3(0.06f, 0.06f, 0.06f));
            // Top drive, red, riding up and down the derrick
            ModelKit top = k.Moving("kind=bob axis=0,1,0 amp=0.22 freq=0.2", P(x0, 1.9f, 0f));
            top.Box(Tile.PaintedOrange, P(x0, 1.95f, 0f), new Vector3(0.38f, 0.3f, 0.38f));
            top.Box(Tile.Grille, P(x0, 1.95f, 0.195f), new Vector3(0.28f, 0.2f, 0.01f));
            top.Cylinder(Tile.DarkSteel, P(x0, 2.1f, 0f), P(x0, 2.16f, 0f), 0.12f, 0.08f, 12);
            // Power cabinet and cooling fan on the right
            k.Box(Tile.PaintedDark, P(0.55f, 0.65f, -0.15f), new Vector3(0.38f, 0.95f, 0.55f));
            k.Box(Tile.Screen, P(0.55f, 0.85f, 0.13f), new Vector3(0.26f, 0.2f, 0.01f));
            k.Box(Tile.Gauge, P(0.48f, 0.55f, 0.13f), new Vector3(0.1f, 0.1f, 0.01f));
            k.Box(Tile.Gauge, P(0.62f, 0.55f, 0.13f), new Vector3(0.1f, 0.1f, 0.01f));
            Vector3 fan = P(0.745f, 0.8f, -0.2f);
            k.Moving("kind=spin axis=1,0,0 speed=600", fan).Box(Tile.Fan, fan, new Vector3(0.01f, 0.3f, 0.3f));
            k.Frame(Tile.DarkSteel, P(0.745f, 0.8f, -0.2f), new Vector2(0.34f, 0.34f), 0.03f, Vector3.right, 0.02f);
            StatusLight(k, P(0.55f, 1.06f, 0.13f), new Vector3(0.05f, 0.05f, 0.02f));
            // Chute from the table to the ore bin at the front
            k.Beam(Tile.Brushed, P(x0 + 0.2f, 0.4f, 0.25f), P(x0 + 0.5f, 0.3f, 0.5f), 0.14f, 0.05f);
            k.Box(Tile.Rust, P(x0 + 0.6f, 0.3f, 0.48f), new Vector3(0.3f, 0.2f, 0.22f));
            k.Pipe(Tile.CableJacket, 0.03f, P(0.4f, 1.0f, -0.3f), P(x0 + 0.3f, 1.0f, -0.3f), P(x0 + 0.3f, 1.9f, -0.15f));
        }
    }
}
