# Tests

Trois niveaux, à ne pas confondre.

## 1. Tests unitaires (automatiques, sans le jeu)

```
dotnet test tests/StellarIndustries.Tests
```

Couvrent la logique pure (compilée telle quelle dans le projet de test) : craft
transactionnel, coproduits, piles pleines, choix de recette, validateur de contenu.

Dernier résultat (0.5.0) : **46 réussis, 0 échec** (dont choix de recette, fichier compagnon, chaînes de production, extraction, convoyeurs, combustible et conservation de l'énergie).

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

Depuis 0.2.0, le vaisseau de test porte aussi fonderie, broyeur, assembleuse et presse, chacun
câblé à son générateur ; la presse est réglée sur « câble uniquement » ; une capture d'écran du
panneau de la presse est écrite dans `UserData/StellarIndustries/dev/hud_press.png`.

Dernier résultat (0.2.0, 12/10/2026) : tout PASS —
fonderie 6 fer + 4 poussière → 6 lingots + 2 verre ; broyeur 2 éclats → 6 poussière + 2 verre
(coproduit) ; assembleuse 4 plaques + 8 vis → 1 plaque renforcée ; presse réglée sur câble :
4 fils → 2 câbles, les 3 tiges intactes ; atelier A 9 fer + 3 cuivre → 1 fer, 4 plaques, 3 fils ;
B sans câble et C plein inchangés ; contenu identique après rechargement ; **choix de recette
relu après rechargement** ; production relancée (atelier et presse). Panneau vérifié sur la
capture (état, recette, énergie 600/600 W, mode, contenu, touche).

0.6.0 (12/10/2026), recherche : PASS 4 techs ajoutées et placées (7801 en (5,2), 7802 (6,3),
7803 (7,3), 7804 (6,4), avec les techs de TF Advanced, Arsenal, Item Lines... dans l'arbre) ;
avant : atelier verrouillé ; tech 7801 marquée faite par l'état réseau du jeu
(`ResearchNetworkedState.ServerMarkResearchAsDone`) → atelier débloqué, presse toujours
verrouillée.

0.5.0 (12/10/2026), générateur à combustion (pièce 15, 2 bûches) câblé à l'atelier D : PASS
l'atelier a produit ses 4 plaques sur ce seul générateur ; 2 412 J tirés de la bûche en cours
(≈ 8 s × 300 W), aucune bûche encore entièrement brûlée, « En veille » une fois l'atelier arrêté.

0.4.0 (12/10/2026), convoyeur : une ligne d'objets Item Lines (câble type 3, port 1 greffé
par Item Lines) relie la fonderie à un atelier D réglé sur « plaques » : PASS fonderie 6 fer +
4 poussière → il lui reste 2 verres (ni fer ni verre envoyés), atelier D → 4 plaques (les 6
lingots sont passés par la ligne). Choix des deux machines relus après rechargement.

0.3.0 (12/10/2026), extracteur (pièce 14, câblé à son générateur) : PASS sans rocher à
portée (« aucun gisement », rien produit) ; un rocher de test (composant MinablePlanetObject du
jeu donnant du fer, créé par le test à 3 m) : PASS 5 fers extraits en ~30 s. Le scanner, lui,
a trouvé les **vrais** rochers du monde (« Iron : 6 rocher(s), le plus proche à 39 m ») ;
l'extraction depuis un vrai rocher n'a pas été testée (aucun à moins de 6 m du vaisseau de test).

Résultat 0.1.0 (11/10/2026) :

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
| Panneau d'état en visant l'atelier | affichage vérifié par capture en jeu (0.2.0) ; visée à la main non testée |
| Icônes et modèles 3D des objets dans l'inventaire | non testé |
| Recycler un atelier plein | non testé |
| Touche N pour changer de recette | non testé à la main (le choix lui-même est testé) |
| Poser un extracteur à côté d'un vrai rocher | non testé |
| Touche K du scanner | non testée à la main (le scan est testé) |
| Poser une ligne Item Lines à la main entre deux machines | non testé |
| Arbre de recherche à l'écran, payer une tech à la table | non testé |
| Deux joueurs (hôte + client) | non testé |
