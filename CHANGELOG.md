# Journal des changements

## 0.17.0 — gisements inépuisables, extracteurs Mk2 et foreuse

- **Gisements inépuisables** : environ un rocher sur huit (tous minerais) ne se casse jamais au
  laser ; il rend ses objets à chaque fois que sa jauge tombe à zéro, puis se remet à neuf. Un
  extracteur posé à côté y travaille 2,5 fois plus vite (en plus de la pureté). On les reconnaît à
  leur couronne de pointes de minerai et d'éclats violets lumineux ; le scanner les marque « ∞ ».
- **Minerais du mod plus faciles à reconnaître** : charbon, soufre, calcaire et quartz couvrent
  tout le rocher, l'or presque tout.
- **Extracteur Mk2** (recherche *Extraction avancée*) : 1 objet toutes les 3 s, portée 8 m.
- **Foreuse de gisement** (recherche *Forage profond*) : 1 objet toutes les 1,5 s, 4 kW.
- **Nouvelle image de présentation** : une usine complète en marche (4 lignes de convoyeurs, centrale
  à trois réacteurs, pylônes, foreuses sur des gisements), construite et photographiée par le test
  en jeu (phase 5).
- Les rochers « statiques » des planètes (dessinés par un autre objet du jeu) reçoivent eux aussi
  leur minerai dessiné.

## 0.16.0 — nouveaux minerais

- **Cinq minerais dans le monde** : charbon, soufre, calcaire, quartz brut et minerai d'or. Environ
  quatre rochers de fer, de cuivre ou d'aluminium sur dix en contiennent un à la place ; chaque
  planète en a trois sur cinq. Le choix vient de l'identifiant du rocher dans le jeu : il est le
  même pour tous les joueurs et à chaque partie. Ils se minent au laser, s'extraient avec
  l'extracteur, apparaissent au scanner et ont leur couleur sur le rocher.
