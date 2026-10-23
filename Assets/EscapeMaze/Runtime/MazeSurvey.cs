using UnityEngine;

namespace EscapeMaze
{
    public sealed class MazeSurvey : MonoBehaviour
    {
        [SerializeField] private TextAsset layoutAsset;
        [SerializeField] private Transform player;
        public MazeLayout Layout { get; private set; }
        public MazeExploration Exploration { get; private set; }
        public MazeCell PlayerCell { get; private set; }
        public float Heading { get { return player.eulerAngles.y; } }

        private void Awake()
        {
            Layout = new MazeLayout(layoutAsset.text);
            Exploration = new MazeExploration(Layout);
            Discover();
        }

        private void LateUpdate() { Discover(); }

        private void Discover()
        {
            PlayerCell = new MazeCell(Mathf.RoundToInt(player.position.x / MazeLayout.CellSize),
                Mathf.RoundToInt(-player.position.z / MazeLayout.CellSize));
            Exploration.Visit(PlayerCell);
        }
    }
}
