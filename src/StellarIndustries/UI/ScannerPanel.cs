using System.Collections.Generic;
using System.Linq;
using SDModKit.Game;
using StellarIndustries.Content;
using StellarIndustries.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace StellarIndustries.UI
{
    /// <summary>
    /// The scanner's module bay: the slots of the scanner carried, the modules fitted in them
    /// (take one out: it goes back to the inventory), and the modules in the inventory (fit one:
    /// it leaves the inventory). Opened by right-clicking with the scanner in hand, or Shift + the
    /// scanner key. It is one of the game's menus while open (free cursor, Escape closes it).
    /// </summary>
    internal sealed class ScannerPanel : MonoBehaviour, global::UI.Interface.Model.IClosableMenu
    {
        private static ScannerPanel _open;
        private static GUIStyle _title, _text, _small, _button;
        private static Texture2D _white;

        public static bool IsOpen => _open != null;

        public static void Toggle()
        {
            if (_open != null)
            {
                _open.Close();
                return;
            }
            var go = new GameObject("SI_ScannerPanel");
            DontDestroyOnLoad(go);
            _open = go.AddComponent<ScannerPanel>();
            GameRefs.Service<global::UI.Common.MenuTracker>()?.TrackMenu(_open);
            SINet.SendToServer(SIMessageKind.ScannerRequest, 0, 0, "");
        }

        public void Close()
        {
            GameRefs.Service<global::UI.Common.MenuTracker>()?.UntrackMenu(this);
            if (_open == this)
                _open = null;
            Destroy(gameObject);
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
                Close();
        }

        private void OnGUI()
        {
            Styles();
            GUI.depth = -10;
            Dictionary<uint, int> counts = PlayerAccess.CurrentCounts();
            int Has(uint item) => counts.TryGetValue(item, out int n) ? n : 0;
            int tier = Has(ResourceCatalog.ScannerMk3) > 0 ? 3 : Has(ResourceCatalog.ScannerMk2) > 0 ? 2 : Has(ResourceCatalog.ScannerMk1) > 0 ? 1 : 0;
            int slots = ScannerModules.Slots(tier);
            IReadOnlyList<uint> fitted = ScannerModules.Own;
            var thumbnails = global::Core.Services.ServiceLocator.GetService<Items.Interface.Service.IItemThumbnailProvider>();

            var rect = new Rect(Screen.width * 0.5f - 360f, Screen.height * 0.5f - 270f, 720f, 540f);
            Fill(rect, new Color(0.04f, 0.06f, 0.08f, 0.95f));
            Fill(new Rect(rect.x, rect.y, rect.width, 3f), new Color(0.3f, 0.85f, 1f));
            GUI.Label(new Rect(rect.x + 20f, rect.y + 14f, 600f, 26f),
                Lang.T("SCANNER DE GISEMENTS", "DEPOSIT SCANNER") + (tier > 0 ? " Mk" + tier : "") + Lang.T(" — MODULES", " — MODULES"), _title);
            if (GUI.Button(new Rect(rect.xMax - 44f, rect.y + 12f, 30f, 28f), "×", _button))
            {
                Close();
                return;
            }
            DepositScanner.Gear gear = DepositScanner.CurrentGear();
            string effect = tier == 0
                ? Lang.T("<color=#f84>Aucun scanner dans l'inventaire : fabriquez-en un à l'atelier (recherche Prospection).</color>",
                    "<color=#f84>No scanner in your inventory: make one at the workshop (Prospecting research).</color>")
                : Lang.T($"Portée {Mathf.RoundToInt(gear.Range)} m", $"Range {Mathf.RoundToInt(gear.Range)} m") +
                  (gear.ShowsPurity ? Lang.T(" · pureté", " · purity") : "") + (gear.ShowsYield ? Lang.T(" · rendement", " · yield") : "") +
                  (gear.Markers ? Lang.T(" · repères dans la vue", " · markers in view") : "");
            GUI.Label(new Rect(rect.x + 20f, rect.y + 44f, 680f, 22f), effect, _text);

            // Slots of the scanner carried
            float y = rect.y + 80f;
            GUI.Label(new Rect(rect.x + 20f, y, 680f, 20f), Lang.T($"EMPLACEMENTS ({fitted.Count}/{slots})", $"SLOTS ({fitted.Count}/{slots})"), _small);
            y += 24f;
            int shown = Mathf.Max(slots, fitted.Count);
            for (int i = 0; i < shown; i++)
            {
                var cell = new Rect(rect.x + 20f + i * 172f, y, 160f, 150f);
                bool active = i < slots;
                Fill(cell, active ? new Color(0.1f, 0.16f, 0.2f, 1f) : new Color(0.16f, 0.08f, 0.08f, 1f));
                Fill(new Rect(cell.x, cell.y, cell.width, 2f), active ? new Color(0.3f, 0.85f, 1f, 0.6f) : new Color(1f, 0.5f, 0.3f, 0.6f));
                if (i < fitted.Count)
                {
                    uint item = fitted[i];
                    Texture2D icon = thumbnails?.GetThumbnail(item);
                    if (icon != null)
                        GUI.DrawTexture(new Rect(cell.x + 48f, cell.y + 8f, 64f, 64f), icon);
                    GUI.Label(new Rect(cell.x + 6f, cell.y + 74f, 148f, 36f), IndustrialItems.NameOf(item) + (active ? "" : Lang.T("\n<color=#f84>inactif</color>", "\n<color=#f84>inactive</color>")), _smallCentered);
                    if (GUI.Button(new Rect(cell.x + 20f, cell.y + 114f, 120f, 28f), Lang.T("Retirer", "Take out"), _button))
                        ScannerModules.RequestRemove(i);
                }
                else
                {
                    GUI.Label(new Rect(cell.x, cell.y + 60f, 160f, 24f), Lang.T("<color=#689>libre</color>", "<color=#689>empty</color>"), _smallCentered);
                }
            }
            if (shown == 0)
                GUI.Label(new Rect(rect.x + 20f, y + 40f, 680f, 24f), Lang.T("<color=#9aa>Pas d'emplacement sans scanner.</color>", "<color=#9aa>No slot without a scanner.</color>"), _text);

            // Modules in the inventory
            y += 166f;
            GUI.Label(new Rect(rect.x + 20f, y, 680f, 20f), Lang.T("MODULES DANS L'INVENTAIRE", "MODULES IN YOUR INVENTORY"), _small);
            y += 24f;
            bool any = false;
            foreach (var kind in ScannerModules.Kinds)
            {
                int n = Has(kind.Item);
                if (n == 0)
                    continue;
                any = true;
                Texture2D icon = thumbnails?.GetThumbnail(kind.Item);
                if (icon != null)
                    GUI.DrawTexture(new Rect(rect.x + 20f, y, 32f, 32f), icon);
                string what = kind.Range > 0f ? Lang.T($"+{Mathf.RoundToInt(kind.Range * 100f)} % de portée", $"+{Mathf.RoundToInt(kind.Range * 100f)}% range")
                    : kind.Analysis >= 2 ? Lang.T("pureté, rendement et repères", "purity, yield and markers") : Lang.T("pureté et rendement", "purity and yield");
                GUI.Label(new Rect(rect.x + 60f, y + 6f, 460f, 24f), $"<b>{IndustrialItems.NameOf(kind.Item)}</b> ×{n}  <color=#9aa>{what}</color>", _text);
                GUI.enabled = fitted.Count < slots;
                if (GUI.Button(new Rect(rect.xMax - 150f, y + 2f, 130f, 28f), Lang.T("Monter", "Fit"), _button))
                    ScannerModules.RequestInstall(kind.Item);
                GUI.enabled = true;
                y += 38f;
            }
            if (!any)
                GUI.Label(new Rect(rect.x + 20f, y + 4f, 680f, 24f), Lang.T("<color=#9aa>Aucun module : fabriquez-en à l'assembleuse, au fabricateur ou à l'accélérateur.</color>",
                    "<color=#9aa>No module: make some at the assembler, the fabricator or the accelerator.</color>"), _text);
            if (ScannerModules.Refused)
                GUI.Label(new Rect(rect.x + 20f, rect.yMax - 58f, 680f, 22f), Lang.T("<color=#f84>Refusé : plus d'emplacement libre, ou plus de place dans l'inventaire.</color>",
                    "<color=#f84>Refused: no free slot, or no room left in your inventory.</color>"), _small);
            GUI.Label(new Rect(rect.x + 20f, rect.yMax - 32f, 680f, 22f), Lang.T("<color=#9aa>Les modules montés suivent votre compte : ils servent avec n'importe quel scanner que vous portez. [Échap] fermer</color>",
                "<color=#9aa>Fitted modules follow your account: they work with any scanner you carry. [Esc] close</color>"), _small);
        }

        private static GUIStyle _smallCentered;

        private static void Fill(Rect r, Color c)
        {
            Color previous = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, _white);
            GUI.color = previous;
        }

        private static void Styles()
        {
            if (_title != null)
                return;
            _white = Texture2D.whiteTexture;
            _title = new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold, richText = true, normal = { textColor = new Color(0.85f, 0.95f, 1f) } };
            _text = new GUIStyle(GUI.skin.label) { fontSize = 15, richText = true, wordWrap = true, normal = { textColor = Color.white } };
            _small = new GUIStyle(GUI.skin.label) { fontSize = 12, richText = true, wordWrap = true, normal = { textColor = new Color(0.6f, 0.85f, 1f) } };
            _smallCentered = new GUIStyle(_small) { alignment = TextAnchor.UpperCenter, normal = { textColor = Color.white } };
            _button = new GUIStyle(GUI.skin.button) { fontSize = 14, fontStyle = FontStyle.Bold };
        }
    }
}
