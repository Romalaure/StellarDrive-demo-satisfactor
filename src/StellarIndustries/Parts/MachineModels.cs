using StellarIndustries.Production;
using UnityEngine;

namespace StellarIndustries.Parts
{
    /// <summary>
    /// The machines' 3D models, built by the mod from boxes and cylinders on its own texture atlas
    /// (<see cref="IndustrialSkin"/>), lit by the game's part shader. Every model stands on the
    /// part's bottom face, front towards +z, and shares a logical layout:
    ///  - a skid with a tread-plate deck and a hazard band at the front;
    ///  - the front is the operator's side: control panel, screen, gauges, status light;
    ///  - the back is the service side: the power socket comes out of a yellow-and-black junction
    ///    box, 0.2 m right of the centre, with its conduit running into the machine; the item port
    ///    an item line plugs into (Item Lines puts it on the same face, at the same height, in the
    ///    middle) sits in a framed hatch at the centre.
    /// Moving pieces (flywheels, rams, drums, drill, pistons, fans, arm, rollers) and glowing
    /// parts are animated by <see cref="MachineVisuals"/> while the machine works.
    /// </summary>
    internal static class MachineModels
    {
        /// <summary>Height of the back service row (power socket and item port), above the bottom.</summary>
        public const float ServiceHeight = 0.4f;
        /// <summary>Power socket: this far right of the centre (the item port then lands in the middle).</summary>
        public const float SocketOffset = 0.2f;

        private static Vector3 _o;

        private static Vector3 P(float x, float y, float z) => _o + new Vector3(x, y, z);

        public static void Build(MachineModel model, Transform visuals, Vector3 o, MachineDef def)
        {
            _o = o;
            var kit = new ModelKit("SI_Model", Vector3.zero);
            float w = def.Width, d = def.Depth;
            switch (model)
            {
                case MachineModel.Smelter: Smelter(kit, w, d); break;
                case MachineModel.Press: Press(kit, w, d); break;
                case MachineModel.Crusher: Crusher(kit, w, d); break;
                case MachineModel.Assembler: Assembler(kit, w, d); break;
                case MachineModel.Extractor: Extractor(kit, w, d); break;
                case MachineModel.Generator: Generator(kit, w, d); break;
                case MachineModel.Container: Container(kit, w, d, def.Height); break;
                case MachineModel.Dock: Dock(kit, w, d); break;
                case MachineModel.Fabricator: Fabricator(kit, w, d); break;
                default: Workshop(kit, w, d); break;
            }
            if (!def.IsLogistics)
                ServicePanel(kit, w, d);
            kit.Build(visuals, visuals.gameObject.layer, IndustrialSkin.Material, GameSkin.Ready ? GameSkin.Palette : (Vector2?)null, Vector3.zero);
        }

        // ---- Shared pieces

        /// <summary>Skid: two rails, cross members, a tread deck, a hazard band along the front, feet.</summary>
        private static void Skid(ModelKit k, float w, float d, float deck = 0.1f)
        {
            float rz = d * 0.5f - 0.07f;
            k.Box(Tile.DarkSteel, P(0f, 0.035f, rz), new Vector3(w, 0.07f, 0.1f));
            k.Box(Tile.DarkSteel, P(0f, 0.035f, -rz), new Vector3(w, 0.07f, 0.1f));
            for (int i = -1; i <= 1; i++)
                k.Box(Tile.DarkSteel, P(i * (w * 0.5f - 0.1f), 0.035f, 0f), new Vector3(0.08f, 0.06f, d - 0.12f));
            k.Box(Tile.Tread, P(0f, 0.07f + deck * 0.5f - 0.02f, 0f), new Vector3(w - 0.02f, deck - 0.04f, d - 0.02f));
            k.Box(Tile.Hazard, P(0f, 0.07f + deck * 0.5f - 0.02f, d * 0.5f - 0.005f), new Vector3(w - 0.04f, deck - 0.05f, 0.02f));
            foreach (float x in new[] { -w * 0.5f + 0.08f, w * 0.5f - 0.08f })
            {
                foreach (float z in new[] { -rz, rz })
                    k.Cylinder(Tile.Rubber, P(x, 0f, z), P(x, 0.02f, z), 0.06f, 0.06f, 8);
            }
        }

        /// <summary>
        /// Back service row: a dark steel band, the power junction box (yellow-and-black, a conduit
        /// up into the machine) right of the centre, the item hatch in the middle.
        /// </summary>
        private static void ServicePanel(ModelKit k, float w, float d)
        {
            float back = -d * 0.5f;
            float y = ServiceHeight;
            k.Box(Tile.DarkSteel, P(0f, y, back + 0.07f), new Vector3(w - 0.24f, 0.3f, 0.06f));
            // Power junction box: its face is the part's back face, the socket comes out of it
            float x = SocketOffset;
            k.Box(Tile.LabelPower, P(x, y, back + 0.05f), new Vector3(0.15f, 0.22f, 0.1f));
            k.Box(Tile.DarkSteel, P(x, y + 0.12f, back + 0.05f), new Vector3(0.17f, 0.03f, 0.11f));
            k.Pipe(Tile.CableJacket, 0.022f, P(x, y + 0.13f, back + 0.05f), P(x, y + 0.32f, back + 0.05f), P(x, y + 0.32f, back + 0.16f));
            k.Box(Tile.LedGreen, P(x + 0.055f, y + 0.08f, back), new Vector3(0.025f, 0.025f, 0.01f));
            // Item hatch: framed opening with an arrow plate, where an item line plugs in
            k.Frame(Tile.PaintedYellow, P(0f, y, back + 0.025f), new Vector2(0.16f, 0.24f), 0.025f, Vector3.back, 0.03f);
            k.Box(Tile.ArrowLabel, P(0f, y + 0.07f, back + 0.04f), new Vector3(0.1f, 0.08f, 0.02f));
            k.Box(Tile.Grille, P(0f, y - 0.03f, back + 0.04f), new Vector3(0.11f, 0.1f, 0.02f));
            // Second hatch left of the power socket (Item Lines' second port is moved there, see PortLayout)
            k.Frame(Tile.PaintedYellow, P(-x, y, back + 0.025f), new Vector2(0.16f, 0.24f), 0.025f, Vector3.back, 0.03f);
            k.Box(Tile.ArrowLabel, P(-x, y + 0.07f, back + 0.04f), new Vector3(0.1f, 0.08f, 0.02f));
            k.Box(Tile.Grille, P(-x, y - 0.03f, back + 0.04f), new Vector3(0.11f, 0.1f, 0.02f));
            // Warning plate above the row
            k.Box(Tile.LabelWarning, P(-x - 0.2f, y + 0.08f, back + 0.04f), new Vector3(0.12f, 0.1f, 0.02f));
        }

