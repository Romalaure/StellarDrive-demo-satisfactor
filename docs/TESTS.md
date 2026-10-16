# Tests

Trois niveaux, à ne pas confondre.

## 1. Tests unitaires (automatiques, sans le jeu)

```
dotnet test tests/StellarIndustries.Tests
```

Couvrent la logique pure (compilée telle quelle dans le projet de test) : craft
transactionnel, coproduits, piles pleines, choix de recette, validateur de contenu.

Dernier résultat (0.7.0) : **50 réussis, 0 échec** (dont choix de recette, fichier compagnon, chaînes de production, extraction, convoyeurs, combustible, transferts).

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

Depuis 0.13.0, le vaisseau a deux rangées de plus vers l'arrière (pour rester en équilibre sur sa
pente) : un coffre de 12 fers vidé par deux convoyeurs Mk1 dans un répartiteur qui remplit deux
conteneurs, et un générateur à bois qui alimente l'atelier E à travers un poteau Mk1. Le cargo est
posé d'après la place réelle du quai A.

0.13.0 (14/10/2026) : 61 tests unitaires. Phase 1 : 26 PASS, 0 FAIL, dont PASS convoyeurs (12 fers,
6 dans chaque conteneur), PASS atelier E derrière le poteau, PASS extracteur selon la pureté du
rocher, PASS wiki 109 pages, 0 lien cassé. Un passage sur trois, le vaisseau de test a glissé dans
la pente et le quai a échoué (cargo hors de portée) : c'est la physique du terrain, pas le code.
Phase 2 tout PASS. Phase 3 : PASS 200/200, 0,45 ms par tick. Phase 4 : 31 planches (dont convoyeurs
et poteaux), photos des gisements décorés (deposits.png) et du scanner Mk3 (scanner.png).
Non testés en jeu : élévateur et fusionneur en service, poteaux Mk2/Mk3 branchés, recettes des
scanners après leur recherche, repères du Mk3 sur des gisements devant le joueur.

Depuis 0.12.0, le vaisseau a une rangée de plus (à gauche, le cargo flotte à droite) :
condenseur d'eau (câblé à un générateur à bois), recycleur (100 fer) alimenté par le réacteur
stellaire (2 barres), tour radar.

```
PHASE=3 FRESH=1 WORLD=test4 tools/devrun.sh 90   # charge : 200 machines
PHASE=4 FRESH=1 WORLD=test4 tools/devrun.sh 70   # vitrine : dev/model_<id>.png, 4 vues par machine
```

0.12.0 (13/10/2026) : phase 1 tout PASS (24 PASS, 0 FAIL), dont PASS recycleur 100 fer → 1 bon
de recyclage ; PASS réacteur stellaire en veille, 1 010 J tirés de la barre en cours (il
alimente le recycleur) ; PASS condenseur 4 glaces en ~32 s ; PASS tour radar en production ;
PASS quai A → cargo (17 objets) ; PASS wiki 98 pages, 0 lien cassé. Phase 2 tout PASS (contenu
identique après rechargement). Phase 3 : PASS 200/200, 0,29 ms par tick. Phase 4 : 22 planches
de modèles relues une à une (pièces flottantes, textures, ports). Recherche avec TF Advanced
installé : 15 techs placées, aucune à gauche de ses prérequis. Non testés en jeu : convertisseur,
encodeur, comptoir d'échange, brûleur (leurs recettes et règles sont couvertes par les tests
unitaires), déchets du réacteur (une barre dure 5 min ; testés en unitaire).

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

