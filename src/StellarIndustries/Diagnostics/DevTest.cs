using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Core.Services;
using Items.Model;
using Managers.Client.Utils;
using Managers.Interface;
using MelonLoader;
using MelonLoader.Utils;
using Saving.Interface.Model;
using Saving.Services;
using Saving.Services.Converters;
using Saving.Services.Model.State;
using Ships;
using Ships.Cables;
using Ships.Interface.Model.Parts;
using Ships.Interface.Model.Parts.Common;
using Ships.Interface.Model.Parts.State;
using Ships.Interface.Settings;
using Ships.Services;
using StellarIndustries.Content;
using StellarIndustries.Core;
using StellarIndustries.Production;
using UnityEngine;
using UnityEngine.SceneManagement;
using WorldTracking.Interface.Values;
using WorldTracking.Services;
using WorldTracking.Spaces;
using Object = UnityEngine.Object;

namespace StellarIndustries.Diagnostics
{
    /// <summary>
    /// In-game acceptance test, entirely off unless DevWorld is set (use a COPY of a save: the
    /// test saves into it). From the main menu it loads that world in single player, then:
    ///  DevPhase 1: drops a test ship beside the player: a wood generator wired to workshop A
    ///    (iron and copper inside), workshop B without cable (iron), workshop C full (blocked).
    ///    Logs the machines every 2 s, photographs the ship, saves the game through the game's
    ///    own saver and quits.
    ///  DevPhase 2: reloads that save, logs every machine found (contents after reload), puts
    ///    iron in workshop A and checks it produces again, then quits.
    /// Lines start with "dev:" in MelonLoader/Latest.log.
    /// </summary>
    internal static class DevTest
    {
        private const string GameScene = "MainGame";
        private const ushort WoodGenerator = 60, IronFloor = 1;
        private const ushort GeneratorPart = 1, WorkshopA = 2, WorkshopB = 3, WorkshopC = 4;
        private const ushort Gen2 = 5, SmelterPart = 6, Gen3 = 7, CrusherPart = 8, Gen4 = 9, AssemblerPart = 10, Gen5 = 11, PressPart = 12, Gen6 = 13, ExtractorPart = 14, Gen7 = 15, WorkshopD = 16;

        private static MelonPreferences_Entry<string> _world;
        private static MelonPreferences_Entry<int> _phase;
        private static MelonPreferences_Entry<float> _quit;
        private static bool _loadRequested, _started;
        private static float _menuSince = -1f, _worldSince = -1f;

        public static string Folder => Path.Combine(MelonEnvironment.UserDataDirectory, "StellarIndustries", "dev");

        public static bool Active => _world != null && !string.IsNullOrWhiteSpace(_world.Value);

        /// <summary>Phase 2 keeps the machines still until the reloaded contents are checked.</summary>
        public static bool HoldProduction => Active && _phase.Value == 2 && !_checked;

        private static bool _checked;

        public static void Load(MelonPreferences_Category category)
        {
            _world = category.CreateEntry("DevWorld", "", description: "Development only: world file loaded for the in-game test (a copy, it is saved into). Empty = off.");
            _phase = category.CreateEntry("DevPhase", 1, description: "Development only: 1 = build, produce and save; 2 = reload and check.");
            _quit = category.CreateEntry("DevQuitSeconds", 90f, description: "Development only: quit the game this long after the test world loads.");
        }

        public static void OnSceneLoaded(string sceneName)
        {
            if (!Active)
                return;
            if (sceneName == GameScene)
            {
                _worldSince = Time.unscaledTime;
                _menuSince = -1f;
            }
            else if (!_loadRequested)
            {
                _menuSince = Time.unscaledTime;
            }
        }

        public static void Update()
        {
            if (!Active)
                return;
            float now = Time.unscaledTime;
            if (_menuSince >= 0f && !_loadRequested && now - _menuSince > 6f)
            {
                _loadRequested = true;
                string path = _world.Value.Trim();
                Log("loading " + path);
                MainGameSceneLoadData.Mode = MainGameSceneLoadData.GameMode.SinglePlayer;
                MainGameSceneLoadData.SaveFileFullName = path;
                SceneManager.LoadScene(GameScene, LoadSceneMode.Single);
                return;
            }
            if (_worldSince < 0f)
                return;
            float inWorld = now - _worldSince;
            if (!_started && inWorld > 25f)
            {
                _started = true;
                MelonCoroutines.Start(Guard(_phase.Value == 2 ? Verify() : Produce()));
            }
            if (_quit.Value > 0f && inWorld > _quit.Value)
            {
                Log("quitting");
                Application.Quit();
                _quit.Value = 0f;
            }
        }

