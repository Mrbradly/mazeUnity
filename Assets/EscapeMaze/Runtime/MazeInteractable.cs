using UnityEngine;

namespace EscapeMaze
{
    public abstract class MazeInteractable : MonoBehaviour
    {
        public bool IsFocused { get; private set; }
        public virtual Color Accent { get { return MazePalette.Mint; } }
        public void SetFocused(bool value) { IsFocused = value; }
        public abstract string Prompt(MazeSession session);
        public abstract void Use(MazeSession session);
    }
}
