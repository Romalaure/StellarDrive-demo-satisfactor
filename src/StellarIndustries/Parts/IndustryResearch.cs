using System.Collections.Generic;
using System.Linq;
using Items.Model;
using Research.Interface.Values;
using SDModKit.Game;
using Ships.Interface.Settings;
using StellarIndustries.Content;
using StellarIndustries.Core;
using StellarIndustries.Production;
using UnityEngine;

namespace StellarIndustries.Parts
{
    /// <summary>
    /// The "Industrie" branch of the research table: the machines are unlocked by techs (ids
    /// 7801 and up) branched on the game's Power Generation, paid at the research table like the
    /// game's own techs, the later ones in the mod's own products so the chain is climbed step by
    /// step. The techs are added to the game's research list (ResearchSettings) and placed in free
    /// cells of the tree so no line crosses a box (same layout as TF Advanced). Off with
    /// ResearchUnlocks = false: every machine is then available from the start.
    /// </summary>
    internal static class IndustryResearch
    {
        internal sealed class Tech
        {
            public ushort Id;
            public string Name;
            public ushort[] Requires;
            public Vector2Int Position;
            public ushort[] Parts;
            /// <summary>Recipes it unlocks (their <see cref="StellarIndustries.Recipes.RecipeDef.Tech"/> is this tech).</summary>
            public string[] Recipes = new string[0];
            public (uint Item, int Count)[] Cost;
        }

        /// <summary>Power Generation, the game's tech that gives the wood generator.</summary>
        private const ushort PowerGeneration = 2;

