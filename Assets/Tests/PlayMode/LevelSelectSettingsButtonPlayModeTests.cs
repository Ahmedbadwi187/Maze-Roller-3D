using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace RollAndEscape.PlayModeTests
{
    /// <summary>
    /// Regression test for a real, repeatedly-reported bug: the Settings button on Level
    /// Select was unresponsive to taps. This loads the actual LevelSelect scene (from Build
    /// Settings, exactly as a device would) and fires a REAL simulated pointer click through
    /// EventSystem/GraphicRaycaster at the button's actual screen position - not just calling
    /// button.onClick.Invoke() directly, which would trivially "pass" even if the real UI
    /// raycast routing were broken. Passing this test is what "clickable" actually means.
    /// </summary>
    public class LevelSelectSettingsButtonPlayModeTests
    {
        [UnityTest]
        public IEnumerator SettingsButton_RespondsToASimulatedRealClick()
        {
            var loadOp = SceneManager.LoadSceneAsync("LevelSelect");
            while (!loadOp.isDone) yield return null;
            yield return null; // let Start()/Awake() finish across everything in the scene

            var buttonGO = GameObject.Find("SettingsButton");
            Assert.IsNotNull(buttonGO, "Could not find a 'SettingsButton' GameObject in the loaded LevelSelect scene.");

            var button = buttonGO.GetComponent<Button>();
            Assert.IsNotNull(button, "SettingsButton has no Button component.");
            Assert.IsTrue(button.IsInteractable(), "SettingsButton is not interactable.");

            var rect = buttonGO.GetComponent<RectTransform>();
            Assert.Greater(rect.rect.width, 0f, "SettingsButton's RectTransform has zero width - it would be unclickable regardless of raycasting.");
            Assert.Greater(rect.rect.height, 0f, "SettingsButton's RectTransform has zero height.");

            var eventSystem = Object.FindFirstObjectByType<EventSystem>();
            Assert.IsNotNull(eventSystem, "No EventSystem in the scene - nothing would be clickable at all.");

            // Raycast at the button's actual current screen-space center, through the real
            // GraphicRaycaster pipeline - this is what a real tap does.
            var canvas = buttonGO.GetComponentInParent<Canvas>();
            var raycaster = canvas.GetComponent<GraphicRaycaster>();
            Assert.IsNotNull(raycaster, "No GraphicRaycaster on the button's Canvas - nothing under it is clickable.");

            var screenPoint = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, rect.position);
            var pointerData = new PointerEventData(eventSystem) { position = screenPoint };
            var results = new System.Collections.Generic.List<RaycastResult>();
            raycaster.Raycast(pointerData, results);

            Assert.IsTrue(results.Count > 0, $"Raycast at SettingsButton's screen position {screenPoint} hit nothing at all.");
            bool hitTheButtonItself = results.Exists(r => r.gameObject == buttonGO || r.gameObject.transform.IsChildOf(buttonGO.transform));
            Assert.IsTrue(hitTheButtonItself,
                $"Raycast at SettingsButton's position hit {results[0].gameObject.name} instead of the button - " +
                "something else is on top of it and stealing the tap.");

            // Now actually fire the click the way EventSystem would, and confirm it does what
            // the button is wired to do (load Settings) - not just that onClick has a listener.
            ExecuteEvents.Execute(buttonGO, pointerData, ExecuteEvents.pointerClickHandler);

            float timeout = Time.realtimeSinceStartup + 3f;
            while (SceneManager.GetActiveScene().name != "Settings" && Time.realtimeSinceStartup < timeout)
            {
                yield return null;
            }

            Assert.AreEqual("Settings", SceneManager.GetActiveScene().name,
                "Clicking SettingsButton did not load the Settings scene within 3 seconds.");
        }
    }
}
