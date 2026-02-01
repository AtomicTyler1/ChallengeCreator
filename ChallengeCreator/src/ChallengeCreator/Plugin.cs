using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System.Collections.Generic;

namespace ChallengeCreator
{
    [BepInAutoPlugin]
    public partial class Plugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log { get; private set; } = null!;

        public static ConfigEntry<string> challengeCustom;
        public static ConfigEntry<string> challengePreset;
        public static ConfigEntry<bool> debugItemIDs;
        public static ConfigEntry<bool> showMessage;

        public static Harmony harmony = new Harmony(Id);

        public static Dictionary<string, string> presets = new Dictionary<string, string>()
        {
            { "Custom", "" },
            { "Itemless", "{'Name': 'Itemless', 'Creators': '@piano.man', 'Notes': 'You are not allowed to use game-breaking glitches.', 'Itemless': true, 'endRunOnCurse': true, 'MinAscent': 0}" },
            { "Crippled", "{'Name': 'Crippled', 'Creators': '@piano.man', 'Notes': 'No game breaking glitches, no scout cannon and no reserve stamina. You cannot jump, sprint or lunge.', 'MinAscent': 0, 'noSprinting': true, 'noJumping': true, 'disallowedItems': [107], 'allowReserveStamina': false}" },
            { "Control-locked", "{'Name': 'Control Locked', 'Creators': '@piano.man', 'Notes': 'No game breaking glitches, no scout cannon.', 'MinAscent': 0, 'controlLockLeftAndRight_Ground': true, 'controlLockForwardAndBackward_Climb': true, 'disallowedItems': [107]}" },
            { "Tick", "{'Name': 'Tick: Shore -> PEAK', 'Creators': '@piano.man, @atomictyler :3', 'Notes': 'No game breaking glitches, you always have a tick, must get leave no trace badge. This must be solo. This is not the same challenge found on Pianos thread!', 'MinAscent': 7, 'noMultiplayer': true, 'disallowedItems': [47], 'alwaysHaveTick': true}" }
        };

        private void Awake()
        {
            Log = Logger;
            Log.LogInfo($"Plugin {Name} is loaded!");

            ConfigDescription presetDescription = new ConfigDescription(
                "Select a premade preset or make your own, to use your own select custom. All premade presets will be available at https://atomictyler.dev/#peakPresets",
                new AcceptableValueList<string>(new List<string>(presets.Keys).ToArray())
            );

            challengePreset = Config.Bind("General", "challengePreset", "Custom", presetDescription);
            challengeCustom = Config.Bind("General", "challengeCustom", "{}", "Custom challenge JSON. Go to https://atomictyler.dev/#peakPresets to make a config value.");
            showMessage = Config.Bind("General", "Show Challenge Warnings", true, "If true, when you try to do something the challenge deems invalid, along with not doing it a message will appear warning you.");
            debugItemIDs = Config.Bind("General", "Debug Item IDs", false, "If true, item IDs will be printed to the log. Useful for making challenges.");

            harmony.PatchAll();
        }
    }
}
