using System;
using UnityEngine;

namespace EscapeMaze
{
    // Hitscan adapter: another input device can call Fire with its own aim ray.
    // Wardens are stunned rather than destroyed; the escape objective stays intact.
    public sealed class MazeBlaster : MonoBehaviour
    {
        [SerializeField] private MazeSession session;
        [SerializeField] private Transform muzzle;
        [SerializeField] private Transform tracer;
        [SerializeField] private Renderer tracerRenderer;
        [SerializeField] private float range = 18f;
        [SerializeField] private float shotInterval = .4f;
        [SerializeField] private float stunSeconds = 4f;
        [SerializeField] private float tracerSeconds = .08f;
        private BlasterState state;
        private float tracerRemaining;
        public bool Ready { get { return state != null && state.Ready; } }
        public int ShotsFired { get { return state == null ? 0 : state.ShotsFired; } }
        public bool LastShotHit { get; private set; }

        private void Awake()
        {
            state = new BlasterState(shotInterval);
            tracerRenderer.enabled = false;
        }

        public void Tick(float deltaTime)
        {
            state.Tick(deltaTime, session.IsRunning);
            if (!session.IsRunning) { tracerRenderer.enabled = false; return; }
            tracerRemaining = Mathf.Max(0f, tracerRemaining - deltaTime);
            tracerRenderer.enabled = tracerRemaining > 0f;
        }

        public bool Fire(Ray aim)
        {
            if (aim.direction.sqrMagnitude < .0001f || !state.TryFire(session.IsRunning)) return false;
            aim.direction = aim.direction.normalized;
            LastShotHit = false;
            Vector3 end = aim.GetPoint(range);
            // Include Warden trigger volumes, ignore key/exit triggers, and respect
            // the first solid wall or closed door. No projectiles or physics bodies.
            Physics.SyncTransforms();
            RaycastHit[] hits = Physics.RaycastAll(aim, range, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
            Array.Sort(hits, CompareHits);
            foreach (RaycastHit hit in hits)
            {
                if (hit.transform == transform || hit.transform.IsChildOf(transform)) continue;
                MazeSentinel warden = hit.collider.GetComponentInParent<MazeSentinel>();
                if (warden == null && hit.collider.isTrigger) continue;
                end = hit.point;
                if (warden != null) LastShotHit = warden.ApplyStun(stunSeconds);
                break;
            }
            Vector3 beam = end - muzzle.position;
            tracer.position = muzzle.position + beam * .5f;
            if (beam.sqrMagnitude > .0001f) tracer.rotation = Quaternion.LookRotation(beam);
            tracer.localScale = new Vector3(.025f, .025f, Mathf.Max(.001f, beam.magnitude));
            tracerRemaining = tracerSeconds;
            tracerRenderer.enabled = true;
            if (LastShotHit) session.Tell("Warden stunned for " + stunSeconds.ToString("0.#") + " seconds. Keep moving.");
            return true;
        }

        private static int CompareHits(RaycastHit a, RaycastHit b) { return a.distance.CompareTo(b.distance); }
        private void OnDisable() { if (tracerRenderer != null) tracerRenderer.enabled = false; }
    }
}
