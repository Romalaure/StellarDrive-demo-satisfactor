using System.Collections.Generic;
using System.Linq;
using StellarIndustries.Content;
using StellarIndustries.Core;
using StellarIndustries.Production;

namespace StellarIndustries.Recipes
{
    internal sealed class ValidationIssue
    {
        public bool IsError;
        public string Message;
        /// <summary>The recipe at fault, if any (a recipe with an error is not run).</summary>
        public string RecipeId;

        public override string ToString() => (IsError ? "ERREUR " : "attention ") + Message;
    }

    /// <summary>
    /// Checks the content data at load (and in the tests): unknown items or machines, empty or
    /// negative recipes, recipes no machine can hold, items nobody can obtain, id clashes and
    /// loops. A recipe with an error is not run by the machines.
    /// </summary>
    internal static class RecipeValidator
    {
        public static List<ValidationIssue> Validate(IEnumerable<RecipeDef> recipes, IEnumerable<MachineDef> machines, IEnumerable<ResourceDef> resources)
        {
            var issues = new List<ValidationIssue>();
            List<RecipeDef> recipeList = recipes.ToList();
            List<MachineDef> machineList = machines.ToList();
            List<ResourceDef> resourceList = resources.ToList();

            foreach (var group in resourceList.GroupBy(r => r.Id).Where(g => g.Count() > 1))
                Error(issues, $"ressource {group.Key} définie {group.Count()} fois");
            foreach (ResourceDef r in resourceList)
            {
                if (r.Id < Ids.FirstItem || r.Id > Ids.LastItem)
                    Error(issues, $"ressource {r.Name}: id {r.Id} hors de la plage {Ids.FirstItem}-{Ids.LastItem}");
                if (r.MaxStack < 1 || r.MaxStack > 255)
                    Error(issues, $"ressource {r.Name}: pile max {r.MaxStack} (1 à 255)");
                if (string.IsNullOrEmpty(r.Name))
                    Error(issues, $"ressource {r.Id} sans nom");
            }
            foreach (var group in machineList.GroupBy(m => m.PartId).Where(g => g.Count() > 1))
                Error(issues, $"machine {group.Key} définie {group.Count()} fois");
            foreach (MachineDef m in machineList)
            {
                if (m.PartId < Ids.FirstPart || m.PartId > Ids.LastPart)
                    Error(issues, $"machine {m.Name}: id {m.PartId} hors de la plage {Ids.FirstPart}-{Ids.LastPart}");
                if (m.Watts < 0f || m.Speed <= 0f)
                    Error(issues, $"machine {m.Name}: puissance ou vitesse invalide");
                if (m.Extracts && !(m.ExtractSeconds > 0f && m.ExtractRadius > 0f))
                    Error(issues, $"machine {m.Name}: extraction invalide");
                if (m.Generates && MachineCatalog.Fuels.Length == 0)
                    Error(issues, $"machine {m.Name}: aucun combustible");
                if (m.Stores && !(m.StoreJoules > 0f && m.Watts > 0f))
                    Error(issues, $"machine {m.Name}: stockage invalide");
                if (!m.HasNoRecipe && !recipeList.Any(r => r.Machine == m.PartId))
                    Warn(issues, $"machine {m.Name}: aucune recette");
            }

            foreach (var group in recipeList.GroupBy(r => r.Id).Where(g => g.Count() > 1))
                Error(issues, $"recette {group.Key} définie {group.Count()} fois");
            foreach (RecipeDef r in recipeList)
                ValidateRecipe(issues, r, machineList);

            // Items the recipes need but nothing gives (the game's own items come from the world)
            var produced = new HashSet<uint>(recipeList.SelectMany(r => r.Outputs ?? new ItemAmount[0]).Select(o => o.Item));
            foreach (MachineDef m in machineList)
            {
                if (m.FixedOutput != 0)
                    produced.Add(m.FixedOutput);
                if (m.WasteItem != 0)
                    produced.Add(m.WasteItem);
                if (m.Sinks)
                    produced.Add(ResourceCatalog.Voucher);
            }
            foreach (RecipeDef r in recipeList)
            {
                foreach (ItemAmount input in r.Inputs ?? new ItemAmount[0])
                {
                    if (ResourceCatalog.Get(input.Item) != null && !produced.Contains(input.Item))
                        Error(issues, $"recette {r.Id}: {input.Item} n'est produit par aucune recette (chaîne bloquée)", r.Id);
                }
            }
            foreach (ResourceDef res in resourceList)
            {
                if (!produced.Contains(res.Id))
                    Warn(issues, $"ressource {res.Name}: aucune recette ne la produit (à obtenir autrement)");
            }

            // Inside one machine, an output that is another recipe's input would be eaten again at once
            foreach (var machine in recipeList.GroupBy(r => r.Machine))
            {
                var outputs = new HashSet<uint>(machine.SelectMany(r => r.Outputs ?? new ItemAmount[0]).Select(o => o.Item));
                foreach (RecipeDef r in machine)
                {
                    foreach (ItemAmount input in (r.Inputs ?? new ItemAmount[0]).Where(i => outputs.Contains(i.Item)))
                        Warn(issues, $"recette {r.Id}: {input.Item} est aussi produit par la même machine (consommé aussitôt)");
                }
            }

            FindLoops(issues, recipeList);
            return issues;
        }

