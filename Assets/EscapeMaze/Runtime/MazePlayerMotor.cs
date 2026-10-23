using UnityEngine;

namespace EscapeMaze
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class MazePlayerMotor : MonoBehaviour
    {
        [SerializeField] private float speed = 3f;
        private CharacterController controller;
        private float verticalSpeed;

        // Call from any locomotion adapter. Movement is relative to this body's yaw.
        public void Tick(Vector2 input, float deltaTime)
        {
            if (controller == null) controller = GetComponent<CharacterController>();
            if (controller.isGrounded && verticalSpeed < 0f) verticalSpeed = -2f;
            verticalSpeed = Mathf.Max(verticalSpeed - 20f * deltaTime, -20f);
            Vector2 limited = Vector2.ClampMagnitude(input, 1f);
            Vector3 horizontal = transform.right * limited.x + transform.forward * limited.y;
            controller.Move((horizontal * speed + Vector3.up * verticalSpeed) * deltaTime);
        }
    }
}
