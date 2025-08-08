using System;
using System.Reflection;
using EFT;
using EFT.Animations;
using EFT.InventoryLogic;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace WTTArmory.Patches
{
    public class ShootPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            ShootPatch._playerField = AccessTools.Field(typeof(Player.FirearmController), "_player");
            ShootPatch._fcField = AccessTools.Field(typeof(ProceduralWeaponAnimation), "_firearmController");
            return typeof(ProceduralWeaponAnimation).GetMethod("Shoot", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static void PatchPrefix(ProceduralWeaponAnimation __instance, ref float str)
        {
            Player.FirearmController firearmController = (Player.FirearmController)ShootPatch._fcField.GetValue(__instance);
            bool flag = firearmController == null;
            if (!flag)
            {
                Player player = (Player)ShootPatch._playerField.GetValue(firearmController);
                bool flag2 = player != null && player.IsYourPlayer && Plugin.IsAN94 && player.MovementContext.CurrentState.Name != (EPlayerState)21;
                if (flag2)
                {
                    Plugin.IsFiring = true;
                    Plugin.ShotTimer = 0f;
                    Plugin.RecoilShotCount++;
                    str *= ((firearmController.Item.SelectedFireMode != (Weapon.EFireMode)1 && Plugin.RecoilShotCount <= 2) ? Plugin.BurstRecoilMulti.Value : 1f);
                }
            }
        }

        [PatchPostfix]
        private static void PatchPostFix(ProceduralWeaponAnimation __instance, ref float str)
        {
            Player.FirearmController firearmController = (Player.FirearmController)ShootPatch._fcField.GetValue(__instance);
            bool flag = firearmController == null;
            if (!flag)
            {
                Player player = (Player)ShootPatch._playerField.GetValue(firearmController);
                bool flag2 = player != null && player.IsYourPlayer && Plugin.IsAN94 && player.MovementContext.CurrentState.Name != (EPlayerState)21;
                if (flag2)
                {
                    Plugin.IsFiring = true;
                    Plugin.ShotTimer = 0f;
                    Plugin.ROFShotCount++;
                }
            }
        }

        private static FieldInfo _playerField;

        private static FieldInfo _fcField;
    }
}