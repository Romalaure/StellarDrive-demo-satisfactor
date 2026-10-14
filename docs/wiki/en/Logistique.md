# Logistics

## Conveyors
Conveyors (the "Convoyeurs" build tab, *Conveyors* research) carry items from one part to the one touching their end, with no power:

- set a [Conveyor Belt Mk1](Machine-7623) behind a machine or a chest, its chevrons pointing where the items should go, then more belts end to end up to a machine, a chest or a [container](Machine-7608);
- the first belt **takes** from the part behind it (a machine only gives its products, a chest gives anything); every belt **puts** into the part in front of it (a machine only takes its recipes' ingredients);
- rate: Mk1 1 item/s, [Mk2](Machine-7624) 2.5/s, [Mk3](Machine-7625) 6/s; a belt carries a few items at most, and a full line stops like a real belt;
- the [Splitter](Machine-7627) hands items out in turn to the front, left and right; the [Merger](Machine-7628) gathers the back, left and right into the front;
- the [Bucket Lift](Machine-7626) climbs two meters: it takes from behind its foot and drops in front of its top.

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
The deposit scanner is an item: keep it in your inventory and press **K**.

| Item | Where | What it does |
|---|---|---|
| [Scanner Mk1](Objet-7760) | workshop, *Prospecting* research | 80 m, each ore, the nearest with its distance and an arrow |
| [Scanner Mk2](Objet-7761) | assembler, *Advanced Prospecting* | 160 m, radar screen, deposit purity |
| [Scanner Mk3](Objet-7762) | fabricator, *Geological Analysis* | 300 m, markers on the deposits in your view for 30 s |
| [Range Module](Objet-7763) | assembler | +25% range (two at most) |
| [Analysis Module](Objet-7764) | fabricator | purity and extraction yield on any scanner |

Within 60 m of a [Radar Tower](Machine-7622), the scanner reaches 400 m.

**Purity**: every rock is *impure* (an extractor works at half speed there), *normal* or *pure* (double speed). A rich deposit shows: its ore veins and nodules are more numerous and bigger.

## Storage
The [Industrial Container](Machine-7608) has 10 slots, like every machine of the mod.