        /// <summary>A status light: green while working, red when stopped.</summary>
        private static void StatusLight(ModelKit k, Vector3 at, Vector3 size)
        {
            k.Box(Tile.DarkSteel, at, size + new Vector3(0.02f, 0.02f, 0f));
            k.Status(true).Box(Tile.LedGreen, at + new Vector3(0f, 0f, size.z * 0.5f + 0.004f), new Vector3(size.x, size.y, 0.008f));
            k.Status(false).Box(Tile.LedRed, at + new Vector3(0f, 0f, size.z * 0.5f + 0.004f), new Vector3(size.x, size.y, 0.008f));
        }

        /// <summary>Operator console on the front: slanted panel with buttons, a screen and a status light.</summary>
        private static void Console(ModelKit k, Vector3 at, float width, Tile screen = Tile.Screen)
        {
            k.Box(Tile.PaintedDark, at, new Vector3(width, 0.36f, 0.12f));
            k.Box(Tile.ControlPanel, at + new Vector3(-width * 0.22f, -0.06f, 0.065f), new Vector3(width * 0.45f, 0.18f, 0.02f), Quaternion.Euler(-15f, 0f, 0f));
            k.Moving("kind=glow freq=0.7", at + new Vector3(width * 0.22f, 0.04f, 0.07f))
                .Box(screen, at + new Vector3(width * 0.22f, 0.04f, 0.065f), new Vector3(width * 0.4f, 0.2f, 0.02f));
            StatusLight(k, at + new Vector3(-width * 0.38f, 0.13f, 0.065f), new Vector3(0.04f, 0.04f, 0.02f));
        }

        /// <summary>A flywheel turning around x at <paramref name="hub"/>: rim, spokes, hub, bolts.</summary>
        private static void Flywheel(ModelKit k, Vector3 hub, float radius, float thickness, float speed, Tile rim = Tile.Steel)
        {
            ModelKit wheel = k.Moving($"kind=spin axis=1,0,0 speed={speed:0}", hub);
            wheel.Cylinder(rim, hub - new Vector3(thickness * 0.5f, 0f, 0f), hub + new Vector3(thickness * 0.5f, 0f, 0f), radius, radius, 20);
            wheel.Cylinder(Tile.DarkSteel, hub - new Vector3(thickness * 0.6f, 0f, 0f), hub + new Vector3(thickness * 0.6f, 0f, 0f), radius * 0.78f, radius * 0.78f, 20);
            for (int i = 0; i < 3; i++)
                wheel.Box(Tile.Steel, hub, new Vector3(thickness * 0.8f, radius * 1.6f, radius * 0.14f), Quaternion.Euler(i * 60f, 0f, 0f));
            wheel.Cylinder(Tile.PaintedOrange, hub - new Vector3(thickness * 0.9f, 0f, 0f), hub + new Vector3(thickness * 0.9f, 0f, 0f), radius * 0.22f, radius * 0.22f, 12);
            wheel.Bolts(hub + new Vector3(thickness * 0.9f, 0f, 0f), Vector3.right, radius * 0.14f, 5, 0.025f);
        }

        /// <summary>An electric motor between a and b: body, cooling fins, fan cover, base.</summary>
        private static void Motor(ModelKit k, Vector3 a, Vector3 b, float radius)
        {
            k.Cylinder(Tile.PaintedBlue, a, b, radius, radius, 16);
            Vector3 dir = (b - a).normalized;
            float length = (b - a).magnitude;
            for (int i = 1; i < 5; i++)
            {
                Vector3 p = a + dir * (length * i / 5f);
                k.Cylinder(Tile.DarkSteel, p - dir * 0.008f, p + dir * 0.008f, radius * 1.12f, radius * 1.12f, 16);
            }
            k.Cylinder(Tile.Fan, a - dir * 0.04f, a, radius * 1.05f, radius * 1.05f, 16);
            k.Box(Tile.DarkSteel, (a + b) * 0.5f - new Vector3(0f, radius * 0.9f, 0f), new Vector3(Mathf.Abs(dir.x) * length + 0.06f, 0.05f, Mathf.Abs(dir.z) * length + radius * 1.6f));
        }

        // ---- Workshop: a bench press with flywheel, die table and top hopper