- **À quoi ils servent** : charbon (combustible des générateurs, acier au haut fourneau), soufre
  (caoutchouc, cellules de batterie), calcaire (béton), quartz brut (silice, cristaux de quartz),
  or (lingots d'or, circuits imprimés). 8 recettes, 6 objets (7768-7773).
- **Pureté stable** : la pureté des rochers des points chauds du jeu (la plupart des gisements)
  dépendait de leur position et pouvait changer d'une session à l'autre ; elle vient maintenant de
  leur identifiant.

## 0.15.0 — modules montés, scanner en main, tableau de bord

- **Modules montés dans le scanner** : clic droit avec le scanner en main (ou Maj+N, Maj+K) ouvre
  sa baie à modules. Monter un module le retire de l'inventaire, le retirer le rend. 2 emplacements
  sur le Mk1, 3 sur le Mk2, 4 sur le Mk3. Le jeu ne peut pas attacher de données à un objet : les
  modules montés suivent le compte du joueur et servent avec n'importe quel scanner porté.
  Les modules gardés seulement dans l'inventaire ne comptent plus : il faut les monter.
- **Nouveaux modules** : portée II (+50 %, fabricateur), portée III (×2, accélérateur), analyse II
  (pureté, rendement et repères dans la vue sur n'importe quel scanner, accélérateur). Nouvelle
  recherche *Prospection quantique*.
- **Scanner en main** : avec le scanner sélectionné dans la barre d'outils, clic gauche = balayage.
- **Son et animation** : balayage sonore avec deux bips (réglage `ScanVolume`), ondes qui
  partent du centre de l'écran ; bourdonnement et ondes rouges sans scanner.
- **Pureté au laser** : miner un rocher à la main donne aussi ×½ (impur) ou ×2 (pur).
- **Tour radar** : les modules de portée s'ajoutent maintenant à sa portée (plafond 800 m).
- **Poteaux électriques** une fois et demie plus grands (3 m, 4,5 m, 6 m).
- **Tableau de bord (F7)** refait : tuiles (machines, en marche, à vérifier, convoyeurs), barres
  d'énergie et d'accumulateurs, barre des états, production de la dernière minute objet par
  objet, machines par type, liste des machines à vérifier.

## 0.14.0 — gisements, modèles d'objets, touche N

- **Gisements** : les blocs posés sur les rochers sont remplacés par des facettes du rocher
  lui-même couvertes de minerai (fer, cuivre, aluminium, éclats, glace), dans le style du jeu ;
  plus de facettes sur un gisement pur. Toujours désactivable (`DepositDetails`).
- **Recherche** : la fiche des techs *Prospection*, *Prospection avancée* et *Analyse géologique*
  liste maintenant les recettes qu'elles débloquent (scanners et modules), avec leur machine.
- **Touche N** : sur une machine, elle change la recette ; ailleurs, elle lance le scanner.
  K reste disponible (réglage `ScanKey`, vide pour la retirer).
- **Objets à taille réelle** : poutres, cadres, tuyaux, moteurs ou barres de combustible sont
  à leur vraie taille (jusqu'à 3 fois le modèle), les vis, puces et modules tiennent dans la main.
- **Modèles** : chaque objet du mod a son propre modèle ; la barre de combustible, le déchet
  stellaire, le bon de recyclage, le processeur neuro-quantique, le cristal temporel et le module
  logique n'avaient qu'une forme générique.
- **Élévateur à godets** une fois et demie plus grand (monte de 3 m).

Note : le soufre, l'acide sulfurique et leurs gisements viennent du mod StellarFactory (un autre
auteur), pas de Stellar Industries : leurs modèles ne peuvent être corrigés que dans ce mod.

## 0.13.0 — convoyeurs, poteaux électriques, scanners

**Convoyeurs** (nouvel onglet « Convoyeurs », recherches *Convoyeurs*, *Convoyeurs rapides*, *Convoyeurs lourds*) :
tapis Mk1 / Mk2 / Mk3 (1, 2,5 et 6 objets/s), élévateur à godets (monte de 2 m), répartiteur et
fusionneur. Un convoyeur prend dans la pièce derrière lui et pose dans celle devant lui, sans
courant ni câble ; les machines gardent leurs règles (elles ne donnent que leurs produits, ne
prennent que leurs ingrédients). Des caisses roulent sur les tapis quand ils transportent.

**Poteaux électriques** (onglet « Industrie+ ») : Mk1 (2 m, 4 branchements), Mk2 (3 m, 8),
pylône Mk3 (4 m, 12). Toutes les prises d'un poteau sont sur le même réseau.

**Scanner de gisements en objets** : la touche K demande maintenant un scanner dans l'inventaire.
Mk1 (atelier, 80 m), Mk2 (assembleuse, 160 m, écran radar, pureté), Mk3 (fabricateur, 300 m,
repères dans la vue), module de portée (+25 %) et module d'analyse (pureté et rendement). Chaque
recette attend sa recherche (*Prospection*, *Prospection avancée*, *Analyse géologique*).
Nouvel affichage : panneau, flèches de direction, radar, repères. Réglage `ScanNeedsScanner`
pour garder l'ancien fonctionnement.

**Gisements** : chaque rocher a une pureté (impur ×½, normal, pur ×2 pour l'extracteur), la même
pour tous les joueurs. Les rochers portent des nodules ou des cristaux de leur minerai, plus
nombreux quand le gisement est riche (purement visuel, réglage `DepositDetails`).

**Machines** : le panneau de visée dit à quoi sert la machine et ce qu'elle produit ; un
pictogramme de son rôle est fixé à l'arrière. Les panneaux, flèches et écrans des modèles sont
maintenant à l'endroit (ils étaient tournés ou en miroir).

**Contenu** : pièces 7623-7631, objets 7760-7764, techs 7816-7823, 5 recettes.

## 0.12.0 — modèles revus, 8 nouvelles machines

**Modèles 3D**
- Presse : le pupitre est fixé sur le montant droit, les flexibles passent derrière les colonnes,
  le réservoir hydraulique est posé au sol.
- Broyeur : refait en vrai concasseur à mâchoires (bâti, mâchoire fixe, mâchoire mobile animée
  par l'arbre excentrique, deux volants, moteur et courroies, trémie, tapis de sortie).
- Assembleuse : un bras robot posé sur le plateau, avec plateau tournant, au lieu du portique.
- Fabricateur : remis à l'endroit (façade ouverte à l'avant, paroi pleine à l'arrière).
- Raffinerie : la pompe est posée sur sa cuve et le pupitre a son pied.
- Générateur à combustion : vilebrequin, palier sous le volant et l'alternateur, radiateur
  contre le bloc, trémie sur sa platine.
- Quai de transfert et conteneur : les ports d'objets sont dans des trappes de la machine, plus
  en l'air. Tous les ports (Item Lines) sont maintenant sur la rangée de service à l'arrière.
- Plus aucun pupitre, panneau ou rangée de service qui flotte : chaque pièce a son support.
- Textures : les grandes faces répètent la texture au lieu de l'étirer, les trémies carrées
  restent carrées, les trémies ouvertes ont un bord au lieu d'un couvercle.
- Machines plus grandes quand elles doivent l'être : de ×1,1 (fonderie) à ×2 (accélérateur,
  réacteur). **Attention** : dans une partie existante, une machine agrandie peut chevaucher sa
  voisine ; déplacez-la si besoin (aucun objet n'est perdu).

**8 machines** (7615-7622), rangées dans un second onglet « Industrie+ » (énergie, extraction,
logistique) :
- *Brûleur à biomasse* : petit générateur de début de partie, 1 500 W.
- *Condenseur d'eau* : une glace toutes les 8 s, n'importe où.
- *Réacteur stellaire* : 25 kW avec des barres de combustible stellaire ; chaque barre laisse
  un déchet à évacuer.
- *Encodeur quantique* : cristaux temporels, processeurs neuro-quantiques, recyclage des
  déchets stellaires.
- *Convertisseur* : fer en cuivre, cuivre en aluminium, poussière en éclats stellaires.
- *Recycleur* : détruit les objets en trop et paie en bons de recyclage, selon ce que l'objet a
  coûté à fabriquer.
- *Comptoir d'échange* : bons contre minerais, éclats ou matière stellaire.
- *Tour radar* : près d'elle, le scanner (K) porte à 400 m. Pas besoin de courant.

**Contenu** : 6 objets (7754-7759), 13 recettes, 6 techs (7810-7815). La recherche peut
maintenant s'étendre vers la droite quand d'autres mods remplissent l'arbre.

**Tests** : 55 tests unitaires ; le vaisseau du test en jeu porte aussi condenseur, recycleur
(alimenté par le réacteur) et tour radar ; nouvelle phase 4 qui photographie chaque modèle.

Toujours impossible dans StellarDrive : fluides en tuyaux, trains, véhicules, drones, ascenseur
spatial (voir la page Limites du wiki).

## 0.11.0 — anglais

- Le mod parle maintenant anglais : noms et descriptions des objets et des machines, recettes,
  recherche, panneau d'état, scanner, tableau de bord et tout le wiki. Il suit la langue du jeu
  (réglage `Language` : `auto`, `fr` ou `en`) et se met à jour sans relancer le jeu.
- README, description du mod et pages du wiki réécrits, plus simples à lire.

## 0.10.0 — contenu étendu et wiki dans le jeu

- Comparaison avec les jeux d'automatisation (liste des bâtiments et objets d'un wiki de
  référence) : ajout de ce qui manquait et que StellarDrive permet, sous des noms et des modèles
  propres au mod. Les fluides, véhicules et trains restent impossibles (voir Limites).
- **4 machines** : *Haut fourneau* (7611, deux entrées : acier au charbon de bois, alliage,
  quartz), *Raffinerie* (7612, chimie de la biomasse avec coproduits), *Accumulateur* (7613,
  60 MJ, se charge du surplus du réseau et le rend, charge gardée dans la sauvegarde),
  *Accélérateur de particules* (7614, 8 kW). Modèles détaillés et animés, prises à l'arrière.
- **26 objets** (7728-7753) : charbon de bois, biomasse, silice, cristal de quartz, plastique,
  caoutchouc, résine, lingot et tôle d'alliage, boîtier en aluminium, fil fin, poutre enrobée,
  panneau intelligent, ossature polyvalente, faisceau de câbles, oscillateur à quartz,
  connecteur rapide, module radio, cellule de batterie, système de refroidissement, bloc moteur,
  contrôleur adaptatif, supercalculateur, turbomoteur, matière stellaire condensée, processeur
  quantique ; chacun avec son modèle. 53 objets et 59 recettes en tout.
- Combustibles : biomasse (15 kJ), charbon de bois (300 kJ).
- Recherche : *Métallurgie et chimie*, *Stockage d'énergie*, *Physique des particules*
  (7807-7809) ; placement de la branche revu (les techs suivantes ont toujours leur place à
  droite, la branche ne se mélange plus quand d'autres mods ajoutent des techs).
- **Wiki dans le jeu** : entrée « Wiki Stellar Industries » dans le Guide (touche G), pages
  écrites (démarrer, énergie, logistique, touches, limites) et pages créées à partir du contenu
  (une fiche par machine et par objet : recettes, débit par minute, obtention, utilisation,
  coût, recherche, prises), liens cliquables.
- Menu de construction : machines rangées en trois rangées (production de base, avancée,
  énergie et logistique).
- **Changement** : le coproduit du broyeur sur les éclats stellaires est la silice (et non plus
  le verre, que la même machine broyait aussitôt).

## 0.9.0 — modèles, textures, animations, nouveaux objets

- **Textures propres au mod** : planche de 48 tuiles dessinées par le code dans le style du jeu
  (tôles peintes à contour, acier brossé, rivets, tôle larmée, bandes de danger, grilles,
  écrans, voyants, cuivre, circuits...), avec carte de normales (rainures, rivets, fentes) et
  carte d'émission (écrans, voyants, bouches de four), portées par le shader des pièces du jeu.
  Générée une fois puis mise en cache (`UserData/StellarIndustries/cache`).
- **Modèles refaits, plus détaillés**, avec une disposition commune : châssis à tôle larmée et
  bande de danger, côté opérateur à l'avant (console à boutons, écran, jauge, voyant d'état),
  côté service à l'arrière.
- **Prises logiques** : la prise électrique sort d'un boîtier électrique jaune et noir (avec
  son conduit) à l'arrière ; les deux ports d'objets qu'ajoute *Item Lines* sont rangés dans
  deux trappes de la même rangée (le second était au milieu de la face avant : il est déplacé).
- **Animations** pendant la production : volants et tambours qui tournent, coulisseau de presse
  qui frappe, bras robotisés, foreuse qui tourne et descend, pistons, ventilateurs, rouleaux du
  quai, gyrophare, bouches de four et écrans qui s'allument, voyant rouge → vert. L'hôte envoie
  la liste des machines actives aux clients (une fois par seconde, si elle change).
- **Objets redessinés** : chaque objet a son modèle (lingots trapézoïdaux, bobines, sacs de
  poudre, tas de vis, plaques boulonnées...) et ses textures ; les icônes d'inventaire en
  découlent.
- **13 nouveaux objets** (noms génériques) : lingot d'acier, poutre et tuyau d'acier, béton,
  feuille de cuivre, rotor, stator, moteur électrique, cadre d'assemblage, circuit imprimé,
  ordinateur, dissipateur thermique, cadre lourd (7715-7727).
- **Fabricateur** (7610, 1 500 W) : cellule robotisée à deux bras, jusqu'à 4 ingrédients
  (moteur, ordinateur, cadre lourd). Tech *Fabrication avancée* (7806).
- 13 nouvelles recettes (acier, poutres, tuyaux, béton, feuilles de cuivre, rotor, stator,
  cadre, circuit, dissipateur, moteur, ordinateur, cadre lourd) : 32 au total.

## 0.8.0 — phases H et I (finitions, diagnostic, charge, multijoueur)

- **Tableau de bord industriel** (`F7`) : machines par état, énergie consommée et produite,
  cycles de la dernière minute, machines à vérifier (sans câble, sans énergie, pleines, sans
  gisement, sans combustible), coût de la simulation (ms par tick, moyenne et pic). Chez un
  client, le texte vient de l'hôte.
- **Bruit des machines** : ronronnement original synthétisé par le mod (WAV écrit dans
  `UserData/StellarIndustries/sounds`), joué par FMOD, volume selon la machine active la plus
  proche (15 m). Réglage `MachineVolume`.
- Modèles propres pour les 9 pièces (atelier, fonderie, presse, broyeur, assembleuse,
  extracteur, générateur, conteneur, quai).
- **Test de charge** (`DevPhase = 3`) : 200 ateliers produisant ensemble → 0,24 ms par tick de
  simulation (pic 2,8 ms), 73 → 64 images/s.
- Multijoueur : tout l'état visible (panneau, tableau de bord, choix de recette, mode des quais)
  passe par les messages réseau du mod ; **non testé à deux joueurs**.

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
