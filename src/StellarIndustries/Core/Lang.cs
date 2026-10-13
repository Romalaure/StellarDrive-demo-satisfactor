using System;
using StellarIndustries.Content;
using StellarIndustries.Production;
using StellarIndustries.Recipes;
using UnityEngine;
using UnityEngine.Localization.Settings;

namespace StellarIndustries.Core
{
    /// <summary>
    /// The mod's language: French or English. "auto" (the default) follows the game's language
    /// (French when the game is in French, English otherwise); the Language setting can force one.
    /// Checked once a second; when it changes, the item and part names are updated in place.
    /// </summary>
    internal static class Lang
    {
        private static float _nextCheck;

        public static bool English { get; private set; }

        /// <summary>Raised when the language changes (item and part names, wiki pages).</summary>
        public static event Action Changed;

        /// <summary>The text in the current language.</summary>
        public static string T(string french, string english) => English ? english : french;

        public static void Refresh(bool force = false)
        {
            if (!force && Time.unscaledTime < _nextCheck)
                return;
            _nextCheck = Time.unscaledTime + 1f;
            bool english = Detect();
            if (english == English && !force)
                return;
            English = english;
            Changed?.Invoke();
        }

        private static bool Detect()
        {
            string setting = (Settings.Language?.Value ?? "auto").Trim().ToLowerInvariant();
            if (setting.StartsWith("fr"))
                return false;
            if (setting.StartsWith("en"))
                return true;
            try
            {
                string code = LocalizationSettings.HasSettings ? LocalizationSettings.SelectedLocale?.Identifier.Code : null;
                if (!string.IsNullOrEmpty(code))
                    return !code.StartsWith("fr", StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception)
            {
                // Localization not ready yet: fall back to the system's language
            }
            return Application.systemLanguage != SystemLanguage.French;
        }

        // ---- Content names

        public static string ItemName(ResourceDef def) => English && Content.English.Items.TryGetValue(def.Id, out var en) ? en.Name : def.Name;

        public static string ItemDescription(ResourceDef def) => English && Content.English.Items.TryGetValue(def.Id, out var en) ? en.Description : def.Description;

        public static string MachineName(MachineDef def) => English && Content.English.Machines.TryGetValue(def.PartId, out var en) ? en.Name : def.Name;

        public static string MachineDescription(MachineDef def) => English && Content.English.Machines.TryGetValue(def.PartId, out var en) ? en.Description : def.Description;

        public static string TechName(ushort id, string french) => English && Content.English.Techs.TryGetValue(id, out string en) ? en : french;

        /// <summary>A recipe's name: its own translation, else its first product's name.</summary>
        public static string RecipeName(RecipeDef recipe)
        {
            if (!English || recipe == null)
                return recipe?.Name;
            if (Content.English.Recipes.TryGetValue(recipe.Id ?? "", out string en))
                return en;
            if (recipe.Id != null && recipe.Id.StartsWith("extract."))
                return "Extraction: " + IndustrialItems.NameOf(recipe.Outputs[0].Item);
            if (recipe.Id != null && recipe.Id.StartsWith("sink."))
                return "Recycling: " + IndustrialItems.NameOf(recipe.Inputs[0].Item);
            return recipe.Outputs != null && recipe.Outputs.Length > 0 ? IndustrialItems.NameOf(recipe.Outputs[0].Item) : recipe.Name;
        }
    }
}
