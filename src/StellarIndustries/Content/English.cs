using System.Collections.Generic;

namespace StellarIndustries.Content
{
    /// <summary>
    /// English names and descriptions of the mod's content. The catalogs hold the French text;
    /// this table holds the English one, keyed by id. Anything missing here falls back to French.
    /// </summary>
    internal static class English
    {
        public static readonly Dictionary<uint, (string Name, string Description)> Items = new Dictionary<uint, (string, string)>
        {
            [7701] = ("Iron Plate", "Iron pressed into a plate. The base of most structural parts."),
            [7702] = ("Copper Wire", "Copper drawn into a coil. The base of every electrical part."),
            [7703] = ("Iron Ingot", "Molten iron cast into a bar. Much nicer to work with than raw ore."),
            [7704] = ("Copper Ingot", "Molten copper cast into a bar."),
            [7705] = ("Aluminum Ingot", "Refined aluminum, light and strong."),
            [7706] = ("Iron Rod", "A drawn iron bar. Becomes screws and reinforcement."),
            [7707] = ("Screws", "Small assembly screws, by the handful."),
            [7708] = ("Reinforced Plate", "Iron plates screwed into a sandwich. The backbone of heavy machines."),
            [7709] = ("Aluminum Sheet", "A thin, light sheet for hulls and advanced machines."),
            [7710] = ("Cable", "Copper wires twisted together and sheathed."),
            [7711] = ("Crushed Iron", "Crushed iron ore: the smelter gets more metal out of it."),
            [7712] = ("Crushed Copper", "Crushed copper ore: the smelter gets more metal out of it."),
            [7713] = ("Stellar Dust", "Stellar shards ground into a sparkling powder. Smelted, it becomes glass."),
            [7714] = ("Biomass Brick", "Pressed wood. Burns five times longer than a log in the combustion generator."),
            [7715] = ("Steel Ingot", "Iron refined with carbon from biomass: harder and tougher."),
            [7716] = ("Steel Beam", "An I-beam for heavy structures."),
            [7717] = ("Steel Pipe", "A steel tube: ducts, shafts, frames."),
            [7718] = ("Concrete", "A cast block of concrete, made from stellar dust and ice."),
            [7719] = ("Copper Sheet", "Copper rolled thin, for electronics and heat exchangers."),
            [7720] = ("Rotor", "The turning shaft and windings of a motor."),
            [7721] = ("Stator", "The fixed housing and windings of a motor."),
            [7722] = ("Electric Motor", "A rotor and a stator put together: the muscle of advanced machines."),
            [7723] = ("Assembly Frame", "A stiff frame of reinforced plates and rods, the base of large structures."),
            [7724] = ("Circuit Board", "A glass board etched with copper tracks."),
            [7725] = ("Computer", "A computing unit: circuits, wiring and a reinforced case."),
            [7726] = ("Heat Sink", "Aluminum fins on a copper base."),
            [7727] = ("Heavy Frame", "Steel, concrete and beams for the heaviest machines."),
            [7728] = ("Charcoal", "Wood charred in the refinery: a dense fuel, and the carbon for steel."),
            [7729] = ("Biomass", "Wood chipped into flakes: raw material for chemistry, and a light fuel."),
            [7730] = ("Silica", "Glass ground into fine sand, the base of quartz crystals."),
            [7731] = ("Quartz Crystal", "A pure crystal grown from silica and stellar dust: it rings at a fixed frequency."),
            [7732] = ("Plastic", "A polymer made from biomass: light and insulating."),
            [7733] = ("Rubber", "Elastic gum for seals, belts and insulation."),
            [7734] = ("Polymer Resin", "The sticky leftover of plastic making, turned into rubber."),
            [7735] = ("Alloy Ingot", "Aluminum alloyed with copper in the blast furnace: light and corrosion-proof."),
            [7736] = ("Alloy Sheet", "A rolled sheet of aluminum-copper alloy."),
            [7737] = ("Aluminum Casing", "A cylindrical casing machined from an aluminum ingot."),
            [7738] = ("Fine Wire", "Very thin copper wire for electronics."),
            [7739] = ("Encased Beam", "Steel beams set in concrete: the frame of large buildings."),
            [7740] = ("Smart Panel", "A reinforced plate driven by a rotor: a panel that can turn."),
            [7741] = ("Framework", "An assembly frame reinforced with steel beams."),
            [7742] = ("Wiring Harness", "A stator wired and ready to fit."),
            [7743] = ("Quartz Oscillator", "Quartz crystals in a casing: the clock of every circuit."),
            [7744] = ("Fast Connector", "A multi-pin connector of fine wire for fast links."),
            [7745] = ("Radio Module", "A transceiver: oscillator, heat sink and casings."),
            [7746] = ("Battery Cell", "An accumulator cell: alloy sheets in a casing."),
            [7747] = ("Cooling System", "Heat sinks, rubber seals and ice in a closed loop."),
            [7748] = ("Engine Block", "An electric motor mounted on panels, with its belts."),
            [7749] = ("Adaptive Controller", "A control computer: harnesses, framework and a computer."),
            [7750] = ("Supercomputer", "A cluster of computers linked by fast connectors."),
            [7751] = ("Turbo Motor", "An engine block, cooled and radio-controlled: the ultimate power."),
            [7752] = ("Quantum Processor", "Computing on condensed stellar matter, straight out of the accelerator."),
            [7753] = ("Condensed Stellar Matter", "Stellar dust squeezed into a glowing particle in the accelerator."),
        };

