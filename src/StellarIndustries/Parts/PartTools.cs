using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Items.Model;
using SDModKit.Content;
using SDModKit.Game;
using Ships.Interface.Model.Parts;
using Ships.Interface.Model.Parts.Common;
using Ships.Interface.Settings;
using Ships.Parts.Common;
using Ships.Parts.Common.Model.Reactions;
using StellarIndustries.Content;
using UnityEngine;
using Object = UnityEngine.Object;

namespace StellarIndustries.Parts
{
    /// <summary>
    /// New ship parts are clones of an existing game part (the donor) under a new id, made by
    /// SDModKit, which also adds them to the build menu and lets the game save and load them like
    /// its own parts. Only call into this class when SDModKit is loaded.
    /// </summary>
    internal static class PartTools
    {
        public const string Tab = "Industrie";

        /// <summary>Every part id this mod adds.</summary>
        public static readonly HashSet<ushort> Ids = new HashSet<ushort>();

        private static readonly AccessTools.FieldRef<PartSettings, List<ItemInstance>> BuildingCost =
            AccessTools.FieldRefAccess<PartSettings, List<ItemInstance>>("buildingCost");

        public static void Register(ushort id, string donor, int row, Action<PartSettings, GameObject> configure)
        {
            Ids.Add(id);
            CustomParts.Register(new CustomPartDefinition
            {
                Id = id,
                Name = "SI_" + id,
                Donor = donor,
                BuildTab = Tab,
                BuildRow = row,
                Configure = (settings, prefab) =>
                {
                    configure(settings, prefab);
                    EnsureVisual(prefab);
                }
            });
        }

        /// <summary>
        /// The build menu thumbnail, the placement preview and the build tool screen all copy the
        /// part's IShipPartVisual object: a part without one throws and breaks the build tool.
        /// </summary>
        private static void EnsureVisual(GameObject prefab)
        {
            if (prefab.GetComponentInChildren<IShipPartVisual>(true) != null)
                return;
            Transform visuals = prefab.transform.Find("Visuals") ?? prefab.transform.Find("Visual") ?? prefab.transform;
            visuals.gameObject.AddComponent<DefaultShipPartVisuals>();
        }

        public static void Describe(PartSettings settings, string label, string description, float mass, (uint Item, int Count)[] cost)
        {
            settings.fullLabel = label;
            settings.description = description;
            settings.localizedDescription = null;
            settings.mass = mass;
            var list = new List<ItemInstance>();
            foreach ((uint item, int count) in cost ?? new (uint, int)[0])
            {
                ItemSettings itemSettings = IndustrialItems.Find(item);
                if (itemSettings != null)
                    list.Add(new ItemInstance(itemSettings, count));
                else
                    IndustriesMod.Log.Warning($"part {settings.id}: cost item {item} not found");
            }
            BuildingCost(settings) = list;
        }

        /// <summary>The donor's placement box (before <see cref="Blank"/> replaces it).</summary>
        public static Bounds DonorBounds(GameObject prefab)
        {
            var bounds = prefab.GetComponent<ShipPartBounds>();
            return bounds != null ? new Bounds(bounds.center, bounds.bounds) : new Bounds(new Vector3(0f, 0.5f, 0f), Vector3.one);
        }

        /// <summary>
        /// Removes the donor's model, its own visual behaviour and its plugs; keeps the part's
        /// state and interactions. Returns the visual transform to build the new model under.
        /// </summary>
        public static Transform Blank(GameObject prefab, Vector3 boundsCenter, Vector3 boundsSize)
        {
            GameSkin.Capture(prefab);
            IShipPartVisual visual = prefab.GetComponentInChildren<IShipPartVisual>(true);
            Transform visuals = visual != null ? visual.GameObject.transform : (prefab.transform.Find("Visuals") ?? prefab.transform.Find("Visual"));
            if (visuals == null)
            {
                visuals = new GameObject("Visuals").transform;
                visuals.SetParent(prefab.transform, false);
            }
            foreach (MonoBehaviour behaviour in visuals.GetComponents<MonoBehaviour>())
            {
                if (behaviour is IShipPartVisual)
                    Object.DestroyImmediate(behaviour);
            }
            foreach (Transform child in visuals.Cast<Transform>().ToList())
                Object.DestroyImmediate(child.gameObject);
            visuals.localPosition = Vector3.zero;
            visuals.localRotation = Quaternion.identity;
            visuals.localScale = Vector3.one;
            SetBounds(prefab, boundsCenter, boundsSize);
            foreach (SocketObject socket in prefab.GetComponentsInChildren<SocketObject>(true))
            {
                if (socket.visualObject != null)
                    Object.DestroyImmediate(socket.visualObject);
                if (socket.interactive != null)
                    Object.DestroyImmediate(socket.interactive);
                Object.DestroyImmediate(socket.gameObject);
            }
            return visuals;
        }

        /// <summary>Placement box and the interaction collider (look at it, use it, recycle it) of the whole part.</summary>
        public static void SetBounds(GameObject prefab, Vector3 center, Vector3 size)
        {
            var bounds = prefab.GetComponent<ShipPartBounds>();
            if (bounds != null)
            {
                bounds.center = center;
                bounds.bounds = size;
            }
            Transform interactions = prefab.transform.Find("Interactions");
            if (interactions == null)
                return;
            foreach (BoxCollider box in interactions.GetComponentsInChildren<BoxCollider>(true))
            {
                box.transform.localPosition = center;
                box.transform.localRotation = Quaternion.identity;
                box.center = Vector3.zero;
                box.size = size;
            }
        }

        /// <summary>
        /// Adds a cable socket (SDModKit graft) facing <paramref name="outward"/>, pushed clear of the
        /// part's box. Socket ids must run 0..n-1 without gaps (the game indexes them in an array).
        /// </summary>
        public static void GraftSocket(GameObject prefab, byte id, CableType type, Vector3 position, Vector3 outward)
        {
            Quaternion rotation = Quaternion.LookRotation(-outward, Mathf.Abs(outward.y) > 0.9f ? Vector3.forward : Vector3.up);
            GameObject socket = ContentBuilder.GraftSocket(prefab, id, type, position, rotation);
            if (socket == null)
            {
                IndustriesMod.Log.Error($"part {prefab.name}: socket {id} ({type}) not grafted");
                return;
            }
            PartFinish.Seat("SI", prefab, prefab.GetComponent<ShipPartBounds>(), socket.GetComponent<SocketObject>());
        }
    }
}

namespace StellarIndustries.Parts
{
    /// <summary>
    /// The mod's parts are in no research yet (the mod's own tech tree comes with a later
    /// phase): they are available from the start instead of staying locked forever.
    /// </summary>
    [HarmonyLib.HarmonyPatch(typeof(Research.ResearchClientProxy), nameof(Research.ResearchClientProxy.IsPartUnlocked))]
    internal static class PartUnlockPatch
    {
        private static void Postfix(Ships.Interface.Settings.PartSettings part, ref bool __result)
        {
            if (!__result && part != null && PartTools.Ids.Contains(part.id))
                __result = true;
        }
    }
}
