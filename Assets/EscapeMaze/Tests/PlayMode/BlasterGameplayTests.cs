using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace EscapeMaze.Tests
{
    public sealed class BlasterGameplayTests
    {
        private MazeSession session;
        private MazeBlaster blaster;
        private MazeSentinel enemy;
        private MazeSentinel[] wardens;

        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            yield return SceneManager.LoadSceneAsync("EscapeMaze");
            yield return null;
            session = Object.FindObjectOfType<MazeSession>();
            blaster = Object.FindObjectOfType<MazeBlaster>();
            wardens = Object.FindObjectsOfType<MazeSentinel>();
            foreach (MazeSentinel warden in wardens)
            {
                warden.enabled = false;
                if (warden.name.StartsWith("Warden 1")) enemy = warden;
            }
            blaster.GetComponent<DesktopMazePlayer>().enabled = false;
            blaster.GetComponent<DesktopMazeInput>().enabled = false;
            Assert.IsNotNull(enemy);
            session.SetRunning(true);
            Physics.SyncTransforms();
        }

        [UnityTest]
        public IEnumerator AllThreeWardensActuallyMoveAndPause()
        {
            Assert.AreEqual(3, wardens.Length);
            var starts = new Vector3[wardens.Length];
            for (int i = 0; i < wardens.Length; i++)
            {
                starts[i] = wardens[i].transform.position;
                wardens[i].Tick(wardens[i].GraceRemaining + .1f);
            }
            for (int frame = 0; frame < 40; frame++)
                foreach (MazeSentinel warden in wardens) warden.Tick(.05f);
            for (int i = 0; i < wardens.Length; i++)
                Assert.Greater(Vector3.Distance(starts[i], wardens[i].transform.position), 1f);
            session.SetRunning(false);
            foreach (MazeSentinel warden in wardens)
            {
                Vector3 position = warden.transform.position;
                warden.Tick(10f);
                Assert.AreEqual(position, warden.transform.position);
            }
            yield return null;
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator ShotHitsStunsFreezesAndThenMovementResumes()
        {
            enemy.Tick(enemy.GraceRemaining + .1f);
            Vector3 position = enemy.transform.position;
            Ray aim = new Ray(position + Vector3.up * 1.4f + Vector3.back * 2f, Vector3.forward);
            Assert.IsTrue(blaster.Fire(aim));
            Assert.IsTrue(blaster.LastShotHit);
            Assert.IsTrue(enemy.IsStunned);
            Assert.AreEqual(4f, enemy.StunRemaining);
            Assert.IsFalse(enemy.IsAlert);
            Assert.IsFalse(blaster.Fire(aim), "Cooldown blocks a second shot.");
            enemy.Tick(1f);
            Assert.AreEqual(position, enemy.transform.position);
            session.SetRunning(false);
            float remaining = enemy.StunRemaining;
            enemy.Tick(20f);
            blaster.Tick(20f);
            Assert.AreEqual(remaining, enemy.StunRemaining);
            Assert.IsFalse(blaster.Fire(aim), "Pause cannot fire.");
            session.SetRunning(true);
            enemy.Tick(3.1f);
            Assert.IsFalse(enemy.IsStunned);
            enemy.Tick(.1f);
            Assert.Greater(Vector3.Distance(position, enemy.transform.position), .01f);
            blaster.Tick(.5f);
            Assert.IsTrue(blaster.Ready);
            yield return null;
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator WallsAndRangeBlockShotsAndVictoryDisablesFiring()
        {
            Vector3 target = enemy.transform.position + Vector3.up * 1.4f;
            Assert.IsTrue(blaster.Fire(new Ray(target + Vector3.right * 5f, Vector3.left)));
            Assert.IsFalse(blaster.LastShotHit, "The wall at x=20 must stop the shot.");
            Assert.IsFalse(enemy.IsStunned);
            blaster.Tick(.5f);
            Assert.IsTrue(blaster.Fire(new Ray(target + Vector3.back * 19f, Vector3.forward)));
            Assert.IsFalse(blaster.LastShotHit, "Shots cannot exceed the blaster's range.");
            Assert.IsFalse(enemy.IsStunned);
            for (int i = 0; i < 3; i++) session.CollectKey(i);
            session.Progress.TryOpenDoor();
            session.Complete();
            blaster.Tick(10f);
            int shots = blaster.ShotsFired;
            Assert.IsFalse(blaster.Fire(new Ray(target + Vector3.back * 2f, Vector3.forward)));
            Assert.AreEqual(shots, blaster.ShotsFired);
            yield return null;
            LogAssert.NoUnexpectedReceived();
        }
    }
}
