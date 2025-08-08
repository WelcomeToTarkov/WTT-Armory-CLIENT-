using System;
using System.Linq;
using System.Reflection;
using EFT;
using EFT.Animations;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace WTTArmory.Patches
{
    public class UpdateWeaponVariablesPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            playerField = AccessTools.Field(typeof(Player.FirearmController), "_player");
            fcField = AccessTools.Field(typeof(ProceduralWeaponAnimation), "_firearmController");
            return typeof(ProceduralWeaponAnimation).GetMethod("UpdateWeaponVariables", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void PatchPostfix(ProceduralWeaponAnimation __instance)
        {
            Player.FirearmController firearmController = (Player.FirearmController)UpdateWeaponVariablesPatch.fcField.GetValue(__instance);
            bool flag = firearmController == null;
            if (!flag)
            {
                Player player = (Player)UpdateWeaponVariablesPatch.playerField.GetValue(firearmController);
                bool flag2 = player != null && player.IsYourPlayer &&  player.MovementContext.CurrentState.Name != (EPlayerState)21;
                if (flag2)
                {
                    Plugin.IsAN94 = Plugin.GunIDs.Contains(firearmController.Weapon.TemplateId.ToString());
                }
            }
        }

        private static FieldInfo playerField;

        private static FieldInfo fcField;
    }
}