        private static IEnumerator Guard(IEnumerator test)
        {
            while (true)
            {
                object current;
                try
                {
                    if (!test.MoveNext())
                        yield break;
                    current = test.Current;
                }
                catch (Exception e)
                {
                    IndustriesMod.Log.Error("dev: test failed: " + e);
                    yield break;
                }
                yield return current;
            }
        }

        // ---- Phase 1

        private static IEnumerator Produce()
        {
            var players = ServiceLocator.GetService<Players.Services.PlayersServerTracker>();
            var player = players?.ActivePlayers.FirstOrDefault();
            SpacesServer spaces = ServiceLocator.GetService<SpacesServer>();
            ShipsServerTracker ships = ServiceLocator.GetService<ShipsServerTracker>();
            CablesServerTracker cables = ServiceLocator.GetService<CablesServerTracker>();
            var ids = ServiceLocator.GetService<WorldObjectsIdsServerTracker>();
            if (player == null || spaces == null || ships == null || cables == null || ids == null)
            {
                Log("FAIL services missing");
                yield break;
            }
            int fromSocket = ItemSocket(MachineCatalog.Smelter), toSocket = ItemSocket(MachineCatalog.Workshop);
            _itemLine = fromSocket >= 0 && toSocket >= 0 && Logistics.ItemLinesBridge.Installed;
            Log(_itemLine ? $"item line: smelter socket {fromSocket} -> workshop D socket {toSocket}" : $"SKIP item line test (Item Lines installed: {Logistics.ItemLinesBridge.Installed}, sockets {fromSocket}/{toSocket})");
            var data = JsonUtility.FromJson<ShipImportExportUtils.ShipImportExportData>(ShipJson(_itemLine ? fromSocket : -1, toSocket));
            SavedShipState state = new ShipsConverter().ConvertToState(data.ships).States.Cast<SavedShipState>().Single();
            state.Id = ids.CreateUniqueIdForObject();
            state.SpaceId = player.SpaceId;
            Vector3 up = player.Rotation * Vector3.up;
            state.Position = player.Position + up * 1.5f + player.Rotation * Vector3.right * 8f;
            state.Rotation = player.Rotation;
            state.Velocity = Vector3.zero;
            uint physicsSpace = ClientOrigin.LocalPhysicsSpaceId;
            var transpose = ServiceLocator.GetService<WorldTracking.Interface.Services.IWorldTransposeServer>();
            if (physicsSpace != 0 && physicsSpace != state.SpaceId && transpose != null)
            {
                state.Position = transpose.Position(state.SpaceId, physicsSpace, state.Position);
                state.SpaceId = physicsSpace;
            }
            state.RelativeSpaceId = spaces.GetOrCreateShipRelativeSpace(state.Id);
            ships.LoadShip(state);
            foreach (var cable in CablesConverter.ConvertToState(data.cables))
            {
                CableState c = cable.State;
                CablePlugSocket a = c.PlugA, b = c.PlugB;
                var sa = a.EntityPartSocket; sa.EntityId = state.Id; a.EntityPartSocket = sa;
                var sb = b.EntityPartSocket; sb.EntityId = state.Id; b.EntityPartSocket = sb;
                c.PlugA = a;
                c.PlugB = b;
                cables.AddCable(c);
            }
            uint shipId = state.Id;
            Log($"test ship {shipId} loaded, {state.StatefulParts.Length} parts");
            Log("build menu: " + string.Join("; ", SDModKit.Game.CustomParts.BuildMenuReport.Where(r => r.StartsWith("SI_"))));
            var research = ServiceLocator.GetService<Research.ResearchClientProxy>();
            PartSettings workshop = SDModKit.Game.CustomParts.Get(MachineCatalog.Workshop);
            foreach (MachineDef m in MachineCatalog.All)
            {
                PartSettings settings = SDModKit.Game.CustomParts.Get(m.PartId);
                Log($"part {m.PartId} {(settings != null ? settings.fullLabel : "MISSING")}: unlocked {(research != null && settings != null ? research.IsPartUnlocked(settings).ToString() : "unknown")}, cost {(settings != null ? string.Join(", ", settings.BuildingCost.Select(c => c.Quantity + " x " + IndustrialItems.NameOf(c.ItemId))) : "-")}");
            }
            CheckResearch(research);
            Log($"workshop part settings {(workshop != null ? workshop.fullLabel : "missing")}, unlocked {(research != null && workshop != null ? research.IsPartUnlocked(workshop).ToString() : "unknown")}, cost {(workshop != null ? string.Join(", ", workshop.BuildingCost.Select(c => c.Quantity + " x " + c.ItemId)) : "-")}");
            yield return Wait(1f);
            TrackedShipServer ship = ships.GetShip(shipId);
            if (ship == null)
            {
                Log("FAIL test ship missing");
                yield break;
            }
            // Contents, through the game's own state updates
            ship.UpdatePartState(GeneratorPart, new WoodPowerGeneratorState { ItemSlot = Slot(GameItems.Wood, 20) });
            ship.UpdatePartState(WorkshopA, new StorageState { InventorySlots = Slots((GameItems.Iron, 9), (GameItems.Copper, 3)) });
            ship.UpdatePartState(WorkshopB, new StorageState { InventorySlots = Slots((GameItems.Iron, 6)) });
            var full = Enumerable.Repeat((GameItems.Iron, IndustrialItems.MaxStack(GameItems.Iron)), 10).ToArray();
            ship.UpdatePartState(WorkshopC, new StorageState { InventorySlots = Slots(full) });
            foreach (ushort generator in new[] { Gen2, Gen3, Gen4, Gen5, Gen6 })
                ship.UpdatePartState(generator, new WoodPowerGeneratorState { ItemSlot = Slot(GameItems.Wood, 20) });
            ship.UpdatePartState(SmelterPart, new StorageState { InventorySlots = Slots((GameItems.Iron, 6), (ResourceCatalog.StellarDust, 4)) });
            ship.UpdatePartState(CrusherPart, new StorageState { InventorySlots = Slots((GameItems.StellarShard, 2)) });
            ship.UpdatePartState(AssemblerPart, new StorageState { InventorySlots = Slots((ResourceCatalog.IronPlate, 4), (ResourceCatalog.Screws, 8)) });
            ship.UpdatePartState(PressPart, new StorageState { InventorySlots = Slots((ResourceCatalog.IronRod, 3), (ResourceCatalog.CopperWire, 4)) });
            // The press would make screws on its own (first recipe): it is set to cables only
            Log($"press set to cables: {MachineControl.SetChoice(shipId, PressPart, "press.cable")}");
            // Workshop D runs on the mod's combustion generator, fed with 2 logs
            ship.UpdatePartState(Gen7, new StorageState { InventorySlots = Slots((GameItems.Wood, 2)) });
            // Workshop D would make rods from single ingots as they arrive: it is set to plates
            Log($"workshop D set to plates: {MachineControl.SetChoice(shipId, WorkshopD, "workshop.iron_plate")}");
            UI.MachineHud.Forced = (shipId, PressPart);
            Log($"contents set (iron stack limit {IndustrialItems.MaxStack(GameItems.Iron)}): {Describe(ship)}");
            for (int i = 1; i <= 16; i++)
            {
                yield return Wait(2f);
                ship = ships.GetShip(shipId);
                if (ship == null)
                {
                    Log("FAIL test ship gone");
                    yield break;
                }
                Log($"t={2 * i} s: {Describe(ship)}");
                if (i == 1)
                    PlaceDevRock(ship);
                if (i == 3)
                    yield return Photograph(shipId, "machines");
                if (i == 4)
                {
                    Directory.CreateDirectory(Folder);
                    ScreenCapture.CaptureScreenshot(Path.Combine(Folder, "hud_press.png"));
                    Log("hud screenshot requested");
                }
            }
            UI.MachineHud.Forced = null;
            Check(ship, WorkshopA, (GameItems.Iron, 1), (ResourceCatalog.IronPlate, 4), (ResourceCatalog.CopperWire, 3));
            if (_itemLine)
            {
                // The item line carried the ingots (not the iron, not the glass) to workshop D
                Check(ship, SmelterPart, (GameItems.Glass, 2));
                Check(ship, WorkshopD, (ResourceCatalog.IronPlate, 4));
            }
            else
            {
                Check(ship, SmelterPart, (ResourceCatalog.IronIngot, 6), (GameItems.Glass, 2));
            }
            Check(ship, CrusherPart, (ResourceCatalog.StellarDust, 6), (GameItems.Glass, 2));
            Check(ship, AssemblerPart, (ResourceCatalog.ReinforcedPlate, 1));
            Check(ship, PressPart, (ResourceCatalog.IronRod, 3), (ResourceCatalog.Cable, 2));
            CheckExtractor(ship);
            MachineSimulation.Runtime gen = MachineSimulation.Get(ship.Id, Gen7);
            Log($"{(gen != null && (gen.Status == MachineStatus.Producing || gen.Status == MachineStatus.Standby) && gen.Burned > 0f ? "PASS" : "FAIL")} combustion generator: {gen?.Status}, {gen?.Burned:0} J drawn from the log being burnt, {gen?.Detail}");
            if (_devRock != null)
                Object.Destroy(_devRock);
            Check(ship, WorkshopB, (GameItems.Iron, 6));
            Check(ship, WorkshopC, full);
            // A last change just before saving, so the save holds a mid-production state
            ship.UpdatePartState(WorkshopA, new StorageState { InventorySlots = Slots((ResourceCatalog.IronPlate, 4), (ResourceCatalog.CopperWire, 3), (GameItems.Copper, 2)) });
            yield return Wait(0.5f);
            Log($"before save: {Describe(ship)}");
            GameSaver saver = Object.FindObjectsByType<GameSaver>(FindObjectsSortMode.None).FirstOrDefault();
            if (saver == null)
            {
                Log("FAIL no game saver");
                yield break;
            }
            saver.SaveGame(new SaveGameParams { IsAutomatic = false });
            Log($"saved ship {shipId} to {MainGameSceneLoadData.SaveFileFullName}");
            Directory.CreateDirectory(Folder);
            File.WriteAllText(Path.Combine(Folder, "saved.txt"), Contents(ship));
        }

