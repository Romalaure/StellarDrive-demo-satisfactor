# Ajouter du contenu

Toutes les valeurs de jeu sont dans trois tables C#. Après une modification : `dotnet test`
(le validateur et les tests tournent sans le jeu), puis compilation.

Plages d'ids réservées (`Core/Ids.cs`) : pièces 7601-7699, objets 7701-7799, techs 7801-7899.
**Un id publié ne doit jamais être réutilisé pour autre chose** : les sauvegardes le gardent.

## Ajouter une ressource

`src/StellarIndustries/Content/ResourceCatalog.cs` :

```csharp
public const uint SteelPlate = 7703;
...
new ResourceDef
{
    Id = SteelPlate, Name = "Plaque d'acier", Category = ResourceCategory.Intermediate, Mass = 2f, Tier = 1,
    Description = "...",
    MaterialFrom = GameItems.Iron,          // matériau (texture) du jeu porté par le modèle
    Tint = new[] { 0.7f, 0.72f, 0.78f },   // teinte multipliée
    Shape = ResourceShape.Plate              // Plate, Coil, Rod, Chunk (Content/IndustrialItems.cs)
}
```

Le validateur signale une ressource qu'aucune recette ne produit (avertissement) et toute
recette qui en a besoin sans source (erreur : chaîne bloquée).

## Ajouter une machine

`src/StellarIndustries/Production/MachineCatalog.cs` : une `MachineDef` (id de pièce, nom,
puissance, vitesse, masse, coût). La pièce est créée par `Parts/MachineParts.cs` (donneur
`StorageChest`, 10 emplacements, prise électrique 0 à l'arrière). Pour un modèle différent,
ajouter une méthode de construction comme `BuildWorkshopModel` et l'appeler selon l'id.

Règles : les prises d'une pièce doivent avoir des ids 0..n-1 sans trou ; la prise électrique
est l'id 0 (`MachineSimulation.PowerSocket`).

## Ajouter une recette

`src/StellarIndustries/Recipes/RecipeBook.cs` :

```csharp
new RecipeDef
{
    Id = "workshop.steel_plate", Name = "Plaque d'acier", Machine = MachineCatalog.Workshop, Seconds = 6f,
    Inputs = new[] { new ItemAmount(ResourceCatalog.IronPlate, 2), new ItemAmount(GameItems.Wood, 1) },
    Outputs = new[] { new ItemAmount(ResourceCatalog.SteelPlate, 1) }
}
```

- Une machine essaie ses recettes **dans l'ordre de la table** et garde la recette en cours
  tant qu'elle peut tourner.
- Plusieurs sorties = coproduits : le cycle n'a lieu que si **toutes** trouvent de la place.
- `Watts = 0` reprend la puissance de la machine.
- Le validateur refuse : objet ou machine inconnus, quantité ≤ 0, durée ≤ 0, aucune entrée,
  aucune sortie, objet à la fois entrée et sortie, recette qui ne tient pas dans 10 emplacements.
  Une recette en erreur est **désactivée** au chargement (journal MelonLoader : `content:`).
- Avertissements : boucles d'objets, sortie d'une recette consommée par une autre recette de la
  même machine.

## Réglages d'équilibrage (sans recompiler)

`UserData/MelonPreferences.cfg`, section `[StellarIndustries]` :

| Clé | Effet |
|---|---|
| `Enabled` | `false` : plus aucune production (pièces et contenus restent) |
| `ProductionSpeed` | multiplie la vitesse de toutes les machines (0,1 à 10) |
| `PowerFactor` | multiplie la consommation (0 à 10) |
| `RequirePower` | `false` : les machines tournent sans câble (tests) |
| `MachineHud`, `MachineHudDistance` | panneau d'état de la machine visée |
| `DevWorld`, `DevPhase`, `DevQuitSeconds` | test en jeu automatique (développement uniquement) |
