using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RollAndEscape.UI
{
    /// <summary>
    /// Loads a scene by name when this GameObject's Button is clicked. A tiny runtime
    /// component rather than wiring Button.onClick.AddListener(lambda) directly from an editor
    /// generator script - a listener added that way is never persisted to the saved scene
    /// (UnityEvent only serializes "persistent" listeners added via the Editor-only
    /// UnityEventTools.AddPersistentListener, not a runtime AddListener call), so the button
    /// would look perfectly wired (raycastable, interactable) but silently do nothing the
    /// moment the scene reloads fresh - every real Play session, every device run. This is
    /// exactly the bug that made the Settings/Back buttons unresponsive despite several
    /// rounds of raycast/anchor fixes aimed at the wrong layer.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class LoadSceneOnClick : MonoBehaviour
    {
        [SerializeField] private string sceneName;

        private void Awake()
        {
            GetComponent<Button>().onClick.AddListener(() => SceneManager.LoadScene(sceneName));
        }
    }
}