        // ---- Phase 2

        private static IEnumerator Verify()
        {
            ShipsServerTracker ships = ServiceLocator.GetService<ShipsServerTracker>();
            if (ships == null)
            {
                Log("FAIL services missing");
                yield break;
            }
            string saved = File.Exists(Path.Combine(Folder, "saved.txt")) ? File.ReadAllText(Path.Combine(Folder, "saved.txt")) : "(none)";
            TrackedShipServer test = null;
            foreach (TrackedShipServer ship in ships.LoadedShipsWithChildren)
            {
                if (!ship.GetAllStatefulParts<StorageState>().Any(p => MachineCatalog.TryGet(p.Settings.id, out _)))
                    continue;
                Log($"after reload, ship {ship.Id}: {Describe(ship)}");
                test = ship;
            }
            if (test == null)
            {
                _checked = true;
                Log("FAIL no machine found after reload (is the test ship loaded? saved: " + saved + ")");
                yield break;
            }
            Log("saved was: " + saved);
            string now = Contents(test);
            Log(now == saved ? "PASS contents identical after reload: " + now : "FAIL contents differ after reload: " + now);
            _checked = true;
            // The press's recipe choice came back from the companion file
            string choice = MachineControl.Choices.Get(test.Id, PressPart);
            Log(choice == "press.cable" ? "PASS press recipe choice kept after reload: " + choice : "FAIL press recipe choice after reload: " + (choice ?? "automatic"));
            // Production starts again after the reload
            test.UpdatePartState(WorkshopA, new StorageState { InventorySlots = Slots((GameItems.Iron, 4)) });
            test.UpdatePartState(PressPart, new StorageState { InventorySlots = Slots((ResourceCatalog.IronRod, 2), (ResourceCatalog.CopperWire, 2)) });
            for (int i = 1; i <= 6; i++)
            {
                yield return Wait(2f);
                Log($"reloaded t={2 * i} s: {Describe(test)}");
            }
            Check(test, WorkshopA, (ResourceCatalog.IronPlate, 2));
            Check(test, PressPart, (ResourceCatalog.IronRod, 2), (ResourceCatalog.Cable, 1));
        }

