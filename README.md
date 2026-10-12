# Stellar Industries — StellarDrive Demo

Extension industrielle pour StellarDrive Demo, inspirée des jeux d'automatisation : extraire,
transformer, fabriquer, alimenter, transporter. Développée par étapes ; chaque version ne
déclare opérationnel que ce qui a été vérifié dans le jeu.

**Version 0.8.0 — phases A à I.** Voir le [rapport de faisabilité](docs/FAISABILITE.md) pour ce
que le jeu permet réellement, et la [feuille de route](docs/FAISABILITE.md#f-stratégie-de-livraison).

![Ateliers industriels et générateur à bois](Mods/thumbnail.png)

## Ce qui fonctionne

Onglet de construction **« Industrie »**, débloqué par la branche de recherche **Industrie**.

| Fonction | État |
|---|---|
| **Atelier, Fonderie, Presse, Broyeur, Assembleuse** : minerai → lingots → plaques, tiges, fil → vis, câbles, tôles → plaques renforcées ; le broyeur donne +33 % de lingots | ✅ testé en jeu |
| Ne produit que s'il a de l'énergie, de la matière **et** de la place ; rien n'est consommé sans être produit | ✅ testé en jeu |
| Énergie : consomme sur le réseau électrique du jeu, ralentit si le réseau manque de puissance | ✅ testé en jeu (énergie insuffisante : non testé) |
| **Choix de recette** (touche `N` en visant la machine), gardé dans la sauvegarde | ✅ testé en jeu (touche : non testée à la main) |
| **Extracteur** : à moins de 6 m d'un rocher minable, en tire son minerai (1 / 6 s) sans l'épuiser | ✅ testé en jeu (avec un rocher de test) |
| **Scanner de gisements** (touche `K`) | ✅ testé en jeu (vrais rochers détectés) |
| **Convoyeurs** avec le mod *Item Lines* (facultatif) : les machines n'envoient que leurs produits et ne reçoivent que leurs ingrédients | ✅ testé en jeu |
| **Générateur à combustion** (6 kW) : bois ou briquettes de biomasse, ne brûle que ce que le réseau consomme | ✅ testé en jeu |
| **Quai de transfert** : d'un vaisseau à l'autre (cargo ↔ base), 4 objets/s, moins de 4 m | ✅ testé en jeu (entre deux vaisseaux) |
| **Conteneur industriel** (10 emplacements) | ✅ testé en jeu |
| **Recherche « Industrie »** (5 techs), coûts en produits du mod | ✅ déblocage testé (arbre non vérifié à l'écran) |
| Panneau d'état en visant une machine | ✅ affichage vérifié par capture |
| **Tableau de bord industriel** (`F7`) : machines par état, énergie, problèmes, coût de la simulation | ✅ testé en jeu |
| Bruit des machines (son original synthétisé, FMOD) | ✅ joue en jeu (écoute non vérifiée) |
| Charge : 200 machines en production | ✅ 0,24 ms par tick de simulation |
| Multijoueur | ⚠️ conçu pour (l'hôte simule, messages réseau du mod), **non testé à deux joueurs** |

## Démarrer

1. Recherchez *Industrie de base* (après *Power Generation*) à la table de recherche.
2. Posez une **Fonderie** et un **Atelier**, reliez leur prise arrière à un générateur.
3. Mettez du minerai de fer dans la fonderie : elle fait des lingots ; mettez les lingots dans
   l'atelier : il fait des plaques (ou des tiges, avec `N`).
4. Un **Extracteur** à côté d'un rocher (touche `K` pour en trouver) fournit le minerai, et
   *Item Lines* relie les machines entre elles.

Chaîne : minerai → **Fonderie** → lingots → **Atelier** → plaques, tiges, fil → **Presse** →
vis, câbles, briquettes → **Assembleuse** → plaques renforcées. Le **Broyeur** double le
minerai avant la fonderie.

## Limitations connues

- Le cycle en cours repart de zéro après un rechargement (aucun objet perdu).
- 10 emplacements par machine (limite du stockage du jeu).
- L'extracteur ne voit que les rochers chargés par le jeu de l'hôte (près d'un joueur).
- Pas de batterie propre (utilisez celles de TF Advanced), pas de fluides industriels, pas de
  vaisseaux automatiques (voir le rapport de faisabilité).
- Chez un client, l'état des machines vient de l'hôte (non testé à deux joueurs).

## Installation

1. Installer **SDModKit** (gestionnaire de mods StellarModManager).
2. Installer **Stellar Industries** depuis le gestionnaire (ou copier `StellarIndustries.dll`
   dans `StellarDrive Demo/Mods/`). *Item Lines* est conseillé pour les convoyeurs.
3. Tous les joueurs d'une partie doivent avoir le mod.

## Désinstallation

Avant de retirer la DLL : faites une copie de votre sauvegarde, videz les machines et coffres
des objets du mod, recyclez les machines, sauvegardez. Sinon les machines disparaissent avec
leur contenu au chargement suivant. Voir [compatibilité](docs/COMPATIBILITE.md).
Pour simplement arrêter la production : `Enabled = false` dans la section
`[StellarIndustries]` de `UserData/MelonPreferences.cfg`.

## Compilation

Prérequis : .NET SDK 8, le jeu installé avec MelonLoader 0.7.3 et SDModKit. Le chemin du jeu est
dans `Directory.Build.props` (`GamePath`, surchargeable par `Directory.Build.props.user` ou
`-p:GamePath=...`). Aucune DLL du jeu n'est commitée.

```
dotnet build src/StellarIndustries/StellarIndustries.csproj -c Release   # copie aussi la DLL dans Mods/
dotnet test tests/StellarIndustries.Tests                                # tests sans le jeu
powershell tools/package.ps1                                             # zip pour une release
```

## Documentation

- [Rapport de faisabilité et architecture](docs/FAISABILITE.md)
- [Ajouter une ressource, une machine, une recette, une tech ; réglages](docs/GUIDE-CONTENU.md)
- [Compatibilité sauvegardes, multijoueur, autres mods](docs/COMPATIBILITE.md)
- [Tests](docs/TESTS.md)
- [Journal des changements](CHANGELOG.md)

Mécaniques inspirées des jeux d'automatisation ; aucun modèle, texture, son, nom ou code d'un
autre jeu n'est utilisé. Les modèles sont construits par le mod avec les matériaux de
StellarDrive ; le son des machines est synthétisé par le mod.