        public static readonly Tech[] All =
        {
            new Tech { Id = 7801, Name = "Industrie de base", Requires = new[] { PowerGeneration }, Position = new Vector2Int(2, 2),
                Parts = new[] { MachineCatalog.Workshop, MachineCatalog.Smelter, MachineCatalog.Extractor },
                Cost = new[] { (GameItems.Iron, 20), (GameItems.Copper, 10) } },
            new Tech { Id = 7802, Name = "Transformation", Requires = new ushort[] { 7801 }, Position = new Vector2Int(3, 2),
                Parts = new[] { MachineCatalog.Press, MachineCatalog.Crusher },
                Cost = new[] { (ResourceCatalog.IronPlate, 20), (ResourceCatalog.IronRod, 10), (ResourceCatalog.CopperWire, 10) } },
            new Tech { Id = 7803, Name = "Assemblage", Requires = new ushort[] { 7802 }, Position = new Vector2Int(4, 2),
                Parts = new[] { MachineCatalog.Assembler },
                Cost = new[] { (ResourceCatalog.Screws, 40), (ResourceCatalog.Cable, 10), (ResourceCatalog.IronPlate, 10) } },
            new Tech { Id = 7804, Name = "Énergie industrielle", Requires = new ushort[] { 7801 }, Position = new Vector2Int(4, 3),
                Parts = new[] { MachineCatalog.CombustionGenerator },
                Cost = new[] { (ResourceCatalog.BiomassBrick, 10), (ResourceCatalog.Cable, 6), (ResourceCatalog.IronPlate, 10) } },
            new Tech { Id = 7805, Name = "Logistique industrielle", Requires = new ushort[] { 7803 }, Position = new Vector2Int(5, 2),
                Parts = new[] { MachineCatalog.IndustrialContainer, MachineCatalog.LoadingDock },
                Cost = new[] { (ResourceCatalog.ReinforcedPlate, 4), (ResourceCatalog.Cable, 10), (ResourceCatalog.Screws, 30) } },
            new Tech { Id = 7806, Name = "Fabrication avancée", Requires = new ushort[] { 7803 }, Position = new Vector2Int(5, 3),
                Parts = new[] { MachineCatalog.Fabricator },
                Cost = new[] { (ResourceCatalog.Rotor, 4), (ResourceCatalog.Stator, 4), (ResourceCatalog.AssemblyFrame, 2) } },
            new Tech { Id = 7807, Name = "Métallurgie et chimie", Requires = new ushort[] { 7802 }, Position = new Vector2Int(5, 4),
                Parts = new[] { MachineCatalog.Foundry, MachineCatalog.Refinery },
                Cost = new[] { (ResourceCatalog.SteelBeam, 10), (ResourceCatalog.Concrete, 20), (ResourceCatalog.ReinforcedPlate, 6) } },
            new Tech { Id = 7808, Name = "Stockage d'énergie", Requires = new ushort[] { 7807 }, Position = new Vector2Int(7, 3),
                Parts = new[] { MachineCatalog.Accumulator },
                Cost = new[] { (ResourceCatalog.AlloySheet, 20), (ResourceCatalog.AluminumCasing, 10), (ResourceCatalog.Rubber, 10) } },
            new Tech { Id = 7809, Name = "Physique des particules", Requires = new ushort[] { 7806, 7807 }, Position = new Vector2Int(7, 4),
                Parts = new[] { MachineCatalog.Accelerator },
                Cost = new[] { (ResourceCatalog.Supercomputer, 2), (ResourceCatalog.CoolingSystem, 2), (ResourceCatalog.QuartzCrystal, 20) } },
            new Tech { Id = 7810, Name = "Bioénergie", Requires = new ushort[] { 7801 }, Position = new Vector2Int(4, 6),
                Parts = new[] { MachineCatalog.BiomassBurner },
                Cost = new[] { (GameItems.Iron, 15), (GameItems.Wood, 20) } },
            new Tech { Id = 7811, Name = "Condensation", Requires = new ushort[] { 7803 }, Position = new Vector2Int(6, 6),
                Parts = new[] { MachineCatalog.WaterCondenser },
                Cost = new[] { (ResourceCatalog.CopperSheet, 10), (ResourceCatalog.Rotor, 2) } },
            new Tech { Id = 7812, Name = "Recyclage", Requires = new ushort[] { 7805 }, Position = new Vector2Int(7, 1),
                Parts = new[] { MachineCatalog.Recycler, MachineCatalog.ExchangeCounter },
                Cost = new[] { (ResourceCatalog.ReinforcedPlate, 6), (ResourceCatalog.CircuitBoard, 4) } },
            new Tech { Id = 7813, Name = "Cartographie industrielle", Requires = new ushort[] { 7802 }, Position = new Vector2Int(5, 5),
                Parts = new[] { MachineCatalog.RadarTower },
                Cost = new[] { (ResourceCatalog.SteelBeam, 6), (ResourceCatalog.Cable, 10) } },
            new Tech { Id = 7814, Name = "Physique quantique", Requires = new ushort[] { 7809 }, Position = new Vector2Int(8, 3),
                Parts = new[] { MachineCatalog.QuantumEncoder, MachineCatalog.Converter },
                Cost = new[] { (ResourceCatalog.QuantumProcessor, 2), (ResourceCatalog.StellarMatter, 4) } },
            new Tech { Id = 7815, Name = "Énergie stellaire", Requires = new ushort[] { 7814 }, Position = new Vector2Int(9, 3),
                Parts = new[] { MachineCatalog.StellarReactor },
                Cost = new[] { (ResourceCatalog.TimeCrystal, 2), (ResourceCatalog.ControlUnit, 2), (ResourceCatalog.HeavyFrame, 4) } },
            // ---- 0.13: conveyors, power lines, prospecting
            new Tech { Id = 7816, Name = "Convoyeurs", Requires = new ushort[] { 7801 }, Position = new Vector2Int(3, 1),
                Parts = new[] { MachineCatalog.BeltMk1, MachineCatalog.Splitter, MachineCatalog.Merger },
                Cost = new[] { (ResourceCatalog.IronPlate, 10), (ResourceCatalog.IronRod, 10) } },
            new Tech { Id = 7817, Name = "Convoyeurs rapides", Requires = new ushort[] { 7816, 7803 }, Position = new Vector2Int(5, 1),
                Parts = new[] { MachineCatalog.BeltMk2, MachineCatalog.ConveyorLift },
                Cost = new[] { (ResourceCatalog.ReinforcedPlate, 4), (ResourceCatalog.Rotor, 2), (ResourceCatalog.Screws, 40) } },
            new Tech { Id = 7818, Name = "Convoyeurs lourds", Requires = new ushort[] { 7817, 7807 }, Position = new Vector2Int(7, 0),
                Parts = new[] { MachineCatalog.BeltMk3 },
                Cost = new[] { (ResourceCatalog.SteelBeam, 10), (ResourceCatalog.Rubber, 10), (ResourceCatalog.AlloySheet, 5) } },
            new Tech { Id = 7819, Name = "Lignes électriques", Requires = new ushort[] { 7801 }, Position = new Vector2Int(3, 3),
                Parts = new[] { PowerPoles.PoleMk1 },
                Cost = new[] { (ResourceCatalog.IronRod, 10), (ResourceCatalog.CopperWire, 10) } },
            new Tech { Id = 7820, Name = "Haute tension", Requires = new ushort[] { 7819, 7804 }, Position = new Vector2Int(5, 3),
                Parts = new[] { PowerPoles.PoleMk2, PowerPoles.PoleMk3 },
                Cost = new[] { (ResourceCatalog.Cable, 20), (ResourceCatalog.SteelBeam, 4) } },
            new Tech { Id = StellarIndustries.Recipes.RecipeBook.ScanTech1, Name = "Prospection", Requires = new ushort[] { 7801 }, Position = new Vector2Int(3, 5),
                Parts = new ushort[0], Recipes = new[] { "workshop.scanner1" },
                Cost = new[] { (ResourceCatalog.IronPlate, 6), (ResourceCatalog.CopperWire, 10) } },
            new Tech { Id = StellarIndustries.Recipes.RecipeBook.ScanTech2, Name = "Prospection avancée", Requires = new ushort[] { StellarIndustries.Recipes.RecipeBook.ScanTech1, 7803 }, Position = new Vector2Int(5, 5),
                Parts = new ushort[0], Recipes = new[] { "assembler.scanner2", "assembler.range_module" },
                Cost = new[] { (ResourceCatalog.CircuitBoard, 4), (ResourceCatalog.Cable, 10) } },
            new Tech { Id = StellarIndustries.Recipes.RecipeBook.ScanTech3, Name = "Analyse géologique", Requires = new ushort[] { StellarIndustries.Recipes.RecipeBook.ScanTech2, 7806 }, Position = new Vector2Int(7, 5),
                Parts = new ushort[0], Recipes = new[] { "fabricator.scanner3", "fabricator.analysis_module" },
                Cost = new[] { (ResourceCatalog.Computer, 2), (ResourceCatalog.QuartzCrystal, 10) } },
        };

