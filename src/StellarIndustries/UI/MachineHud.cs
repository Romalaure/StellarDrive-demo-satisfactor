using System;
using System.Text;
using FishNet;
using SDModKit.Game;
using Ships.Interface.Model.Parts;
using Ships.Interface.Model.Parts.State;
using Ships.Interface.Model.State;
using StellarIndustries.Content;
using StellarIndustries.Core;
using StellarIndustries.Production;
using StellarIndustries.Recipes;
using UnityEngine;
using UnityEngine.InputSystem;

namespace StellarIndustries.UI
{
    /// <summary>
    /// A small panel beside the crosshair when the player looks at a machine: state, recipe,
    /// progress, power, contents, and the key that changes its recipe. The host reads the
    /// simulation itself; a client asks the host twice a second for the aimed machine's report
    /// (clients do not simulate). Looked up ten times a second, drawn only while a machine is
    /// aimed at and no menu shows the cursor.
    /// </summary>
    internal static class MachineHud
    {
        private static readonly Stack[] Slots = new Stack[10];
        private static readonly StringBuilder Text = new StringBuilder();
        private static float _nextLook, _nextAsk;
        private static string _shown;
        private static GUIStyle _style;
        private static (uint Ship, ushort Part) _aimed;
        private static MachineDef _aimedDef;
        private static Key _key = Key.None;
        private static string _keyName;

        /// <summary>Development: a machine to describe as if aimed at (in-game test screenshots).</summary>
        public static (uint Ship, ushort Part)? Forced;

        public static void Update()
        {
            HandleKey();
            if (Time.unscaledTime < _nextLook)
                return;
            _nextLook = Time.unscaledTime + 0.1f;
            _shown = null;
            _aimedDef = null;
            if (!Settings.MachineHud.Value || (Cursor.visible && Forced == null))
                return;
            IShipStateRead ship;
            StatefulPart part;
            if (Forced != null)
            {
                part = null;
                ship = null;
                Ships.Services.ShipsClientTracker client = GameRefs.Service<Ships.Services.ShipsClientTracker>();
                if (client == null || !client.TryGetShip(Forced.Value.Ship, out ship) || ship == null || !ship.TryGetStatefulPart(Forced.Value.Part, out part))
                    return;
            }
            else if (!ShipAim.TryGetAimedPart(Settings.MachineHudDistance.Value, out ship, out part))
            {
                return;
            }
            if (part?.Settings == null || !MachineCatalog.TryGet(part.Settings.id, out MachineDef def) || !(part.State is StorageState state))
                return;
            _aimed = (ship.Id, part.Id);
            _aimedDef = def;
            if (!InstanceFinder.IsServerStarted && Time.unscaledTime >= _nextAsk)
            {
                _nextAsk = Time.unscaledTime + 0.5f;
                SINet.SendToServer(SIMessageKind.StatusRequest, ship.Id, part.Id, "");
            }
            _shown = Describe(ship.Id, part.Id, def, state);
        }

        private static void HandleKey()
        {
            if (_key == Key.None || _keyName != Settings.RecipeKey.Value)
            {
                _keyName = Settings.RecipeKey.Value;
                if (!Enum.TryParse(_keyName?.Trim(), true, out _key) || _key == Key.None)
                    _key = Key.N;
            }
            Keyboard keyboard = Keyboard.current;
            if (_aimedDef == null || keyboard == null || Cursor.visible || !keyboard[_key].wasPressedThisFrame)
                return;
            bool back = keyboard.shiftKey.isPressed;
            if (_aimedDef.Dock)
            {
                // Off -> send -> receive -> off
                string[] modes = { "", Logistics.LoadingDocks.Send, Logistics.LoadingDocks.Receive };
                string current = InstanceFinder.IsServerStarted ? MachineControl.Choices.Get(_aimed.Ship, _aimed.Part) : MachineControl.ClientReport(_aimed.Ship, _aimed.Part, 5f)?.ChosenId;
                int index = Math.Max(0, Array.IndexOf(modes, current ?? ""));
                SINet.SendToServer(SIMessageKind.SetRecipe, _aimed.Ship, _aimed.Part, modes[(index + (back ? 2 : 1)) % 3]);
                _nextLook = 0f;
                return;
            }
            RecipeDef[] recipes = RecipeBook.Available(_aimedDef.PartId);
            if (recipes.Length < 2)
                return;
            RecipeDef chosen = CurrentChoice(_aimed.Ship, _aimed.Part, _aimedDef);
            RecipeDef next = RecipeSelector.Next(recipes, chosen, back ? -1 : 1);
            SINet.SendToServer(SIMessageKind.SetRecipe, _aimed.Ship, _aimed.Part, next?.Id ?? "");
            _nextLook = 0f;
        }

