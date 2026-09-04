using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MazeRoller3D.UI
{
    /// <summary>
    /// Splash screen: fades/scales the logo in, holds briefly, then loads Level Select.
    /// Simple CanvasGroup alpha + RectTransform scale tween via a coroutine rather than an
    /// Animator/Timeline asset - nothing here needs authored animation curves.
    /// </summary>
    public class SplashAnimator : MonoBehaviour
    {
        [SerializeField] private CanvasGroup logoGroup;
        [SerializeField] private RectTransform logoTransform;
        [SerializeField] private float fadeInSeconds = 0.6f;
        [SerializeField] private float holdSeconds = 0.9f;
        [SerializeField] private string nextSceneName = "LevelSelect";

        private void Start()
        {
            StartCoroutine(PlaySequence());
        }

        private IEnumerator PlaySequence()
        {
            if (logoGroup != null) logoGroup.alpha = 0f;
            if (logoTransform != null) logoTransform.localScale = Vector3.one * 0.8f;

            float t = 0f;
            while (t < fadeInSeconds)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / fadeInSeconds);
                float eased = 1f - (1f - p) * (1f - p); // ease-out
                if (logoGroup != null) logoGroup.alpha = eased;
                if (logoTransform != null) logoTransform.localScale = Vector3.one * Mathf.Lerp(0.8f, 1f, eased);
                yield return null;
            }

            if (logoGroup != null) logoGroup.alpha = 1f;
            if (logoTransform != null) logoTransform.localScale = Vector3.one;

            yield return new WaitForSeconds(holdSeconds);

            SceneManager.LoadScene(nextSceneName);
        }
    }
}
