using SDModKit.Game;
using Ships.Interface.Model.Parts;
using Ships.Interface.Settings;
using StellarIndustries.Core;
using StellarIndustries.Production;
using UnityEngine;
using UnityEngine.Rendering;

namespace StellarIndustries.Parts
{
    /// <summary>
    /// The production machines' build parts. Each is a clone of the game's Storage Chest (its
    /// StorageState gives the machine 10 slots, opened with the usual "use" key, saved and
    /// synchronized by the game), with its own model and a power socket at the back.
    /// </summary>
    internal static class MachineParts
    {
        private const string Donor = "StorageChest";
        private static Material _accent, _screen, _fallback;

        /// <summary>Only call when SDModKit is loaded.</summary>
        public static void Register()
        {
            BuildTabs.Declare(PartTools.Tab, 60, TabIcon());
            foreach (MachineDef def in MachineCatalog.All)
            {
                MachineDef machine = def;
                PartTools.Register(machine.PartId, Donor, machine.Tier, (settings, prefab) => Configure(machine, settings, prefab));
            }
            IndustriesMod.Log.Msg($"registered {MachineCatalog.All.Length} machine(s)");
        }

        private static void Configure(MachineDef def, PartSettings settings, GameObject prefab)
        {
            PartTools.Describe(settings, def.Name, def.Description, def.Mass, def.BuildCost);
            Bounds donor = PartTools.DonorBounds(prefab);
            // The panel rectangles of the models are measured on the LogicGates atlas, not the chest's
            if (!GameSkin.CaptureLogicGates())
                IndustriesMod.Log.Warning("part look: LogicGates atlas not loaded, the machine wears the chest's texture");
            // Same footprint origin as the chest (its bottom face), a machine-sized box above it
            float bottom = donor.center.y - donor.size.y * 0.5f;
            var size = new Vector3(1.4f, 1.5f, 1.2f);
            var center = new Vector3(donor.center.x, bottom + size.y * 0.5f, donor.center.z);
            Transform visuals = PartTools.Blank(prefab, center, size);
            var origin = new Vector3(donor.center.x, bottom, donor.center.z);
            BuildWorkshopModel(visuals, origin);
            // Power in at the back, low on the body
            PartTools.GraftSocket(prefab, MachineSimulation.PowerSocket, CableType.Power, origin + new Vector3(0.35f, 0.35f, -0.55f), Vector3.back);
            IndustriesMod.Log.Msg($"machine {def.PartId} '{def.Name}': donor box {donor.center} {donor.size}, machine box {center} {size}");
            if (Diagnostics.DevTest.Active)
            {
                foreach (Renderer r in prefab.GetComponentsInChildren<Renderer>(true))
                {
                    string path = r.name;
                    for (Transform t = r.transform.parent; t != null && t != prefab.transform; t = t.parent)
                        path = t.name + "/" + path;
                    IndustriesMod.Log.Msg($"dev: prefab renderer {path} at {prefab.transform.InverseTransformPoint(r.bounds.center)} mat {r.sharedMaterial?.name}");
                }
                foreach (Ships.Interface.Model.Parts.Common.SocketObject socket in prefab.GetComponentsInChildren<Ships.Interface.Model.Parts.Common.SocketObject>(true))
                    IndustriesMod.Log.Msg($"dev: prefab socket {socket.name} id {socket.Id} {socket.Type} at {socket.transform.localPosition}");
            }
        }

