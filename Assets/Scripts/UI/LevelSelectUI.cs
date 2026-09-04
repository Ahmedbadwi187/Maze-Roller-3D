using MazeRoller3D.Core;
using MazeRoller3D.Gameplay;
using MazeRoller3D.Levels;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MazeRoller3D.UI
{
    /// <summary>
    /// Populates the level-select grid at runtime from LevelRepository + LevelProgressService
    /// rather than the editor generator baking every button into the scene file - unlock
    /// state/stars can change between visits (a level completed just now), so this has to be
    /// live data read at Start anyway, not something worth pre-baking.
    /// </summary>
    public class LevelSelectUI : MonoBehaviour
    {
        [SerializeField] private Transform content;
        [SerializeField] private LevelSelectButton buttonTemplate;
        [SerializeField] private string gameSceneName = "Game";

        private void Start()
        {
            // Banner lives on menu/level-select only, never in-game, per the monetization spec.
            if (GameServices.AdsGate.ShouldShowBanner()) GameServices.Ads.ShowBanner();

            var progress = GameServices.LevelProgress;
            buttonTemplate.gameObject.SetActive(false);

            foreach (var level in LevelRepository.GetAllLevels())
            {
                var button = Instantiate(buttonTemplate, content);
                button.gameObject.SetActive(true);
                button.Configure(level, progress.IsUnlocked(level.LevelIndex), progress.GetStars(level.LevelIndex), OnLevelSelected);
            }
        }

        private void OnLevelSelected(LevelDefinition level)
        {
            LevelSessionContext.SelectLevel(level.LevelIndex, level.Width, level.Height, level.Seed);
            SceneManager.LoadScene(gameSceneName);
        }
    }
}