        private static void Workshop(ModelKit k, float w, float d)
        {
            Skid(k, w, d);
            k.Box(Tile.PaintedLight, P(-0.05f, 0.55f, -0.08f), new Vector3(1.0f, 0.86f, 0.82f));
            k.Box(Tile.PaintedOrange, P(-0.05f, 1.0f, -0.08f), new Vector3(1.04f, 0.06f, 0.86f));
            k.Box(Tile.Grille, P(-0.56f, 0.55f, -0.08f), new Vector3(0.02f, 0.5f, 0.5f));
            // Feed hopper on top, its funnel into the press
            k.Cylinder(Tile.Brushed, P(-0.25f, 1.03f, -0.15f), P(-0.25f, 1.38f, -0.15f), 0.16f, 0.32f, 4, capA: true, capB: false);
            k.Box(Tile.Hazard, P(-0.25f, 1.39f, -0.15f), new Vector3(0.48f, 0.03f, 0.48f), Quaternion.Euler(0f, 45f, 0f));
            // Press frame in front: column, head, die table
            k.Box(Tile.PaintedOrange, P(-0.32f, 0.85f, 0.42f), new Vector3(0.12f, 0.62f, 0.12f));
            k.Box(Tile.PaintedOrange, P(-0.08f, 1.12f, 0.42f), new Vector3(0.6f, 0.12f, 0.18f));
            k.Box(Tile.Tread, P(-0.05f, 0.6f, 0.45f), new Vector3(0.5f, 0.06f, 0.24f));
            k.Box(Tile.DarkSteel, P(-0.05f, 0.65f, 0.45f), new Vector3(0.22f, 0.04f, 0.16f));
            ModelKit ram = k.Moving("kind=press axis=0,1,0 amp=0.24 freq=0.5", P(-0.05f, 1.0f, 0.45f));
            ram.Tube(Tile.Brushed, P(-0.05f, 1.06f, 0.45f), P(-0.05f, 0.86f, 0.45f), 0.035f);
            ram.Box(Tile.DarkSteel, P(-0.05f, 0.83f, 0.45f), new Vector3(0.2f, 0.06f, 0.14f));
            // Drive: motor at the back right, belt to the flywheel on the right side
            Motor(k, P(0.6f, 0.3f, -0.4f), P(0.6f, 0.3f, -0.12f), 0.1f);
            Flywheel(k, P(0.58f, 0.72f, -0.08f), 0.28f, 0.07f, 200f);
            k.Box(Tile.Rubber, P(0.6f, 0.5f, -0.2f), new Vector3(0.03f, 0.45f, 0.05f), Quaternion.Euler(-20f, 0f, 0f));
            // Operator side
            Console(k, P(0.3f, 0.62f, 0.4f), 0.4f);
            k.Cylinder(Tile.Gauge, P(-0.45f, 0.75f, 0.33f), P(-0.45f, 0.75f, 0.35f), 0.07f, 0.07f, 16);
            // Output chute on the left
            k.Box(Tile.Tread, P(-0.62f, 0.32f, 0.25f), new Vector3(0.14f, 0.04f, 0.45f), Quaternion.Euler(0f, 0f, -20f));
            k.Box(Tile.PaintedYellow, P(-0.68f, 0.36f, 0.25f), new Vector3(0.02f, 0.08f, 0.45f));
        }

        // ---- Smelter: a riveted furnace with a glowing mouth, blower, chimney and ingot moulds

        private static void Smelter(ModelKit k, float w, float d)
        {
            Skid(k, w, d);
            k.Box(Tile.Rust, P(-0.05f, 0.6f, -0.05f), new Vector3(1.05f, 0.95f, 0.9f));
            for (int i = 0; i < 3; i++)
                k.Box(Tile.Rivets, P(-0.05f, 0.25f + i * 0.33f, -0.05f), new Vector3(1.08f, 0.06f, 0.93f));
            // Tapered hood and chimney with bands
            k.Cylinder(Tile.DarkSteel, P(-0.05f, 1.07f, -0.05f), P(-0.05f, 1.35f, -0.05f), 0.72f, 0.26f, 4);
            k.Tube(Tile.Pipe, P(0.12f, 1.3f, -0.2f), P(0.12f, 1.95f, -0.2f), 0.11f, 14);
            k.Tube(Tile.DarkSteel, P(0.12f, 1.55f, -0.2f), P(0.12f, 1.6f, -0.2f), 0.13f, 14);
            k.Cylinder(Tile.DarkSteel, P(0.12f, 1.95f, -0.2f), P(0.12f, 2.02f, -0.2f), 0.15f, 0.15f, 14);
            // Mouth: heavy frame, glowing fire behind a grate
            k.Frame(Tile.DarkSteel, P(-0.05f, 0.5f, 0.41f), new Vector2(0.6f, 0.46f), 0.07f, Vector3.forward, 0.06f);
            k.Moving("kind=glow freq=1.3", P(-0.05f, 0.5f, 0.4f)).Box(Tile.Furnace, P(-0.05f, 0.5f, 0.395f), new Vector3(0.48f, 0.34f, 0.02f));
            for (int i = -2; i <= 2; i++)
                k.Box(Tile.DarkSteel, P(-0.05f + i * 0.09f, 0.5f, 0.42f), new Vector3(0.02f, 0.34f, 0.02f));
            // Blower on the left: fan housing and duct into the furnace
            k.Cylinder(Tile.PaintedBlue, P(-0.62f, 0.38f, 0.0f), P(-0.62f, 0.38f, 0.22f), 0.17f, 0.17f, 16);
            k.Moving("kind=spin axis=0,0,1 speed=540", P(-0.62f, 0.38f, 0.23f)).Cylinder(Tile.Fan, P(-0.62f, 0.38f, 0.22f), P(-0.62f, 0.38f, 0.235f), 0.15f, 0.15f, 16);
            k.Pipe(Tile.Pipe, 0.06f, P(-0.62f, 0.38f, -0.02f), P(-0.62f, 0.38f, -0.3f), P(-0.55f, 0.38f, -0.3f));
            // Ingot moulds on the right, warm
            k.Box(Tile.DarkSteel, P(0.6f, 0.32f, 0.2f), new Vector3(0.2f, 0.14f, 0.55f));
            k.Moving("kind=glow freq=0.6", P(0.6f, 0.4f, 0.2f)).Box(Tile.Furnace, P(0.6f, 0.395f, 0.2f), new Vector3(0.14f, 0.01f, 0.48f));
            // Pouring ladle swinging over the moulds
            ModelKit ladle = k.Moving("kind=swing axis=0,0,1 amp=12 freq=0.3", P(0.48f, 0.82f, 0.2f));
            ladle.Box(Tile.DarkSteel, P(0.52f, 0.82f, 0.2f), new Vector3(0.12f, 0.03f, 0.03f));
            ladle.Cylinder(Tile.Rust, P(0.6f, 0.62f, 0.2f), P(0.6f, 0.78f, 0.2f), 0.06f, 0.09f, 10);
            // Operator side
            Console(k, P(0.42f, 0.95f, 0.38f), 0.3f, Tile.ScreenAmber);
            k.Cylinder(Tile.Gauge, P(-0.42f, 0.9f, 0.4f), P(-0.42f, 0.9f, 0.42f), 0.08f, 0.08f, 16);
        }