        // A squat press: skid, body, square hopper on top, flywheel on the right, control panel in front
        private static void BuildWorkshopModel(Transform visuals, Vector3 o)
        {
            int layer = visuals.gameObject.layer;
            var body = new MeshBuilder { Panel = GameSkin.PlainPanel };
            body.Box(o + new Vector3(0f, 0.06f, 0f), new Vector3(1.4f, 0.12f, 1.2f));
            body.Box(o + new Vector3(0f, 0.57f, -0.05f), new Vector3(1.15f, 0.9f, 0.95f));
            // Hopper: a square funnel (4-sided cone, turned 45 degrees by its vertex layout)
            body.Cylinder(o + new Vector3(0f, 1.02f, -0.05f), o + new Vector3(0f, 1.42f, -0.05f), 0.32f, 0.55f, 4, capA: true, capB: false);
            // Press column and its head over the front table
            body.Box(o + new Vector3(-0.35f, 0.75f, 0.47f), new Vector3(0.14f, 0.7f, 0.14f));
            body.Box(o + new Vector3(-0.15f, 1.05f, 0.47f), new Vector3(0.55f, 0.12f, 0.2f));
            body.Box(o + new Vector3(0.05f, 0.62f, 0.5f), new Vector3(0.5f, 0.06f, 0.22f));
            // Output chute on the left
            body.Box(o + new Vector3(-0.62f, 0.32f, 0.15f), new Vector3(0.12f, 0.08f, 0.5f), Quaternion.Euler(0f, 0f, -25f));
            GameSkin.AddMesh(visuals, layer, "SI_WorkshopBody", body, Fallback());

            var accent = new MeshBuilder { Panel = GameSkin.PlainPanel };
            // Flywheel and its hub on the right side
            accent.Cylinder(o + new Vector3(0.6f, 0.75f, -0.1f), o + new Vector3(0.68f, 0.75f, -0.1f), 0.32f, 0.32f, 18);
            accent.Cylinder(o + new Vector3(0.68f, 0.75f, -0.1f), o + new Vector3(0.74f, 0.75f, -0.1f), 0.08f, 0.08f, 10);
            // Hazard rim of the hopper and the skid's corners
            accent.Box(o + new Vector3(0f, 1.43f, -0.05f), new Vector3(0.8f, 0.04f, 0.8f), Quaternion.Euler(0f, 45f, 0f));
            accent.Box(o + new Vector3(0.62f, 0.14f, 0.52f), new Vector3(0.14f, 0.04f, 0.14f));
            accent.Box(o + new Vector3(-0.62f, 0.14f, 0.52f), new Vector3(0.14f, 0.04f, 0.14f));
            Add(visuals, layer, "SI_WorkshopAccent", accent, Accent());

            var screen = new MeshBuilder { Panel = GameSkin.PlainPanel };
            screen.Box(o + new Vector3(0.38f, 0.85f, 0.43f), new Vector3(0.3f, 0.2f, 0.02f));
            Add(visuals, layer, "SI_WorkshopScreen", screen, Screen());
        }

        private static void Add(Transform parent, int layer, string name, MeshBuilder builder, Material material)
        {
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

        // Safety orange, from the game's part material
        private static Material Accent()
        {
            if (_accent == null)
            {
                _accent = new Material(GameSkin.Material ?? Fallback()) { name = "SI_Accent" };
                if (_accent.HasProperty("_Color"))
                    _accent.SetColor("_Color", new Color(1f, 0.55f, 0.12f));
            }
            return _accent;
        }

        // A glowing cyan control screen
        private static Material Screen()
        {
            if (_screen == null)
            {
                _screen = new Material(GameSkin.Material ?? Fallback()) { name = "SI_Screen" };
                if (_screen.HasProperty("_Color"))
                    _screen.SetColor("_Color", new Color(0.2f, 0.75f, 0.9f));
                if (_screen.HasProperty("_EmissionColor"))
                {
                    _screen.EnableKeyword("_EMISSION");
                    _screen.SetColor("_EmissionColor", new Color(0.1f, 0.6f, 0.8f));
                }
            }
            return _screen;
        }

        // A gear on a dark disc
        private static Texture2D TabIcon()
        {
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "SI_TabIcon", hideFlags = HideFlags.HideAndDontSave };
            var clear = new Color(1f, 1f, 1f, 0f);
            var gear = new Color(1f, 0.6f, 0.15f, 1f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - 31.5f, dy = y - 31.5f;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Atan2(dy, dx);
                    float teeth = Mathf.Cos(a * 8f) > 0.3f ? 28f : 22f;
                    Color c = r <= teeth && r >= 9f ? gear : clear;
                    texture.SetPixel(x, y, c);
                }
            }
            texture.Apply();
            return texture;
        }
    }
}
