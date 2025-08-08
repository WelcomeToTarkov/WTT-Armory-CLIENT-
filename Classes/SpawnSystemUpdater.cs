using System;
using UnityEngine;
using WTTArmory.Classes;

namespace WTTArmory
{
    public class SpawnSystemUpdater : MonoBehaviour
    {
        private void Update()
        {
            try
            {
                SpawnCommands.UpdateEditMode();
                
                if (SpawnCommands.IsEditing)
                {
                    if (SpawnCommands.LastSpawnedObject != null)
                    {
                        Vector3 pos = SpawnCommands.LastSpawnedObject.transform.position;
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.LoggerInstance.LogError($"Updater failed: {ex}");
            }
        }
    }
}