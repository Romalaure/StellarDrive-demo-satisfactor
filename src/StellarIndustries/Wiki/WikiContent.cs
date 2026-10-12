using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using StellarIndustries.Content;
using StellarIndustries.Core;
using StellarIndustries.Parts;
using StellarIndustries.Production;
using StellarIndustries.Recipes;

namespace StellarIndustries.Wiki
{
    /// <summary>
    /// The wiki pages made from the mod's own tables: the machines (power, cost, tech, recipes,
    /// sockets), the items (how to get them, what uses them, fuel value), the recipes and the
    /// research. Written as markdown in the current language, shown by <see cref="WikiPages"/>
    /// like the hand-written pages. Page ids stay the same in both languages.
    /// </summary>
    internal static class WikiContent
    {
        private static CultureInfo Culture => CultureInfo.GetCultureInfo(Lang.English ? "en-US" : "fr-FR");

        private static string T(string fr, string en) => Lang.T(fr, en);

        public static Dictionary<string, string> Generate()
        {
            var pages = new Dictionary<string, string> { ["Machines"] = MachinesPage(), ["Objets"] = ItemsPage(), ["Recettes"] = RecipesPage(), ["Recherche"] = ResearchPage() };
            foreach (MachineDef m in MachineCatalog.All)
                pages[MachinePage(m.PartId)] = MachineText(m);
            foreach (uint id in AllItems())
                pages[ItemPage(id)] = ItemText(id);
            return pages;
        }

        public static string MachinePage(ushort id) => "Machine-" + id;

        public static string ItemPage(uint id) => "Objet-" + id;

        private static string Name(uint id) => IndustrialItems.NameOf(id);

        private static string ItemLink(uint id) => $"[{Name(id)}]({ItemPage(id)})";

        private static string MachineLink(MachineDef m) => $"[{Lang.MachineName(m)}]({MachinePage(m.PartId)})";

        private static string Amounts(IEnumerable<ItemAmount> amounts) => string.Join(" + ", amounts.Select(a => a.Count + " " + ItemLink(a.Item)));

        private static string Number(float v) => v.ToString(v >= 10f ? "0" : "0.#", Culture);

        private static string Colon => T(" : ", ": ");

        /// <summary>Every item a recipe, a cost or a fuel names: the mod's and the game's.</summary>
        private static IEnumerable<uint> AllItems()
        {
            var ids = new HashSet<uint>(ResourceCatalog.All.Select(r => r.Id));
            foreach (RecipeDef r in RecipeBook.All)
            {
                foreach (ItemAmount a in r.Inputs.Concat(r.Outputs))
                    ids.Add(a.Item);
            }
            foreach (MachineDef m in MachineCatalog.All)
            {
                foreach (var c in m.BuildCost)
                    ids.Add(c.Item);
            }
            foreach (var fuel in MachineCatalog.Fuels)
                ids.Add(fuel.Item);
            return ids.OrderBy(i => i);
        }

        private static IndustryResearch.Tech TechOf(ushort part) => IndustryResearch.All.FirstOrDefault(t => t.Parts.Contains(part));

        private static string TechName(IndustryResearch.Tech tech) => Lang.TechName(tech.Id, tech.Name);

        private static string Role(MachineDef m)
        {
            if (m.Extracts) return T("extraction", "extraction");
            if (m.Generates) return T("production d'énergie", "power generation");
            if (m.Stores) return T("stockage d'énergie", "energy storage");
            if (m.Dock) return T("transfert entre vaisseaux", "ship-to-ship transfer");
            if (m.Container) return T("stockage", "storage");
            return RecipeBook.ForMachine(m.PartId).Length + T(" recette(s)", " recipe(s)");
        }

        // ---- Machines

