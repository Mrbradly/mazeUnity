using System;
using System.Collections.Generic;
using System.Globalization;

namespace EscapeMaze
{
    public enum SentinelState { Patrol, Chase, Search, Stunned }

    // Pure decision logic. Unity supplies visibility and moves one grid edge at a time.
    public sealed class SentinelBrain
    {
        private readonly MazeLayout layout;
        private readonly MazeCell[] patrol;
        private readonly float memorySeconds;
        private int patrolIndex;
        private float memoryRemaining;
        public SentinelState State { get; private set; }
        public MazeCell? LastSeenPlayer { get; private set; }
        public float StunRemaining { get; private set; }

        public void Stun(float seconds)
        {
            if (seconds <= 0f || float.IsNaN(seconds) || float.IsInfinity(seconds))
                throw new ArgumentOutOfRangeException(nameof(seconds));
            StunRemaining = Math.Max(StunRemaining, seconds);
            LastSeenPlayer = null;
            memoryRemaining = 0f;
            State = SentinelState.Stunned;
        }

        public SentinelBrain(MazeLayout layout, MazeCell[] patrol, float memorySeconds = 3f)
        {
            if (layout == null) throw new ArgumentNullException(nameof(layout));
            if (patrol == null || patrol.Length < 2) throw new ArgumentException("Patrol needs at least two waypoints.");
            if (memorySeconds <= 0f || float.IsNaN(memorySeconds) || float.IsInfinity(memorySeconds))
                throw new ArgumentOutOfRangeException(nameof(memorySeconds));
            this.layout = layout;
            this.patrol = (MazeCell[])patrol.Clone();
            this.memorySeconds = memorySeconds;
            foreach (MazeCell point in patrol)
                if (!layout.IsWalkable(point, false) || layout.FindPath(patrol[0], point, false) == null)
                    throw new ArgumentException("Patrol waypoints must be connected with the door locked.");
        }

        public void Observe(MazeCell player, bool visible, float deltaTime)
        {
            if (deltaTime < 0f || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime))
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            if (State == SentinelState.Stunned)
            {
                StunRemaining = Math.Max(0f, StunRemaining - deltaTime);
                if (StunRemaining == 0f) State = SentinelState.Patrol;
                return;
            }
            if (visible && layout.IsWalkable(player, true))
            {
                LastSeenPlayer = player;
                memoryRemaining = memorySeconds;
                State = SentinelState.Chase;
            }
            else if (State != SentinelState.Patrol)
            {
                memoryRemaining = Math.Max(0f, memoryRemaining - deltaTime);
                State = memoryRemaining > 0f ? SentinelState.Search : SentinelState.Patrol;
                if (State == SentinelState.Patrol) LastSeenPlayer = null;
            }
        }

        public MazeCell NextStep(MazeCell current, bool doorOpen)
        {
            if (State == SentinelState.Stunned || !layout.IsWalkable(current, doorOpen)) return current;
            MazeCell target;
            if (State != SentinelState.Patrol && LastSeenPlayer.HasValue) target = LastSeenPlayer.Value;
            else
            {
                if (current.Equals(patrol[patrolIndex])) patrolIndex = (patrolIndex + 1) % patrol.Length;
                target = patrol[patrolIndex];
            }
            List<MazeCell> path = layout.FindPath(current, target, doorOpen);
            return path != null && path.Count > 1 ? path[1] : current;
        }

        public static bool CanDetect(float distance, float forwardDot, bool clearSight, bool alerted,
            float range, float minimumDot)
        {
            return distance >= 0f && distance <= range && clearSight && (alerted || forwardDot >= minimumDot);
        }

        public static bool CanCatch(float distance, bool clearSight, float captureRadius)
        {
            return distance >= 0f && distance <= captureRadius && clearSight;
        }

        public static MazeCell[] ParsePatrol(string text)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));
            string[] lines = text.Replace("\r", "").Trim().Split('\n');
            var result = new MazeCell[lines.Length];
            for (int i = 0; i < lines.Length; i++)
            {
                string[] parts = lines[i].Split(',');
                int x, row;
                if (parts.Length != 2 || !int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out x)
                    || !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out row))
                    throw new ArgumentException("Expected x,row on each patrol line.");
                result[i] = new MazeCell(x, row);
            }
            return result;
        }
    }
}
