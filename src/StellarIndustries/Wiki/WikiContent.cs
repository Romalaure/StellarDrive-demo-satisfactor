using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using StellarIndustries.Content;
using StellarIndustries.Parts;
using StellarIndustries.Production;
using StellarIndustries.Recipes;

namespace StellarIndustries.Wiki
{
    /// <summary>
    /// The wiki pages made from the mod's own tables: the machines (power, cost, tech, recipes,
    /// sockets), the items (how to get them, what uses them, fuel value) and the recipe list.
    /// Written as markdown, shown by <see cref="WikiPages"/> like the hand-written pages.
    /// </summary>
    internal static class WikiContent
    {
        private static readonly CultureInfo Fr = CultureInfo.GetCultureInfo("fr-FR");

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

        private static string MachineLink(MachineDef m) => $"[{m.Name}]({MachinePage(m.PartId)})";

        private static string Amounts(IEnumerable<ItemAmount> amounts) => string.Join(" + ", amounts.Select(a => a.Count + " " + ItemLink(a.Item)));

        private static string Number(float v) => v.ToString(v >= 10f ? "0" : "0.#", Fr);

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

        private static string Role(MachineDef m)
        {
            if (m.Extracts) return "extraction";
            if (m.Generates) return "production d'énergie";
            if (m.Stores) return "stockage d'énergie";
            if (m.Dock) return "transfert entre vaisseaux";
            if (m.Container) return "stockage";
            return RecipeBook.ForMachine(m.PartId).Length + " recette(s)";
        }

        // ---- Machines

