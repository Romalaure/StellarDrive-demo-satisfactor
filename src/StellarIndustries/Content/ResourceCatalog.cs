using System.Collections.Generic;
using System.Linq;
using StellarIndustries.Core;

namespace StellarIndustries.Content
{
    internal enum ResourceCategory
    {
        Ore,
        Intermediate,
        Component,
        Advanced,
        Fluid
    }

    /// <summary>The look of an item's 3D model (built by the mod, worn with a game item's material).</summary>
    internal enum ResourceShape
    {
        Plate,
        Coil,
        Rod,
        Chunk,
        Ingot,
        Screws,
        Powder,
        HeavyPlate,
        Brick
    }

    /// <summary>
    /// One industrial resource. Pure data: the game item (ItemSettings) is made from it at load
    /// by <see cref="IndustrialItems"/>. Colors are RGB in 0-1 (tint over the donor material).
    /// </summary>
    internal sealed class ResourceDef
    {
        public uint Id;
        public string Name;
        public string Description;
        public ResourceCategory Category;
        public float Mass = 2f;
        /// <summary>Largest stack in one slot (the game's slots hold at most 255).</summary>
        public int MaxStack = 100;
        /// <summary>The game item whose material (texture, shader) the model wears.</summary>
        public uint MaterialFrom = GameItems.Iron;
        public float[] Tint = { 1f, 1f, 1f };
        public ResourceShape Shape;
        /// <summary>Tech level that makes it obtainable (documentation and validation only for now).</summary>
        public int Tier;
    }

    /// <summary>
    /// The mod's items. Only what a released phase really produces is listed: every entry here
    /// becomes a game item, saved in chests and inventories by the game itself, so an id must
    /// never be reused for another resource once released.
    /// </summary>
    internal static class ResourceCatalog
    {
        public const uint IronPlate = 7701;
        public const uint CopperWire = 7702;
        public const uint IronIngot = 7703;
        public const uint CopperIngot = 7704;
        public const uint AluminumIngot = 7705;
        public const uint IronRod = 7706;
        public const uint Screws = 7707;
        public const uint ReinforcedPlate = 7708;
        public const uint AluminumSheet = 7709;
        public const uint Cable = 7710;
        public const uint CrushedIron = 7711;
        public const uint CrushedCopper = 7712;
        public const uint StellarDust = 7713;
        public const uint BiomassBrick = 7714;
        public const uint SteelIngot = 7715;
        public const uint SteelBeam = 7716;
        public const uint SteelPipe = 7717;
        public const uint Concrete = 7718;
        public const uint CopperSheet = 7719;
        public const uint Rotor = 7720;
        public const uint Stator = 7721;
        public const uint Motor = 7722;
        public const uint AssemblyFrame = 7723;
        public const uint CircuitBoard = 7724;
        public const uint Computer = 7725;
        public const uint HeatSink = 7726;
        public const uint HeavyFrame = 7727;
        public const uint Charcoal = 7728;
        public const uint Biomass = 7729;
        public const uint Silica = 7730;
        public const uint QuartzCrystal = 7731;
        public const uint Plastic = 7732;
        public const uint Rubber = 7733;
        public const uint Resin = 7734;
        public const uint AlloyIngot = 7735;
        public const uint AlloySheet = 7736;
        public const uint AluminumCasing = 7737;
        public const uint FineWire = 7738;
        public const uint EncasedBeam = 7739;
        public const uint SmartPanel = 7740;
        public const uint Framework = 7741;
        public const uint WiringHarness = 7742;
        public const uint Oscillator = 7743;
        public const uint FastConnector = 7744;
        public const uint RadioModule = 7745;
        public const uint BatteryCell = 7746;
        public const uint CoolingSystem = 7747;
        public const uint EngineBlock = 7748;
        public const uint ControlUnit = 7749;
        public const uint Supercomputer = 7750;
        public const uint TurboMotor = 7751;
        public const uint QuantumProcessor = 7752;
        public const uint StellarMatter = 7753;
        public const uint StellarFuelRod = 7754;
        public const uint StellarWaste = 7755;
        public const uint Voucher = 7756;
        public const uint NeuroProcessor = 7757;
        public const uint TimeCrystal = 7758;
        public const uint LogicModule = 7759;
        public const uint ScannerMk1 = 7760;
        public const uint ScannerMk2 = 7761;
        public const uint ScannerMk3 = 7762;
        public const uint RangeModule = 7763;
        public const uint AnalysisModule = 7764;

