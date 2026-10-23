using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace EscapeMaze.Tests
{
    public sealed class SentinelRulesTests
    {
        [Test]
        public void WardenSearchesLastKnownPositionThenResumesPatrol()
        {
            var layout = new MazeLayout(File.ReadAllText(Path.Combine(Application.dataPath, "EscapeMaze/Data/MazeLayout.txt")));
            var route = SentinelBrain.ParsePatrol(File.ReadAllText(Path.Combine(Application.dataPath, "EscapeMaze/Data/SentinelPatrol.txt")));
            var brain = new SentinelBrain(layout, route);
            brain.Observe(layout.Find('a'), true, .1f);
            Assert.AreEqual(SentinelState.Chase, brain.State);
            brain.Observe(layout.Find('c'), false, 1f);
            Assert.AreEqual(SentinelState.Search, brain.State);
            Assert.AreEqual(layout.Find('a'), brain.LastSeenPlayer.Value);
            brain.Observe(layout.Find('c'), false, 2.1f);
            Assert.AreEqual(SentinelState.Patrol, brain.State);
            Assert.IsFalse(brain.LastSeenPlayer.HasValue);
        }

        [Test]
        public void WallsAndConeGateDetectionAndWallsGateCapture()
        {
            Assert.IsFalse(SentinelBrain.CanDetect(4f, 1f, false, true, 8f, .5f));
            Assert.IsFalse(SentinelBrain.CanDetect(4f, -1f, true, false, 8f, .5f));
            Assert.IsTrue(SentinelBrain.CanDetect(4f, 1f, true, false, 8f, .5f));
            Assert.IsFalse(SentinelBrain.CanCatch(.5f, false, .7f));
            Assert.IsTrue(SentinelBrain.CanCatch(.5f, true, .7f));
        }

        [Test]
        public void CaughtAndVictoryAreMutuallyExclusive()
        {
            var progress = new MazeProgress();
            Assert.IsTrue(progress.TryCatch());
            Assert.IsFalse(progress.TryCatch());
            Assert.IsFalse(progress.CollectKey(0));
            Assert.IsFalse(progress.TryOpenDoor());
            Assert.IsFalse(progress.TryComplete());
            Assert.IsTrue(progress.IsFinished);
            Assert.IsFalse(progress.HasWon);
        }
    }
}
