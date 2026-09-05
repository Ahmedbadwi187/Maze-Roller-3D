namespace RollAndEscape.Monetization
{
    /// <summary>True once "Remove Ads" has been purchased. Separate from IAdsService so
    /// AdsGate's cadence logic can be unit-tested without any ad SDK or Unity IAP involved.</summary>
    public interface IPurchaseState
    {
        bool AdsRemoved { get; }
    }

    /// <summary>
    /// Decides *whether* ads should show at all (never once Remove Ads is purchased) and the
    /// interstitial *cadence* (every 3 level completions, not every single one, per spec) -
    /// deliberately pure logic with no ad-SDK calls in it, so this is fully unit-testable
    /// independent of IAdsService/IAPService.
    /// </summary>
    public class AdsGate
    {
        private const int LevelsPerInterstitial = 3;

        private readonly IPurchaseState _purchaseState;
        private int _completionsSinceLastInterstitial;

        public AdsGate(IPurchaseState purchaseState)
        {
            _purchaseState = purchaseState;
        }

        public bool ShouldShowBanner() => !_purchaseState.AdsRemoved;

        /// <summary>Call once per level completion. Returns true on the calls where an
        /// interstitial should actually show (every 3rd completion), false otherwise - and
        /// always false once ads are removed.</summary>
        public bool RecordLevelCompletionAndCheckInterstitial()
        {
            if (_purchaseState.AdsRemoved) return false;

            _completionsSinceLastInterstitial++;
            if (_completionsSinceLastInterstitial < LevelsPerInterstitial) return false;

            _completionsSinceLastInterstitial = 0;
            return true;
        }
    }
}
