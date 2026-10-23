using System;

namespace EscapeMaze
{
    // Continuous, device-independent movement. Decisions happen only at cell centres,
    // so chasing and resuming after a stun cannot take diagonal shortcuts through walls.
    public sealed class SentinelNavigator
    {
        private readonly SentinelBrain brain;
        private MazeCell current, target;
        public float X { get; private set; }
        public float Z { get; private set; }

        public SentinelNavigator(SentinelBrain brain, MazeCell spawn)
        {
            this.brain = brain ?? throw new ArgumentNullException(nameof(brain));
            current = target = spawn;
            X = spawn.X * MazeLayout.CellSize;
            Z = -spawn.Row * MazeLayout.CellSize;
        }

        public void Advance(float deltaTime, float speed, bool doorOpen)
        {
            if (deltaTime < 0f || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime))
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            if (speed < 0f || float.IsNaN(speed) || float.IsInfinity(speed))
                throw new ArgumentOutOfRangeException(nameof(speed));
            if (brain.State == SentinelState.Stunned) return;
            float distanceLeft = speed * Math.Min(deltaTime, .1f);
            int edges = 0;
            while (distanceLeft > 0f && edges++ < 64)
            {
                if (current.Equals(target)) target = brain.NextStep(current, doorOpen);
                if (current.Equals(target)) break;
                float dx = target.X * MazeLayout.CellSize - X;
                float dz = -target.Row * MazeLayout.CellSize - Z;
                float distance = (float)Math.Sqrt(dx * dx + dz * dz);
                float step = Math.Min(distance, distanceLeft);
                if (distance <= distanceLeft)
                {
                    X = target.X * MazeLayout.CellSize;
                    Z = -target.Row * MazeLayout.CellSize;
                    current = target;
                }
                else { X += dx / distance * step; Z += dz / distance * step; }
                distanceLeft -= step;
            }
        }
    }
}