        public static readonly Dictionary<ushort, (string Name, string Description)> Machines = new Dictionary<ushort, (string, string)>
        {
            [7601] = ("Industrial Workshop", "Shapes metal: iron plates and rods, copper wire, steel beams and pipes. It also takes raw ore, just less efficiently. Plug the socket on its back into the power grid and it works as long as it has power, materials and room."),
            [7602] = ("Smelter", "Melts ore into ingots (iron, copper, aluminum, steel) and stellar dust into glass. Crushed ore gives more ingots."),
            [7603] = ("Press", "Presses and cuts: screws, cables, copper and aluminum sheets, concrete, biomass bricks."),
            [7604] = ("Crusher", "Crushes iron and copper ore (the smelter then gets more ingots out of it), grinds stellar shards into dust, wood into biomass and glass into silica."),
            [7605] = ("Assembler", "Puts several parts together into components: reinforced plates, rotors, stators, frames, circuits..."),
            [7606] = ("Extractor", "Drills the nearest minable rock (within 6 m) and pulls its ore out without ever using it up: one item every 6 s. Set it on a ship or a base on the ground, next to a rock, and give it power."),
            [7607] = ("Combustion Generator", "Burns logs, biomass, bricks or charcoal and powers its grid with up to 6,000 W. It only burns what the grid actually uses."),
            [7608] = ("Industrial Container", "A reinforced crate with 10 slots: a buffer between machines, or the hold of a cargo ship."),
            [7609] = ("Transfer Dock", "Moves items to another dock, for example from a cargo ship to a base. Look at it and press N: off, send or receive. A sending dock empties into the nearest receiving dock within 4 m, even on another ship."),
            [7610] = ("Fabricator", "A robotic cell with two arms: motors, computers, heavy frames and other parts with up to 4 ingredients."),
            [7611] = ("Blast Furnace", "A two-input furnace: steel with charcoal (better yield), aluminum-copper alloy, quartz crystals."),
            [7612] = ("Refinery", "Biomass chemistry: charcoal, resin, plastic and rubber (with by-products)."),
            [7613] = ("Accumulator", "Stores 60 MJ: it charges from its grid's spare power and gives it back when the grid runs short (up to 3 kW). The charge is kept in the save."),
            [7614] = ("Particle Accelerator", "An acceleration ring: condenses stellar dust into matter and makes quantum processors. A big consumer (8 kW)."),
        };

        /// <summary>Recipes whose English name is not simply their first product's.</summary>
        public static readonly Dictionary<string, string> Recipes = new Dictionary<string, string>
        {
            ["workshop.iron_plate_raw"] = "Iron Plate (raw ore)",
            ["workshop.copper_wire_raw"] = "Copper Wire (raw ore)",
            ["smelter.crushed_iron"] = "Iron Ingot (crushed ore)",
            ["smelter.crushed_copper"] = "Copper Ingot (crushed ore)",
            ["foundry.steel"] = "Steel Ingot (charcoal)",
            ["smelter.glass"] = "Glass",
        };

        public static readonly Dictionary<ushort, string> Techs = new Dictionary<ushort, string>
        {
            [7801] = "Basic Industry",
            [7802] = "Processing",
            [7803] = "Assembly",
            [7804] = "Industrial Power",
            [7805] = "Industrial Logistics",
            [7806] = "Advanced Fabrication",
            [7807] = "Metallurgy and Chemistry",
            [7808] = "Energy Storage",
            [7809] = "Particle Physics",
        };
    }
}
