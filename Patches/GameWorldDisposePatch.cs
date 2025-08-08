using System.Reflection;
using EFT;
using SPT.Reflection.Patching;
using WTTArmory.Classes;

namespace WTTArmory.Patches;

internal class GameWorldDisposePatch : ModulePatch
{
    protected override MethodBase GetTargetMethod() => typeof(GameWorld).GetMethod(nameof(GameWorld.Dispose));
    [PatchPostfix]
    private static void PatchPostfix()
    {
        AssetLoader.UnloadAllBundles();
    }
}