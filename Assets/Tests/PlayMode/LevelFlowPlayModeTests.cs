using System.Collections;
using System.IO;
using System.Reflection;
using RollAndEscape.Gameplay;
using RollAndEscape.Levels;
using RollAndEscape.Persistence;
using RollAndEscape.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace RollAndEscape.PlayModeTests
{
    /// <summary>
    /// Runtime (Play mode) coverage for the win-condition chain: LevelExitTrigger firing on
    /// real physics contact, LevelFlowController reacting to it, LevelCompleteUI showing, and
    /// (milestone 6) star/time progress actually being persisted. This needs actual Play mode -
    /// MonoBehaviour lifecycle methods (Awake, OnTriggerEnter) simply don't run during
    /// edit-time editor scripting, only real physics kinematics/collision response do, which is
    /// why this is a PlayMode test and not just another edit-time editor script like the
    /// milestone generators use.
    ///
    /// Builds its own minimal scene objects at runtime (no UnityEditor/AssetDatabase calls, so
    /// this could run on a real device build too, unlike the editor generator scripts) and uses
    /// reflection to set the private [SerializeField] wiring fields, the same way the editor
    /// generators do via SerializedObject - just without the UnityEditor dependency.
    /// </summary>
    public class LevelFlowPlayModeTests
    {
        private string _tempSavePath;

        [SetUp]
        public void SetUp()
        {
            _tempSavePath = Path.Combine(Path.GetTempPath(), $"mazeroller3d-playmode-save-{System.Guid.NewGuid():N}.json");
            SaveSystem.OverrideFilePathForTests = _tempSavePath;
        }

        [TearDown]
        public void TearDown()
        {
            SaveSystem.OverrideFilePathForTests = null;
            if (File.Exists(_tempSavePath)) File.Delete(_tempSavePath);
        }

        [UnityTest]
        public IEnumerator BallEnteringExitTrigger_CompletesLevelAndDisablesBallControl()
        {
            var scene = BuildTestScene();

            yield return null; // let Awake() run for everything spawned this frame

            Assert.IsFalse(scene.OverlayRootGO.activeSelf, "Overlay should start hidden (LevelCompleteUI.Awake hides it).");
            Assert.IsTrue(scene.BallController.enabled, "Ball control should start enabled.");

            yield return DropUntilCompleted(scene);

            Assert.IsTrue(scene.OverlayRootGO.activeSelf, "Overlay never became active - LevelExitTrigger/LevelFlowController didn't react to the ball entering it.");
            Assert.IsFalse(scene.BallController.enabled, "BallController should be disabled once the level completes.");

            scene.Destroy();
        }

        [UnityTest]
        public IEnumerator CompletingASelectedLevel_RecordsStarsAndTimeToProgressService()
        {
            LevelSessionContext.SelectLevel(levelIndex: 0, width: 8, height: 8, seed: 1);

            var scene = BuildTestScene();
            yield return null;
            yield return DropUntilCompleted(scene);

            Assert.IsTrue(scene.OverlayRootGO.activeSelf, "Level never completed - can't check progress recording.");

            var progress = new LevelProgressService();
            Assert.IsTrue(progress.IsCompleted(0), "Expected level 0 to be recorded as completed.");
            Assert.GreaterOrEqual(progress.GetStars(0), 1, "Completing a level should always award at least 1 star.");
            Assert.Greater(progress.GetBestTimeSeconds(0), 0f, "Expected a recorded best time greater than zero.");
            Assert.IsTrue(progress.IsUnlocked(1), "Completing level 0 should unlock level 1.");

            scene.Destroy();
        }

        private static IEnumerator DropUntilCompleted(TestScene scene)
        {
            // Let the ball fall, land in/pass through the trigger zone, and physics dispatch
            // OnTriggerEnter - up to ~3 seconds of fixed steps, generous for a 3m drop.
            for (int i = 0; i < 180 && !scene.OverlayRootGO.activeSelf; i++)
            {
                yield return new WaitForFixedUpdate();
            }
        }

        private static TestScene BuildTestScene()
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.transform.position = new Vector3(0f, -0.1f, 0f);
            floor.transform.localScale = new Vector3(10f, 0.2f, 10f);

            var ballGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ballGO.transform.localScale = Vector3.one * 0.8f;
            ballGO.transform.position = new Vector3(0f, 3f, 0f); // drop from above into the trigger zone
            ballGO.AddComponent<Rigidbody>();
            var ballController = ballGO.AddComponent<BallController>();

            var triggerGO = new GameObject("ExitTrigger");
            triggerGO.transform.position = new Vector3(0f, 0.5f, 0f);
            var collider = triggerGO.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.size = new Vector3(2f, 2f, 2f);
            var exitTrigger = triggerGO.AddComponent<LevelExitTrigger>();

            var overlayRootGO = new GameObject("OverlayRoot");

            // AddComponent fires Awake() immediately/synchronously in Play mode - building
            // these two while inactive lets the private fields get injected *before* Awake
            // runs (which is what reads them), rather than racing it.
            var levelCompleteGO = new GameObject("LevelCompleteUI");
            levelCompleteGO.SetActive(false);
            var levelCompleteUI = levelCompleteGO.AddComponent<LevelCompleteUI>();
            SetPrivateField(levelCompleteUI, "root", overlayRootGO);
            levelCompleteGO.SetActive(true);

            var flowGO = new GameObject("Flow");
            flowGO.SetActive(false);
            var flow = flowGO.AddComponent<LevelFlowController>();
            SetPrivateField(flow, "exitTrigger", exitTrigger);
            SetPrivateField(flow, "ballController", ballController);
            SetPrivateField(flow, "levelCompleteUI", levelCompleteUI);
            flowGO.SetActive(true);

            return new TestScene(floor, ballGO, triggerGO, overlayRootGO, levelCompleteUI, flowGO, ballController);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field, $"Expected a private field named '{fieldName}' on {target.GetType().Name}.");
            field.SetValue(target, value);
        }

        private readonly struct TestScene
        {
            private readonly GameObject _floor;
            private readonly GameObject _ballGO;
            private readonly GameObject _triggerGO;
            public readonly GameObject OverlayRootGO;
            private readonly LevelCompleteUI _levelCompleteUI;
            private readonly GameObject _flowGO;
            public readonly BallController BallController;

            public TestScene(GameObject floor, GameObject ballGO, GameObject triggerGO, GameObject overlayRootGO,
                LevelCompleteUI levelCompleteUI, GameObject flowGO, BallController ballController)
            {
                _floor = floor;
                _ballGO = ballGO;
                _triggerGO = triggerGO;
                OverlayRootGO = overlayRootGO;
                _levelCompleteUI = levelCompleteUI;
                _flowGO = flowGO;
                BallController = ballController;
            }

            public void Destroy()
            {
                Object.Destroy(_floor);
                Object.Destroy(_ballGO);
                Object.Destroy(_triggerGO);
                Object.Destroy(OverlayRootGO);
                Object.Destroy(_levelCompleteUI.gameObject);
                Object.Destroy(_flowGO);
            }
        }
    }
}
