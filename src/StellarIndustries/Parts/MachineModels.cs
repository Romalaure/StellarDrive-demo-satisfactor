using StellarIndustries.Core;
using StellarIndustries.Production;
using UnityEngine;
using UnityEngine.Rendering;

namespace StellarIndustries.Parts
{
    /// <summary>
    /// The machines' 3D models, built from boxes and cylinders by the mod (no imported asset).
    /// Bodies wear the game's own part material and palette (<see cref="GameSkin"/>); accents are
    /// tinted copies of it: safety orange, a glowing control screen, furnace glow, dark steel.
    /// Every model stands on the part's bottom face at <c>o</c>, front towards +z.
    /// </summary>
    internal static class MachineModels
    {
        private static Material _accent, _screen, _glow, _dark, _fallback;

        public static void Build(MachineModel model, Transform visuals, Vector3 o, MachineDef def)
        {
            int layer = visuals.gameObject.layer;
            var body = new MeshBuilder { Panel = GameSkin.PlainPanel };
            var accent = new MeshBuilder { Panel = GameSkin.PlainPanel };
            var screen = new MeshBuilder { Panel = GameSkin.PlainPanel };
            var glow = new MeshBuilder { Panel = GameSkin.PlainPanel };
            var dark = new MeshBuilder { Panel = GameSkin.PlainPanel };
            switch (model)
            {
                case MachineModel.Smelter: Smelter(o, body, accent, screen, glow, dark); break;
                case MachineModel.Press: Press(o, body, accent, screen, glow, dark); break;
                case MachineModel.Crusher: Crusher(o, body, accent, screen, glow, dark); break;
                case MachineModel.Assembler: Assembler(o, body, accent, screen, glow, dark); break;
                case MachineModel.Extractor: Extractor(o, body, accent, screen, glow, dark); break;
                default: Workshop(o, body, accent, screen, glow, dark); break;
            }
            string name = "SI_" + model;
            Add(visuals, layer, name + "Body", body, GameSkin.Ready ? GameSkin.Material : Fallback());
            Add(visuals, layer, name + "Accent", accent, Accent());
            Add(visuals, layer, name + "Screen", screen, Screen());
            Add(visuals, layer, name + "Glow", glow, Glow());
            Add(visuals, layer, name + "Dark", dark, Dark());
        }

        // A squat press: skid, body, square hopper on top, flywheel on the right, control panel in front
        private static void Workshop(Vector3 o, MeshBuilder body, MeshBuilder accent, MeshBuilder screen, MeshBuilder glow, MeshBuilder dark)
        {
            body.Box(o + new Vector3(0f, 0.06f, 0f), new Vector3(1.4f, 0.12f, 1.2f));
            body.Box(o + new Vector3(0f, 0.57f, -0.05f), new Vector3(1.15f, 0.9f, 0.95f));
            body.Cylinder(o + new Vector3(0f, 1.02f, -0.05f), o + new Vector3(0f, 1.42f, -0.05f), 0.32f, 0.55f, 4, capA: true, capB: false);
            body.Box(o + new Vector3(-0.35f, 0.75f, 0.47f), new Vector3(0.14f, 0.7f, 0.14f));
            body.Box(o + new Vector3(-0.15f, 1.05f, 0.47f), new Vector3(0.55f, 0.12f, 0.2f));
            body.Box(o + new Vector3(0.05f, 0.62f, 0.5f), new Vector3(0.5f, 0.06f, 0.22f));
            body.Box(o + new Vector3(-0.62f, 0.32f, 0.15f), new Vector3(0.12f, 0.08f, 0.5f), Quaternion.Euler(0f, 0f, -25f));
            accent.Cylinder(o + new Vector3(0.6f, 0.75f, -0.1f), o + new Vector3(0.68f, 0.75f, -0.1f), 0.32f, 0.32f, 18);
            accent.Cylinder(o + new Vector3(0.68f, 0.75f, -0.1f), o + new Vector3(0.74f, 0.75f, -0.1f), 0.08f, 0.08f, 10);
            accent.Box(o + new Vector3(0f, 1.43f, -0.05f), new Vector3(0.8f, 0.04f, 0.8f), Quaternion.Euler(0f, 45f, 0f));
            Corners(o, accent, 1.4f, 1.2f);
            screen.Box(o + new Vector3(0.38f, 0.85f, 0.43f), new Vector3(0.3f, 0.2f, 0.02f));
        }

