using EFT;
using Newtonsoft.Json;
using SPT.Reflection.Patching;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using EFT.Quests;
using EFT.UI;
using WTTArmory.Models;
using UnityEngine;
using WTTArmory.Classes;

namespace WTTArmory.Patches
{

    internal class GameWorldOnGameStartedPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GameWorld).GetMethod(nameof(GameWorld.OnGameStarted));
        }
        
        [PatchPostfix]
        private static void PatchPostfix(GameWorld __instance)
        {
            try
            {
                LogHelper.LogDebug("GameWorldOnGameStartedPatch triggered");
                
                if (AssetLoader._spawnConfigs == null)
                {
                    AssetLoader.LoadSpawnConfigurations();
                }

                var player = __instance.MainPlayer;
                string locationID = __instance.LocationId;
                if (player?.Profile?.QuestsData == null) return;
                if (locationID == null) return;

                foreach (var config in AssetLoader._spawnConfigs)
                {
                    AssetLoader.ProcessSpawnConfig(player, config, locationID);
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"System error: {ex}");
            }
        }

    }


}