        public static readonly ResourceDef[] All =
        {
            // ---- Intermediates (phase A)
            new ResourceDef
            {
                Id = IronPlate, Name = "Plaque de fer", Category = ResourceCategory.Intermediate, Mass = 1.5f, Tier = 0,
                Description = "Fer pressé en plaque. Base des pièces structurelles.",
                MaterialFrom = GameItems.Iron, Tint = new[] { 0.85f, 0.88f, 0.92f }, Shape = ResourceShape.Plate
            },
            new ResourceDef
            {
                Id = CopperWire, Name = "Fil de cuivre", Category = ResourceCategory.Intermediate, Mass = 0.5f, Tier = 0,
                Description = "Cuivre étiré en bobine. Base des composants électriques.",
                MaterialFrom = GameItems.Copper, Tint = new[] { 1f, 0.75f, 0.5f }, Shape = ResourceShape.Coil
            },
            // ---- Smelted (phase C)
            new ResourceDef
            {
                Id = IronIngot, Name = "Lingot de fer", Category = ResourceCategory.Intermediate, Mass = 2f, Tier = 0,
                Description = "Fer fondu et coulé. Se travaille mieux que le minerai brut.",
                MaterialFrom = GameItems.Iron, Tint = new[] { 0.7f, 0.72f, 0.78f }, Shape = ResourceShape.Ingot
            },
            new ResourceDef
            {
                Id = CopperIngot, Name = "Lingot de cuivre", Category = ResourceCategory.Intermediate, Mass = 2f, Tier = 0,
                Description = "Cuivre fondu et coulé.",
                MaterialFrom = GameItems.Copper, Tint = new[] { 1.1f, 0.7f, 0.45f }, Shape = ResourceShape.Ingot
            },
            new ResourceDef
            {
                Id = AluminumIngot, Name = "Lingot d'aluminium", Category = ResourceCategory.Intermediate, Mass = 1f, Tier = 1,
                Description = "Aluminium affiné, léger et résistant.",
                MaterialFrom = GameItems.Aluminum, Tint = new[] { 1.05f, 1.05f, 1.1f }, Shape = ResourceShape.Ingot
            },
            // ---- Shaped parts (phase C)
            new ResourceDef
            {
                Id = IronRod, Name = "Tige de fer", Category = ResourceCategory.Intermediate, Mass = 1f, Tier = 0,
                Description = "Barre de fer étirée. Donne les vis et les armatures.",
                MaterialFrom = GameItems.Iron, Tint = new[] { 0.8f, 0.82f, 0.86f }, Shape = ResourceShape.Rod
            },
            new ResourceDef
            {
                Id = Screws, Name = "Vis", Category = ResourceCategory.Component, Mass = 0.1f, Tier = 0, MaxStack = 200,
                Description = "Petites vis d'assemblage, par poignées.",
                MaterialFrom = GameItems.Iron, Tint = new[] { 0.75f, 0.75f, 0.7f }, Shape = ResourceShape.Screws
            },
            new ResourceDef
            {
                Id = Cable, Name = "Câble", Category = ResourceCategory.Component, Mass = 0.8f, Tier = 0,
                Description = "Fils de cuivre torsadés et gainés.",
                MaterialFrom = GameItems.Copper, Tint = new[] { 0.35f, 0.35f, 0.38f }, Shape = ResourceShape.Coil
            },
            new ResourceDef
            {
                Id = AluminumSheet, Name = "Tôle d'aluminium", Category = ResourceCategory.Component, Mass = 1f, Tier = 1,
                Description = "Tôle fine et légère pour les coques et les machines avancées.",
                MaterialFrom = GameItems.Aluminum, Tint = new[] { 1.1f, 1.1f, 1.15f }, Shape = ResourceShape.Plate
            },
            new ResourceDef
            {
                Id = ReinforcedPlate, Name = "Plaque renforcée", Category = ResourceCategory.Component, Mass = 6f, Tier = 1, MaxStack = 50,
                Description = "Plaques de fer vissées en sandwich. Structure des machines lourdes.",
                MaterialFrom = GameItems.Iron, Tint = new[] { 0.6f, 0.62f, 0.68f }, Shape = ResourceShape.HeavyPlate
            },
            // ---- Crushed (phase C)
            new ResourceDef
            {
                Id = CrushedIron, Name = "Fer broyé", Category = ResourceCategory.Ore, Mass = 1f, Tier = 1,
                Description = "Minerai de fer concassé : la fonderie en tire plus de métal.",
                MaterialFrom = GameItems.Iron, Tint = new[] { 0.55f, 0.45f, 0.42f }, Shape = ResourceShape.Powder
            },
            new ResourceDef
            {
                Id = CrushedCopper, Name = "Cuivre broyé", Category = ResourceCategory.Ore, Mass = 1f, Tier = 1,
                Description = "Minerai de cuivre concassé : la fonderie en tire plus de métal.",
                MaterialFrom = GameItems.Copper, Tint = new[] { 0.9f, 0.55f, 0.35f }, Shape = ResourceShape.Powder
            },
            new ResourceDef
            {
                Id = StellarDust, Name = "Poussière stellaire", Category = ResourceCategory.Ore, Mass = 0.3f, Tier = 1, MaxStack = 200,
                Description = "Éclats stellaires réduits en poudre scintillante. Fondue, elle donne du verre.",
                MaterialFrom = GameItems.StellarShard, Tint = new[] { 1f, 1f, 1f }, Shape = ResourceShape.Powder
            },
            // ---- Fuel (phase E)
            new ResourceDef
            {
                Id = BiomassBrick, Name = "Briquette de biomasse", Category = ResourceCategory.Component, Mass = 1.5f, Tier = 1,
                Description = "Bois broyé et pressé. Brûle cinq fois plus longtemps qu'une bûche dans le générateur à combustion.",
                MaterialFrom = GameItems.Wood, Tint = new[] { 0.6f, 0.45f, 0.3f }, Shape = ResourceShape.Brick
            },
            // ---- Steel, concrete, components (0.9)
            new ResourceDef
            {
                Id = SteelIngot, Name = "Lingot d'acier", Category = ResourceCategory.Intermediate, Mass = 2f, Tier = 1, MaxStack = 100,
                Description = "Fer affiné avec du carbone de biomasse : plus dur et plus résistant.",
                MaterialFrom = GameItems.Iron
            },
            new ResourceDef
            {
                Id = SteelBeam, Name = "Poutre d'acier", Category = ResourceCategory.Component, Mass = 6f, Tier = 1, MaxStack = 50,
                Description = "Poutre en I pour les structures lourdes.",
                MaterialFrom = GameItems.Iron
            },
            new ResourceDef
            {
                Id = SteelPipe, Name = "Tuyau d'acier", Category = ResourceCategory.Component, Mass = 2f, Tier = 1, MaxStack = 100,
                Description = "Tube d'acier : conduites, axes, bâtis.",
                MaterialFrom = GameItems.Iron
            },
            new ResourceDef
            {
                Id = Concrete, Name = "Béton", Category = ResourceCategory.Intermediate, Mass = 4f, Tier = 1, MaxStack = 50,
                Description = "Bloc de béton coulé (poussière stellaire et glace).",
                MaterialFrom = GameItems.Glass
            },
            new ResourceDef
            {
                Id = CopperSheet, Name = "Feuille de cuivre", Category = ResourceCategory.Intermediate, Mass = 1f, Tier = 1, MaxStack = 100,
                Description = "Cuivre laminé en feuille mince, pour l'électronique et les échangeurs.",
                MaterialFrom = GameItems.Copper
            },
            new ResourceDef
            {
                Id = Rotor, Name = "Rotor", Category = ResourceCategory.Component, Mass = 5f, Tier = 2, MaxStack = 50,
                Description = "Arbre et bobinages tournants d'un moteur.",
                MaterialFrom = GameItems.Iron
            },
            new ResourceDef
            {
                Id = Stator, Name = "Stator", Category = ResourceCategory.Component, Mass = 6f, Tier = 2, MaxStack = 50,
                Description = "Carcasse fixe et bobinages d'un moteur.",
                MaterialFrom = GameItems.Iron
            },
            new ResourceDef
            {
                Id = Motor, Name = "Moteur électrique", Category = ResourceCategory.Advanced, Mass = 12f, Tier = 2, MaxStack = 20,
                Description = "Rotor et stator assemblés : la force des machines avancées.",
                MaterialFrom = GameItems.Iron
            },
            new ResourceDef
            {
                Id = AssemblyFrame, Name = "Cadre d'assemblage", Category = ResourceCategory.Component, Mass = 8f, Tier = 2, MaxStack = 50,
                Description = "Cadre rigide de plaques renforcées et de tiges, base des grandes structures.",
                MaterialFrom = GameItems.Iron
            },
            new ResourceDef
            {
                Id = CircuitBoard, Name = "Circuit imprimé", Category = ResourceCategory.Component, Mass = 0.5f, Tier = 2, MaxStack = 100,
                Description = "Plaque de verre gravée de pistes de cuivre.",
                MaterialFrom = GameItems.Copper
            },
            new ResourceDef
            {
                Id = Computer, Name = "Ordinateur", Category = ResourceCategory.Advanced, Mass = 8f, Tier = 3, MaxStack = 20,
                Description = "Unité de calcul : circuits, câblage et boîtier renforcé.",
                MaterialFrom = GameItems.Iron
            },
            new ResourceDef
            {
                Id = HeatSink, Name = "Dissipateur thermique", Category = ResourceCategory.Component, Mass = 3f, Tier = 2, MaxStack = 50,
                Description = "Ailettes d'aluminium sur une semelle de cuivre.",
                MaterialFrom = GameItems.Aluminum
            },
            new ResourceDef
            {
                Id = HeavyFrame, Name = "Cadre lourd", Category = ResourceCategory.Advanced, Mass = 30f, Tier = 3, MaxStack = 20,
                Description = "Cadre d'acier, de béton et de poutres pour les machines les plus lourdes.",
                MaterialFrom = GameItems.Iron
            },
            // ---- Chemistry, alloys, electronics, top tier (0.10)
            new ResourceDef
            {
                Id = Charcoal, Name = "Charbon de bois", Category = ResourceCategory.Intermediate, Mass = 1f, Tier = 1, MaxStack = 100,
                Description = "Bois carbonisé à la raffinerie : combustible dense et carbone pour l'acier.",
                MaterialFrom = GameItems.Wood
            },
            new ResourceDef
            {
                Id = Biomass, Name = "Biomasse", Category = ResourceCategory.Ore, Mass = 0.5f, Tier = 0, MaxStack = 200,
                Description = "Bois broyé en copeaux : matière première de la chimie et combustible léger.",
                MaterialFrom = GameItems.Wood
            },
            new ResourceDef
            {
                Id = Silica, Name = "Silice", Category = ResourceCategory.Intermediate, Mass = 0.5f, Tier = 1, MaxStack = 200,
                Description = "Verre broyé en sable fin, base des cristaux de quartz.",
                MaterialFrom = GameItems.Glass
            },
            new ResourceDef
            {
                Id = QuartzCrystal, Name = "Cristal de quartz", Category = ResourceCategory.Intermediate, Mass = 0.5f, Tier = 2, MaxStack = 100,
                Description = "Cristal pur fondu de silice et de poussière stellaire : il vibre à fréquence fixe.",
                MaterialFrom = GameItems.Glass
            },
            new ResourceDef
            {
                Id = Plastic, Name = "Plastique", Category = ResourceCategory.Intermediate, Mass = 0.5f, Tier = 2, MaxStack = 100,
                Description = "Polymère tiré de la biomasse : léger, isolant.",
                MaterialFrom = GameItems.Glass
            },
            new ResourceDef
            {
                Id = Rubber, Name = "Caoutchouc", Category = ResourceCategory.Intermediate, Mass = 0.5f, Tier = 2, MaxStack = 100,
                Description = "Gomme élastique pour joints, courroies et isolants.",
                MaterialFrom = GameItems.Wood
            },
            new ResourceDef
            {
                Id = Resin, Name = "Résine polymère", Category = ResourceCategory.Intermediate, Mass = 0.3f, Tier = 2, MaxStack = 200,
                Description = "Résidu collant de la fabrication du plastique, transformé en caoutchouc.",
                MaterialFrom = GameItems.Wood
            },
            new ResourceDef
            {
                Id = AlloyIngot, Name = "Lingot d'alliage", Category = ResourceCategory.Intermediate, Mass = 1.5f, Tier = 2, MaxStack = 100,
                Description = "Aluminium allié de cuivre au haut fourneau : léger et résistant à la corrosion.",
                MaterialFrom = GameItems.Aluminum
            },
            new ResourceDef
            {
                Id = AlloySheet, Name = "Tôle d'alliage", Category = ResourceCategory.Component, Mass = 1f, Tier = 2, MaxStack = 100,
                Description = "Tôle laminée d'alliage aluminium-cuivre.",
                MaterialFrom = GameItems.Aluminum
            },
            new ResourceDef
            {
                Id = AluminumCasing, Name = "Boîtier en aluminium", Category = ResourceCategory.Component, Mass = 1f, Tier = 2, MaxStack = 100,
                Description = "Boîtier cylindrique usiné dans un lingot d'aluminium.",
                MaterialFrom = GameItems.Aluminum
            },
            new ResourceDef
            {
                Id = FineWire, Name = "Fil fin", Category = ResourceCategory.Intermediate, Mass = 0.05f, Tier = 1, MaxStack = 200,
                Description = "Fil de cuivre très fin pour l'électronique.",
                MaterialFrom = GameItems.Copper
            },
            new ResourceDef
            {
                Id = EncasedBeam, Name = "Poutre enrobée", Category = ResourceCategory.Component, Mass = 10f, Tier = 2, MaxStack = 50,
                Description = "Poutres d'acier noyées dans le béton : charpente des grands bâtiments.",
                MaterialFrom = GameItems.Iron
            },
            new ResourceDef
            {
                Id = SmartPanel, Name = "Panneau intelligent", Category = ResourceCategory.Component, Mass = 6f, Tier = 2, MaxStack = 50,
                Description = "Plaque renforcée motorisée par un rotor : panneau orientable.",
                MaterialFrom = GameItems.Iron
            },
            new ResourceDef
            {
                Id = Framework, Name = "Ossature polyvalente", Category = ResourceCategory.Component, Mass = 10f, Tier = 2, MaxStack = 50,
                Description = "Cadre d'assemblage renforcé de poutres d'acier.",
                MaterialFrom = GameItems.Iron
            },
            new ResourceDef
            {
                Id = WiringHarness, Name = "Faisceau de câbles", Category = ResourceCategory.Component, Mass = 4f, Tier = 2, MaxStack = 50,
                Description = "Stator câblé prêt à monter.",
                MaterialFrom = GameItems.Copper
            },
            new ResourceDef
            {
                Id = Oscillator, Name = "Oscillateur à quartz", Category = ResourceCategory.Component, Mass = 1f, Tier = 3, MaxStack = 50,
                Description = "Cristaux de quartz montés dans un boîtier : l'horloge des circuits.",
                MaterialFrom = GameItems.Glass
            },
            new ResourceDef
            {
                Id = FastConnector, Name = "Connecteur rapide", Category = ResourceCategory.Component, Mass = 1f, Tier = 3, MaxStack = 50,
                Description = "Connecteur multibroche à fil fin pour les liaisons rapides.",
                MaterialFrom = GameItems.Copper
            },
            new ResourceDef
            {
                Id = RadioModule, Name = "Module radio", Category = ResourceCategory.Advanced, Mass = 3f, Tier = 3, MaxStack = 20,
                Description = "Émetteur-récepteur : oscillateur, dissipateur et boîtiers.",
                MaterialFrom = GameItems.Aluminum
            },
            new ResourceDef
            {
                Id = BatteryCell, Name = "Cellule de batterie", Category = ResourceCategory.Component, Mass = 2f, Tier = 2, MaxStack = 50,
                Description = "Cellule d'accumulateur : tôles d'alliage dans un boîtier.",
                MaterialFrom = GameItems.Aluminum
            },
            new ResourceDef
            {
                Id = CoolingSystem, Name = "Système de refroidissement", Category = ResourceCategory.Advanced, Mass = 6f, Tier = 3, MaxStack = 20,
                Description = "Dissipateurs, joints de caoutchouc et glace en circuit fermé.",
                MaterialFrom = GameItems.Aluminum
            },
            new ResourceDef
            {
                Id = EngineBlock, Name = "Bloc moteur", Category = ResourceCategory.Advanced, Mass = 20f, Tier = 3, MaxStack = 20,
                Description = "Moteur électrique monté sur panneaux, avec ses courroies.",
                MaterialFrom = GameItems.Iron
            },
            new ResourceDef
            {
                Id = ControlUnit, Name = "Contrôleur adaptatif", Category = ResourceCategory.Advanced, Mass = 6f, Tier = 3, MaxStack = 20,
                Description = "Calculateur de pilotage : faisceaux, ossature et ordinateur.",
                MaterialFrom = GameItems.Iron
            },
            new ResourceDef
            {
                Id = Supercomputer, Name = "Supercalculateur", Category = ResourceCategory.Advanced, Mass = 20f, Tier = 3, MaxStack = 10,
                Description = "Grappe d'ordinateurs reliés par connecteurs rapides.",
                MaterialFrom = GameItems.Iron
            },
            new ResourceDef
            {
                Id = TurboMotor, Name = "Turbomoteur", Category = ResourceCategory.Advanced, Mass = 30f, Tier = 4, MaxStack = 10,
                Description = "Bloc moteur refroidi et piloté par radio : la puissance ultime.",
                MaterialFrom = GameItems.Iron
            },
            new ResourceDef
            {
                Id = QuantumProcessor, Name = "Processeur quantique", Category = ResourceCategory.Advanced, Mass = 2f, Tier = 4, MaxStack = 10,
                Description = "Calcul sur matière stellaire condensée, sorti de l'accélérateur.",
                MaterialFrom = GameItems.StellarShard
            },
            new ResourceDef
            {
                Id = StellarMatter, Name = "Matière stellaire condensée", Category = ResourceCategory.Advanced, Mass = 1f, Tier = 4, MaxStack = 20,
                Description = "Poussière stellaire compressée en particule lumineuse dans l'accélérateur.",
                MaterialFrom = GameItems.StellarShard
            },
            // ---- 0.12: power, conversion, recycling
            new ResourceDef
            {
                Id = StellarFuelRod, Name = "Barre de combustible stellaire", Category = ResourceCategory.Advanced, Mass = 8f, Tier = 4, MaxStack = 10,
                Description = "Matière stellaire condensée gainée d'aluminium et de béton : le combustible du réacteur stellaire.",
                MaterialFrom = GameItems.StellarShard
            },
            new ResourceDef
            {
                Id = StellarWaste, Name = "Déchet stellaire", Category = ResourceCategory.Ore, Mass = 4f, Tier = 4, MaxStack = 50,
                Description = "Ce qui reste d'une barre usée. L'encodeur quantique sait encore en tirer de la matière.",
                MaterialFrom = GameItems.StellarShard
            },
            new ResourceDef
            {
                Id = Voucher, Name = "Bon de recyclage", Category = ResourceCategory.Component, Mass = 0.01f, Tier = 1, MaxStack = 200,
                Description = "Un bon gagné au recycleur. Le comptoir d'échange l'accepte comme monnaie.",
                MaterialFrom = GameItems.Iron
            },
            new ResourceDef
            {
                Id = NeuroProcessor, Name = "Processeur neuro-quantique", Category = ResourceCategory.Advanced, Mass = 2f, Tier = 4, MaxStack = 10,
                Description = "Un processeur quantique couplé à des modules logiques : le cerveau des machines du futur.",
                MaterialFrom = GameItems.StellarShard
            },
            new ResourceDef
            {
                Id = TimeCrystal, Name = "Cristal temporel", Category = ResourceCategory.Advanced, Mass = 0.5f, Tier = 4, MaxStack = 20,
                Description = "Un cristal qui oscille hors du temps, formé dans l'encodeur quantique.",
                MaterialFrom = GameItems.Glass
            },
            new ResourceDef
            {
                Id = LogicModule, Name = "Module logique", Category = ResourceCategory.Component, Mass = 0.3f, Tier = 2, MaxStack = 100,
                Description = "Une petite carte de fil fin et de cuivre qui prend des décisions simples.",
                MaterialFrom = GameItems.Copper
            },
            // ---- 0.13: the deposit scanner, kept in the player's inventory (see UI.DepositScanner)
            new ResourceDef
            {
                Id = ScannerMk1, Name = "Scanner de gisements Mk1", Category = ResourceCategory.Component, Mass = 1f, Tier = 0, MaxStack = 5,
                Description = "Un scanner de poche : gardez-le dans votre inventaire et appuyez sur K pour lister les rochers minables à moins de 80 m, avec leur distance et leur direction.",
                MaterialFrom = GameItems.Iron
            },
            new ResourceDef
            {
                Id = ScannerMk2, Name = "Scanner de gisements Mk2", Category = ResourceCategory.Component, Mass = 1f, Tier = 1, MaxStack = 5,
                Description = "Porte à 160 m, ajoute un écran radar et donne la pureté de chaque gisement (impur, normal, pur).",
                MaterialFrom = GameItems.Iron
            },
            new ResourceDef
            {
                Id = ScannerMk3, Name = "Scanner de gisements Mk3", Category = ResourceCategory.Advanced, Mass = 1f, Tier = 2, MaxStack = 5,
                Description = "Porte à 300 m et marque les gisements directement dans votre vue pendant 30 secondes, avec leur pureté et leur rendement.",
                MaterialFrom = GameItems.Iron
            },
            new ResourceDef
            {
                Id = RangeModule, Name = "Module de portée", Category = ResourceCategory.Component, Mass = 0.3f, Tier = 1, MaxStack = 5,
                Description = "Amélioration du scanner : +25 % de portée. Deux au plus comptent, gardés dans l'inventaire.",
                MaterialFrom = GameItems.Copper
            },
            new ResourceDef
            {
                Id = AnalysisModule, Name = "Module d'analyse", Category = ResourceCategory.Advanced, Mass = 0.3f, Tier = 2, MaxStack = 5,
                Description = "Amélioration du scanner : tout scanner indique alors la pureté des gisements et le rendement qu'en tirerait un extracteur.",
                MaterialFrom = GameItems.Copper
            }
        };

        private static Dictionary<uint, ResourceDef> _byId;

        public static ResourceDef Get(uint id)
        {
            if (_byId == null)
                _byId = All.ToDictionary(r => r.Id);
            return _byId.TryGetValue(id, out ResourceDef def) ? def : null;
        }

        /// <summary>Whether a recipe may name this item: one of the mod's or one of the game's.</summary>
        public static bool Exists(uint id) => Get(id) != null || GameItems.All.Contains(id);
    }
}
