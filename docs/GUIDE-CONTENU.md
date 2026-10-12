# Ajouter du contenu

Toutes les valeurs de jeu sont dans quelques tables C#. Après une modification : `dotnet test`
(le validateur et les tests tournent sans le jeu), puis compilation, puis le test en jeu
(`docs/TESTS.md`).

Plages d'ids réservées (`Core/Ids.cs`) : pièces 7601-7699, objets 7701-7799, techs 7801-7899.
**Un id publié ne doit jamais être réutilisé pour autre chose** : les sauvegardes le gardent.

Utilisés en 0.9.0 : pièces 7601-7610, objets 7701-7727, techs 7801-7806.

## Ajouter une ressource

`src/StellarIndustries/Content/ResourceCatalog.cs` :

```csharp
public const uint SteelPlate = 7715;
...
new ResourceDef
{
    Id = SteelPlate, Name = "Plaque d'acier", Category = ResourceCategory.Intermediate, Mass = 2f, Tier = 1,
    MaxStack = 100,                          // 1 à 200
    Description = "...",
    MaterialFrom = GameItems.Iron,           // matériau (texture) du jeu porté par le modèle
    Tint = new[] { 0.7f, 0.72f, 0.78f },    // teinte multipliée
    Shape = ResourceShape.Plate              // Plate, Coil, Rod, Chunk, Ingot, Screws, Powder, HeavyPlate, Brick
}
```

Le modèle 3D (et donc l'icône d'inventaire) est construit dans `Content/ItemModels.cs` :
ajouter un `case` pour le nouvel id, avec des boîtes et cylindres sur les tuiles de la planche
de textures (`Tile.Steel`, `Tile.Copper`, `Tile.Circuit`...). Sans `case`, l'objet prend une
forme simple selon `Shape`.
Le validateur signale une ressource qu'aucune recette ne produit (avertissement) et toute
recette qui en a besoin sans source (erreur : chaîne bloquée).

Un **combustible** : ajouter `(item, joules)` à `MachineCatalog.Fuels` (bûche du jeu : 40 kJ).

## Ajouter une machine

`src/StellarIndustries/Production/MachineCatalog.cs` : une `MachineDef`.

| Champ | Rôle |
|---|---|
| `PartId`, `Name`, `Description`, `Mass`, `BuildCost` | pièce de construction (onglet « Industrie ») |
| `Tier` | rangée du menu de construction (0 à 2) |
| `Watts`, `Speed` | consommation pendant la production ; durée des recettes divisée par `Speed` |
| `Model` | modèle construit par `Parts/MachineModels.cs` (ajouter une valeur et sa méthode) |
| `Width`, `Height`, `Depth` | boîte de placement (m) |
| `Extracts`, `ExtractSeconds`, `ExtractRadius` | extracteur : pas de recette, tire du rocher le plus proche |
| `Generates` | générateur : brûle `Fuels`, donne jusqu'à `Watts` au réseau |
| `Container`, `Dock`, `DockRange`, `DockRate` | stockage simple / quai de transfert |

La pièce est un clone du coffre du jeu (`StorageChest`, 10 emplacements, sauvegardés et
synchronisés par le jeu) avec le modèle du mod et une prise électrique 0 à l'arrière (sauf
conteneur et quai). Les prises d'une pièce doivent avoir des ids 0..n-1 sans trou.

Modèle : une méthode dans `Parts/MachineModels.cs` avec `ModelKit` (`Box`, `Cylinder`,
`Pipe`, `Frame`, `Bolts`, `Ladder`, chaque forme sur une tuile de `Parts/IndustrialSkin.cs`).
Pièces mobiles : `k.Moving("kind=spin axis=1,0,0 speed=200", pivot)` puis les formes de la
pièce ; types `spin`, `bob` (aller-retour), `press` (frappe rapide, retour lent), `swing`
(oscillation), `glow` (lumière qui s'allume) ; `k.Status(true/false)` pour ce qui n'apparaît
qu'en marche ou à l'arrêt. Les pièces bougent seulement pendant la production
(`Parts/MachineVisuals.cs`). Laisser libre l'arrière de la rangée de service (hauteur 0,4 m :
boîtier électrique à +0,2 m, trappes à 0 et -0,2 m).

Pour la rendre disponible par la recherche : l'ajouter aux `Parts` d'une tech de
`Parts/IndustryResearch.cs` (sinon elle est disponible d'office).

## Ajouter une recette

`src/StellarIndustries/Recipes/RecipeBook.cs` :

```csharp
R("press.steel_plate", "Plaque d'acier", MachineCatalog.Press, 6f, In(ResourceCatalog.IronPlate, 2), Out(ResourceCatalog.SteelPlate, 1), tier: 1),
// plusieurs entrées ou coproduits :
new RecipeDef
{
    Id = "assembler.frame", Name = "Cadre", Machine = MachineCatalog.Assembler, Seconds = 10f, Tier = 1,
    Inputs = new[] { new ItemAmount(ResourceCatalog.IronRod, 4), new ItemAmount(ResourceCatalog.Screws, 8) },
    Outputs = new[] { new ItemAmount(ResourceCatalog.Frame, 1), new ItemAmount(ResourceCatalog.Screws, 1) }
}
```

- En mode automatique, une machine essaie ses recettes **dans l'ordre de la table** et garde la
  recette en cours tant qu'elle peut tourner. Le joueur peut fixer une recette (touche `N`).
- Plusieurs sorties = coproduits : le cycle n'a lieu que si **toutes** trouvent de la place.
- Les convoyeurs n'apportent que les entrées des recettes et n'emportent que leurs sorties.
- `Watts = 0` reprend la puissance de la machine.
- Le validateur refuse : objet ou machine inconnus, quantité ≤ 0, durée ≤ 0, aucune entrée,
  aucune sortie, objet à la fois entrée et sortie, recette qui ne tient pas dans 10 emplacements.
  Une recette en erreur est **désactivée** au chargement (journal MelonLoader : `content:`).
- Avertissements : boucles d'objets, sortie d'une recette consommée par une autre recette de la
  même machine.

## Ajouter une tech

`src/StellarIndustries/Parts/IndustryResearch.cs` : un `Tech` (id 7801-7899, nom, prérequis,
case souhaitée, machines débloquées, coût). Le placement final est calculé dans les cases libres
de l'arbre partagé avec les autres mods.

## Réglages (sans recompiler)

`UserData/MelonPreferences.cfg`, section `[StellarIndustries]` :

| Clé | Effet |
|---|---|
| `Enabled` | `false` : plus aucune production (pièces et contenus restent) |
| `ProductionSpeed` | multiplie la vitesse de toutes les machines (0,1 à 10) |
| `PowerFactor` | multiplie la consommation (0 à 10) |
| `RequirePower` | `false` : les machines tournent sans câble (tests) |
| `ResearchUnlocks` | `false` : toutes les machines disponibles sans recherche |
| `MachineHud`, `MachineHudDistance` | panneau d'état de la machine visée |
| `RecipeKey` (N) | changer la recette / le mode d'un quai |
| `ScanKey` (K), `ScanRadius` (80) | scanner de gisements |
| `OverviewKey` (F7) | tableau de bord industriel |
| `MachineVolume` (0,5) | volume du bruit des machines |
| `DevWorld`, `DevPhase`, `DevQuitSeconds` | test en jeu automatique (développement uniquement) |
