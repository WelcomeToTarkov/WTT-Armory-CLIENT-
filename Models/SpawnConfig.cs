using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace WTTArmory.Models;

public class SpawnConfig
{
    // Basic spawn information
    [JsonProperty("QuestId")]
    public string QuestId { get; set; }
    
    [JsonProperty("LocationID")]
    public string LocationID { get; set; }
    
    [JsonProperty("BundleName")]
    public string BundleName { get; set; }
    
    [JsonProperty("PrefabName")]
    public string PrefabName { get; set; }
    
    [JsonProperty("Position")]
    public Vector3 Position { get; set; } = Vector3.zero;
    
    [JsonProperty("Rotation")]
    public Vector3 Rotation { get; set; } = Vector3.zero;
    
    [JsonProperty("RequiredQuestStatuses")]
    public List<string> RequiredQuestStatuses { get; set; } = new List<string>();
    
    [JsonProperty("ExcludedQuestStatuses")]
    public List<string> ExcludedQuestStatuses { get; set; } = new List<string>();
    
    [JsonProperty("QuestMustExist")]
    public bool? QuestMustExist { get; set; }
    
    [JsonProperty("LinkedQuestId")]
    public string LinkedQuestId { get; set; }
    
    [JsonProperty("LinkedRequiredStatuses")]
    public List<string> LinkedRequiredStatuses { get; set; } = new List<string>();
    
    [JsonProperty("LinkedExcludedStatuses")]
    public List<string> LinkedExcludedStatuses { get; set; } = new List<string>();
    
    [JsonProperty("LinkedQuestMustExist")]
    public bool? LinkedQuestMustExist { get; set; }
    
    [JsonProperty("RequiredItemInInventory")]
    public string RequiredItemInInventory { get; set; }
    
    [JsonProperty("RequiredLevel")]
    public int? RequiredLevel { get; set; }
    
    [JsonProperty("RequiredFaction")]
    public string RequiredFaction { get; set; }
    
    [JsonProperty("RequiredBossSpawned")]
    public string RequiredBossSpawned { get; set; }
}