using Steamworks;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.UIElements;

namespace ChallengeCreator;

public class CurrentChallenge
{
    public string Name { get; set; } = ""; // You get to set a funny name
    public string Creators { get; set; } = ""; // You can credit yourself and others here
    public string Notes { get; set; } = ""; // Any extra notes such as custom instructions they couldnt have in the config
    public int MinAscent { get; set; } = -1; // Ascent -1 = Tenderfoot
    public bool AllowHigherAscents { get; set; } = true; // If minAscent is like 4, can they do 5, 6 and above? 
    public List<int> disallowedItems { get; set; } = new List<int>(); // List of items that are not allowed
    public List<int> oneTimeUseItems { get; set; } = new List<int>();// List of items that can only be used once
    public List<int> allowedItemsOnly { get; set; } = new List<int>(); // If this list has anything in it, only these items can be used, please include flare!!
    public bool Itemless { get; set; } = false; // Cant use items except flare and 1 use of an item
    public bool DisableRopeTypes { get; set; } = false; // If true, all rope, chains and vines are disabled.
    public bool alwaysHaveTick { get; set; } = false; // You will always have a tick attached to you
    public bool noMultiplayer { get; set; } = false; // You can only play singleplayer
    public int minimumPlayers { get; set; } = 1; // For multiplayer, minimum players required to start the run
    public bool allowReserveStamina { get; set; } = true; // If false all reserver stamina is removed upon gaining it
    public bool controlLockLeftAndRight_Ground { get; set; } = false; // If true, you can only move left and right on the ground
    public bool controlLockForwardAndBackward_Ground { get; set; } = false; // If true, you can only move forward and backward on the ground
    public bool controlLockLeftAndRight_Climb { get; set; } = false; // If true, you can only move left and right whilst climbing
    public bool controlLockForwardAndBackward_Climb { get; set; } = false; // If true, you can only move forward and backward whilst climbing
    public bool noSprinting { get; set; } = false; // If true, you cannot sprint
    public bool noJumping { get; set; } = false; // If true, you cannot jump. This should also disable ropes and chains according to piano's crippled challenge
    public bool endRunOnCurse { get; set; } = false; // If true, the run will end if you get the curse affliction
    public bool noBackpack { get; set; } = false; // If true, you cannot use a backpack
    public bool startSkeleton { get; set; } = false; // If true, you start the run as a skeleton (If you have endRunOnCurse on you will not start with curse.)
}
