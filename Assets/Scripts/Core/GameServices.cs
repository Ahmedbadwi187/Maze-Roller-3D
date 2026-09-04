using MazeRoller3D.Levels;
using MazeRoller3D.Monetization;
using MazeRoller3D.Persistence;

namespace MazeRoller3D.Core
{
    /// <summary>
    /// Static service locator so gameplay/UI scripts reach SettingsService/LevelProgressService
    /// /IAdsService/IAPService through one place rather than each constructing (and separately
    /// loading/saving) their own copy of the save file - SettingsService and
    /// LevelProgressService below share one loaded GameSaveData instance for the app's
    /// lifetime, which is what actually prevents one service's save clobbering the other's
    /// in-memory changes.
    /// </summary>
    public static class GameServices
    {
        private static GameSaveData _sharedData;
        private static SettingsService _settings;
        private static LevelProgressService _levelProgress;
        private static IAPService _iap;
        private static IAdsService _ads;
        private static AdsGate _adsGate;

        private static GameSaveData SharedData => _sharedData ??= SaveSystem.Load<GameSaveData>();

        public static SettingsService Settings => _settings ??= new SettingsService(SharedData);
        public static LevelProgressService LevelProgress => _levelProgress ??= new LevelProgressService(SharedData);

        public static IAPService IAP => _iap ??= CreateAndInitializeIAP();

        /// <summary>NoOpAdsService until a real ad network SDK is imported - see its doc
        /// comment for why that step can't be automated here.</summary>
        public static IAdsService Ads => _ads ??= new NoOpAdsService();

        public static AdsGate AdsGate => _adsGate ??= new AdsGate(IAP);

        private static IAPService CreateAndInitializeIAP()
        {
            var service = new IAPService();
            service.Initialize();
            return service;
        }
    }
}
