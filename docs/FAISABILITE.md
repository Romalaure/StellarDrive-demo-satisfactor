# Stellar Industries — Rapport de faisabilité (phase zéro)

Audit réalisé le 11/10/2026 sur StellarDrive Demo (build Steam du 08/10/2026), MelonLoader 0.7.3,
SDModKit installé. Sources : assemblies du jeu décompilés localement avec ilspycmd (jamais
commités), code des mods TF et TF Advanced du même auteur (mécanismes déjà éprouvés en jeu),
mods tiers installés (SDModKit, ItemLines), et **tests en jeu automatisés** de ce dépôt.

Légende : ✅ confirmé par le code du jeu **et** utilisé en jeu · 🟡 confirmé dans le code, à
prototyper · ❌ indisponible proprement.

## Environnement technique

| Élément | Constat |
|---|---|
| Moteur | Unity, Mono, URP. Assemblies `StellarDrive_Data/Managed` (Ships, Items, Saving, Research…) |
| Chargeur | MelonLoader 0.7.3 (net35 pour le chargeur, mods compilés en **net472**) |
| Patchs | HarmonyX (fourni par MelonLoader). Leçons connues : ne jamais patcher une méthode d'instance d'un `struct`, ne pas utiliser `__args` sur des méthodes à paramètres `out`/`ref` |
| Pièces | SDModKit (`CustomParts.Register`) clone une pièce du jeu (« donneur ») sous un nouvel id, l'ajoute au menu de construction, et le jeu la sauvegarde comme les siennes |
| Réseau | FishNet. Le serveur (hôte) simule les vaisseaux ; les états des pièces sont répliqués par le jeu |
| Sauvegarde | `world.json` (format V15). Les états de pièces ne sont sauvés que pour une **liste fermée de types** (`ShipsConverter`) |

## A. Capacités confirmées

