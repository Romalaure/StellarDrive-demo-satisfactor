# Compatibilité

## Sauvegardes

| Donnée | Où | Statut |
|---|---|---|
| Machines posées | Pièce de vaisseau du jeu (id 7601), sauvée par le jeu | ✅ testé |
| Contenu des machines | `storageStates` du vaisseau (format du jeu) | ✅ testé : identique après rechargement |
| Objets du mod (7701, 7702) dans coffres et inventaires | Ids d'objets ordinaires | ✅ dans les machines ; coffres/inventaire joueur : même mécanisme, non testé à part |
| Progression du cycle en cours | Mémoire seulement | ⚠️ repart à 0 après rechargement, **sans perte d'objets** |
| Recette en cours | Recalculée depuis le contenu | ✅ |

Aucun fichier de sauvegarde propre au mod n'existe en 0.1 : pas de schéma à migrer.

**Retirer le mod** : le jeu ignore au chargement les pièces dont l'id est inconnu (les machines
disparaissent avec leur contenu). Les objets 7701/7702 restés dans des coffres deviennent des
objets inconnus. Pour désinstaller proprement : vider les machines et coffres des objets du mod,
recycler les machines, sauvegarder, puis retirer la DLL. Faire une copie de la sauvegarde avant.

## Multijoueur

| Point | État |
|---|---|
| Autorité | L'hôte seul simule (patch dans `PowerServerTracker`, code serveur du jeu) : pas de double production |
| Réplication des inventaires | Par le jeu (`UpdatePartState` → synchronisation des états de pièces) |
| Joueur qui ajoute/retire des objets pendant un cycle | Le cycle relit l'inventaire avant d'écrire ; transaction refusée si les entrées ont disparu |
| Panneau d'état chez un client | Partiel : ce que le client peut déduire du contenu (« Prête », « En attente », « Sortie bloquée ») ; énergie et progression visibles chez l'hôte seulement |
| Déconnexion d'un joueur | Sans effet sur la simulation (hôte) |
| Tous les joueurs doivent avoir le mod et SDModKit | Oui (pièce 7601 et objets 7701/7702 doivent exister chez chacun) |
| **Testé à deux joueurs** | ❌ **non** — à faire avant d'annoncer la compatibilité |

## Autres mods

| Mod | Interaction |
|---|---|
| SDModKit | Requis pour les pièces |
| ItemLines (Krunk) | Ajoute un port d'objets à nos machines (comme à toute pièce à inventaire) : on peut les alimenter/vider par ligne d'objets. Compatible (constaté en jeu, transport non testé) |
| ExpandedStacks | Piles à 255 : les machines respectent la taille de pile de chaque objet |
| TF Advanced | Patche aussi le tick électrique et la liste d'objets : patchs indépendants, ids disjoints, testé ensemble |
