using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace EscapeMaze.Tests
{
    public sealed class MazeRulesTests
    {
        [Test]
        public void DuplicateKeysCannotUnlockDoor()
        {
            var progress = new MazeProgress();
            Assert.IsTrue(progress.CollectKey(0));
            Assert.IsFalse(progress.CollectKey(0));
            Assert.IsFalse(progress.CollectKey(0));
            Assert.AreEqual(1, progress.KeyCount);
            Assert.IsFalse(progress.TryOpenDoor());
            Assert.IsFalse(progress.TryComplete());
        }

        [Test]
        public void AllKeysThenDoorThenExitAreRequired()
        {
            var progress = new MazeProgress();
            Assert.IsFalse(progress.TryComplete());
            for (int i = 0; i < 3; i++)
            {
                Assert.IsFalse(progress.TryOpenDoor());
                Assert.IsTrue(progress.CollectKey(i));
            }
            Assert.IsFalse(progress.TryComplete());
            Assert.IsTrue(progress.TryOpenDoor());
            Assert.IsFalse(progress.HasWon, "Unlocking alone must not win.");
            Assert.IsTrue(progress.TryComplete());
            Assert.IsTrue(progress.HasWon);
            Assert.IsFalse(progress.TryComplete());
        }

        [Test]
        public void InvalidKeyIdsAreRejected()
        {
            var progress = new MazeProgress();
            Assert.Throws<ArgumentOutOfRangeException>(() => progress.CollectKey(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => progress.CollectKey(3));
        }

        [Test]
        public void SurveyRevealsOnlyLocalKnowledge()
        {
            var layout = new MazeLayout(File.ReadAllText(Path.Combine(Application.dataPath, "EscapeMaze/Data/MazeLayout.txt")));
            var survey = new MazeExploration(layout);
            var start = layout.Find('S');
            survey.Visit(start);
            survey.Visit(start);
            Assert.AreEqual(1, survey.VisitedCount);
            Assert.IsTrue(survey.IsDiscovered(start));
            Assert.IsFalse(survey.IsDiscovered(layout.Find('a')));
            Assert.IsFalse(survey.IsDiscovered(layout.Find('E')));
            survey.Visit(new MazeCell(0, 0));
            Assert.AreEqual(1, survey.VisitedCount);
            Assert.IsFalse(survey.IsDiscovered(new MazeCell(0, 0)));
        }

        [Test]
        public void KeysAreReachableAndExitHasNoLockedBypass()
        {
            var layout = new MazeLayout(File.ReadAllText(Path.Combine(Application.dataPath, "EscapeMaze/Data/MazeLayout.txt")));
            foreach (char key in "abc")
                Assert.IsNotNull(layout.FindPath(layout.Find('S'), layout.Find(key), false));
            Assert.IsNull(layout.FindPath(layout.Find('S'), layout.Find('E'), false));
            Assert.IsNotNull(layout.FindPath(layout.Find('S'), layout.Find('E'), true));
        }
    }
}
