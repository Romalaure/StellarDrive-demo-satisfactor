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
Le scanner de gisements est un objet : gardez-le dans votre inventaire et appuyez sur **N** sans viser de machine (ou **K**).

| Objet | Où | Ce qu'il fait |
|---|---|---|
| [Scanner Mk1](Objet-7760) | atelier, recherche *Prospection* | 80 m, chaque minerai, le plus proche avec sa distance et une flèche |
| [Scanner Mk2](Objet-7761) | assembleuse, *Prospection avancée* | 160 m, écran radar, pureté des gisements |
| [Scanner Mk3](Objet-7762) | fabricateur, *Analyse géologique* | 300 m, repères sur les gisements dans votre vue pendant 30 s |
| [Module de portée](Objet-7763) | assembleuse | +25 % de portée (deux au plus) |
| [Module d'analyse](Objet-7764) | fabricateur | pureté et rendement d'extraction sur tout scanner |

Près d'une [Tour radar](Machine-7622) (moins de 60 m), le scanner porte jusqu'à 400 m.

**Pureté** : chaque rocher est *impur* (un extracteur y travaille à moitié de sa vitesse), *normal* ou *pur* (vitesse doublée). Un gisement riche se voit : ses filons et nodules de minerai sont plus nombreux et plus gros.

## Stocker
Le [Conteneur industriel](Machine-7608) offre 10 emplacements, comme toutes les machines du mod.
