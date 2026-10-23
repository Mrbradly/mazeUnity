using System.Collections.Generic;

namespace EscapeMaze
{
    // Discovery records local knowledge rather than revealing the solution.
    public sealed class MazeExploration
    {
        private readonly MazeLayout layout;
        private readonly HashSet<MazeCell> discovered = new HashSet<MazeCell>();
        private readonly HashSet<MazeCell> visited = new HashSet<MazeCell>();
        public int VisitedCount { get { return visited.Count; } }

        public MazeExploration(MazeLayout layout) { this.layout = layout; }
        public bool IsDiscovered(MazeCell cell) { return discovered.Contains(cell); }
        public bool IsVisited(MazeCell cell) { return visited.Contains(cell); }

        public void Visit(MazeCell cell)
        {
            if (!layout.IsWalkable(cell, true)) return;
            visited.Add(cell);
            Reveal(cell);
            Reveal(new MazeCell(cell.X + 1, cell.Row));
            Reveal(new MazeCell(cell.X - 1, cell.Row));
            Reveal(new MazeCell(cell.X, cell.Row + 1));
            Reveal(new MazeCell(cell.X, cell.Row - 1));
        }

        private void Reveal(MazeCell cell)
        {
            if (cell.X >= 0 && cell.Row >= 0 && cell.X < layout.Width && cell.Row < layout.Height)
                discovered.Add(cell);
        }
    }
}
