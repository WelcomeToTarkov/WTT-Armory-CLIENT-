
using System;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using EFT.Console.Core;
using EFT.UI;

namespace WTTArmory.Classes;

public class CommandProcessor
{
    public void RegisterCommandProcessor()
    {
        ConsoleScreen.Processor.RegisterCommand("clear", delegate ()
        {
            MonoBehaviourSingleton<PreloaderUI>.Instance.Console.Clear();
        });
        if (Plugin.DebugMode.Value)
        {
            ConsoleScreen.Processor.RegisterCommand("GetPlayerWorldStats",
                delegate() { PlayerWorldStats.GetPlayerWorldStats(); });
            ConsoleScreen.Processor.RegisterCommand("EnterEditMode",
                delegate() { SpawnCommands.StartEditMode(); });
            ConsoleScreen.Processor.RegisterCommand("ExitEditMode",
                delegate() { SpawnCommands.ExitEditMode(); });
            ConsoleScreen.Processor.RegisterCommand("ExportSpawnedObjectInfo",
                delegate() { SpawnCommands.ExportSpawnedObjectsLocations();
            });

            ConsoleScreen.Processor.RegisterCommandGroup<AdvancedConsoleCommands>();
        }
        
    }
    
}

public class AdvancedConsoleCommands
{
    [ConsoleCommand("SpawnObject",
        "Spawn Static Object using bundle name, prefab name",
        "<String>, <String>", "", new string[] { })]
    public static void SpawnObject(string bundleName, string prefabName)
    {
        SpawnCommands.SpawnObject(bundleName, prefabName);
    }
}
