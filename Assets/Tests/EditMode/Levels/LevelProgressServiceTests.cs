using System.IO;
using RollAndEscape.Persistence;
using NUnit.Framework;

namespace RollAndEscape.Levels.Tests
{
    [TestFixture]
    public class LevelProgressServiceTests
    {
        private string _tempFilePath;

        [SetUp]
        public void SetUp()
        {
            _tempFilePath = Path.Combine(Path.GetTempPath(), $"mazeroller3d-test-save-{System.Guid.NewGuid():N}.json");
            SaveSystem.OverrideFilePathForTests = _tempFilePath;
        }

        [TearDown]
        public void TearDown()
        {
            SaveSystem.OverrideFilePathForTests = null;
            if (File.Exists(_tempFilePath)) File.Delete(_tempFilePath);
        }

        [Test]
        public void FirstLevel_IsAlwaysUnlocked_EvenWithNoSaveDataYet()
        {
            var service = new LevelProgressService();
            Assert.IsTrue(service.IsUnlocked(0));
        }

        [Test]
        public void SecondLevel_IsLocked_UntilFirstLevelCompleted()
        {
            var service = new LevelProgressService();
            Assert.IsFalse(service.IsUnlocked(1));

            service.RecordCompletion(0, stars: 2, timeSeconds: 30f);
            Assert.IsTrue(service.IsUnlocked(1));
        }

        [Test]
        public void RecordCompletion_KeepsBestStarsAndBestTimeAcrossAttempts()
        {
            var service = new LevelProgressService();

            service.RecordCompletion(0, stars: 1, timeSeconds: 60f);
            service.RecordCompletion(0, stars: 3, timeSeconds: 45f); // better run: more stars, faster
            service.RecordCompletion(0, stars: 2, timeSeconds: 90f); // worse run: shouldn't regress either stat

            Assert.AreEqual(3, service.GetStars(0));
            Assert.AreEqual(45f, service.GetBestTimeSeconds(0));
        }

        [Test]
        public void Progress_PersistsAcrossServiceInstances()
        {
            var first = new LevelProgressService();
            first.RecordCompletion(0, stars: 3, timeSeconds: 20f);

            var reloaded = new LevelProgressService();
            Assert.IsTrue(reloaded.IsCompleted(0));
            Assert.AreEqual(3, reloaded.GetStars(0));
            Assert.AreEqual(20f, reloaded.GetBestTimeSeconds(0));
            Assert.IsTrue(reloaded.IsUnlocked(1));
        }

        [Test]
        public void UncompletedLevel_ReportsNotCompletedAndZeroStars()
        {
            var service = new LevelProgressService();
            Assert.IsFalse(service.IsCompleted(5));
            Assert.AreEqual(0, service.GetStars(5));
        }
    }
}
