using EFT;
using EFT.UI;

namespace WTTArmory.Classes;

public static class PlayerWorldStats
{
    public static void GetPlayerWorldStats()
    {
        if (Plugin.Player != null)
        {
            LogPlayerStats("Player", Plugin.Player);
        }
        else
        {
            LogHelper.LogError("Player is null. You aren't in raid or hideout.");
        }
    }

    private static void LogPlayerStats(string playerType, Player player)
    {
        LogHelper.LogDebug($"{playerType} Position X: {player.Transform.position.x} Y: {player.Transform.position.y} Z: {player.Transform.position.z}");
        LogHelper.LogDebug($"{playerType} Rotation X: {player.gameObject.transform.rotation.eulerAngles.x} Y: {player.gameObject.transform.rotation.eulerAngles.y} Z: {player.gameObject.transform.rotation.eulerAngles.z}");
    }
}