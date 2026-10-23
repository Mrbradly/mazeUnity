using UnityEngine;

namespace EscapeMaze
{
    // This root contains only immovable decor, not keys, the player, or the animated door.
    public sealed class MazeStaticGeometry : MonoBehaviour
    {
        private void Start() { StaticBatchingUtility.Combine(gameObject); }
    }
}
