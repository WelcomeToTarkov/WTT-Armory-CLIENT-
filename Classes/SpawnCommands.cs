using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Comfort.Common;
using EFT.UI;
using EFT;
using EFT.InputSystem;
using Newtonsoft.Json;
using System.IO;
using WTTArmory.Models;


namespace WTTArmory.Classes
{
    public static class SpawnCommands
    {
        private static GameObject _lastSpawnedObject;
        private static List<SpawnedObjectInfo> _spawnedObjects = new List<SpawnedObjectInfo>();
        private static int _currentSpawnedIndex = -1;
        private static bool _isEditing = false;
        private const float MoveSpeed = 1f; 
        private const float RotateSpeed = 15f;
        private static bool _shiftPressed = false;
        private static bool _altPressed = false;
        public static GameObject LastSpawnedObject => 
            _currentSpawnedIndex >= 0 ? _spawnedObjects[_currentSpawnedIndex].Object : null;

        private static SpawnedObjectInfo CurrentSpawnedInfo => 
            _currentSpawnedIndex >= 0 ? _spawnedObjects[_currentSpawnedIndex] : null;
        private static GameObject _editIndicator;
        public static bool IsEditing => _isEditing;
        private static GamePlayerOwner _gamePlayerOwner;

        public static void SpawnObject(string bundleName, string prefabName, string[] args = null)
        {
            // Get player position
            var player = Singleton<GameWorld>.Instance.MainPlayer;
            if (player == null)
            {
                LogHelper.LogDebug("Player not found. Are you in a raid?");
                return;
            }

            // Calculate spawn position 2 meters in front of player
            Vector3 spawnPosition = player.Transform.position + player.Transform.forward * 3f;
            Quaternion spawnRotation = Quaternion.identity;

            // Parse coordinates if provided
            if (args != null && args.Length >= 3)
            {
                if (float.TryParse(args[0], out float x) &&
                    float.TryParse(args[1], out float y) &&
                    float.TryParse(args[2], out float z))
                {
                    spawnPosition = new Vector3(x, y, z);
                }

                // Parse rotation if provided
                if (args.Length >= 6)
                {
                    if (float.TryParse(args[3], out float rx) &&
                        float.TryParse(args[4], out float ry) &&
                        float.TryParse(args[5], out float rz))
                    {
                        spawnRotation = Quaternion.Euler(rx, ry, rz);
                    }
                }
            }

            // Load and spawn the prefab
            var prefab = AssetLoader.LoadPrefabFromBundle(bundleName, prefabName);
            if (prefab == null)
            {
                LogHelper.LogDebug($"Failed to load prefab: {prefabName} from bundle: {bundleName}");
                return;
            }

            var newObject = GameObject.Instantiate(prefab, spawnPosition, spawnRotation);
            _lastSpawnedObject = newObject;
    
            // Store metadata with the object
            _spawnedObjects.Add(new SpawnedObjectInfo {
                Object = newObject,
                BundleName = bundleName,
                PrefabName = prefabName
            });
            _currentSpawnedIndex = _spawnedObjects.Count - 1;
            LogHelper.LogDebug($"Spawned {prefabName} at {spawnPosition}");
        }


        public static void StartEditMode()
        {
            if (_lastSpawnedObject == null)
            {
                LogHelper.LogDebug("No object to edit. Spawn an object first.");
                return;
            }

            _isEditing = true;
            _gamePlayerOwner = Plugin.Player.GetComponentInChildren<GamePlayerOwner>();
            _gamePlayerOwner.enabled = false;
            
            // In StartEditMode():
            if (_editIndicator == null)
            {
                _editIndicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                _editIndicator.transform.localScale = Vector3.one * 0.3f;
                _editIndicator.GetComponent<Renderer>().material.color = Color.red;
                _editIndicator.GetComponent<Collider>().enabled = false;
    
                // Add visible rotation indicator
                GameObject arrow = GameObject.CreatePrimitive(PrimitiveType.Cube);
                arrow.transform.SetParent(_editIndicator.transform);
                arrow.transform.localPosition = new Vector3(0, 0, 0.5f);
                arrow.transform.localScale = new Vector3(0.1f, 0.1f, 1f);
            }
    
            LogHelper.LogDebug("Entering edit mode. Use WASD to move, Arrow Keys to rotate. Press Enter to confirm.");
            LogHelper.LogDebug("Current position: " + _lastSpawnedObject.transform.position);
        }