        // ---- Research

        /// <summary>The branch is in the game's list; researching its first tech unlocks its machines and only those.</summary>
        private static void CheckResearch(Research.ResearchClientProxy research)
        {
            Log("research techs: " + string.Join(", ", Parts.IndustryResearch.Techs.Select(t => $"{t.id} {t.techName} at {t.positionInTechTree} requires [{string.Join(",", t.requirements.Select(r => r.id))}]")));
            Log("research tree: " + Parts.IndustryResearch.Map());
            var state = Object.FindObjectsByType<Research.ResearchNetworkedState>(FindObjectsSortMode.None).FirstOrDefault();
            PartSettings workshop = SDModKit.Game.CustomParts.Get(MachineCatalog.Workshop), press = SDModKit.Game.CustomParts.Get(MachineCatalog.Press);
            if (research == null || state == null || workshop == null || press == null)
            {
                Log("FAIL research services missing");
                return;
            }
            bool before = research.IsPartUnlocked(workshop);
            Log($"before: tech 7801 researched {state.IsResearched(7801)}, workshop unlocked {before}, press unlocked {research.IsPartUnlocked(press)}");
            state.ServerMarkResearchAsDone(7801);
            bool workshopAfter = research.IsPartUnlocked(workshop), pressAfter = research.IsPartUnlocked(press);
            bool ok = Parts.IndustryResearch.Techs.Count == Parts.IndustryResearch.All.Length && (before == false || state.IsResearched(7801)) && workshopAfter && !pressAfter;
            Log($"{(ok ? "PASS" : "FAIL")} research: {Parts.IndustryResearch.Techs.Count} techs in the tree; after Industrie de base: workshop unlocked {workshopAfter}, press unlocked {pressAfter} (needs Transformation)");
        }

