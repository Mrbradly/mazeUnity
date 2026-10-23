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
        [SerializeField] private float patrolSpeed = 1.2f;
        [SerializeField] private float chaseSpeed = 2.1f;
        [SerializeField] private float sightRange = 8f;
        [SerializeField] private float fieldOfView = 120f;
        [SerializeField] private float captureRadius = .7f;
        [SerializeField] private float memorySeconds = 3f;
        [SerializeField] private float startGraceSeconds = 5f;
        private SentinelBrain brain;
        private MazeCell nextCell;
        private SentinelState shownState;
        private float bobTime;
        public SentinelState State { get { return brain == null ? SentinelState.Patrol : brain.State; } }
        public float GraceRemaining { get; private set; }
        public bool PlayerVisible { get; private set; }
        public bool IsAlert { get { return State != SentinelState.Patrol; } }

        private void Awake()
        {
            var layout = new MazeLayout(layoutAsset.text);
            brain = new SentinelBrain(layout, SentinelBrain.ParsePatrol(patrolAsset.text), memorySeconds);
            nextCell = Cell(transform.position);
            GraceRemaining = startGraceSeconds;
            eye.sharedMaterial = patrolMaterial;
        }

        private void Update() { Tick(Time.deltaTime); }

        // Device-independent session clock. Tests or another controller can drive this directly.
        public void Tick(float deltaTime)
        {
            if (!session.IsRunning || session.Progress.IsFinished || deltaTime < 0f) return;
            if (GraceRemaining > 0f)
            {
                GraceRemaining = Mathf.Max(0f, GraceRemaining - deltaTime);
                return;
            }

            Vector3 offset = player.position - transform.position;
            offset.y = 0f;
            float distance = offset.magnitude;
            bool clearSight = distance <= sightRange && ClearSight();
            float dot = distance > .001f ? Vector3.Dot(transform.forward, offset / distance) : 1f;
            PlayerVisible = SentinelBrain.CanDetect(distance, dot, clearSight, IsAlert, sightRange,
                Mathf.Cos(fieldOfView * .5f * Mathf.Deg2Rad));
            brain.Observe(Cell(player.position), PlayerVisible, deltaTime);
            if (shownState != State)
            {
                eye.sharedMaterial = State == SentinelState.Patrol ? patrolMaterial : alertMaterial;
                shownState = State;
            }
            if (SentinelBrain.CanCatch(distance, clearSight, captureRadius))
            {
                session.Catch();
                return;
            }

            // Finish the current edge before taking another. Replanning mid-edge would cut corners.
            Vector3 target = World(nextCell);
            if (Vector3.Distance(transform.position, target) < .001f)
            {
                nextCell = brain.NextStep(nextCell, session.Progress.DoorOpened);
                target = World(nextCell);
            }
            float dt = Mathf.Min(deltaTime, .1f);
            Vector3 direction = target - transform.position;
            if (direction.sqrMagnitude > .00001f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), dt * 8f);
                transform.position = Vector3.MoveTowards(transform.position, target,
                    (State == SentinelState.Patrol ? patrolSpeed : chaseSpeed) * dt);
            }
            else if (State == SentinelState.Search) transform.Rotate(0f, dt * 65f, 0f);
            bobTime += dt;
            visual.localPosition = Vector3.up * (Mathf.Sin(bobTime * 2.2f) * .035f);
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

        private static Vector3 World(MazeCell cell)
        {
            return new Vector3(cell.X * MazeLayout.CellSize, 0f, -cell.Row * MazeLayout.CellSize);
        }
    }
}
