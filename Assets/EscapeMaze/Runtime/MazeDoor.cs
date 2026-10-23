using UnityEngine;

namespace EscapeMaze
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class MazeDoor : MazeInteractable
    {
        [SerializeField] private MazeSession session;
        [SerializeField] private Renderer[] indicators;
        [SerializeField] private Material[] keyMaterials;
        private int shownKeys = -1;
        private bool opening;
        private Vector3 openPosition;
        private Vector3 closedPosition;
        private float openAmount;
        public override Color Accent { get { return session.Progress.KeyCount == 3 ? MazePalette.Mint : MazePalette.Keys[0]; } }

        private void Awake() { closedPosition = transform.position; }

        public override string Prompt(MazeSession session)
        {
            return session.Progress.KeyCount == MazeProgress.RequiredKeys
                ? "Release the exit seal"
                : "Seal locked  /  " + session.Progress.KeyCount + " of 3 keys recovered";
        }

        public override void Use(MazeSession session)
        {
            if (session.Progress.DoorOpened) return;
            if (!session.Progress.TryOpenDoor())
            {
                session.Tell("Exit locked. Find all three keys.  " + session.Progress.KeyCount + " / 3");
                return;
            }
            session.Tell("Exit unlocked. Walk through the doorway!");
            GetComponent<BoxCollider>().enabled = false;
            openPosition = transform.position + Vector3.up * 3f;
            opening = true;
        }

        private void Update()
        {
            if (shownKeys != session.Progress.KeyCount)
            {
                for (int i = 0; i < indicators.Length; i++)
                    if (session.Progress.HasKey(i)) indicators[i].sharedMaterial = keyMaterials[i];
                shownKeys = session.Progress.KeyCount;
            }
            if (!opening) return;
            openAmount = Mathf.Min(1f, openAmount + Time.deltaTime / .85f);
            transform.position = Vector3.Lerp(closedPosition, openPosition, Mathf.SmoothStep(0f, 1f, openAmount));
            if (openAmount >= 1f) opening = false;
        }
    }
}
