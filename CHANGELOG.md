# Journal des changements

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
