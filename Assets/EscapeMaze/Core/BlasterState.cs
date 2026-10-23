using System;

namespace EscapeMaze
{
    // Only active play advances the cooldown. Input and physics live in adapters.
    public sealed class BlasterState
    {
        private readonly float interval;
        public float CooldownRemaining { get; private set; }
        public int ShotsFired { get; private set; }
        public bool Ready { get { return CooldownRemaining <= 0f; } }

        public BlasterState(float interval = .4f)
        {
            if (interval <= 0f || float.IsNaN(interval) || float.IsInfinity(interval))
                throw new ArgumentOutOfRangeException(nameof(interval));
            this.interval = interval;
        }

        public void Tick(float deltaTime, bool running)
        {
            if (deltaTime < 0f || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime))
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            if (running) CooldownRemaining = Math.Max(0f, CooldownRemaining - deltaTime);
        }

        public bool TryFire(bool running)
        {
            if (!running || !Ready) return false;
            ShotsFired++;
            CooldownRemaining = interval;
            return true;
        }
    }
}