        // ---- Press: two orange columns, a crown with hydraulic cylinders, a heavy ram

        private static void Press(ModelKit k, float w, float d)
        {
            Skid(k, w, d, 0.16f);
            k.Box(Tile.PaintedDark, P(0f, 0.35f, 0f), new Vector3(1.0f, 0.3f, 0.8f));
            k.Box(Tile.Tread, P(0f, 0.53f, 0.05f), new Vector3(0.66f, 0.06f, 0.52f));
            k.Box(Tile.DarkSteel, P(0f, 0.58f, 0.05f), new Vector3(0.36f, 0.04f, 0.3f));
            foreach (float x in new[] { -0.52f, 0.52f })
            {
                k.Box(Tile.PaintedOrange, P(x, 1.0f, 0f), new Vector3(0.2f, 1.5f, 0.32f));
                k.Box(Tile.Hazard, P(x, 0.62f, 0.165f), new Vector3(0.2f, 0.3f, 0.01f));
                k.Bolts(P(x, 1.62f, 0.165f), Vector3.forward, 0.07f, 4, 0.03f);
            }
            k.Box(Tile.PaintedOrange, P(0f, 1.75f, 0f), new Vector3(1.3f, 0.26f, 0.44f));
            // Hydraulic cylinders on the crown
            foreach (float x in new[] { -0.22f, 0.22f })
            {
                k.Tube(Tile.Pipe, P(x, 1.85f, 0f), P(x, 1.88f, 0f), 0.11f, 14);
                k.Tube(Tile.Brushed, P(x, 1.62f, 0f), P(x, 1.86f, 0f), 0.08f, 14);
            }
            // Ram: guided plate with its rods
            ModelKit ram = k.Moving("kind=press axis=0,1,0 amp=0.42 freq=0.45", P(0f, 1.2f, 0.02f));
            foreach (float x in new[] { -0.22f, 0.22f })
                ram.Tube(Tile.Brushed, P(x, 1.62f, 0.02f), P(x, 1.15f, 0.02f), 0.04f, 10);
            ram.Box(Tile.PaintedYellow, P(0f, 1.1f, 0.02f), new Vector3(0.78f, 0.18f, 0.4f));
            ram.Box(Tile.DarkSteel, P(0f, 0.99f, 0.04f), new Vector3(0.36f, 0.05f, 0.3f));
            // Hydraulic tank and hoses at the back (left, clear of the service row's socket)
            k.Cylinder(Tile.PaintedGreen, P(-0.45f, 0.3f, -0.28f), P(-0.1f, 0.3f, -0.28f), 0.11f, 0.11f, 14);
            k.Pipe(Tile.CableJacket, 0.025f, P(-0.2f, 0.41f, -0.28f), P(-0.2f, 0.75f, -0.24f), P(-0.4f, 1.6f, -0.2f));
            k.Pipe(Tile.CableJacket, 0.025f, P(-0.3f, 0.41f, -0.28f), P(-0.3f, 0.72f, -0.26f), P(0.4f, 1.6f, -0.2f));
            Console(k, P(0.75f, 0.85f, 0.3f), 0.18f);
        }

        // ---- Crusher: a hopper over two toothed drums, gear covers, motor and output belt

