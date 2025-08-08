using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using UnityEngine;
using WTTArmory.Classes;
using WTTArmory.Patches;

namespace WTTArmory
{
    [BepInPlugin("WTT-Armory.GrooveypenguinX", "WTT-Armory", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        private GameObject _updaterObject;
        internal static ManualLogSource LoggerInstance { get; private set; }
        internal static ConfigEntry<bool> DebugMode { get; private set; }
        public static CommandProcessor CommandProcessor;
        public static GameWorld GameWorld;
        public static Player Player;
        public static readonly string PluginPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        public static string[] GunIDs = new string[] { "678fe4a4906c7bd23722c71f", "679a6a534f3d279c99b135b9" };
        // Keybinds configuration
        internal static ConfigEntry<KeyboardShortcut> MoveForwardKey { get; private set; }
        internal static ConfigEntry<KeyboardShortcut> MoveBackwardKey { get; private set; }
        internal static ConfigEntry<KeyboardShortcut> MoveLeftKey { get; private set; }
        internal static ConfigEntry<KeyboardShortcut> MoveRightKey { get; private set; }
        internal static ConfigEntry<KeyboardShortcut> MoveUpKey { get; private set; }
        internal static ConfigEntry<KeyboardShortcut> MoveDownKey { get; private set; }
        internal static ConfigEntry<KeyboardShortcut> RotatePitchUpKey { get; private set; }
        internal static ConfigEntry<KeyboardShortcut> RotatePitchDownKey { get; private set; }
        internal static ConfigEntry<KeyboardShortcut> RotateYawLeftKey { get; private set; }
        internal static ConfigEntry<KeyboardShortcut> RotateYawRightKey { get; private set; }
        internal static ConfigEntry<KeyboardShortcut> RotatePitchRollLeftKey { get; private set; }
        internal static ConfigEntry<KeyboardShortcut> RotatePitchRollRightKey { get; private set; }
        internal static ConfigEntry<KeyboardShortcut> RotatePitchRollRightInvertKey { get; private set; }
        internal static ConfigEntry<KeyboardShortcut> RotatePitchRollLeftInvertKey { get; private set; }
        internal static ConfigEntry<KeyboardShortcut> DeleteSelectedObject { get; private set; }
        internal static ConfigEntry<KeyboardShortcut> CycleSpawnedObjects { get; private set; }
        internal static ConfigEntry<KeyboardShortcut> CyclePreviousSpawnedObject { get; private set; }
        internal static ConfigEntry<KeyboardShortcut> ConfirmPositionKey { get; private set; }
        
        // Hyperburst configuration
        public static ConfigEntry<float> BurstROFMulti { get; private set; }
        public static ConfigEntry<float> BurstRecoilMulti { get; private set; }
        public static ConfigEntry<float> ShotResetDelay { get; private set; }
        public static ConfigEntry<float> ShotThreshold { get; private set; }

        // Hyperburst state
        public static bool IsAN94 { get; set; } = false;
        public static bool IsFiring { get; set; } = false;
        public static int RecoilShotCount { get; set; } = 0;
        public static int ROFShotCount { get; set; } = 0;
        public static float ShotTimer { get; set; } = 0f;
        public Player You { get; set; }

        private void Awake()
        {
            LoggerInstance = Logger;
            
            // General configuration with ordering
            DebugMode = Config.Bind(
                "General",
                "DebugMode",
#if DEBUG
                true,
#else
                false,
#endif
                new ConfigDescription("Enable detailed debug logging and controls", null,
                    new ConfigurationManagerAttributes { Order = 0 }
                )
            );

            // Hyperburst configuration with ordering
            BurstROFMulti = Config.Bind(
                "Hyperburst",
                "Burst ROF Multi",
                3f,
                new ConfigDescription("Rate of fire multiplier during hyperburst",
                    new AcceptableValueRange<float>(1f, 4f),
                    new ConfigurationManagerAttributes { Order = 1 }
                )
            );

            BurstRecoilMulti = Config.Bind(
                "Hyperburst",
                "Burst Recoil Multi",
                0.5f,
                new ConfigDescription("Recoil multiplier during hyperburst",
                    new AcceptableValueRange<float>(0.1f, 1f),
                    new ConfigurationManagerAttributes { Order = 2 }
                )
            );

            ShotResetDelay = Config.Bind(
                "Hyperburst",
                "Shot Reset Delay",
                0.05f,
                new ConfigDescription("Time delay after firing to determine if firing has stopped",
                    new AcceptableValueRange<float>(0.01f, 2f),
                    new ConfigurationManagerAttributes { Order = 3 }
                )
            );

            ShotThreshold = Config.Bind(
                "Hyperburst",
                "Hyperburst Shot Threshold",
                1f,
                new ConfigDescription("Shot count when hyperburst ends (adjust for timing issues)",
                    new AcceptableValueRange<float>(0f, 5f),
                    new ConfigurationManagerAttributes { Order = 4 }
                )
            );

            // Keybinds configuration (only in debug mode)
            if (DebugMode.Value)
            {
                LoggerInstance.LogInfo("Debug mode ENABLED");
                CreateKeybindConfigs();
            }

            // Enable patches
            new UpdateWeaponVariablesPatch().Enable();
            new ShootPatch().Enable();
            new GameWorldOnGameStartedPatch().Enable();
        }

        private void CreateKeybindConfigs()
        {
            int orderCounter = 10;

            MoveForwardKey = CreateKeybind("Move Forward", KeyCode.W, orderCounter++);
            MoveBackwardKey = CreateKeybind("Move Backward", KeyCode.S, orderCounter++);
            MoveLeftKey = CreateKeybind("Move Left", KeyCode.A, orderCounter++);
            MoveRightKey = CreateKeybind("Move Right", KeyCode.D, orderCounter++);
            MoveUpKey = CreateKeybind("Move Up", KeyCode.E, orderCounter++);
            MoveDownKey = CreateKeybind("Move Down", KeyCode.Q, orderCounter++);

            RotatePitchUpKey = CreateKeybind("Rotate Pitch Up", KeyCode.Keypad8, orderCounter++);
            RotatePitchDownKey = CreateKeybind("Rotate Pitch Down", KeyCode.Keypad2, orderCounter++);
            RotateYawLeftKey = CreateKeybind("Rotate Yaw Left", KeyCode.Keypad4, orderCounter++);
            RotateYawRightKey = CreateKeybind("Rotate Yaw Right", KeyCode.Keypad6, orderCounter++);

            RotatePitchRollLeftKey = CreateKeybind("Rotate Pitch+Roll Left", KeyCode.Keypad7, orderCounter++);
            RotatePitchRollRightKey = CreateKeybind("Rotate Pitch+Roll Right", KeyCode.Keypad9, orderCounter++);
            RotatePitchRollLeftInvertKey = CreateKeybind("Rotate Pitch+Roll Left Inverted", KeyCode.Keypad1, orderCounter++);
            RotatePitchRollRightInvertKey = CreateKeybind("Rotate Pitch+Roll Right Inverted", KeyCode.Keypad3, orderCounter++);

            DeleteSelectedObject = CreateKeybind("Delete Selected Object", KeyCode.Delete, orderCounter++);
            CycleSpawnedObjects = CreateKeybind("Cycle Spawned Object", KeyCode.Period, orderCounter++);
            CyclePreviousSpawnedObject = CreateKeybind("Cycle Previous Spawned Object", KeyCode.Comma, orderCounter++);
            ConfirmPositionKey = CreateKeybind("Confirm Position", KeyCode.Backslash, orderCounter++);
        }

        private ConfigEntry<KeyboardShortcut> CreateKeybind(string name, KeyCode defaultKey, int order)
        {
            return Config.Bind(
                "Keybinds",
                name,
                new KeyboardShortcut(defaultKey),
                new ConfigDescription($"Key for {name}", null,
                    new ConfigurationManagerAttributes { Order = order }
                )
            );
        }

        private void Update()
        {
            // Update game world references
            if (Singleton<GameWorld>.Instantiated && (GameWorld == null || Player == null))
            {
                GameWorld = Singleton<GameWorld>.Instance;
                Player = GameWorld.MainPlayer;
            }

            // Update Hyperburst logic
            if (You == null)
            {
                if (GameWorld?.MainPlayer is Player mainPlayer && mainPlayer.IsYourPlayer)
                {
                    You = mainPlayer;
                }
            }
            else
            {
                var firearmController = You.HandsController as Player.FirearmController;
                UpdateHyperburst(firearmController);
            }
        }

        private void UpdateHyperburst(Player.FirearmController fc)
        {
            if (fc == null || !IsAN94) return;

            fc.Item.MalfState.OverheatFirerateMultInited = true;
            fc.Item.MalfState.OverheatFirerateMult = 
                (ROFShotCount <= ShotThreshold.Value && fc.Item.SelectedFireMode != Weapon.EFireMode.single) 
                ? BurstROFMulti.Value 
                : 1f;

            if (IsFiring)
            {
                ShotTimer += Time.deltaTime;
                if (!fc.autoFireOn && ShotTimer >= ShotResetDelay.Value)
                {
                    ShotTimer = 0f;
                    IsFiring = false;
                    RecoilShotCount = 0;
                    ROFShotCount = 0;
                }
            }
        }

        internal void Start()
        {
            Init();
        }

        internal void Init()
        {
            if (CommandProcessor == null)
            {
                CommandProcessor = new CommandProcessor();
                CommandProcessor.RegisterCommandProcessor();
            }
            
            if (_updaterObject == null)
            {
                _updaterObject = new GameObject("SpawnSystemUpdater");
                _updaterObject.AddComponent<SpawnSystemUpdater>();
                DontDestroyOnLoad(_updaterObject);
            }
        }
    }
}