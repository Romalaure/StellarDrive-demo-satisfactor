# Journal des changements

## 0.7.0 — phase G (transferts entre vaisseaux)

- **Quai de transfert** (7609) : touche `N` en le visant → arrêt / envoi / réception. Un quai en
  envoi vide son contenu (4 objets/s) dans le quai en réception le plus proche à moins de 4 m,
  **même sur un autre vaisseau** (les lignes d'Item Lines restent dans un vaisseau). Les deux
  inventaires sont écrits dans le même tick ; le mode est gardé dans `stellar-industries.txt`.
- **Conteneur industriel** (7608) : 10 emplacements, tampon pour les lignes ou soute de cargo.
- Tech *Logistique industrielle* (7805, après *Assemblage*).
- Pas de vaisseaux automatiques : le jeu n'offre pas de pilotage automatique fiable ; le quai
  sert aux cargos pilotés par les joueurs.

## 0.6.0 — phase F (recherche)

- Branche **Industrie** à la table de recherche (techs 7801-7804), après *Power Generation* :
  - *Industrie de base* (20 fer, 10 cuivre) : atelier, fonderie, extracteur ;
  - *Transformation* (20 plaques, 10 tiges, 10 fils) : presse, broyeur ;
  - *Assemblage* (40 vis, 10 câbles, 10 plaques) : assembleuse ;
  - *Énergie industrielle* (10 briquettes, 6 câbles, 10 plaques) : générateur à combustion.
- Les coûts des niveaux supérieurs sont payés en produits du mod : on remonte la chaîne.
- Placement automatique dans l'arbre partagé avec les autres mods (cases libres à droite des
  prérequis, au plus près). Un id de tech déjà pris est signalé et la tech n'est pas ajoutée.
- Réglage `ResearchUnlocks` (activé) ; désactivé, toutes les machines sont disponibles.
- **Changement** : sur une partie existante, les machines doivent maintenant être recherchées
  pour être construites (celles déjà posées restent et fonctionnent).

## 0.5.0 — phase E (énergie)

- **Générateur à combustion** (7607, jusqu'à 6 000 W) : brûle bois (40 kJ, comme le générateur
  du jeu) ou **briquettes de biomasse** (7714, 200 kJ ; presse : 4 bois → 1 briquette). Il
  s'ajoute à la capacité de son réseau dans le tick électrique du jeu et ne brûle que ce que le
  réseau a réellement pris. Un objet ne quitte son emplacement qu'une fois toute son énergie
  donnée : un rechargement ne fait jamais perdre de combustible.
- États : « En veille » (le réseau ne demande rien), « Plus de combustible ».
- Convoyeurs : un générateur reçoit du combustible (20 d'avance) et n'envoie rien.
- **Batteries** : non ajoutées ici, celles de *TF Advanced* (même tick électrique, ids
  disjoints) fonctionnent avec les machines du mod.
- **Fluides industriels** : non disponibles (voir FAISABILITE.md, section C).

## 0.4.0 — phase D (convoyeurs)

- **Décision** : les convoyeurs passent par le mod **Item Lines** (Krunk, facultatif), qui
  donne un port d'objets à toute pièce à inventaire et déplace les objets le long de câbles
  d'objets SDModKit. Pas de second système de convoyeurs concurrent sur le même type de câble.
- Règles des machines sur les lignes (`Logistics/ConveyorRules.cs`) : une machine n'envoie que
  ses produits (jamais un ingrédient de sa recette en cours ou choisie ; l'extracteur envoie
  tout) ; elle ne reçoit que les ingrédients de ses recettes (de la recette choisie si elle est
  réglée), jusqu'à 4 cycles d'avance, et un nouvel objet seulement s'il reste 2 emplacements
  libres pour les produits. L'extracteur ne reçoit rien.
- Les machines reçoivent par défaut depuis une ligne (Item Lines ne le fait d'office que pour
  les machines du jeu) ; le réglage « Receives » du port, s'il est changé, est respecté.
- Sans Item Lines : rien ne change, les machines se remplissent à la main.

## 0.3.0 — phase B (extraction)

- **Extracteur** (7606, 700 W) : produit 1 objet toutes les 6 s du rocher minable le plus
  proche à moins de 6 m (fer, cuivre, aluminium, cristaux de verre, glace, minerais d'autres
  mods...). Le rocher n'est pas épuisé ; s'il est miné par un joueur, l'extracteur s'arrête
  (« Aucun gisement à portée »). Les arbres et les outils posés au sol ne comptent pas.
- **Scanner de gisements** : touche `K`, liste pendant 8 s les rochers par type autour du
  joueur (nombre, distance, direction). Réglages `ScanKey`, `ScanRadius` (80 m).
- Le panneau de l'extracteur indique le gisement et sa distance.

## 0.2.0 — phase C (transformation)

- Machines : **Fonderie** (7602, 400 W), **Presse** (7603, 600 W), **Broyeur** (7604, 350 W),
  **Assembleuse** (7605, 800 W), chacune avec son modèle.
- Objets : lingots de fer/cuivre/aluminium, tige de fer, vis, câble, tôle d'aluminium, plaque
  renforcée, fer broyé, cuivre broyé, poussière stellaire (7703-7713).
- 18 recettes. Chaîne minerai → lingot → plaque/tige/fil → vis/câble → plaque renforcée ;
  broyer le minerai donne 4/3 de lingots ; coproduit verre au broyeur.
- L'atelier préfère les lingots ; le minerai brut reste accepté (moins rentable). **Changement** :
  1 cuivre brut → 1 fil (au lieu de 2) ; 2 fils par lingot de cuivre.
- **Choix de recette** : touche `N` en visant une machine (Maj+N : précédente), mode
  automatique ou recette unique. Sauvegardé dans `stellar-industries.txt` à côté de la
  sauvegarde du monde (écriture atomique ; machines disparues oubliées).
- Multijoueur : messages réseau propres au mod (FishNet broadcast) ; un client demande
  l'état de la machine visée à l'hôte (énergie, progression visibles chez les clients) et
  envoie son choix de recette à l'hôte.
- Réglage `RecipeKey`.
- Tests : 34 unitaires ; test en jeu étendu (5 machines, choix conservé après rechargement,
  capture du panneau).

## 0.1.0 — phase A (prototype de faisabilité)

- Audit technique et rapport de faisabilité (`docs/FAISABILITE.md`).
- Atelier industriel (pièce 7601, onglet « Industrie ») : clone du coffre de stockage du jeu
  (10 emplacements), modèle propre, prise électrique à l'arrière, 300 W.
- Objets Plaque de fer (7701) et Fil de cuivre (7702).
- Recettes : 2 fer → 1 plaque de fer (4 s), 1 cuivre → 2 fils de cuivre (3 s).
- Simulation sur l'hôte dans le tick électrique du jeu, cycles transactionnels.
- Validateur de contenu au chargement (recettes en erreur désactivées).
- Panneau d'état de la machine visée.
- Réglages : `Enabled`, `ProductionSpeed`, `PowerFactor`, `RequirePower`, `MachineHud`.
- Tests unitaires (21) et test d'acceptation en jeu automatisé (production, énergie,
  sortie bloquée, sauvegarde/rechargement).
