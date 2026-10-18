# Logistics

## Conveyors
Conveyors (the "Convoyeurs" build tab, *Conveyors* research) carry items from one part to the one touching their end, with no power:

- set a [Conveyor Belt Mk1](Machine-7623) behind a machine or a chest, its chevrons pointing where the items should go, then more belts end to end up to a machine, a chest or a [container](Machine-7608);
- the first belt **takes** from the part behind it (a machine only gives its products, a chest gives anything); every belt **puts** into the part in front of it (a machine only takes its recipes' ingredients);
- rate: Mk1 1 item/s, [Mk2](Machine-7624) 2.5/s, [Mk3](Machine-7625) 6/s; a belt carries a few items at most, and a full line stops like a real belt;
- the [Splitter](Machine-7627) hands items out in turn to the front, left and right; the [Merger](Machine-7628) gathers the back, left and right into the front;
- the [Bucket Lift](Machine-7626) climbs three meters: it takes from behind its foot and drops in front of its top.

Look at a conveyor to see what is linked to its input and output. Conveyors stay inside one ship or base.

## Linking machines (Item Lines mod)
With the *Item Lines* mod, each machine gets two item ports on its back, in the small yellow hatches left of the power socket. Connect them with item cables and let them work:

- a machine only sends out what it made, never the ingredients of the recipe it is running;
- it only takes what it needs (or what the recipe chosen with N needs), a few cycles ahead, and always keeps two slots free for its products;
- a generator gets fuel delivered (and the reactor sends its waste back out), an extractor or a [condenser](Machine-7616) sends out everything it makes;
- the [Recycler](Machine-7620) takes almost anything and only sends out recycling vouchers;
- [containers](Machine-7608) and [docks](Machine-7609) take and give anything: great as buffers.

## From one ship to another
Item lines stay inside one ship. To unload a cargo ship into your base, use two [transfer docks](Machine-7609):
1. place one dock on each side, less than 4 m apart;
2. look at the first one and press **N** until it says "send", then the second one until it says "receive";
3. the sending dock empties into the other one, 4 items per second.

## Recycling and trading
Drowning in spare screws or plates? Send them to the [Recycler](Machine-7620): it destroys them and pays you in [recycling vouchers](Objet-7756). The more work an item took, the more it is worth (100 points make a voucher; an ore is worth 1 point). Then trade the vouchers at the [Exchange Counter](Machine-7621) for ore, stellar shards or stellar matter: pick what you want with **N**.

## Finding deposits
The deposit scanner is an item: keep it in your inventory and press **N** while not aiming at a machine (or **K**), or hold it from the toolbar and **click**.

| Item | Where | What it does |
|---|---|---|
| [Scanner Mk1](Objet-7760) | workshop, *Prospecting* research | 80 m, each ore, the nearest with its distance and an arrow |
| [Scanner Mk2](Objet-7761) | assembler, *Advanced Prospecting* | 160 m, radar screen, deposit purity |
| [Scanner Mk3](Objet-7762) | fabricator, *Geological Analysis* | 300 m, markers on the deposits in your view for 30 s |
| [Range Module](Objet-7763) | assembler | +25% range |
| [Range Module II](Objet-7765) | fabricator | +50% range |
| [Range Module III](Objet-7766) | accelerator, *Quantum Prospecting* | doubles the range |
| [Analysis Module](Objet-7764) | fabricator | purity and extraction yield |
| [Analysis Module II](Objet-7767) | accelerator, *Quantum Prospecting* | purity, yield and markers in view |

**Fitting modules**: right-click with the scanner in hand (or **Shift+N**) opens its module bay. The Mk1 has 2 slots, the Mk2 3, the Mk3 4. A fitted module leaves your inventory; taking it out gives it back. Fitted modules follow your account: they work with any scanner you carry.

Within 60 m of a [Radar Tower](Machine-7622), the scanner reaches at least 400 m, plus its range modules (800 m at most).

**Purity**: every rock is *impure* (an extractor works at half speed there, the laser gets half as much), *normal* or *pure* (double speed and mining). A rich deposit shows: its ore veins and nodules are more numerous and bigger.

## New ores
About four metal rocks in ten hold one of the mod's ores instead of iron, copper or aluminum. Each planet has three of the five. They are mined with the laser, extracted and found with the scanner like the others.

| Ore | Colour on the rock | Used for |
|---|---|---|
| [Coal](Objet-7768) | black | generator fuel, steel in the blast furnace |
| [Sulfur](Objet-7769) | yellow, small crystals | rubber (refinery), battery cells |
| [Limestone](Objet-7770) | off-white | concrete (press) |
| [Raw Quartz](Objet-7771) | clear crystals | silica (crusher), quartz crystals (blast furnace) |
| [Gold Ore](Objet-7772) | golden | [gold ingots](Objet-7773) (smelter), then circuit boards |

## Inexhaustible deposits and extractors
About one rock in eight is an **inexhaustible deposit**: a crown of ore points and glowing purple shards, "∞" on the scanner. It is for extractors only: the mining laser does not wear it down at all. An extractor next to it works **2.5 times faster**.

| Extractor | Speed | Reach | Research |
|---|---|---|---|
| [Extractor](Machine-7606) | 1 item / 6 s | 6 m | *Basic Industry* |
| [Extractor Mk2](Machine-7632) | 1 item / 3 s | 8 m | *Advanced Extraction* |
| [Deposit Drill](Machine-7633) | 1 item / 1.5 s | 6 m | *Deep Drilling* |

Purity (×½, ×1, ×2) and an inexhaustible deposit (×2.5) add to these speeds.

## Storage
The [Industrial Container](Machine-7608) has 40 slots: scroll the grid (mouse wheel or the bar on its right) to reach the last ones. It is light (15 kg empty), to serve as a cargo ship's hold; what it holds weighs as usual.