        private static string MachinesPage()
        {
            var t = new StringBuilder();
            t.Append("# Machines\n\nToutes les machines sont dans l'onglet de construction **Industrie**. Cliquez sur un nom pour sa fiche.\n\n");
            t.Append("| Machine | Puissance | Rôle | Recherche |\n|---|---|---|---|\n");
            foreach (MachineDef m in MachineCatalog.All)
            {
                IndustryResearch.Tech tech = TechOf(m.PartId);
                string power = m.IsLogistics ? "aucune" : m.Generates ? "donne " + Number(m.Watts) + " W" : m.Stores ? "± " + Number(m.Watts) + " W" : Number(m.Watts) + " W";
                t.Append($"| {MachineLink(m)} | {power} | {Role(m)} | {(tech != null ? tech.Name : "d'office")} |\n");
            }
            return t.ToString();
        }

        private static string MachineText(MachineDef m)
        {
            var t = new StringBuilder();
            t.Append("# ").Append(m.Name).Append("\n\n").Append(m.Description).Append("\n\n");
            IndustryResearch.Tech tech = TechOf(m.PartId);
            t.Append("- **Puissance** : ");
            if (m.IsLogistics)
                t.Append("aucune");
            else if (m.Generates)
                t.Append("donne jusqu'à ").Append(Number(m.Watts)).Append(" W à son réseau");
            else if (m.Stores)
                t.Append(Number(m.StoreJoules / 1e6f)).Append(" MJ stockés, ").Append(Number(m.Watts)).Append(" W en charge et en décharge");
            else
                t.Append(Number(m.Watts)).Append(" W pendant la production");
            t.Append("\n- **Coût de construction** : ").Append(string.Join(" + ", m.BuildCost.Select(c => c.Count + " " + ItemLink(c.Item))));
            t.Append("\n- **Recherche** : ").Append(tech != null ? $"[{tech.Name}](Recherche)" : "disponible d'office");
            t.Append("\n- **Taille** : ").Append(Number(m.Width)).Append(" × ").Append(Number(m.Depth)).Append(" m, ").Append(Number(m.Height)).Append(" m de haut");
            if (!m.IsLogistics)
                t.Append("\n- **Prises** (à l'arrière, de gauche à droite) : port d'objets, port d'objets, prise électrique (boîtier jaune et noir). Les ports d'objets viennent du mod Item Lines.");
            else
                t.Append("\n- **Prises** : port d'objets (Item Lines) ; pas d'électricité.");
            t.Append("\n\n");
            if (m.Extracts)
                t.Append("## Fonctionnement\nTire 1 objet toutes les ").Append(Number(m.ExtractSeconds)).Append(" s du rocher minable le plus proche, à moins de ").Append(Number(m.ExtractRadius)).Append(" m. Utilisez le scanner (touche K) pour trouver les rochers.\n");
            if (m.Generates)
            {
                t.Append("## Combustibles\n");
                foreach (var fuel in MachineCatalog.Fuels)
                    t.Append("- ").Append(ItemLink(fuel.Item)).Append(" : ").Append(Number(fuel.Joules / 1000f)).Append(" kJ, soit ").Append(Number(fuel.Joules / m.Watts)).Append(" s à pleine puissance\n");
            }
            RecipeDef[] recipes = RecipeBook.ForMachine(m.PartId);
            if (recipes.Length > 0)
            {
                t.Append("## Recettes\nEn mode automatique la machine prend la première recette possible ; touche N pour en fixer une.\n\n");
                foreach (RecipeDef r in recipes)
                    t.Append(RecipeLine(r)).Append('\n');
            }
            return t.ToString();
        }

        private static string RecipeLine(RecipeDef r)
        {
            float perMinute = 60f / r.Seconds;
            return $"- **{r.Name}** ({Number(r.Seconds)} s) : {Amounts(r.Inputs)} » {Amounts(r.Outputs)}  " +
                   $"*({string.Join(", ", r.Outputs.Select(o => Number(o.Count * perMinute) + "/min"))})*";
        }

        // ---- Items

        private static string ItemsPage()
        {
            var t = new StringBuilder();
            t.Append("# Objets\n\nObjets du mod par catégorie, et ressources du jeu qu'ils utilisent. Cliquez pour savoir comment les obtenir et à quoi ils servent.\n");
            foreach (var group in ResourceCatalog.All.GroupBy(r => r.Category).OrderBy(g => g.Key))
            {
                t.Append("\n## ").Append(CategoryName(group.Key)).Append('\n');
                t.Append(string.Join(" · ", group.Select(r => ItemLink(r.Id)))).Append('\n');
            }
            t.Append("\n## Ressources du jeu\n");
            t.Append(string.Join(" · ", AllItems().Where(i => ResourceCatalog.Get(i) == null).Select(ItemLink))).Append('\n');
            return t.ToString();
        }

        private static string CategoryName(ResourceCategory c)
        {
            switch (c)
            {
                case ResourceCategory.Ore: return "Minerais et matières";
                case ResourceCategory.Intermediate: return "Produits intermédiaires";
                case ResourceCategory.Component: return "Composants";
                case ResourceCategory.Advanced: return "Composants avancés";
                default: return "Fluides";
            }
        }

        private static string ItemText(uint id)
        {
            var t = new StringBuilder();
            ResourceDef def = ResourceCatalog.Get(id);
            t.Append("# ").Append(Name(id)).Append("\n\n");
            if (def != null)
                t.Append(def.Description).Append("\n\n- **Masse** : ").Append(Number(def.Mass)).Append(" kg\n- **Pile** : ").Append(def.MaxStack).Append('\n');
            else
                t.Append("Ressource du jeu.\n");
            float fuel = MachineCatalog.FuelEnergy(id);
            if (fuel > 0f)
                t.Append("- **Combustible** : ").Append(Number(fuel / 1000f)).Append(" kJ dans le générateur à combustion\n");

            var makes = RecipeBook.All.Where(r => r.Outputs.Any(o => o.Item == id)).ToList();
            t.Append("\n## Obtention\n");
            if (def == null)
                t.Append("- Minage au laser, ou un [Extracteur](").Append(MachinePage(MachineCatalog.Extractor)).Append(") à côté d'un rocher qui en donne.\n");
            foreach (RecipeDef r in makes)
                t.Append(Made(r)).Append('\n');
            if (def != null && makes.Count == 0)
                t.Append("- Aucune recette.\n");

            var uses = RecipeBook.All.Where(r => r.Inputs.Any(i => i.Item == id)).ToList();
            var costs = MachineCatalog.All.Where(m => m.BuildCost.Any(c => c.Item == id)).ToList();
            var techs = IndustryResearch.All.Where(tech => tech.Cost.Any(c => c.Item == id)).ToList();
            if (uses.Count + costs.Count + techs.Count > 0)
            {
                t.Append("\n## Utilisation\n");
                foreach (RecipeDef r in uses)
                    t.Append(Made(r)).Append('\n');
                foreach (MachineDef m in costs)
                    t.Append("- Construction : ").Append(MachineLink(m)).Append(" (").Append(m.BuildCost.First(c => c.Item == id).Count).Append(")\n");
                foreach (IndustryResearch.Tech tech in techs)
                    t.Append("- Recherche : [").Append(tech.Name).Append("](Recherche) (").Append(tech.Cost.First(c => c.Item == id).Count).Append(")\n");
            }
            return t.ToString();
        }

        private static string Made(RecipeDef r)
        {
            MachineCatalog.TryGet(r.Machine, out MachineDef m);
            return $"- {(m != null ? MachineLink(m) : "?")} : {Amounts(r.Inputs)} » {Amounts(r.Outputs)} ({Number(r.Seconds)} s)";
        }

        // ---- Research

        private static string ResearchPage()
        {
            var t = new StringBuilder();
            t.Append("# Recherche\n\nLa branche **Industrie** de la table de recherche débloque les machines. Elle commence après *Power Generation* (le générateur à bois du jeu). ");
            t.Append("Les niveaux supérieurs se paient avec les produits du mod : il faut remonter la chaîne pour avancer. Réglage `ResearchUnlocks = false` : tout est disponible d'office.\n\n");
            foreach (IndustryResearch.Tech tech in IndustryResearch.All)
            {
                var requires = tech.Requires.Select(id => IndustryResearch.All.FirstOrDefault(x => x.Id == id)?.Name ?? (id == 2 ? "Power Generation" : "tech " + id));
                t.Append("## ").Append(tech.Name).Append('\n');
                t.Append("- **Après** : ").Append(string.Join(", ", requires)).Append('\n');
                t.Append("- **Coût** : ").Append(string.Join(" + ", tech.Cost.Select(c => c.Count + " " + ItemLink(c.Item)))).Append('\n');
                t.Append("- **Débloque** : ").Append(string.Join(", ", tech.Parts.Select(p => MachineCatalog.TryGet(p, out MachineDef m) ? MachineLink(m) : p.ToString()))).Append("\n\n");
            }
            return t.ToString();
        }

        // ---- Recipes

        private static string RecipesPage()
        {
            var t = new StringBuilder();
            t.Append("# Recettes\n\nToutes les recettes, par machine. Durées à vitesse 1 (réglage ProductionSpeed).\n");
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
