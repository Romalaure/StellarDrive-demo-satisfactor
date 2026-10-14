using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Core.Services;
using FishNet.Connection;
using HarmonyLib;
using Saving.Interface.Model;
using Saving.Services;
using Ships;
using Ships.Interface.Model.Parts;
using Ships.Services;
using StellarIndustries.Core;
using StellarIndustries.Recipes;
using UnityEngine;

namespace StellarIndustries.Production
{
    /// <summary>
    /// The players' control over the machines: the recipe chosen for each one (host side, kept in
    /// the companion file <see cref="FileName"/> beside the world save) and the machine reports
    /// a client asks the host for when it looks at a machine.
    /// </summary>
    internal static class MachineControl
    {
        public const string FileName = "stellar-industries.txt";

        public static readonly MachineChoices Choices = new MachineChoices();

        private static readonly Dictionary<(uint, ushort), (MachineReport Report, float Time)> Reports = new Dictionary<(uint, ushort), (MachineReport, float)>();

        public static void Install()
        {
            SINet.OnServer(SIMessageKind.SetRecipe, OnSetRecipe);
            SINet.OnServer(SIMessageKind.StatusRequest, OnStatusRequest);
            SINet.OnClient(SIMessageKind.Status, (ship, part, text) =>
            {
                if (MachineReport.TryDecode(text, out MachineReport report))
                    Reports[(ship, part)] = (report, Time.unscaledTime);
            });
        }

        public static void Forget() => Reports.Clear();

        /// <summary>The recipe a player chose for this machine, if it still exists and is valid for it.</summary>
        public static RecipeDef Chosen(uint ship, ushort part, ushort machine)
        {
            string id = Choices.Get(ship, part);
            return id == null ? null : RecipeBook.ForMachine(machine).FirstOrDefault(r => r.Id == id);
        }

        /// <summary>Host: sets a machine's recipe (null = automatic). Returns false if refused.</summary>
        public static bool SetChoice(uint shipId, ushort partId, string recipeId)
        {
            ShipsServerTracker ships = ServiceLocator.GetService<ShipsServerTracker>();
            TrackedShipServer ship = ships?.GetShip(shipId);
            if (ship == null || !ship.TryGetStatefulPart(partId, out StatefulPart part) || part?.Settings == null ||
                !MachineCatalog.TryGet(part.Settings.id, out MachineDef def))
                return false;
            if (def.Dock)
            {
                if (!string.IsNullOrEmpty(recipeId) && recipeId != Logistics.LoadingDocks.Send && recipeId != Logistics.LoadingDocks.Receive)
                    return false;
            }
            else if (!string.IsNullOrEmpty(recipeId) && RecipeBook.ForMachine(def.PartId).All(r => r.Id != recipeId))
            {
                return false;
            }
            Choices.Set(shipId, partId, recipeId);
            MachineSimulation.Restart(shipId, partId);
            IndustriesMod.Log.Msg($"machine {shipId}/{partId} ({def.Name}): recipe {(string.IsNullOrEmpty(recipeId) ? "automatic" : recipeId)}");
            return true;
        }

        private static void OnSetRecipe(NetworkConnection sender, uint ship, ushort part, string recipeId)
        {
            SetChoice(ship, part, recipeId);
            OnStatusRequest(sender, ship, part, "");
        }

        private static void OnStatusRequest(NetworkConnection sender, uint ship, ushort part, string text)
        {
            MachineReport? report = HostReport(ship, part);
            if (report == null)
                return;
            if (sender == null || sender.IsLocalClient)
                Reports[(ship, part)] = (report.Value, Time.unscaledTime);
            else
                SINet.SendTo(sender, SIMessageKind.Status, ship, part, report.Value.Encode());
        }

        /// <summary>Host: the machine's report from the simulation (null if not simulated).</summary>
        public static MachineReport? HostReport(uint ship, ushort part)
        {
            MachineSimulation.Runtime rt = MachineSimulation.Get(ship, part);
            if (rt == null)
                return null;
            return new MachineReport
            {
                Status = rt.Status, RecipeId = rt.Recipe?.Id, ChosenId = Choices.Get(ship, part), Progress = rt.Progress,
                Watts = rt.Watts, PowerRate = rt.PowerRate, Detail = rt.Detail
            };
        }

        /// <summary>Client: the last report the host sent for this machine (null if none or too old).</summary>
        public static MachineReport? ClientReport(uint ship, ushort part, float maxAge)
        {
            if (Reports.TryGetValue((ship, part), out var entry) && Time.unscaledTime - entry.Time <= maxAge)
                return entry.Report;
            return null;
        }

        // ---- Companion file

        public static void Load(string worldDirectory)
        {
            UI.ScannerModules.Load(worldDirectory);
            Choices.Clear();
            Reports.Clear();
            if (string.IsNullOrEmpty(worldDirectory))
                return;
            string path = Path.Combine(worldDirectory, FileName);
            if (!File.Exists(path))
                return;
            int bad = Choices.Parse(File.ReadAllText(path));
            IndustriesMod.Log.Msg($"{Choices.Count} machine recipe choice(s) read from {FileName}" + (bad > 0 ? $", {bad} unreadable line(s) skipped" : ""));
        }

        public static void Save(string worldDirectory)
        {
            if (string.IsNullOrEmpty(worldDirectory) || !Directory.Exists(worldDirectory))
                return;
            UI.ScannerModules.Save(worldDirectory);
            MachineSimulation.KeepCharges();
            ShipsServerTracker ships = ServiceLocator.GetService<ShipsServerTracker>();
            int pruned = Choices.Prune((shipId, partId) =>
            {
                TrackedShipServer ship = ships?.GetShip(shipId);
                if (ship == null)
                    return null;
                return ship.TryGetStatefulPart(partId, out StatefulPart part) && part?.Settings != null && MachineCatalog.TryGet(part.Settings.id, out _);
            });
            string path = Path.Combine(worldDirectory, FileName);
            if (Choices.Count == 0 && !File.Exists(path))
                return;
            // Written beside, then swapped: a crash while writing never leaves half a file
            string temp = path + ".tmp";
            File.WriteAllText(temp, Choices.Serialize());
            if (File.Exists(path))
                File.Delete(path);
            File.Move(temp, path);
            Choices.Changed = false;
            IndustriesMod.Log.Msg($"{Choices.Count} machine recipe choice(s) saved" + (pruned > 0 ? $" ({pruned} removed machine(s) forgotten)" : ""));
        }
    }

    /// <summary>The recipe choices are written next to the world save, right after the game wrote it.</summary>
    [HarmonyPatch(typeof(GameSaver), "SaveGame", typeof(string))]
    internal static class MachineControlSavePatch
    {
        private static void Postfix(string worldDirectoryFullName)
        {
            try
            {
                MachineControl.Save(worldDirectoryFullName);
            }
            catch (Exception e)
            {
                ErrorLog.Report("save recipe choices", e);
            }
        }
    }

    /// <summary>... and read back before the game loads that world.</summary>
    [HarmonyPatch(typeof(GameLoader), nameof(GameLoader.LoadState))]
    internal static class MachineControlLoadPatch
    {
        private static void Prefix(ISavedGameFile fileEntry)
        {
            try
            {
                MachineControl.Load(fileEntry?.SaveFileInfo?.Directory?.FullName);
            }
            catch (Exception e)
            {
                ErrorLog.Report("load recipe choices", e);
            }
        }
    }
}