        private static void Crusher(ModelKit k, float w, float d)
        {
            Skid(k, w, d);
            k.Box(Tile.PaintedYellow, P(0f, 0.45f, -0.05f), new Vector3(1.05f, 0.7f, 0.85f));
            k.Box(Tile.DarkSteel, P(0f, 0.86f, -0.05f), new Vector3(1.0f, 0.14f, 0.78f));
            // Drums seen through the open top, turning against each other
            foreach ((float z, float speed) in new[] { (-0.2f, 260f), (0.1f, -260f) })
            {
                ModelKit drum = k.Moving($"kind=spin axis=1,0,0 speed={speed:0}", P(0f, 0.95f, z));
                drum.Cylinder(Tile.DarkSteel, P(-0.42f, 0.95f, z), P(0.42f, 0.95f, z), 0.12f, 0.12f, 12);
                for (int i = 0; i < 6; i++)
                    drum.Box(Tile.Steel, P(0f, 0.95f, z), new Vector3(0.8f, 0.3f, 0.03f), Quaternion.Euler(i * 30f, 0f, 0f));
            }
            // Hopper: open funnel with a hazard rim
            k.Cylinder(Tile.Rust, P(0f, 1.0f, -0.05f), P(0f, 1.45f, -0.05f), 0.5f, 0.72f, 4, capA: false, capB: false);
            k.Box(Tile.Hazard, P(0f, 1.46f, -0.05f), new Vector3(1.02f, 0.04f, 1.02f), Quaternion.Euler(0f, 45f, 0f));
            // Gear covers on the right, turning with the drums
            foreach ((float z, float speed) in new[] { (-0.2f, 260f), (0.1f, -260f) })
            {
                ModelKit gear = k.Moving($"kind=spin axis=1,0,0 speed={speed:0}", P(0.56f, 0.95f, z));
                gear.Cylinder(Tile.PaintedOrange, P(0.53f, 0.95f, z), P(0.6f, 0.95f, z), 0.19f, 0.19f, 14);
                gear.Bolts(P(0.6f, 0.95f, z), Vector3.right, 0.1f, 6, 0.025f);
            }
            Motor(k, P(0.56f, 0.35f, -0.4f), P(0.56f, 0.35f, -0.12f), 0.1f);
            // Output belt at the front with its roller
            k.Box(Tile.DarkSteel, P(0f, 0.2f, 0.52f), new Vector3(0.5f, 0.06f, 0.2f));
            k.Moving("kind=spin axis=1,0,0 speed=180", P(0f, 0.26f, 0.52f))
                .Cylinder(Tile.Rubber, P(-0.22f, 0.26f, 0.52f), P(0.22f, 0.26f, 0.52f), 0.05f, 0.05f, 10);
            Console(k, P(0.32f, 0.55f, 0.4f), 0.32f);
            k.Box(Tile.LabelWarning, P(-0.35f, 0.6f, 0.38f), new Vector3(0.16f, 0.14f, 0.02f));
        }

        // ---- Assembler: bench with a turntable, a gantry and a two-joint robot arm, part trays

        private static void Assembler(ModelKit k, float w, float d)
        {
            Skid(k, w, d);
            k.Box(Tile.PaintedLight, P(0f, 0.42f, 0f), new Vector3(1.5f, 0.6f, 1.0f));
            k.Box(Tile.Brushed, P(0f, 0.74f, 0.05f), new Vector3(1.4f, 0.04f, 0.86f));
            k.Box(Tile.Grille, P(-0.3f, 0.42f, 0.505f), new Vector3(0.5f, 0.3f, 0.01f));
            // Turntable with the work piece
            ModelKit table = k.Moving("kind=spin axis=0,1,0 speed=40", P(-0.15f, 0.78f, 0.1f));
            table.Cylinder(Tile.DarkSteel, P(-0.15f, 0.76f, 0.1f), P(-0.15f, 0.8f, 0.1f), 0.24f, 0.24f, 18);
            table.Box(Tile.Steel, P(-0.15f, 0.86f, 0.1f), new Vector3(0.22f, 0.1f, 0.16f));
            table.Box(Tile.BoltHeads, P(-0.15f, 0.915f, 0.1f), new Vector3(0.2f, 0.01f, 0.14f));
            // Gantry
            foreach (float x in new[] { -0.7f, 0.7f })
                k.Box(Tile.PaintedOrange, P(x, 1.15f, -0.38f), new Vector3(0.1f, 0.8f, 0.1f));
            k.Box(Tile.PaintedOrange, P(0f, 1.56f, -0.38f), new Vector3(1.5f, 0.1f, 0.14f));
            k.Box(Tile.Hazard, P(0f, 1.56f, -0.305f), new Vector3(1.3f, 0.06f, 0.01f));
            // Robot arm hanging from the gantry, swinging over the turntable, its forearm nodding
            ModelKit arm = k.Moving("kind=swing axis=0,1,0 amp=35 freq=0.35", P(-0.15f, 1.45f, -0.3f));
            arm.Box(Tile.DarkSteel, P(-0.15f, 1.47f, -0.3f), new Vector3(0.2f, 0.1f, 0.2f));
            arm.Box(Tile.PaintedYellow, P(-0.15f, 1.3f, -0.18f), new Vector3(0.08f, 0.36f, 0.08f), Quaternion.Euler(35f, 0f, 0f));
            arm.Cylinder(Tile.DarkSteel, P(-0.2f, 1.17f, -0.07f), P(-0.1f, 1.17f, -0.07f), 0.05f, 0.05f, 10);
            ModelKit forearm = arm.Moving("kind=swing axis=1,0,0 amp=18 freq=0.7", P(-0.15f, 1.17f, -0.07f));
            forearm.Box(Tile.PaintedYellow, P(-0.15f, 1.03f, 0.03f), new Vector3(0.07f, 0.3f, 0.07f), Quaternion.Euler(-25f, 0f, 0f));
            forearm.Cylinder(Tile.Steel, P(-0.15f, 0.9f, 0.09f), P(-0.15f, 0.84f, 0.1f), 0.04f, 0.015f, 8);
            forearm.Box(Tile.LedGreen, P(-0.15f, 0.93f, 0.09f), new Vector3(0.03f, 0.03f, 0.03f));
            // Parts trays
            k.Box(Tile.DarkSteel, P(0.42f, 0.8f, 0.2f), new Vector3(0.36f, 0.08f, 0.3f));
            k.Box(Tile.BoltHeads, P(0.42f, 0.845f, 0.2f), new Vector3(0.3f, 0.01f, 0.24f));
            k.Box(Tile.DarkSteel, P(-0.58f, 0.8f, 0.25f), new Vector3(0.24f, 0.08f, 0.3f));
            k.Box(Tile.Steel, P(-0.58f, 0.85f, 0.25f), new Vector3(0.2f, 0.02f, 0.24f));
            Console(k, P(0.45f, 0.45f, 0.53f), 0.4f);
        }

