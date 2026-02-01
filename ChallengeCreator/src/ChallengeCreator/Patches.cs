using System.Collections.Generic;
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
    public static HashSet<int> UsedOneTimeUseItems { get; } = new();

    private static bool _usedOneUseInItemless = false;
    private static bool _characterHasTick = false;

    private static readonly HashSet<int> ItemlessBlockedItems = new()
    {
        1,   // ANTI-ROPE SPOOL
        7,   // BANDAGES
        13, 124, // BING BONG
        14, 125, // BINOCULARS
        15, 16, 77, 126, // BUGLE
        17,  // CHAIN LAUNCHER
        18,  // PITON
        23, 61, 74, 113, // COMPASS
        25,  // CURSED SKULL
        29,  // MED KIT
        30,  // CHECKPOINT FLAG
        31,  // BASKETBALL
        34,  // GUIDEBOOK
        37,  // BALLOON BUNCH
        42, 43, // LANTERN
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
        98, 164, // PARASOL
        99,  // FLYING DISC
        100, // RESCUE CLAW
        105, // BALLOON
        106, // DYNAMITE
        107, // SCOUT CANNON
        109, // TORCH
        115, // THE BOOK OF BONES
        116, // CLIMBING CHALK
        163, // SKULL
        165, // SNOWBALL
    };

    [HarmonyPostfix]
    [HarmonyPatch(typeof(RunManager), nameof(RunManager.StartRun))]
    public static void RunStarted(RunManager __instance)
    {
        UsedOneTimeUseItems.Clear();
        _characterHasTick = false;
        _usedOneUseInItemless = false;
        ChallengeReader.GetCurrentChallenge();
        UIUtils.DisplayChallenge(GUIManager.instance);

        if (Plugin.debugItemIDs.Value)
        {
            LogItemDatabase();
        }
    }

    private static void LogItemDatabase()
    {
        var database = ItemDatabase.Instance;
        var itemMap = new Dictionary<string, List<int>>();

        foreach (var item in database.Objects)
        {
            var name = item.GetName().ToUpper();
            var id = item.itemID;

            if (!itemMap.ContainsKey(name))
                itemMap[name] = new List<int>();

            itemMap[name].Add(id);
        }

        var jsonBuilder = new StringBuilder();
        jsonBuilder.AppendLine("{");

        var count = 0;
        foreach (var kvp in itemMap)
        {
            var ids = string.Join(", ", kvp.Value);
            jsonBuilder.Append($"  \"{kvp.Key}\": [{ids}]");

            if (++count < itemMap.Count)
                jsonBuilder.AppendLine(",");
            else
                jsonBuilder.AppendLine();
        }

        jsonBuilder.Append("}");
        Plugin.Log.LogInfo(jsonBuilder.ToString());
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
        if (Challenge.noJumping)
        {
            UIUtils.ChallengeBreakingMessage("You cannot jump!");
            return false;
        }
        return true;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Character), nameof(Character.Start))]
    public static void StartAsSkeleton(Character __instance)
    {
        if (!__instance.IsLocal) return;
        if (Challenge.startSkeleton)
        {
            __instance.data.SetSkeleton(true);
            __instance.refs.customization.refs.SetSkeleton(true, true);
            __instance.refs.customization.HideHuman();
            if (Challenge.endRunOnCurse)
            {
                return;
            }
            __instance.refs.afflictions.AddStatus(CharacterAfflictions.STATUSTYPE.Curse, 0.2f);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(CharacterMovement), nameof(CharacterMovement.SetMovementState))]
    public static void BlockSprintingPostfix(CharacterMovement __instance)
    {
        if (!__instance.character.IsLocal) return;
        if (!Challenge.noSprinting) return;

        if (__instance.character.data.isSprinting)
            UIUtils.ChallengeBreakingMessage("You cannot sprint!");

        __instance.character.data.isSprinting = false;
        __instance.sprintToggleEnabled = false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Character), nameof(Character.UseStamina))]
    public static bool BlockStaminaDrain(Character __instance)
    {
        return !(__instance.IsLocal && Challenge.noSprinting && __instance.data.isSprinting);
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

        var isClimbing = __instance.data.isClimbing || __instance.data.isRopeClimbing;
        var input = __instance.input.movementInput;

        if (isClimbing)
        {
            if (Challenge.controlLockLeftAndRight_Climb) input.y = 0f;
            if (Challenge.controlLockForwardAndBackward_Climb) input.x = 0f;
        }
        else
        {
            if (Challenge.controlLockLeftAndRight_Ground) input.y = 0f;
            if (Challenge.controlLockForwardAndBackward_Ground) input.x = 0f;
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
    public static void TrackOneTimeUseItems(Item __instance)
    {
        if (!__instance.holderCharacter.IsLocal || !SceneManager.GetActiveScene().name.Contains("Level")) return;

        var itemID = __instance.itemID;

        if (Challenge.oneTimeUseItems.Contains(itemID) && itemID != 32 && itemID != 6)
        {
            if (!UsedOneTimeUseItems.Contains(itemID))
            {
                UsedOneTimeUseItems.Add(itemID);
                Plugin.Log.LogInfo($"Added item {itemID} to used one-time use items list");
            }
        }

        if (Challenge.Itemless && itemID != 32 && itemID != 6)
        {
            _usedOneUseInItemless = true;
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Item), nameof(Item.StartUsePrimary))]
    public static bool CheckOneTimeUseItems(Item __instance)
    {
        return CheckItemUsage(__instance, true);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Item), nameof(Item.StartUseSecondary))]
    public static bool CheckOneTimeUseItemsSecondary(Item __instance)
    {
        return CheckItemUsage(__instance, false);
    }

    private static bool CheckItemUsage(Item item, bool isPrimary)
    {
        if (!item.holderCharacter.IsLocal) return true;
        if (!SceneManager.GetActiveScene().name.Contains("Level")) return true;

        var itemID = item.itemID;

        if (itemID == 6 && !Challenge.noBackpack) return true;
        if (itemID == 32) return true;

        if (Challenge.allowedItemsOnly.Count > 0 && !Challenge.allowedItemsOnly.Contains(itemID))
        {
            DropItemWithMessage(item, "The challenge has not added this item id to the allowedItemsOnly!");
            return false;
        }

        if (Challenge.Itemless)
        {
            if (ItemlessBlockedItems.Contains(itemID) || Challenge.disallowedItems.Contains(itemID))
            {
                DropItemWithMessage(item, "You cannot use this item!");
                return false;
            }

            if (_usedOneUseInItemless && itemID != 6 && itemID != 32)
            {
                DropItemWithMessage(item, "Itemless only allows one item use!");
                return false;
            }
        }

        if (Challenge.oneTimeUseItems.Contains(itemID))
        {
            if (UsedOneTimeUseItems.Contains(itemID))
            {
                DropItemWithMessage(item, "You have already used this item once!");
                return false;
            }
        }

        if (Challenge.disallowedItems.Contains(itemID))
        {
            DropItemWithMessage(item, "This item is disallowed in the challenge!");
            return false;
        }

        return true;
    }

    private static void DropItemWithMessage(Item item, string message)
    {
        UIUtils.ChallengeBreakingMessage(message);

        var character = item.holderCharacter;
        var itemTransform = character.data.currentItem.transform;
        var dropPosition = itemTransform.position + Vector3.down * 0.2f;
        var dropVelocity = character.data.currentItem.rig.linearVelocity;
        var dropRotation = itemTransform.rotation;

        character.refs.items.DropItemRpc(
            5f,
            character.refs.items.currentSelectedSlot.Value,
            dropPosition,
            dropVelocity,
            dropRotation,
            item.data,
            false
        );
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Item), nameof(Item.RequestPickup))]
    public static bool ItemPickup(Item __instance, PhotonView characterView)
    {
        if (characterView == null || !SceneManager.GetActiveScene().name.Contains("Level")) return true;
        if (characterView.OwnerActorNr != PhotonNetwork.LocalPlayer.ActorNumber) return true;

        var itemID = __instance.itemID;

        if ((itemID == 6 && !Challenge.noBackpack) || itemID == 32) return true;

        if (Challenge.alwaysHaveTick && itemID == 95) return false;

        if (Challenge.allowedItemsOnly.Count > 0 && !Challenge.allowedItemsOnly.Contains(itemID))
        {
            DenyPickup(__instance, characterView, "The challenge has not added this item id to the allowedItemsOnly list!");
            return false;
        }

        if (Challenge.oneTimeUseItems.Contains(itemID))
        {
            if (UsedOneTimeUseItems.Contains(itemID))
            {
                DenyPickup(__instance, characterView, "You have already used this item once!");
                return false;
            }
        }

        if (Challenge.Itemless)
        {
            if (ItemlessBlockedItems.Contains(itemID))
            {
                DenyPickup(__instance, characterView, "You cannot pick this item up in itemless!");
                return false;
            }

            if (_usedOneUseInItemless)
            {
                DenyPickup(__instance, characterView, "You cannot pick this item as Itemless only allows 1 item.");
                return false;
            }
        }

        if (Challenge.disallowedItems.Contains(itemID))
        {
            DenyPickup(__instance, characterView, "This item is disallowed in the challenge!");
            return false;
        }

        return true;
    }

    private static void DenyPickup(Item item, PhotonView characterView, string message)
    {
        UIUtils.ChallengeBreakingMessage(message);
        item.view.RPC("DenyPickupRPC", characterView.Owner);
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

    [HarmonyPostfix]
    [HarmonyPatch(typeof(CharacterAfflictions), nameof(CharacterAfflictions.AddCurse))]
    public static void RestartRunOnCurse(CharacterAfflictions __instance)
    {
        if (Challenge.endRunOnCurse)
        {
            UIUtils.ChallengeBreakingMessage("You got cursed. The run is invalid.");
        }
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

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Character), nameof(Character.FixedUpdate))]
    public static void ExcessAndTick(Character __instance)
    {
        if (!__instance.IsLocal) return;

        if (!Challenge.allowReserveStamina)
        {
            __instance.data.extraStamina = 0f;
        }

        if (!Challenge.alwaysHaveTick || _characterHasTick) return;
        if (!SceneManager.GetActiveScene().name.Contains("Level")) return;

        foreach (var bugPair in Bugfix.AllAttachedBugs)
        {
            if (bugPair.Value == __instance)
            {
                _characterHasTick = true;
                return;
            }
        }

        var bugObj = PhotonNetwork.Instantiate("BugfixOnYou", Vector3.zero, Quaternion.identity, 0);
        bugObj.GetComponent<PhotonView>().RPC("AttachBug", RpcTarget.All, __instance.photonView.ViewID);
        _characterHasTick = true;
    }
}