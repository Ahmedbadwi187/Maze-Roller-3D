using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RollAndEscape.UI
{
    /// <summary>
    /// In-game pause menu: Resume, Restart, Quit to Menu. Opened via a visible on-screen
    /// Pause button or the Android hardware Back button (Unity maps it to
    /// KeyCode.Escape) - added after real device testing showed there was no way to back out
    /// of a level once inside it. Pauses via Time.timeScale so physics/ball movement genuinely
    /// stop, not just visually.
    /// </summary>
    public class PauseUI : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Button pauseButton;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button quitToMenuButton;
        [SerializeField] private string levelSelectSceneName = "LevelSelect";

        private bool _isPaused;

        private void Awake()
        {
            if (root != null) root.SetActive(false);
            if (pauseButton != null) pauseButton.onClick.AddListener(Pause);
            if (resumeButton != null) resumeButton.onClick.AddListener(Resume);
            if (restartButton != null) restartButton.onClick.AddListener(Restart);
            if (quitToMenuButton != null) quitToMenuButton.onClick.AddListener(QuitToMenu);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) TogglePause();
        }

        private void OnDestroy()
        {
            // Guard against leaving the whole game frozen if this object goes away mid-pause
            // (e.g. a scene load triggered some other way while paused).
            Time.timeScale = 1f;
        }

        public void TogglePause()
        {
            if (_isPaused) Resume();
            else Pause();
        }

        private void Pause()
        {
            _isPaused = true;
            Time.timeScale = 0f;
            if (root != null) root.SetActive(true);
        }

        private void Resume()
        {
            _isPaused = false;
            Time.timeScale = 1f;
            if (root != null) root.SetActive(false);
        }

        private void Restart()
        {
            Time.timeScale = 1f;

            // Re-arm the exact same level before reloading - LevelSessionContext's selection
            // flag is one-shot and was already consumed when this level first loaded, so
            // without this MazeView3D would fall back to its hardcoded preview maze instead of
            // the level actually in progress. See LevelSessionHelper. Real bug found via
            // device testing ("Restart returns to level 1 instead of the level being played").
            LevelSessionHelper.RearmCurrentLevel();

            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void QuitToMenu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(levelSelectSceneName);
        }
    }
}
