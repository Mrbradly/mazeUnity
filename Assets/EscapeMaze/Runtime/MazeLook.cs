using UnityEngine;

namespace EscapeMaze
{
    public sealed class MazeLook : MonoBehaviour
    {
        [SerializeField] private Transform eye;
        private float pitch;

        public void Apply(Vector2 degrees)
        {
            transform.Rotate(0f, degrees.x, 0f, Space.World);
            pitch = Mathf.Clamp(pitch - degrees.y, -80f, 80f);
            eye.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }
    }
}