        public static void ExitEditMode()
        {
            LogHelper.LogDebug("Exiting edit mode.");
            _isEditing = false;
            _gamePlayerOwner = Plugin.Player.GetComponentInChildren<GamePlayerOwner>();
            _gamePlayerOwner.enabled = true;
    
            // Clean up visual indicator
            if (_editIndicator != null)
            {
                Object.Destroy(_editIndicator);
                _editIndicator = null;
            }
        }
    public static void UpdateEditMode()
    {
        if (!_isEditing || _lastSpawnedObject == null)
        {
            if (_isEditing)
                Plugin.LoggerInstance.LogError("EditMode active but no object!");
            return;
        }

        Transform cameraTransform = Camera.main?.transform;
        if (cameraTransform == null) return;

        // Handle object cycling and deletion
        if (Plugin.CycleSpawnedObjects.Value.BetterIsDown())
        {
            CycleSpawnedObjects();
        }
        else if (Plugin.CyclePreviousSpawnedObject.Value.BetterIsDown())
        {
            CyclePreviousSpawnedObject();
        }
        else if (Plugin.DeleteSelectedObject.Value.BetterIsDown())
        {
            DeleteSelectedObject();
            // Skip movement if we just deleted the object
            if (_lastSpawnedObject == null) return;
        }
        _shiftPressed = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        _altPressed = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);


        // Modifier key-based speed multiplier
        float speedMultiplier = 1f;
        if (_shiftPressed)
        {
            speedMultiplier = 2f;
            if (_altPressed)
            {
                speedMultiplier = 4f;
            }
        }

        // Get input states
        bool forwardPressed = Plugin.MoveForwardKey.Value.BetterIsPressed();
        bool backwardPressed = Plugin.MoveBackwardKey.Value.BetterIsPressed();
        bool leftPressed = Plugin.MoveLeftKey.Value.BetterIsPressed();
        bool rightPressed = Plugin.MoveRightKey.Value.BetterIsPressed();
        bool upPressed = Plugin.MoveUpKey.Value.BetterIsPressed();
        bool downPressed = Plugin.MoveDownKey.Value.BetterIsPressed();

        // Camera-relative movement vectors
        Vector3 moveDirection = Vector3.zero;
        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;
        Vector3 up = Vector3.up; // Use world up for vertical movement

        forward.y = 0;
        right.y = 0;
        forward.Normalize();
        right.Normalize();

        if (forwardPressed) moveDirection += forward;
        if (backwardPressed) moveDirection -= forward;
        if (leftPressed) moveDirection -= right;
        if (rightPressed) moveDirection += right;
        if (upPressed) moveDirection += up;
        if (downPressed) moveDirection -= up;

        if (moveDirection.sqrMagnitude > 1f)
            moveDirection.Normalize();

        _lastSpawnedObject.transform.position += moveDirection * MoveSpeed * speedMultiplier * Time.deltaTime;

        // ROTATION SYSTEM - CORRECTED DIRECTIONS
        Vector3 rotationInput = Vector3.zero;

        // Primary directions
        bool pitchUpPressed = Input.GetKey(Plugin.RotatePitchUpKey.Value.MainKey) && !_shiftPressed && !_altPressed;
        bool pitchDownPressed = Input.GetKey(Plugin.RotatePitchDownKey.Value.MainKey) && !_shiftPressed && !_altPressed;
        bool yawLeftPressed = Input.GetKey(Plugin.RotateYawLeftKey.Value.MainKey) && !_shiftPressed && !_altPressed;
        bool yawRightPressed = Input.GetKey(Plugin.RotateYawRightKey.Value.MainKey) && !_shiftPressed && !_altPressed;
        
        if (pitchUpPressed)       // 8 - Pitch up (forward)
            rotationInput.x += 1;
        if (pitchDownPressed)     // 2 - Pitch down (backward)
            rotationInput.x = 1;
        if (yawLeftPressed)       // 4 - Yaw left (counter-clockwise)
            rotationInput.y -= 1;
        if (yawRightPressed)      // 6 - Yaw right (clockwise)
            rotationInput.y += 1;

        // Diagonal directions
        bool pitchRollLeftKey = Input.GetKey(Plugin.RotatePitchRollLeftKey.Value.MainKey) && !_shiftPressed && !_altPressed;
        bool pitchRollRightKey = Input.GetKey(Plugin.RotatePitchRollRightKey.Value.MainKey) && !_shiftPressed && !_altPressed;
        bool pitchRollLeftInvertKey = Input.GetKey(Plugin.RotatePitchRollLeftInvertKey.Value.MainKey) && !_shiftPressed && !_altPressed;
        bool pitchRollRightInvertKey = Input.GetKey(Plugin.RotatePitchRollRightInvertKey.Value.MainKey) && !_shiftPressed && !_altPressed;

        
        if (pitchRollLeftKey)         // 7 - Forward left
        {
            rotationInput.x -= 1;  // Pitch up
            rotationInput.z += 1;  // Roll left
        }
        if (pitchRollRightKey)        // 9 - Forward right
        {
            rotationInput.x -= 1;  // Pitch up
            rotationInput.z -= 1;  // Roll right
        }
        if (pitchRollLeftInvertKey)   // 1 - Backward left
        {
            rotationInput.x += 1;  // Pitch down
            rotationInput.z += 1;  // Roll left
        }
        if (pitchRollRightInvertKey)  // 3 - Backward right
        {
            rotationInput.x += 1;  // Pitch down
            rotationInput.z -= 1;  // Roll right
        }

