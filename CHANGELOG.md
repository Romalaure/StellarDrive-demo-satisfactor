# Journal des changements

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
