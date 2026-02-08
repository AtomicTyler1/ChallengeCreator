# ChallengeCreator

This is a semi feature rich peak challenge creator!

This mod lets you create your own challenges that people might accidentally break the rules
of or hard to pull off in vanilla peak.

# EVERYONE NEEDS THE MOD TO FUNCTION AS INTENDED.

Some elements will not function as the challenge maker intended with some features disabled.

## How do I create my own chllange?

I have tried my best to make a user friendly tool on my website!
> Go to https://atomictyler.dev/#peakPresets to get started!

## How do I apply the challenge?

After you get your value from the website or you have made your own, click `Copy Final Config`.

Go to the config section of your mod manager, look up a tutorial on how to do this if you cannot find it.
If you download mods manually, you can also look up a tutorial for this for any BepInEx mod.

1. Change the preset to `Custom` if not already.
2. Paste your config into the `challengeCustom` section.
3. Play your challenge

## What can I actually do?

Here is the current list:

<details>
<summary>Expand to see the list</summary>

```cs
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
public bool noLuggages { get; set; } = false; // If true, you cannot open luggages.
public bool noAncientStatues { get; set; } = false; // If true, you cannot interact with ancient statues at all.
public bool noCampfireHealAndMorale { get; set; } = false; // If true, you cannot heal or gain morale from campfires.
public bool temporaryStatusesDecay { get; set; } = true; // If true, the statuses that normally decay will decay like cold, head and poison.
public List<int> requiredBadges { get; set; } = new List<int>(); // List of badge IDs that are required to have a valid run
```

</details>

## How can I write a config manually?

Well, its simple once you get the hang of it, but beware of itemIds!
For a list of [item Ids you can go here](https://gist.githubusercontent.com/AtomicTyler1/913a40238b453d557cb1073fd4c05a83/raw/0802ccd517ba8a052631ea7ba0fd14d876edf48b/peak_list.json)

For actually writing the json config, it follows this structure:

`{"Name": "Example", "Creators": "@you", "Notes": "This is an example!"}`

> NOTE: You do not need every value, you dont even need a name, creator or note.

Please note that using the list above is necassary, you need to get the capitals 100% accurate.
Here are all the data types and example on how to use them.

- List: `{"disallowedItems": [13, 124, 91, 92, 39, 94]}` -- This disables Bing Bong and all berrynana's
- Bool: `{"noJumping": true}` -- This disables jumping
- Int: `{"minimumPlayers": 2}` -- You cannot start the run until all players have joined
- String: `{"Name": "Example"}` -- Sets the name of the challenge to show

As you might have noticed, some items have duplicate types/names and therefor have mutliple itemIds.
It is recommended you use the public list found [here](https://gist.githubusercontent.com/AtomicTyler1/913a40238b453d557cb1073fd4c05a83/raw/0802ccd517ba8a052631ea7ba0fd14d876edf48b/peak_list.json)

# This mod doesnt have a feature I want!

I am all ears for suggestions, please find my thread on the [Peak Modding Discord](https://discord.gg/SAw86z24rB) or ping me! (@atomictyler)
You can also find me on the official peak discord, you can also ping me there.

# I want to request my challenge be a preset!

After every couple challenge preset submissions I will gladly add unique ones!
Again, if you want to submit one, follow the process above for adding a feature. Just please send me the challenge JSON.