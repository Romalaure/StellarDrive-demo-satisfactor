using System.Collections.Generic;
using System.Linq;
using StellarIndustries.Content;
using StellarIndustries.Core;

namespace StellarIndustries.Production
{
    /// <summary>The look of a machine (its model is built by <see cref="Parts.MachineModels"/>).</summary>
    internal enum MachineModel
    {
        Workshop,
        Smelter,
        Press,
        Crusher,
        Assembler,
        Extractor,
        Generator,
        Container,
        Dock
    }

    /// <summary>
    /// One production machine. Pure data: the build part is made from it by
    /// <see cref="Parts.MachineParts"/>, its behaviour is <see cref="MachineSimulation"/>.
    /// </summary>
    internal sealed class MachineDef
    {
        public ushort PartId;
        public string Name;
        public string Description;
        public MachineModel Model;
        /// <summary>Power drawn while producing (watts). The game's wood generator gives 4000 W.</summary>
        public float Watts;
        /// <summary>Recipe durations are divided by this.</summary>
        public float Speed = 1f;
        /// <summary>Inventory slots (the game's storage state: always 10).</summary>
        public int Slots = 10;
        public float Mass;
        public int Tier;
        /// <summary>Extractor: produces the item of the nearest minable rock instead of running recipes.</summary>
        public bool Extracts;
        /// <summary>Extractor: seconds per item at speed 1, and how far (meters) it reaches for a rock.</summary>
        public float ExtractSeconds = 6f, ExtractRadius = 6f;
        /// <summary>Generator: burns the fuels of <see cref="Fuels"/> from its slots and gives up to <see cref="Watts"/> to its network.</summary>
        public bool Generates;
        /// <summary>Plain storage (no production): a buffer for conveyors and transfers.</summary>
        public bool Container;
        /// <summary>Loading dock: sends its contents to a receiving dock of another ship (or part) within <see cref="DockRange"/>.</summary>
        public bool Dock;
        public float DockRange = 4f;
        /// <summary>Dock: items moved per second.</summary>
        public int DockRate = 4;
        /// <summary>No production, no recipe: a container or a dock.</summary>
        public bool IsLogistics => Container || Dock;
        /// <summary>Placement box (meters): width (x), height (y), depth (z).</summary>
        public float Width = 1.4f, Height = 1.5f, Depth = 1.2f;
        public (uint Item, int Count)[] BuildCost;
    }

    internal static class MachineCatalog
    {
        public const ushort Workshop = 7601;
        public const ushort Smelter = 7602;
        public const ushort Press = 7603;
        public const ushort Crusher = 7604;
        public const ushort Assembler = 7605;
        public const ushort Extractor = 7606;
        public const ushort CombustionGenerator = 7607;
        public const ushort IndustrialContainer = 7608;
        public const ushort LoadingDock = 7609;

