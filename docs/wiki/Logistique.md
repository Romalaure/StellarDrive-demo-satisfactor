# Logistique

## Convoyeurs
Les convoyeurs (onglet de construction « Convoyeurs », recherche *Convoyeurs*) transportent les objets d'une pièce à celle qui touche leur bout, sans courant :

- posez un [Convoyeur Mk1](Machine-7623) derrière une machine ou un coffre, les chevrons du tapis pointant vers la destination, puis d'autres convoyeurs bout à bout jusqu'à une machine, un coffre ou un [conteneur](Machine-7608) ;
- le premier tapis **prend** dans la pièce derrière lui (une machine ne donne que ses produits, un coffre donne tout) ; chaque tapis **pose** dans la pièce devant lui (une machine ne prend que les ingrédients de ses recettes) ;
- débit : Mk1 1 objet/s, [Mk2](Machine-7624) 2,5/s, [Mk3](Machine-7625) 6/s ; un tapis porte quelques objets au plus, et une ligne pleine s'arrête comme un vrai tapis ;
- le [Répartiteur](Machine-7627) distribue à tour de rôle vers l'avant, la gauche et la droite ; le [Fusionneur](Machine-7628) rassemble l'arrière, la gauche et la droite vers l'avant ;
- l'[Élévateur à godets](Machine-7626) monte de trois mètres : il prend derrière son pied et pose devant son sommet.

Regardez un convoyeur pour voir ce qui est branché à son entrée et à sa sortie. Les convoyeurs restent à l'intérieur d'un vaisseau ou d'une base.

## Relier les machines (mod Item Lines)
Avec le mod *Item Lines*, chaque machine reçoit deux ports d'objets, à l'arrière, dans les petites trappes jaunes à gauche de la prise électrique. Reliez-les avec des câbles d'objets et laissez faire :

- une machine n'envoie que ce qu'elle a fabriqué, jamais les ingrédients de sa recette en cours ;
- elle ne prend que ce dont elle a besoin (ou ce dont a besoin la recette choisie avec N), avec quelques cycles d'avance, et garde toujours deux emplacements libres pour ranger ses produits ;
- un générateur se fait livrer du combustible (et le réacteur renvoie ses déchets), un extracteur ou un [condenseur](Machine-7616) envoie tout ce qu'il produit ;
- le [Recycleur](Machine-7620) accepte presque tout et ne renvoie que des bons de recyclage ;
- les [conteneurs](Machine-7608) et les [quais](Machine-7609) prennent et rendent de tout : parfaits comme zones tampons.

## D'un vaisseau à l'autre
Les lignes d'objets restent à l'intérieur d'un vaisseau. Pour décharger un cargo dans votre base, utilisez deux [quais de transfert](Machine-7609) :
1. posez un quai de chaque côté, à moins de 4 m l'un de l'autre ;
2. regardez le premier et appuyez sur **N** jusqu'à « envoi », puis le second jusqu'à « réception » ;
3. le quai en envoi se vide dans l'autre, 4 objets par seconde.

## Recycler et échanger
Vous croulez sous les vis ou les plaques en trop ? Envoyez-les au [Recycleur](Machine-7620) : il les détruit et vous paie en [bons de recyclage](Objet-7756). Plus un objet demande de travail, plus il rapporte (100 points font un bon ; un minerai vaut 1 point). Les bons s'échangent ensuite au [Comptoir d'échange](Machine-7621) contre du minerai, des éclats stellaires ou de la matière stellaire : choisissez ce que vous voulez avec **N**.

## Trouver les gisements
Le scanner de gisements est un objet : gardez-le dans votre inventaire et appuyez sur **N** sans viser de machine (ou **K**), ou prenez-le en main dans la barre d'outils et **cliquez**.

| Objet | Où | Ce qu'il fait |
|---|---|---|
| [Scanner Mk1](Objet-7760) | atelier, recherche *Prospection* | 80 m, chaque minerai, le plus proche avec sa distance et une flèche |
| [Scanner Mk2](Objet-7761) | assembleuse, *Prospection avancée* | 160 m, écran radar, pureté des gisements |
| [Scanner Mk3](Objet-7762) | fabricateur, *Analyse géologique* | 300 m, repères sur les gisements dans votre vue pendant 30 s |
| [Module de portée](Objet-7763) | assembleuse | +25 % de portée |
| [Module de portée II](Objet-7765) | fabricateur | +50 % de portée |
| [Module de portée III](Objet-7766) | accélérateur, *Prospection quantique* | double la portée |
| [Module d'analyse](Objet-7764) | fabricateur | pureté et rendement d'extraction |
| [Module d'analyse II](Objet-7767) | accélérateur, *Prospection quantique* | pureté, rendement et repères dans la vue |

**Monter les modules** : clic droit avec le scanner en main (ou **Maj+N**) ouvre la baie à modules. Le Mk1 a 2 emplacements, le Mk2 3, le Mk3 4. Un module monté quitte l'inventaire ; le retirer le rend. Les modules montés suivent votre compte : ils servent avec n'importe quel scanner que vous portez.

Près d'une [Tour radar](Machine-7622) (moins de 60 m), le scanner porte au moins à 400 m, plus ses modules de portée (800 m au plus).

**Pureté** : chaque rocher est *impur* (un extracteur y travaille à moitié de sa vitesse, le laser en tire moitié moins), *normal* ou *pur* (vitesse et minage doublés). Un gisement riche se voit : ses filons et nodules de minerai sont plus nombreux et plus gros.

## Nouveaux minerais
Environ quatre rochers de métal sur dix contiennent un minerai du mod à la place du fer, du cuivre ou de l'aluminium. Chaque planète en a trois sur cinq. Ils se minent au laser, s'extraient et se repèrent au scanner comme les autres.

| Minerai | Couleur sur le rocher | Sert à |
|---|---|---|
| [Charbon](Objet-7768) | noir | combustible des générateurs, acier au haut fourneau |
| [Soufre](Objet-7769) | jaune, petits cristaux | caoutchouc (raffinerie), cellules de batterie |
| [Calcaire](Objet-7770) | blanc cassé | béton (presse) |
| [Quartz brut](Objet-7771) | cristaux clairs | silice (broyeur), cristaux de quartz (haut fourneau) |
| [Minerai d'or](Objet-7772) | doré | [lingots d'or](Objet-7773) (fonderie), puis circuits imprimés |

## Gisements inépuisables et extracteurs
Environ un rocher sur huit est un **gisement inépuisable** : couronne de pointes de minerai et d'éclats violets lumineux, « ∞ » au scanner. Il est réservé aux extracteurs : le laser de minage ne l'entame pas. Un extracteur posé à côté y travaille **2,5 fois plus vite**.

| Extracteur | Vitesse | Portée | Recherche |
|---|---|---|---|
| [Extracteur](Machine-7606) | 1 objet / 6 s | 6 m | *Industrie de base* |
| [Extracteur Mk2](Machine-7632) | 1 objet / 3 s | 8 m | *Extraction avancée* |
| [Foreuse de gisement](Machine-7633) | 1 objet / 1,5 s | 6 m | *Forage profond* |

La pureté (×½, ×1, ×2) et le gisement inépuisable (×2,5) s'ajoutent à ces vitesses.

## Stocker
Le [Conteneur industriel](Machine-7608) offre 40 emplacements : la grille défile (molette ou barre à droite) pour atteindre les derniers. Il est léger (15 kg à vide), pour servir de soute à un vaisseau cargo ; ce qu'il contient pèse son poids habituel.