        // A brick furnace: wide base, firebox with a glowing mouth, tapered hood and a chimney
        private static void Smelter(Vector3 o, MeshBuilder body, MeshBuilder accent, MeshBuilder screen, MeshBuilder glow, MeshBuilder dark)
        {
            body.Box(o + new Vector3(0f, 0.06f, 0f), new Vector3(1.4f, 0.12f, 1.2f));
            dark.Box(o + new Vector3(0f, 0.55f, -0.05f), new Vector3(1.2f, 0.86f, 1.0f));
            // Hood: a 4-sided frustum from the firebox to the chimney
            body.Cylinder(o + new Vector3(0f, 0.98f, -0.05f), o + new Vector3(0f, 1.3f, -0.05f), 0.82f, 0.32f, 4, capA: true, capB: true);
            body.Cylinder(o + new Vector3(0.25f, 1.25f, -0.2f), o + new Vector3(0.25f, 1.7f, -0.2f), 0.12f, 0.12f, 12);
            accent.Cylinder(o + new Vector3(0.25f, 1.62f, -0.2f), o + new Vector3(0.25f, 1.7f, -0.2f), 0.15f, 0.15f, 12);
            // Mouth: a dark frame and the fire behind it
            dark.Box(o + new Vector3(0f, 0.48f, 0.47f), new Vector3(0.62f, 0.44f, 0.04f));
            glow.Box(o + new Vector3(0f, 0.46f, 0.48f), new Vector3(0.48f, 0.3f, 0.03f));
            // Pouring spout and ingot tray on the left
            body.Box(o + new Vector3(-0.62f, 0.5f, 0.2f), new Vector3(0.18f, 0.08f, 0.3f), Quaternion.Euler(0f, 0f, -20f));
            dark.Box(o + new Vector3(-0.6f, 0.2f, 0.3f), new Vector3(0.22f, 0.16f, 0.4f));
            glow.Box(o + new Vector3(-0.6f, 0.285f, 0.3f), new Vector3(0.16f, 0.02f, 0.32f));
            Corners(o, accent, 1.4f, 1.2f);
            screen.Box(o + new Vector3(0.48f, 0.8f, 0.46f), new Vector3(0.2f, 0.14f, 0.02f));
        }

        // A tall press: two columns, a crossbeam, a heavy ram over an anvil table
        private static void Press(Vector3 o, MeshBuilder body, MeshBuilder accent, MeshBuilder screen, MeshBuilder glow, MeshBuilder dark)
        {
            body.Box(o + new Vector3(0f, 0.1f, 0f), new Vector3(1.4f, 0.2f, 1.2f));
            body.Box(o + new Vector3(0f, 0.38f, 0f), new Vector3(1.0f, 0.36f, 0.8f));
            body.Box(o + new Vector3(-0.55f, 1.0f, 0f), new Vector3(0.2f, 1.6f, 0.3f));
            body.Box(o + new Vector3(0.55f, 1.0f, 0f), new Vector3(0.2f, 1.6f, 0.3f));
            body.Box(o + new Vector3(0f, 1.75f, 0f), new Vector3(1.35f, 0.25f, 0.45f));
            dark.Box(o + new Vector3(0f, 0.6f, 0.05f), new Vector3(0.6f, 0.08f, 0.5f));
            dark.Cylinder(o + new Vector3(0f, 1.62f, 0f), o + new Vector3(0f, 1.2f, 0f), 0.09f, 0.09f, 10);
            accent.Box(o + new Vector3(0f, 1.1f, 0.02f), new Vector3(0.7f, 0.2f, 0.5f));
            // Hazard stripes on the columns
            for (int i = 0; i < 3; i++)
            {
                accent.Box(o + new Vector3(-0.55f, 0.5f + i * 0.18f, 0.16f), new Vector3(0.21f, 0.06f, 0.02f), Quaternion.Euler(0f, 0f, 30f));
                accent.Box(o + new Vector3(0.55f, 0.5f + i * 0.18f, 0.16f), new Vector3(0.21f, 0.06f, 0.02f), Quaternion.Euler(0f, 0f, -30f));
            }
            // Hydraulic tank at the back
            body.Cylinder(o + new Vector3(-0.3f, 0.2f, -0.45f), o + new Vector3(0.3f, 0.2f, -0.45f), 0.15f, 0.15f, 14);
            screen.Box(o + new Vector3(0.45f, 0.45f, 0.41f), new Vector3(0.22f, 0.16f, 0.02f));
        }

