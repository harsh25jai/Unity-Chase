namespace Core.Testing
{
    /// <summary>
    /// Placeholder for CheckpointManager to satisfy ReloadStressTest requirements.
    /// In a real project, this would be part of the Checkpoint System.
    /// </summary>
    public static class CheckpointManager
    {
        public static string currentCheckpoint = "None";

        public static void Reset()
        {
            currentCheckpoint = "None";
        }
    }
}
