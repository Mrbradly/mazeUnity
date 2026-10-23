using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace EscapeMaze.Tests
{
    public sealed class MazeGameplayTests
    {
        private MazePlayerMotor motor;
        private MazeInteractor interactor;
        private MazeSession session;
        private MazeLayout layout;
        private MazeCell current;

        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            yield return SceneManager.LoadSceneAsync("EscapeMaze");
            yield return null;
            motor = Object.FindObjectOfType<MazePlayerMotor>();
            interactor = Object.FindObjectOfType<MazeInteractor>();
            session = Object.FindObjectOfType<MazeSession>();
            Assert.IsNotNull(motor);
            Assert.IsNotNull(interactor);
            Assert.IsNotNull(session);
            motor.GetComponent<DesktopMazePlayer>().enabled = false;
            motor.GetComponent<DesktopMazeInput>().enabled = false;
            foreach (MazeSentinel enemy in Object.FindObjectsOfType<MazeSentinel>()) enemy.enabled = false;
            motor.transform.rotation = Quaternion.identity;
            layout = new MazeLayout(File.ReadAllText(Path.Combine(Application.dataPath, "EscapeMaze/Data/MazeLayout.txt")));
            current = layout.Find('S');
            for (int i = 0; i < 8; i++) { motor.Tick(Vector2.zero, 1f / 30f); yield return null; }
        }

        [UnityTest]
        public IEnumerator WallsAndLockedDoorBlockPlayer()
        {
            // North of the start is a wall. Moving into it must stop the capsule.
            float startZ = motor.transform.position.z;
            for (int i = 0; i < 30; i++) { motor.Tick(Vector2.up, 1f / 30f); yield return null; }
            Assert.Less(motor.transform.position.z - startZ, 1.05f);
            yield return MoveToWorld(World(layout.Find('S')));
            MazeCell door = layout.Find('D');
            yield return WalkTo(new MazeCell(door.X - 1, door.Row), false);
            ActivateAt(Object.FindObjectOfType<MazeDoor>().transform.position);
            Assert.IsFalse(session.Progress.DoorOpened);
            for (int i = 0; i < 30; i++) { motor.Tick(Vector2.right, 1f / 30f); yield return null; }
            Assert.Less(motor.transform.position.x, World(door).x - 0.35f);
            Assert.IsFalse(session.Progress.HasWon);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator InteractionCannotReachThroughWallsOrAcrossMaze()
        {
            KeyPickup key = Object.FindObjectOfType<KeyPickup>();
            ActivateAt(key.transform.position);
            Assert.AreEqual(0, session.Progress.KeyCount, "A distant key cannot be collected.");
            // Cast through the north boundary wall at an otherwise interactable key.
            // This temporary key is within reach, so the wall alone must prevent collection.
            Vector3 wallCenter = World(new MazeCell(1, 0));
            Vector3 old = key.transform.position;
            key.transform.position = wallCenter + Vector3.up * 1.15f;
            Physics.SyncTransforms();
            ActivateAt(key.transform.position);
            Assert.AreEqual(0, session.Progress.KeyCount, "The wall must occlude the key ray.");
            key.transform.position = old;
            Physics.SyncTransforms();
            yield return null;
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator CollectUnlockEscapeAndReplay()
        {
            for (int id = 0; id < 3; id++)
            {
                yield return WalkTo(layout.Find((char)('a' + id)), false);
                KeyPickup key = null;
                foreach (KeyPickup candidate in Object.FindObjectsOfType<KeyPickup>())
                    if (candidate.KeyId == id) key = candidate;
                Assert.IsNotNull(key);
                ActivateAt(key.transform.position);
                Assert.AreEqual(id + 1, session.Progress.KeyCount);
                Assert.IsFalse(key.gameObject.activeSelf);
                Assert.IsFalse(session.Progress.HasWon);
                yield return null;
                string color = new[] { "Amber", "Sky", "Coral" }[id];
                Assert.AreEqual(color, GameObject.Find(color + " lock indicator").GetComponent<Renderer>().sharedMaterial.name,
                    "Door indicator must reflect the recovered key.");
                Assert.AreEqual("Spent", GameObject.Find(color + " floor indicator").GetComponent<Renderer>().sharedMaterial.name,
                    "Recovered key socket must become inactive.");
            }
            MazeCell doorCell = layout.Find('D');
            yield return WalkTo(new MazeCell(doorCell.X - 1, doorCell.Row), false);
            MazeDoor door = Object.FindObjectOfType<MazeDoor>();
            ActivateAt(door.transform.position);
            Assert.IsTrue(session.Progress.DoorOpened);
            Assert.IsFalse(door.GetComponent<BoxCollider>().enabled);
            Assert.IsFalse(session.Progress.HasWon);
            yield return WalkTo(layout.Find('E'), true);
            yield return new WaitForFixedUpdate();
            yield return null;
            Assert.IsTrue(session.Progress.HasWon, "Walking through the unlocked exit must win.");
            Assert.IsTrue(Object.FindObjectOfType<MazeHud>().VictoryVisible);
            session.Restart();
            yield return null;
            yield return null;
            MazeSession fresh = Object.FindObjectOfType<MazeSession>();
            Assert.AreEqual(0, fresh.Progress.KeyCount);
            Assert.IsFalse(fresh.Progress.DoorOpened);
            Assert.IsFalse(fresh.Progress.HasWon);
            Assert.AreEqual(3, Object.FindObjectsOfType<KeyPickup>().Length);
            Assert.AreEqual(3, Object.FindObjectsOfType<MazeSentinel>().Length);
            Assert.AreEqual(0, Object.FindObjectOfType<MazeBlaster>().ShotsFired);
            Assert.IsTrue(Object.FindObjectOfType<MazeDoor>().GetComponent<BoxCollider>().enabled);
            Assert.IsFalse(Object.FindObjectOfType<MazeHud>().VictoryVisible);
            Assert.AreEqual(1, Object.FindObjectOfType<MazeSurvey>().Exploration.VisitedCount);
            LogAssert.NoUnexpectedReceived();
        }

        private void ActivateAt(Vector3 target)
        {
            Vector3 origin = motor.transform.position + Vector3.up * 1.6f;
            interactor.Tick(new Ray(origin, (target - origin).normalized), true);
        }

        private IEnumerator WalkTo(MazeCell target, bool doorOpen)
        {
            List<MazeCell> path = layout.FindPath(current, target, doorOpen);
            Assert.IsNotNull(path);
            foreach (MazeCell cell in path) yield return MoveToWorld(World(cell));
            current = target;
        }

        private IEnumerator MoveToWorld(Vector3 target)
        {
            for (int i = 0; i < 100; i++)
            {
                Vector3 delta = target - motor.transform.position;
                delta.y = 0f;
                if (delta.magnitude < 0.06f) yield break;
                Vector3 direction = delta.normalized;
                motor.Tick(new Vector2(direction.x, direction.z) * Mathf.Min(1f, delta.magnitude / 0.1f), 1f / 30f);
                yield return null;
            }
            Assert.Fail("CharacterController failed to reach " + target + " from " + motor.transform.position);
        }

        private static Vector3 World(MazeCell cell)
        {
            return new Vector3(cell.X * MazeLayout.CellSize, 0f, -cell.Row * MazeLayout.CellSize);
        }
    }
}
