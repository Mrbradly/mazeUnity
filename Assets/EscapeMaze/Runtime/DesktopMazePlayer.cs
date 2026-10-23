using UnityEngine;

namespace EscapeMaze
{
    public sealed class DesktopMazePlayer : MonoBehaviour
    {
        [SerializeField] private MazeInputSource input;
        [SerializeField] private MazePlayerMotor motor;
        [SerializeField] private MazeLook look;
        [SerializeField] private MazeInteractor interactor;
        [SerializeField] private Transform eye;
        [SerializeField] private MazeSession session;

        private void Update()
        {
            session.SetRunning(input.IsActive);
            if (!session.IsRunning) { interactor.ClearFocus(); return; }
            session.TickTime(Time.deltaTime);
            look.Apply(input.LookDegrees);
            motor.Tick(input.Move, Time.deltaTime);
            interactor.Tick(new Ray(eye.position, eye.forward), input.UsePressed);
        }

        private void OnDisable() { if (session != null) session.SetRunning(false); }
    }
}
