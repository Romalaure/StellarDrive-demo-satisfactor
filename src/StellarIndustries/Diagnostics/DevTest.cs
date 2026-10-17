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
    internal static partial class DevTest
    {
        private const string GameScene = "MainGame";
        private const ushort WoodGenerator = 60, IronFloor = 1;
        /// <summary>The game's storage chest (the donor of the machines).</summary>
        private static ushort Chest => SDModKit.Game.PartLookup.ByAssetName("StorageChest")?.id ?? 0;
        private const ushort GeneratorPart = 1, WorkshopA = 2, WorkshopB = 3, WorkshopC = 4;
        private const ushort Gen2 = 5, SmelterPart = 6, Gen3 = 7, CrusherPart = 8, Gen4 = 9, AssemblerPart = 10, Gen5 = 11, PressPart = 12, Gen6 = 13, ExtractorPart = 14, Gen7 = 15, WorkshopD = 16, DockA = 17, ContainerPart = 18, CargoDock = 1;
        private const ushort Gen8 = 19, AccumulatorPart = 20, Gen9 = 21, FoundryPart = 22, Gen10 = 23, RefineryPart = 24, FabricatorPart = 25, AcceleratorPart = 26;
        private const ushort Gen11 = 27, CondenserPart = 28, RecyclerPart = 29, RadarPart = 30, ReactorPart = 31;
        // 0.13: a chest emptied by two belts into a splitter that fills two containers; a generator powering workshop E through a pole
        private const ushort SourceChest = 32, Belt1 = 33, Belt2 = 34, SplitterPart = 35, OutA = 36, OutB = 37, Gen12 = 38, PolePart = 39, WorkshopE = 40;
        /// <summary>Which way a part's front faces on the test ship (read from the placement the game makes).</summary>
        private static Vector3 _forward = Vector3.forward, _right = Vector3.right;

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
                MelonCoroutines.Start(Guard(Ready(_phase.Value == 5 ? Factory() : _phase.Value == 4 ? Showcase() : _phase.Value == 3 ? LoadTest() : _phase.Value == 2 ? Verify() : Produce())));
            }
            if (_quit.Value > 0f && inWorld > _quit.Value)
            {
                Log("quitting");
                Application.Quit();
                _quit.Value = 0f;
            }
        }

        /// <summary>Waits (90 s at most) until the player is in the world, then runs the test.</summary>
        private static IEnumerator Ready(IEnumerator test)
        {
            for (int i = 0; i < 180; i++)
            {
                var players = ServiceLocator.GetService<Players.Services.PlayersServerTracker>();
                if (players != null && players.ActivePlayers.Count > 0 && ServiceLocator.GetService<ShipsServerTracker>() != null)
                    break;
                yield return Wait(0.5f);
            }
            var tracker = ServiceLocator.GetService<Players.Services.PlayersServerTracker>();
            if (tracker == null || tracker.ActivePlayers.Count == 0)
            {
                Directory.CreateDirectory(Folder);
                ScreenCapture.CaptureScreenshot(Path.Combine(Folder, "not_ready.png"));
                Log($"world not ready after 90 s: players service {tracker != null}, scene {SceneManager.GetActiveScene().name}, cursor {Cursor.visible}");
                yield return Wait(1f);
            }
            while (test.MoveNext())
                yield return test.Current;
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
            var probe = JsonUtility.FromJson<ShipImportExportUtils.ShipImportExportData>(ShipJson(-1, toSocket));
            StatefulPart probePart = new ShipsConverter().ConvertToState(probe.ships).States.Cast<SavedShipState>().Single().StatefulParts.First(p => p.Id == GeneratorPart);
            _forward = Axis(probePart.LocalRotation * Vector3.forward);
            _right = Axis(probePart.LocalRotation * Vector3.right);
            Log($"parts face {_forward} (right {_right}) on the test ship");
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
            // A small cargo ship beside it: its receiving dock 3 m in front of dock A
            var cargoData = JsonUtility.FromJson<ShipImportExportUtils.ShipImportExportData>(CargoJson());
            SavedShipState cargo = new ShipsConverter().ConvertToState(cargoData.ships).States.Cast<SavedShipState>().Single();
            cargo.Id = ids.CreateUniqueIdForObject();
            // Its dock 3 m right of dock A, off the deck, from both ships' own part frames (the test ship's origin moves as its deck grows)
            Vector3 dockA = state.StatefulParts.First(p => p.Id == DockA).LocalPosition;
            Vector3 cargoDock = cargo.StatefulParts.First(p => p.Id == CargoDock).LocalPosition;
            cargo.SpaceId = state.SpaceId;
            cargo.Rotation = state.Rotation;
            cargo.Position = state.Position + state.Rotation * (dockA + new Vector3(0f, 0f, 3f) - cargoDock);
            cargo.Velocity = Vector3.zero;
            Log($"dock A local {dockA}, cargo dock local {cargoDock}, test ship at {state.Position}, cargo at {cargo.Position}");
            cargo.RelativeSpaceId = spaces.GetOrCreateShipRelativeSpace(cargo.Id);
            ships.LoadShip(cargo);
            _cargoId = cargo.Id;
            Log($"cargo ship {cargo.Id} loaded");
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
            foreach (ushort generator in new[] { Gen2, Gen3, Gen4, Gen5, Gen6, Gen8, Gen9, Gen10, Gen11, Gen12 })
                ship.UpdatePartState(generator, new WoodPowerGeneratorState { ItemSlot = Slot(GameItems.Wood, 20) });
            ship.UpdatePartState(SmelterPart, new StorageState { InventorySlots = Slots((GameItems.Iron, 6), (ResourceCatalog.StellarDust, 4)) });
            ship.UpdatePartState(CrusherPart, new StorageState { InventorySlots = Slots((GameItems.StellarShard, 2)) });
            ship.UpdatePartState(AssemblerPart, new StorageState { InventorySlots = Slots((ResourceCatalog.IronPlate, 4), (ResourceCatalog.Screws, 8)) });
            ship.UpdatePartState(PressPart, new StorageState { InventorySlots = Slots((ResourceCatalog.IronRod, 3), (ResourceCatalog.CopperWire, 4)) });
            // The press would make screws on its own (first recipe): it is set to cables only
            Log($"press set to cables: {MachineControl.SetChoice(shipId, PressPart, "press.cable")}");
            // 0.10 machines: the foundry makes steel with charcoal, the refinery is set to plastic
            ship.UpdatePartState(FoundryPart, new StorageState { InventorySlots = Slots((GameItems.Iron, 6), (ResourceCatalog.Charcoal, 2)) });
            ship.UpdatePartState(RefineryPart, new StorageState { InventorySlots = Slots((ResourceCatalog.Biomass, 12), (GameItems.Ice, 2)) });
            Log($"refinery set to plastic: {MachineControl.SetChoice(shipId, RefineryPart, "refinery.plastic")}");
            // 0.12 machines: the reactor powers the recycler, which turns 100 iron into a voucher
            ship.UpdatePartState(ReactorPart, new StorageState { InventorySlots = Slots((ResourceCatalog.StellarFuelRod, 2)) });
            ship.UpdatePartState(RecyclerPart, new StorageState { InventorySlots = Slots((GameItems.Iron, 100)) });
            // 0.13: 12 iron to carry, a log-fired generator and 3 iron for workshop E behind the pole
            ship.UpdatePartState(SourceChest, new StorageState { InventorySlots = Slots((GameItems.Iron, 12)) });
            ship.UpdatePartState(Gen12, new WoodPowerGeneratorState { ItemSlot = Slot(GameItems.Wood, 20) });
            ship.UpdatePartState(WorkshopE, new StorageState { InventorySlots = Slots((GameItems.Iron, 3)) });
            // Dock A sends to the cargo ship's dock
            ship.UpdatePartState(DockA, new StorageState { InventorySlots = Slots((GameItems.Iron, 12), (GameItems.Copper, 5)) });
            ship.UpdatePartState(ContainerPart, new StorageState { InventorySlots = Slots((ResourceCatalog.Screws, 50)) });
            Log($"dock A send: {MachineControl.SetChoice(shipId, DockA, Logistics.LoadingDocks.Send)}, cargo dock receive: {MachineControl.SetChoice(_cargoId, CargoDock, Logistics.LoadingDocks.Receive)}");
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
                if (i == 1 || i == 8)
                {
                    TrackedShipServer cargoNow = ships.GetShip(_cargoId);
                    Vector3? a = ship.TryGetStatefulPart(DockA, out StatefulPart da) ? Deposits.WorldPosition(shipId, da) : null;
                    Vector3? b = cargoNow != null && cargoNow.TryGetStatefulPart(CargoDock, out StatefulPart db) ? Deposits.WorldPosition(_cargoId, db) : null;
                    Log($"docks {(a != null && b != null ? Vector3.Distance(a.Value, b.Value).ToString("0.00") + " m apart: " + a.Value + " " + b.Value : "not both shown")}");
                }
                if (i == 1)
                    PlaceDevRock(ship);
                if (i == 3)
                {
                    yield return Photograph(shipId, "machines");
                    yield return CheckAnimations(shipId);
                }
                if (i == 5)
                    yield return PhotographMachines(shipId);
                if (i == 8)
                    yield return PhotographItems(shipId);
                if (i == 10)
                {
                    UI.MachineHud.Forced = null;
                    yield return CheckWiki();
                }
                if (i == 13)
                {
                    FactoryOverview.Shown = true;
                    yield return Wait(1.5f);
                    Directory.CreateDirectory(Folder);
                    ScreenCapture.CaptureScreenshot(Path.Combine(Folder, "overview.png"));
                    yield return Wait(0.5f);
                    FactoryOverview.Shown = false;
                    Log("overview screenshot requested: " + FactoryOverview.Build());
                }
                if (i == 6)
                {
                    Log("overview: " + Strip(FactoryOverview.Build()));
                    Log($"machine hum volume {UI.MachineSound.CurrentVolume:0.00} (setting {Settings.MachineVolume.Value:0.00})");
                }
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
            Check(ship, CrusherPart, (ResourceCatalog.StellarDust, 6), (ResourceCatalog.Silica, 2));
            Check(ship, AssemblerPart, (ResourceCatalog.ReinforcedPlate, 1));
            Check(ship, PressPart, (ResourceCatalog.IronRod, 3), (ResourceCatalog.Cable, 2));
            CheckExtractor(ship);
            Check(ship, FoundryPart, (ResourceCatalog.SteelIngot, 6));
            Check(ship, RefineryPart, (ResourceCatalog.Plastic, 4), (ResourceCatalog.Resin, 2));
            MachineSimulation.Runtime acc = MachineSimulation.Get(ship.Id, AccumulatorPart);
            Log($"{(acc != null && acc.Stored > 10000f ? "PASS" : "FAIL")} accumulator charged from the generator's spare power: {acc?.Stored:0} J, {acc?.Status}, {acc?.Detail}");
            Check(ship, DockA);
            Check(ship, ContainerPart, (ResourceCatalog.Screws, 50));
            TrackedShipServer cargoShip = ships.GetShip(_cargoId);
            Log($"dock A: {MachineSimulation.Get(shipId, DockA)?.Detail}");
            if (cargoShip != null)
                Check(cargoShip, CargoDock, (GameItems.Iron, 12), (GameItems.Copper, 5));
            else
                Log("FAIL cargo ship missing");
            MachineSimulation.Runtime gen = MachineSimulation.Get(ship.Id, Gen7);
            Log($"{(gen != null && (gen.Status == MachineStatus.Producing || gen.Status == MachineStatus.Standby) && gen.Burned > 0f ? "PASS" : "FAIL")} combustion generator: {gen?.Status}, {gen?.Burned:0} J drawn from the log being burnt, {gen?.Detail}");
            Check(ship, RecyclerPart, (ResourceCatalog.Voucher, 1));
            MachineSimulation.Runtime reactor = MachineSimulation.Get(ship.Id, ReactorPart);
            Log($"{(reactor != null && reactor.Burned > 0f ? "PASS" : "FAIL")} stellar reactor: {reactor?.Status}, {reactor?.Burned:0} J drawn from the rod being burnt, {reactor?.Detail}");
            int ice = Crafting.CountOf(Stacks(ship, CondenserPart), GameItems.Ice);
            Log($"{(ice >= 3 ? "PASS" : "FAIL")} water condenser: {ice} ice made in about 32 s (one every 8 s), {MachineSimulation.Get(ship.Id, CondenserPart)?.Status}");
            MachineSimulation.Runtime radar = MachineSimulation.Get(ship.Id, RadarPart);
            Log($"{(radar != null && radar.Status == MachineStatus.Producing ? "PASS" : "FAIL")} radar tower: {radar?.Status}, {radar?.Detail}");
            CheckConveyors(ship);
            Check(ship, WorkshopE, (ResourceCatalog.IronPlate, 1), (GameItems.Iron, 1));
            Log($"workshop E behind the pole: {MachineSimulation.Get(ship.Id, WorkshopE)?.Status}");
            if (_devRock != null)
                Object.Destroy(_devRock);
            yield return CheckClientReports(ship);
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
                // The main test ship is the one with the most machines (the cargo ship has one dock)
                if (test == null || ship.GetAllStatefulParts<StorageState>().Count() > test.GetAllStatefulParts<StorageState>().Count())
                    test = ship;
            }
            if (test == null)
            {
                _checked = true;
                Log("FAIL no machine found after reload (is the test ship loaded? saved: " + saved + ")");
                yield break;
            }
            LogReach(test);
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
            MachineSimulation.Runtime acc = MachineSimulation.Get(test.Id, AccumulatorPart);
            Log($"{(acc != null && acc.Stored >= 100000f ? "PASS" : "FAIL")} accumulator charge after reload: {acc?.Stored:0} J ({acc?.Detail})");
        }

        /// <summary>Development: where the extractor and the real rocks are seen from, with the player aboard or not.</summary>
        private static void LogReach(TrackedShipServer ship)
        {
            var player = SDModKit.Game.GameRefs.PlayersClient?.LocalPlayer;
            Camera camera = Camera.main;
            Vector3? at = ship.TryGetStatefulPart(ExtractorPart, out StatefulPart part) ? Deposits.WorldPosition(ship.Id, part) : null;
            Log($"reach: player space {player?.SpaceId}, ship {ship.Id} space {ship.SpaceId}, ship visual {ShipVisual(ship.Id)?.position}, extractor {at}, camera {camera?.transform.position}");
            if (at == null)
                return;
            List<Deposits.Rock> all = Deposits.Loaded();
            foreach (Deposits.Rock r in all.Where(r => r.Shape != null).OrderByDescending(r => r.Shape.Value.size.magnitude).Take(4))
                Log($"reach: big rock {r.Name} ({r.Object.name}) colliders {r.Shape.Value.size}, at {Vector3.Distance(r.Position, at.Value):0.0} m from the extractor");
            Log($"reach: {all.Count(r => r.Shape == null)}/{all.Count} rock(s) without collider");
            foreach (Planets.Objects.MinablePlanetObject m in Object.FindObjectsByType<Planets.Objects.MinablePlanetObject>(FindObjectsSortMode.None))
            {
                Bounds b = new Bounds(m.transform.position, Vector3.zero);
                foreach (Collider c in m.GetComponentsInChildren<Collider>())
                    b.Encapsulate(c.bounds);
                if (b.size.magnitude > 2.5f)
                    Log($"reach: large minable {m.name} item {m.dropRate.item?.itemName} colliders {b.size} {string.Join(",", m.GetComponentsInChildren<Collider>().Select(c => c.GetType().Name + (c.isTrigger ? "(trigger)" : "")))}");
            }
            var big = new List<string>();
            foreach (Renderer r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {
                if (r.bounds.size.magnitude < 4f || r.bounds.size.magnitude > 80f || Vector3.Distance(r.bounds.center, camera != null ? camera.transform.position : at.Value) > 600f)
                    continue;
                string n = r.name.ToLowerInvariant();
                if (!(n.Contains("crystal") || n.Contains("cristal") || n.Contains("rock") || n.Contains("ice") || n.Contains("ore")))
                    continue;
                var minable = r.GetComponentInParent<Planets.Objects.MinablePlanetObject>();
                string path = r.name;
                for (Transform t = r.transform.parent; t != null && path.Length < 160; t = t.parent)
                    path = t.name + "/" + path;
                big.Add($"{path} size {r.bounds.size.magnitude:0.0} minable {(minable != null ? minable.dropRate.item?.itemName : "no")}");
            }
            Log($"reach: {big.Count} big rock/crystal renderer(s): " + string.Join(" ; ", big.Distinct().Take(25)));
            foreach (Deposits.Rock r in all.OrderBy(r => Vector3.Distance(r.Position, at.Value)).Take(3))
            {
                MeshFilter visual = Parts.DepositDecor.VisualOf(r);
                Log($"reach: rock {r.Name} physics {r.Position} visual {(visual != null ? visual.transform.position.ToString() : "none")}, extractor to rock {Vector3.Distance(r.Position, at.Value):0.0} m, camera to rock {(camera != null ? Vector3.Distance(r.Position, camera.transform.position).ToString("0.0") : "-")} m");
            }
        }

        // ---- Looks and animations

        private static Transform ShipVisual(uint shipId)
        {
            var client = SDModKit.Game.GameRefs.Service<ShipsClientTracker>();
            return client != null && client.TryGetShipVisualTransform(shipId, out Transform t) ? t : null;
        }

        /// <summary>Moving pieces of working machines must move: positions and rotations compared 0.5 s apart.</summary>
        private static IEnumerator CheckAnimations(uint shipId)
        {
            Transform ship = ShipVisual(shipId);
            if (ship == null)
            {
                Log("FAIL animation: no ship visual");
                yield break;
            }
            var pieces = ship.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith(Parts.MachineVisuals.AnimPrefix)).ToList();
            var before = pieces.Select(t => (t.localPosition, t.localRotation)).ToList();
            int visuals = ship.GetComponentsInChildren<Parts.MachineVisuals>(true).Length;
            yield return Wait(0.5f);
            int moved = 0;
            var kinds = new Dictionary<string, int>();
            for (int i = 0; i < pieces.Count; i++)
            {
                if (pieces[i] == null)
                    continue;
                bool changed = (pieces[i].localPosition - before[i].localPosition).sqrMagnitude > 1e-8f || Quaternion.Angle(pieces[i].localRotation, before[i].localRotation) > 0.01f;
                if (!changed)
                    continue;
                moved++;
                string kind = pieces[i].name.Split(' ').FirstOrDefault(x => x.StartsWith("kind=")) ?? "?";
                kinds.TryGetValue(kind, out int n);
                kinds[kind] = n + 1;
            }
            int greens = ship.GetComponentsInChildren<Transform>(true).Count(t => t.name == Parts.MachineVisuals.StatusOn && t.gameObject.activeSelf);
            int reds = ship.GetComponentsInChildren<Transform>(true).Count(t => t.name == Parts.MachineVisuals.StatusOff && t.gameObject.activeSelf);
            Log($"{(moved > 0 && greens > 0 ? "PASS" : "FAIL")} animation: {visuals} machine visuals, {moved}/{pieces.Count} moving pieces moved in 0.5 s ({string.Join(", ", kinds.Select(k => k.Value + " " + k.Key))}); status lights {greens} green, {reds} red");
        }

        /// <summary>Close-ups of every machine of the test ship, front and back, in two sheets.</summary>
        private static IEnumerator PhotographMachines(uint shipId)
        {
            Transform ship = ShipVisual(shipId);
            ShipsServerTracker ships = ServiceLocator.GetService<ShipsServerTracker>();
            TrackedShipServer server = ships?.GetShip(shipId);
            if (ship == null || server == null)
                yield break;
            ushort[] ids = { WorkshopA, SmelterPart, PressPart, CrusherPart, AssemblerPart, ExtractorPart, Gen7, DockA, ContainerPart, FabricatorPart, FoundryPart, RefineryPart, AccumulatorPart, AcceleratorPart };
            const int cell = 500, cols = 5;
            int rows = (ids.Length + cols - 1) / cols;
            var cameraObject = new GameObject("SI_DevCamera");
            var camera = cameraObject.AddComponent<Camera>();
            if (Camera.main != null)
                camera.CopyFrom(Camera.main);
            camera.enabled = false;
            camera.fieldOfView = 45f;
            camera.nearClipPlane = 0.05f;
            var target = RenderTexture.GetTemporary(cell, cell, 24, RenderTextureFormat.ARGB32);
            camera.targetTexture = target;
            var shot = new Texture2D(cell, cell, TextureFormat.RGBA32, false);
            foreach ((string side, Vector3 dir) in new[] { ("front", new Vector3(0.45f, 0.5f, 1f)), ("back", new Vector3(-0.35f, 0.45f, -1f)) })
            {
                var sheet = new Texture2D(cell * cols, cell * rows, TextureFormat.RGBA32, false);
                for (int i = 0; i < ids.Length; i++)
                {
                    if (!server.TryGetStatefulPart(ids[i], out StatefulPart part))
                        continue;
                    MachineCatalog.TryGet(part.Settings.id, out MachineDef def);
                    Vector3 center = ship.TransformPoint(part.LocalPosition + part.LocalRotation * new Vector3(0f, (def?.Height ?? 1.5f) * 0.45f, 0f));
                    Vector3 view = ship.rotation * (part.LocalRotation * dir.normalized);
                    camera.transform.position = center + view * 2.9f;
                    camera.transform.rotation = Quaternion.LookRotation(center - camera.transform.position, ship.up);
                    camera.Render();
                    RenderTexture previous = RenderTexture.active;
                    RenderTexture.active = target;
                    shot.ReadPixels(new Rect(0, 0, cell, cell), 0, 0);
                    shot.Apply();
                    RenderTexture.active = previous;
                    sheet.SetPixels((i % cols) * cell, (rows - 1 - i / cols) * cell, cell, cell, shot.GetPixels());
                    yield return null;
                }
                sheet.Apply();
                Directory.CreateDirectory(Folder);
                File.WriteAllBytes(Path.Combine(Folder, $"closeup_{side}.png"), sheet.EncodeToPNG());
                Object.Destroy(sheet);
            }
            camera.targetTexture = null;
            RenderTexture.ReleaseTemporary(target);
            Object.Destroy(shot);
            Object.Destroy(cameraObject);
            Log("machine close-ups written");
        }

        /// <summary>Every item model of the mod, in a grid floating above the test ship, photographed.</summary>
        private static IEnumerator PhotographItems(uint shipId)
        {
            Transform ship = ShipVisual(shipId);
            if (ship == null)
                yield break;
            var holder = new GameObject("SI_DevItems");
            Vector3 origin = ship.position + ship.up * 6f;
            holder.transform.SetPositionAndRotation(origin, ship.rotation);
            const int cols = 7;
            int n = 0;
            foreach (ResourceDef def in ResourceCatalog.All)
            {
                ItemSettings settings = IndustrialItems.Find(def.Id);
                if (settings?.itemObject == null)
                    continue;
                GameObject copy = Object.Instantiate(settings.itemObject, holder.transform, false);
                copy.SetActive(true);
                copy.transform.localPosition = new Vector3((n % cols - (cols - 1) * 0.5f) * 0.5f, -(n / cols) * 0.45f, 0f);
                copy.transform.localRotation = Quaternion.Euler(-25f, 30f, 0f);
                n++;
            }
            var cameraObject = new GameObject("SI_DevCamera");
            var camera = cameraObject.AddComponent<Camera>();
            if (Camera.main != null)
                camera.CopyFrom(Camera.main);
            camera.enabled = false;
            camera.fieldOfView = 30f;
            camera.nearClipPlane = 0.05f;
            camera.transform.position = holder.transform.TransformPoint(new Vector3(0f, -0.65f, 6.2f));
            camera.transform.rotation = Quaternion.LookRotation(holder.transform.TransformPoint(new Vector3(0f, -0.65f, 0f)) - camera.transform.position, ship.up);
            var target = RenderTexture.GetTemporary(1600, 900, 24, RenderTextureFormat.ARGB32);
            camera.targetTexture = target;
            yield return null;
            camera.Render();
            var shot = new Texture2D(1600, 900, TextureFormat.RGBA32, false);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = target;
            shot.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0);
            shot.Apply();
            RenderTexture.active = previous;
            File.WriteAllBytes(Path.Combine(Folder, "items.png"), shot.EncodeToPNG());
            camera.targetTexture = null;
            RenderTexture.ReleaseTemporary(target);
            Object.Destroy(shot);
            Object.Destroy(cameraObject);
            Object.Destroy(holder);
            Log($"{n} item models photographed");
        }

        // ---- Wiki

        /// <summary>Every wiki page renders and every link leads to a page; the guide opens on the wiki (screenshots).</summary>
        private static IEnumerator CheckWiki()
        {
            PartSettings label = SDModKit.Game.CustomParts.Get(MachineCatalog.Workshop);
            Log($"language: {(Lang.English ? "English" : "French")}; item 7720 '{IndustrialItems.NameOf(ResourceCatalog.Rotor)}', item 7701 '{IndustrialItems.NameOf(ResourceCatalog.IronPlate)}', part 7601 '{label?.fullLabel}', tech 7801 '{Lang.TechName(7801, "?")}', recipe '{Lang.RecipeName(Recipes.RecipeBook.Get("workshop.iron_plate_raw"))}'");
            var names = Wiki.WikiPages.Names.ToList();
            var broken = new List<string>();
            var link = new System.Text.RegularExpressions.Regex("\\[([^\\]]+)\\]\\(([^)]+)\\)");
            foreach (string page in names)
            {
                foreach (string md in new[] { Wiki.WikiContent.Generate().TryGetValue(page, out string g) ? g : null })
                {
                    if (md == null)
                        continue;
                    foreach (System.Text.RegularExpressions.Match m in link.Matches(md))
                    {
                        string target = Path.GetFileNameWithoutExtension(m.Groups[2].Value.Split('#')[0]);
                        if (!m.Groups[2].Value.StartsWith("http") && !Wiki.WikiPages.Exists(target))
                            broken.Add(page + " -> " + target);
                    }
                }
                Wiki.WikiPages.Render(page);
            }
            Log($"{(broken.Count == 0 && names.Count > 60 ? "PASS" : "FAIL")} wiki: {names.Count} pages ({string.Join(", ", names.Where(n => !n.StartsWith("Objet-") && !n.StartsWith("Machine-")))} + machine and item pages), {broken.Count} broken link(s) {string.Join("; ", broken.Take(10))}");
            var activator = Object.FindObjectsByType<global::UI.Guide.GuideMenuActivator>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault();
            if (activator == null)
            {
                Log("wiki: no guide in this scene, no screenshots");
                yield break;
            }
            activator.ToggleGuide();
            yield return Wait(1.5f);
            var canvas = Object.FindObjectsByType<global::UI.Guide.GuideCanvas>(FindObjectsSortMode.None).FirstOrDefault();
            var buttons = canvas != null ? HarmonyLib.Traverse.Create(canvas).Field<List<global::UI.Guide.GuideEntryButton>>("_buttons").Value : null;
            global::UI.Guide.GuideEntryButton entry = buttons?.FirstOrDefault(b => b.PageContent != null && b.PageContent.EntryId == Wiki.WikiGuide.EntryId);
            Log($"wiki: guide open {canvas != null}, {buttons?.Count} entries, wiki entry {(entry != null ? "found" : "MISSING")}");
            if (entry != null)
            {
                canvas.SelectGuideEntry(entry);
                foreach ((string page, string file) in new[] { ("Home", "wiki_home"), ("Machine-7612", "wiki_machine"), ("Objet-7720", "wiki_item"), ("Recherche", "wiki_research") })
                {
                    Wiki.WikiPage.Show(page);
                    yield return Wait(1f);
                    ScreenCapture.CaptureScreenshot(Path.Combine(Folder, file + ".png"));
                    yield return Wait(0.5f);
                }
            }
            activator.Close();
        }

        // ---- Showcase (DevPhase 4): every machine alone, four views each

        private static IEnumerator Showcase()
        {
            var players = ServiceLocator.GetService<Players.Services.PlayersServerTracker>();
            var player = players?.ActivePlayers.FirstOrDefault();
            SpacesServer spaces = ServiceLocator.GetService<SpacesServer>();
            ShipsServerTracker ships = ServiceLocator.GetService<ShipsServerTracker>();
            var ids = ServiceLocator.GetService<WorldObjectsIdsServerTracker>();
            if (player == null || spaces == null || ships == null || ids == null)
            {
                Log($"FAIL services missing: player {player != null} (players {players?.ActivePlayers.Count}), spaces {spaces != null}, ships {ships != null}, ids {ids != null}");
                yield break;
            }
            // Machines and poles in a row along x, each centred in its slot
            var slots = new List<(ushort Id, Vector3 Size, float X)>();
            float cursor = 0f;
            var models = MachineCatalog.All.Select(m => (m.PartId, new Vector3(m.SizeX, m.SizeY, m.SizeZ)))
                .Concat(Parts.PowerPoles.All.Select(p => (p.Id, new Vector3(0.48f, p.Height * 1.5f, 0.48f))));
            foreach ((ushort id, Vector3 size) in models)
            {
                slots.Add((id, size, cursor + size.x * 0.5f));
                cursor += size.x + 2.2f;
            }
            float half = cursor * 0.5f;
            var hull = new StringBuilder();
            for (int x = -Mathf.CeilToInt(half) - 1; x <= Mathf.CeilToInt(half) + 1; x++)
            {
                for (int z = -3; z <= 3; z++)
                {
                    if (hull.Length > 0)
                        hull.Append(',');
                    hull.Append($"{{\"partId\":{IronFloor},\"x\":{x},\"y\":0,\"z\":{z},\"forwardOrientationId\":4,\"upOrientationId\":0,\"voxelPartTypeId\":0,\"FrontColor\":{{\"x\":150,\"y\":150,\"z\":150}},\"BackColor\":{{\"x\":120,\"y\":120,\"z\":120}}}}");
                }
            }
            var parts = new StringBuilder();
            for (int i = 0; i < slots.Count; i++)
            {
                if (parts.Length > 0)
                    parts.Append(',');
                parts.Append(string.Format(CultureInfo.InvariantCulture, "{{\"id\":{0},\"settingsId\":{1},\"coord\":{{\"x\":{2},\"y\":-0.46,\"z\":0}},\"forwardOrientationId\":2,\"upOrientationId\":4}}",
                    i + 1, slots[i].Id, slots[i].X - half));
            }
            var sb = new StringBuilder();
            sb.Append("{\"rootShipId\":1,\"cables\":[],\"ships\":[{");
            sb.Append("\"id\":1,\"relativeSpaceId\":0,\"spaceId\":0,\"position\":{\"x\":0,\"y\":0,\"z\":0},\"velocity\":{\"x\":0,\"y\":0,\"z\":0},\"rotation\":{\"x\":0,\"y\":0,\"z\":0,\"w\":1},");
            sb.Append("\"rooms\":[],\"hullParts\":[").Append(hull).Append("],\"statefulFloatingParts\":[").Append(parts).Append("],\"statefulHullParts\":[],");
            foreach (string empty in new[] { "thrustersStates", "storageStates", "fluidTankStates", "constantStates", "beaconStates", "leverStates", "warpDriveStates",
                         "dockingDoorStates", "targeterStates", "mathGateStates", "unaryMathGateStates", "memoryRegisterStates", "transmitterStates", "rotorStates",
                         "electrolyzerStates", "woodThrusterStates", "woodGeneratorStates", "ethanolDistillerStates" })
                sb.Append('"').Append(empty).Append("\":[],");
            sb.Length--;
            sb.Append("}]}");
            var data = JsonUtility.FromJson<ShipImportExportUtils.ShipImportExportData>(sb.ToString());
            SavedShipState state = new ShipsConverter().ConvertToState(data.ships).States.Cast<SavedShipState>().Single();
            state.Id = ids.CreateUniqueIdForObject();
            state.SpaceId = player.SpaceId;
            Vector3 up = player.Rotation * Vector3.up;
            state.Position = player.Position + up * 2f + player.Rotation * Vector3.forward * 14f;
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
            Log($"showcase ship {state.Id}: {slots.Count} machines");
            yield return Wait(6f);
            Transform ship = ShipVisual(state.Id);
            TrackedShipServer server = ships.GetShip(state.Id);
            if (ship == null || server == null)
            {
                Log("FAIL showcase ship not shown");
                yield break;
            }
            const int cell = 640;
            var cameraObject = new GameObject("SI_DevCamera");
            var camera = cameraObject.AddComponent<Camera>();
            if (Camera.main != null)
                camera.CopyFrom(Camera.main);
            camera.enabled = false;
            camera.fieldOfView = 40f;
            camera.nearClipPlane = 0.05f;
            var target = RenderTexture.GetTemporary(cell, cell, 24, RenderTextureFormat.ARGB32);
            camera.targetTexture = target;
            var shot = new Texture2D(cell, cell, TextureFormat.RGBA32, false);
            (Vector3 Dir, string Name)[] views = { (new Vector3(0.5f, 0.45f, 1f), "front"), (new Vector3(-0.5f, 0.4f, -1f), "back"), (new Vector3(1f, 0.25f, -0.15f), "side"), (new Vector3(-0.35f, 1.1f, 0.6f), "top") };
            for (int i = 0; i < slots.Count; i++)
            {
                ushort id = slots[i].Id;
                Vector3 size = slots[i].Size;
                if (!server.TryGetStatefulPart((ushort)(i + 1), out StatefulPart part))
                    continue;
                var sheet = new Texture2D(cell * 2, cell * 2, TextureFormat.RGBA32, false);
                float reach = Mathf.Max(1.6f, size.magnitude);
                for (int v = 0; v < views.Length; v++)
                {
                    Vector3 center = ship.TransformPoint(part.LocalPosition + part.LocalRotation * new Vector3(0f, size.y * 0.45f, 0f));
                    Vector3 dir = ship.rotation * (part.LocalRotation * views[v].Dir.normalized);
                    camera.transform.position = center + dir * reach * 1.15f;
                    camera.transform.rotation = Quaternion.LookRotation(center - camera.transform.position, ship.up);
                    camera.Render();
                    RenderTexture previous = RenderTexture.active;
                    RenderTexture.active = target;
                    shot.ReadPixels(new Rect(0, 0, cell, cell), 0, 0);
                    shot.Apply();
                    RenderTexture.active = previous;
                    sheet.SetPixels((v % 2) * cell, (1 - v / 2) * cell, cell, cell, shot.GetPixels());
                    yield return null;
                }
                sheet.Apply();
                Directory.CreateDirectory(Folder);
                File.WriteAllBytes(Path.Combine(Folder, $"model_{id}.png"), sheet.EncodeToPNG());
                Object.Destroy(sheet);
            }
            // The deposits near the player, with their ore model, two views each
            List<Deposits.Rock> rocks = Deposits.Loaded().OrderBy(r => Vector3.Distance(r.Position, camera.transform.position)).ToList();
            if (rocks.Count > 0)
            {
                Deposits.Rock first = rocks[0];
                Transform rt = first.Object.transform;
                Log($"probe rock: components {string.Join(",", rt.GetComponents<Component>().Select(c => c.GetType().Name))}, children {rt.childCount}, colliders {string.Join(",", rt.GetComponentsInChildren<Collider>().Select(c => c.GetType().Name + (c is MeshCollider mc && mc.sharedMesh != null ? ":" + mc.sharedMesh.name + "/" + mc.sharedMesh.isReadable + "/" + mc.sharedMesh.vertexCount : "")))}");
                foreach (MeshFilter mf in Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
                {
                    Renderer r = mf.GetComponent<Renderer>();
                    if (r == null || mf.sharedMesh == null || Vector3.Distance(r.bounds.center, first.Position) > 2.5f || r.bounds.size.magnitude > 8f)
                        continue;
                    string path = mf.name;
                    for (Transform t = mf.transform.parent; t != null; t = t.parent)
                        path = t.name + "/" + path;
                    Log($"probe renderer near rock: {path} mesh {mf.sharedMesh.name} readable {mf.sharedMesh.isReadable} verts {mf.sharedMesh.vertexCount} mat {r.sharedMaterial?.name} shader {r.sharedMaterial?.shader?.name} pos {mf.transform.position} rot {mf.transform.rotation.eulerAngles} scale {mf.transform.lossyScale}");
                }
                Log("probe all rocks: " + string.Join(", ", Object.FindObjectsByType<Planets.Objects.MinablePlanetObject>(FindObjectsSortMode.None).GroupBy(m => m.dropRate.item != null ? m.dropRate.item.itemName : "?").Select(g => g.Count() + " " + g.Key)));
            }
            Log("deposit keys: " + string.Join(" ; ", rocks.Select(r => r.Name + " " + Deposits.DescribeKey(r.Object))));
            Log($"deposits: {rocks.Count(r => r.Node)} inexhaustible");
            Log($"deposits: {rocks.Count} rock(s) loaded: " + string.Join(", ", rocks.GroupBy(r => r.Name + " " + r.Purity).Select(g => g.Count() + " " + g.Key)));
            var localPlayer = SDModKit.Game.GameRefs.PlayersClient?.LocalPlayer;
            Vector3 worldUp = localPlayer != null ? localPlayer.Rotation * Vector3.up : ship.up;
            int shown = 0;
            var rockSheet = new Texture2D(cell * 4, cell * 2, TextureFormat.RGBA32, false);
            // Inexhaustible deposits first, then the mod's ores, then the rest
            foreach (Deposits.Rock rock in rocks.OrderByDescending(r => r.Node).ThenByDescending(r => ResourceCatalog.Get(r.Item) != null).Take(4))
            {
                Parts.DepositDecor.Decorate(rock);
                Log($"rock {rock.Name} {rock.Purity}{(rock.Node ? " node" : "")}: " + Parts.DepositDecor.Explain(rock));
                Transform t = rock.Object.transform;
                Bounds b = new Bounds(t.position, Vector3.zero);
                foreach (Renderer r in t.GetComponentsInChildren<Renderer>())
                    b.Encapsulate(r.bounds);
                Log($"rock {shown}: {rock.Name} {rock.Purity} at {rock.Position}, parent {(t.parent != null ? t.parent.name : "-")}, up·world up {Vector3.Dot(t.up, worldUp):0.00}, scale {t.lossyScale}, " +
                    $"renderers {string.Join("/", t.GetComponentsInChildren<Renderer>().Select(r => r.name + ":" + r.sharedMaterial?.name))}, bounds {b.size}");
                for (int v = 0; v < 2; v++)
                {
                    Vector3 dir = Quaternion.AngleAxis(v * 140f, worldUp) * (Vector3.ProjectOnPlane(camera.transform.position - b.center, worldUp).normalized + worldUp * 0.55f);
                    float reach = Mathf.Max(2f, b.extents.magnitude * 2.4f);
                    camera.transform.position = b.center + dir.normalized * reach;
                    camera.transform.rotation = Quaternion.LookRotation(b.center - camera.transform.position, worldUp);
                    camera.Render();
                    RenderTexture previous = RenderTexture.active;
                    RenderTexture.active = target;
                    shot.ReadPixels(new Rect(0, 0, cell, cell), 0, 0);
                    shot.Apply();
                    RenderTexture.active = previous;
                    rockSheet.SetPixels(shown * cell, (1 - v) * cell, cell, cell, shot.GetPixels());
                    yield return null;
                }
                shown++;
            }
            rockSheet.Apply();
            File.WriteAllBytes(Path.Combine(Folder, "deposits.png"), rockSheet.EncodeToPNG());
            Object.Destroy(rockSheet);
            Log($"deposits: {shown} photographed, {Parts.DepositDecor.Count} decorated in all");
            // The laser on an inexhaustible deposit: no wear, nothing given (extractors only)
            Deposits.Rock? node = rocks.Where(r => r.Node && r.Object != null).Cast<Deposits.Rock?>().FirstOrDefault();
            if (node != null)
            {
                var minable = node.Value.Object;
                var before = SDModKit.Game.PlayerAccess.CurrentCounts();
                int had = before.TryGetValue(node.Value.Item, out int h) ? h : 0;
                float health = minable.CurrentHealth;
                minable.ReduceHealth(minable.MaxHealth + 1f);
                yield return Wait(1f);
                var after = SDModKit.Game.PlayerAccess.CurrentCounts();
                int has = after.TryGetValue(node.Value.Item, out int n2) ? n2 : 0;
                Log($"{(minable != null && minable.CurrentHealth == health && has == had ? "PASS" : "FAIL")} inexhaustible deposit not minable by the laser: {IndustrialItems.NameOf(node.Value.Item)} {had} -> {has}, health {health:0.0} -> {minable?.CurrentHealth:0.0}");
            }
            else
            {
                Log("SKIP inexhaustible deposit and the laser: none loaded near the player");
            }
            yield return CheckDecorFollows(rocks);
            camera.targetTexture = null;
            RenderTexture.ReleaseTemporary(target);
            Object.Destroy(shot);
            Object.Destroy(cameraObject);
            Log($"showcase: {slots.Count} model sheets written");
            // The scanner panel (a Mk3 with both modules) over the player's view
            Log("scanner: " + UI.DepositScanner.Scan(best: true).Replace("\n", " | "));
            UI.DepositScanner.Hold = true;
            yield return Wait(1.5f);
            ScreenCapture.CaptureScreenshot(Path.Combine(Folder, "scanner.png"));
            yield return Wait(1f);
            UI.DepositScanner.Hold = false;
            Log("scanner screenshot requested");
            yield return CheckModules();
        }

        /// <summary>The host gets a Mk2 and three modules, fits two (Mk2: 3 slots), sees the effect, takes one out.</summary>
        /// <summary>
        /// The ore drawn on a rock must never float on its own: hidden with the rock's model, gone
        /// when the rock object moves (the game reuses rock objects), back on the rock afterwards.
        /// No churn: decorations stay put while nothing changes.
        /// </summary>
        private static IEnumerator CheckDecorFollows(List<Deposits.Rock> rocks)
        {
            float wait = 0f;
            while (Parts.DepositDecor.Live == 0 && wait < 8f)
            {
                yield return Wait(1f);
                wait += 1f;
            }
            int made = Parts.DepositDecor.Count, live = Parts.DepositDecor.Live;
            yield return Wait(4f);
            Log($"{(Parts.DepositDecor.Count == made && Parts.DepositDecor.Live == live && live > 0 ? "PASS" : "FAIL")} deposit decorations steady: {live} shown, {Parts.DepositDecor.Count - made} remade in 4 s");
            Deposits.Rock? pick = rocks.Where(r => r.Object != null && !r.Node && r.Object.transform.Find(Parts.DepositDecor.ChildName) != null && Parts.DepositDecor.VisualOf(r) != null)
                .Cast<Deposits.Rock?>().FirstOrDefault();
            if (pick == null)
            {
                Log("SKIP deposit decoration follow: no decorated rock with a separate model");
                yield break;
            }
            Transform rock = pick.Value.Object.transform;
            MeshFilter visual = Parts.DepositDecor.VisualOf(pick.Value);
            bool own = visual.transform.IsChildOf(rock);
            // Mined: the game hides the rock's model
            visual.gameObject.SetActive(false);
            yield return Wait(1.2f);
            bool hidden = rock.Find(Parts.DepositDecor.ChildName) == null;
            visual.gameObject.SetActive(true);
            // A rock whose model was not found is looked at again after 10 s
            yield return Wait(14f);
            bool back = rock.Find(Parts.DepositDecor.ChildName) != null;
            Log($"{(hidden && back ? "PASS" : "FAIL")} ore follows the rock's model: gone when hidden {hidden}, back when shown {back}");
            // Reused: the rock object moved elsewhere (its own model with it only if it carries it)
            if (!own)
            {
                Vector3 was = rock.position;
                rock.position = was + rock.up * 3f;
                yield return Wait(1.2f);
                bool gone = rock.Find(Parts.DepositDecor.ChildName) == null;
                rock.position = was;
                Log($"{(gone ? "PASS" : "FAIL")} no ore left floating when the rock object moves away from its model: {gone}");
            }
        }

        private static IEnumerator CheckModules()
        {
            foreach (uint item in new[] { ResourceCatalog.ScannerMk2, ResourceCatalog.RangeModule2, ResourceCatalog.AnalysisModule2, ResourceCatalog.RangeModule })
                SDModKit.Game.PlayerAccess.TryGive(item, 1, out _);
            yield return Wait(1f);
            float before = UI.DepositScanner.CurrentGear().Range;
            UI.ScannerModules.RequestInstall(ResourceCatalog.RangeModule2);
            UI.ScannerModules.RequestInstall(ResourceCatalog.AnalysisModule2);
            yield return Wait(1f);
            UI.DepositScanner.Gear gear = UI.DepositScanner.CurrentGear();
            var counts = SDModKit.Game.PlayerAccess.CurrentCounts();
            int left = counts.TryGetValue(ResourceCatalog.RangeModule2, out int n) ? n : 0;
            bool fitted = UI.ScannerModules.Own.Count == 2 && Mathf.Approximately(gear.RangeBonus, 0.5f) && gear.Markers && left == 0;
            Log($"{(fitted ? "PASS" : "FAIL")} scanner modules fitted: [{string.Join(",", UI.ScannerModules.Own)}], Mk{gear.Tier} range bonus +{gear.RangeBonus * 100f:0} % ({before:0} m -> {gear.Range:0} m, radar {gear.RadarBoost}), markers {gear.Markers}, range module II left in inventory {left}");
            UI.ScannerPanel.Toggle();
            yield return Wait(1f);
            ScreenCapture.CaptureScreenshot(Path.Combine(Folder, "modules.png"));
            yield return Wait(0.5f);
            UI.ScannerPanel.Toggle();
            UI.ScannerModules.RequestRemove(0);
            yield return Wait(1f);
            counts = SDModKit.Game.PlayerAccess.CurrentCounts();
            left = counts.TryGetValue(ResourceCatalog.RangeModule2, out n) ? n : 0;
            Log($"{(UI.ScannerModules.Own.Count == 1 && left == 1 ? "PASS" : "FAIL")} scanner module taken out: [{string.Join(",", UI.ScannerModules.Own)}], range module II back in inventory {left}");
        }

        // ---- Load test (DevPhase 3)

        private const int LoadMachines = 200;

        /// <summary>
        /// 200 workshops on one ship, all producing at once (power not required for the test),
        /// the simulation's cost measured over 15 s; nothing is saved.
        /// </summary>
        private static IEnumerator LoadTest()
        {
            var players = ServiceLocator.GetService<Players.Services.PlayersServerTracker>();
            var player = players?.ActivePlayers.FirstOrDefault();
            SpacesServer spaces = ServiceLocator.GetService<SpacesServer>();
            ShipsServerTracker ships = ServiceLocator.GetService<ShipsServerTracker>();
            var ids = ServiceLocator.GetService<WorldObjectsIdsServerTracker>();
            if (player == null || spaces == null || ships == null || ids == null)
            {
                Log("FAIL services missing");
                yield break;
            }
            yield return Wait(10f);
            double idleMs = FactoryOverview.AverageMs;
            float idleFps = 0f;
            yield return MeasureFps(5f, f => idleFps = f);
            bool requirePower = Settings.RequirePower.Value;
            Settings.RequirePower.Value = false;
            var data = JsonUtility.FromJson<ShipImportExportUtils.ShipImportExportData>(LoadJson());
            SavedShipState state = new ShipsConverter().ConvertToState(data.ships).States.Cast<SavedShipState>().Single();
            state.Id = ids.CreateUniqueIdForObject();
            state.SpaceId = player.SpaceId;
            Vector3 up = player.Rotation * Vector3.up;
            state.Position = player.Position + up * 1.5f + player.Rotation * Vector3.right * 30f;
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
            yield return Wait(1f);
            TrackedShipServer ship = ships.GetShip(state.Id);
            if (ship == null)
            {
                Log("FAIL load ship missing");
                Settings.RequirePower.Value = requirePower;
                yield break;
            }
            for (ushort id = 1; id <= LoadMachines; id++)
                ship.UpdatePartState(id, new StorageState { InventorySlots = Slots((GameItems.Iron, 200), (GameItems.Copper, 100)) });
            Log($"load ship {state.Id}: {LoadMachines} workshops filled, simulation idle cost {idleMs:0.000} ms/tick, {idleFps:0} fps");
            // Two full 5 s windows of the overview's cost counter
            yield return Wait(11f);
            float loadFps = 0f;
            yield return MeasureFps(5f, f => loadFps = f);
            double loadMs = FactoryOverview.AverageMs;
            int producing = ship.GetAllStatefulParts<StorageState>().Count(p => MachineSimulation.Get(ship.Id, p.Id)?.Status == MachineStatus.Producing);
            Log("overview: " + Strip(FactoryOverview.Build()));
            Settings.RequirePower.Value = requirePower;
            bool ok = producing == LoadMachines && loadMs < 2.0;
            Log($"{(ok ? "PASS" : "FAIL")} load: {producing}/{LoadMachines} producing, simulation {loadMs:0.000} ms/tick (peak {FactoryOverview.PeakMs:0.000}), {loadFps:0} fps (before: {idleFps:0} fps)");
        }

        private static IEnumerator MeasureFps(float seconds, Action<float> result)
        {
            float start = Time.unscaledTime;
            int frames = 0;
            while (Time.unscaledTime - start < seconds)
            {
                frames++;
                yield return null;
            }
            result(frames / Mathf.Max(0.01f, Time.unscaledTime - start));
        }

        private static string LoadJson()
        {
            var hull = new StringBuilder();
            for (int x = -16; x <= 16; x++)
            {
                for (int z = -12; z <= 12; z++)
                {
                    if (hull.Length > 0)
                        hull.Append(',');
                    hull.Append($"{{\"partId\":{IronFloor},\"x\":{x},\"y\":0,\"z\":{z},\"forwardOrientationId\":4,\"upOrientationId\":0,\"voxelPartTypeId\":0,\"FrontColor\":{{\"x\":150,\"y\":150,\"z\":150}},\"BackColor\":{{\"x\":120,\"y\":120,\"z\":120}}}}");
                }
            }
            var parts = new StringBuilder();
            for (int i = 0; i < LoadMachines; i++)
            {
                float x = -15f + (i % 20) * 1.55f, z = -11f + (i / 20) * 2.2f;
                if (parts.Length > 0)
                    parts.Append(',');
                parts.Append(string.Format(CultureInfo.InvariantCulture,
                    "{{\"id\":{0},\"settingsId\":{1},\"coord\":{{\"x\":{2},\"y\":-0.46,\"z\":{3}}},\"forwardOrientationId\":2,\"upOrientationId\":4}}", i + 1, MachineCatalog.Workshop, x, z));
            }
            var sb = new StringBuilder();
            sb.Append("{\"rootShipId\":1,\"cables\":[],\"ships\":[{");
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

        private static string Strip(string rich) => System.Text.RegularExpressions.Regex.Replace(rich, "<[^>]+>", "").Replace("\n", " | ");

        // ---- Transfers

        private static uint _cargoId;

        /// <summary>A 3 x 3 floor with one loading dock.</summary>
        private static string CargoJson()
        {
            var hull = new StringBuilder();
            for (int x = -1; x <= 1; x++)
            {
                for (int z = -1; z <= 1; z++)
                {
                    if (hull.Length > 0)
                        hull.Append(',');
                    hull.Append($"{{\"partId\":{IronFloor},\"x\":{x},\"y\":0,\"z\":{z},\"forwardOrientationId\":4,\"upOrientationId\":0,\"voxelPartTypeId\":0,\"FrontColor\":{{\"x\":120,\"y\":140,\"z\":170}},\"BackColor\":{{\"x\":120,\"y\":120,\"z\":120}}}}");
                }
            }
            string part = string.Format(CultureInfo.InvariantCulture,
                "{{\"id\":{0},\"settingsId\":{1},\"coord\":{{\"x\":0,\"y\":-0.46,\"z\":0}},\"forwardOrientationId\":2,\"upOrientationId\":4}}", CargoDock, MachineCatalog.LoadingDock);
            var sb = new StringBuilder();
            sb.Append("{\"rootShipId\":1,\"cables\":[],\"ships\":[{");
            sb.Append("\"id\":1,\"relativeSpaceId\":0,\"spaceId\":0,\"position\":{\"x\":0,\"y\":0,\"z\":0},\"velocity\":{\"x\":0,\"y\":0,\"z\":0},\"rotation\":{\"x\":0,\"y\":0,\"z\":0,\"w\":1},");
            sb.Append("\"rooms\":[],\"hullParts\":[").Append(hull).Append("],\"statefulFloatingParts\":[").Append(part).Append("],\"statefulHullParts\":[],");
            foreach (string empty in new[] { "thrustersStates", "storageStates", "fluidTankStates", "constantStates", "beaconStates", "leverStates", "warpDriveStates",
                         "dockingDoorStates", "targeterStates", "mathGateStates", "unaryMathGateStates", "memoryRegisterStates", "transmitterStates", "rotorStates",
                         "electrolyzerStates", "woodThrusterStates", "woodGeneratorStates", "ethanolDistillerStates" })
                sb.Append('"').Append(empty).Append("\":[],");
            sb.Length--;
            sb.Append("}]}");
            return sb.ToString();
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
            // In a world where Transformation is already researched the press is rightly unlocked
            bool ok = Parts.IndustryResearch.Techs.Count == Parts.IndustryResearch.All.Length && (before == false || state.IsResearched(7801)) && workshopAfter && (!pressAfter || state.IsResearched(7802));
            Log($"{(ok ? "PASS" : "FAIL")} research: {Parts.IndustryResearch.Techs.Count} techs in the tree; after Industrie de base: workshop unlocked {workshopAfter}, press unlocked {pressAfter} (needs Transformation)");
            // The prospecting tech's card lists the recipes it unlocks
            var card = Resources.FindObjectsOfTypeAll<global::UI.Research.ResearchInfo>().FirstOrDefault(c => c.gameObject.scene.IsValid());
            Research.Interface.Values.ResearchSettings prospecting = Parts.IndustryResearch.Techs.FirstOrDefault(t => t.id == Recipes.RecipeBook.ScanTech2);
            if (card == null || prospecting == null)
            {
                Log("SKIP research card: no card in the scene");
                return;
            }
            card.Show(prospecting);
            string[] rows = card.GetComponentsInChildren<TMPro.TMP_Text>(true).Select(t => t.text).Where(t => t != null && (t.StartsWith("Recette") || t.StartsWith("Recipe"))).ToArray();
            Log($"{(rows.Length == 2 ? "PASS" : "FAIL")} research card of {prospecting.techName}: {string.Join(" | ", rows)}");
            card.Hide();
        }

        // ---- Conveyors

        private static bool _itemLine;

        /// <summary>The item port Item Lines grafted on a machine (SDModKit item cable, type 3), or -1.</summary>
        private static int ItemSocket(ushort partId)
        {
            PartSettings settings = SDModKit.Game.CustomParts.Get(partId);
            if (settings?.part == null)
                return -1;
            int found = -1;
            foreach (SocketObject socket in settings.part.GetComponentsInChildren<SocketObject>(true))
            {
                Log($"part {partId} socket {socket.Id} type {(int)socket.Type} at {settings.part.transform.InverseTransformPoint(socket.transform.position):F2}");
                if ((int)socket.Type == 3 && found < 0)
                    found = socket.Id;
            }
            return found;
        }

        // ---- Extraction

        private static GameObject _devRock;
        private static string _devRockItem;

        /// <summary>
        /// The scanner's report, then: with no rock in reach the extractor must stay idle; a test
        /// rock (a game MinablePlanetObject giving iron, on this PC only) is then set beside it: a big
        /// one, drawn 8 m wide around a small collider, its centre 6.5 m away (beyond the reach) and
        /// its side 2.5 m away, like the big crystals players set extractors against.
        /// </summary>
        private static void PlaceDevRock(TrackedShipServer ship)
        {
            Log("scanner (player's): " + UI.DepositScanner.Scan().Replace("\n", " | "));
            Log("scanner (Mk3, both modules): " + UI.DepositScanner.Scan(best: true).Replace("\n", " | "));
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
            // Real rocks are measured to their surface (colliders), not to their centre
            foreach (Deposits.Rock r in Deposits.Loaded().OrderBy(r => Vector3.Distance(r.Position, at.Value)).Take(5))
            {
                MeshFilter visual = Parts.DepositDecor.VisualOf(r);
                Bounds? seen = visual != null ? visual.GetComponent<Renderer>()?.bounds : null;
                Log($"real rock {r.Name}: centre {Vector3.Distance(r.Position, at.Value):0.0} m, surface {Deposits.DistanceTo(r, at.Value):0.0} m, " +
                    $"colliders {(r.Shape != null ? r.Shape.Value.size.ToString() + " off " + (r.Shape.Value.center - r.Position).magnitude.ToString("0.0") : "none")}, " +
                    $"visual {(seen != null ? seen.Value.size.ToString() + " off " + (seen.Value.center - r.Position).magnitude.ToString("0.0") : "none")}");
            }
            Transform shipVisual = ShipVisual(ship.Id);
            // Away from the test ship, so that the big rock covers no other machine
            Vector3 away = shipVisual != null ? Vector3.ProjectOnPlane(at.Value - shipVisual.position, shipVisual.up) : Vector3.forward;
            away = away.sqrMagnitude > 0.01f ? away.normalized : Vector3.forward;
            _devRock = new GameObject("SI_DevRock");
            _devRock.transform.position = at.Value + away * 6.5f;
            var look = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.Destroy(look.GetComponent<Collider>());
            look.name = "SI_DevRock look";
            look.transform.SetParent(_devRock.transform, false);
            look.transform.localScale = Vector3.one * 8f;
            look.GetComponent<Renderer>().forceRenderingOff = true;
            _devRock.AddComponent<SphereCollider>().radius = 0.6f;
            // The rock moves with the ship (a big test ship can still settle or slide on the ground)
            if (shipVisual != null)
                _devRock.transform.SetParent(shipVisual, true);
            var minable = _devRock.AddComponent<Planets.Objects.MinablePlanetObject>();
            minable.dropRate = new Planets.Objects.Common.Model.ItemDropRate { item = IndustrialItems.Find(GameItems.Iron), amountMin = 1, amountMax = 2 };
            _devRockItem = minable.dropRate.item != null ? minable.dropRate.item.itemName : "?";
            Deposits.Rescan();
            Log($"big test rock ({_devRockItem}) set beside the extractor at {at.Value}: centre {Vector3.Distance(_devRock.transform.position, at.Value):0.0} m");
        }

        /// <summary>The 12 iron went from the chest along both belts and were shared by the splitter between the two containers.</summary>
        private static void CheckConveyors(TrackedShipServer ship)
        {
            int a = Crafting.CountOf(Stacks(ship, OutA), GameItems.Iron), b = Crafting.CountOf(Stacks(ship, OutB), GameItems.Iron);
            int left = Crafting.CountOf(Stacks(ship, SourceChest), GameItems.Iron);
            int onBelts = Crafting.CountOf(Stacks(ship, Belt1), GameItems.Iron) + Crafting.CountOf(Stacks(ship, Belt2), GameItems.Iron) + Crafting.CountOf(Stacks(ship, SplitterPart), GameItems.Iron);
            bool pass = a > 0 && b > 0 && a + b + left + onBelts == 12 && a + b >= 8;
            Log($"{(pass ? "PASS" : "FAIL")} conveyors: chest {left} left, {onBelts} on the belts, container A {a}, container B {b} (12 iron in all)");
            foreach (ushort id in new[] { Belt1, Belt2, SplitterPart })
                Log($"conveyor {id}: {MachineSimulation.Get(ship.Id, id)?.Status}, {MachineSimulation.Get(ship.Id, id)?.Detail?.Replace("\n", " | ")}");
        }

        private static Vector3 Axis(Vector3 v) =>
            Mathf.Abs(v.x) >= Mathf.Abs(v.z) ? new Vector3(Mathf.Sign(v.x), 0f, 0f) : new Vector3(0f, 0f, Mathf.Sign(v.z));

        private static MachineDef Def(ushort id) => MachineCatalog.TryGet(id, out MachineDef d) ? d : null;

        /// <summary>
        /// Multiplayer: with no rock in reach on the host, a client's reports (sent through the
        /// mod's network message, handled as from the host's own client) make the extractor work;
        /// when they stop, it stops.
        /// </summary>
        private static IEnumerator CheckClientReports(TrackedShipServer ship)
        {
            Deposits.Rescan();
            MachineDef def = Def(MachineCatalog.Extractor);
            string report = DepositReports.Encode(new Deposits.Deposit { Item = GameItems.Iron, Purity = Purity.Pure, Distance = 2.5f });
            int before = Crafting.CountOf(Stacks(ship, ExtractorPart), GameItems.Iron);
            for (int i = 0; i < 6; i++)
            {
                SINet.SendToServer(SIMessageKind.DepositSeen, ship.Id, ExtractorPart, report);
                yield return Wait(2f);
            }
            MachineSimulation.Runtime rt = MachineSimulation.Get(ship.Id, ExtractorPart);
            int made = Crafting.CountOf(Stacks(ship, ExtractorPart), GameItems.Iron) - before;
            Log($"{(made >= 2 && rt?.Status == MachineStatus.Producing ? "PASS" : "FAIL")} extractor fed by a client's report (no rock on the host): {made} iron in 12 s, {rt?.Status} {rt?.Detail}");
            // A report too far for this extractor is refused
            SINet.SendToServer(SIMessageKind.DepositSeen, ship.Id, ExtractorPart, DepositReports.Encode(new Deposits.Deposit { Item = GameItems.Iron, Distance = def.ExtractRadius + 1f }));
            bool refused = DepositReports.Reported(ship.Id, ExtractorPart, def) == null;
            yield return Wait(10f);
            rt = MachineSimulation.Get(ship.Id, ExtractorPart);
            Log($"{(refused && rt?.Status == MachineStatus.NoDeposit ? "PASS" : "FAIL")} client reports stopped: {rt?.Status} (out-of-reach report refused: {refused})");
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
            MachineSimulation.Runtime rt = MachineSimulation.Get(ship.Id, ExtractorPart);
            // The test rock gives iron, or the mod's ore it holds
            Production.Deposits.Deposit? found = Production.Deposits.Find(ship.Id, part, Def(MachineCatalog.Extractor));
            uint ore = found?.Item ?? GameItems.Iron;
            int iron = Crafting.CountOf(slots, ore);
            int all = slots.Sum(x => x.Count);
            // About 28 s with the rock at 1 item per 6 s, times the rock's purity (x0.5, x1 or x2)
            float factor = found != null ? DepositPurity.Factor(found.Value.Purity) : 1f;
            bool ok = iron >= Mathf.FloorToInt(3 * factor) && iron <= Mathf.CeilToInt(6 * factor) + 1 && all == iron && iron > 0;
            Log($"{(ok ? "PASS" : "FAIL")} extractor: {iron} {IndustrialItems.NameOf(ore)} extracted ({all} items), {rt?.Status} {rt?.Detail}");
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

        private static Production.Stack[] Stacks(TrackedShipServer ship, ushort partId)
        {
            var slots = new Production.Stack[10];
            StatefulPart part = ship.GetAllStatefulParts<StorageState>().FirstOrDefault(p => p.Id == partId);
            if (part != null)
                MachineSimulation.Read((StorageState)part.State, slots);
            return slots;
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
                for (int z = -13; z <= 17; z++)
                {
                    // The 0.12 rows extend the deck forward and the 0.13 rows backward, on the left only:
                    // the cargo ship floats on the right, and the ship stays balanced on its slope
                    if ((z > 8 || z < -3) && x > 1)
                        continue;
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
                Part(Gen7, MachineCatalog.CombustionGenerator, -4f, 4.4f), Part(WorkshopD, MachineCatalog.Workshop, -2f, 4.4f),
                Part(DockA, MachineCatalog.LoadingDock, 4.3f, 4.4f), Part(ContainerPart, MachineCatalog.IndustrialContainer, 1.5f, 4.4f),
                Part(Gen8, WoodGenerator, -4.3f, 6.8f), Part(AccumulatorPart, MachineCatalog.Accumulator, -2.6f, 6.8f),
                Part(Gen9, WoodGenerator, -1f, 6.8f), Part(FoundryPart, MachineCatalog.Foundry, 0.6f, 6.8f),
                Part(Gen10, WoodGenerator, 2.2f, 6.8f), Part(RefineryPart, MachineCatalog.Refinery, 3.8f, 6.8f),
                Part(FabricatorPart, MachineCatalog.Fabricator, 3.6f, 9.3f), Part(AcceleratorPart, MachineCatalog.Accelerator, 0.4f, 9.3f),
                Part(Gen11, WoodGenerator, -4.3f, 12.4f), Part(CondenserPart, MachineCatalog.WaterCondenser, -2.8f, 12.4f),
                Part(RecyclerPart, MachineCatalog.Recycler, -1f, 12.4f), Part(RadarPart, MachineCatalog.RadarTower, 0.6f, 12.4f),
                Part(ReactorPart, MachineCatalog.StellarReactor, -2.5f, 15.6f),
                Part(Gen12, WoodGenerator, -4.2f, -11.6f), Part(PolePart, Parts.PowerPoles.PoleMk1, -2.8f, -11.6f), Part(WorkshopE, MachineCatalog.Workshop, -1f, -11.6f));
            // The conveyor line runs the way the parts face: chest, two belts, splitter, container A ahead and B on its right
            Vector3 start = _forward.z > 0.5f ? new Vector3(-2.5f, 0f, -9.6f) : _forward.z < -0.5f ? new Vector3(-2.5f, 0f, -4.4f)
                : _forward.x > 0.5f ? new Vector3(-4.6f, 0f, -7f) : new Vector3(0.6f, 0f, -7f);
            string Along(int id, int settings, float ahead, float right)
            {
                Vector3 p = start + _forward * ahead + _right * right;
                return Part(id, settings, p.x, p.z);
            }
            parts += "," + string.Join(",", Along(SourceChest, Chest, 0f, 0f), Along(Belt1, MachineCatalog.BeltMk1, 0.92f, 0f), Along(Belt2, MachineCatalog.BeltMk1, 1.94f, 0f),
                Along(SplitterPart, MachineCatalog.Splitter, 2.96f, 0f), Along(OutA, MachineCatalog.IndustrialContainer, 4.08f, 0f),
                Along(OutB, MachineCatalog.IndustrialContainer, 2.96f, 1.24f));
            // Power cables (type 2): each generator's socket 0 -> its machine's socket 0
            string Cable(int id, ushort from, ushort to) =>
                $"{{\"id\":{id},\"type\":2,\"plugA\":{{\"shipId\":1,\"partId\":{from},\"socketId\":0}},\"plugB\":{{\"shipId\":1,\"partId\":{to},\"socketId\":{MachineSimulation.PowerSocket}}}}}";
            string cables = string.Join(",", Cable(1, GeneratorPart, WorkshopA), Cable(2, Gen2, SmelterPart), Cable(3, Gen3, CrusherPart),
                Cable(4, Gen4, AssemblerPart), Cable(5, Gen5, PressPart), Cable(6, Gen6, ExtractorPart), Cable(7, Gen7, WorkshopD), Cable(9, Gen8, AccumulatorPart), Cable(10, Gen9, FoundryPart), Cable(11, Gen10, RefineryPart),
                Cable(12, Gen11, CondenserPart), Cable(13, ReactorPart, RecyclerPart),
                // Through the pole: generator -> pole socket 0, pole socket 1 -> workshop E
                $"{{\"id\":14,\"type\":2,\"plugA\":{{\"shipId\":1,\"partId\":{Gen12},\"socketId\":0}},\"plugB\":{{\"shipId\":1,\"partId\":{PolePart},\"socketId\":0}}}}",
                $"{{\"id\":15,\"type\":2,\"plugA\":{{\"shipId\":1,\"partId\":{PolePart},\"socketId\":1}},\"plugB\":{{\"shipId\":1,\"partId\":{WorkshopE},\"socketId\":{MachineSimulation.PowerSocket}}}}}");
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
