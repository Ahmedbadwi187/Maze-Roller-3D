using MazeRoller3D.Core;
using MazeRoller3D.Gameplay;
using MazeRoller3D.Levels;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MazeRoller3D.UI
{
    /// <summary>
    /// Wires a level's exit trigger to its Level Complete overlay: tracks elapsed time from
    /// Awake, freezes ball control and records star/time progress on completion, and hooks up
    /// Replay/Next Level. Progress is only recorded when LevelSessionContext knows which level
    /// this is (i.e. reached via Level Select) - opening Game.unity directly in-editor during
    /// development still shows the overlay, just without touching save data.
    /// </summary>
    public class LevelFlowController : MonoBehaviour
    {
        [SerializeField] private LevelExitTrigger exitTrigger;
        [SerializeField] private BallController ballController;
        [SerializeField] private LevelCompleteUI levelCompleteUI;

        [Header("Polish (milestone 9) - all optional, null-checked")]
        [SerializeField] private ParticleSystem completionParticles;
        [SerializeField] private AudioSource completionAudioSource;

        private float _startTime;
        private bool _completed;

        private void Awake()
        {
            _startTime = Time.time;

            if (exitTrigger != null) exitTrigger.LevelCompleted += HandleLevelCompleted;
            if (levelCompleteUI != null)
            {
                levelCompleteUI.ReplayRequested += Replay;
                levelCompleteUI.NextLevelRequested += NextLevel;
            }
        }

        private void OnDestroy()
        {
            if (exitTrigger != null) exitTrigger.LevelCompleted -= HandleLevelCompleted;
            if (levelCompleteUI != null)
            {
                levelCompleteUI.ReplayRequested -= Replay;
                levelCompleteUI.NextLevelRequested -= NextLevel;
            }
        }

        private void HandleLevelCompleted()
        {
            if (_completed) return;
            _completed = true;

            float elapsed = Time.time - _startTime;
            if (ballController != null) ballController.enabled = false;

            int stars = 0;
            if (LevelSessionContext.CurrentLevelIndex >= 0)
            {
                stars = StarCalculator.CalculateStars(LevelSessionContext.CurrentWidth, LevelSessionContext.CurrentHeight, elapsed);
                GameServices.LevelProgress.RecordCompletion(LevelSessionContext.CurrentLevelIndex, stars, elapsed);

                // Every 3rd completion (never once Remove Ads is purchased), not every level.
                if (GameServices.AdsGate.RecordLevelCompletionAndCheckInterstitial())
                {
                    GameServices.Ads.ShowInterstitial();
                }
            }

            if (levelCompleteUI != null) levelCompleteUI.Show(elapsed, stars);

            if (completionParticles != null) completionParticles.Play();
            if (completionAudioSource != null) completionAudioSource.PlayOneShot(ProceduralSfx.CreateSuccessChime());
        }

        private void Replay()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void NextLevel()
        {
            int nextIndex = LevelSessionContext.CurrentLevelIndex + 1;
            if (LevelSessionContext.CurrentLevelIndex < 0 || nextIndex >= LevelRepository.TotalLevels)
            {
                Replay(); // no known next level (e.g. testing Game.unity directly) - just restart
                return;
            }

            var next = LevelRepository.GetLevel(nextIndex);
            LevelSessionContext.SelectLevel(next.LevelIndex, next.Width, next.Height, next.Seed);
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
