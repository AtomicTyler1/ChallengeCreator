using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using HarmonyLib;
using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ChallengeCreator;

[HarmonyPatch]
public static class ChallengeCreatorPatches
{
    public static CurrentChallenge Challenge => ChallengeReader.currentChallenge;

    public static List<int> usedOneTimeUseItems = new List<int>();

    private static bool usedOneUseInItemless = false;

    private static List<int> itemless_100PercentBlocked = new List<int>()
    {
        1,   // ANTI-ROPE SPOOL
        7,   // BANDAGES
        13,  // BING BONG (13 & 124)
        124,
        14,  // BINOCULARS (14 & 125)
        125,
        15,  // BUGLE (15, 126, 16, 77)
        16,
        77,
        126,
        17,  // CHAIN LAUNCHER
        18,  // PITON
        23,  // COMPASS (23, 61, 74, 113)
        61,
        74,
        113,
        25,  // CURSED SKULL
        30,  // CHECKPOINT FLAG
        31,  // BASKETBALL
        34,  // GUIDEBOOK
        37,  // BALLOON BUNCH
        42,  // LANTERN (42 & 43)
        43,
        47,  // ANCIENT IDOL
        49,  // SCROLL
        58,  // PANDORA'S LUNCHBOX
        59,  // PASSPORT
        62,  // PORTABLE STOVE
        63,  // ROPE CANNON
        64,  // ANTI-ROPE CANNON
        65,  // ROPE SPOOL
        67,  // SCOUT EFFIGY
        69,  // CONCH
        70,  // BLOWGUN
        72,  // STONE
        78,  // MEGAPHONE
        98,  // PARASOL (98 & 164)
        164,
        99,  // FLYING DISC
        100, // RESCUE CLAW
        105, // BALLOON
        37, // BALLOON BUNCH
        106, // DYNAMITE
        107, // SCOUT CANNON
        109, // TORCH
        115, // THE BOOK OF BONES
        116, // CLIMBING CHALK
        163, // SKULL
        165,  // SNOWBALL
        29 // MED KIT
    };

    private static bool characterHasTick = false;

