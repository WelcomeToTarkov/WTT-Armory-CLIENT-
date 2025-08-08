using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Comfort.Common;
using EFT;
using EFT.Quests;
using EFT.UI;
using Newtonsoft.Json;
using UnityEngine;
using WTTArmory.Models;

namespace WTTArmory.Classes;

    public static class AssetLoader
    {
        private static readonly Dictionary<string, AssetBundle> _loadedBundles = new();
        private static readonly Assembly _assembly = Assembly.GetExecutingAssembly();
        private static readonly string _resourcePrefix = $"{_assembly.GetName().Name}.Resources.";
        
        public static List<SpawnConfig> _spawnConfigs = null;

        public static void LoadSpawnConfigurations()
        {
            try
            {
                _spawnConfigs = new List<SpawnConfig>();
                var allConfigs = new List<SpawnConfig>();
                var resourceNames = _assembly.GetManifestResourceNames()
                    .Where(name => name.StartsWith(_resourcePrefix + "Configs.") && name.EndsWith(".json"));

                foreach (var resourceName in resourceNames)
                {
                    try
                    {
                        using var stream = _assembly.GetManifestResourceStream(resourceName);
                        using var reader = new StreamReader(stream);
                        var json = reader.ReadToEnd();
                        var fileConfigs = JsonConvert.DeserializeObject<List<SpawnConfig>>(json);
                        
                        if (fileConfigs != null)
                        {
                            allConfigs.AddRange(fileConfigs);
                        }
                    }
                    catch (JsonException jex)
                    {
                        LogHelper.LogError($"JSON error in {resourceName}: {jex.Message}");
                    }
                    catch (Exception ex)
                    {
                        LogHelper.LogError($"Error loading {resourceName}: {ex.Message}");
                    }
                }

                _spawnConfigs = allConfigs;
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"Config loading failed: {ex}");
                _spawnConfigs = new List<SpawnConfig>();
            }
        }

    public static void ProcessSpawnConfig(Player player, SpawnConfig config, string locationID)
    {
        try
        {
            if (string.IsNullOrEmpty(config.PrefabName) || 
                string.IsNullOrEmpty(config.BundleName) || 
                string.IsNullOrEmpty(config.LocationID))
            {
                LogHelper.LogDebug($"[WTT-Armory] Invalid config: {JsonConvert.SerializeObject(config)}");
                return;
            }

            // Location check
            if (!locationID.Equals(config.LocationID, StringComparison.OrdinalIgnoreCase)) 
                return;

            // Get quest reference
            QuestDataClass quest = null;
            if (!string.IsNullOrEmpty(config.QuestId))
            {
                quest = player.Profile.QuestsData.FirstOrDefault(q => q.Id == config.QuestId);
            }

            // Evaluate conditions (all must pass)
            if (!EvaluateConditions(player, quest, config))
            {
                LogHelper.LogDebug($"[WTT-Armory] Conditions not met for {config.PrefabName}");
                return;
            }

            // Load and spawn prefab
            var prefab = LoadPrefabFromBundle(config.BundleName, config.PrefabName);
            if (prefab == null) return;

            var rotation = Quaternion.Euler(config.Rotation);
            SpawnPrefab(prefab, config.Position, rotation);
            LogHelper.LogDebug($"[WTT-Armory] Spawned {config.PrefabName}");
        }
        catch (Exception ex)
        {
            LogHelper.LogError($"[WTT-Armory] Config processing failed: {ex}");
        }
    }

    private static bool EvaluateConditions(Player player, QuestDataClass quest, SpawnConfig config)
    {
        // Quest existence check
        if (config.QuestMustExist.HasValue)
        {
            bool exists = quest != null;
            if (exists != config.QuestMustExist.Value)
            {
                LogHelper.LogDebug($"[CONDITION] Quest existence check failed. Expected: {config.QuestMustExist}, Actual: {exists}");
                return false;
            }
        }
        // Required quest statuses (multiple)
        if (config.RequiredQuestStatuses != null && config.RequiredQuestStatuses.Count > 0)
        {
            if (quest == null)
            {
                LogHelper.LogDebug($"[CONDITION] Required statuses but quest doesn't exist");
                return false;
            }
        
            bool anyMatch = false;
            List<string> validStatuses = new List<string>();
        
            foreach (var statusStr in config.RequiredQuestStatuses)
            {
                if (Enum.TryParse<EQuestStatus>(statusStr, out var requiredStatus))
                {
                    validStatuses.Add(statusStr);
                    if (quest.Status == requiredStatus)
                    {
                        anyMatch = true;
                    }
                }
            }
        
            if (!anyMatch)
            {
                LogHelper.LogDebug($"[CONDITION] None of required statuses matched: {string.Join(", ", validStatuses)}. Actual: {quest.Status}");
                return false;
            }
        }

        // Excluded quest statuses (multiple)
        if (config.ExcludedQuestStatuses != null && config.ExcludedQuestStatuses.Count > 0)
        {
            if (quest != null)
            {
                foreach (var statusStr in config.ExcludedQuestStatuses)
                {
                    if (Enum.TryParse<EQuestStatus>(statusStr, out var excludedStatus))
                    {
                        if (quest.Status == excludedStatus)
                        {
                            LogHelper.LogDebug($"[CONDITION] Excluded status matched: {excludedStatus}");
                            return false;
                        }
                    }
                }
            }
        }

        // Required item in inventory
        if (!string.IsNullOrEmpty(config.RequiredItemInInventory))
        {
            bool hasItem = player.Profile.Inventory.AllRealPlayerItems
                .Any(item => item.TemplateId == config.RequiredItemInInventory);
            
            if (!hasItem)
            {
                LogHelper.LogDebug($"[CONDITION] Missing required item: {config.RequiredItemInInventory}");
                return false;
            }
        }

        // Required level
        if (config.RequiredLevel.HasValue)
        {
            if (player.Profile.Info.Level < config.RequiredLevel.Value)
            {
                LogHelper.LogDebug($"[CONDITION] Level too low. Required: {config.RequiredLevel}, Actual: {player.Profile.Info.Level}");
                return false;
            }
        }

        // Required faction
        if (!string.IsNullOrEmpty(config.RequiredFaction))
        {
            string playerFaction = player.Profile.Side.ToString();
            if (!playerFaction.Equals(config.RequiredFaction, StringComparison.OrdinalIgnoreCase))
            {
                LogHelper.LogDebug($"[CONDITION] Wrong faction. Required: {config.RequiredFaction}, Actual: {playerFaction}");
                return false;
            }
        }
        
        // Enhanced linked quest condition
        if (!string.IsNullOrEmpty(config.LinkedQuestId))
        {
            var linkedQuest = player.Profile.QuestsData.FirstOrDefault(q => q.Id == config.LinkedQuestId);
        
            // Existence check
            if (config.LinkedQuestMustExist.HasValue)
            {
                bool linkedExists = linkedQuest != null;
                if (linkedExists != config.LinkedQuestMustExist.Value)
                {
                    LogHelper.LogDebug($"[CONDITION] Linked quest existence check failed. Expected: {config.LinkedQuestMustExist}, Actual: {linkedExists}");
                    return false;
                }
            }
            // Linked required statuses (multiple)
            if (config.LinkedRequiredStatuses != null && config.LinkedRequiredStatuses.Count > 0)
            {
                if (linkedQuest == null)
                {
                    LogHelper.LogDebug($"[CONDITION] Required linked statuses but quest doesn't exist");
                    return false;
                }
            
                bool anyMatch = false;
                List<string> validStatuses = new List<string>();
            
                foreach (var statusStr in config.LinkedRequiredStatuses)
                {
                    if (Enum.TryParse<EQuestStatus>(statusStr, out var requiredStatus))
                    {
                        validStatuses.Add(statusStr);
                        if (linkedQuest.Status == requiredStatus)
                        {
                            anyMatch = true;
                        }
                    }
                }
            
                if (!anyMatch)
                {
                    LogHelper.LogDebug($"[CONDITION] None of linked required statuses matched: {string.Join(", ", validStatuses)}. Actual: {linkedQuest?.Status}");
                    return false;
                }
            }
        
            // Linked excluded statuses (multiple)
            if (config.LinkedExcludedStatuses != null && config.LinkedExcludedStatuses.Count > 0)
            {
                if (linkedQuest != null)
                {
                    foreach (var statusStr in config.LinkedExcludedStatuses)
                    {
                        if (Enum.TryParse<EQuestStatus>(statusStr, out var excludedStatus))
                        {
                            if (linkedQuest.Status == excludedStatus)
                            {
                                LogHelper.LogDebug($"[CONDITION] Linked excluded status matched: {excludedStatus}");
                                return false;
                            }
                        }
                    }
                }
            }
        }
        // Boss spawn detection
        if (!string.IsNullOrEmpty(config.RequiredBossSpawned))
        {
            if (!CheckBossSpawned(config.RequiredBossSpawned))
            {
                LogHelper.LogDebug($"[CONDITION] Required boss not spawned: {config.RequiredBossSpawned}");
                return false;
            }
        }


        // All conditions passed
        return true;
    }  
    private static bool CheckBossSpawned(string bossName)
    {
        try
        {
            var gameWorld = Singleton<GameWorld>.Instance;
            if (gameWorld == null)
            {
                LogHelper.LogDebug("[BOSS] GameWorld instance not found");
                return false;
            }

            // Search through all alive bots
            foreach (var player in gameWorld.AllAlivePlayersList)
            {
                // Skip main player
                if (player.IsYourPlayer) 
                    continue;

                // Check if this is a boss
                if (player.AIData?.BotOwner?.Profile?.Info?.Settings?.Role == null)
                    continue;

                // Get bot role name (e.g., "bossKilla", "bossGlukhar")
                string roleName = player.AIData.BotOwner.Profile.Info.Settings.Role.ToString();

                // Compare with requested boss name
                if (roleName.Equals(bossName, StringComparison.OrdinalIgnoreCase))
                {
                    LogHelper.LogDebug($"[BOSS] Found {bossName} at {player.Transform.position}");
                    return true;
                }
            }

            LogHelper.LogDebug($"[BOSS] {bossName} not found in raid");
            return false;
        }
        catch (Exception ex)
        {
            LogHelper.LogError($"[BOSS] Detection failed: {ex}");
            return false;
        }
    }
    public static GameObject LoadPrefabFromBundle(string bundleName, string assetName)
    {
        try
        {
            // Check cache first
            if (_loadedBundles.TryGetValue(bundleName, out var cachedBundle))
            {
                return LoadAssetFromBundle(cachedBundle, assetName, bundleName);
            }

            // Load from embedded resources
            var resourceName = $"{_resourcePrefix}Bundles.{bundleName}";
            using var stream = _assembly.GetManifestResourceStream(resourceName);
                
            if (stream == null)
            {
                LogHelper.LogError($"[ASSET LOADER] Bundle not found: {resourceName}");
                return null;
            }

            // Read into byte array
            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            var bundleBytes = ms.ToArray();

            // Load bundle
            var bundle = AssetBundle.LoadFromMemory(bundleBytes);
            if (bundle == null)
            {
                LogHelper.LogError($"[ASSET LOADER] Failed to load bundle: {bundleName}");
                return null;
            }

            _loadedBundles[bundleName] = bundle;
            return LoadAssetFromBundle(bundle, assetName, bundleName);
        }
        catch (Exception ex)
        {
            LogHelper.LogError($"[ASSET LOADER] Critical error: {ex}");
            return null;
        }
    }
    private static GameObject LoadAssetFromBundle(AssetBundle bundle, string assetName, string bundleName)
    {
        var prefab = bundle.LoadAsset<GameObject>(assetName);
        if (prefab == null)
        {
            LogHelper.LogError($"[ASSET LOADER] Prefab '{assetName}' not found in {bundleName}");
        }
        return prefab;
    }
        public static void SpawnPrefab(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            try
            {
                UnityEngine.Object.Instantiate(prefab, position, rotation);
                LogHelper.LogDebug($"[SPAWNER] Created {prefab.name} at {position}");
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"[SPAWNER] Instantiation failed: {ex}");
            }
        }

        public static void UnloadAllBundles()
        {
            if (_loadedBundles.Count == 0)
            {
                return;
            }
            foreach (var bundle in _loadedBundles.Values)
            {
                bundle.Unload(true);
            }
            _loadedBundles.Clear();
        }
    }
    
    