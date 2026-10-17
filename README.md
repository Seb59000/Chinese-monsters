# Chinese-monsters — tranche jouable (Mission 1 : « La poursuite »)

Jeu d'action 3D à la troisième personne, Unity 6 LTS. Vous êtes un agent de police de Jinhai, une mégapole du
Consortium Tianlong. Une patrouille de nuit tourne à la course-poursuite contre un individu aux capacités
surhumaines : le Sujet T-07, issu d'une manipulation génétique qui a mal tourné.

Cette première version est une **tranche jouable** : une mission complète, construite entièrement par le code
(ville, personnages, sons, interface). Les systèmes suivants (armure, vaccin, usines de clones, fusion des monstres,
Épisodes 1 Prime et 2, mode monstre) viendront un par un. Voir [`docs/GDD.md`](docs/GDD.md).

## Lancer le jeu

1. **Unity Hub › Add › Add project from disk** et sélectionnez ce dossier.
   Choisissez votre éditeur Unity 6 LTS si le Hub demande une version.
2. À la première ouverture, Unity installe les paquets (Input System…). Si Unity propose
   **d'activer les nouveaux « backends » du système d'entrée et de redémarrer, répondez Oui**.
3. Le projet crée tout seul la scène `Assets/Scenes/Mission01.unity` et l'ouvre.
   Sinon : menu **Chinese Monsters › Ouvrir la Mission 01**.
4. Appuyez sur **Play** (ou menu **Chinese Monsters › Jouer**). Cliquez dans la vue Game pour capturer la souris.

> Astuce : dans un projet vide, appuyer sur Play dans une scène « Untitled » lance aussi le jeu.

Unity génère au premier lancement des fichiers dans `ProjectSettings/` et `Packages/packages-lock.json` :
c'est normal, vous pouvez les committer.

## Commandes

| Action | Clavier / souris | Manette |
| --- | --- | --- |
| Se déplacer | ZQSD (AZERTY) / WASD (QWERTY), flèches | Stick gauche |
| Caméra | Souris | Stick droit |
| Sprint | Maj (maintenir) | L3 (clic, bascule) |
| Sauter | Espace | A |
| Viser / Tirer | Clic droit / Clic gauche | LT / RT |
| Recharger | R | Y |
| Extraire le parasite (lent) | E (maintenir) | X (maintenir) |
| Neutraliser l'hôte | F (maintenir) | B (maintenir) |
| Pause | Échap | Start |

Le menu pause permet de changer la langue (français / anglais), la sensibilité, l'axe Y,
et de recommencer au dernier point de contrôle.

## Déroulé de la mission

1. **Patrouille** : rejoignez la ruelle de Longmen signalée par la centrale.
2. **Poursuite** : le suspect est bien plus rapide que vous ; ne le perdez pas de vue. Il infecte des civils en
   traversant le marché de nuit.
3. **Combat** dans la cour des docks : coups de tentacules, charges, sauts (le cercle rouge au sol annonce
   l'atterrissage). Il s'enfuit par les toits.
4. **Échantillon** : l'analyse du Dr Shen révèle le programme Taotie et le parasite.
5. **Hôtes infectés** : chaque hôte a un compte à rebours. Extraire le parasite prend du temps, neutraliser l'hôte
   est immédiat. Si l'incubation arrive à terme, un rejeton naît et vous attaque.
6. **Rapport** : vos choix (hôtes sauvés ou neutralisés, naissances, civils abattus) sont comptés. Ils serviront
   aux fins du jeu et à l'Épisode 2.

## Organisation du code

```
Assets/Game/
  Scripts/Core      GameBootstrap (construit tout), GameSession (points de contrôle, choix moraux), localisation
  Scripts/Input     Commandes clavier/souris + manette (nouveau Input System, défini en code)
  Scripts/Player    Déplacement, caméra 3e personne, pistolet, interaction avec les hôtes
  Scripts/AI        Civils, hôtes infectés, suspect T-07, rejetons, tentacules
  Scripts/World     Génération de la ville, NavMesh au lancement, matériaux, ambiance (nuit, pluie, néons)
  Scripts/Mission   Scénario de la mission, dialogues radio, marqueurs d'objectifs
  Scripts/UI        Interface (HUD, menus, rapport)
  Scripts/Audio     Sons synthétisés (aucun fichier audio nécessaire)
  Editor            Création automatique de la scène
  Resources/Localization/strings.json   Tous les textes FR/EN (modifiables sans toucher au code)
```

Les graphismes sont des formes simples générées par le code, prévues pour être remplacées par de vrais modèles
(Asset Store, Mixamo…). Aucun asset externe n'est requis.

---

### English (short)

Open the folder with Unity Hub (Unity 6 LTS), accept the Input System backend prompt, then press Play — the
`Mission01` scene is created automatically (menu **Chinese Monsters › Jouer**). The pause menu switches the
language to English. Controls are listed above (keyboard/mouse and gamepad).
