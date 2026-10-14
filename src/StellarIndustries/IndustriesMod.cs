using System;
using System.Linq;
using System.Runtime.CompilerServices;
using MelonLoader;
using StellarIndustries;
using StellarIndustries.Content;
using StellarIndustries.Core;
using StellarIndustries.Production;
using StellarIndustries.Recipes;

[assembly: MelonInfo(typeof(IndustriesMod), "Stellar Industries", "0.16.0", "Romalaure")]
[assembly: MelonGame("CuriousOwlGames", "StellarDrive")]
[assembly: MelonOptionalDependencies("SDModKit")]
[assembly: HarmonyDontPatchAll]

namespace StellarIndustries
{
    public class IndustriesMod : MelonMod
    {
        internal static MelonLogger.Instance Log { get; private set; }

        private static bool _partsRegistered;
        private static bool _inGame;

        public override void OnInitializeMelon()
        {
            Log = LoggerInstance;
            Settings.Load();
            Lang.Refresh(force: true);
            Lang.Changed += OnLanguageChanged;
            ValidateContent();
            IndustrialItems.Create();
            SINet.InstallSerializers();
            MachineControl.Install();
            Diagnostics.FactoryOverview.Install();
            MachineActivity.Install();
            UI.ScannerModules.Install();
            ApplyPatches();
            Log.Msg("ready");
        }

        /// <summary>Checks the resources, machines and recipes; recipes with an error are switched off.</summary>
        private static void ValidateContent()
        {
            var issues = RecipeValidator.Validate(RecipeBook.All, MachineCatalog.All, ResourceCatalog.All);
            foreach (ValidationIssue issue in issues)
            {
                if (issue.IsError)
                    Log.Error("content: " + issue.Message);
                else
                    Log.Warning("content: " + issue.Message);
            }
            RecipeBook.Disable(issues.Where(i => i.IsError).Select(i => i.RecipeId));
            Log.Msg($"content: {ResourceCatalog.All.Length} resources, {MachineCatalog.All.Length} machines, {RecipeBook.All.Length} recipes, " +
                    $"{issues.Count(i => i.IsError)} error(s), {issues.Count(i => !i.IsError)} warning(s)");
        }

        // One patch class at a time: if a game update breaks one, the others (and the game) keep working
        private void ApplyPatches()
        {
            Type[] types;
            try
            {
                types = typeof(IndustriesMod).Assembly.GetTypes();
            }
            catch (System.Reflection.ReflectionTypeLoadException e)
            {
                types = e.Types.Where(t => t != null).ToArray();
            }
            int applied = 0, failed = 0;
            foreach (Type type in types.Where(t => t.GetCustomAttributes(typeof(HarmonyLib.HarmonyPatch), false).Length > 0))
            {
                try
                {
                    HarmonyInstance.CreateClassProcessor(type).Patch();
                    applied++;
                }
                catch (Exception e)
                {
                    failed++;
                    Log.Error($"patch {type.Name} not applied, that feature is off: {e.GetBaseException().Message}");
                }
            }
            Log.Msg($"{applied} patch(es) applied" + (failed > 0 ? $", {failed} failed" : ""));
        }

        public override void OnLateInitializeMelon()
        {
            // Every part of this mod is a clone of a game part made by SDModKit
            if (AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetName().Name == "SDModKit"))
                RegisterParts();
            else
                Log.Warning("SDModKit is not installed: the machines cannot be built (the items still exist)");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void RegisterParts()
        {
            try
            {
                Parts.MachineParts.Register();
                _partsRegistered = true;
            }
            catch (Exception e)
            {
                Log.Error("could not register the machines: " + e);
            }
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            MachineSimulation.Forget();
            MachineControl.Forget();
            Deposits.Forget();
            Logistics.LoadingDocks.Forget();
            Logistics.Conveyors.Forget();
            if (_partsRegistered)
                ForgetDeposits();
            Diagnostics.FactoryOverview.Forget();
            MachineActivity.Forget();
            if (_partsRegistered)
                StopSound();
            _inGame = sceneName == "MainGame";
            if (_inGame && _partsRegistered)
                InstallConveyors();
            if (_partsRegistered)
                EnsureResearch();
            Diagnostics.DevTest.OnSceneLoaded(sceneName);
        }

        public override void OnUpdate()
        {
            SINet.Update();
            Lang.Refresh();
            if (_inGame)
                MachineActivity.Update();
            if (_inGame && _partsRegistered)
                UpdateHud();
            if (Diagnostics.DevTest.Active)
                Diagnostics.DevTest.Update();
        }

        public override void OnGUI()
        {
            if (_inGame && _partsRegistered)
            {
                UI.MachineHud.OnGUI();
                UI.DepositScanner.OnGUI();
                Diagnostics.FactoryOverview.OnGUI();
            }
        }

        private static void OnLanguageChanged()
        {
            try
            {
                IndustrialItems.Rename();
                Wiki.WikiPages.Reset();
                if (_partsRegistered)
                    RenameParts();
                Log.Msg("language: " + (Lang.English ? "English" : "French"));
            }
            catch (Exception e)
            {
                ErrorLog.Report("language", e);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void RenameParts()
        {
            Parts.MachineParts.Rename();
            Parts.IndustryResearch.Rename();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ForgetDeposits()
        {
            Parts.DepositDecor.Forget();
            UI.DepositScanner.Forget();
            UI.ScannerModules.Forget();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void StopSound()
        {
            try
            {
                UI.MachineSound.Stop();
            }
            catch (Exception e)
            {
                ErrorLog.Report("machine sound", e);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void EnsureResearch()
        {
            try
            {
                Parts.IndustryResearch.Forget();
                RecipeBook.TechDone = Parts.IndustryResearch.IsDone;
                Parts.IndustryResearch.Ensure();
                Parts.PortLayout.Arrange();
            }
            catch (Exception e)
            {
                Log.Error("research: " + e);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void InstallConveyors()
        {
            try
            {
                Logistics.ItemLinesBridge.TryInstall(HarmonyInstance);
            }
            catch (Exception e)
            {
                Log.Error("conveyors: Item Lines bridge not installed: " + e.GetBaseException().Message);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void UpdateHud()
        {
            try
            {
                Parts.IndustryResearch.Tick();
                UI.MachineHud.Update();
                UI.DepositScanner.Update();
                UI.ScannerModules.Update();
                Parts.DepositDecor.Update();
                Diagnostics.FactoryOverview.Update();
                UI.MachineSound.Update();
            }
            catch (Exception e)
            {
                ErrorLog.Report("machine hud", e);
            }
        }
    }
}