        /// <summary>
        /// Whether one of the mod's techs is researched (everything is when the research branch is
        /// off or the tech could not be added). Read from the game's research state, shared by all players.
        /// </summary>
        public static bool IsDone(ushort techId)
        {
            if (!Settings.ResearchUnlocks.Value || !Added.Any(t => t.id == techId))
                return true;
            if (_proxy == null)
                _proxy = global::Core.Services.ServiceLocator.GetService<Research.ResearchClientProxy>();
            return _proxy != null && _proxy.IsResearched(techId);
        }

        private static Research.ResearchClientProxy _proxy;

        /// <summary>A new world: the research state is looked up again.</summary>
        public static void Forget() => _proxy = null;

        private static readonly HashSet<ushort> Locked = new HashSet<ushort>();
        private static readonly List<ResearchSettings> Added = new List<ResearchSettings>();
        private static ResearchSettingsList _list;
        private static int _placedCount;
        private static float _nextCheck, _nextEnsure;
        private static readonly HashSet<ushort> _placedOnce = new HashSet<ushort>();

        public static IReadOnlyList<ResearchSettings> Techs => Added;

        /// <summary>The game language changed: the techs are renamed in place.</summary>
        public static void Rename()
        {
            foreach (ResearchSettings tech in Added)
            {
                Tech def = All.FirstOrDefault(t => t.Id == tech.id);
                if (def != null)
                    tech.techName = Lang.TechName(def.Id, def.Name);
            }
        }

