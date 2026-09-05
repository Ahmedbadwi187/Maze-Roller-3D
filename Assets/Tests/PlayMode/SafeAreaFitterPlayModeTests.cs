using System.Collections;
using RollAndEscape.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace RollAndEscape.PlayModeTests
{
    /// <summary>
    /// Regression test for a real bug found via testing: a button wrapped in a SafeAreaFitter
    /// became unresponsive/invisible in the Editor. This builds the same Canvas -> SafeArea ->
    /// Button structure the milestone generators use and verifies the button's RectTransform
    /// ends up with a real, clickable, non-zero size after a frame passes - not just that the
    /// code compiles and runs without throwing.
    /// </summary>
    public class SafeAreaFitterPlayModeTests
    {
        [UnityTest]
        public IEnumerator ButtonInsideSafeArea_EndsUpWithNonZeroSize()
        {
            var canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGO.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;

            var safeAreaGO = new GameObject("SafeArea", typeof(RectTransform), typeof(SafeAreaFitter));
            safeAreaGO.transform.SetParent(canvasGO.transform, false);
            var safeAreaRect = safeAreaGO.GetComponent<RectTransform>();
            safeAreaRect.anchorMin = Vector2.zero;
            safeAreaRect.anchorMax = Vector2.one;
            safeAreaRect.offsetMin = Vector2.zero;
            safeAreaRect.offsetMax = Vector2.zero;

            var buttonGO = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonGO.transform.SetParent(safeAreaGO.transform, false);
            var buttonRect = buttonGO.GetComponent<RectTransform>();
            buttonRect.sizeDelta = new Vector2(260, 90);

            yield return null; // let Awake/layout run

            Assert.Greater(safeAreaRect.rect.width, 0f, "SafeArea collapsed to zero width - this is exactly what made the Settings button unclickable.");
            Assert.Greater(safeAreaRect.rect.height, 0f, "SafeArea collapsed to zero height.");

            var button = buttonGO.GetComponent<Button>();
            Assert.IsTrue(button.IsInteractable(), "Button should be interactable.");
            Assert.Greater(buttonRect.rect.width, 0f, "Button collapsed to zero width - would be unclickable regardless of interactable state.");

            Object.Destroy(canvasGO);
        }
    }
}
