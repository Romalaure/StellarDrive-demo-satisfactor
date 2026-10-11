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

namespace StellarIndustries.UI
{
    /// <summary>
    /// A small panel under the crosshair when the player looks at a machine: state, recipe,
    /// progress, power, contents. The host reads the simulation itself; a client (which does not
    /// simulate) shows what the machine's synchronized slots allow it to know. Looked up ten
    /// times a second, drawn only while a machine is aimed at and no menu shows the cursor.
    /// </summary>
    internal static class MachineHud
    {
        private static readonly Stack[] Slots = new Stack[10];
        private static readonly StringBuilder Text = new StringBuilder();
        private static float _nextLook;
        private static string _shown;
        private static GUIStyle _style;

        public static void Update()
        {
            if (Time.unscaledTime < _nextLook)
                return;
            _nextLook = Time.unscaledTime + 0.1f;
            _shown = null;
            if (!Settings.MachineHud.Value || Cursor.visible)
                return;
            if (!ShipAim.TryGetAimedPart(Settings.MachineHudDistance.Value, out IShipStateRead ship, out StatefulPart part))
                return;
            if (part?.Settings == null || !MachineCatalog.TryGet(part.Settings.id, out MachineDef def) || !(part.State is StorageState state))
                return;
            _shown = Describe(ship.Id, part.Id, def, state);
        }

        private static string Describe(uint shipId, ushort partId, MachineDef def, StorageState state)
        {
            MachineSimulation.Read(state, Slots);
            Text.Clear();
            Text.Append("<b>").Append(def.Name).Append("</b>\n");
            MachineSimulation.Runtime rt = InstanceFinder.IsServerStarted ? MachineSimulation.Get(shipId, partId) : null;
            if (rt != null)
            {
                Text.Append(StatusText(rt.Status));
                if (rt.Recipe != null)
                {
                    Text.Append("\nRecette : ").Append(rt.Recipe.Name).Append(" (").Append(Mathf.FloorToInt(Mathf.Clamp01(rt.Progress) * 100f)).Append(" %)");
                    Text.Append("\nÉnergie : ").Append(Mathf.RoundToInt(rt.Watts * rt.PowerRate)).Append(" / ").Append(Mathf.RoundToInt(rt.Watts)).Append(" W");
                }
                if (!Settings.Enabled.Value)
                    Text.Append("\n(production désactivée dans les réglages)");
            }
            else
            {
                // A client sees the slots only; power and progress are the host's
                RecipeDef recipe = RecipeSelector.Pick(Slots, RecipeBook.ForMachine(def.PartId), null, IndustrialItems.MaxStack, out MachineStatus status);
                Text.Append(recipe != null ? "Prête : " + recipe.Name + " (si alimentée)" : StatusText(status));
            }
            foreach (Stack s in Slots)
            {
                if (!s.IsEmpty)
                    Text.Append("\n  ").Append(s.Count).Append(" × ").Append(IndustrialItems.NameOf(s.Item));
            }
            return Text.ToString();
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