        // ---- Fabricator: an enclosed cell with windows, two arms inside, a stack light and a cable tray

        private static void Fabricator(ModelKit k, float w, float d)
        {
            Skid(k, w, d);
            float half = w * 0.5f - 0.08f;
            k.Box(Tile.PaintedLight, P(0f, 0.32f, 0f), new Vector3(w - 0.1f, 0.4f, d - 0.12f));
            // Cell frame and windows
            foreach (float x in new[] { -half, half })
            {
                foreach (float z in new[] { -0.45f, 0.45f })
                    k.Box(Tile.PaintedBlue, P(x, 1.0f, z), new Vector3(0.07f, 1.0f, 0.07f));
            }
            k.Box(Tile.PaintedBlue, P(0f, 1.53f, 0f), new Vector3(w - 0.1f, 0.08f, 0.98f));
            k.Box(Tile.Grille, P(0f, 1.58f, 0f), new Vector3(w * 0.5f, 0.03f, 0.5f));
            k.Box(Tile.Glass, P(0f, 1.0f, 0.46f), new Vector3(w - 0.3f, 0.9f, 0.01f));
            k.Box(Tile.Glass, P(-half, 1.0f, 0f), new Vector3(0.01f, 0.9f, 0.82f));
            k.Box(Tile.PaintedDark, P(half, 1.0f, 0f), new Vector3(0.02f, 0.9f, 0.82f));
            k.Box(Tile.DarkSteel, P(0f, 0.53f, 0f), new Vector3(w - 0.3f, 0.02f, 0.82f));
            // Work piece and two arms working on it
            k.Box(Tile.Circuit, P(0f, 0.58f, 0.02f), new Vector3(0.3f, 0.06f, 0.24f));
            foreach ((float x, float phase) in new[] { (-0.42f, 0f), (0.42f, 0.5f) })
            {
                k.Cylinder(Tile.DarkSteel, P(x, 0.54f, 0f), P(x, 0.62f, 0f), 0.08f, 0.08f, 12);
                ModelKit arm = k.Moving($"kind=swing axis=0,1,0 amp=40 freq=0.5 phase={phase:0.0}", P(x, 0.62f, 0f));
                arm.Box(Tile.PaintedOrange, P(x, 0.8f, 0f), new Vector3(0.07f, 0.36f, 0.07f));
                ModelKit fore = arm.Moving($"kind=swing axis=0,0,1 amp=20 freq=1 phase={phase:0.0}", P(x, 0.97f, 0f));
                fore.Box(Tile.PaintedOrange, P(x * 0.7f, 0.93f, 0f), new Vector3(0.28f, 0.06f, 0.06f), Quaternion.Euler(0f, 0f, x < 0f ? -25f : 25f));
                fore.Cylinder(Tile.Steel, P(x * 0.45f, 0.86f, 0f), P(x * 0.45f, 0.78f, 0f), 0.03f, 0.01f, 8);
            }
            // Stack light on top, cable tray along the back
            k.Tube(Tile.DarkSteel, P(half - 0.1f, 1.57f, 0.35f), P(half - 0.1f, 1.7f, 0.35f), 0.02f, 6);
            k.Status(true).Cylinder(Tile.LedGreen, P(half - 0.1f, 1.7f, 0.35f), P(half - 0.1f, 1.78f, 0.35f), 0.04f, 0.04f, 10);
            k.Status(false).Cylinder(Tile.LedRed, P(half - 0.1f, 1.7f, 0.35f), P(half - 0.1f, 1.78f, 0.35f), 0.04f, 0.04f, 10);
            k.Box(Tile.DarkSteel, P(0f, 1.3f, -0.47f), new Vector3(w - 0.3f, 0.06f, 0.08f));
            k.Pipe(Tile.CableJacket, 0.02f, P(-0.5f, 1.3f, -0.45f), P(-0.5f, 0.6f, -0.45f));
            k.Pipe(Tile.CableJacket, 0.02f, P(0.5f, 1.3f, -0.45f), P(0.5f, 0.6f, -0.45f));
            Console(k, P(half - 0.25f, 0.35f, 0.53f), 0.36f);
            k.Moving("kind=glow freq=0.4", P(-0.3f, 0.35f, 0.53f)).Box(Tile.ScreenAmber, P(-0.3f, 0.35f, 0.535f), new Vector3(0.3f, 0.18f, 0.01f));
        }

        // ---- Extractor: a braced drilling tower, motor head, turning drill that bores, ore bin

