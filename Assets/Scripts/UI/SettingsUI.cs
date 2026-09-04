using MazeRoller3D.Core;
using MazeRoller3D.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace MazeRoller3D.UI
{
    /// <summary>
    /// Settings screen: sound/music toggles (persisted via GameServices.Settings - SoundOn
    /// also immediately gates AudioListener.volume since there's no dedicated audio mixer yet;
    /// milestone 9 wires real SFX/music through it instead) and a Tilt/Joystick control-scheme
    /// toggle that PlayerInputRouter reads on Awake. Remove Ads / Restore Purchases go through
    /// the real Unity IAP wrapper (GameServices.IAP) - testable in-editor via Unity IAP's Fake
    /// Store, no real store credentials needed for that part.
    /// </summary>
    public class SettingsUI : MonoBehaviour
    {
        [SerializeField] private Toggle soundToggle;
        [SerializeField] private Toggle musicToggle;
        [SerializeField] private Toggle controlSchemeToggle; // on = Joystick, off = Tilt
        [SerializeField] private Button restorePurchasesButton;
        [SerializeField] private Button removeAdsButton;

        private void Start()
        {
            var settings = GameServices.Settings;

            if (soundToggle != null) soundToggle.isOn = settings.SoundOn;
            if (musicToggle != null) musicToggle.isOn = settings.MusicOn;
            if (controlSchemeToggle != null) controlSchemeToggle.isOn = settings.ControlSchemeRaw == (int)ControlScheme.Joystick;

            ApplySoundVolume(settings.SoundOn);

            if (soundToggle != null)
            {
                soundToggle.onValueChanged.AddListener(isOn =>
                {
                    settings.SoundOn = isOn;
                    ApplySoundVolume(isOn);
                });
            }

            if (musicToggle != null) musicToggle.onValueChanged.AddListener(isOn => settings.MusicOn = isOn);

            if (controlSchemeToggle != null)
            {
                controlSchemeToggle.onValueChanged.AddListener(isOn =>
                    settings.ControlSchemeRaw = (int)(isOn ? ControlScheme.Joystick : ControlScheme.Tilt));
            }

            if (restorePurchasesButton != null)
            {
                restorePurchasesButton.onClick.AddListener(() =>
                    GameServices.IAP.RestorePurchases(success =>
                        Debug.Log(success ? "Restore Purchases: succeeded." : "Restore Purchases: failed.")));
            }

            if (removeAdsButton != null)
            {
                removeAdsButton.onClick.AddListener(() => GameServices.IAP.BuyRemoveAds());
            }
        }

        private static void ApplySoundVolume(bool on) => AudioListener.volume = on ? 1f : 0f;
    }
}