        // ---- Conveyors

        private static bool _itemLine;

        /// <summary>The item port Item Lines grafted on a machine (SDModKit item cable, type 3), or -1.</summary>
        private static int ItemSocket(ushort partId)
        {
            PartSettings settings = SDModKit.Game.CustomParts.Get(partId);
            if (settings?.part == null)
                return -1;
            foreach (SocketObject socket in settings.part.GetComponentsInChildren<SocketObject>(true))
            {
                if ((int)socket.Type == 3)
                    return socket.Id;
            }
            return -1;
        }

        // ---- Extraction

        private static GameObject _devRock;
        private static string _devRockItem;

        /// <summary>
        /// The scanner's report, then: with no rock in reach the extractor must stay idle; a test
        /// rock (a game MinablePlanetObject giving iron, on this PC only) is then set 3 m from it.
        /// </summary>
        private static void PlaceDevRock(TrackedShipServer ship)
        {
            Log("scanner: " + UI.DepositScanner.Scan().Replace("\n", " | "));
            var real = Object.FindObjectsByType<Planets.Objects.MinablePlanetObject>(FindObjectsSortMode.None);
            Log($"{real.Length} minable rock(s) loaded: " + string.Join(", ", real.GroupBy(r => r.dropRate.item != null ? r.dropRate.item.itemName : "?").Select(g => g.Count() + " " + g.Key)));
            ship.TryGetStatefulPart(ExtractorPart, out StatefulPart part);
            MachineSimulation.Runtime rt = MachineSimulation.Get(ship.Id, ExtractorPart);
            Log($"extractor before the test rock: {rt?.Status.ToString() ?? "-"} ({rt?.Detail ?? "-"})");
            if (rt != null && rt.Status == MachineStatus.NoDeposit)
                Log("PASS extractor without a rock in reach: no deposit, nothing produced");
            Vector3? at = part != null ? Deposits.WorldPosition(ship.Id, part) : null;
            if (at == null)
            {
                Log("FAIL extractor position unknown");
                return;
            }
            _devRock = new GameObject("SI_DevRock");
            _devRock.transform.position = at.Value + new Vector3(0f, 0f, 3f);
            var minable = _devRock.AddComponent<Planets.Objects.MinablePlanetObject>();
            minable.dropRate = new Planets.Objects.Common.Model.ItemDropRate { item = IndustrialItems.Find(GameItems.Iron), amountMin = 1, amountMax = 2 };
            _devRockItem = minable.dropRate.item != null ? minable.dropRate.item.itemName : "?";
            Deposits.Rescan();
            Log($"test rock ({_devRockItem}) set 3 m from the extractor at {at.Value}");
        }

