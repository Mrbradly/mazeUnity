using System;
using System.Collections.Generic;

namespace EscapeMaze
{
    public struct MazeCell : IEquatable<MazeCell>
    {
        public readonly int X;
        public readonly int Row;
        public MazeCell(int x, int row) { X = x; Row = row; }
        public bool Equals(MazeCell other) { return X == other.X && Row == other.Row; }
        public override bool Equals(object obj) { return obj is MazeCell && Equals((MazeCell)obj); }
        public override int GetHashCode() { return (X * 397) ^ Row; }
        public override string ToString() { return X + "," + Row; }
    }

    // Source of truth: Data/MazeLayout.txt. World position = (X * CellSize, 0, -Row * CellSize).
    public sealed class MazeLayout
    {
        public const float CellSize = 2.5f;
        private readonly string[] rows;
        public int Width { get { return rows[0].Length; } }
        public int Height { get { return rows.Length; } }

        public MazeLayout(string text)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));
            rows = text.Replace("\r", "").TrimEnd('\n').Split('\n');
            if (rows.Length < 3 || rows[0].Length < 3)
                throw new ArgumentException("Maze is too small.");
            foreach (string row in rows)
            {
                if (row.Length != Width) throw new ArgumentException("Maze must be rectangular.");
                foreach (char cell in row)
                    if ("#.SabcDE".IndexOf(cell) < 0)
                        throw new ArgumentException("Unknown cell: " + cell);
            }
            foreach (char marker in "SabcDE")
            {
                int count = 0;
                foreach (string row in rows) foreach (char cell in row) if (cell == marker) count++;
                if (count != 1) throw new ArgumentException("Expected one " + marker);
            }
            for (int x = 0; x < Width; x++)
                if (rows[0][x] != '#' || rows[Height - 1][x] != '#')
                    throw new ArgumentException("Maze must be enclosed.");
            foreach (string row in rows)
                if (row[0] != '#' || row[Width - 1] != '#')
                    throw new ArgumentException("Maze must be enclosed.");
        }

        public MazeCell Find(char marker)
        {
            for (int row = 0; row < Height; row++)
                for (int x = 0; x < Width; x++)
                    if (rows[row][x] == marker) return new MazeCell(x, row);
            throw new ArgumentException("Missing marker: " + marker);
        }

        public char At(MazeCell cell) { return rows[cell.Row][cell.X]; }

        public bool IsWalkable(MazeCell cell, bool doorOpen)
        {
            if (cell.X < 0 || cell.Row < 0 || cell.X >= Width || cell.Row >= Height) return false;
            char value = At(cell);
            return value != '#' && (value != 'D' || doorOpen);
        }

        public List<MazeCell> FindPath(MazeCell start, MazeCell end, bool doorOpen)
        {
            if (!IsWalkable(start, doorOpen) || !IsWalkable(end, doorOpen)) return null;
            var queue = new Queue<MazeCell>();
            var previous = new Dictionary<MazeCell, MazeCell>();
            queue.Enqueue(start);
            previous[start] = start;
            int[] dx = { 1, -1, 0, 0 }, dy = { 0, 0, 1, -1 };
            while (queue.Count > 0)
            {
                MazeCell current = queue.Dequeue();
                if (current.Equals(end))
                {
                    var result = new List<MazeCell> { end };
                    while (!current.Equals(start)) { current = previous[current]; result.Add(current); }
                    result.Reverse();
                    return result;
                }
                for (int i = 0; i < 4; i++)
                {
                    var next = new MazeCell(current.X + dx[i], current.Row + dy[i]);
                    if (!IsWalkable(next, doorOpen) || previous.ContainsKey(next)) continue;
                    previous[next] = current;
                    queue.Enqueue(next);
                }
            }
            return null;
        }
    }
}
