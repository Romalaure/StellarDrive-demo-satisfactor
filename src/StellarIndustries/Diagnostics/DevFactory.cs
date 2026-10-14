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
using Saving.Interface.Model;
using Saving.Services.Converters;
using Saving.Services.Model.State;
using Ships;
using Ships.Cables;
using Ships.Interface.Model.Parts;
using Ships.Interface.Model.Parts.Common;
using Ships.Interface.Model.Parts.State;
using Ships.Services;
using StellarIndustries.Content;
using StellarIndustries.Core;
using StellarIndustries.Production;
using StellarIndustries.Recipes;
using UnityEngine;
using WorldTracking.Interface.Values;
using WorldTracking.Services;
using WorldTracking.Spaces;
using Object = UnityEngine.Object;

namespace StellarIndustries.Diagnostics
{
    /// <summary>
    /// DevPhase 5: the mod's presentation picture. A large working factory beside the player:
    /// two conveyor lines (iron to plates and screws, copper split between a smelter and a
    /// crusher), every machine fed with the ingredients of its first recipe, three extractors on
    /// rocks, three stellar reactors powering everything through pylons and cables. After it has
    /// run for a while, it is photographed from above (dev/factory_*.png).
    /// </summary>
    internal static partial class DevTest
    {
        private sealed class Placed
        {
            public int Id;
            public ushort Settings;
            public Vector3 At;
            public MachineDef Def;
            public bool Powered;
        }

        private static IEnumerator Factory()
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
            // Which way the parts face (the conveyors run that way)
            var probe = JsonUtility.FromJson<ShipImportExportUtils.ShipImportExportData>(ShipJson(-1, 0));
            StatefulPart probePart = new ShipsConverter().ConvertToState(probe.ships).States.Cast<SavedShipState>().Single().StatefulParts.First(p => p.Id == GeneratorPart);
            _forward = Axis(probePart.LocalRotation * Vector3.forward);
            _right = Axis(probePart.LocalRotation * Vector3.right);

            var placed = new List<Placed>();
            int next = 1;
            Placed Put(ushort settings, Vector3 at, bool powered = false)
            {
                MachineCatalog.TryGet(settings, out MachineDef def);
                var p = new Placed { Id = next++, Settings = settings, At = at, Def = def, Powered = powered || (def != null && Parts.MachineModels.NeedsPower(def)) };
                placed.Add(p);
                return p;
            }
            float Depth(ushort settings) => MachineCatalog.TryGet(settings, out MachineDef d) ? d.SizeZ : 0.8f;

            // Four conveyor lines along the parts' front, on the left of the deck
            var lineParts = new List<Placed>();
            var chests = new Dictionary<int, uint>();
            var choices = new List<(Placed Part, string Recipe)>();
            Placed splitter = null;
            void Chain(float across, uint ore, params (ushort Part, string Recipe)[] chain)
            {
                float cursor = 0f;
                foreach ((ushort s, string recipe) in chain)
                {
                    float d = s == Chest ? 0.74f : Depth(s);
                    Placed p = Put(s, _right * across - _forward * 11f + _forward * (cursor + d * 0.5f));
                    lineParts.Add(p);
                    if (s == Chest)
                        chests[p.Id] = ore;
                    if (recipe != null)
                        choices.Add((p, recipe));
                    if (s == MachineCatalog.Splitter)
                        splitter = p;
                    cursor += d + 0.02f;
                }
            }
            (ushort, string) B3 = (MachineCatalog.BeltMk3, null), B2 = (MachineCatalog.BeltMk2, null), B1 = (MachineCatalog.BeltMk1, null);
            Chain(-21f, GameItems.Iron, (Chest, null), B3, B3, (MachineCatalog.Smelter, null), B3, B3, (MachineCatalog.Workshop, "workshop.iron_plate"), B3, B3,
                (MachineCatalog.IndustrialContainer, null));
            Chain(-17.5f, GameItems.Iron, (Chest, null), B2, (MachineCatalog.Crusher, null), B2, (MachineCatalog.Smelter, null), B2, (MachineCatalog.Workshop, "workshop.iron_rod"), B2,
                (MachineCatalog.Press, "press.screws"), B2, (MachineCatalog.IndustrialContainer, null));
            Chain(-14f, GameItems.Copper, (Chest, null), B1, B1, (MachineCatalog.Smelter, null), B1, (MachineCatalog.Workshop, "workshop.copper_wire"), B1,
                (MachineCatalog.Press, "press.cable"), B1, (MachineCatalog.IndustrialContainer, null));
            Chain(-10.5f, GameItems.Copper, (Chest, null), B2, B2, (MachineCatalog.Splitter, null), B2, (MachineCatalog.Smelter, null), B2, (MachineCatalog.Merger, null), B2,
                (MachineCatalog.IndustrialContainer, null));
            // The splitter's right output: a belt into a crusher
            Put(MachineCatalog.BeltMk2, splitter.At + _right * 1.02f);
            Put(MachineCatalog.Crusher, splitter.At + _right * (1.54f + MachineCatalog.All.First(m => m.PartId == MachineCatalog.Crusher).SizeX * 0.5f));