        private static void CheckExtractor(TrackedShipServer ship)
        {
            StatefulPart part = ship.GetAllStatefulParts<StorageState>().FirstOrDefault(p => p.Id == ExtractorPart);
            if (part == null)
            {
                Log("FAIL extractor missing");
                return;
            }
            var slots = new Production.Stack[10];
            MachineSimulation.Read((StorageState)part.State, slots);
            int iron = Crafting.CountOf(slots, GameItems.Iron);
            int all = slots.Sum(x => x.Count);
            MachineSimulation.Runtime rt = MachineSimulation.Get(ship.Id, ExtractorPart);
            // About 28 s with the rock at 1 item per 6 s
            bool ok = iron >= 3 && iron <= 6 && all == iron;
            Log($"{(ok ? "PASS" : "FAIL")} extractor: {iron} iron extracted ({all} items), {rt?.Status} {rt?.Detail}");
        }

        // ---- Helpers

        private static void Check(TrackedShipServer ship, ushort partId, params (uint Item, int Count)[] expected)
        {
            StatefulPart part = ship.GetAllStatefulParts<StorageState>().FirstOrDefault(p => p.Id == partId);
            if (part == null)
            {
                Log($"FAIL part {partId} missing");
                return;
            }
            var slots = new Production.Stack[10];
            MachineSimulation.Read((StorageState)part.State, slots);
            var have = slots.Where(s => !s.IsEmpty).GroupBy(s => s.Item).ToDictionary(g => g.Key, g => g.Sum(s => s.Count));
            var want = expected.GroupBy(e => e.Item).ToDictionary(g => g.Key, g => g.Sum(e => e.Count));
            bool ok = have.Count == want.Count && want.All(w => have.TryGetValue(w.Key, out int n) && n == w.Value);
            Log($"{(ok ? "PASS" : "FAIL")} part {partId}: has {Text(have)}, expected {Text(want)}");
        }

        private static string Text(Dictionary<uint, int> counts) => counts.Count == 0 ? "nothing" : string.Join(", ", counts.Select(c => c.Value + " " + IndustrialItems.NameOf(c.Key)));

        /// <summary>The machines' slots only (what the save must keep).</summary>
        private static string Contents(TrackedShipServer ship)
        {
            var text = new StringBuilder();
            foreach (StatefulPart part in ship.GetAllStatefulParts<StorageState>().OrderBy(p => p.Id))
            {
                if (!MachineCatalog.TryGet(part.Settings.id, out _))
                    continue;
                var slots = new Production.Stack[10];
                MachineSimulation.Read((StorageState)part.State, slots);
                text.Append('[').Append(part.Id).Append(':');
                foreach (Production.Stack s in slots)
                    text.Append(' ').Append(s.IsEmpty ? "." : s.Count + "x" + s.Item);
                text.Append("] ");
            }
            return text.ToString().TrimEnd();
        }

        private static string Describe(TrackedShipServer ship)
        {
            var text = new StringBuilder();
            foreach (StatefulPart part in ship.GetAllStatefulParts<StorageState>().OrderBy(p => p.Id))
            {
                if (!MachineCatalog.TryGet(part.Settings.id, out _))
                    continue;
                var slots = new Production.Stack[10];
                MachineSimulation.Read((StorageState)part.State, slots);
                MachineSimulation.Runtime rt = MachineSimulation.Get(ship.Id, part.Id);
                text.Append($"[{part.Id} {rt?.Status.ToString() ?? "-"} {rt?.Recipe?.Id ?? "-"} {(rt != null ? (rt.Progress * 100f).ToString("0", CultureInfo.InvariantCulture) : "-")}% {(rt != null ? (rt.Watts * rt.PowerRate).ToString("0", CultureInfo.InvariantCulture) : "-")}W:");
                foreach (Production.Stack s in slots)
                    text.Append(' ').Append(s.IsEmpty ? "." : s.Count + "x" + s.Item);
                text.Append("] ");
            }
            StatefulPart generator = ship.GetAllStatefulParts<WoodPowerGeneratorState>().FirstOrDefault();
            if (generator != null)
                text.Append($"generator wood {((WoodPowerGeneratorState)generator.State).GetWoodAvailable().ToString("0.00", CultureInfo.InvariantCulture)}");
            return text.ToString();
        }

        private static InventorySlot Slot(uint item, int count) => new InventorySlot { HasItem = true, ItemId = item, Quantity = (byte)count };

