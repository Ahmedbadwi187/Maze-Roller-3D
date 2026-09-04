using System;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace MazeRoller3D.Persistence
{
    /// <summary>
    /// Lightweight JSON-file save system (Newtonsoft.Json), per the project spec's persistence
    /// approach. Generic over the save-data type so it's reusable, though the project only
    /// ever stores one <see cref="GameSaveData"/> in practice.
    ///
    /// <see cref="OverrideFilePathForTests"/> exists purely so edit-mode tests can point this
    /// at a throwaway temp file instead of the player's real save (Application.persistentDataPath) -
    /// production code should never set it.
    /// </summary>
    public static class SaveSystem
    {
        public static string OverrideFilePathForTests;

        private static string FilePath => OverrideFilePathForTests ?? Path.Combine(Application.persistentDataPath, "save.json");

        public static T Load<T>() where T : new()
        {
            try
            {
                if (!File.Exists(FilePath)) return new T();
                var json = File.ReadAllText(FilePath);
                return JsonConvert.DeserializeObject<T>(json) ?? new T();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"SaveSystem: failed to load '{FilePath}', starting with fresh data. {e}");
                return new T();
            }
        }

        public static void Save<T>(T data)
        {
            try
            {
                var directory = Path.GetDirectoryName(FilePath);
                if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

                var json = JsonConvert.SerializeObject(data, Formatting.Indented);
                File.WriteAllText(FilePath, json);
            }
            catch (Exception e)
            {
                Debug.LogError($"SaveSystem: failed to save to '{FilePath}'. {e}");
            }
        }
    }
}