        // A crusher: wide hopper over two counter-rotating drums, a chute below
        private static void Crusher(Vector3 o, MeshBuilder body, MeshBuilder accent, MeshBuilder screen, MeshBuilder glow, MeshBuilder dark)
        {
            body.Box(o + new Vector3(0f, 0.06f, 0f), new Vector3(1.4f, 0.12f, 1.2f));
            body.Box(o + new Vector3(0f, 0.45f, -0.05f), new Vector3(1.1f, 0.66f, 0.95f));
            // Drum housing and the two drums seen through its open top
            body.Box(o + new Vector3(0f, 0.9f, -0.05f), new Vector3(1.0f, 0.24f, 0.8f));
            dark.Cylinder(o + new Vector3(-0.42f, 1.0f, -0.2f), o + new Vector3(0.42f, 1.0f, -0.2f), 0.14f, 0.14f, 12);
            dark.Cylinder(o + new Vector3(-0.42f, 1.0f, 0.1f), o + new Vector3(0.42f, 1.0f, 0.1f), 0.14f, 0.14f, 12);
            // Hopper: open funnel
            body.Cylinder(o + new Vector3(0f, 1.05f, -0.05f), o + new Vector3(0f, 1.48f, -0.05f), 0.5f, 0.78f, 4, capA: false, capB: false);
            accent.Box(o + new Vector3(0f, 1.49f, -0.05f), new Vector3(1.1f, 0.04f, 1.1f), Quaternion.Euler(0f, 45f, 0f));
            // Gear covers on the side
            accent.Cylinder(o + new Vector3(0.55f, 0.95f, -0.2f), o + new Vector3(0.62f, 0.95f, -0.2f), 0.2f, 0.2f, 16);
            accent.Cylinder(o + new Vector3(0.55f, 0.95f, 0.1f), o + new Vector3(0.62f, 0.95f, 0.1f), 0.2f, 0.2f, 16);
            // Output chute at the front
            dark.Box(o + new Vector3(0f, 0.25f, 0.5f), new Vector3(0.45f, 0.08f, 0.3f), Quaternion.Euler(25f, 0f, 0f));
            Corners(o, accent, 1.4f, 1.2f);
            screen.Box(o + new Vector3(0.38f, 0.6f, 0.43f), new Vector3(0.22f, 0.15f, 0.02f));
        }

        // An assembler: low bench with a gantry and a two-joint arm, parts trays on both sides
        private static void Assembler(Vector3 o, MeshBuilder body, MeshBuilder accent, MeshBuilder screen, MeshBuilder glow, MeshBuilder dark)
        {
            body.Box(o + new Vector3(0f, 0.06f, 0f), new Vector3(1.6f, 0.12f, 1.2f));
            body.Box(o + new Vector3(0f, 0.42f, 0f), new Vector3(1.5f, 0.6f, 1.0f));
            dark.Box(o + new Vector3(0f, 0.74f, 0.05f), new Vector3(1.3f, 0.04f, 0.8f));
            // Gantry
            body.Box(o + new Vector3(-0.68f, 1.15f, -0.38f), new Vector3(0.1f, 0.8f, 0.1f));
            body.Box(o + new Vector3(0.68f, 1.15f, -0.38f), new Vector3(0.1f, 0.8f, 0.1f));
            body.Box(o + new Vector3(0f, 1.55f, -0.38f), new Vector3(1.46f, 0.1f, 0.12f));
            // Arm: carriage, upper and lower links, tool
            accent.Box(o + new Vector3(0.15f, 1.48f, -0.3f), new Vector3(0.2f, 0.12f, 0.2f));
            accent.Box(o + new Vector3(0.15f, 1.28f, -0.15f), new Vector3(0.08f, 0.4f, 0.08f), Quaternion.Euler(35f, 0f, 0f));
            accent.Box(o + new Vector3(0.15f, 1.0f, 0.02f), new Vector3(0.07f, 0.32f, 0.07f), Quaternion.Euler(-20f, 0f, 0f));
            dark.Cylinder(o + new Vector3(0.15f, 0.86f, 0.06f), o + new Vector3(0.15f, 0.78f, 0.06f), 0.05f, 0.02f, 8);
            // Trays
            dark.Box(o + new Vector3(-0.5f, 0.8f, 0.2f), new Vector3(0.35f, 0.08f, 0.3f));
            dark.Box(o + new Vector3(0.55f, 0.8f, 0.25f), new Vector3(0.3f, 0.08f, 0.25f));
            Corners(o, accent, 1.6f, 1.2f);
            screen.Box(o + new Vector3(-0.45f, 0.45f, 0.51f), new Vector3(0.3f, 0.18f, 0.02f));
            glow.Box(o + new Vector3(0.5f, 0.45f, 0.51f), new Vector3(0.06f, 0.06f, 0.02f));
        }