        private static InventorySlot[] Slots(params (uint Item, int Count)[] stacks)
        {
            var slots = new InventorySlot[10];
            for (int i = 0; i < stacks.Length && i < 10; i++)
                slots[i] = Slot(stacks[i].Item, stacks[i].Count);
            return slots;
        }

        private static IEnumerator Wait(float seconds)
        {
            float until = Time.unscaledTime + seconds;
            while (Time.unscaledTime < until)
                yield return null;
        }

        private static void Log(string message) => IndustriesMod.Log.Msg("dev: " + message);

        /// <summary>
        /// An 11 x 7 iron floor: row 1 the generator wired to workshop A, workshop B (no cable) and C
        /// (full); rows 2 and 3 a generator wired to each of the smelter, crusher, assembler and press.
        /// </summary>
        private static string ShipJson(int itemFrom, int itemTo)
        {
            var hull = new StringBuilder();
            for (int x = -5; x <= 5; x++)
            {
                for (int z = -3; z <= 5; z++)
                {
                    if (hull.Length > 0)
                        hull.Append(',');
                    hull.Append($"{{\"partId\":{IronFloor},\"x\":{x},\"y\":0,\"z\":{z},\"forwardOrientationId\":4,\"upOrientationId\":0,\"voxelPartTypeId\":0,\"FrontColor\":{{\"x\":150,\"y\":150,\"z\":150}},\"BackColor\":{{\"x\":120,\"y\":120,\"z\":120}}}}");
                }
            }
            string Part(int id, int settings, float x, float z) => string.Format(CultureInfo.InvariantCulture,
                "{{\"id\":{0},\"settingsId\":{1},\"coord\":{{\"x\":{2},\"y\":-0.46,\"z\":{3}}},\"forwardOrientationId\":2,\"upOrientationId\":4}}", id, settings, x, z);
            string parts = string.Join(",",
                Part(GeneratorPart, WoodGenerator, -4f, -2.2f), Part(WorkshopA, MachineCatalog.Workshop, -2f, -2.2f),
                Part(WorkshopB, MachineCatalog.Workshop, 0.5f, -2.2f), Part(WorkshopC, MachineCatalog.Workshop, 3f, -2.2f),
                Part(Gen2, WoodGenerator, -4f, 0f), Part(SmelterPart, MachineCatalog.Smelter, -2f, 0f),
                Part(Gen3, WoodGenerator, 0.5f, 0f), Part(CrusherPart, MachineCatalog.Crusher, 2.5f, 0f),
                Part(Gen4, WoodGenerator, -4f, 2.2f), Part(AssemblerPart, MachineCatalog.Assembler, -2f, 2.2f),
                Part(Gen5, WoodGenerator, 0.5f, 2.2f), Part(PressPart, MachineCatalog.Press, 2.5f, 2.2f),
                Part(Gen6, WoodGenerator, 4.5f, 2.2f), Part(ExtractorPart, MachineCatalog.Extractor, 4.3f, 0f),
                Part(Gen7, MachineCatalog.CombustionGenerator, -4f, 4.4f), Part(WorkshopD, MachineCatalog.Workshop, -2f, 4.4f));
            // Power cables (type 2): each generator's socket 0 -> its machine's socket 0
            string Cable(int id, ushort from, ushort to) =>
                $"{{\"id\":{id},\"type\":2,\"plugA\":{{\"shipId\":1,\"partId\":{from},\"socketId\":0}},\"plugB\":{{\"shipId\":1,\"partId\":{to},\"socketId\":{MachineSimulation.PowerSocket}}}}}";
            string cables = string.Join(",", Cable(1, GeneratorPart, WorkshopA), Cable(2, Gen2, SmelterPart), Cable(3, Gen3, CrusherPart),
                Cable(4, Gen4, AssemblerPart), Cable(5, Gen5, PressPart), Cable(6, Gen6, ExtractorPart), Cable(7, Gen7, WorkshopD));
            // Item line (SDModKit item cable, type 3): smelter -> workshop D
            if (itemFrom >= 0)
                cables += $",{{\"id\":8,\"type\":3,\"plugA\":{{\"shipId\":1,\"partId\":{SmelterPart},\"socketId\":{itemFrom}}},\"plugB\":{{\"shipId\":1,\"partId\":{WorkshopD},\"socketId\":{itemTo}}}}}";
            var sb = new StringBuilder();
            sb.Append("{\"rootShipId\":1,\"cables\":[").Append(cables).Append("],\"ships\":[{");
            sb.Append("\"id\":1,\"relativeSpaceId\":0,\"spaceId\":0,\"position\":{\"x\":0,\"y\":0,\"z\":0},\"velocity\":{\"x\":0,\"y\":0,\"z\":0},\"rotation\":{\"x\":0,\"y\":0,\"z\":0,\"w\":1},");
            sb.Append("\"rooms\":[],\"hullParts\":[").Append(hull).Append("],\"statefulFloatingParts\":[").Append(parts).Append("],\"statefulHullParts\":[],");
            foreach (string empty in new[] { "thrustersStates", "storageStates", "fluidTankStates", "constantStates", "beaconStates", "leverStates", "warpDriveStates",
                         "dockingDoorStates", "targeterStates", "mathGateStates", "unaryMathGateStates", "memoryRegisterStates", "transmitterStates", "rotorStates",
                         "electrolyzerStates", "woodThrusterStates", "woodGeneratorStates", "ethanolDistillerStates" })
                sb.Append('"').Append(empty).Append("\":[],");
            sb.Length--;
            sb.Append("}]}");
            return sb.ToString();
        }