        /// <summary>The visible tree as text, one line per row (development log).</summary>
        public static string Map()
        {
            if (_list == null)
                return "(no tree)";
            var lines = new List<string>();
            for (int y = MaxRow; y >= 0; y--)
            {
                var cells = new List<string>();
                for (int x = 0; x <= MaxColumn; x++)
                {
                    ResearchSettings r = _list.list.FirstOrDefault(t => Shown(t) && t.positionInTechTree == new Vector2Int(x, y));
                    cells.Add(r == null ? "....." : r.id.ToString().PadLeft(5));
                }
                lines.Add($"y{y}: " + string.Join(" ", cells));
            }
            return string.Join(" | ", lines);
        }

        /// <summary>Whether the part waits for one of the mod's techs (otherwise it is always available).</summary>
        public static bool IsResearchPart(ushort partId) => Settings.ResearchUnlocks.Value && Locked.Contains(partId);

        /// <summary>Adds the techs to the game's research list once its parts exist (on scene load; <see cref="Tick"/> retries).</summary>
        public static void Ensure()
        {
            if (!Settings.ResearchUnlocks.Value)
                return;
            ResearchSettingsList list = Resources.FindObjectsOfTypeAll<ResearchSettingsList>()
                .FirstOrDefault(l => l != null && l.list != null && l.list.Any(r => r != null && r.id == 12));
            if (list == null)
                return;
            if (Added.Count > 0 && Added.All(t => list.list.Contains(t)))
                return;
            CustomParts.EnsureAll();
            // SDModKit lays out its content packs' techs (ids above 99): ours are placed here
            SDModKit.Content.ResearchLayout.Exclude(list.list.Where(r => r != null && r.id > 99 && !SDModKit.Content.ContentResearch.All.Contains(r))
                .Select(r => r.id).Concat(All.Select(t => t.Id)));
            list.list.RemoveAll(r => r != null && Added.Contains(r));
            Added.Clear();
            Locked.Clear();
            foreach (Tech tech in All)
            {
                PartSettings[] parts = tech.Parts.Select(id => CustomParts.Get(id)).Where(p => p != null).ToArray();
                if (parts.Length == 0 && tech.Recipes.Length == 0)
                    continue;
                if (list.list.Any(r => r != null && r.id == tech.Id))
                {
                    IndustriesMod.Log.Error($"research: tech id {tech.Id} already used by another mod, {tech.Name} not added (its machines stay available)");
                    continue;
                }
                var settings = ScriptableObject.CreateInstance<ResearchSettings>();
                settings.name = "SI_Tech" + tech.Id;
                settings.hideFlags = HideFlags.DontUnloadUnusedAsset;
                settings.id = tech.Id;
                settings.techName = Lang.TechName(tech.Id, tech.Name);
                settings.unlockedParts = parts;
                settings.unlockedFeatures = new Research.Interface.Model.ResearchUnlockableFeature[0];
                settings.unlockedGuideEntriesIds = new uint[0];
                settings.requirements = tech.Requires
                    .Select(id => list.list.FirstOrDefault(r => r != null && r.id == id) ?? Added.FirstOrDefault(r => r.id == id))
                    .Where(r => r != null).ToArray();
                settings.resourceCost = tech.Cost.Select(c => Item(c.Item, c.Count)).Where(i => i != null).ToArray();
                settings.positionInTechTree = tech.Position;
                settings.availableInDemo = true;
                settings.hideFromTechTree = false;
                list.list.Add(settings);
                Added.Add(settings);
                foreach (PartSettings part in parts)
                    Locked.Add(part.id);
            }
            Place(list);
            _list = list;
            _placedCount = list.list.Count;
            IndustriesMod.Log.Msg("research: " + string.Join(", ", Added.Select(t =>
                $"{t.techName} {t.positionInTechTree} ({t.unlockedParts.Length} part(s), cost {string.Join(" + ", t.resourceCost.Select(c => c.Quantity + " " + IndustrialItems.NameOf(c.ItemId)))})")));
        }