    [HarmonyPostfix]
    [HarmonyPatch(typeof(RunManager), nameof(RunManager.StartRun))]
    public static void RunStarted(RunManager __instance)
    {
        usedOneTimeUseItems.Clear();
        characterHasTick = false;
        usedOneUseInItemless = false;
        ChallengeReader.GetCurrentChallenge();
        UIUtils.DisplayChallenge(GUIManager.instance);

        if (Plugin.debugItemIDs.Value)
        {
            ItemDatabase database = ItemDatabase.Instance;
            Dictionary<string, List<int>> itemMap = new Dictionary<string, List<int>>();

            foreach (Item item in database.Objects)
            {
                string name = item.GetName().ToUpper();
                int id = item.itemID;

                if (!itemMap.ContainsKey(name))
                {
                    itemMap[name] = new List<int>();
                }
                itemMap[name].Add(id);
            }

            StringBuilder jsonBuilder = new StringBuilder();
            jsonBuilder.AppendLine("{");

            int count = 0;
            foreach (var kvp in itemMap)
            {
                string ids = string.Join(", ", kvp.Value);
                jsonBuilder.Append($"  \"{kvp.Key}\": [{ids}]");

                if (++count < itemMap.Count)
                {
                    jsonBuilder.AppendLine(",");
                }
                else
                {
                    jsonBuilder.AppendLine("");
                }
            }
            jsonBuilder.Append("}");

            Plugin.Log.LogInfo(jsonBuilder.ToString());
            itemMap.Clear();
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(BoardingPass), nameof(BoardingPass.UpdateAscent))]
    public static void BoardingPassUpdateAscent(BoardingPass __instance)
    {
        UIUtils.UpdateBoardingPass(__instance);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(CharacterMovement), nameof(CharacterMovement.TryToJump))]
    public static bool BlockJump(CharacterMovement __instance)
    {
        if (!__instance.character.IsLocal) return true;

        if (Challenge.noJumping) { UIUtils.ChallengeBreakingMessage("You cannot jump!"); return false; }
        return true;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(CharacterMovement), nameof(CharacterMovement.SetMovementState))]
    public static void BlockSprintingPostfix(CharacterMovement __instance)
    {
        if (!__instance.character.IsLocal) return;

        if (Challenge.noSprinting)
        {
            if (__instance.character.data.isSprinting)
                UIUtils.ChallengeBreakingMessage("You cannot sprint!");
            __instance.character.data.isSprinting = false;
            __instance.sprintToggleEnabled = false;
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Character), nameof(Character.UseStamina))]
    public static bool BlockStaminaDrain(Character __instance)
    {
        if (__instance.IsLocal && Challenge.noSprinting && __instance.data.isSprinting)
        {
            return false;
        }
        return true;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(CharacterClimbing), nameof(CharacterClimbing.RPCA_ClimbJump))]
    public static bool BlockLungeJump(CharacterMovement __instance)
    {
        if (!__instance.character.IsLocal) return true;
        if (Challenge.noJumping)
        {
            UIUtils.ChallengeBreakingMessage("You cannot jump so you cannot lunge!!");
            return false;
        }
        return true;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Character), nameof(Character.CalculateWorldMovementDir))]
    public static void ControlLock(Character __instance)
    {
        if (!__instance.IsLocal) return;

        bool isClimbing = __instance.data.isClimbing || __instance.data.isRopeClimbing;

        if (!isClimbing)
        {
            if (Challenge.controlLockLeftAndRight_Ground)
            {
                __instance.input.movementInput.y = 0f;
            }

            if (Challenge.controlLockForwardAndBackward_Ground)
            {
                __instance.input.movementInput.x = 0f;
            }
        }
        else
        {
            if (Challenge.controlLockLeftAndRight_Climb)
            {
                __instance.input.movementInput.y = 0f;
            }

            if (Challenge.controlLockForwardAndBackward_Climb)
            {
                __instance.input.movementInput.x = 0f;
            }
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(RopeSegment), nameof(RopeSegment.IsInteractible))]
    public static bool DisableRope(RopeSegment __instance, ref bool __result)
    {
        if (Challenge.noJumping || Challenge.DisableRopeTypes)
        {
            __result = false;
            return false;
        }
        return true;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(JungleVine), nameof(JungleVine.IsInteractible))]
    public static bool DisableVine(JungleVine __instance, ref bool __result)
    {
        if (Challenge.noJumping || Challenge.DisableRopeTypes)
        {
            __result = false;
            return false;
        }
        return true;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Item), nameof(Item.FinishCastPrimary))]
    public static void TrackOneUseInItemless(Item __instance)
    {
        if (!__instance.holderCharacter.IsLocal || !SceneManager.GetActiveScene().name.Contains("Level")) return;

        Vector3 vector = __instance.holderCharacter.data.currentItem.transform.position + Vector3.down * 0.2f;
        Vector3 linearVelocity = __instance.holderCharacter.data.currentItem.rig.linearVelocity;

        if (Challenge.Itemless)
        {
            if (__instance.itemID == 32 && __instance.itemID != 6)
            {
                return;
            }
            usedOneUseInItemless = true;
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Item), nameof(Item.StartUsePrimary))]
    public static bool OneTimeUseItems(Item __instance)
    {
        if (!__instance.holderCharacter.IsLocal || !SceneManager.GetActiveScene().name.Contains("Level")) return true;

        Vector3 vector = __instance.holderCharacter.data.currentItem.transform.position + Vector3.down * 0.2f;
        Vector3 linearVelocity = __instance.holderCharacter.data.currentItem.rig.linearVelocity;

        if ((__instance.itemID == 6 && !Challenge.noBackpack) || __instance.itemID == 32)
        {
            return true;
        }

        if (Challenge.allowedItemsOnly.Count > 0)
        {
            if (!Challenge.allowedItemsOnly.Contains(__instance.itemID))
            {
                UIUtils.ChallengeBreakingMessage("The challenge has not added this item id to the allowedItemsOnly!");
                __instance.holderCharacter.refs.items.DropItemRpc(5f, __instance.holderCharacter.refs.items.currentSelectedSlot.Value, vector, linearVelocity, __instance.holderCharacter.data.currentItem.transform.rotation, __instance.data, false);
                return false;
            }
        }

        if (Challenge.Itemless)
        {
            if (itemless_100PercentBlocked.Contains(__instance.itemID) || Challenge.disallowedItems.Contains(__instance.itemID) || (usedOneUseInItemless && (__instance.itemID != 6 || __instance.itemID != 32)))
            {
                UIUtils.ChallengeBreakingMessage("You cannot use this item!");
                __instance.holderCharacter.refs.items.DropItemRpc(5f, __instance.holderCharacter.refs.items.currentSelectedSlot.Value, vector, linearVelocity, __instance.holderCharacter.data.currentItem.transform.rotation, __instance.data, false);
                return false;
            }
        }

        if (Challenge.oneTimeUseItems.Contains(__instance.itemID))
        {
            if (usedOneTimeUseItems.Contains(__instance.itemID))
            {
                UIUtils.ChallengeBreakingMessage("You have already used this item once!");
                __instance.holderCharacter.refs.items.DropItemRpc(5f, __instance.holderCharacter.refs.items.currentSelectedSlot.Value, vector, linearVelocity, __instance.holderCharacter.data.currentItem.transform.rotation, __instance.data, false);
                return false;
            }
            else if (__instance.itemID != 32 && __instance.itemID != 6) // Ignore flare for one-time use tracking
            {
                usedOneTimeUseItems.Add(__instance.itemID);
            }
        }
        return true;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Item), nameof(Item.StartUseSecondary))]
    public static bool OneTimeUseItemsSecondary(Item __instance)
    {
        if (!__instance.holderCharacter.IsLocal || !SceneManager.GetActiveScene().name.Contains("Level")) return true;

        Vector3 vector = __instance.holderCharacter.data.currentItem.transform.position + Vector3.down * 0.2f;
        Vector3 linearVelocity = __instance.holderCharacter.data.currentItem.rig.linearVelocity;

        if ((__instance.itemID == 6 && !Challenge.noBackpack) || __instance.itemID == 32 || __instance.itemID == 48)
        {
            return true;
        }

        if (Challenge.allowedItemsOnly.Count > 0)
        {
            if (!Challenge.allowedItemsOnly.Contains(__instance.itemID))
            {
                UIUtils.ChallengeBreakingMessage("The challenge has not added this item id to the allowedItemsOnly!");
                __instance.holderCharacter.refs.items.DropItemRpc(5f, __instance.holderCharacter.refs.items.currentSelectedSlot.Value, vector, linearVelocity, __instance.holderCharacter.data.currentItem.transform.rotation, __instance.data, false);
                return false;
            }
        }

        if (Challenge.Itemless)
        {
            if (itemless_100PercentBlocked.Contains(__instance.itemID) || Challenge.disallowedItems.Contains(__instance.itemID))
            {
                UIUtils.ChallengeBreakingMessage("You cannot use this item!");
                __instance.holderCharacter.refs.items.DropItemRpc(5f, __instance.holderCharacter.refs.items.currentSelectedSlot.Value, vector, linearVelocity, __instance.holderCharacter.data.currentItem.transform.rotation, __instance.data, false);
                return false;
            }
        }

        if (Challenge.oneTimeUseItems.Contains(__instance.itemID))
        {
            if (usedOneTimeUseItems.Contains(__instance.itemID))
            {
                UIUtils.ChallengeBreakingMessage("You have already used this item once!");
                __instance.holderCharacter.refs.items.DropItemRpc(5f, __instance.holderCharacter.refs.items.currentSelectedSlot.Value, vector, linearVelocity, __instance.holderCharacter.data.currentItem.transform.rotation, __instance.data, false);
                return false;
            }
            else
            {
                usedOneTimeUseItems.Add(__instance.itemID);
            }
        }
        return true;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Bugfix), nameof(Bugfix.RPCA_Remove))]
    public static bool PreventTickRemoval(Bugfix __instance)
    {
        if (Challenge.alwaysHaveTick)
        {
            UIUtils.ChallengeBreakingMessage("You cannot remove the tick!");
            return false;
        }
        return true;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Item), nameof(Item.RequestPickup))]
    public static bool ItemPickup(Item __instance, PhotonView characterView)
    {
        if (characterView == null || !SceneManager.GetActiveScene().name.Contains("Level")) return true;

        if (characterView.OwnerActorNr != PhotonNetwork.LocalPlayer.ActorNumber) return true;

        if ((__instance.itemID == 6 && !Challenge.noBackpack) || __instance.itemID == 32)
        {
            return true;
        }

        if (Challenge.alwaysHaveTick && __instance.itemID == 95)
        {
            return false;
        }

        if (Challenge.allowedItemsOnly.Count > 0)
        {
            if (!Challenge.allowedItemsOnly.Contains(__instance.itemID))
            {
                UIUtils.ChallengeBreakingMessage("The challenge has not added this item id to the allowedItemsOnly list!");
                __instance.view.RPC("DenyPickupRPC", characterView.Owner);
                return false;
            }
        }

        if (Challenge.Itemless)
        {
            if (itemless_100PercentBlocked.Contains(__instance.itemID))
            {
                UIUtils.ChallengeBreakingMessage("You cannot pick this item up in itemless!");
                __instance.view.RPC("DenyPickupRPC", characterView.Owner);
                return false;
            }

            if (usedOneUseInItemless)
            {
                UIUtils.ChallengeBreakingMessage("You cannot pick this item as Itemless only allows 1 item.");
                __instance.view.RPC("DenyPickupRPC", characterView.Owner);
                return false;
            }
        }

        if (Challenge.disallowedItems.Contains(__instance.itemID))
        {
            UIUtils.ChallengeBreakingMessage("This item is disallowed in the challenge!");
            __instance.view.RPC("DenyPickupRPC", characterView.Owner);
            return false;
        }
        return true;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(CharacterAfflictions), nameof(CharacterAfflictions.AddCurse))]
    public static void RestartRunOnCurse(CharacterAfflictions __instance)
    {
        if (Challenge.endRunOnCurse)
        {
            UIUtils.ChallengeBreakingMessage("You got cursed. The run is invalid.");
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Character), nameof(Character.FixedUpdate))]
    public static void ExcessAndTick(Character __instance)
    {
        if (!Challenge.allowReserveStamina)
        {
            __instance.data.extraStamina = 0f;
        }

        if (!Challenge.alwaysHaveTick || !__instance.IsLocal || characterHasTick) return;

        if (!SceneManager.GetActiveScene().name.Contains("Level")) return;

        foreach (var bugPair in Bugfix.AllAttachedBugs)
        {
            if (bugPair.Value == __instance)
            {
                characterHasTick = true;
                return;
            }
        }

        var bugObj = PhotonNetwork.Instantiate("BugfixOnYou", Vector3.zero, Quaternion.identity, 0);
        bugObj.GetComponent<PhotonView>().RPC("AttachBug", RpcTarget.All, __instance.photonView.ViewID);

        characterHasTick = true;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Backpack), nameof(Backpack.Stash))]
    public static bool DisableBackpack(Backpack __instance)
    {
        if (Challenge.noBackpack)
        {
            UIUtils.ChallengeBreakingMessage("You cannot use the backpack!");
            return false;
        }
        return true;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(BackpackWheel), nameof(BackpackWheel.Choose))]
    public static bool DisableBackpackWheel(BackpackWheel __instance)
    {
        if (Challenge.noBackpack && __instance.chosenSlice.Value.isBackpackWear)
        {
            UIUtils.ChallengeBreakingMessage("You cannot use the backpack!");
            return false;
        }
        return true;
    }
}
