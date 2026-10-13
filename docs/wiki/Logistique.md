# Logistique

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
Le scanner (touche **K**) voit à 80 m (réglage `ScanRadius`). Près d'une [Tour radar](Machine-7622) (moins de 60 m), il porte jusqu'à 400 m.

## Stocker
Le [Conteneur industriel](Machine-7608) offre 10 emplacements, comme toutes les machines du mod.
