using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Items.Model;
using StellarIndustries.Core;
using UnityEngine;
using Object = UnityEngine.Object;

namespace StellarIndustries.Content
{
    /// <summary>
    /// Makes the catalog's resources real game items (ItemSettings) added to the game's item
    /// list: they stack, weigh, show in inventories and chests, drop in the world and are saved
    /// by the game like its own. The 3D model is built by the mod and wears a game item's
    /// material, tinted; the game renders the inventory icon from it.
    /// </summary>
    internal static class IndustrialItems
    {
        private static readonly AccessTools.FieldRef<ItemSettingsList, ItemSettings[]> Items = AccessTools.FieldRefAccess<ItemSettingsList, ItemSettings[]>("items");
        private static readonly AccessTools.FieldRef<ItemSettingsList, Dictionary<uint, ItemSettings>> Dict = AccessTools.FieldRefAccess<ItemSettingsList, Dictionary<uint, ItemSettings>>("_dict");
        private static readonly Dictionary<uint, ItemSettings> Settings = new Dictionary<uint, ItemSettings>();
        private static readonly HashSet<ItemSettingsList> Merged = new HashSet<ItemSettingsList>();
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static GameObject _holder;

        /// <summary>Largest stack of any item (mod or game) a machine slot may hold.</summary>
        public static int MaxStack(uint id)
        {
            if (Settings.TryGetValue(id, out ItemSettings own))
                return own.maxStackSize;
            ItemSettings game = Find(id);
            return game != null ? Math.Max(1, (int)game.maxStackSize) : 20;
        }

        private static readonly Dictionary<uint, ItemSettings> GameCache = new Dictionary<uint, ItemSettings>();

        public static ItemSettings Find(uint id)
        {
            if (Settings.TryGetValue(id, out ItemSettings own))
                return own;
            if (GameCache.TryGetValue(id, out ItemSettings cached) && cached != null)
                return cached;
            ItemSettings found = Resources.FindObjectsOfTypeAll<ItemSettings>().FirstOrDefault(i => i != null && i.id == id);
            if (found != null)
                GameCache[id] = found;
            return found;
        }

        public static string NameOf(uint id) => Find(id)?.itemName ?? ResourceCatalog.Get(id)?.Name ?? ("objet " + id);

        /// <summary>Creates the item settings (at start, before any game list is read).</summary>
        public static void Create()
        {
            foreach (ResourceDef def in ResourceCatalog.All)
            {
                ItemSettings settings = ScriptableObject.CreateInstance<ItemSettings>();
                settings.name = "SI_Item" + def.Id;
                settings.hideFlags = HideFlags.DontUnloadUnusedAsset;
                settings.id = def.Id;
                settings.itemName = def.Name;
                settings.mass = def.Mass;
                settings.maxStackSize = (byte)Mathf.Clamp(def.MaxStack, 1, 255);
                settings.isFluid = def.Category == ResourceCategory.Fluid;
                Settings[def.Id] = settings;
            }
        }

        /// <summary>Adds the items to a game item list, once, and builds their models when the game's materials are there.</summary>
        public static void Merge(ItemSettingsList list)
        {
            if (list == null || Settings.Count == 0)
                return;
            BuildModels(list);
            if (Merged.Contains(list))
                return;
            ItemSettings[] items = Items(list);
            if (items == null)
                return;
            List<ItemSettings> merged = items.ToList();
            foreach (ItemSettings own in Settings.Values)
            {
                ItemSettings clash = merged.FirstOrDefault(i => i != null && i.id == own.id && i != own);
                if (clash != null)
                {
                    IndustriesMod.Log.Error($"items: id {own.id} is already used by '{clash.itemName}', {own.itemName} not added");
                    continue;
                }
                if (!merged.Contains(own))
                    merged.Add(own);
            }
            Items(list) = merged.ToArray();
            Dict(list) = null;
            Merged.Add(list);
            IndustriesMod.Log.Msg($"items: {Settings.Count} industrial items added ({merged.Count} in the game's list)");
        }

