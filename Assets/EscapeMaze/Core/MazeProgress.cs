using System;

namespace EscapeMaze
{
    // No Unity or device dependencies: the same rules work with keyboard or gaze input.
    public sealed class MazeProgress
    {
        public const int RequiredKeys = 3;
        private readonly bool[] keys = new bool[RequiredKeys];
        public int KeyCount { get; private set; }
        public bool DoorOpened { get; private set; }
        public bool HasWon { get; private set; }
        public bool WasCaught { get; private set; }
        public bool IsFinished { get { return HasWon || WasCaught; } }

        public bool HasKey(int id)
        {
            ValidateId(id);
            return keys[id];
        }

        public bool CollectKey(int id)
        {
            ValidateId(id);
            if (keys[id] || IsFinished) return false;
            keys[id] = true;
            KeyCount++;
            return true;
        }

        public bool TryOpenDoor()
        {
            if (IsFinished || KeyCount != RequiredKeys || DoorOpened) return false;
            DoorOpened = true;
            return true;
        }

        public bool TryComplete()
        {
            if (!DoorOpened || KeyCount != RequiredKeys || IsFinished) return false;
            HasWon = true;
            return true;
        }

        public bool TryCatch()
        {
            if (IsFinished) return false;
            WasCaught = true;
            return true;
        }

        private static void ValidateId(int id)
        {
            if (id < 0 || id >= RequiredKeys)
                throw new ArgumentOutOfRangeException(nameof(id));
        }
    }
}
