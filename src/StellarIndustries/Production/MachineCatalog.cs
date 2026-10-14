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
        Dock,
        Fabricator,
        Foundry,
        Refinery,
        Accumulator,
        Accelerator,
        BiomassBurner,
        Condenser,
        Reactor,
        Encoder,
        Converter,
        Recycler,
        Exchange,
        Radar,
        Belt,
        Lift,
        Splitter,
        Merger,
        Extractor2,
        DeepDrill
    }

    /// <summary>How a conveyor part takes and gives items (see <see cref="Logistics.Conveyors"/>).</summary>
    internal enum ConveyorKind
    {
        None,
        /// <summary>From the part behind it to the part in front.</summary>
        Belt,
        /// <summary>From the part behind its foot to the part in front of its top.</summary>
        Lift,
        /// <summary>From behind, shared in turn between the front, left and right.</summary>
        Splitter,
        /// <summary>From behind, left and right, all to the front.</summary>
        Merger
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
        /// <summary>Accumulator: stores up to <see cref="StoreJoules"/>, charges and discharges at <see cref="Watts"/>.</summary>
        public bool Stores;
        public float StoreJoules;
        /// <summary>Makes this item from nothing but power (condenser), one every <see cref="ExtractSeconds"/>.</summary>
        public uint FixedOutput;
        /// <summary>Generator: its own fuels (null: <see cref="MachineCatalog.Fuels"/>) and the waste left by each burnt fuel item.</summary>
        public (uint Item, float Joules)[] OwnFuels;
        public uint WasteItem;
        /// <summary>Recycler: destroys any item and pays one <see cref="ResourceCatalog.Voucher"/> per 100 points of value.</summary>
        public bool Sinks;
        /// <summary>Radar tower: the deposit scanner reaches <see cref="RadarRange"/> m near it.</summary>
        public bool Radar;
        public float RadarRange = 400f;
        /// <summary>Second build tab (power, extraction, logistics) instead of the production one.</summary>
        public bool UtilityTab;
        /// <summary>Plain storage (no production): a buffer for conveyors and transfers.</summary>
        public bool Container;
        /// <summary>Loading dock: sends its contents to a receiving dock of another ship (or part) within <see cref="DockRange"/>.</summary>
        public bool Dock;
        public float DockRange = 4f;
        /// <summary>Dock: items moved per second.</summary>
        public int DockRate = 4;
        /// <summary>Conveyor: carries items between the parts at its ends, <see cref="ConveyorRate"/> per second, at most <see cref="ConveyorHold"/> at once.</summary>
        public ConveyorKind Conveyor;
        public float ConveyorRate;
        public int ConveyorHold = 4;
        public bool IsConveyor => Conveyor != ConveyorKind.None;
        /// <summary>One line on what the machine is for (shown first when it is aimed at).</summary>
        public string Role;
        /// <summary>No production, no recipe: a container, a dock or a conveyor.</summary>
        public bool IsLogistics => Container || Dock || IsConveyor;
        /// <summary>No recipe at all: extractor, generator, accumulator, container, dock.</summary>
        public bool HasNoRecipe => Extracts || Generates || Stores || IsLogistics || FixedOutput != 0 || Sinks || Radar;
        /// <summary>Model box (meters) the model is designed in: width (x), height (y), depth (z).</summary>
        public float Width = 1.4f, Height = 1.5f, Depth = 1.2f;
        /// <summary>The whole machine (model, box, sockets) is built this many times bigger.</summary>
        public float Scale = 1f;
        public float SizeX => Width * Scale;
        public float SizeY => Height * Scale;
        public float SizeZ => Depth * Scale;
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
        public const ushort Fabricator = 7610;
        public const ushort Foundry = 7611;
        public const ushort Refinery = 7612;
        public const ushort Accumulator = 7613;
        public const ushort Accelerator = 7614;
        public const ushort BiomassBurner = 7615;
        public const ushort WaterCondenser = 7616;
        public const ushort StellarReactor = 7617;
        public const ushort QuantumEncoder = 7618;
        public const ushort Converter = 7619;
        public const ushort Recycler = 7620;
        public const ushort ExchangeCounter = 7621;
        public const ushort RadarTower = 7622;
        public const ushort BeltMk1 = 7623;
        public const ushort BeltMk2 = 7624;
        public const ushort BeltMk3 = 7625;
        public const ushort ConveyorLift = 7626;
        public const ushort Splitter = 7627;
        public const ushort Merger = 7628;
        public const ushort ExtractorMk2 = 7632;
        public const ushort DeepDrill = 7633;

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
                PartId = Smelter, Name = "Fonderie", Model = MachineModel.Smelter, Scale = 1.1f, Tier = 0, Watts = 400f, Mass = 160f, Height = 1.7f,
                Description = "Fond le minerai en lingots (fer, cuivre, aluminium) et la poussière stellaire en verre. " +
                              "Le minerai broyé donne plus de lingots.",
                BuildCost = new[] { (GameItems.Iron, 14), (GameItems.Copper, 4), (GameItems.Glass, 4) }
            },
            new MachineDef
            {
                PartId = Press, Name = "Presse", Model = MachineModel.Press, Scale = 1.25f, Tier = 1, Watts = 600f, Mass = 200f, Height = 1.9f,
                Description = "Presse et coupe : vis, câbles, tôles d'aluminium.",
                BuildCost = new[] { (ResourceCatalog.IronPlate, 8), (ResourceCatalog.IronRod, 6), (ResourceCatalog.CopperWire, 6) }
            },
            new MachineDef
            {
                PartId = Crusher, Name = "Broyeur", Model = MachineModel.Crusher, Scale = 1.25f, Tier = 1, Watts = 350f, Mass = 180f,
                Description = "Concasse le minerai de fer et de cuivre (la fonderie en tire plus de lingots) et réduit les éclats stellaires en poussière.",
                BuildCost = new[] { (ResourceCatalog.IronPlate, 10), (ResourceCatalog.IronRod, 4), (ResourceCatalog.CopperWire, 4) }
            },
            new MachineDef
            {
                PartId = Assembler, Name = "Assembleuse", Model = MachineModel.Assembler, Scale = 1.3f, Tier = 1, Watts = 800f, Mass = 220f, Width = 1.6f, Height = 1.6f,
                Description = "Assemble plusieurs pièces en composants : plaques renforcées.",
                BuildCost = new[] { (ResourceCatalog.IronPlate, 12), (ResourceCatalog.Screws, 40), (ResourceCatalog.Cable, 6) }
            },
            new MachineDef
            {
                PartId = Extractor, Name = "Extracteur", Model = MachineModel.Extractor, Scale = 1.3f, Tier = 0, Watts = 700f, Mass = 250f, Height = 2.2f,
                Extracts = true, ExtractSeconds = 6f, ExtractRadius = 6f,
                Description = "Fore le rocher minable le plus proche (à moins de 6 m) et en tire son minerai sans l'épuiser : " +
                              "1 objet toutes les 6 s. Posez-le sur un vaisseau ou une base au sol, à côté d'un rocher, et alimentez-le.",
                BuildCost = new[] { (GameItems.Iron, 20), (GameItems.Copper, 8), (GameItems.Glass, 2) }
            },
            new MachineDef
            {
                PartId = CombustionGenerator, Name = "Générateur à combustion", Model = MachineModel.Generator, Scale = 1.3f, Tier = 1, Watts = 6000f, Mass = 260f,
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
            },
            new MachineDef
            {
                PartId = Fabricator, Name = "Fabricateur", Model = MachineModel.Fabricator, Scale = 1.6f, Tier = 2, Watts = 1500f, Mass = 320f, Width = 1.6f, Height = 1.7f,
                Description = "Cellule robotisée à deux bras : moteurs, ordinateurs, cadres lourds (jusqu'à 4 ingrédients).",
                BuildCost = new[] { (ResourceCatalog.AssemblyFrame, 4), (ResourceCatalog.Rotor, 4), (ResourceCatalog.Cable, 20) }
            },
            new MachineDef
            {
                PartId = Foundry, Name = "Haut fourneau", Model = MachineModel.Foundry, Scale = 1.45f, Tier = 2, Watts = 900f, Mass = 300f, Height = 2.1f,
                Description = "Four à deux entrées : acier au charbon de bois (meilleur rendement), alliage aluminium-cuivre, cristaux de quartz.",
                BuildCost = new[] { (ResourceCatalog.SteelBeam, 6), (ResourceCatalog.Concrete, 10), (ResourceCatalog.ReinforcedPlate, 4) }
            },
            new MachineDef
            {
                PartId = Refinery, Name = "Raffinerie", Model = MachineModel.Refinery, Scale = 1.6f, Tier = 2, Watts = 1000f, Mass = 280f, Height = 2.0f,
                Description = "Chimie de la biomasse : charbon de bois, résine, plastique, caoutchouc (coproduits).",
                BuildCost = new[] { (ResourceCatalog.SteelPipe, 12), (ResourceCatalog.SteelBeam, 4), (ResourceCatalog.Motor, 1) }
            },
            new MachineDef
            {
                PartId = Accumulator, Name = "Accumulateur", Model = MachineModel.Accumulator, Scale = 1.25f, Tier = 2, Watts = 3000f, Mass = 400f, Height = 1.6f,
                Stores = true, StoreJoules = 60000000f,
                Description = "Stocke 60 MJ : se charge du surplus de son réseau, le rend quand le réseau manque (3 kW). La charge est gardée dans la sauvegarde.",
                BuildCost = new[] { (ResourceCatalog.BatteryCell, 10), (ResourceCatalog.AlloySheet, 8), (ResourceCatalog.Cable, 12) }
            },
            new MachineDef
            {
                PartId = Accelerator, Name = "Accélérateur de particules", Model = MachineModel.Accelerator, Scale = 2.0f, Tier = 3, Watts = 8000f, Mass = 600f,
                Width = 1.8f, Height = 2.2f, Depth = 1.6f,
                Description = "Anneau d'accélération : condense la poussière stellaire en matière et fabrique les processeurs quantiques. Gros consommateur (8 kW).",
                BuildCost = new[] { (ResourceCatalog.HeavyFrame, 4), (ResourceCatalog.Supercomputer, 1), (ResourceCatalog.CoolingSystem, 4) }
            },
            new MachineDef
            {
                PartId = BiomassBurner, Name = "Brûleur à biomasse", Model = MachineModel.BiomassBurner, Tier = 0, Watts = 1500f, Mass = 80f,
                Width = 1.0f, Height = 1.5f, Depth = 1.0f, Generates = true, UtilityTab = true,
                Description = "Un petit poêle industriel pour démarrer : il brûle bois, biomasse, briquettes ou charbon de bois et donne jusqu'à 1 500 W.",
                BuildCost = new[] { (GameItems.Iron, 12), (GameItems.Copper, 4) }
            },
            new MachineDef
            {
                PartId = WaterCondenser, Name = "Condenseur d'eau", Model = MachineModel.Condenser, Scale = 1.15f, Tier = 1, Watts = 400f, Mass = 150f,
                Width = 1.2f, Height = 1.8f, Depth = 1.2f, FixedOutput = GameItems.Ice, ExtractSeconds = 8f, UtilityTab = true,
                Description = "Refroidit l'air et en tire de la glace, n'importe où : une glace toutes les 8 s tant qu'il a du courant et de la place.",
                BuildCost = new[] { (ResourceCatalog.CopperSheet, 6), (ResourceCatalog.IronPlate, 8), (ResourceCatalog.Motor, 1) }
            },
            new MachineDef
            {
                PartId = StellarReactor, Name = "Réacteur stellaire", Model = MachineModel.Reactor, Scale = 2.0f, Tier = 4, Watts = 25000f, Mass = 900f,
                Width = 1.6f, Height = 1.8f, Depth = 1.4f, Generates = true, UtilityTab = true,
                OwnFuels = new[] { (ResourceCatalog.StellarFuelRod, 7500000f) }, WasteItem = ResourceCatalog.StellarWaste,
                Description = "La centrale ultime : 25 kW à partir de barres de combustible stellaire (5 minutes chacune à pleine puissance). Chaque barre usée laisse un déchet stellaire, qu'il faut sortir de la machine.",
                BuildCost = new[] { (ResourceCatalog.HeavyFrame, 6), (ResourceCatalog.CoolingSystem, 6), (ResourceCatalog.ControlUnit, 2) }
            },
            new MachineDef
            {
                PartId = QuantumEncoder, Name = "Encodeur quantique", Model = MachineModel.Encoder, Scale = 1.6f, Tier = 4, Watts = 10000f, Mass = 500f,
                Width = 1.4f, Height = 1.8f, Depth = 1.2f,
                Description = "Le sommet de la chaîne : cristaux temporels, processeurs neuro-quantiques et recyclage des déchets stellaires.",
                BuildCost = new[] { (ResourceCatalog.Supercomputer, 2), (ResourceCatalog.CoolingSystem, 4), (ResourceCatalog.HeavyFrame, 2) }
            },
            new MachineDef
            {
                PartId = Converter, Name = "Convertisseur", Model = MachineModel.Converter, Scale = 1.4f, Tier = 3, Watts = 2500f, Mass = 300f,
                Width = 1.4f, Height = 1.6f, Depth = 1.2f,
                Description = "Transforme un minerai en un autre grâce à la poussière stellaire : fer en cuivre, cuivre en aluminium, poussière en éclats stellaires.",
                BuildCost = new[] { (ResourceCatalog.Framework, 2), (ResourceCatalog.Oscillator, 2), (ResourceCatalog.Cable, 20) }
            },
            new MachineDef
            {
                PartId = Recycler, Name = "Recycleur", Model = MachineModel.Recycler, Scale = 1.2f, Tier = 2, Watts = 500f, Mass = 200f,
                Sinks = true, UtilityTab = true,
                Description = "Détruit ce dont vous n'avez plus besoin et vous paie en bons de recyclage : plus un objet est élaboré, plus il rapporte. Les bons s'échangent au comptoir.",
                BuildCost = new[] { (ResourceCatalog.ReinforcedPlate, 4), (ResourceCatalog.Rotor, 2), (ResourceCatalog.Cable, 10) }
            },
            new MachineDef
            {
                PartId = ExchangeCounter, Name = "Comptoir d'échange", Model = MachineModel.Exchange, Tier = 2, Watts = 100f, Mass = 120f,
                UtilityTab = true,
                Description = "Échange vos bons de recyclage contre des ressources : minerais, éclats stellaires, et même de la matière stellaire.",
                BuildCost = new[] { (ResourceCatalog.IronPlate, 10), (ResourceCatalog.CircuitBoard, 2), (GameItems.Glass, 4) }
            },
            new MachineDef
            {
                PartId = RadarTower, Name = "Tour radar", Model = MachineModel.Radar, Scale = 1.4f, Tier = 1, Watts = 0f, Mass = 160f,
                Width = 1.2f, Height = 2.6f, Depth = 1.2f, Radar = true, RadarRange = 400f, UtilityTab = true,
                Description = "Une antenne qui balaie les environs : à moins de 60 m d'une tour, le scanner de gisements (touche K) porte jusqu'à 400 m.",
                BuildCost = new[] { (ResourceCatalog.SteelBeam, 4), (ResourceCatalog.Cable, 10), (ResourceCatalog.CircuitBoard, 2) }
            },
            // ---- 0.13: conveyors (front towards +z: the items go that way)
            new MachineDef
            {
                PartId = BeltMk1, Name = "Convoyeur Mk1", Model = MachineModel.Belt, Tier = 0, Mass = 15f, Width = 0.8f, Height = 0.35f, Depth = 1f,
                Conveyor = ConveyorKind.Belt, ConveyorRate = 1f, ConveyorHold = 4,
                Description = "Un tapis roulant d'un mètre : il prend les produits de la machine (ou du coffre) derrière lui et les pose dans la machine, le coffre " +
                              "ou le convoyeur devant lui, 1 objet par seconde. Mettez-en plusieurs bout à bout ; les flèches montrent le sens.",
                BuildCost = new[] { (ResourceCatalog.IronPlate, 1), (ResourceCatalog.IronRod, 1) }
            },
            new MachineDef
            {
                PartId = BeltMk2, Name = "Convoyeur Mk2", Model = MachineModel.Belt, Tier = 1, Mass = 18f, Width = 0.8f, Height = 0.35f, Depth = 1f,
                Conveyor = ConveyorKind.Belt, ConveyorRate = 2.5f, ConveyorHold = 5,
                Description = "Tapis renforcé : 2,5 objets par seconde. Se pose et se raccorde comme le Mk1.",
                BuildCost = new[] { (ResourceCatalog.ReinforcedPlate, 1), (ResourceCatalog.IronRod, 2) }
            },
            new MachineDef
            {
                PartId = BeltMk3, Name = "Convoyeur Mk3", Model = MachineModel.Belt, Tier = 2, Mass = 22f, Width = 0.8f, Height = 0.35f, Depth = 1f,
                Conveyor = ConveyorKind.Belt, ConveyorRate = 6f, ConveyorHold = 8,
                Description = "Tapis lourd en acier et caoutchouc : 6 objets par seconde, pour les grosses chaînes.",
                BuildCost = new[] { (ResourceCatalog.SteelBeam, 1), (ResourceCatalog.Rubber, 2), (ResourceCatalog.AlloySheet, 1) }
            },
            new MachineDef
            {
                PartId = ConveyorLift, Name = "Élévateur à godets", Model = MachineModel.Lift, Scale = 1.5f, Tier = 1, Mass = 60f, Width = 0.8f, Height = 2f, Depth = 1f,
                Conveyor = ConveyorKind.Lift, ConveyorRate = 2.5f, ConveyorHold = 6,
                Description = "Monte les objets de trois mètres : il prend derrière son pied et dépose devant son sommet (2,5 objets par seconde). " +
                              "Pour passer d'un pont à l'autre ou remplir une machine haute.",
                BuildCost = new[] { (ResourceCatalog.IronPlate, 4), (ResourceCatalog.IronRod, 4), (ResourceCatalog.Rotor, 1) }
            },
            new MachineDef
            {
                PartId = Splitter, Name = "Répartiteur", Model = MachineModel.Splitter, Tier = 0, Mass = 20f, Width = 1f, Height = 0.6f, Depth = 1f,
                Conveyor = ConveyorKind.Splitter, ConveyorRate = 4f, ConveyorHold = 4,
                Description = "Reçoit par l'arrière et distribue à tour de rôle vers l'avant, la gauche et la droite (4 objets par seconde) : " +
                              "une ligne de minerai alimente ainsi plusieurs fonderies. Une sortie bouchée est sautée.",
                BuildCost = new[] { (ResourceCatalog.IronPlate, 2), (ResourceCatalog.CopperWire, 2) }
            },
            new MachineDef
            {
                PartId = Merger, Name = "Fusionneur", Model = MachineModel.Merger, Tier = 0, Mass = 20f, Width = 1f, Height = 0.6f, Depth = 1f,
                Conveyor = ConveyorKind.Merger, ConveyorRate = 4f, ConveyorHold = 4,
                Description = "Rassemble sur une seule ligne ce qui arrive par l'arrière, la gauche et la droite, et l'envoie vers l'avant (4 objets par seconde).",
                BuildCost = new[] { (ResourceCatalog.IronPlate, 2), (ResourceCatalog.CopperWire, 2) }
            },
            // ---- 0.17: better extractors
            new MachineDef
            {
                PartId = ExtractorMk2, Name = "Extracteur Mk2", Model = MachineModel.Extractor2, Scale = 1.45f, Tier = 2, Watts = 1500f, Mass = 380f,
                Width = 1.4f, Height = 2.5f, Depth = 1.2f, Extracts = true, ExtractSeconds = 3f, ExtractRadius = 8f, UtilityTab = true,
                Description = "Un mât de forage à tête rotative : deux fois plus rapide que l'extracteur (1 objet toutes les 3 s) et il atteint un rocher jusqu'à 8 m. " +
                              "Sur un gisement inépuisable, il va encore 2,5 fois plus vite.",
                BuildCost = new[] { (ResourceCatalog.SteelBeam, 6), (ResourceCatalog.Motor, 2), (ResourceCatalog.CircuitBoard, 2) }
            },
            new MachineDef
            {
                PartId = DeepDrill, Name = "Foreuse de gisement", Model = MachineModel.DeepDrill, Scale = 1.8f, Tier = 3, Watts = 4000f, Mass = 700f,
                Width = 1.6f, Height = 2.7f, Depth = 1.4f, Extracts = true, ExtractSeconds = 1.5f, ExtractRadius = 6f, UtilityTab = true,
                Description = "Un derrick à table rotative et trois tiges : 1 objet toutes les 1,5 s, 4 kW. Posée sur un gisement inépuisable, elle en tire 2,5 fois plus.",
                BuildCost = new[] { (ResourceCatalog.HeavyFrame, 4), (ResourceCatalog.TurboMotor, 1), (ResourceCatalog.ControlUnit, 1) }
            }
        };

        /// <summary>One line on what each machine is for (French; English in <see cref="Content.English.Roles"/>).</summary>
        private static readonly Dictionary<ushort, string> Roles = new Dictionary<ushort, string>
        {
            [Workshop] = "Façonne lingots et minerai en pièces de base",
            [Smelter] = "Fond le minerai en lingots",
            [Press] = "Presse, coupe et compacte les pièces",
            [Crusher] = "Broie le minerai pour plus de rendement",
            [Assembler] = "Assemble deux pièces en un composant",
            [Extractor] = "Extrait le minerai d'un rocher proche, sans fin",
            [CombustionGenerator] = "Produit du courant en brûlant du combustible",
            [IndustrialContainer] = "Stocke : tampon entre machines",
            [LoadingDock] = "Transfère des objets vers un autre quai",
            [Fabricator] = "Fabrique des pièces complexes (jusqu'à 4 ingrédients)",
            [Foundry] = "Four à deux entrées : acier, alliages, quartz",
            [Refinery] = "Chimie : charbon, plastique, caoutchouc",
            [Accumulator] = "Stocke le surplus de courant et le rend",
            [Accelerator] = "Matière stellaire et processeurs quantiques",
            [BiomassBurner] = "Petit générateur de début de partie",
            [WaterCondenser] = "Produit de la glace à partir de l'air",
            [StellarReactor] = "Centrale électrique de fin de partie",
            [QuantumEncoder] = "Fabrique le haut de gamme quantique",
            [Converter] = "Change un minerai en un autre",
            [Recycler] = "Détruit le surplus contre des bons",
            [ExchangeCounter] = "Échange les bons contre des ressources",
            [RadarTower] = "Augmente la portée du scanner de gisements",
            [BeltMk1] = "Transporte les objets d'une machine à l'autre",
            [BeltMk2] = "Transporte les objets d'une machine à l'autre",
            [BeltMk3] = "Transporte les objets d'une machine à l'autre",
            [ConveyorLift] = "Monte les objets d'un étage",
            [Splitter] = "Partage une ligne entre trois sorties",
            [Merger] = "Réunit trois lignes en une",
            [ExtractorMk2] = "Extrait le minerai deux fois plus vite, jusqu'à 8 m",
            [DeepDrill] = "Fore les gisements à grande vitesse"
        };

        static MachineCatalog()
        {
            foreach (MachineDef def in All)
            {
                if (Roles.TryGetValue(def.PartId, out string role))
                    def.Role = role;
            }
        }

        /// <summary>What a generator burns: item and energy per item (joules). The game's wood generator gets 40 kJ from a log.</summary>
        public static readonly (uint Item, float Joules)[] Fuels =
        {
            (GameItems.Wood, 40000f),
            (ResourceCatalog.BiomassBrick, 200000f),
            (ResourceCatalog.Biomass, 15000f),
            (ResourceCatalog.Charcoal, 300000f),
            (ResourceCatalog.Coal, 250000f)
        };

        /// <summary>What this generator burns.</summary>
        public static (uint Item, float Joules)[] FuelsOf(MachineDef def) => def?.OwnFuels ?? Fuels;

        public static float FuelEnergy(MachineDef def, uint item)
        {
            foreach (var fuel in FuelsOf(def))
            {
                if (fuel.Item == item)
                    return fuel.Joules;
            }
            return 0f;
        }

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
