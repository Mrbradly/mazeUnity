using UnityEngine;

namespace EscapeMaze
{
    public sealed class MazeExitZone : MonoBehaviour
    {
        [SerializeField] private MazeSession session;
        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<MazePlayerMotor>() != null) session.Complete();
        }
    }
}
