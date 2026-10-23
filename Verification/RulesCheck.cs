using System;
using System.Collections.Generic;
using System.IO;
using EscapeMaze;

internal static class RulesCheck
{
    private static int checks;
    private static void Check(bool condition, string description)
    {
        checks++;
        if (!condition) throw new Exception("FAIL: " + description);
    }

    public static void Main(string[] args)
    {
        var layout = new MazeLayout(File.ReadAllText(args[0]));
        MazeCell start = layout.Find('S'), exit = layout.Find('E');
        foreach (char key in "abc")
            Check(layout.FindPath(start, layout.Find(key), false) != null, key + " reachable with door locked");
        Check(layout.FindPath(start, exit, false) == null, "No route bypasses the locked door");
        Check(layout.FindPath(start, exit, true) != null, "Exit reachable with door open");

        int[][] orders = { new[] { 0, 1, 2 }, new[] { 0, 2, 1 }, new[] { 1, 0, 2 },
                           new[] { 1, 2, 0 }, new[] { 2, 0, 1 }, new[] { 2, 1, 0 } };
        foreach (int[] order in orders)
        {
            var progress = new MazeProgress();
            var current = start;
            int steps = 0;
            Check(!progress.TryComplete(), "Cannot win immediately");
            for (int i = 0; i < 3; i++)
            {
                Check(!progress.TryOpenDoor(), "Door rejects " + i + " keys");
                List<MazeCell> path = layout.FindPath(current, layout.Find((char)('a' + order[i])), false);
                Check(path != null, "Walk to next key");
                ValidatePath(layout, path, false);
                steps += path.Count - 1;
                current = path[path.Count - 1];
                Check(progress.CollectKey(order[i]), "Collect a distinct key");
                Check(!progress.CollectKey(order[i]), "Duplicate ignored");
                Check(progress.KeyCount == i + 1, "Key count correct");
                Check(progress.HasKey(order[i]), "Key identity retained");
                Check(!progress.TryComplete(), "Exit cannot win before unlocking");
            }
            Check(progress.TryOpenDoor(), "All keys unlock door");
            Check(!progress.TryOpenDoor(), "Door opening is idempotent");
            Check(!progress.HasWon, "Opening door alone does not win");
            List<MazeCell> finish = layout.FindPath(current, exit, true);
            Check(finish != null, "Walk from final key to exit");
            ValidatePath(layout, finish, true);
            Check(progress.TryComplete(), "Exit completes game");
            Check(progress.HasWon, "Victory stored");
            Check(!progress.TryComplete(), "Victory occurs only once");
            Console.WriteLine("PASS complete maze route " + string.Join("", order) + " (" + (steps + finish.Count - 1) + " grid steps)");
        }

        foreach (int id in new[] { -1, 3, int.MaxValue })
        {
            bool rejected = false;
            try { new MazeProgress().CollectKey(id); } catch (ArgumentOutOfRangeException) { rejected = true; }
            Check(rejected, "Invalid key ID rejected");
        }
        var fresh = new MazeProgress();
        Check(fresh.KeyCount == 0 && !fresh.DoorOpened && !fresh.HasWon, "Fresh session starts clean");
        foreach (string malformed in new[] { "", "###\n#S#\n###", "###\n#S\n###" })
        {
            bool rejected = false;
            try { new MazeLayout(malformed); } catch (ArgumentException) { rejected = true; }
            Check(rejected, "Malformed layout rejected");
        }
        var exploration = new MazeExploration(layout);
        Check(exploration.VisitedCount == 0, "Survey begins empty");
        exploration.Visit(start);
        Check(exploration.VisitedCount == 1 && exploration.IsVisited(start), "Starting cell recorded");
        Check(exploration.IsDiscovered(start), "Starting cell visible");
        foreach (char marker in "abcDE")
            Check(!exploration.IsDiscovered(layout.Find(marker)), "Survey does not leak distant objective " + marker);
        exploration.Visit(start);
        Check(exploration.VisitedCount == 1, "Revisits do not inflate discovery count");
        exploration.Visit(new MazeCell(0, 0));
        exploration.Visit(new MazeCell(-1, -1));
        Check(exploration.VisitedCount == 1, "Walls and out-of-bounds cells are not visited");
        Check(!exploration.IsDiscovered(new MazeCell(0, 0)), "A wall visit cannot reveal remote cells");
        for (int row = 0; row < layout.Height; row++)
            for (int x = 0; x < layout.Width; x++)
            {
                var cell = new MazeCell(x, row);
                bool local = Math.Abs(cell.X - start.X) + Math.Abs(cell.Row - start.Row) <= 1;
                Check(exploration.IsDiscovered(cell) == local, "Only current cell and four neighbors initially revealed");
            }
        foreach (MazeCell cell in layout.FindPath(start, layout.Find('a'), false)) exploration.Visit(cell);
        Check(exploration.IsDiscovered(layout.Find('a')), "Walking to a key reveals it");
        Check(!exploration.IsDiscovered(layout.Find('c')), "Remote alcove stays hidden");
        Check(new MazeExploration(layout).VisitedCount == 0, "Replay starts with a fresh survey");
        CheckSentinel(layout, Path.Combine(Path.GetDirectoryName(args[0]), "SentinelPatrol.txt"));
        Console.WriteLine("PASS " + checks + " assertions (pure C# rules, navigation, discovery, and sentinel decisions; not Unity physics)");
    }

