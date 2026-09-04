using System;
using System.Collections.Generic;

namespace MazeRoller3D.Persistence
{
    /// <summary>Per-level progress: whether it's been completed, best (lowest) star-earning
    /// stars seen, and best (lowest) completion time. Default value (Completed=false,
    /// Stars=0, BestTime=0) correctly represents "never played".</summary>
    [Serializable]
    public struct LevelProgressEntry
    {
        public bool Completed;
        public int Stars;
        public float BestTimeSeconds;
    }

    /// <summary>Sound/music/control-scheme prefs - fields added here as milestone 7 needs them,
    /// kept in this same save file rather than a second one per the spec's save-system note.</summary>
    [Serializable]
    public class SettingsData
    {
        public bool SoundOn = true;
        public bool MusicOn = true;
        public int ControlScheme; // 0 = Tilt, 1 = Joystick - int rather than the Gameplay enum so Persistence never needs to reference the Gameplay assembly.
    }

    /// <summary>
    /// The single JSON document SaveSystem reads/writes. One combined file (per the project
    /// spec's save-system note) rather than separate files per concern - level progress now,
    /// settings joins it in milestone 7.
    /// </summary>
    [Serializable]
    public class GameSaveData
    {
        public Dictionary<int, LevelProgressEntry> LevelProgress = new Dictionary<int, LevelProgressEntry>();
        public SettingsData Settings = new SettingsData();
    }
}
