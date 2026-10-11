# Tests

Trois niveaux, à ne pas confondre.

## 1. Tests unitaires (automatiques, sans le jeu)

```
dotnet test tests/StellarIndustries.Tests
```

Couvrent la logique pure (compilée telle quelle dans le projet de test) : craft
transactionnel, coproduits, piles pleines, choix de recette, validateur de contenu.

Dernier résultat (0.1.0) : **21 réussis, 0 échec**.

## 2. Test d'acceptation en jeu (automatique, jeu lancé)

`tools/devrun.sh` copie une sauvegarde dans `SI_DevTest`, active `DevWorld` dans
`MelonPreferences.cfg` (restauré à la fin), lance le jeu par Steam ; le mod charge le monde,
fait le test, quitte. Les lignes `dev:` du journal donnent le résultat.

```
PHASE=1 FRESH=1 WORLD=test4 tools/devrun.sh 75   # construire, produire, sauvegarder
PHASE=2 tools/devrun.sh 50                       # recharger et vérifier
```

Phase 1 : un vaisseau de test apparaît près du joueur — générateur à bois (20 bois) câblé à
l'atelier A (9 fer, 3 cuivre), atelier B sans câble (6 fer), atelier C plein (10 × 255 fer).
Phase 2 : production gelée jusqu'au contrôle du contenu rechargé, puis 4 fer ajoutés à A.

Dernier résultat (0.1.0, 11/10/2026) :

| Vérification | Résultat |
|---|---|
| A : 9 fer + 3 cuivre → 1 fer, 4 plaques, 6 fils | PASS |
| A consomme de l'énergie (bois du générateur 20 → 19,81), rien à l'arrêt | PASS (constaté dans le journal) |
| B sans câble : « Hors tension », rien consommé | PASS |
| C plein : « Sortie bloquée », rien perdu | PASS |
| Contenu identique après sauvegarde, fermeture, rechargement | PASS |
| Production relancée après rechargement (4 fer → 2 plaques) | PASS |
| Pièce débloquée, coût de construction lu | PASS (journal) |

## 3. Tests à la main (à faire)

| Vérification | État |
|---|---|
| Atelier visible dans l'onglet « Industrie » et posable avec l'outil de construction | non testé |
| Ouvrir l'atelier (touche utiliser), y déposer/retirer des objets | non testé |
| Panneau d'état en visant l'atelier | non testé |
| Icônes et modèles 3D des objets dans l'inventaire | non testé |
| Recycler un atelier plein | non testé |
| Deux joueurs (hôte + client) | non testé |
