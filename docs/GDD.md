# Document de conception (GDD) — version de travail

Ce document résume la vision du jeu et les décisions déjà prises. Il sert de référence pour les prochaines étapes.

## Pitch

Le Consortium Tianlong, dont les lois sur la manipulation génétique sont les plus permissives au monde, a créé une
nouvelle lignée d'humains augmentés, plus rapides que les humains. Un essai a mal tourné : une partie d'un gène a été
inhibée alors qu'elle n'aurait pas dû l'être. Le sujet se reproduit très vite en parasitant des civils à l'aide de
tentacules. Vous êtes un flic, et une simple patrouille vous jette dans cette guerre.

## Univers

- **Consortium Tianlong** : bloc englobant la Chine, une partie de la Russie et d'autres pays. Programme génétique
  « Taotie ». Le jeu commence dans sa mégapole de **Jinhai** (quartier de Longmen) : noms à consonance asiatique.
- **Union Panaméricaine** (nom de travail) : des États-Unis élargis au Groenland, au Canada et à une partie de
  l'Amérique du Sud. On découvre à la fin qu'elle a ses propres usines de clones et son programme d'humains augmentés.
- Les mutants sont des **victimes d'un programme**, pas une ethnie : l'antagoniste est un État et ses laboratoires.
- Ton : **sombre et sérieux** pour la première partie.

## Le parasite (règles)

- Le mutant infecte n'importe quel civil avec ses tentacules (contact, **jamais de contenu sexuel**).
- Chez les femmes, le parasite déclenche une **gestation accélérée** ; chez les hommes, il se développe dans le
  corps de l'hôte. Représentation **non graphique** : une excroissance lumineuse qui pulse, une jauge d'incubation.
- À terme, un **rejeton** naît : aussi rapide que son parent et capable de propager le parasite à son tour.
- Choix du joueur pour chaque hôte :
  - **Extraire le parasite** : lent (6 s maintenues), l'hôte survit ;
  - **Neutraliser l'hôte** : immédiat, abstrait (l'hôte s'effondre), et comptabilisé.
- Ces choix alimentent la jauge morale, les fins, et l'Épisode 2 (des images de vos actes fuitent sur Internet).

## Structure du jeu (ordre de développement validé)

1. ✅ **Mission 1 « La poursuite »** (cette tranche jouable) : patrouille, poursuite, combat dans les docks,
   échantillon, hôtes infectés, rapport.
2. **Armure évolutive** (style Iron Man) : course sur les murs, vol, missiles, bâtiments destructibles.
   Monde : zones de ville semi-ouvertes, voire une ville entière totalement ouverte.
3. **Vaccin** : rend les mutants inoffensifs et conscients de la manipulation. Puis les **résistants** : le vaccin
   ne marche plus sur certains, la seule solution est de les éliminer.
4. **Usines de clones** capturées : elles produisent des monstres alliés **autonomes**. **Fusion par parties**
   (tête, bras, jambes), chaque partie apporte un pouvoir, statistiques combinées.
5. **Révélation** du programme de l'Union Panaméricaine, et **2 ou 3 fins** selon la jauge morale :
   déposer les armes, ou devenir la prochaine bête à abattre.
6. **Épisode 1 Prime** : dominer le monde sans se faire tuer, armements de plus en plus efficaces, **action avec une
   carte de conquête** entre les missions.
7. **Épisode 2** : après la fuite d'images, tout le monde est contre vous. Objectif : s'échapper **sans tuer
   personne**. Pouvoirs des esprits (folklore asiatique, fantômes, schizophrénie, sixième sens) et gadgets non
   létaux (leurres, camouflage, changement d'apparence, paralysants).
8. **Mode monstre** : rejouer les mêmes niveaux avec les pouvoirs du mutant.

## Technique

- Unity 6 LTS, 3D à la troisième personne, nouveau Input System (clavier/souris + manette).
- Textes en français et en anglais (`Assets/Game/Resources/Localization/strings.json`).
- La tranche jouable génère tout par le code. Les graphismes seront remplacés par des modèles de l'Asset Store,
  importés par l'équipe dans l'éditeur (l'Asset Store nécessite un compte Unity), puis branchés sur le code.

## Personnages de la Mission 1

| Personnage | Rôle |
| --- | --- |
| Vous (agent de l'unité 7) | Héros sans nom |
| Opératrice Lin | Centrale radio |
| Capitaine Zhao | Supérieur hiérarchique, veut étouffer l'affaire |
| Dr Shen Yao | Médecine légale, analyse l'échantillon et cherchera le vaccin |
| Sujet T-07 | Le mutant, issu du programme Taotie |
