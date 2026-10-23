using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace EscapeMaze.Tests
{
    public sealed class SentinelGameplayTests
    {
        private MazeSentinel enemy;
        private MazeSession session;
        private MazePlayerMotor player;

        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            yield return SceneManager.LoadSceneAsync("EscapeMaze");
            yield return null;
            enemy = Object.FindObjectOfType<MazeSentinel>();
            session = Object.FindObjectOfType<MazeSession>();
            player = Object.FindObjectOfType<MazePlayerMotor>();
            Assert.IsNotNull(enemy);
            enemy.enabled = false;
            player.GetComponent<DesktopMazePlayer>().enabled = false;
            player.GetComponent<DesktopMazeInput>().enabled = false;
        }

        [UnityTest]
        public IEnumerator PatrolAndGraceUseTheActiveSessionClock()
        {
            Vector3 start = enemy.transform.position;
            float grace = enemy.GraceRemaining;
            enemy.Tick(10f);
            Assert.AreEqual(start, enemy.transform.position);
            Assert.AreEqual(grace, enemy.GraceRemaining, "The menu must not use the grace period.");
            session.SetRunning(true);
            enemy.Tick(grace + .1f);
            Assert.AreEqual(0f, enemy.GraceRemaining);
            Assert.AreEqual(start, enemy.transform.position);
            for (int i = 0; i < 20; i++) { enemy.Tick(.05f); yield return null; }
            Assert.Greater(Vector3.Distance(start, enemy.transform.position), .5f);
            session.TickTime(.5f);
            session.SetRunning(false);
            Vector3 paused = enemy.transform.position;
            float pausedTime = session.ElapsedSeconds;
            session.TickTime(10f);
            enemy.Tick(10f);
            Assert.AreEqual(pausedTime, session.ElapsedSeconds, "The session timer must freeze while paused.");
            Assert.AreEqual(paused, enemy.transform.position, "Esc/menu pause must freeze the Warden.");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator SightWallsCaptureAndRetry()
        {
            session.SetRunning(true);
            enemy.Tick(enemy.GraceRemaining + .1f);
            WarpPlayer(new Vector3(17.5f, .04f, -15f)); // One open cell in front of the spawn.
            enemy.Tick(.02f);
            Assert.IsTrue(enemy.PlayerVisible);
            Assert.AreEqual(SentinelState.Chase, enemy.State);
            WarpPlayer(new Vector3(22.5f, .04f, -12.5f)); // Solid wall at x=20 separates the two corridors.
            enemy.Tick(.05f);
            Assert.IsFalse(enemy.PlayerVisible, "Walls must occlude native sight rays.");
            Assert.AreEqual(SentinelState.Search, enemy.State);
            Assert.IsFalse(session.Progress.WasCaught);
            WarpPlayer(enemy.transform.position + enemy.transform.forward * .55f + Vector3.up * .04f);
            enemy.Tick(.02f);
            Assert.IsTrue(session.Progress.WasCaught);
            Assert.IsFalse(session.Progress.HasWon);
            Assert.IsFalse(session.IsRunning);
            Assert.IsTrue(Object.FindObjectOfType<MazeHud>().CaughtVisible);
            float time = session.ElapsedSeconds;
            session.TickTime(1f);
            Assert.AreEqual(time, session.ElapsedSeconds);
            Vector3 caught = enemy.transform.position;
            enemy.Tick(1f);
            Assert.AreEqual(caught, enemy.transform.position);
            Assert.IsFalse(session.CollectKey(0));
            session.Restart();
            yield return null;
            yield return null;
            MazeSession fresh = Object.FindObjectOfType<MazeSession>();
            Assert.IsFalse(fresh.Progress.IsFinished);
            Assert.AreEqual(0, fresh.Progress.KeyCount);
            Assert.Greater(Object.FindObjectOfType<MazeSentinel>().GraceRemaining, 0f);
            Assert.AreEqual(new Vector3(17.5f, 0f, -12.5f), Object.FindObjectOfType<MazeSentinel>().transform.position);
            Assert.IsFalse(Object.FindObjectOfType<MazeHud>().CaughtVisible);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator VictoryStopsTheWarden()
        {
            session.SetRunning(true);
            enemy.Tick(enemy.GraceRemaining + .1f);
            for (int i = 0; i < 3; i++) session.CollectKey(i);
            session.Progress.TryOpenDoor();
            session.Complete();
            Vector3 position = enemy.transform.position;
            enemy.Tick(10f);
            Assert.IsTrue(session.Progress.HasWon);
            Assert.IsFalse(session.Progress.WasCaught);
            Assert.IsFalse(session.IsRunning);
            Assert.AreEqual(position, enemy.transform.position);
            Assert.IsFalse(session.Progress.TryCatch());
            yield return null;
            LogAssert.NoUnexpectedReceived();
        }

        private void WarpPlayer(Vector3 position)
        {
            CharacterController controller = player.GetComponent<CharacterController>();
            controller.enabled = false;
            player.transform.position = position;
            controller.enabled = true;
            Physics.SyncTransforms();
        }
    }
}
