using SDModKit.Game;
using Ships.Interface.Model.Parts;
using Ships.Interface.Settings;
using StellarIndustries.Core;
using StellarIndustries.Production;
using UnityEngine;

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
            var size = new Vector3(def.Width, def.Height, def.Depth);
            var center = new Vector3(donor.center.x, bottom + size.y * 0.5f, donor.center.z);
            Transform visuals = PartTools.Blank(prefab, center, size);
            var origin = new Vector3(donor.center.x, bottom, donor.center.z);
            MachineModels.Build(def.Model, visuals, origin, def);
            // Power in (or out, for a generator) at the back, low on the body; containers and docks need none
            if (!def.IsLogistics)
                PartTools.GraftSocket(prefab, MachineSimulation.PowerSocket, CableType.Power, origin + new Vector3(def.Width * 0.25f, 0.35f, -(def.Depth * 0.5f - 0.05f)), Vector3.back);
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
