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
    internal static partial class MachineModels
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
            // The model is designed at scale 1 around the bottom centre, then scaled there as a whole
            var kit = new ModelKit("SI_Model", o);
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
                case MachineModel.Foundry: Foundry(kit, w, d); break;
                case MachineModel.Refinery: Refinery(kit, w, d); break;
                case MachineModel.Accumulator: Accumulator(kit, w, d); break;
                case MachineModel.Accelerator: Accelerator(kit, w, d); break;
                case MachineModel.BiomassBurner: BiomassBurner(kit, w, d); break;
                case MachineModel.Condenser: Condenser(kit, w, d); break;
                case MachineModel.Reactor: Reactor(kit, w, d); break;
                case MachineModel.Encoder: Encoder(kit, w, d); break;
                case MachineModel.Converter: Converter(kit, w, d); break;
                case MachineModel.Recycler: Recycler(kit, w, d); break;
                case MachineModel.Exchange: Exchange(kit, w, d); break;
                case MachineModel.Radar: Radar(kit, w, d); break;
                default: Workshop(kit, w, d); break;
            }
            if (!def.IsLogistics)
                ServicePanel(kit, w, d, NeedsPower(def));
            Transform root = kit.Build(visuals, visuals.gameObject.layer, IndustrialSkin.Material, GameSkin.Ready ? GameSkin.Palette : (Vector2?)null, Vector3.zero);
            root.localScale = Vector3.one * def.Scale;
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
        /// <summary>Whether the part has a power socket (machines, generators, accumulators; not the radar).</summary>
        public static bool NeedsPower(Production.MachineDef def) => !def.IsLogistics && (def.Watts > 0f || def.Generates || def.Stores);

        private static void ServicePanel(ModelKit k, float w, float d, bool power)
        {
            float back = -d * 0.5f;
            float y = ServiceHeight;
            k.Box(Tile.DarkSteel, P(0f, y, back + 0.07f), new Vector3(w - 0.24f, 0.3f, 0.06f));
            // Legs down to the deck, so the row never floats behind open machines
            foreach (float side in new[] { -1f, 1f })
                k.Box(Tile.DarkSteel, P(side * (w * 0.5f - 0.17f), (0.1f + y - 0.15f) * 0.5f, back + 0.07f), new Vector3(0.05f, y - 0.15f - 0.1f + 0.02f, 0.05f));
            float x = SocketOffset;
            if (power)
            {
                // Power junction box: its face is the part's back face, the socket comes out of it
                k.Box(Tile.LabelPower, P(x, y, back + 0.05f), new Vector3(0.15f, 0.22f, 0.1f));
                k.Box(Tile.DarkSteel, P(x, y + 0.12f, back + 0.05f), new Vector3(0.17f, 0.03f, 0.11f));
                k.Pipe(Tile.CableJacket, 0.022f, P(x, y + 0.13f, back + 0.05f), P(x, y + 0.32f, back + 0.05f), P(x, y + 0.32f, back + 0.16f));
                k.Box(Tile.LedGreen, P(x + 0.055f, y + 0.08f, back), new Vector3(0.025f, 0.025f, 0.01f));
            }
            // Item hatches in the middle and left of the power socket (Item Lines' ports are set there, see PortLayout)
            ItemHatch(k, 0f, back);
            ItemHatch(k, -x, back);
            // Warning plate above the row
            k.Box(Tile.LabelWarning, P(-x - 0.2f, y + 0.08f, back + 0.04f), new Vector3(0.12f, 0.1f, 0.02f));
        }

        /// <summary>A framed item hatch on a back face at <paramref name="back"/>, at the service row's height.</summary>
        private static void ItemHatch(ModelKit k, float x, float back)
        {
            float y = ServiceHeight;
            k.Frame(Tile.PaintedYellow, P(x, y, back + 0.025f), new Vector2(0.16f, 0.24f), 0.025f, Vector3.back, 0.03f);
            k.Box(Tile.ArrowLabel, P(x, y + 0.07f, back + 0.04f), new Vector3(0.1f, 0.08f, 0.02f));
            k.Box(Tile.Grille, P(x, y - 0.03f, back + 0.04f), new Vector3(0.11f, 0.1f, 0.02f));
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
            // A post down to the deck: a console never floats (inside a body, the post is hidden)
            float bottom = at.y - 0.18f, deck = _o.y + 0.08f;
            if (bottom > deck + 0.02f)
                k.Box(Tile.DarkSteel, new Vector3(at.x, (bottom + deck) * 0.5f, at.z - 0.02f), new Vector3(0.07f, bottom - deck, 0.07f));
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
            k.Cylinder(Tile.Hazard, P(-0.25f, 1.36f, -0.15f), P(-0.25f, 1.4f, -0.15f), 0.335f, 0.335f, 4, capA: false, capB: false);
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
            foreach (float x in new[] { -0.22f, 0.22f })
            {
                k.Tube(Tile.Pipe, P(x, 1.85f, 0f), P(x, 1.88f, 0f), 0.11f, 14);
                k.Tube(Tile.Brushed, P(x, 1.62f, 0f), P(x, 1.86f, 0f), 0.08f, 14);
            }
            ModelKit ram = k.Moving("kind=press axis=0,1,0 amp=0.42 freq=0.45", P(0f, 1.2f, 0.02f));
            foreach (float x in new[] { -0.22f, 0.22f })
                ram.Tube(Tile.Brushed, P(x, 1.62f, 0.02f), P(x, 1.15f, 0.02f), 0.04f, 10);
            ram.Box(Tile.PaintedYellow, P(0f, 1.1f, 0.02f), new Vector3(0.78f, 0.18f, 0.4f));
            ram.Box(Tile.DarkSteel, P(0f, 0.99f, 0.04f), new Vector3(0.36f, 0.05f, 0.3f));
            // Hydraulic tank on the deck at the back, its two hoses running up behind the columns into the crown
            k.Cylinder(Tile.PaintedGreen, P(-0.4f, 0.3f, -0.3f), P(0.05f, 0.3f, -0.3f), 0.1f, 0.1f, 14);
            k.Bolts(P(0.05f, 0.3f, -0.3f), Vector3.right, 0.06f, 5, 0.02f);
            k.Pipe(Tile.CableJacket, 0.025f, P(-0.3f, 0.4f, -0.3f), P(-0.3f, 0.52f, -0.22f), P(-0.48f, 0.52f, -0.22f), P(-0.48f, 1.62f, -0.22f));
            k.Pipe(Tile.CableJacket, 0.025f, P(-0.05f, 0.4f, -0.3f), P(-0.05f, 0.56f, -0.24f), P(0.48f, 0.56f, -0.24f), P(0.48f, 1.62f, -0.24f));
            // Control box bolted on the right column, facing the operator
            Console(k, P(0.52f, 1.0f, 0.22f), 0.18f);
        }

        // ---- Crusher: a jaw crusher. Feed hopper, fixed jaw and swinging jaw driven by an
        //      eccentric shaft between two heavy flywheels, belt drive from a motor, discharge belt

        private static void Crusher(ModelKit k, float w, float d)
        {
            Skid(k, w, d);
            // Two heavy side frames
            foreach (float x in new[] { -0.33f, 0.33f })
            {
                k.Box(Tile.PaintedYellow, P(x, 0.58f, -0.05f), new Vector3(0.1f, 0.92f, 0.82f));
                k.Bolts(P(x + Mathf.Sign(x) * 0.05f, 0.35f, 0.25f), Vector3.right * Mathf.Sign(x), 0.06f, 6, 0.025f);
                k.Bolts(P(x + Mathf.Sign(x) * 0.05f, 0.35f, -0.35f), Vector3.right * Mathf.Sign(x), 0.06f, 6, 0.025f);
            }
            k.Box(Tile.DarkSteel, P(0f, 0.18f, -0.05f), new Vector3(0.56f, 0.12f, 0.82f));
            // Fixed jaw (front), slightly leaning back, with its wear plate
            k.Box(Tile.Rust, P(0f, 0.68f, 0.24f), new Vector3(0.56f, 0.72f, 0.1f), Quaternion.Euler(-8f, 0f, 0f));
            k.Box(Tile.Tread, P(0f, 0.68f, 0.185f), new Vector3(0.5f, 0.66f, 0.02f), Quaternion.Euler(-8f, 0f, 0f));
            // Eccentric shaft and its bearings on top of the frames
            k.Tube(Tile.Pipe, P(-0.6f, 1.05f, -0.15f), P(0.6f, 1.05f, -0.15f), 0.05f, 12);
            foreach (float x in new[] { -0.33f, 0.33f })
                k.Box(Tile.DarkSteel, P(x, 1.07f, -0.15f), new Vector3(0.14f, 0.12f, 0.2f));
            // Swinging jaw hanging from the shaft: it rocks against the fixed jaw
            ModelKit jaw = k.Moving("kind=swing axis=1,0,0 amp=5 freq=1.4", P(0f, 1.05f, -0.15f));
            jaw.Box(Tile.Steel, P(0f, 0.68f, -0.08f), new Vector3(0.5f, 0.74f, 0.12f), Quaternion.Euler(12f, 0f, 0f));
            jaw.Box(Tile.Tread, P(0f, 0.66f, -0.01f), new Vector3(0.46f, 0.66f, 0.02f), Quaternion.Euler(12f, 0f, 0f));
            jaw.Box(Tile.DarkSteel, P(0f, 1.0f, -0.15f), new Vector3(0.3f, 0.14f, 0.16f));
            // Flywheels on both ends of the shaft
            Flywheel(k, P(-0.5f, 1.05f, -0.15f), 0.36f, 0.08f, 160f, Tile.DarkSteel);
            Flywheel(k, P(0.5f, 1.05f, -0.15f), 0.36f, 0.08f, 160f, Tile.DarkSteel);
            // Feed hopper over the jaws
            k.Cylinder(Tile.Rust, P(0f, 1.12f, 0.05f), P(0f, 1.5f, 0.05f), 0.3f, 0.52f, 4, capA: false, capB: false);
            k.Cylinder(Tile.Hazard, P(0f, 1.48f, 0.05f), P(0f, 1.53f, 0.05f), 0.535f, 0.535f, 4, capA: false, capB: false);
            // Motor on the skid, V-belts up to the left flywheel
            Motor(k, P(-0.52f, 0.26f, 0.48f), P(-0.52f, 0.26f, 0.2f), 0.1f);
            k.Cylinder(Tile.DarkSteel, P(-0.52f, 0.26f, 0.2f), P(-0.52f, 0.26f, 0.12f), 0.07f, 0.07f, 12);
            foreach (float dx in new[] { -0.02f, 0.02f })
                k.Beam(Tile.Rubber, P(-0.52f + dx, 0.31f, 0.15f), P(-0.52f + dx, 0.98f, -0.07f), 0.025f);
            // Discharge belt under the jaws, out of the front
            k.Box(Tile.DarkSteel, P(0f, 0.3f, 0.42f), new Vector3(0.42f, 0.05f, 0.4f), Quaternion.Euler(-12f, 0f, 0f));
            k.Box(Tile.CrushedIron, P(0.02f, 0.34f, 0.4f), new Vector3(0.2f, 0.05f, 0.16f), Quaternion.Euler(-12f, 20f, 0f));
            k.Moving("kind=spin axis=1,0,0 speed=180", P(0f, 0.37f, 0.6f))
                .Cylinder(Tile.Rubber, P(-0.2f, 0.37f, 0.6f), P(0.2f, 0.37f, 0.6f), 0.04f, 0.04f, 10);
            Console(k, P(0.48f, 0.5f, 0.42f), 0.26f);
        }

        // ---- Assembler: a bench with a turntable and a floor-mounted robot arm (turret, upper arm,
        //      forearm, gripper: every joint where the pieces meet), parts trays, a light mast

        private static void Assembler(ModelKit k, float w, float d)
        {
            Skid(k, w, d);
            k.Box(Tile.PaintedLight, P(0f, 0.42f, 0f), new Vector3(1.5f, 0.6f, 1.0f));
            k.Box(Tile.Brushed, P(0f, 0.74f, 0.05f), new Vector3(1.4f, 0.04f, 0.86f));
            k.Box(Tile.Grille, P(-0.3f, 0.42f, 0.505f), new Vector3(0.5f, 0.3f, 0.01f));
            // Turntable with the work piece
            ModelKit table = k.Moving("kind=spin axis=0,1,0 speed=40", P(-0.2f, 0.78f, 0.12f));
            table.Cylinder(Tile.DarkSteel, P(-0.2f, 0.76f, 0.12f), P(-0.2f, 0.8f, 0.12f), 0.24f, 0.24f, 18);
            table.Box(Tile.Steel, P(-0.2f, 0.86f, 0.12f), new Vector3(0.22f, 0.1f, 0.16f));
            table.Box(Tile.BoltHeads, P(-0.2f, 0.915f, 0.12f), new Vector3(0.2f, 0.01f, 0.14f));
            // Robot arm: base on the bench, turret turning around y, then shoulder and elbow
            Vector3 dir = new Vector3(-0.86f, 0f, 0.51f);
            Vector3 axis = new Vector3(dir.z, 0f, -dir.x);
            string ax = ModelKit.Axis(axis);
            Vector3 basePos = P(0.38f, 0.76f, -0.22f);
            k.Cylinder(Tile.DarkSteel, basePos, basePos + Vector3.up * 0.06f, 0.13f, 0.13f, 16);
            ModelKit turret = k.Moving("kind=swing axis=0,1,0 amp=35 freq=0.3", basePos + Vector3.up * 0.06f);
            Vector3 shoulder = basePos + Vector3.up * 0.2f;
            turret.Cylinder(Tile.PaintedYellow, basePos + Vector3.up * 0.06f, shoulder, 0.1f, 0.08f, 14);
            turret.Cylinder(Tile.DarkSteel, shoulder - axis * 0.07f, shoulder + axis * 0.07f, 0.06f, 0.06f, 12);
            ModelKit upper = turret.Moving($"kind=swing axis={ax} amp=10 freq=0.6", shoulder);
            Vector3 elbow = shoulder + Vector3.up * 0.3f + dir * 0.16f;
            upper.Beam(Tile.PaintedYellow, shoulder, elbow, 0.07f);
            upper.Cylinder(Tile.DarkSteel, elbow - axis * 0.06f, elbow + axis * 0.06f, 0.05f, 0.05f, 12);
            ModelKit fore = upper.Moving($"kind=swing axis={ax} amp=18 freq=0.6 phase=0.25", elbow);
            Vector3 wrist = elbow + dir * 0.34f - Vector3.up * 0.24f;
            fore.Beam(Tile.PaintedYellow, elbow, wrist, 0.055f);
            fore.Cylinder(Tile.DarkSteel, wrist, wrist - Vector3.up * 0.05f, 0.035f, 0.035f, 10);
            fore.Box(Tile.Steel, wrist - Vector3.up * 0.08f + axis * 0.025f, new Vector3(0.015f, 0.05f, 0.02f));
            fore.Box(Tile.Steel, wrist - Vector3.up * 0.08f - axis * 0.025f, new Vector3(0.015f, 0.05f, 0.02f));
            fore.Box(Tile.LedGreen, wrist - Vector3.up * 0.03f + dir * 0.03f, new Vector3(0.02f, 0.02f, 0.02f));
            // Parts trays and a light mast
            k.Box(Tile.DarkSteel, P(0.45f, 0.8f, 0.25f), new Vector3(0.34f, 0.08f, 0.28f));
            k.Box(Tile.BoltHeads, P(0.45f, 0.845f, 0.25f), new Vector3(0.28f, 0.01f, 0.22f));
            k.Box(Tile.DarkSteel, P(-0.6f, 0.8f, -0.25f), new Vector3(0.24f, 0.08f, 0.3f));
            k.Box(Tile.Steel, P(-0.6f, 0.85f, -0.25f), new Vector3(0.2f, 0.02f, 0.24f));
            k.Tube(Tile.DarkSteel, P(-0.68f, 0.74f, 0.4f), P(-0.68f, 1.35f, 0.4f), 0.02f, 8);
            k.Status(true).Cylinder(Tile.LedGreen, P(-0.68f, 1.35f, 0.4f), P(-0.68f, 1.43f, 0.4f), 0.035f, 0.035f, 10);
            k.Status(false).Cylinder(Tile.LedRed, P(-0.68f, 1.35f, 0.4f), P(-0.68f, 1.43f, 0.4f), 0.035f, 0.035f, 10);
            Console(k, P(0.45f, 0.45f, 0.53f), 0.4f);
        }

        // ---- Fabricator: a robot cell open at the front (safety rails), closed at the back, two
        //      floor-mounted arms working on a part carried by a belt, stack light on top

        private static void Fabricator(ModelKit k, float w, float d)
        {
            Skid(k, w, d);
            float half = w * 0.5f - 0.08f;
            k.Box(Tile.PaintedLight, P(0f, 0.32f, 0f), new Vector3(w - 0.1f, 0.4f, d - 0.12f));
            k.Box(Tile.Tread, P(0f, 0.53f, 0f), new Vector3(w - 0.14f, 0.02f, d - 0.16f));
            // Frame: four posts, top beams, a light bar under the roof at the back
            foreach (float x in new[] { -half, half })
            {
                foreach (float z in new[] { -0.47f, 0.47f })
                    k.Box(Tile.PaintedBlue, P(x, 1.02f, z), new Vector3(0.07f, 0.96f, 0.07f));
                k.Box(Tile.PaintedBlue, P(x, 1.52f, 0f), new Vector3(0.07f, 0.06f, 1.0f));
                // Side walls: solid low panel, mesh above
                k.Box(Tile.PaintedLight, P(x, 0.75f, 0f), new Vector3(0.03f, 0.42f, 0.88f));
                k.Box(Tile.Grille, P(x, 1.22f, 0f), new Vector3(0.02f, 0.52f, 0.88f));
            }
            k.Box(Tile.PaintedBlue, P(0f, 1.52f, -0.47f), new Vector3(w - 0.1f, 0.06f, 0.07f));
            k.Box(Tile.PaintedBlue, P(0f, 1.52f, 0.47f), new Vector3(w - 0.1f, 0.06f, 0.07f));
            // Back wall, closed
            k.Box(Tile.PaintedDark, P(0f, 1.02f, -0.48f), new Vector3(w - 0.24f, 0.96f, 0.03f));
            k.Moving("kind=glow freq=0.3", P(0f, 1.45f, -0.45f)).Box(Tile.Screen, P(0f, 1.45f, -0.46f), new Vector3(w - 0.4f, 0.05f, 0.02f));
            // Front: open, with two yellow safety rails and a hazard sill
            k.Box(Tile.PaintedYellow, P(0f, 0.8f, 0.5f), new Vector3(w - 0.24f, 0.04f, 0.04f));
            k.Box(Tile.PaintedYellow, P(0f, 1.1f, 0.5f), new Vector3(w - 0.24f, 0.04f, 0.04f));
            k.Box(Tile.Hazard, P(0f, 0.56f, 0.48f), new Vector3(w - 0.24f, 0.04f, 0.04f));
            // Belt across the cell carrying the work piece
            k.Box(Tile.DarkSteel, P(0f, 0.6f, 0.05f), new Vector3(w - 0.3f, 0.06f, 0.24f));
            for (int i = 0; i < 4; i++)
                k.Moving("kind=spin axis=0,0,1 speed=200", P(-0.5f + i * 0.33f, 0.65f, 0.05f))
                    .Cylinder(Tile.Pipe, P(-0.5f + i * 0.33f, 0.65f, -0.06f), P(-0.5f + i * 0.33f, 0.65f, 0.16f), 0.03f, 0.03f, 8);
            k.Box(Tile.Circuit, P(0f, 0.7f, 0.05f), new Vector3(0.3f, 0.05f, 0.2f));
            // Two arms on pedestals behind the belt, reaching over the part
            foreach ((float x, float phase) in new[] { (-0.42f, 0f), (0.42f, 0.5f) })
            {
                Vector3 basePos = P(x, 0.54f, -0.28f);
                Vector3 dir = new Vector3(-Mathf.Sign(x) * 0.6f, 0f, 0.8f);
                Vector3 axis = new Vector3(dir.z, 0f, -dir.x);
                k.Cylinder(Tile.DarkSteel, basePos, basePos + Vector3.up * 0.1f, 0.1f, 0.1f, 14);
                ModelKit turret = k.Moving($"kind=swing axis=0,1,0 amp=30 freq=0.45 phase={phase:0.0}", basePos + Vector3.up * 0.1f);
                Vector3 shoulder = basePos + Vector3.up * 0.24f;
                turret.Cylinder(Tile.PaintedOrange, basePos + Vector3.up * 0.1f, shoulder, 0.08f, 0.07f, 12);
                ModelKit upper = turret.Moving($"kind=swing axis={ModelKit.Axis(axis)} amp=12 freq=0.9 phase={phase:0.0}", shoulder);
                Vector3 elbow = shoulder + Vector3.up * 0.32f + dir * 0.1f;
                upper.Beam(Tile.PaintedOrange, shoulder, elbow, 0.06f);
                upper.Cylinder(Tile.DarkSteel, elbow - axis * 0.05f, elbow + axis * 0.05f, 0.045f, 0.045f, 10);
                Vector3 wrist = elbow + dir * 0.3f - Vector3.up * 0.3f;
                upper.Beam(Tile.PaintedOrange, elbow, wrist, 0.045f);
                upper.Cylinder(Tile.Steel, wrist, wrist - Vector3.up * 0.06f, 0.025f, 0.01f, 8);
            }
            // Stack light on the front right post, control desk outside the front right corner
            k.Tube(Tile.DarkSteel, P(half, 1.55f, 0.47f), P(half, 1.68f, 0.47f), 0.02f, 6);
            k.Status(true).Cylinder(Tile.LedGreen, P(half, 1.68f, 0.47f), P(half, 1.76f, 0.47f), 0.04f, 0.04f, 10);
            k.Status(false).Cylinder(Tile.LedRed, P(half, 1.68f, 0.47f), P(half, 1.76f, 0.47f), 0.04f, 0.04f, 10);
            Console(k, P(half - 0.25f, 0.35f, 0.54f), 0.32f);
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

        // ---- Generator: firebox, engine block with four pistons, flywheel and alternator on their
        //      bearing stand, radiator and fan against the block, exhausts

        private static void Generator(ModelKit k, float w, float d)
        {
            Skid(k, w, d);
            // Firebox and its fuel hopper on the left
            k.Box(Tile.Rust, P(-0.45f, 0.45f, 0f), new Vector3(0.45f, 0.62f, 0.85f));
            k.Frame(Tile.DarkSteel, P(-0.45f, 0.4f, 0.43f), new Vector2(0.3f, 0.26f), 0.05f, Vector3.forward, 0.04f);
            k.Moving("kind=glow freq=1.6", P(-0.45f, 0.4f, 0.42f)).Box(Tile.Furnace, P(-0.45f, 0.4f, 0.425f), new Vector3(0.22f, 0.18f, 0.01f));
            k.Box(Tile.DarkSteel, P(-0.45f, 0.78f, -0.15f), new Vector3(0.3f, 0.04f, 0.3f));
            k.Cylinder(Tile.Brushed, P(-0.45f, 0.8f, -0.15f), P(-0.45f, 1.05f, -0.15f), 0.12f, 0.22f, 4, capA: true, capB: false);
            // Engine block, cylinders and pistons
            k.Box(Tile.DarkSteel, P(0.12f, 0.45f, -0.05f), new Vector3(0.55f, 0.6f, 0.6f));
            for (int i = 0; i < 4; i++)
            {
                float z = -0.27f + i * 0.15f;
                k.Tube(Tile.Steel, P(0.12f, 0.75f, z), P(0.12f, 0.95f, z), 0.06f, 10);
                ModelKit piston = k.Moving($"kind=bob axis=0,1,0 amp=0.05 freq=2.2 phase={i * 0.25f:0.00}", P(0.12f, 1.0f, z));
                piston.Tube(Tile.Brushed, P(0.12f, 0.93f, z), P(0.12f, 1.03f, z), 0.045f, 10);
                piston.Box(Tile.DarkSteel, P(0.12f, 1.04f, z), new Vector3(0.1f, 0.02f, 0.1f));
            }
            // Crankshaft out of the block into the flywheel and alternator, all on a bearing stand
            k.Tube(Tile.Pipe, P(0.39f, 0.5f, -0.05f), P(0.7f, 0.5f, -0.05f), 0.04f, 10);
            k.Box(Tile.DarkSteel, P(0.6f, 0.26f, -0.05f), new Vector3(0.18f, 0.3f, 0.36f));
            k.Box(Tile.DarkSteel, P(0.6f, 0.43f, -0.05f), new Vector3(0.22f, 0.06f, 0.36f));
            Flywheel(k, P(0.45f, 0.5f, -0.05f), 0.22f, 0.06f, 320f, Tile.PaintedOrange);
            k.Cylinder(Tile.Winding, P(0.52f, 0.5f, -0.05f), P(0.68f, 0.5f, -0.05f), 0.15f, 0.15f, 16);
            k.Bolts(P(0.68f, 0.5f, -0.05f), Vector3.right, 0.1f, 6, 0.025f);
            // Radiator against the front of the block, fan in front of it
            k.Box(Tile.Grille, P(0.12f, 0.48f, 0.28f), new Vector3(0.5f, 0.5f, 0.06f));
            k.Moving("kind=spin axis=0,0,1 speed=600", P(0.12f, 0.48f, 0.32f)).Cylinder(Tile.Fan, P(0.12f, 0.48f, 0.31f), P(0.12f, 0.48f, 0.325f), 0.2f, 0.2f, 18);
            foreach (float x in new[] { -0.05f, 0.29f })
            {
                k.Pipe(Tile.Pipe, 0.05f, P(x, 0.75f, -0.3f), P(x, 1.4f, -0.3f));
                k.Tube(Tile.DarkSteel, P(x, 1.38f, -0.3f), P(x, 1.45f, -0.3f), 0.065f, 10);
            }
            Console(k, P(-0.45f, 0.95f, 0.36f), 0.3f, Tile.ScreenAmber);
        }

        // ---- Container: ribbed crate, corner castings, door bars, item hatches at the back

        private static void Container(ModelKit k, float w, float d, float h)
        {
            k.Box(Tile.PaintedBlue, P(0f, 0.6f, 0f), new Vector3(1.28f, 1.06f, 1.08f));
            for (int i = 0; i < 6; i++)
            {
                float x = -0.55f + i * 0.22f;
                k.Box(Tile.PaintedBlue, P(x, 0.6f, 0.55f), new Vector3(0.05f, 0.98f, 0.03f));
                if (x > 0.1f || x < -0.35f)
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
            ItemHatch(k, 0f, -0.555f);
            ItemHatch(k, -SocketOffset, -0.555f);
        }

        // ---- Loading dock: deck with rollers, an arch with a beacon and a chute, a service cabinet
        //      at the back holding the item ports

        private static void Dock(ModelKit k, float w, float d)
        {
            Skid(k, w, d, 0.2f);
            for (int i = 0; i < 5; i++)
            {
                float z = -0.3f + i * 0.18f;
                k.Moving("kind=spin axis=1,0,0 speed=-240", P(0f, 0.3f, z)).Tube(Tile.Pipe, P(-0.5f, 0.3f, z), P(0.5f, 0.3f, z), 0.045f, 10);
            }
            foreach (float x in new[] { -0.56f, 0.56f })
                k.Box(Tile.PaintedYellow, P(x, 0.32f, 0.05f), new Vector3(0.06f, 0.1f, 0.95f));
            foreach (float x in new[] { -0.62f, 0.62f })
                k.Box(Tile.Hazard, P(x, 0.8f, -0.42f), new Vector3(0.12f, 1.1f, 0.12f));
            k.Box(Tile.PaintedDark, P(0f, 1.38f, -0.42f), new Vector3(1.38f, 0.16f, 0.2f));
            k.Box(Tile.DarkSteel, P(0f, 1.1f, -0.35f), new Vector3(0.44f, 0.4f, 0.3f));
            k.Box(Tile.ArrowLabel, P(0f, 1.1f, -0.195f), new Vector3(0.24f, 0.24f, 0.01f));
            // Service cabinet at the back: the item lines plug into its hatches
            k.Box(Tile.PaintedDark, P(-0.1f, 0.4f, -0.5f), new Vector3(0.62f, 0.44f, 0.16f));
            ItemHatch(k, 0f, -0.6f);
            ItemHatch(k, -SocketOffset, -0.6f);
            k.Cylinder(Tile.DarkSteel, P(0.45f, 1.46f, -0.42f), P(0.45f, 1.5f, -0.42f), 0.07f, 0.07f, 10);
            ModelKit beacon = k.Moving("kind=spin axis=0,1,0 speed=300", P(0.45f, 1.55f, -0.42f));
            beacon.Cylinder(Tile.Glass, P(0.45f, 1.5f, -0.42f), P(0.45f, 1.62f, -0.42f), 0.06f, 0.05f, 10);
            beacon.Box(Tile.LedGreen, P(0.45f, 1.56f, -0.39f), new Vector3(0.05f, 0.06f, 0.02f));
            StatusLight(k, P(-0.45f, 1.38f, -0.31f), new Vector3(0.06f, 0.06f, 0.02f));
        }
    }
}
