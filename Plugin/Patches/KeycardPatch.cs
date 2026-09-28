using System.Reflection;
using Diz.LanguageExtensions;
using EFT;
using EFT.Interactive;
using EFT.InventoryLogic;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace RaidOverhaul.Patches
{
    internal class KeycardPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(KeycardDoor), nameof(KeycardDoor.UnlockOperation));
        }

        [PatchPrefix]
        private static bool PatchPrefix(
            ref Option<UnlockResult> __result,
            KeyComponent key,
            Player player,
            KeycardDoor __instance
        )
        {
            var canInteract = player.MovementContext.CanInteract;
            if (canInteract != null)
            {
                __result = canInteract;
                return false;
            }

            var isAuthorized = key.Template.KeyId == __instance.KeyId || key.Template.KeyId == Helpers.Utils.VipKeycard;
            if (!isAuthorized)
            {
                __result = new UnlockResult(key, null, false);
                return false;
            }

            key.NumberOfUsages++;
            if (key.NumberOfUsages >= key.Template.MaximumNumberOfUsage && key.Template.MaximumNumberOfUsage > 0)
            {
                var discardResult = ItemManipulator.Discard(key.Item, (ItemController)key.Item.Parent.GetOwner(), false);

                if (discardResult.Failed)
                {
                    __result = discardResult.Error;
                    return false;
                }
                __result = new UnlockResult(key, discardResult.Value, true);
                return false;
            }
            __result = new UnlockResult(key, null, true);
            return false;
        }
    }
}