        private static void Extractor(ModelKit k, float w, float d)
        {
            Skid(k, w, d, 0.14f);
            // Bore well through the deck
            k.Cylinder(Tile.DarkSteel, P(-0.1f, 0.12f, 0f), P(-0.1f, 0.24f, 0f), 0.24f, 0.2f, 16);
            // Tower: four legs and braces
            foreach (float x in new[] { -0.42f, 0.22f })
            {
                foreach (float z in new[] { -0.32f, 0.32f })
                    k.Box(Tile.PaintedYellow, P(x, 1.1f, z), new Vector3(0.07f, 1.85f, 0.07f));
                for (int i = 0; i < 4; i++)
                    k.Box(Tile.DarkSteel, P(x, 0.45f + i * 0.45f, 0f), new Vector3(0.04f, 0.04f, 0.64f));
                k.Box(Tile.DarkSteel, P(x, 0.9f, 0f), new Vector3(0.03f, 0.9f, 0.03f), Quaternion.Euler(35f, 0f, 0f));
            }
            for (int i = 0; i < 4; i++)
            {
                k.Box(Tile.DarkSteel, P(-0.1f, 0.45f + i * 0.45f, -0.32f), new Vector3(0.64f, 0.04f, 0.04f));
                k.Box(Tile.DarkSteel, P(-0.1f, 0.45f + i * 0.45f, 0.32f), new Vector3(0.64f, 0.04f, 0.04f));
            }
            k.Box(Tile.PaintedYellow, P(-0.1f, 2.05f, 0f), new Vector3(0.8f, 0.08f, 0.8f));
            k.Ladder(P(0.3f, 0.14f, 0.36f), 1.9f, 0.3f);
            // Drill: the head goes up and down, the string turns inside it
            ModelKit feed = k.Moving("kind=bob axis=0,1,0 amp=0.12 freq=0.25", P(-0.1f, 1.8f, 0f));
            feed.Box(Tile.PaintedBlue, P(-0.1f, 1.85f, 0f), new Vector3(0.4f, 0.26f, 0.4f));
            feed.Box(Tile.Grille, P(-0.1f, 1.85f, 0.205f), new Vector3(0.3f, 0.18f, 0.01f));
            feed.Cylinder(Tile.DarkSteel, P(-0.1f, 1.98f, 0f), P(-0.1f, 2.0f, 0f), 0.12f, 0.12f, 12);
            ModelKit drill = feed.Moving("kind=spin axis=0,1,0 speed=420", P(-0.1f, 1.0f, 0f));
            drill.Tube(Tile.Steel, P(-0.1f, 1.72f, 0f), P(-0.1f, 0.4f, 0f), 0.05f, 10);
            for (int i = 0; i < 6; i++)
                drill.Box(Tile.DarkSteel, P(-0.1f, 0.5f + i * 0.2f, 0f), new Vector3(0.15f, 0.02f, 0.04f), Quaternion.Euler(0f, i * 30f, 15f));
            drill.Cylinder(Tile.Gold, P(-0.1f, 0.4f, 0f), P(-0.1f, 0.2f, 0f), 0.11f, 0.02f, 8);
            // Ore bin on the right with its level display, the motor driving the head
            k.Box(Tile.PaintedDark, P(0.5f, 0.45f, 0.15f), new Vector3(0.34f, 0.6f, 0.5f));
            k.Box(Tile.Grille, P(0.5f, 0.76f, 0.15f), new Vector3(0.3f, 0.02f, 0.46f));
            k.Moving("kind=glow freq=0.5", P(0.5f, 0.5f, 0.41f)).Box(Tile.ScreenAmber, P(0.5f, 0.5f, 0.405f), new Vector3(0.24f, 0.22f, 0.01f));
            Motor(k, P(-0.5f, 0.3f, -0.4f), P(-0.5f, 0.3f, -0.12f), 0.1f);
            StatusLight(k, P(0.5f, 0.82f, 0.41f), new Vector3(0.05f, 0.05f, 0.02f));
        }

        // ---- Generator: firebox, engine block with four pistons, flywheel, alternator, radiator fan

        private static void Generator(ModelKit k, float w, float d)
        {
            Skid(k, w, d);
            // Firebox and fuel hopper on the left
            k.Box(Tile.Rust, P(-0.45f, 0.45f, 0f), new Vector3(0.45f, 0.62f, 0.85f));
            k.Frame(Tile.DarkSteel, P(-0.45f, 0.4f, 0.43f), new Vector2(0.3f, 0.26f), 0.05f, Vector3.forward, 0.04f);
            k.Moving("kind=glow freq=1.6", P(-0.45f, 0.4f, 0.42f)).Box(Tile.Furnace, P(-0.45f, 0.4f, 0.425f), new Vector3(0.22f, 0.18f, 0.01f));
            k.Cylinder(Tile.Brushed, P(-0.45f, 0.76f, -0.1f), P(-0.45f, 1.05f, -0.1f), 0.14f, 0.24f, 4, capA: true, capB: false);
            // Engine block with a bank of four cylinders and pistons
            k.Box(Tile.DarkSteel, P(0.12f, 0.45f, -0.05f), new Vector3(0.55f, 0.6f, 0.6f));
            for (int i = 0; i < 4; i++)
            {
                float z = -0.27f + i * 0.15f;
                k.Tube(Tile.Steel, P(0.12f, 0.75f, z), P(0.12f, 0.95f, z), 0.06f, 10);
                ModelKit piston = k.Moving($"kind=bob axis=0,1,0 amp=0.06 freq=2.2 phase={i * 0.25f:0.00}", P(0.12f, 1.0f, z));
                piston.Tube(Tile.Brushed, P(0.12f, 0.95f, z), P(0.12f, 1.05f, z), 0.045f, 10);
                piston.Box(Tile.DarkSteel, P(0.12f, 1.06f, z), new Vector3(0.1f, 0.02f, 0.1f));
            }
            // Flywheel and alternator on the right
            Flywheel(k, P(0.46f, 0.5f, -0.05f), 0.25f, 0.06f, 320f, Tile.PaintedOrange);
            k.Cylinder(Tile.Winding, P(0.52f, 0.5f, -0.05f), P(0.66f, 0.5f, -0.05f), 0.16f, 0.16f, 16);
            k.Bolts(P(0.66f, 0.5f, -0.05f), Vector3.right, 0.11f, 6, 0.025f);
            // Radiator and fan at the front, exhaust stacks at the back
            k.Box(Tile.Grille, P(0.12f, 0.5f, 0.42f), new Vector3(0.5f, 0.5f, 0.05f));
            k.Moving("kind=spin axis=0,0,1 speed=600", P(0.12f, 0.5f, 0.46f)).Cylinder(Tile.Fan, P(0.12f, 0.5f, 0.45f), P(0.12f, 0.5f, 0.46f), 0.21f, 0.21f, 18);
            foreach (float x in new[] { -0.05f, 0.29f })
            {
                k.Pipe(Tile.Pipe, 0.05f, P(x, 0.78f, -0.3f), P(x, 1.4f, -0.3f));
                k.Tube(Tile.DarkSteel, P(x, 1.38f, -0.3f), P(x, 1.45f, -0.3f), 0.065f, 10);
            }
            Console(k, P(-0.45f, 0.9f, 0.34f), 0.3f, Tile.ScreenAmber);
        }

