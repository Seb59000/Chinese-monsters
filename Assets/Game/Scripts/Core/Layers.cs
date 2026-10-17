using UnityEngine;

namespace Game
{
    /// <summary>
    /// Uses Unity's built-in layers only, so the project needs no custom TagManager setup.
    /// Default = world geometry, TransparentFX = NPC hit capsules, Ignore Raycast = player.
    /// </summary>
    public static class Layers
    {
        public const int World = 0;
        public const int Npc = 1;
        public const int Player = 2;

        public static readonly int WorldMask = 1 << World;
        public static readonly int ShootMask = (1 << World) | (1 << Npc);
        public static readonly int CameraMask = 1 << World;
        public static readonly int CharacterMask = (1 << Npc) | (1 << Player);

        public static void SetRecursive(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform child in go.transform)
                SetRecursive(child.gameObject, layer);
        }
    }
}
