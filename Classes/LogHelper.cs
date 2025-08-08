using BepInEx.Logging;

namespace WTTArmory.Classes
{
    public static class LogHelper
    {
        public static void LogDebug(string message)
        {
            if (Plugin.DebugMode.Value)
            {
                Plugin.LoggerInstance.LogInfo($"[DEBUG] {message}");
            }
        }
        
        public static void LogError(string message)
        {
            Plugin.LoggerInstance.LogError(message);
        }
        
        public static void LogWarning(string message)
        {
            Plugin.LoggerInstance.LogWarning(message);
        }
        
        public static void LogAlways(string message)
        {
            Plugin.LoggerInstance.LogMessage(message);
        }
    }
}