        // ---- Layout: the tree's cells are 500 x 260 apart, boxes 410 x 170, lines from a box's right edge to the next one's left edge

        private const float CellWidth = 500f, CellHeight = 260f, HalfWidth = 205f, HalfHeight = 85f;
        // The game's research canvas widens to its furthest column (ResearchCanvas: 770 + x * 500),
        // rows are fixed: the tree grows to the right when other mods fill it
        private const int MaxColumn = 13, MaxRow = 6;

        private static bool Shown(ResearchSettings r) => r != null && r.availableInDemo && !r.hideFromTechTree;
        private static Vector2 Center(Vector2Int c) => new Vector2(c.x * CellWidth, c.y * CellHeight);
        private static Vector2 Out(Vector2Int c) => Center(c) + new Vector2(HalfWidth, 0f);
        private static Vector2 In(Vector2Int c) => Center(c) - new Vector2(HalfWidth, 0f);
        private static Rect Box(Vector2Int c) => new Rect(Center(c).x - HalfWidth - 10f, Center(c).y - HalfHeight - 10f, HalfWidth * 2f + 20f, HalfHeight * 2f + 20f);

        private static bool Crosses(Vector2 a, Vector2 b, Rect box)
        {
            for (int i = 1; i < 64; i++)
            {
                if (box.Contains(Vector2.Lerp(a, b, i / 64f)))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Puts each of the mod's techs in a free cell right of its requirements, as close as possible
        /// to its wished cell, where neither its lines cross a box nor a line crosses it. The game's
        /// techs never move.
        /// </summary>
        private static void Place(ResearchSettingsList list)
        {
            var shown = list.list.Where(Shown).ToList();
            foreach (ResearchSettings tech in Added)
            {
                Vector2Int wish = All.FirstOrDefault(t => t.Id == tech.id)?.Position ?? tech.positionInTechTree;
                int minX = tech.requirements.Where(Shown).Select(r => r.positionInTechTree.x + 1).DefaultIfEmpty(0).Max();
                // Room is kept on the right for the techs that follow it (a chain must stay left to right)
                int maxX = Mathf.Max(minX, MaxColumn - DepthBelow(tech.id));
                // Stay put when the current cell still fits: placing again after another mod added
                // techs must not shuffle the branch
                Vector2Int here = tech.positionInTechTree;
                if (_placedOnce.Contains(tech.id) && here.x >= minX && here.x <= maxX && !shown.Any(o => o != tech && o.positionInTechTree == here))
                    continue;
                _placedOnce.Add(tech.id);
                Vector2Int? best = null;
                float bestScore = float.MaxValue;
                for (int x = minX; x <= MaxColumn; x++)
                {
                    for (int y = 0; y <= MaxRow; y++)
                    {
                        var cell = new Vector2Int(x, y);
                        if (shown.Any(o => o != tech && o.positionInTechTree == cell))
                            continue;
                        // The tree is shared with other mods and often full: a crossing line is only cosmetic,
                        // a tech far from its parents is harder to read, so nearness weighs as much
                        int crossings = Crossings(tech, cell, shown);
                        float score = (x > maxX ? 5000f : 0f) + crossings * 30f + (x - minX) * 12f + Mathf.Abs(y - wish.y) * 4f + Mathf.Abs(y - ParentRow(tech)) * 6f;
                        if (score < bestScore)
                        {
                            bestScore = score;
                            best = cell;
                        }
                    }
                }
                if (best.HasValue)
                    tech.positionInTechTree = best.Value;
                if (best == null)
                    IndustriesMod.Log.Warning($"research: no free cell right of {tech.techName}'s requirements, left at {tech.positionInTechTree}");
            }
        }

        /// <summary>How many of the mod's techs follow this one at most (columns needed on its right).</summary>
        private static int DepthBelow(ushort id)
        {
            int depth = 0;
            foreach (Tech child in All.Where(t => t.Requires.Contains(id)))
                depth = Mathf.Max(depth, 1 + DepthBelow(child.Id));
            return depth;
        }

        private static int ParentRow(ResearchSettings tech) => tech.requirements.Where(Shown).Select(r => r.positionInTechTree.y).DefaultIfEmpty(tech.positionInTechTree.y).First();

        private static int Crossings(ResearchSettings tech, Vector2Int cell, List<ResearchSettings> shown)
        {
            int count = 0;
            // Its own lines, from each requirement, through any other box
            foreach (ResearchSettings req in tech.requirements.Where(Shown))
            {
                foreach (ResearchSettings other in shown)
                {
                    if (other != tech && other != req && Crosses(Out(req.positionInTechTree), In(cell), Box(other.positionInTechTree)))
                        count++;
                }
            }
            // Other techs' lines through its box
            Rect box = Box(cell);
            foreach (ResearchSettings other in shown)
            {
                if (other == tech || other.requirements == null)
                    continue;
                foreach (ResearchSettings req in other.requirements.Where(Shown))
                {
                    if (req != tech && Crosses(Out(req.positionInTechTree), In(other.positionInTechTree), box))
                        count++;
                }
            }
            return count;
        }

        /// <summary>Retries adding the techs; other mods add techs later (when a world loads): ours move out of their way.</summary>
        public static void Tick()
        {
            if (Added.Count == 0 && Settings.ResearchUnlocks.Value && Time.unscaledTime >= _nextEnsure)
            {
                _nextEnsure = Time.unscaledTime + 2f;
                Ensure();
            }
            if (_list == null || Time.unscaledTime < _nextCheck)
                return;
            _nextCheck = Time.unscaledTime + 2f;
            if (_list.list.Count == _placedCount)
                return;
            SDModKit.Content.ResearchLayout.Exclude(_list.list.Where(r => r != null && r.id > 99 && !SDModKit.Content.ContentResearch.All.Contains(r)).Select(r => r.id));
            Place(_list);
            _placedCount = _list.list.Count;
            IndustriesMod.Log.Msg("research: placed again: " + string.Join(", ", Added.Select(t => $"{t.techName} {t.positionInTechTree}")));
        }

        private static ItemInstance Item(uint id, int count)
        {
            ItemSettings settings = IndustrialItems.Find(id);
            return settings != null ? new ItemInstance(settings, count) : null;
        }
    }
}

namespace StellarIndustries.Parts
{
    /// <summary>
    /// The research table's card of a tech lists the parts it unlocks; the mod's prospecting techs
    /// unlock recipes (the scanners and their modules): those are listed too, one line per recipe
    /// with its item's icon and the machine that makes it.
    /// </summary>
    [HarmonyLib.HarmonyPatch(typeof(global::UI.Research.ResearchInfo), nameof(global::UI.Research.ResearchInfo.Show))]
    internal static class ResearchRecipesPatch
    {
        private static readonly HarmonyLib.AccessTools.FieldRef<global::UI.Research.ResearchInfo, global::UI.Research.TechUnlockItem> Prefab =
            HarmonyLib.AccessTools.FieldRefAccess<global::UI.Research.ResearchInfo, global::UI.Research.TechUnlockItem>("techUnlockItemPrefab");
        private static readonly HarmonyLib.AccessTools.FieldRef<global::UI.Research.ResearchInfo, Transform> Content =
            HarmonyLib.AccessTools.FieldRefAccess<global::UI.Research.ResearchInfo, Transform>("contentTransform");
        private static readonly HarmonyLib.AccessTools.FieldRef<global::UI.Research.ResearchInfo, Transform> UnlockLabel =
            HarmonyLib.AccessTools.FieldRefAccess<global::UI.Research.ResearchInfo, Transform>("unlockLabel");
        private static readonly HarmonyLib.AccessTools.FieldRef<global::UI.Research.TechUnlockItem, UnityEngine.UI.RawImage> Thumbnail =
            HarmonyLib.AccessTools.FieldRefAccess<global::UI.Research.TechUnlockItem, UnityEngine.UI.RawImage>("thumbnail");
        private static readonly HarmonyLib.AccessTools.FieldRef<global::UI.Research.TechUnlockItem, UnityEngine.UI.RawImage> Shadow =
            HarmonyLib.AccessTools.FieldRefAccess<global::UI.Research.TechUnlockItem, UnityEngine.UI.RawImage>("thumbnailShadow");
        private static readonly HarmonyLib.AccessTools.FieldRef<global::UI.Research.TechUnlockItem, GameObject> Container =
            HarmonyLib.AccessTools.FieldRefAccess<global::UI.Research.TechUnlockItem, GameObject>("thumbnailContainer");
        private static readonly HarmonyLib.AccessTools.FieldRef<global::UI.Research.TechUnlockItem, TMPro.TMP_Text> Label =
            HarmonyLib.AccessTools.FieldRefAccess<global::UI.Research.TechUnlockItem, TMPro.TMP_Text>("label");
        private static readonly HarmonyLib.AccessTools.FieldRef<global::UI.Research.TechUnlockItem, UnityEngine.UI.Image> Background =
            HarmonyLib.AccessTools.FieldRefAccess<global::UI.Research.TechUnlockItem, UnityEngine.UI.Image>("background");