            // Every other machine in packed rows to the right, row by row: power, production, top tier, extraction
            ushort[][] rows =
            {
                new[] { MachineCatalog.StellarReactor, MachineCatalog.StellarReactor, MachineCatalog.StellarReactor, MachineCatalog.CombustionGenerator, MachineCatalog.CombustionGenerator,
                    MachineCatalog.BiomassBurner, MachineCatalog.Accumulator, MachineCatalog.Accumulator },
                new[] { MachineCatalog.Assembler, MachineCatalog.Assembler, MachineCatalog.Fabricator, MachineCatalog.Fabricator, MachineCatalog.Foundry, MachineCatalog.Foundry,
                    MachineCatalog.Refinery, MachineCatalog.Refinery },
                new[] { MachineCatalog.Accelerator, MachineCatalog.QuantumEncoder, MachineCatalog.Converter, MachineCatalog.Recycler, MachineCatalog.ExchangeCounter,
                    MachineCatalog.WaterCondenser, MachineCatalog.WaterCondenser },
                new[] { MachineCatalog.Extractor, MachineCatalog.ExtractorMk2, MachineCatalog.DeepDrill, MachineCatalog.DeepDrill, MachineCatalog.RadarTower,
                    MachineCatalog.ConveyorLift, MachineCatalog.LoadingDock, MachineCatalog.IndustrialContainer }
            };
            var extractors = new List<Placed>();
            var rowEdges = new List<float>();
            float ahead = -11f;
            const float gridStart = -6.5f;
            float gridEnd = gridStart;
            foreach (ushort[] row in rows)
            {
                float depth = row.Max(s => Depth(s));
                float across = gridStart;
                foreach (ushort s in row)
                {
                    MachineDef def = MachineCatalog.All.First(m => m.PartId == s);
                    Placed p = Put(s, _right * (across + def.SizeX * 0.5f) + _forward * (ahead + depth * 0.5f));
                    if (def.Extracts)
                        extractors.Add(p);
                    across += def.SizeX + (def.Extracts ? 2.4f : 1.3f);
                }
                gridEnd = Mathf.Max(gridEnd, across);
                ahead += depth + (row == rows[2] ? 3.2f : 2.4f);
                rowEdges.Add(ahead - 1.2f);
            }
            float farAhead = ahead;
            // Pylons along the gaps between the rows, and one by the lines; every powered part is cabled to the nearest one
            var pylonList = new List<Placed>();
            for (int r = 0; r < rowEdges.Count - 1; r++)
            {
                for (float across = gridStart + 4f; across < gridEnd - 2f; across += 9f)
                    pylonList.Add(Put(Parts.PowerPoles.PoleMk3, _right * across + _forward * rowEdges[r]));
            }
            pylonList.Add(Put(Parts.PowerPoles.PoleMk3, _right * -8.3f + _forward * 0f));
            pylonList.Add(Put(Parts.PowerPoles.PoleMk3, _right * -8.3f + _forward * 8f));
            Put(Parts.PowerPoles.PoleMk1, _right * -8.3f - _forward * 8f);
            Put(Parts.PowerPoles.PoleMk2, _right * -8.3f + _forward * 4f);
            Placed[] pylons = pylonList.ToArray();

            var cableJson = new List<string>();
            var used = new Dictionary<int, int>();
            int cableId = 1;
            void Cable(Placed a, int socketA, Placed b, int socketB) =>
                cableJson.Add($"{{\"id\":{cableId++},\"type\":2,\"plugA\":{{\"shipId\":1,\"partId\":{a.Id},\"socketId\":{socketA}}},\"plugB\":{{\"shipId\":1,\"partId\":{b.Id},\"socketId\":{socketB}}}}}");
            int Free(Placed pole)
            {
                used.TryGetValue(pole.Id, out int n);
                used[pole.Id] = n + 1;
                return n;
            }
            for (int i = 0; i + 1 < pylons.Length; i++)
                Cable(pylons[i], Free(pylons[i]), pylons[i + 1], Free(pylons[i + 1]));
            foreach (Placed p in placed.Where(p => p.Def != null && p.Powered))
            {
                Placed pole = pylons.Where(y => !used.TryGetValue(y.Id, out int n) || n < 12).OrderBy(y => (y.At - p.At).sqrMagnitude).FirstOrDefault();
                if (pole == null)
                {
                    Log($"factory: no pylon socket left for {p.Def.Name}");
                    continue;
                }
                Cable(p, MachineSimulation.PowerSocket, pole, Free(pole));
            }
            Log($"factory: {placed.Count} parts, {cableJson.Count} cables, pylon sockets used {string.Join("/", pylons.Select(y => used.TryGetValue(y.Id, out int n) ? n : 0))}");