    private static void CheckSentinel(MazeLayout layout, string patrolPath)
    {
        var caught = new MazeProgress();
        caught.CollectKey(0);
        Check(caught.TryCatch(), "Capture ends a live run");
        Check(caught.WasCaught && caught.IsFinished && !caught.HasWon, "Capture is distinct from victory");
        Check(caught.KeyCount == 1, "Caught screen can show recovered key count");
        Check(!caught.TryCatch() && !caught.CollectKey(1) && !caught.TryOpenDoor() && !caught.TryComplete(),
            "A captured run cannot progress or finish twice");
        var unlocked = new MazeProgress();
        for (int i = 0; i < 3; i++) unlocked.CollectKey(i);
        unlocked.TryOpenDoor();
        Check(unlocked.TryCatch() && !unlocked.TryComplete(), "Capture before entering exit prevents victory");
        var winner = new MazeProgress();
        for (int i = 0; i < 3; i++) winner.CollectKey(i);
        winner.TryOpenDoor();
        winner.TryComplete();
        Check(!winner.TryCatch() && !winner.WasCaught && winner.HasWon, "Victory cannot turn into capture");
        Check(!new MazeProgress().IsFinished && !new MazeProgress().WasCaught, "Restart clears loss state");

        Check(SentinelBrain.CanDetect(8f, .5f, true, false, 8f, .5f), "Sight range and cone boundary included");
        Check(!SentinelBrain.CanDetect(8.01f, 1f, true, false, 8f, .5f), "Out of range is invisible");
        Check(!SentinelBrain.CanDetect(3f, -1f, true, false, 8f, .5f), "Idle sentinel cannot see behind itself");
        Check(SentinelBrain.CanDetect(3f, -1f, true, true, 8f, .5f), "Alert sentinel tracks visible player behind it");
        Check(!SentinelBrain.CanDetect(3f, 1f, false, true, 8f, .5f), "Walls stop even an alerted sentinel's sight");
        Check(!SentinelBrain.CanCatch(.4f, false, .7f), "Cannot catch through walls");
        Check(SentinelBrain.CanCatch(.7f, true, .7f), "Visible contact catches at boundary");
        Check(!SentinelBrain.CanCatch(.71f, true, .7f), "Cannot catch beyond contact range");
        Check(!SentinelBrain.CanCatch(-1f, true, .7f), "Negative distances rejected");

        MazeCell[] route = SentinelBrain.ParsePatrol(File.ReadAllText(patrolPath));
        Check(route.Length == 4 && !route[0].Equals(layout.Find('S')), "Four-point patrol starts away from player");
        var brain = new SentinelBrain(layout, route);
        MazeCell current = route[0];
        var reached = new HashSet<MazeCell> { current };
        for (int i = 0; i < 400; i++)
        {
            brain.Observe(layout.Find('S'), false, .1f);
            MazeCell next = brain.NextStep(current, false);
            Check(layout.IsWalkable(next, false), "Patrol stays in unlocked corridor space");
            Check(Math.Abs(next.X - current.X) + Math.Abs(next.Row - current.Row) == 1, "Patrol advances one edge without cutting corners");
            Check(layout.At(next) != 'D' && layout.At(next) != 'E', "Patrol never crosses locked exit");
            Check(brain.State == SentinelState.Patrol, "Unseen player does not trigger chase");
            current = next;
            reached.Add(current);
        }
        foreach (MazeCell point in route) Check(reached.Contains(point), "Patrol reaches waypoint " + point);

        MazeCell seen = layout.Find('a');
        brain.Observe(seen, true, .1f);
        Check(brain.State == SentinelState.Chase && brain.LastSeenPlayer.Value.Equals(seen), "Sight acquires a chase target");
        brain.Observe(layout.Find('c'), false, 1f);
        Check(brain.State == SentinelState.Search && brain.LastSeenPlayer.Value.Equals(seen), "Search uses last sighting, not hidden player position");
        brain.Observe(layout.Find('c'), false, 2.1f);
        Check(brain.State == SentinelState.Patrol && !brain.LastSeenPlayer.HasValue, "Memory expires and patrol resumes");
        brain.Observe(layout.Find('b'), true, 0f);
        Check(brain.State == SentinelState.Chase && brain.LastSeenPlayer.Value.Equals(layout.Find('b')), "Player can be reacquired after escaping");
        brain.Observe(layout.Find('E'), true, .1f);
        var approach = new MazeCell(layout.Find('D').X - 1, layout.Find('D').Row);
        Check(brain.NextStep(approach, false).Equals(approach), "Chase cannot route through locked door");
        Check(brain.NextStep(approach, true).Equals(layout.Find('D')), "Chase respects newly opened passage");

        foreach (string malformed in new[] { "", "7;5", "a,5", "7,5,9" })
        {
            bool rejected = false;
            try { SentinelBrain.ParsePatrol(malformed); } catch (ArgumentException) { rejected = true; }
            Check(rejected, "Malformed patrol data rejected");
        }
        bool badPatrol = false;
        try { new SentinelBrain(layout, new[] { new MazeCell(0, 0), route[0] }); }
        catch (ArgumentException) { badPatrol = true; }
        Check(badPatrol, "Patrol cannot start in a wall");
        Console.WriteLine("PASS sentinel patrol soak, sight/cone/capture gates, chase memory, door routing, and loss/win invariants");
    }

    private static void ValidatePath(MazeLayout layout, List<MazeCell> path, bool doorOpen)
    {
        for (int i = 0; i < path.Count; i++)
        {
            Check(layout.IsWalkable(path[i], doorOpen), "Route avoids walls / closed door");
            if (i > 0) Check(Math.Abs(path[i].X - path[i - 1].X) + Math.Abs(path[i].Row - path[i - 1].Row) == 1,
                "Route steps are adjacent");
        }
    }
}