        if (rotationInput != Vector3.zero)
        {
            // Apply rotation in world space
            _lastSpawnedObject.transform.Rotate(
                rotationInput.x * RotateSpeed * speedMultiplier * Time.deltaTime,
                rotationInput.y * RotateSpeed * speedMultiplier * Time.deltaTime,
                rotationInput.z * RotateSpeed * speedMultiplier * Time.deltaTime,
                Space.World
            );
        }


        if (Plugin.ConfirmPositionKey.Value.BetterIsDown())
        {
            ExitEditMode();
            LogHelper.LogDebug("Position confirmed!");
            LogHelper.LogDebug($"Position: {_lastSpawnedObject.transform.position}");
            LogHelper.LogDebug($"Rotation: {_lastSpawnedObject.transform.rotation.eulerAngles}");
        }

        if (_editIndicator != null)
        {
            _editIndicator.transform.position = _lastSpawnedObject.transform.position;
            _editIndicator.transform.rotation = Quaternion.identity; // Keep indicator upright
        }
    }

        public static void DeleteSelectedObject()
        {
            if (_spawnedObjects.Count == 0 || _currentSpawnedIndex < 0)
            {
                LogHelper.LogDebug("No object to delete.");
                return;
            }

            var currentInfo = _spawnedObjects[_currentSpawnedIndex];
            GameObject.Destroy(currentInfo.Object);
            _spawnedObjects.RemoveAt(_currentSpawnedIndex);
    
            LogHelper.LogDebug($"Deleted object: {currentInfo.PrefabName}");

            // Update selection
            if (_spawnedObjects.Count > 0)
            {
                _currentSpawnedIndex = Mathf.Clamp(_currentSpawnedIndex, 0, _spawnedObjects.Count - 1);
                _lastSpawnedObject = _spawnedObjects[_currentSpawnedIndex].Object;
            }
            else
            {
                _currentSpawnedIndex = -1;
                _lastSpawnedObject = null;
        
                // Exit edit mode if no objects left
                if (_isEditing)
                {
                    ExitEditMode();
                }
            }
        }
        public static void CycleSpawnedObjects()
        {
            if (_spawnedObjects.Count == 0)
            {
                LogHelper.LogDebug("No spawned objects to cycle.");
                return;
            }

            _currentSpawnedIndex = (_currentSpawnedIndex + 1) % _spawnedObjects.Count;
            SpawnedObjectInfo currentInfo = _spawnedObjects[_currentSpawnedIndex];
            _lastSpawnedObject = currentInfo.Object;
    
            LogHelper.LogDebug($"Switched to object #{_currentSpawnedIndex}: " +
                               $"{currentInfo.PrefabName} [Bundle: {currentInfo.BundleName}]");

            if (_isEditing && _editIndicator != null)
            {
                _editIndicator.transform.position = _lastSpawnedObject.transform.position;
            }
        }

        public static void CyclePreviousSpawnedObject()
        {
            if (_spawnedObjects.Count == 0)
            {
                LogHelper.LogDebug("No spawned objects to cycle.");
                return;
            }

            _currentSpawnedIndex = (_currentSpawnedIndex - 1 + _spawnedObjects.Count) % _spawnedObjects.Count;
            SpawnedObjectInfo currentInfo = _spawnedObjects[_currentSpawnedIndex];
            _lastSpawnedObject = currentInfo.Object;
    
            LogHelper.LogDebug($"Switched to object #{_currentSpawnedIndex}: " +
                               $"{currentInfo.PrefabName} [Bundle: {currentInfo.BundleName}]");

            if (_isEditing && _editIndicator != null)
            {
                _editIndicator.transform.position = _lastSpawnedObject.transform.position;
            }
        }


        public static void ExportSpawnedObjectsLocations()
        {
            if (_spawnedObjects == null || _spawnedObjects.Count == 0)
            {
                LogHelper.LogDebug("No spawned objects to export.");
                return;
            }

            List<SpawnConfig> exportList = new List<SpawnConfig>();

            foreach (var info in _spawnedObjects)
            {
                SpawnConfig config = new SpawnConfig
                {
                    QuestId = null,
                    LocationID = Plugin.Player.Location ?? "unknown_location",
                    BundleName = info.BundleName,  // Directly use stored name
                    PrefabName = info.PrefabName,  // Directly use stored name
                    Position = info.Object.transform.position,
                    Rotation = info.Object.transform.rotation.eulerAngles,
                    RequiredQuestStatuses = new List<string>(),
                    ExcludedQuestStatuses = new List<string>(),
                    QuestMustExist = null,
                    LinkedQuestId = null,
                    LinkedRequiredStatuses = new List<string>(),
                    LinkedExcludedStatuses = new List<string>(),
                    LinkedQuestMustExist = null,
                    RequiredItemInInventory = null,
                    RequiredLevel = null,
                    RequiredFaction = null,
                    RequiredBossSpawned = null
                };

                exportList.Add(config);
            }

            string exportPath = Path.Combine(Plugin.PluginPath, "SpawnedObjectsExport.json");
            File.WriteAllText(exportPath, JsonConvert.SerializeObject(exportList, Formatting.Indented));
    
            LogHelper.LogDebug($"Exported {_spawnedObjects.Count} spawned objects to {exportPath}");
        }

    }
}