        private static RecipeDef CurrentChoice(uint ship, ushort part, MachineDef def)
        {
            if (InstanceFinder.IsServerStarted)
                return MachineControl.Chosen(ship, part, def.PartId);
            MachineReport? report = MachineControl.ClientReport(ship, part, 5f);
            return report?.ChosenId != null ? RecipeBook.Get(report.Value.ChosenId) : null;
        }

        private static string Describe(uint shipId, ushort partId, MachineDef def, StorageState state)
        {
            MachineSimulation.Read(state, Slots);
            Text.Clear();
            Text.Append("<b>").Append(Lang.MachineName(def)).Append("</b>\n");
            // What it is for, and what it can make
            string role = Lang.MachineRole(def);
            if (!string.IsNullOrEmpty(role))
                Text.Append("<i><color=#9cd>").Append(role).Append("</color></i>\n");
            RecipeDef[] recipes = RecipeBook.Available(def.PartId);
            string makes = Products(def, recipes);
            if (makes != null)
                Text.Append("<color=#aaa>").Append(Lang.T("Produit : ", "Makes: ")).Append(makes).Append("</color>\n");
            MachineReport? report = InstanceFinder.IsServerStarted ? MachineControl.HostReport(shipId, partId) : MachineControl.ClientReport(shipId, partId, 2f);
            if (report != null)
            {
                MachineReport r = report.Value;
                if (def.IsLogistics)
                    Text.Append(def.Dock ? (r.Status == MachineStatus.Producing ? Lang.T("<color=#6f8>Transfert en cours</color>", "<color=#6f8>Transferring</color>") : Lang.T("<color=#9aa>Pas de transfert</color>", "<color=#9aa>Not transferring</color>"))
                        : def.IsConveyor ? (r.Status == MachineStatus.Producing ? Lang.T("<color=#6f8>Transport en cours</color>", "<color=#6f8>Carrying items</color>") : Lang.T("<color=#9aa>À l'arrêt (rien à prendre ou sortie pleine)</color>", "<color=#9aa>Stopped (nothing to take or output full)</color>"))
                        : Lang.T("<color=#9aa>Stockage</color>", "<color=#9aa>Storage</color>"));
                else
                    Text.Append(StatusText(r.Status));
                RecipeDef running = r.RecipeId != null ? RecipeBook.Get(r.RecipeId) : null;
                if (running != null)
                {
                    Text.Append(Lang.T("\nRecette : ", "\nRecipe: ")).Append(Lang.RecipeName(running)).Append(" (").Append(Mathf.FloorToInt(Mathf.Clamp01(r.Progress) * 100f)).Append(" %)");
                    Text.Append("\n  ").Append(Describe(running));
                    Text.Append(Lang.T("\nÉnergie : ", "\nPower: ")).Append(Mathf.RoundToInt(r.Watts * r.PowerRate)).Append(" / ").Append(Mathf.RoundToInt(r.Watts)).Append(" W");
                }
                if (running == null && def.Generates)
                    Text.Append(Lang.T("\nProduction : ", "\nOutput: ")).Append(Mathf.RoundToInt(r.Watts * r.PowerRate)).Append(" / ").Append(Mathf.RoundToInt(r.Watts)).Append(" W");
                if (r.Detail != null)
                    Text.Append('\n').Append(r.Detail);
                if (recipes.Length > 1)
                {
                    RecipeDef chosen = r.ChosenId != null ? RecipeBook.Get(r.ChosenId) : null;
                    Text.Append(Lang.T("\nMode : ", "\nMode: ")).Append(chosen != null ? "<color=#8cf>" + Lang.RecipeName(chosen) + Lang.T(" uniquement</color>", " only</color>") : Lang.T("automatique", "automatic"));
                }
                if (!Settings.Enabled.Value && InstanceFinder.IsServerStarted)
                    Text.Append(Lang.T("\n(production désactivée dans les réglages)", "\n(production turned off in the settings)"));
            }
            else
            {
                Text.Append(Lang.T("<color=#9aa>En attente de l'hôte...</color>", "<color=#9aa>Waiting for the host...</color>"));
            }
            foreach (Stack s in Slots)
            {
                if (!s.IsEmpty)
                    Text.Append("\n  ").Append(s.Count).Append(" × ").Append(IndustrialItems.NameOf(s.Item));
            }
            if (def.Dock)
                Text.Append("\n<color=#aaa>[").Append(_key == Key.None ? "N" : _key.ToString()).Append(Lang.T("] arrêt / envoi / réception</color>", "] off / send / receive</color>"));
            if (recipes.Length > 1)
                Text.Append("\n<color=#aaa>[").Append(_key == Key.None ? "N" : _key.ToString()).Append(Lang.T("] recette suivante, Maj+[", "] next recipe, Shift+[")).Append(_key == Key.None ? "N" : _key.ToString()).Append(Lang.T("] précédente</color>", "] previous</color>"));
            return Text.ToString();
        }

