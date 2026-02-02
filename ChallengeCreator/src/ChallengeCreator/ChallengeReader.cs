using System;
using Newtonsoft.Json;

namespace ChallengeCreator;

internal class ChallengeReader
{
    public static CurrentChallenge currentChallenge = new CurrentChallenge();
    public static bool isPreset()
    {
        return Plugin.challengePreset.Value != "Custom";
    }

    public static void GetCurrentChallenge()
    {
        bool customPreset = Plugin.challengePreset.Value == "Custom";
        string challengeJSON = customPreset ? Plugin.challengeCustom.Value : Plugin.presets[Plugin.challengePreset.Value];

        if (string.IsNullOrWhiteSpace(challengeJSON) || challengeJSON == "{}")
        {
            Plugin.Log.LogWarning("Challenge JSON is empty. Using default settings.");
            currentChallenge = new CurrentChallenge();
            return;
        }

        try
        {
            challengeJSON = challengeJSON.Replace('\'', '\"');

            var settings = new JsonSerializerSettings
            {
                MissingMemberHandling = MissingMemberHandling.Ignore,
                Error = (sender, args) =>
                {
                    Plugin.Log.LogError($"JSON Parsing Error at {args.ErrorContext.Path}: {args.ErrorContext.Error.Message}");
                    args.ErrorContext.Handled = true;
                }
            };

            CurrentChallenge? decoded = JsonConvert.DeserializeObject<CurrentChallenge>(challengeJSON, settings);

            if (decoded != null)
            {
                currentChallenge = decoded;
                Plugin.Log.LogInfo($"Successfully loaded challenge: {currentChallenge.Name}");
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError("--- CHALLENGE LOAD ERROR ---");
            Plugin.Log.LogError($"Failed to parse challenge JSON. Ensure your formatting is correct.");
            Plugin.Log.LogError($"Error Details: {ex.Message}");
            if (customPreset)
                Plugin.Log.LogError($"Current Custom JSON: {challengeJSON}");
            Plugin.Log.LogError("----------------------------");

            currentChallenge = new CurrentChallenge();
        }
    }

    public static void ResetToDefault()
    {
        currentChallenge = new CurrentChallenge();
    }
}
