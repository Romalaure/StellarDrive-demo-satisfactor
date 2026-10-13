using System.Linq;
using Ships.Interface.Model.Parts.Common;
using Ships.Interface.Settings;
using Ships.Parts.Common;
using Ships.Interface.Model.Parts;
using StellarIndustries.Production;
using UnityEngine;

namespace StellarIndustries.Parts
{
    /// <summary>
    /// Item Lines (another mod) grafts two item ports on each of our parts after we built them, on
    /// the face and at the height it guesses: one ends up in the middle of the front on several
    /// models, and on containers and docks both float in front of the model. They are all put on
    /// the back service row the models are built for: hatch 0 in the middle, hatch 1 left of it,
    /// at the power socket's height, facing back like it. Only their place changes (id, type and
    /// behaviour stay Item Lines'), on the part prefab, before the world's parts are shown.
    /// </summary>
    internal static class PortLayout
    {
        private const int ItemCable = 3, PowerCable = 2;

        public static void Arrange()
        {
            int moved = 0;
            Quaternion? facingBack = null;
            // A port facing back, as Item Lines makes it, for the parts that have none (containers, docks)
            foreach (MachineDef def in MachineCatalog.All.Where(d => !d.IsLogistics))
            {
                GameObject prefab = Prefab(def);
                var bounds = prefab != null ? prefab.GetComponent<ShipPartBounds>() : null;
                SocketObject port = prefab != null ? prefab.GetComponentsInChildren<SocketObject>(true).Where(x => (int)x.Type == ItemCable).OrderBy(x => x.Id).FirstOrDefault() : null;
                if (port != null && bounds != null && prefab.transform.InverseTransformPoint(port.transform.position).z < bounds.center.z)
                {
                    facingBack = Quaternion.Inverse(prefab.transform.rotation) * port.transform.rotation;
                    break;
                }
            }
            foreach (MachineDef def in MachineCatalog.All)
            {
                GameObject prefab = Prefab(def);
                if (prefab == null)
                    continue;
                Transform root = prefab.transform;
                SocketObject[] sockets = prefab.GetComponentsInChildren<SocketObject>(true);
                SocketObject[] ports = sockets.Where(s => (int)s.Type == ItemCable).OrderBy(s => s.Id).ToArray();
                if (ports.Length == 0)
                    continue;
                SocketObject power = sockets.FirstOrDefault(s => (int)s.Type == PowerCable);
                var bounds = prefab.GetComponent<ShipPartBounds>();
                Vector3 row;
                Quaternion rotation;
                if (power != null)
                {
                    row = root.InverseTransformPoint(power.transform.position);
                    rotation = Quaternion.Inverse(root.rotation) * power.transform.rotation;
                    // Item Lines' own back port keeps its orientation (its plug model differs from the power one)
                    if (facingBack != null)
                        rotation = facingBack.Value;
                }
                else
                {
                    if (bounds == null || facingBack == null)
                        continue;
                    row = new Vector3(bounds.center.x, bounds.center.y - bounds.bounds.y * 0.5f + MachineModels.ServiceHeight * def.Scale,
                        bounds.center.z - bounds.bounds.z * 0.5f - 0.06f);
                    rotation = facingBack.Value;
                }
                for (int i = 0; i < ports.Length; i++)
                {
                    var target = new Vector3((bounds != null ? bounds.center.x : 0f) - MachineModels.SocketOffset * def.Scale * i, row.y, row.z);
                    Vector3 now = root.InverseTransformPoint(ports[i].transform.position);
                    Quaternion nowRotation = Quaternion.Inverse(root.rotation) * ports[i].transform.rotation;
                    if ((now - target).sqrMagnitude < 1e-4f && Quaternion.Angle(nowRotation, rotation) < 1f)
                        continue;
                    ports[i].transform.position = root.TransformPoint(target);
                    ports[i].transform.rotation = root.rotation * rotation;
                    moved++;
                }
            }
            if (moved > 0)
            {
                SDModKit.Game.PartGeometry.Invalidate();
                IndustriesMod.Log.Msg($"ports: {moved} item port(s) set on the parts' back service row");
            }
        }

        private static GameObject Prefab(MachineDef def)
        {
            PartSettings settings = SDModKit.Game.CustomParts.Get(def.PartId);
            return settings != null ? settings.part : null;
        }
    }
}

namespace StellarIndustries.Parts
{
    /// <summary>Right after SDModKit attached other mods' sockets (Item Lines' ports) to the parts.</summary>
    [HarmonyLib.HarmonyPatch(typeof(SDModKit.Content.ContentAttach), nameof(SDModKit.Content.ContentAttach.Apply))]
    internal static class PortLayoutPatch
    {
        private static void Postfix()
        {
            try
            {
                PortLayout.Arrange();
            }
            catch (System.Exception e)
            {
                Core.ErrorLog.Report("ports", e);
            }
        }
    }
}