            // The ship: a deck just round the factory, walkways under the conveyor lines and along the pylons
            var hull = new StringBuilder();
            float minAcross = -23f, maxAcross = gridEnd + 1f, minAhead = -13f, maxAhead = farAhead + 0.5f;
            for (int x = -40; x <= 40; x++)
            {
                for (int z = -40; z <= 40; z++)
                {
                    Vector3 cell = new Vector3(x, 0f, z);
                    float across = Vector3.Dot(cell, _right), along = Vector3.Dot(cell, _forward);
                    if (across < minAcross || across > maxAcross || along < minAhead || along > maxAhead)
                        continue;
                    if (hull.Length > 0)
                        hull.Append(',');
                    bool lane = (across > -22.5f && across < -9.5f) || rowEdges.Any(e => Mathf.Abs(along - e) < 0.6f);
                    string front = lane ? "{\"x\":176,\"y\":140,\"z\":70}" : "{\"x\":120,\"y\":126,\"z\":134}";
                    hull.Append($"{{\"partId\":{IronFloor},\"x\":{x},\"y\":0,\"z\":{z},\"forwardOrientationId\":4,\"upOrientationId\":0,\"voxelPartTypeId\":0,\"FrontColor\":{front},\"BackColor\":{{\"x\":90,\"y\":90,\"z\":90}}}}");
                }
            }
            string parts = string.Join(",", placed.Select(p => string.Format(CultureInfo.InvariantCulture,
                "{{\"id\":{0},\"settingsId\":{1},\"coord\":{{\"x\":{2},\"y\":-0.46,\"z\":{3}}},\"forwardOrientationId\":2,\"upOrientationId\":4}}", p.Id, p.Settings, p.At.x, p.At.z)));
            var sb = new StringBuilder();
            sb.Append("{\"rootShipId\":1,\"cables\":[").Append(string.Join(",", cableJson)).Append("],\"ships\":[{");
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
            state.Position = player.Position + up * 3f + player.Rotation * Vector3.forward * 40f;
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
                var sbk = b.EntityPartSocket; sbk.EntityId = state.Id; b.EntityPartSocket = sbk;
                c.PlugA = a;
                c.PlugB = b;
                cables.AddCable(c);
            }
            uint shipId = state.Id;
            Log($"factory ship {shipId} loaded");
            yield return Wait(2f);
            TrackedShipServer ship = ships.GetShip(shipId);
            if (ship == null)
            {
                Log("FAIL factory ship missing");
                yield break;
            }

            // Fill: the lines' chests, every machine with ten cycles of its first recipe, the generators with fuel
            foreach (Placed p in placed)
            {
                if (p.Settings == Chest)
                {
                    uint ore = chests.TryGetValue(p.Id, out uint o) ? o : GameItems.Iron;
                    ship.UpdatePartState((ushort)p.Id, new StorageState { InventorySlots = Slots(Enumerable.Repeat((ore, 100), 10).ToArray()) });
                    continue;
                }
                if (p.Def == null)
                    continue;
                (uint Item, int Count)[] fill = null;
                if (p.Def.PartId == MachineCatalog.StellarReactor)
                    fill = new[] { (ResourceCatalog.StellarFuelRod, 6) };
                else if (p.Def.Generates)
                    fill = new[] { (ResourceCatalog.BiomassBrick, 60) };
                else if (p.Def.Sinks)
                    fill = new[] { (GameItems.Iron, 100), (GameItems.Iron, 100) };
                else if (p.Def.IsLogistics || p.Def.HasNoRecipe)
                    fill = null;
                else if (lineParts.Contains(p))
                    fill = null;
                else
                {
                    RecipeDef recipe = RecipeBook.Available(p.Def.PartId).FirstOrDefault();
                    if (recipe != null)
                        fill = recipe.Inputs.Select(i => (i.Item, Mathf.Min(i.Count * 10, IndustrialItems.MaxStack(i.Item)))).ToArray();
                }
                if (fill != null)
                    ship.UpdatePartState((ushort)p.Id, new StorageState { InventorySlots = Slots(fill) });
            }
            foreach ((Placed part, string recipe) in choices)
                MachineControl.SetChoice(shipId, (ushort)part.Id, recipe);

