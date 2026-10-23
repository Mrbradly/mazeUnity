using UnityEngine;

namespace EscapeMaze
{
    [DefaultExecutionOrder(100)]
    public sealed class MazeSentinel : MonoBehaviour
    {
        [SerializeField] private MazeSession session;
        [SerializeField] private TextAsset layoutAsset;
        [SerializeField] private TextAsset patrolAsset;
        [SerializeField] private Transform player;
        [SerializeField] private Transform visual;
        [SerializeField] private Renderer eye;
        [SerializeField] private Material patrolMaterial;
        [SerializeField] private Material alertMaterial;
        [SerializeField] private Material stunnedMaterial;
        [SerializeField] private float patrolSpeed = 1.2f;
        [SerializeField] private float chaseSpeed = 2.1f;
        [SerializeField] private float sightRange = 8f;
        [SerializeField] private float fieldOfView = 120f;
        [SerializeField] private float captureRadius = .7f;
        [SerializeField] private float memorySeconds = 3f;
        [SerializeField] private float startGraceSeconds = 5f;
        private SentinelBrain brain;
        private SentinelNavigator navigation;
        private SentinelState shownState;
        private float bobTime;
        public SentinelState State { get { return brain == null ? SentinelState.Patrol : brain.State; } }
        public float GraceRemaining { get; private set; }
        public bool PlayerVisible { get; private set; }
        public bool IsAlert { get { return State == SentinelState.Chase || State == SentinelState.Search; } }
        public bool IsStunned { get { return State == SentinelState.Stunned; } }
        public float StunRemaining { get { return brain == null ? 0f : brain.StunRemaining; } }

        private void Awake()
        {
            var layout = new MazeLayout(layoutAsset.text);
            MazeCell[] route = SentinelBrain.ParsePatrol(patrolAsset.text);
            brain = new SentinelBrain(layout, route, memorySeconds);
            navigation = new SentinelNavigator(brain, route[0]);
            // Use a connected waypoint as the authoritative spawn, even after an import
            // or an Inspector edit. Horizontal navigation never depends on model height.
            transform.position = new Vector3(navigation.X, transform.position.y, navigation.Z);
            GraceRemaining = startGraceSeconds;
            eye.sharedMaterial = patrolMaterial;
        }

        private void Update() { Tick(Time.deltaTime); }

        // Device-independent session clock. Tests or another controller can drive this directly.
        public void Tick(float deltaTime)
        {
            if (!session.IsRunning || session.Progress.IsFinished || deltaTime < 0f ||
                float.IsNaN(deltaTime) || float.IsInfinity(deltaTime)) return;
            bool standby = GraceRemaining > 0f;
            if (standby) GraceRemaining = Mathf.Max(0f, GraceRemaining - deltaTime);
            if (IsStunned)
            {
                PlayerVisible = false;
                brain.Observe(Cell(player.position), false, deltaTime);
                UpdateEye();
                return;
            }
            if (standby) return;

            Vector3 offset = player.position - transform.position;
            offset.y = 0f;
            float distance = offset.magnitude;
            bool clearSight = distance <= sightRange && ClearSight();
            float dot = distance > .001f ? Vector3.Dot(transform.forward, offset / distance) : 1f;
            PlayerVisible = SentinelBrain.CanDetect(distance, dot, clearSight, IsAlert, sightRange,
                Mathf.Cos(fieldOfView * .5f * Mathf.Deg2Rad));
            brain.Observe(Cell(player.position), PlayerVisible, deltaTime);
            UpdateEye();
            if (SentinelBrain.CanCatch(distance, clearSight, captureRadius))
            {
                session.Catch();
                return;
            }

            float dt = Mathf.Min(deltaTime, .1f);
            Vector3 previous = transform.position;
            navigation.Advance(dt, State == SentinelState.Patrol ? patrolSpeed : chaseSpeed, session.Progress.DoorOpened);
            transform.position = new Vector3(navigation.X, previous.y, navigation.Z);
            Vector3 direction = transform.position - previous;
            if (direction.sqrMagnitude > .00001f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), dt * 8f);
            }
            else if (State == SentinelState.Search) transform.Rotate(0f, dt * 65f, 0f);
            bobTime += dt;
            visual.localPosition = Vector3.up * (Mathf.Sin(bobTime * 2.2f) * .035f);
        }

        public bool ApplyStun(float seconds)
        {
            if (!session.IsRunning || session.Progress.IsFinished) return false;
            brain.Stun(seconds);
            PlayerVisible = false;
            UpdateEye();
            return true;
        }

        private void UpdateEye()
        {
            if (shownState == State) return;
            eye.sharedMaterial = IsStunned ? stunnedMaterial : IsAlert ? alertMaterial : patrolMaterial;
            shownState = State;
        }

        private bool ClearSight()
        {
            RaycastHit hit;
            Vector3 from = transform.position + Vector3.up * 1.4f;
            Vector3 to = player.position + Vector3.up;
            // Keys and the Warden itself are triggers. Solid walls/closed door occlude sight.
            if (!Physics.Linecast(from, to, out hit, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                return true;
            return hit.transform == player || hit.transform.IsChildOf(player);
        }

        private static MazeCell Cell(Vector3 position)
        {
            return new MazeCell(Mathf.RoundToInt(position.x / MazeLayout.CellSize),
                Mathf.RoundToInt(-position.z / MazeLayout.CellSize));
        }

    }
}
