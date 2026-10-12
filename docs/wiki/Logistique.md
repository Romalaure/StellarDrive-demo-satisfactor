# Logistique

## Lignes d'objets (mod Item Lines)
Le mod facultatif *Item Lines* ajoute deux ports d'objets à chaque machine, à l'arrière, dans les trappes jaunes à gauche de la prise électrique. Reliez-les avec des câbles d'objets.

- Une machine **n'envoie que ses produits** et jamais un ingrédient de sa recette en cours.
- Elle **ne reçoit que les ingrédients** de ses recettes (ou de la recette choisie avec N), jusqu'à 4 cycles d'avance, et garde 2 emplacements libres pour ses produits.
- Un générateur reçoit du combustible (20 d'avance), un extracteur envoie tout ce qu'il extrait.
- Les [conteneurs](Machine-7608) et les [quais](Machine-7609) acceptent et rendent tout : ils servent de tampons.

## Entre deux vaisseaux : le quai de transfert
Les lignes d'objets restent dans un vaisseau. Pour passer d'un vaisseau à l'autre (un cargo et une base) :
1. Posez un [Quai de transfert](Machine-7609) de chaque côté, à moins de 4 m l'un de l'autre.
2. Visez le premier et appuyez sur **N** jusqu'à « envoi », le second jusqu'à « réception ».
3. Le quai en envoi vide son contenu dans l'autre, 4 objets par seconde.

## Stocker
Le [Conteneur industriel](Machine-7608) a 10 emplacements, comme toutes les machines du mod (limite du stockage du jeu).
