using UnityEngine;

namespace EscapeMaze
{
    public sealed class MazeInteractor : MonoBehaviour
    {
        [SerializeField] private MazeSession session;
        [SerializeField] private float reach = 2.8f;
        public MazeInteractable Focus { get; private set; }
        public string Prompt { get { return Focus == null ? "" : Focus.Prompt(session); } }

        // The caller supplies both ray and activation. A gaze/dwell adapter can call this directly.
        public void Tick(Ray ray, bool activate)
        {
            ClearFocus();
            if (session.Progress.IsFinished) return;
            RaycastHit hit;
            // Include key triggers. Solid walls occlude the ray before an object behind them.
            if (Physics.Raycast(ray, out hit, reach, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide))
                Focus = hit.collider.GetComponentInParent<MazeInteractable>();
            if (Focus != null) Focus.SetFocused(true);
            if (Focus != null && activate)
            {
                Focus.Use(session);
                if (!Focus.gameObject.activeInHierarchy) ClearFocus();
            }
        }

        public void ClearFocus()
        {
            if (Focus != null) Focus.SetFocused(false);
            Focus = null;
        }
    }
}
