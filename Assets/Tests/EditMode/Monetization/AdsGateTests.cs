using NUnit.Framework;

namespace MazeRoller3D.Monetization.Tests
{
    /// <summary>A fake IPurchaseState so these tests don't need real Unity IAP / the Fake Store.</summary>
    internal class FakePurchaseState : IPurchaseState
    {
        public bool AdsRemoved { get; set; }
    }

    [TestFixture]
    public class AdsGateTests
    {
        [Test]
        public void FirstTwoCompletions_DoNotTriggerAnInterstitial()
        {
            var gate = new AdsGate(new FakePurchaseState());

            Assert.IsFalse(gate.RecordLevelCompletionAndCheckInterstitial());
            Assert.IsFalse(gate.RecordLevelCompletionAndCheckInterstitial());
        }

        [Test]
        public void ThirdCompletion_TriggersAnInterstitial()
        {
            var gate = new AdsGate(new FakePurchaseState());

            gate.RecordLevelCompletionAndCheckInterstitial();
            gate.RecordLevelCompletionAndCheckInterstitial();
            Assert.IsTrue(gate.RecordLevelCompletionAndCheckInterstitial());
        }

        [Test]
        public void CadenceResetsAfterEachInterstitial_NotEveryLevelAfterTheFirstOne()
        {
            var gate = new AdsGate(new FakePurchaseState());

            // Levels 1-3: interstitial only on the 3rd.
            Assert.IsFalse(gate.RecordLevelCompletionAndCheckInterstitial());
            Assert.IsFalse(gate.RecordLevelCompletionAndCheckInterstitial());
            Assert.IsTrue(gate.RecordLevelCompletionAndCheckInterstitial());

            // Levels 4-6: cadence starts over, not an interstitial on every completion from here on.
            Assert.IsFalse(gate.RecordLevelCompletionAndCheckInterstitial());
            Assert.IsFalse(gate.RecordLevelCompletionAndCheckInterstitial());
            Assert.IsTrue(gate.RecordLevelCompletionAndCheckInterstitial());
        }

        [Test]
        public void AdsRemoved_NeverTriggersAnInterstitial_RegardlessOfCompletionCount()
        {
            var purchaseState = new FakePurchaseState { AdsRemoved = true };
            var gate = new AdsGate(purchaseState);

            for (int i = 0; i < 10; i++)
            {
                Assert.IsFalse(gate.RecordLevelCompletionAndCheckInterstitial());
            }
        }

        [Test]
        public void ShouldShowBanner_FalseOnceAdsRemoved()
        {
            var purchaseState = new FakePurchaseState();
            var gate = new AdsGate(purchaseState);

            Assert.IsTrue(gate.ShouldShowBanner());
            purchaseState.AdsRemoved = true;
            Assert.IsFalse(gate.ShouldShowBanner());
        }
    }
}
