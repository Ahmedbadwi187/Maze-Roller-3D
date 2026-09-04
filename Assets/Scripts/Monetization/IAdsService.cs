using System;

namespace MazeRoller3D.Monetization
{
    /// <summary>
    /// Ad network abstraction - banner, interstitial, rewarded. Nothing in the rest of the
    /// game talks to an ad SDK directly; everything goes through this interface, so the real
    /// network can be dropped in later without touching call sites.
    /// </summary>
    public interface IAdsService
    {
        void ShowBanner();
        void HideBanner();
        void ShowInterstitial();

        /// <summary>Shows a rewarded ad; calls exactly one of the two callbacks once it
        /// resolves (never both, never neither).</summary>
        void ShowRewardedAd(Action onRewardGranted, Action onFailedOrCancelled);
    }
}
