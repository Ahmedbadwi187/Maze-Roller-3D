namespace RollAndEscape.Persistence
{
    /// <summary>
    /// Sound/music/control-scheme preferences, backed by the same GameSaveData/SaveSystem as
    /// LevelProgressService. ControlScheme is a plain int (not the Gameplay assembly's enum)
    /// so this assembly never needs to reference Gameplay - callers cast at the boundary.
    /// </summary>
    public class SettingsService
    {
        private readonly GameSaveData _data;

        public SettingsService() : this(SaveSystem.Load<GameSaveData>()) { }

        /// <summary>Takes a shared GameSaveData instance (see GameServices) rather than
        /// loading its own copy, so a change made here and a change made through
        /// LevelProgressService in the same session don't clobber each other on save.</summary>
        public SettingsService(GameSaveData data)
        {
            _data = data;
        }

        public bool SoundOn
        {
            get => _data.Settings.SoundOn;
            set { _data.Settings.SoundOn = value; SaveSystem.Save(_data); }
        }

        public bool MusicOn
        {
            get => _data.Settings.MusicOn;
            set { _data.Settings.MusicOn = value; SaveSystem.Save(_data); }
        }

        public int ControlSchemeRaw
        {
            get => _data.Settings.ControlScheme;
            set { _data.Settings.ControlScheme = value; SaveSystem.Save(_data); }
        }
    }
}
