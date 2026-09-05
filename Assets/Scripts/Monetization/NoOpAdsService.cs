using System;
using UnityEngine;

namespace RollAndEscape.Monetization
{
    /// <summary>
    /// Placeholder IAdsService with no real ad network wired in. The Google Mobile Ads Unity
    /// plugin isn't distributed as a UPM package (see Docs/SETUP.md's note on it), so it can't
    /// be added by automated project setup - import it manually (Assets > Import Package >
    /// Custom Package, from the plugin's .unitypackage release), then write a
    /// GoogleMobileAdsService implementing this same IAdsService interface and register that
    /// in GameServices.Ads instead of this class. Every call here just logs, and rewarded ads
    /// grant their reward immediately, so the rest of the game (banner placement, interstitial
    /// cadence, the hint flow) can be built and tested against the interface today without
    /// waiting on that manual step.
    /// </summary>
    public class NoOpAdsService : IAdsService
    {
        public void ShowBanner() => Debug.Log("[Ads] ShowBanner (NoOpAdsService placeholder - see its doc comment).");
        public void HideBanner() => Debug.Log("[Ads] HideBanner (NoOpAdsService placeholder).");
        public void ShowInterstitial() => Debug.Log("[Ads] ShowInterstitial (NoOpAdsService placeholder).");

        public void ShowRewardedAd(Action onRewardGranted, Action onFailedOrCancelled)
        {
            Debug.Log("[Ads] ShowRewardedAd (NoOpAdsService placeholder) - granting reward immediately.");
            onRewardGranted?.Invoke();
        }
    }
}
