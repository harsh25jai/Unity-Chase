using System.IO;
using UnityEngine;

namespace Data.Save.Repository
{
    public class SaveSlot0Repository
    {
        private const string Slot0FileName = "checkpoint_slot0.json";
        private const string TempSuffix = ".tmp";

        public bool LastWriteSuccess { get; private set; }
        public string LastErrorMessage { get; private set; }

        private string GetSlotPath(string playerId = "Player_0")
        {
            // For Slot 0, we use a fixed filename in persistentDataPath.
            // Future MP implementation might prefix with playerId.
            return Path.Combine(Application.persistentDataPath, Slot0FileName);
        }

        /// <summary>
        /// Attempts to write data to Slot 0 atomically.
        /// </summary>
        public void WriteSlot0(string data, string playerId = "Player_0")
        {
            string path = GetSlotPath(playerId);
            string tempPath = path + TempSuffix;

            try
            {
                // 1. Write to temp file first
                File.WriteAllText(tempPath, data);

                // 2. Atomic Replace/Move
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
                File.Move(tempPath, path);

                LastWriteSuccess = true;
                LastErrorMessage = string.Empty;
                Debug.Log($"[SaveSlot0Repository] Slot 0 saved successfully to {path}");
            }
            catch (System.Exception e)
            {
                LastWriteSuccess = false;
                LastErrorMessage = e.Message;
                Debug.LogError($"[SaveSlot0Repository] Failed to save Slot 0: {e.Message}");
                
                // Cleanup temp if it exists
                if (File.Exists(tempPath)) File.Delete(tempPath);
            }
        }

        /// <summary>
        /// Attempts to read data from Slot 0.
        /// </summary>
        public bool TryReadSlot0(out string data, string playerId = "Player_0")
        {
            string path = GetSlotPath(playerId);
            data = string.Empty;

            if (!File.Exists(path))
            {
                return false;
            }

            try
            {
                data = File.ReadAllText(path);
                return true;
            }
            catch (System.Exception e)
            {
                LastErrorMessage = e.Message;
                Debug.LogError($"[SaveSlot0Repository] Failed to read Slot 0: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Checks if a Slot 0 save exists.
        /// </summary>
        public bool DoesSlot0Exist(string playerId = "Player_0")
        {
            return File.Exists(GetSlotPath(playerId));
        }

        /// <summary>
        /// Deletes the Slot 0 save.
        /// </summary>
        public void DeleteSlot0(string playerId = "Player_0")
        {
            string path = GetSlotPath(playerId);
            if (File.Exists(path)) File.Delete(path);
            Debug.Log($"[SaveSlot0Repository] Slot 0 deleted.");
        }
    }
}
