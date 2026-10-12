using System.Linq;
using Ships.Interface.Model.Parts.Common;
using Ships.Interface.Settings;
using StellarIndustries.Production;
using UnityEngine;

namespace StellarIndustries.Parts
{
    /// <summary>
    /// Item Lines (another mod) grafts its item ports on our machines after we built them: the
    /// first one lands on the back service row, in the centre hatch, as the models expect; a second
    /// one ends up in the middle of the front, in front of the operator console. That one is moved
    /// to the second hatch of the service row, left of the power socket, so every port and socket
    /// of a machine is on its back, side by side. Only the socket's place changes (its id, type and
    /// behaviour stay Item Lines'), on the part prefab, before the world's parts are shown.
    /// </summary>
    internal static class PortLayout
    {
        private const int ItemCable = 3;

        public static void Arrange()
        {
            int moved = 0;
            foreach (MachineDef def in MachineCatalog.All)
            {
                if (def.IsLogistics)
                    continue;
                PartSettings settings = SDModKit.Game.CustomParts.Get(def.PartId);
                GameObject prefab = settings != null ? settings.part : null;
                if (prefab == null)
                    continue;
                SocketObject[] ports = prefab.GetComponentsInChildren<SocketObject>(true).Where(s => (int)s.Type == ItemCable).OrderBy(s => s.Id).ToArray();
                if (ports.Length < 2)
                    continue;
                Transform root = prefab.transform;
                Vector3 first = root.InverseTransformPoint(ports[0].transform.position);
                for (int i = 1; i < ports.Length; i++)
                {
                    // Second hatch: mirror of the power socket; any further port, further left
                    var target = new Vector3(-MachineModels.SocketOffset * i, first.y, first.z);
                    Vector3 now = root.InverseTransformPoint(ports[i].transform.position);
                    if ((now - target).sqrMagnitude < 1e-4f)
                        continue;
                    ports[i].transform.position = root.TransformPoint(target);
                    ports[i].transform.rotation = ports[0].transform.rotation;
                    moved++;
                }
            }
            if (moved > 0)
            {
                SDModKit.Game.PartGeometry.Invalidate();
                IndustriesMod.Log.Msg($"ports: {moved} extra item port(s) moved to the machines' service row");
            }
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