        private static string MachinesPage()
        {
            var t = new StringBuilder();
            t.Append(T("# Machines\n\nToutes les machines sont dans l'onglet de construction **Industrie**. Cliquez sur un nom pour sa fiche.\n\n",
                "# Machines\n\nEvery machine is in the **Industrie** build tab. Click a name to open its page.\n\n"));
            t.Append(T("| Machine | Puissance | Rôle | Recherche |\n|---|---|---|---|\n", "| Machine | Power | Role | Research |\n|---|---|---|---|\n"));
            foreach (MachineDef m in MachineCatalog.All)
            {
                IndustryResearch.Tech tech = TechOf(m.PartId);
                string power = m.IsLogistics ? T("aucune", "none") : m.Generates ? T("donne ", "gives ") + Number(m.Watts) + " W" : m.Stores ? "± " + Number(m.Watts) + " W" : Number(m.Watts) + " W";
                t.Append($"| {MachineLink(m)} | {power} | {Role(m)} | {(tech != null ? TechName(tech) : T("d'office", "from the start"))} |\n");
            }
            return t.ToString();
        }

        private static string MachineText(MachineDef m)
        {
            var t = new StringBuilder();
            t.Append("# ").Append(Lang.MachineName(m)).Append("\n\n").Append(Lang.MachineDescription(m)).Append("\n\n");
            IndustryResearch.Tech tech = TechOf(m.PartId);
            t.Append("- **").Append(T("Puissance", "Power")).Append("**").Append(Colon);
            if (m.IsLogistics)
                t.Append(T("aucune", "none"));
            else if (m.Generates)
                t.Append(T("donne jusqu'à ", "gives up to ")).Append(Number(m.Watts)).Append(T(" W à son réseau", " W to its grid"));
            else if (m.Stores)
                t.Append(Number(m.StoreJoules / 1e6f)).Append(T(" MJ stockés, ", " MJ stored, ")).Append(Number(m.Watts)).Append(T(" W en charge et en décharge", " W charging and discharging"));
            else
                t.Append(Number(m.Watts)).Append(T(" W pendant la production", " W while producing"));
            t.Append("\n- **").Append(T("Coût de construction", "Build cost")).Append("**").Append(Colon).Append(string.Join(" + ", m.BuildCost.Select(c => c.Count + " " + ItemLink(c.Item))));
            t.Append("\n- **").Append(T("Recherche", "Research")).Append("**").Append(Colon).Append(tech != null ? $"[{TechName(tech)}](Recherche)" : T("disponible d'office", "available from the start"));
            t.Append("\n- **").Append(T("Taille", "Size")).Append("**").Append(Colon).Append(Number(m.Width)).Append(" × ").Append(Number(m.Depth)).Append(" m, ").Append(Number(m.Height)).Append(T(" m de haut", " m tall"));
            if (!m.IsLogistics)
                t.Append(T("\n- **Prises** (à l'arrière, de gauche à droite) : port d'objets, port d'objets, prise électrique (boîtier jaune et noir). Les ports d'objets viennent du mod Item Lines.",
                    "\n- **Sockets** (on the back, left to right): item port, item port, power socket (yellow and black box). The item ports come from the Item Lines mod."));
            else
                t.Append(T("\n- **Prises** : port d'objets (Item Lines) ; pas d'électricité.", "\n- **Sockets**: item port (Item Lines); no power needed."));
            t.Append("\n\n");
            if (m.Extracts)
                t.Append(T("## Fonctionnement\nTire 1 objet toutes les ", "## How it works\nPulls 1 item every ")).Append(Number(m.ExtractSeconds))
                    .Append(T(" s du rocher minable le plus proche, à moins de ", " s from the nearest minable rock, within ")).Append(Number(m.ExtractRadius))
                    .Append(T(" m. Utilisez le scanner (touche K) pour trouver les rochers.\n", " m. Use the scanner (K key) to find rocks.\n"));
            if (m.Generates)
            {
                t.Append(T("## Combustibles\n", "## Fuels\n"));
                foreach (var fuel in MachineCatalog.Fuels)
                    t.Append("- ").Append(ItemLink(fuel.Item)).Append(Colon).Append(Number(fuel.Joules / 1000f)).Append(T(" kJ, soit ", " kJ, that is ")).Append(Number(fuel.Joules / m.Watts)).Append(T(" s à pleine puissance\n", " s at full power\n"));
            }
            RecipeDef[] recipes = RecipeBook.ForMachine(m.PartId);
            if (recipes.Length > 0)
            {
                t.Append(T("## Recettes\nEn mode automatique la machine prend la première recette possible ; touche N pour en fixer une.\n\n",
                    "## Recipes\nIn automatic mode the machine runs the first recipe it can; press N to lock one.\n\n"));
                foreach (RecipeDef r in recipes)
                    t.Append(RecipeLine(r)).Append('\n');
            }
            return t.ToString();
        }

        private static string RecipeLine(RecipeDef r)
        {
            float perMinute = 60f / r.Seconds;
            return $"- **{Lang.RecipeName(r)}** ({Number(r.Seconds)} s){Colon}{Amounts(r.Inputs)} » {Amounts(r.Outputs)}  " +
                   $"*({string.Join(", ", r.Outputs.Select(o => Number(o.Count * perMinute) + "/min"))})*";
        }

        // ---- Items

        private static string ItemsPage()
        {
            var t = new StringBuilder();
            t.Append(T("# Objets\n\nObjets du mod par catégorie, et ressources du jeu qu'ils utilisent. Cliquez pour savoir comment les obtenir et à quoi ils servent.\n",
                "# Items\n\nThe mod's items by category, and the game resources they use. Click one to see how to get it and what it is for.\n"));
            foreach (var group in ResourceCatalog.All.GroupBy(r => r.Category).OrderBy(g => g.Key))
            {
                t.Append("\n## ").Append(CategoryName(group.Key)).Append('\n');
                t.Append(string.Join(" · ", group.Select(r => ItemLink(r.Id)))).Append('\n');
            }
            t.Append(T("\n## Ressources du jeu\n", "\n## Game resources\n"));
            t.Append(string.Join(" · ", AllItems().Where(i => ResourceCatalog.Get(i) == null).Select(ItemLink))).Append('\n');
            return t.ToString();
        }

        private static string CategoryName(ResourceCategory c)
        {
            switch (c)
            {
                case ResourceCategory.Ore: return T("Minerais et matières", "Ores and raw materials");
                case ResourceCategory.Intermediate: return T("Produits intermédiaires", "Intermediate products");
                case ResourceCategory.Component: return T("Composants", "Components");
                case ResourceCategory.Advanced: return T("Composants avancés", "Advanced components");
                default: return T("Fluides", "Fluids");
            }
        }

        private static string ItemText(uint id)
        {
            var t = new StringBuilder();
            ResourceDef def = ResourceCatalog.Get(id);
            t.Append("# ").Append(Name(id)).Append("\n\n");
            if (def != null)
                t.Append(Lang.ItemDescription(def)).Append("\n\n- **").Append(T("Masse", "Mass")).Append("**").Append(Colon).Append(Number(def.Mass)).Append(" kg\n- **")
                    .Append(T("Pile", "Stack")).Append("**").Append(Colon).Append(def.MaxStack).Append('\n');
            else
                t.Append(T("Ressource du jeu.\n", "A resource of the game.\n"));
            float fuel = MachineCatalog.FuelEnergy(id);
            if (fuel > 0f)
                t.Append("- **").Append(T("Combustible", "Fuel")).Append("**").Append(Colon).Append(Number(fuel / 1000f)).Append(T(" kJ dans le générateur à combustion\n", " kJ in the combustion generator\n"));

            var makes = RecipeBook.All.Where(r => r.Outputs.Any(o => o.Item == id)).ToList();
            t.Append(T("\n## Obtention\n", "\n## How to get it\n"));
            if (def == null)
                t.Append(T("- Minage au laser, ou un ", "- Mining laser, or an ")).Append($"[{Lang.T("Extracteur", "Extractor")}]({MachinePage(MachineCatalog.Extractor)})")
                    .Append(T(" à côté d'un rocher qui en donne.\n", " next to a rock that gives it.\n"));
            foreach (RecipeDef r in makes)
                t.Append(Made(r)).Append('\n');
            if (def != null && makes.Count == 0)
                t.Append(T("- Aucune recette.\n", "- No recipe.\n"));

            var uses = RecipeBook.All.Where(r => r.Inputs.Any(i => i.Item == id)).ToList();
            var costs = MachineCatalog.All.Where(m => m.BuildCost.Any(c => c.Item == id)).ToList();
            var techs = IndustryResearch.All.Where(tech => tech.Cost.Any(c => c.Item == id)).ToList();
            if (uses.Count + costs.Count + techs.Count > 0)
            {
                t.Append(T("\n## Utilisation\n", "\n## Used for\n"));
                foreach (RecipeDef r in uses)
                    t.Append(Made(r)).Append('\n');
                foreach (MachineDef m in costs)
                    t.Append(T("- Construction : ", "- Building: ")).Append(MachineLink(m)).Append(" (").Append(m.BuildCost.First(c => c.Item == id).Count).Append(")\n");
                foreach (IndustryResearch.Tech tech in techs)
                    t.Append(T("- Recherche : [", "- Research: [")).Append(TechName(tech)).Append("](Recherche) (").Append(tech.Cost.First(c => c.Item == id).Count).Append(")\n");
            }
            return t.ToString();
        }

        private static string Made(RecipeDef r)
        {
            MachineCatalog.TryGet(r.Machine, out MachineDef m);
            return $"- {(m != null ? MachineLink(m) : "?")}{Colon}{Amounts(r.Inputs)} » {Amounts(r.Outputs)} ({Number(r.Seconds)} s)";
        }

        // ---- Research

        private static string ResearchPage()
        {
            var t = new StringBuilder();
            t.Append(T("# Recherche\n\nLa branche **Industrie** de la table de recherche débloque les machines. Elle commence après *Power Generation* (le générateur à bois du jeu). " +
                       "Les niveaux supérieurs se paient avec les produits du mod : il faut remonter la chaîne pour avancer. Réglage `ResearchUnlocks = false` : tout est disponible d'office.\n\n",
                "# Research\n\nThe **Industrie** branch of the research table unlocks the machines. It starts after *Power Generation* (the game's wood generator). " +
                "Higher levels are paid with the mod's own products: you climb the production chain to move on. Setting `ResearchUnlocks = false` makes everything available from the start.\n\n"));
            foreach (IndustryResearch.Tech tech in IndustryResearch.All)
            {
                var requires = tech.Requires.Select(id =>
                {
                    IndustryResearch.Tech other = IndustryResearch.All.FirstOrDefault(x => x.Id == id);
                    return other != null ? TechName(other) : id == 2 ? "Power Generation" : "tech " + id;
                });
                t.Append("## ").Append(TechName(tech)).Append('\n');
                t.Append("- **").Append(T("Après", "After")).Append("**").Append(Colon).Append(string.Join(", ", requires)).Append('\n');
                t.Append("- **").Append(T("Coût", "Cost")).Append("**").Append(Colon).Append(string.Join(" + ", tech.Cost.Select(c => c.Count + " " + ItemLink(c.Item)))).Append('\n');
                t.Append("- **").Append(T("Débloque", "Unlocks")).Append("**").Append(Colon).Append(string.Join(", ", tech.Parts.Select(p => MachineCatalog.TryGet(p, out MachineDef m) ? MachineLink(m) : p.ToString()))).Append("\n\n");
            }
            return t.ToString();
        }

        // ---- Recipes

        private static string RecipesPage()
        {
            var t = new StringBuilder();
            t.Append(T("# Recettes\n\nToutes les recettes, par machine. Durées à vitesse 1 (réglage ProductionSpeed).\n",
                "# Recipes\n\nEvery recipe, by machine. Times at speed 1 (ProductionSpeed setting).\n"));
            foreach (MachineDef m in MachineCatalog.All)
            {
                RecipeDef[] recipes = RecipeBook.ForMachine(m.PartId);
                if (recipes.Length == 0)
                    continue;
                t.Append("\n## ").Append(MachineLink(m)).Append('\n');
                foreach (RecipeDef r in recipes)
                    t.Append(RecipeLine(r)).Append('\n');
            }
            return t.ToString();
        }
    }
}