        /// <summary>The products of a machine's recipes, the first few, and how many more wait for a research.</summary>
        private static string Products(MachineDef def, RecipeDef[] available)
        {
            if (def.HasNoRecipe)
                return null;
            var names = new System.Collections.Generic.List<string>();
            foreach (RecipeDef r in available)
            {
                string name = IndustrialItems.NameOf(r.Outputs[0].Item);
                if (!names.Contains(name))
                    names.Add(name);
            }
            if (names.Count == 0)
                return null;
            var text = new StringBuilder(string.Join(", ", names.GetRange(0, Math.Min(5, names.Count))));
            if (names.Count > 5)
                text.Append(Lang.T($" et {names.Count - 5} autre(s)", $" and {names.Count - 5} more"));
            int locked = RecipeBook.ForMachine(def.PartId).Length - available.Length;
            if (locked > 0)
                text.Append(Lang.T($" (+{locked} après recherche)", $" (+{locked} after research)"));
            return text.ToString();
        }

        private static string Describe(RecipeDef recipe)
        {
            var text = new StringBuilder();
            for (int i = 0; i < recipe.Inputs.Length; i++)
                text.Append(i > 0 ? " + " : "").Append(recipe.Inputs[i].Count).Append(' ').Append(IndustrialItems.NameOf(recipe.Inputs[i].Item));
            text.Append(" → ");
            for (int i = 0; i < recipe.Outputs.Length; i++)
                text.Append(i > 0 ? " + " : "").Append(recipe.Outputs[i].Count).Append(' ').Append(IndustrialItems.NameOf(recipe.Outputs[i].Item));
            return text.ToString();
        }

        public static string StatusText(MachineStatus status)
        {
            switch (status)
            {
                case MachineStatus.Idle: return Lang.T("<color=#9aa>Vide</color>", "<color=#9aa>Empty</color>");
                case MachineStatus.WaitingInputs: return Lang.T("<color=#fc6>En attente de ressources</color>", "<color=#fc6>Waiting for materials</color>");
                case MachineStatus.OutputBlocked: return Lang.T("<color=#f84>Sortie bloquée (plus de place)</color>", "<color=#f84>Output blocked (no room left)</color>");
                case MachineStatus.Unpowered: return Lang.T("<color=#f66>Hors tension (aucun câble)</color>", "<color=#f66>Unpowered (no cable)</color>");
                case MachineStatus.NoPower: return Lang.T("<color=#f66>Pas d'énergie sur le réseau</color>", "<color=#f66>No power on the grid</color>");
                case MachineStatus.Underpowered: return Lang.T("<color=#fd6>En production (énergie insuffisante)</color>", "<color=#fd6>Producing (not enough power)</color>");
                case MachineStatus.Producing: return Lang.T("<color=#6f8>En production</color>", "<color=#6f8>Producing</color>");
                case MachineStatus.NoDeposit: return Lang.T("<color=#f84>Aucun gisement à portée</color>", "<color=#f84>No deposit in reach</color>");
                case MachineStatus.NoFuel: return Lang.T("<color=#f84>Plus de combustible</color>", "<color=#f84>Out of fuel</color>");
                case MachineStatus.Standby: return Lang.T("<color=#9cf>En veille (le réseau ne demande rien)</color>", "<color=#9cf>Standby (the grid needs nothing)</color>");
                default: return Lang.T("<color=#f44>Erreur</color>", "<color=#f44>Error</color>");
            }
        }

        public static void OnGUI()
        {
            if (_shown == null)
                return;
            if (_style == null)
                _style = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.UpperLeft, richText = true, fontSize = 15, wordWrap = false, padding = new RectOffset(10, 10, 8, 8) };
            var content = new GUIContent(_shown);
            Vector2 size = _style.CalcSize(content);
            GUI.Box(new Rect(Screen.width * 0.5f + 40f, Screen.height * 0.5f - size.y * 0.5f, size.x, size.y), content, _style);
        }
    }
}