        private static void ValidateRecipe(List<ValidationIssue> issues, RecipeDef r, List<MachineDef> machines)
        {
            string name = "recette " + (string.IsNullOrEmpty(r.Id) ? "(sans id)" : r.Id);
            MachineDef machine = machines.FirstOrDefault(m => m.PartId == r.Machine);
            if (machine == null)
                Error(issues, $"{name}: machine {r.Machine} inexistante", r.Id);
            if (r.Outputs == null || r.Outputs.Length == 0)
                Error(issues, $"{name}: aucune sortie", r.Id);
            if (r.Inputs == null || r.Inputs.Length == 0)
                Error(issues, $"{name}: aucune entrée (production à partir de rien)", r.Id);
            if (!(r.Seconds > 0f) || float.IsInfinity(r.Seconds))
                Error(issues, $"{name}: durée {r.Seconds} invalide", r.Id);
            if (r.Watts < 0f)
                Error(issues, $"{name}: puissance négative", r.Id);
            foreach (ItemAmount a in (r.Inputs ?? new ItemAmount[0]).Concat(r.Outputs ?? new ItemAmount[0]))
            {
                if (a.Count <= 0)
                    Error(issues, $"{name}: quantité {a.Count} pour l'objet {a.Item}", r.Id);
                if (!ResourceCatalog.Exists(a.Item))
                    Error(issues, $"{name}: objet {a.Item} inexistant", r.Id);
            }
            if (r.Inputs != null && r.Outputs != null && r.Inputs.Any(i => r.Outputs.Any(o => o.Item == i.Item)))
                Error(issues, $"{name}: un objet est à la fois entrée et sortie", r.Id);
            // Inputs and outputs must fit in an empty machine, one stack per slot
            if (machine != null && r.Inputs != null && r.Outputs != null)
            {
                int stacks = r.Inputs.Concat(r.Outputs).Sum(a => Slots(a));
                if (stacks > machine.Slots)
                    Error(issues, $"{name}: {stacks} emplacements nécessaires, la machine en a {machine.Slots}", r.Id);
            }
            if (machine != null && r.Tier < machine.Tier)
                Warn(issues, $"{name}: niveau {r.Tier} inférieur à celui de sa machine ({machine.Tier})");
        }

        private static int Slots(ItemAmount a)
        {
            int max = ResourceCatalog.Get(a.Item)?.MaxStack ?? 20;
            return (a.Count + max - 1) / max;
        }

        /// <summary>Item loops (A makes B, B makes A): reported, since they can multiply items if the counts allow.</summary>
        private static void FindLoops(List<ValidationIssue> issues, List<RecipeDef> recipes)
        {
            var edges = new Dictionary<uint, HashSet<uint>>();
            foreach (RecipeDef r in recipes)
            {
                foreach (ItemAmount i in r.Inputs ?? new ItemAmount[0])
                {
                    if (!edges.TryGetValue(i.Item, out HashSet<uint> to))
                        edges[i.Item] = to = new HashSet<uint>();
                    foreach (ItemAmount o in r.Outputs ?? new ItemAmount[0])
                        to.Add(o.Item);
                }
            }
            var state = new Dictionary<uint, int>();
            foreach (uint start in edges.Keys)
            {
                if (Visit(start, edges, state, out uint loopAt))
                {
                    Warn(issues, $"boucle de recettes passant par l'objet {loopAt}: vérifiez qu'elle ne crée pas d'objets");
                    return;
                }
            }
        }

        // Depth-first search: 1 = on the current path, 2 = done
        private static bool Visit(uint item, Dictionary<uint, HashSet<uint>> edges, Dictionary<uint, int> state, out uint loopAt)
        {
            loopAt = 0;
            state.TryGetValue(item, out int s);
            if (s == 1)
            {
                loopAt = item;
                return true;
            }
            if (s == 2)
                return false;
            state[item] = 1;
            if (edges.TryGetValue(item, out HashSet<uint> next))
            {
                foreach (uint n in next)
                {
                    if (Visit(n, edges, state, out loopAt))
                        return true;
                }
            }
            state[item] = 2;
            return false;
        }

        private static void Error(List<ValidationIssue> issues, string message, string recipeId = null) =>
            issues.Add(new ValidationIssue { IsError = true, Message = message, RecipeId = recipeId });

        private static void Warn(List<ValidationIssue> issues, string message) => issues.Add(new ValidationIssue { IsError = false, Message = message });
    }
}