        public static readonly MachineDef[] All =
        {
            new MachineDef
            {
                PartId = Workshop, Name = "Atelier industriel", Model = MachineModel.Workshop, Tier = 0, Watts = 300f, Mass = 120f,
                Description = "Façonne les métaux : plaques et tiges de fer, fil de cuivre. Accepte aussi le minerai brut (moins rentable). " +
                              "Branchez sa prise arrière au réseau électrique ; il produit tant qu'il a de l'énergie, de la matière et de la place.",
                BuildCost = new[] { (GameItems.Iron, 10), (GameItems.Copper, 4), (GameItems.Glass, 2) }
            },
            new MachineDef
            {
                PartId = Smelter, Name = "Fonderie", Model = MachineModel.Smelter, Tier = 0, Watts = 400f, Mass = 160f, Height = 1.7f,
                Description = "Fond le minerai en lingots (fer, cuivre, aluminium) et la poussière stellaire en verre. " +
                              "Le minerai broyé donne plus de lingots.",
                BuildCost = new[] { (GameItems.Iron, 14), (GameItems.Copper, 4), (GameItems.Glass, 4) }
            },
            new MachineDef
            {
                PartId = Press, Name = "Presse", Model = MachineModel.Press, Tier = 1, Watts = 600f, Mass = 200f, Height = 1.9f,
                Description = "Presse et coupe : vis, câbles, tôles d'aluminium.",
                BuildCost = new[] { (ResourceCatalog.IronPlate, 8), (ResourceCatalog.IronRod, 6), (ResourceCatalog.CopperWire, 6) }
            },
            new MachineDef
            {
                PartId = Crusher, Name = "Broyeur", Model = MachineModel.Crusher, Tier = 1, Watts = 350f, Mass = 180f,
                Description = "Concasse le minerai de fer et de cuivre (la fonderie en tire plus de lingots) et réduit les éclats stellaires en poussière.",
                BuildCost = new[] { (ResourceCatalog.IronPlate, 10), (ResourceCatalog.IronRod, 4), (ResourceCatalog.CopperWire, 4) }
            },
            new MachineDef
            {
                PartId = Assembler, Name = "Assembleuse", Model = MachineModel.Assembler, Tier = 1, Watts = 800f, Mass = 220f, Width = 1.6f, Height = 1.6f,
                Description = "Assemble plusieurs pièces en composants : plaques renforcées.",
                BuildCost = new[] { (ResourceCatalog.IronPlate, 12), (ResourceCatalog.Screws, 40), (ResourceCatalog.Cable, 6) }
            },
            new MachineDef
            {
                PartId = Extractor, Name = "Extracteur", Model = MachineModel.Extractor, Tier = 0, Watts = 700f, Mass = 250f, Height = 2.2f,
                Extracts = true, ExtractSeconds = 6f, ExtractRadius = 6f,
                Description = "Fore le rocher minable le plus proche (à moins de 6 m) et en tire son minerai sans l'épuiser : " +
                              "1 objet toutes les 6 s. Posez-le sur un vaisseau ou une base au sol, à côté d'un rocher, et alimentez-le.",
                BuildCost = new[] { (GameItems.Iron, 20), (GameItems.Copper, 8), (GameItems.Glass, 2) }
            },
            new MachineDef
            {
                PartId = CombustionGenerator, Name = "Générateur à combustion", Model = MachineModel.Generator, Tier = 1, Watts = 6000f, Mass = 260f,
                Generates = true,
                Description = "Brûle bois ou briquettes de biomasse et alimente son réseau électrique jusqu'à 6 000 W. " +
                              "Ne brûle que ce que le réseau consomme. Une briquette (presse : 4 bois) vaut 5 bois.",
                BuildCost = new[] { (ResourceCatalog.IronPlate, 10), (ResourceCatalog.Cable, 6), (ResourceCatalog.IronRod, 6) }
            },
            new MachineDef
            {
                PartId = IndustrialContainer, Name = "Conteneur industriel", Model = MachineModel.Container, Tier = 2, Watts = 0f, Mass = 60f, Height = 1.2f,
                Container = true,
                Description = "Caisse renforcée de 10 emplacements : tampon entre machines (lignes d'objets) ou soute d'un vaisseau cargo.",
                BuildCost = new[] { (ResourceCatalog.IronPlate, 8), (ResourceCatalog.Screws, 16) }
            },
            new MachineDef
            {
                PartId = LoadingDock, Name = "Quai de transfert", Model = MachineModel.Dock, Tier = 2, Watts = 0f, Mass = 90f, Height = 1.4f,
                Dock = true, DockRange = 4f, DockRate = 4,
                Description = "Transfère des objets vers un autre quai, par exemple d'un vaisseau cargo amarré à une base. " +
                              "Visez-le et appuyez sur N : arrêt, envoi ou réception. Un quai en envoi vide son contenu (4 objets/s) " +
                              "dans le quai en réception le plus proche, à moins de 4 m, même sur un autre vaisseau.",
                BuildCost = new[] { (ResourceCatalog.ReinforcedPlate, 2), (ResourceCatalog.Cable, 4), (ResourceCatalog.IronRod, 6) }
            }
        };

        /// <summary>What a generator burns: item and energy per item (joules). The game's wood generator gets 40 kJ from a log.</summary>
        public static readonly (uint Item, float Joules)[] Fuels =
        {
            (GameItems.Wood, 40000f),
            (ResourceCatalog.BiomassBrick, 200000f)
        };

        public static float FuelEnergy(uint item)
        {
            foreach (var fuel in Fuels)
            {
                if (fuel.Item == item)
                    return fuel.Joules;
            }
            return 0f;
        }

        private static Dictionary<ushort, MachineDef> _byId;

        public static bool TryGet(ushort partId, out MachineDef def)
        {
            if (_byId == null)
                _byId = All.ToDictionary(m => m.PartId);
            return _byId.TryGetValue(partId, out def);
        }
    }
}
