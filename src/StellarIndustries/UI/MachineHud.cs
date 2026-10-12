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
            RecipeDef[] recipes = RecipeBook.ForMachine(_aimedDef.PartId);
            if (recipes.Length < 2)
                return;
            RecipeDef chosen = CurrentChoice(_aimed.Ship, _aimed.Part, _aimedDef);
            bool back = keyboard.shiftKey.isPressed;
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
            Text.Append("<b>").Append(def.Name).Append("</b>\n");
            MachineReport? report = InstanceFinder.IsServerStarted ? MachineControl.HostReport(shipId, partId) : MachineControl.ClientReport(shipId, partId, 2f);
            RecipeDef[] recipes = RecipeBook.ForMachine(def.PartId);
            if (report != null)
            {
                MachineReport r = report.Value;
                Text.Append(StatusText(r.Status));
                RecipeDef running = r.RecipeId != null ? RecipeBook.Get(r.RecipeId) : null;
                if (running != null)
                {
                    Text.Append("\nRecette : ").Append(running.Name).Append(" (").Append(Mathf.FloorToInt(Mathf.Clamp01(r.Progress) * 100f)).Append(" %)");
                    Text.Append("\n  ").Append(Describe(running));
                    Text.Append("\nÉnergie : ").Append(Mathf.RoundToInt(r.Watts * r.PowerRate)).Append(" / ").Append(Mathf.RoundToInt(r.Watts)).Append(" W");
                }
                if (r.Detail != null)
                    Text.Append('\n').Append(r.Detail);
                if (recipes.Length > 1)
                {
                    RecipeDef chosen = r.ChosenId != null ? RecipeBook.Get(r.ChosenId) : null;
                    Text.Append("\nMode : ").Append(chosen != null ? "<color=#8cf>" + chosen.Name + " uniquement</color>" : "automatique");
                }
                if (!Settings.Enabled.Value && InstanceFinder.IsServerStarted)
                    Text.Append("\n(production désactivée dans les réglages)");
            }
            else
            {
                Text.Append("<color=#9aa>En attente de l'hôte...</color>");
            }
            foreach (Stack s in Slots)
            {
                if (!s.IsEmpty)
                    Text.Append("\n  ").Append(s.Count).Append(" × ").Append(IndustrialItems.NameOf(s.Item));
            }
            if (recipes.Length > 1)
                Text.Append("\n<color=#aaa>[").Append(_key == Key.None ? "N" : _key.ToString()).Append("] recette suivante, Maj+[").Append(_key == Key.None ? "N" : _key.ToString()).Append("] précédente</color>");
            return Text.ToString();
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
                case MachineStatus.Idle: return "<color=#9aa>Vide</color>";
                case MachineStatus.WaitingInputs: return "<color=#fc6>En attente de ressources</color>";
                case MachineStatus.OutputBlocked: return "<color=#f84>Sortie bloquée (plus de place)</color>";
                case MachineStatus.Unpowered: return "<color=#f66>Hors tension (aucun câble)</color>";
                case MachineStatus.NoPower: return "<color=#f66>Pas d'énergie sur le réseau</color>";
                case MachineStatus.Underpowered: return "<color=#fd6>En production (énergie insuffisante)</color>";
                case MachineStatus.Producing: return "<color=#6f8>En production</color>";
                case MachineStatus.NoDeposit: return "<color=#f84>Aucun gisement à portée</color>";
                default: return "<color=#f44>Erreur</color>";
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