        private static void Postfix(global::UI.Research.ResearchInfo __instance, ResearchSettings researchSettings)
        {
            try
            {
                IndustryResearch.Tech tech = researchSettings != null ? IndustryResearch.All.FirstOrDefault(t => t.Id == researchSettings.id) : null;
                if (tech == null || tech.Recipes.Length == 0)
                    return;
                var thumbnails = global::Core.Services.ServiceLocator.GetService<Items.Interface.Service.IItemThumbnailProvider>();
                int index = UnlockLabel(__instance).GetSiblingIndex() + researchSettings.unlockedParts.Length;
                foreach (string id in tech.Recipes)
                {
                    StellarIndustries.Recipes.RecipeDef recipe = StellarIndustries.Recipes.RecipeBook.Get(id);
                    if (recipe == null)
                        continue;
                    uint item = recipe.Outputs[0].Item;
                    global::UI.Research.TechUnlockItem row = Object.Instantiate(Prefab(__instance), Content(__instance));
                    row.transform.SetSiblingIndex(++index);
                    Container(row).SetActive(true);
                    Texture2D icon = thumbnails?.GetThumbnail(item);
                    Thumbnail(row).texture = icon;
                    Shadow(row).texture = icon;
                    string machine = MachineCatalog.TryGet(recipe.Machine, out MachineDef def) ? Lang.MachineName(def) : "?";
                    Label(row).text = Lang.T("Recette : ", "Recipe: ") + IndustrialItems.NameOf(item) + " (" + machine + ")";
                    Color c = Background(row).color;
                    c.a = 0f;
                    Background(row).color = c;
                }
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(Content(__instance) as RectTransform);
            }
            catch (System.Exception e)
            {
                ErrorLog.Report("research card", e);
            }
        }
    }
}
