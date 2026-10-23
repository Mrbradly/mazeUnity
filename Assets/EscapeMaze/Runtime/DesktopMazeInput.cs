using UnityEngine;

namespace EscapeMaze
{
    [DefaultExecutionOrder(-100)]
    public sealed class DesktopMazeInput : MazeInputSource
    {
        [SerializeField] private float mouseSensitivity = 2f;
        [SerializeField] private MazeSession session;
        public override Vector2 Move { get { return move; } }
        public override Vector2 LookDegrees { get { return look; } }
        public override bool UsePressed { get { return usePressed; } }
        public override bool IsActive { get { return captured && Application.isFocused; } }
        public bool MapVisible { get; private set; }
        private bool captured;
        private Vector2 move;
        private Vector2 look;
        private bool usePressed;

        private void Update()
        {
            move = Vector2.zero;
            look = Vector2.zero;
            usePressed = false;
            if (session.Progress.IsFinished) { if (captured) Release(); return; }
            if (Input.GetKeyDown(KeyCode.Escape)) { Release(); return; }
            if (!captured)
            {
                if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Return)) Capture();
                return;
            }
            if (!IsActive) return;
            if (Input.GetKeyDown(KeyCode.M)) MapVisible = !MapVisible;
            float x = (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ? 1 : 0)
                    - (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) ? 1 : 0);
            float y = (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) ? 1 : 0)
                    - (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) ? 1 : 0);
            move = Vector2.ClampMagnitude(new Vector2(x, y), 1f);
            look = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * mouseSensitivity;
            usePressed = Input.GetKeyDown(KeyCode.E) || Input.GetMouseButtonDown(0);
        }

        public void Capture()
        {
            captured = true;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        public void Release()
        {
            captured = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void OnApplicationFocus(bool focused) { if (!focused) Release(); }
        private void OnDisable() { Release(); }
    }
}