        private static void BuildModels(ItemSettingsList list)
        {
            if (Settings.Values.All(s => s.isFluid || s.itemObject != null))
                return;
            ItemSettings[] items = Items(list);
            if (items == null)
                return;
            if (_holder == null)
            {
                _holder = new GameObject("SI_ItemModels");
                _holder.SetActive(false);
                Object.DontDestroyOnLoad(_holder);
            }
            foreach (ResourceDef def in ResourceCatalog.All)
            {
                ItemSettings settings = Settings[def.Id];
                if (settings.isFluid || settings.itemObject != null)
                    continue;
                ItemSettings donor = items.FirstOrDefault(i => i != null && i.id == def.MaterialFrom);
                MeshRenderer donorRenderer = donor?.itemObject?.GetComponentInChildren<MeshRenderer>(true);
                Material material = donorRenderer?.sharedMaterial;
                Mesh donorMesh = donorRenderer?.GetComponent<MeshFilter>()?.sharedMesh;
                if (material == null)
                    continue;
                var root = new GameObject(settings.name);
                root.transform.SetParent(_holder.transform, false);
                var visual = new GameObject("Visuals");
                visual.transform.SetParent(root.transform, false);
                // The donor item's own area of its texture atlas
                var builder = new MeshBuilder { Panel = MeshBuilder.UvArea(donorMesh) };
                BuildShape(builder, def.Shape);
                // The game's object shader reads its palette color from UV1, like the donor's model
                visual.AddComponent<MeshFilter>().sharedMesh = builder.Build(settings.name, Parts.GameSkin.MostUsedUv1(donorMesh));
                var tinted = new Material(material) { name = settings.name };
                if (tinted.HasProperty(ColorId))
                    tinted.SetColor(ColorId, tinted.GetColor(ColorId) * new Color(def.Tint[0], def.Tint[1], def.Tint[2]));
                visual.AddComponent<MeshRenderer>().sharedMaterial = tinted;
                settings.itemObject = root;
            }
        }

        // ---- Models (meters, about the size of the game's ore items: 0.3 m)

        private static void BuildShape(MeshBuilder m, ResourceShape shape)
        {
            switch (shape)
            {
                case ResourceShape.Plate:
                    // Two stacked plates, the top one slightly turned
                    m.Box(new Vector3(0f, 0.015f, 0f), new Vector3(0.32f, 0.03f, 0.24f));
                    m.Box(new Vector3(0.01f, 0.045f, 0.005f), new Vector3(0.3f, 0.03f, 0.22f), Quaternion.Euler(0f, 8f, 0f));
                    break;
                case ResourceShape.Coil:
                    // A spool: two flanges and a thick winding
                    m.Cylinder(new Vector3(0f, 0f, 0f), new Vector3(0f, 0.025f, 0f), 0.14f, 0.14f, 14);
                    m.Cylinder(new Vector3(0f, 0.025f, 0f), new Vector3(0f, 0.175f, 0f), 0.11f, 0.11f, 14);
                    m.Cylinder(new Vector3(0f, 0.175f, 0f), new Vector3(0f, 0.2f, 0f), 0.14f, 0.14f, 14);
                    break;
                case ResourceShape.Rod:
                    m.Cylinder(new Vector3(-0.17f, 0.04f, 0f), new Vector3(0.17f, 0.04f, 0f), 0.04f, 0.04f, 8);
                    m.Cylinder(new Vector3(-0.17f, 0.04f, 0.09f), new Vector3(0.17f, 0.04f, 0.09f), 0.04f, 0.04f, 8);
                    break;
                default:
                    m.Box(new Vector3(0f, 0.08f, 0f), new Vector3(0.22f, 0.16f, 0.2f), Quaternion.Euler(10f, 25f, 5f));
                    break;
            }
        }
    }

    /// <summary>The game's item list gets the mod's items before any lookup.</summary>
    [HarmonyPatch(typeof(ItemSettingsList), nameof(ItemSettingsList.GetItemSettingsById))]
    internal static class IndustrialItemsListPatch
    {
        private static void Prefix(ItemSettingsList __instance)
        {
            try
            {
                IndustrialItems.Merge(__instance);
            }
            catch (Exception e)
            {
                ErrorLog.Report("items", e);
            }
        }
    }
}
