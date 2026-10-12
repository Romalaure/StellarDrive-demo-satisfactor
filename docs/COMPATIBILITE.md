# Compatibilité

## Sauvegardes

| Donnée | Où | Statut |
|---|---|---|
| Machines posées | Pièces de vaisseau du jeu (ids 7601-7609), sauvées par le jeu | ✅ testé |
| Contenu des machines, conteneurs, quais | `storageStates` du vaisseau (format du jeu) | ✅ testé : identique après rechargement |
| Objets du mod (7701-7714) dans coffres et inventaires | Ids d'objets ordinaires | ✅ dans les machines ; coffres/inventaire joueur : même mécanisme, non testé à part |
| Recette choisie, mode des quais | `stellar-industries.txt` à côté de `world.json` (une ligne `vaisseau pièce recette`) | ✅ testé : relu après rechargement |
| Techs de la branche Industrie | État de recherche du jeu (ids 7801-7805) | ✅ déblocage testé |
| Progression du cycle en cours | Mémoire seulement | ⚠️ repart à 0 après rechargement, **sans perte d'objets** |
| Énergie tirée de la bûche en cours (générateur) | Mémoire seulement | ⚠️ au rechargement la bûche entamée est rendue entière (jamais de perte) |

Le fichier compagnon est écrit juste après la sauvegarde du jeu, d'abord dans un fichier
temporaire puis renommé (pas de fichier à moitié écrit). Une ligne illisible est ignorée.
Les machines retirées sont oubliées à la sauvegarde suivante ; celles d'un vaisseau non chargé
sont gardées.

**Retirer le mod** : le jeu ignore au chargement les pièces dont l'id est inconnu (les machines
disparaissent avec leur contenu). Les objets du mod restés dans des coffres deviennent des objets
inconnus. Pour désinstaller proprement : vider les machines et coffres des objets du mod,
recycler les machines, sauvegarder, puis retirer la DLL. Faire une copie de la sauvegarde avant.
`stellar-industries.txt` peut rester ou être supprimé.

## Multijoueur

| Point | État |
|---|---|
| Autorité | L'hôte seul simule (patch dans `PowerServerTracker`, code serveur du jeu) : pas de double production |
| Réplication des inventaires | Par le jeu (`UpdatePartState` → synchronisation des états de pièces) |
| Joueur qui ajoute/retire des objets pendant un cycle | Le cycle relit l'inventaire avant d'écrire ; transaction refusée si les entrées ont disparu |
| Panneau d'état chez un client | Le client demande l'état de la machine visée à l'hôte (message réseau du mod, 2 fois par seconde) |
| Changer de recette / mode de quai chez un client | Message envoyé à l'hôte, qui vérifie la machine et la recette |
| Tableau de bord (F7) chez un client | Texte construit par l'hôte et envoyé sur demande |
| Bruit des machines chez un client | Approché : une machine dont le contenu permet une recette est considérée active |
| Recherche | État de recherche du jeu, synchronisé par le jeu |
| Tous les joueurs doivent avoir le mod et SDModKit | Oui (pièces, objets et messages réseau doivent exister chez chacun) |
| **Testé à deux joueurs** | ❌ **non** — seul le mode hôte (qui passe par les mêmes messages, en local) est testé |

Les messages réseau du mod sont un type FishNet (`SIMessage`) distinct de ceux des autres mods.

## Autres mods

| Mod | Interaction |
|---|---|
| SDModKit | Requis pour les pièces |
| Item Lines (Krunk) | Convoyeurs du mod (facultatif). Nos règles d'entrée/sortie s'appliquent par un patch sur `SDModKit.Game.ItemMove.Transfer` et sur `AttachedPortBehaviour.CanReceive` (par réflexion, absent = ignoré). Transport testé en jeu |
| ExpandedStacks | Piles à 255 : les machines respectent la taille de pile de chaque objet |
| TF Advanced | Patche aussi le tick électrique et la liste d'objets : patchs indépendants, ids disjoints, testé ensemble. Ses batteries fonctionnent avec nos machines |
| Mods qui ajoutent des techs (Arsenal, Item Lines, TF Advanced...) | Nos techs se placent dans les cases libres ; un id déjà pris est signalé et la tech n'est pas ajoutée |