        /// <summary>Photographs a ship's model on this client from four angles, to dev/{prefix}_*.png.</summary>
        private static IEnumerator Photograph(uint shipId, string prefix)
        {
            GameObject visual = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)
                .Where(t => t.parent == null && t.name.StartsWith("ship " + shipId + " ") && t.name.Contains("visual")).Select(t => t.gameObject).FirstOrDefault();
            if (visual == null)
            {
                Log("no visual for the test ship, no photo");
                yield break;
            }
            var bounds = new Bounds(visual.transform.position, Vector3.zero);
            foreach (Renderer r in visual.GetComponentsInChildren<Renderer>())
                bounds.Encapsulate(r.bounds);
            Transform frame = visual.transform;
            var tree = new StringBuilder();
            foreach (Renderer r in visual.GetComponentsInChildren<Renderer>())
            {
                string path = r.name;
                for (Transform t = r.transform.parent; t != null && t != frame; t = t.parent)
                    path = t.name + "/" + path;
                tree.AppendLine($"{path} mat={r.sharedMaterial?.name} at {frame.InverseTransformPoint(r.bounds.center):F2} size {r.bounds.size:F2}");
            }
            Directory.CreateDirectory(Folder);
            File.WriteAllText(Path.Combine(Folder, prefix + "_tree.txt"), tree.ToString());
            var cameraObject = new GameObject("SI_DevCamera");
            var camera = cameraObject.AddComponent<Camera>();
            if (Camera.main != null)
                camera.CopyFrom(Camera.main);
            camera.enabled = false;
            camera.fieldOfView = 40f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 2000f;
            var target = RenderTexture.GetTemporary(1600, 900, 24, RenderTextureFormat.ARGB32);
            camera.targetTexture = target;
            var sheet = new Texture2D(1600, 900, TextureFormat.RGBA32, false);
            float reach = bounds.extents.magnitude;
            Directory.CreateDirectory(Folder);
            (string Name, Vector3 Direction, float Distance)[] views =
            {
                ("front", new Vector3(0.15f, 0.45f, 1f), 1.3f),
                ("back", new Vector3(-0.2f, 0.4f, -1f), 1.3f),
                ("side", new Vector3(1f, 0.35f, 0.1f), 1.2f),
                ("top", new Vector3(0.05f, 1f, -0.25f), 1.3f),
            };
            foreach ((string name, Vector3 direction, float distance) in views)
            {
                Vector3 world = frame.TransformDirection(direction.normalized);
                camera.transform.position = bounds.center + world * reach * distance;
                camera.transform.rotation = Quaternion.LookRotation(bounds.center - camera.transform.position, frame.up);
                camera.Render();
                RenderTexture previous = RenderTexture.active;
                RenderTexture.active = target;
                sheet.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0);
                sheet.Apply();
                RenderTexture.active = previous;
                File.WriteAllBytes(Path.Combine(Folder, prefix + "_" + name + ".png"), sheet.EncodeToPNG());
                yield return null;
            }
            camera.targetTexture = null;
            RenderTexture.ReleaseTemporary(target);
            Object.Destroy(sheet);
            Object.Destroy(cameraObject);
            Log("photos written to " + Folder);
        }
    }
}