        // ---- Container: ribbed crate, corner castings, door bars

        private static void Container(ModelKit k, float w, float d, float h)
        {
            k.Box(Tile.PaintedBlue, P(0f, 0.6f, 0f), new Vector3(1.28f, 1.06f, 1.08f));
            for (int i = 0; i < 6; i++)
            {
                float x = -0.55f + i * 0.22f;
                k.Box(Tile.PaintedBlue, P(x, 0.6f, 0.55f), new Vector3(0.05f, 0.98f, 0.03f));
                k.Box(Tile.PaintedBlue, P(x, 0.6f, -0.55f), new Vector3(0.05f, 0.98f, 0.03f));
            }
            foreach (float x in new[] { -0.62f, 0.62f })
            {
                foreach (float z in new[] { -0.52f, 0.52f })
                {
                    k.Box(Tile.DarkSteel, P(x, 0.08f, z), new Vector3(0.12f, 0.16f, 0.12f));
                    k.Box(Tile.DarkSteel, P(x, 1.12f, z), new Vector3(0.12f, 0.16f, 0.12f));
                }
            }
            k.Box(Tile.DarkSteel, P(0f, 0.08f, 0f), new Vector3(1.2f, 0.12f, 1.0f));
            k.Box(Tile.Tread, P(0f, 1.135f, 0f), new Vector3(1.16f, 0.03f, 0.96f));
            foreach (float x in new[] { -0.2f, 0.2f })
            {
                k.Tube(Tile.Brushed, P(x, 0.15f, 0.58f), P(x, 1.05f, 0.58f), 0.018f, 6);
                k.Box(Tile.DarkSteel, P(x, 0.55f, 0.6f), new Vector3(0.06f, 0.12f, 0.03f));
            }
            k.Box(Tile.LabelWarning, P(0.42f, 0.85f, 0.57f), new Vector3(0.16f, 0.14f, 0.02f));
        }

        // ---- Loading dock: deck with rollers, an arch with a beacon and a chute

        private static void Dock(ModelKit k, float w, float d)
        {
            Skid(k, w, d, 0.2f);
            for (int i = 0; i < 5; i++)
            {
                float z = -0.4f + i * 0.2f;
                k.Moving("kind=spin axis=1,0,0 speed=-240", P(0f, 0.3f, z)).Tube(Tile.Pipe, P(-0.5f, 0.3f, z), P(0.5f, 0.3f, z), 0.045f, 10);
            }
            foreach (float x in new[] { -0.56f, 0.56f })
                k.Box(Tile.PaintedYellow, P(x, 0.32f, 0f), new Vector3(0.06f, 0.1f, 1.1f));
            foreach (float x in new[] { -0.62f, 0.62f })
                k.Box(Tile.Hazard, P(x, 0.8f, -0.42f), new Vector3(0.12f, 1.1f, 0.12f));
            k.Box(Tile.PaintedDark, P(0f, 1.38f, -0.42f), new Vector3(1.38f, 0.16f, 0.2f));
            k.Box(Tile.DarkSteel, P(0f, 1.1f, -0.35f), new Vector3(0.44f, 0.4f, 0.3f));
            k.Box(Tile.ArrowLabel, P(0f, 1.1f, -0.195f), new Vector3(0.24f, 0.24f, 0.01f));
            k.Cylinder(Tile.DarkSteel, P(0.45f, 1.46f, -0.42f), P(0.45f, 1.5f, -0.42f), 0.07f, 0.07f, 10);
            ModelKit beacon = k.Moving("kind=spin axis=0,1,0 speed=300", P(0.45f, 1.55f, -0.42f));
            beacon.Cylinder(Tile.Glass, P(0.45f, 1.5f, -0.42f), P(0.45f, 1.62f, -0.42f), 0.06f, 0.05f, 10);
            beacon.Box(Tile.LedGreen, P(0.45f, 1.56f, -0.39f), new Vector3(0.05f, 0.06f, 0.02f));
            StatusLight(k, P(-0.45f, 1.38f, -0.31f), new Vector3(0.06f, 0.06f, 0.02f));
        }
    }
}