0.10.0 (12/10/2026) : PASS haut fourneau 6 fer + 2 charbon de bois → 6 lingots d'acier ;
PASS raffinerie réglée sur plastique 12 biomasse + 2 glace → 4 plastique + 2 résine ; PASS
accumulateur chargé par le surplus du générateur (120 kJ en ~30 s), charge écrite dans
`stellar-industries.txt` et relue après rechargement ; PASS broyeur avec le nouveau coproduit
(silice) ; PASS wiki : 84 pages, 0 lien cassé, entrée trouvée dans le Guide, captures
`dev/wiki_*.png` vérifiées ; recherche : 9 techs placées de gauche à droite (colonnes 5 à 9,
l'écran de recherche du jeu s'élargit jusqu'à la dernière colonne) ; animations (17 machines)
et tous les tests précédents repassés. Non testés en production : accélérateur, fabricateur et
les recettes de haut niveau (validées sans le jeu).

0.9.0 (12/10/2026), modèles : PASS animation (12 visuels de machines, 29 des 46 pièces
mobiles en mouvement — celles des machines en production —, 8 voyants verts et 3 rouges) ;
photos rapprochées avant/arrière (`dev/closeup_front.png`, `closeup_back.png`) et planche des
27 objets (`dev/items.png`) vérifiées à l'œil ; ports d'objets : le second port d'Item Lines
déplacé de l'avant (0 ; 0,75 ; 0,77) à la trappe arrière (-0,2 ; 0,4 ; -0,55) ; tous les tests
de production, rechargement et charge repassés (200 machines animées : 0,244 ms par tick,
66 → 60 images/s). Les 13 nouvelles recettes sont validées sans le jeu, pas produites en jeu.

0.8.0 (12/10/2026), finitions : tableau de bord construit pendant le test (10 machines : 4 en
production, 3 en attente, 1 hors tension, 1 sortie bloquée, 1 générateur en veille ; les deux
machines à problème listées ; simulation 0,075 ms par tick) ; bruit des machines démarré par
FMOD sans erreur, volume 0,18 à ~10 m (réglage 0,5) — **l'écoute elle-même n'a pas été
vérifiée**.

Test de charge (`PHASE=3 FRESH=1 WORLD=test4 tools/devrun.sh 75`) : un vaisseau de 200
ateliers remplis (énergie non requise pendant le test), rien n'est sauvegardé. PASS 200/200 en
production, **0,238 ms par tick** en moyenne (pic 2,792 ms), 64 images/s (73 avant).

0.7.0 (12/10/2026), quais : un petit vaisseau cargo (3 × 3, un quai en réception) est posé
à côté du vaisseau de test ; le quai A (12 fer, 5 cuivre, en envoi) l'a trouvé à 3,0 m : PASS
quai A vide, quai du cargo 12 fer + 5 cuivre ; conteneur intact (50 vis) ; contenus identiques
après rechargement.

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

0.17.1 (16/10/2026), extracteur contre un gros rocher (bug signalé par un joueur : extracteur
collé à un grand cristal, « No deposit in reach ») : la portée se mesurait jusqu'au centre du
rocher. Le rocher de test est maintenant gros (dessiné sur 8 m autour d'un petit collider de
1,2 m, centre à 6,5 m de l'extracteur, hors de portée par l'ancienne mesure) : PASS 6 minerais
extraits, « Gisement … à 1.0 m ». Mesures des vrais rochers du monde « test factory » : colliders
1,23 m, modèle ~1,5 m, centre à 0,2-0,3 m du pivot ; aucun grand cristal chargé près du joueur,
le cristal du joueur n'a donc pas été mesuré lui-même. Phase 1 : 26 PASS, 0 FAIL ; phase 2 :
5 PASS, 0 FAIL ; tests unitaires 66/66.

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
| Quai : amarrer un vrai cargo piloté à côté d'une base | non testé |
| Touche F7, écouter le bruit des machines | non testé à la main |
| Deux joueurs (hôte + client) | non testé |
| Onglet « Industrie+ » à l'écran, poser les machines de 0.12 à la main | non testé |
| Machines agrandies dans une ancienne sauvegarde (chevauchement) | non testé |
| Poser convoyeurs et poteaux à la main, sens des tapis | non testé |
| Scanner avec un vrai scanner en inventaire (client et hôte) | non testé |