| Capacité | Mécanisme réel | Preuve |
|---|---|---|
| Charger le mod sans casser le jeu | `MelonMod`, patchs appliqués classe par classe (un patch cassé n'arrête pas le reste) | ✅ log « 2 patch(es) applied » |
| Nouvelles ressources (objets) | `ItemSettings` ajoutés à `ItemSettingsList` (préfixe sur `GetItemSettingsById`), modèle 3D construit par le mod | ✅ objets 7701/7702 produits, stockés, sauvegardés |
| Nouvelle pièce constructible | SDModKit, donneur `StorageChest`, coût de construction, déverrouillage | ✅ pièce 7601 posée, coût lu (10 fer, 4 cuivre, 2 verre) |
| Inventaire de machine | `StorageState` du jeu : 10 emplacements, ouverts avec la touche « utiliser », **sauvegardés et répliqués par le jeu** | ✅ |
| Machine qui transforme réellement | Simulation hôte dans le tick électrique du jeu (`PowerServerTracker.UpdatePowerTransfers`) | ✅ 9 fer + 3 cuivre → 4 plaques + 6 fils |
| Consommation électrique réelle | `PowerGroupServerState.TotalPowerNeeded` / `NormalizedConsumerRate` ; prise greffée (`ContentBuilder.GraftSocket`) | ✅ le générateur à bois brûle plus de bois quand l'atelier produit, rien à l'arrêt |
| Recettes transactionnelles | Entrées et sorties appliquées en une seule mise à jour d'état | ✅ machine sans câble : rien consommé ; machine pleine : rien perdu |
| Persistance | Contenu des emplacements via `StorageState` | ✅ sauvegarde → fermeture → rechargement → contenu identique → production relancée |
| Fluides (pompage, ajout) | `FluidsServerTracker` / `FluidGroupServerState.AddFluid` | ✅ (condenseur de TF Advanced) |
| Recherche | `ResearchSettings` ajoutés à `ResearchSettingsList`, coûts en objets, pièces débloquées | ✅ (TF Advanced, 11 techs) |
| Gisements minables | Clones des rochers du jeu (`MinablePlanetObject`) avec un autre objet en drop | ✅ (TF Advanced, planètes du mod) |
| Signaux | Une pièce à `ConstantState` émet sa valeur sur sa prise signal | ✅ (TF Advanced) |
| Transport d'objets par câble | Type de câble 3 « Item Line » fourni par SDModKit ; le mod tiers **ItemLines** (Krunk) l'utilise et ajoute un port d'objets à toute pièce à inventaire — y compris nos machines | ✅ constaté sur nos ateliers en jeu |

## B. Capacités incertaines (prototype nécessaire)

| Capacité | Piste | Risque |
|---|---|---|
| Choix de recette persistant | Fichier compagnon à côté de la sauvegarde (comme `tfa_sites.txt` de TF Advanced), clé navire+pièce | Ids de vaisseau stables ? à vérifier après fusion/séparation de vaisseaux |
| État de la machine chez les clients | Le client ne simule pas : il voit les emplacements, pas l'énergie ni la progression. Option : message réseau du mod, ou pièce « capteur » à `ConstantState` | Synchronisation à écrire et tester à deux joueurs |
| Convoyeurs | (1) s'appuyer sur ItemLines (dépendance tierce, compatible aujourd'hui) ; (2) notre propre logique sur le câble « Item Line » de SDModKit. Mode **logique par lots**, pas un GameObject par objet | Sauvegarde des câbles de type 3 à vérifier ; règles d'entrée/sortie des machines à définir |
| Extracteurs sur gisement | Machine posée près d'un gisement minable : détecter le rocher (requête monde) et produire son objet à débit fixe | Les rochers sont des objets du monde planétaire, pas des pièces ; épuisement = santé du rocher ? |
| Scanner minéralogique | Requêtes sur les points chauds planétaires (`PlanetHotspotsEnvironmentPhysics`) | Accès aux positions non chargées |
| Signaux des machines | Une machine `StorageState` ne peut pas émettre (le moteur de signaux lit des types d'état précis). Contournement : pièce capteur `ConstantState` adjacente | Moyen |
| Machines en vol | La simulation tourne sur tout vaisseau chargé par l'hôte, posé ou en vol | Masse : le contenu des emplacements compte déjà dans la masse du vaisseau |
| Écran d'information de production | Données disponibles côté hôte uniquement | Multijoueur |

## C. Capacités indisponibles (proprement)

| Capacité | Raison | Repli proposé |
|---|---|---|
| Nouveaux types d'état de pièce | `PartStateFactory` ne résout que les types de l'assembly `Ships.Interface` ; la sauvegarde et le réseau ne connaissent qu'une liste fermée | Réutiliser les états existants (`StorageState`, `ConstantState`, `FluidTankState`) + fichier compagnon pour le reste |
| Plus de 10 emplacements par machine | `StorageState` a 10 emplacements fixes | Machines multi-blocs ou conteneurs reliés |
| Progression de cycle sauvegardée | Pas de champ libre dans `StorageState` | Le cycle reprend à 0 après rechargement — **rien n'est perdu** (rien n'est pris avant la fin d'un cycle) |
| Réécrire la génération planétaire | Non extensible proprement | Gisements ajoutés sur des emplacements choisis (déjà fait par TF Advanced sur ses planètes) |
| Tension/intensité électrique réalistes | Le jeu ne simule qu'une puissance abstraite (W) | Puissance + priorités + batteries |
| Véhicules terrestres autonomes, trajets automatiques de vaisseaux | Pas de navigation automatique fiable | Transferts par arrimage/conteneurs, pas de fausse automatisation |
| Sons originaux via Unity | L'audio Unity est désactivé ; tout passe par FMOD | Sons via FMOD (WAV/OGG) en phase H |
| Fluides industriels (machines qui pompent, mélangent, remplissent des réservoirs) | Les fluides du jeu sont l'oxygène (108) et l'éthanol (111) ; un emplacement de stockage n'accepte pas un objet fluide et nos machines (état `StorageState`) n'ont pas de logique de réservoir ; un nouvel état fluide est impossible (voir ligne 1) | Non fait (0.5.0). Pistes : condenseur de TF Advanced (prise fluide sur `ConstantState`) |
| Lumières dynamiques des machines | Les shaders du jeu n'éclairent qu'avec le soleil | Matériaux émissifs |

## D. Architecture retenue

Un seul assembly `StellarIndustries.dll`, dossiers par responsabilité :

```
src/StellarIndustries/
  Core/         initialisation, réglages, plages d'ids, journal d'erreurs, MeshBuilder, messages réseau (SINet)
  Content/      catalogue de ressources (données pures) + création des ItemSettings
  Recipes/      livre de recettes (données pures) + validateur
  Production/   catalogue de machines, craft transactionnel, choix de recette, combustible (purs),
                simulation hôte branchée sur le tick électrique du jeu, gisements, fichier compagnon
  Logistics/    règles des convoyeurs, transferts (purs), pont Item Lines, quais de transfert
  Parts/        pièces SDModKit (donneur, modèles, prise), apparence du jeu, branche de recherche
  UI/           panneau de la machine visée, scanner de gisements, bruit des machines
  Diagnostics/  tableau de bord industriel, test d'acceptation et de charge en jeu (désactivé par défaut)
tests/          tests unitaires de la logique pure (sans le jeu)
```

Principes :

- **Données séparées du comportement** : ressources, machines et recettes sont des tables ; le
  validateur les contrôle au chargement et désactive toute recette en erreur.
- **Une seule autorité** : seul l'hôte simule ; les clients reçoivent les états par le jeu.
- **Pas de nouveau format de sauvegarde** quand le jeu en fournit un (`StorageState`).
- **Transactions** : un cycle ne modifie l'inventaire qu'en une seule écriture, entrées et
  sorties ensemble.
- **Désactivable** : réglage `Enabled = false` arrête la production ; retirer la DLL laisse les
  objets du mod dans les coffres (le jeu ignore les ids de pièces inconnus au chargement).
- **Coût** : machines inactives = une lecture de 10 emplacements par tick, aucune allocation
  dans la boucle chaude hors écriture d'un cycle terminé.

## E. Prototype minimal (phase A) — réalisé

| Objectif | Résultat |
|---|---|
| 1. Chargement du mod | ✅ |
| 2. Nouvelle pièce constructible | ✅ Atelier industriel (7601), onglet « Industrie » — placement via le menu de construction **non testé à la main** |
| 3. Nouvelle ressource | ✅ Plaque de fer (7701), Fil de cuivre (7702) |
| 4. Recette | ✅ 2 fer → 1 plaque (4 s) ; 1 cuivre → 2 fils (3 s) ; 300 W |
| 5. Transformation réelle | ✅ test en jeu |
| 6. Persistance | ✅ test en jeu (sauvegarde du jeu, rechargement, contenu identique, reprise) |

## F. Stratégie de livraison

| Version | Contenu | État |
|---|---|---|
| 0.1 (A) | Atelier, 2 objets, 2 recettes, énergie, persistance, panneau d'état | ✅ tests en jeu automatiques |
| 0.2 (C) | Fonderie, presse, broyeur, assembleuse ; chaînes fer/cuivre/aluminium ; choix de recette sauvegardé (fichier compagnon) | ✅ testé en jeu ; panneau vérifié par capture ; menu de construction non vérifié à la main |
| 0.3 (B) | Extracteur posé près d'un gisement, scanner simple | ✅ testé en jeu (rocher de test ; vrais rochers détectés par le scanner) |
| 0.4 (D) | Convoyeurs : **décision** intégration Item Lines (facultatif) + règles d'entrée/sortie | ✅ testé en jeu |
| 0.5 (E) | Générateur à combustion, briquettes ; batteries : celles de TF Advanced ; fluides : non disponibles | ✅ générateur testé en jeu |
| 0.6 (F) | Branche de recherche « Industrie » (5 techs) | ✅ déblocage testé en jeu |
| 0.7 (G) | Quai de transfert entre vaisseaux, conteneur ; pas de vaisseaux automatiques | ✅ testé en jeu (deux vaisseaux) |
| 0.8 (H, I) | Modèles, son FMOD synthétisé, tableau de bord, test de charge, messages réseau | ✅ testé en jeu (solo) ; ❌ multijoueur non testé à deux |

## Obstacles connus

1. **Multijoueur non validé** : la simulation est mono-autorité (hôte) et les inventaires sont
   répliqués par le jeu, ce qui est le bon modèle, mais aucun test à deux joueurs n'a été fait.
   Tous les joueurs doivent avoir le mod (et SDModKit).
2. **Progression de cycle non sauvegardée** (voir C).
3. **ItemLines** déplace des objets vers/depuis nos machines si un joueur les relie : compatible
   (la machine lit tout son inventaire), mais l'ordre « entrées en haut, sorties en bas » n'est
   pas imposé à ItemLines.
