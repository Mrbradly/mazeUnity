using UnityEngine;

namespace EscapeMaze
{
    // A future input adapter can supply these values without changing the motor or rules.
    public abstract class MazeInputSource : MonoBehaviour
    {
        public abstract Vector2 Move { get; }
        public abstract Vector2 LookDegrees { get; }
        public abstract bool UsePressed { get; }
        public abstract bool IsActive { get; }
    }
}
