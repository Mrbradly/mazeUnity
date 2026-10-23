using UnityEngine;
using UnityEngine.SceneManagement;

namespace EscapeMaze
{
    public sealed class MazeSession : MonoBehaviour
    {
        public MazeProgress Progress { get; private set; } = new MazeProgress();
        public string Message { get; private set; } = "Find the three keys, then unlock the exit.";
        public float ElapsedSeconds { get; private set; }
        public bool IsRunning { get; private set; }
        private float messageChangedAt;
        public float MessageAge { get { return Time.unscaledTime - messageChangedAt; } }

        private void Awake() { messageChangedAt = Time.unscaledTime; }

        public void TickTime(float deltaTime)
        {
            if (IsRunning) ElapsedSeconds += Mathf.Max(0f, deltaTime);
        }

        public void SetRunning(bool running) { IsRunning = running && !Progress.IsFinished; }

        public void Catch()
        {
            if (!Progress.TryCatch()) return;
            IsRunning = false;
            Tell("The Warden found you. Break its line of sight at corners, then try again.");
        }

        public void Tell(string message) { Message = message; messageChangedAt = Time.unscaledTime; }

        public bool CollectKey(int id)
        {
            if (!Progress.CollectKey(id)) return false;
            Tell("SIGNAL " + (id + 1).ToString("00") + " RECOVERED   /   " + MazePalette.KeyNames[id] +
                (Progress.KeyCount == 3 ? "   —   Return to the exit" : "   —   " + Progress.KeyCount + " of 3"));
            return true;
        }

        public void Complete()
        {
            if (!Progress.TryComplete()) return;
            IsRunning = false;
            Tell("You escaped!");
        }

        public void Restart()
        {
            SceneManager.LoadScene(gameObject.scene.path);
        }
    }
}