        // A drilling rig: braced tower, motor head on top, drill string and bit down through the base
        private static void Extractor(Vector3 o, MeshBuilder body, MeshBuilder accent, MeshBuilder screen, MeshBuilder glow, MeshBuilder dark)
        {
            body.Box(o + new Vector3(0f, 0.08f, 0f), new Vector3(1.4f, 0.16f, 1.2f));
            body.Box(o + new Vector3(-0.42f, 0.4f, -0.1f), new Vector3(0.45f, 0.5f, 0.8f));
            // Four legs leaning in, and cross braces
            foreach (float x in new[] { -0.45f, 0.45f })
            {
                foreach (float z in new[] { -0.4f, 0.4f })
                    body.Box(o + new Vector3(x * 0.8f, 1.1f, z * 0.8f), new Vector3(0.08f, 1.9f, 0.08f), Quaternion.Euler(z > 0 ? 6f : -6f, 0f, x > 0 ? -6f : 6f));
                accent.Box(o + new Vector3(x * 0.8f, 0.9f, 0f), new Vector3(0.05f, 0.05f, 0.75f));
                accent.Box(o + new Vector3(x * 0.72f, 1.5f, 0f), new Vector3(0.05f, 0.05f, 0.65f));
            }
            // Motor head
            body.Box(o + new Vector3(0f, 2.05f, 0f), new Vector3(0.6f, 0.3f, 0.6f));
            accent.Cylinder(o + new Vector3(0f, 2.2f, 0f), o + new Vector3(0f, 2.3f, 0f), 0.22f, 0.15f, 12);
            // Drill string and bit
            dark.Cylinder(o + new Vector3(0f, 1.9f, 0f), o + new Vector3(0f, 0.3f, 0f), 0.07f, 0.07f, 10);
            dark.Cylinder(o + new Vector3(0f, 0.3f, 0f), o + new Vector3(0f, 0.02f, 0f), 0.16f, 0.04f, 8);
            dark.Box(o + new Vector3(0f, 0.17f, 0f), new Vector3(0.5f, 0.04f, 0.5f));
            // Ore bin at the front, with its glowing level light
            body.Box(o + new Vector3(0.4f, 0.4f, 0.25f), new Vector3(0.5f, 0.5f, 0.55f));
            glow.Box(o + new Vector3(0.4f, 0.55f, 0.53f), new Vector3(0.3f, 0.05f, 0.02f));
            screen.Box(o + new Vector3(-0.42f, 0.5f, 0.31f), new Vector3(0.26f, 0.18f, 0.02f));
        }

        private static void Corners(Vector3 o, MeshBuilder accent, float width, float depth)
        {
            float x = width * 0.5f - 0.08f, z = depth * 0.5f - 0.08f;
            accent.Box(o + new Vector3(x, 0.14f, z), new Vector3(0.14f, 0.04f, 0.14f));
            accent.Box(o + new Vector3(-x, 0.14f, z), new Vector3(0.14f, 0.04f, 0.14f));
        }

        private static void Add(Transform parent, int layer, string name, MeshBuilder builder, Material material)
        {
            if (!builder.HasGeometry)
                return;
            var child = new GameObject(name) { layer = layer };
            child.transform.SetParent(parent, false);
            child.AddComponent<MeshFilter>().sharedMesh = builder.Build(name, GameSkin.Ready ? GameSkin.Palette : (Vector2?)null);
            var renderer = child.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.On;
        }

        private static Material Fallback()
        {
            if (_fallback == null)
                _fallback = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard")) { name = "SI_Fallback", color = new Color(0.6f, 0.62f, 0.65f) };
            return _fallback;
        }

        private static Material Tinted(ref Material cache, string name, Color color, Color? emission = null)
        {
            if (cache != null)
                return cache;
            cache = new Material(GameSkin.Material ?? Fallback()) { name = name };
            if (cache.HasProperty("_Color"))
                cache.SetColor("_Color", color);
            if (emission != null && cache.HasProperty("_EmissionColor"))
            {
                cache.EnableKeyword("_EMISSION");
                cache.SetColor("_EmissionColor", emission.Value);
            }
            return cache;
        }

        private static Material Accent() => Tinted(ref _accent, "SI_Accent", new Color(1f, 0.55f, 0.12f));

        private static Material Screen() => Tinted(ref _screen, "SI_Screen", new Color(0.2f, 0.75f, 0.9f), new Color(0.1f, 0.6f, 0.8f));

        private static Material Glow() => Tinted(ref _glow, "SI_Glow", new Color(1f, 0.5f, 0.1f), new Color(1.6f, 0.55f, 0.08f));

        private static Material Dark() => Tinted(ref _dark, "SI_Dark", new Color(0.32f, 0.33f, 0.36f));
    }
}