            // Three rocks with the game's own rock mesh, one beside each extractor
            yield return Wait(1f);
            Transform frame = ShipVisual(shipId);
            MeshFilter rockLook = Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None)
                .FirstOrDefault(f => f.sharedMesh != null && f.sharedMesh.name.StartsWith("RockSmall") && f.GetComponent<MeshRenderer>() != null);
            uint[] ores = { GameItems.Iron, GameItems.Copper, ResourceCatalog.Coal, ResourceCatalog.Sulfur };
            for (int i = 0; i < extractors.Count && frame != null; i++)
            {
                Placed e = extractors[i];
                var rock = new GameObject("SI_FactoryRock" + i);
                rock.transform.SetParent(frame, false);
                rock.transform.localPosition = e.At + new Vector3(0f, -0.46f, 0f) - _forward * (e.Def.SizeZ * 0.5f + 1.1f) + Vector3.up * 0.55f;
                rock.transform.localRotation = Quaternion.Euler(i * 40f, i * 70f, 0f);
                if (rockLook != null)
                {
                    var look = new GameObject("RockLook");
                    look.transform.SetParent(rock.transform, false);
                    look.transform.localScale = Vector3.one * 0.85f;
                    look.AddComponent<MeshFilter>().sharedMesh = rockLook.sharedMesh;
                    look.AddComponent<MeshRenderer>().sharedMaterial = rockLook.GetComponent<MeshRenderer>().sharedMaterial;
                }
                var minable = rock.AddComponent<Planets.Objects.MinablePlanetObject>();
                minable.dropRate = new Planets.Objects.Common.Model.ItemDropRate { item = IndustrialItems.Find(ores[i % ores.Length]), amountMin = 1, amountMax = 2 };
            }
            Deposits.Rescan();
            Log("factory: filled, rocks set; running");

            // Let it run, then photograph from above
            yield return Wait(25f);
            Log("factory: " + FactoryOverview.Build());
            frame = ShipVisual(shipId);
            if (frame == null)
            {
                Log("FAIL factory ship not shown");
                yield break;
            }
            var cameraObject = new GameObject("SI_FactoryCamera");
            var camera = cameraObject.AddComponent<Camera>();
            if (Camera.main != null)
                camera.CopyFrom(Camera.main);
            camera.enabled = false;
            camera.fieldOfView = 42f;
            camera.nearClipPlane = 0.2f;
            camera.farClipPlane = 4000f;
            // Framed on the factory: its middle, seen from each corner at about 35 degrees, close enough to fill the picture
            Vector3 middle = _right * ((minAcross + maxAcross) * 0.5f) + _forward * ((minAhead + maxAhead) * 0.5f) + Vector3.up * 1f;
            float reach = Mathf.Max(maxAcross - minAcross, maxAhead - minAhead) * 0.62f;
            Vector3 Eye(float a, float b, float height) => middle + (_right * a + _forward * b).normalized * reach + Vector3.up * height;
            (string Name, Vector3 Eye, Vector3 Look, int W, int H)[] shots =
            {
                ("factory_a", Eye(-0.8f, -1f, reach * 0.62f), middle, 1920, 1080),
                ("factory_b", Eye(1f, -0.7f, reach * 0.55f), middle, 1920, 1080),
                ("factory_c", Eye(-1f, 0.8f, reach * 0.6f), middle, 1920, 1080),
                ("factory_d", Eye(0.9f, 1f, reach * 0.5f), middle, 1920, 1080),
                ("factory_e", middle + (-_right * 0.4f - _forward).normalized * reach * 0.55f + Vector3.up * reach * 0.25f, middle - _forward * 2f, 1920, 1080)
            };
            Directory.CreateDirectory(Folder);
            foreach (var shot in shots)
            {
                var target = new RenderTexture(shot.W, shot.H, 24, RenderTextureFormat.ARGB32) { antiAliasing = 8 };
                camera.targetTexture = target;
                camera.transform.position = frame.TransformPoint(shot.Eye);
                camera.transform.rotation = Quaternion.LookRotation(frame.TransformPoint(shot.Look) - camera.transform.position, frame.up);
                camera.Render();
                RenderTexture previous = RenderTexture.active;
                RenderTexture.active = target;
                var picture = new Texture2D(shot.W, shot.H, TextureFormat.RGB24, false);
                picture.ReadPixels(new Rect(0, 0, shot.W, shot.H), 0, 0);
                picture.Apply();
                RenderTexture.active = previous;
                File.WriteAllBytes(Path.Combine(Folder, shot.Name + ".png"), picture.EncodeToPNG());
                camera.targetTexture = null;
                Object.Destroy(picture);
                target.Release();
                Object.Destroy(target);
                yield return null;
            }
            Object.Destroy(cameraObject);
            Log($"factory: {shots.Length} pictures written");
        }
    }
}
