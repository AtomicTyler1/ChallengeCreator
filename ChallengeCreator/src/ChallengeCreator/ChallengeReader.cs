using ExitGames.Client.Photon;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.Networking;

namespace ChallengeCreator;

internal class ChallengeReader
{
    public static CurrentChallenge currentChallenge = new CurrentChallenge();
    private static string lastLoadedCode = "";
    private static string cachedJson = "";
    private static string publicAnonKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImR1anB4aXdjdHNsYnBjemlxdXFyIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NzA1MzkzMjIsImV4cCI6MjA4NjExNTMyMn0.FKJ3Mei_i3psMBkWbrZ11HwMuNl2h6-wEFloYGRkOnw";

    public static bool isPreset()
    {
        return Plugin.challengePreset.Value != "Custom";
    }

    public static IEnumerator GetCurrentChallengeRoutine(Action onComplete)
    {
        bool customPreset = Plugin.challengePreset.Value == "Custom";
        string input = customPreset ? Plugin.challengeCustom.Value : Plugin.presets[Plugin.challengePreset.Value];

        if (string.IsNullOrWhiteSpace(input) || input == "{}")
        {
            currentChallenge = new CurrentChallenge();
            onComplete?.Invoke();
            yield break;
        }

        string finalJson = "";

        if (input.StartsWith("CHALLENGE_"))
        {
            if (input == lastLoadedCode)
            {
                finalJson = cachedJson;
            }
            else
            {
                string shortId = input.Replace("CHALLENGE_", "").Trim().ToUpper();
                string url = $"https://dujpxiwctslbpcziquqr.supabase.co/rest/v1/challenges?id=eq.{shortId}&select=config";

                using (UnityWebRequest www = UnityWebRequest.Get(url))
                {
                    www.SetRequestHeader("apikey", publicAnonKey);
                    www.SetRequestHeader("Authorization", $"Bearer {publicAnonKey}");

                    yield return www.SendWebRequest();

                    if (www.result != UnityWebRequest.Result.Success)
                    {
                        Plugin.Log.LogError($"Failed to fetch challenge code: {www.error}");
                        finalJson = "{}";
                    }
                    else
                    {
                        var response = JsonConvert.DeserializeObject<List<SupabaseResponse>>(www.downloadHandler.text);
                        if (response != null && response.Count > 0)
                        {
                            finalJson = JsonConvert.SerializeObject(response[0].config);
                            lastLoadedCode = input;
                            cachedJson = finalJson;
                        }
                    }
                }
            }
        }
        else
        {
            finalJson = input.Replace('\'', '\"');
        }

        try
        {
            var settings = new JsonSerializerSettings { MissingMemberHandling = MissingMemberHandling.Ignore };
            CurrentChallenge decoded = JsonConvert.DeserializeObject<CurrentChallenge>(finalJson, settings);
            if (decoded != null)
            {
                currentChallenge = decoded;
                Plugin.Log.LogInfo($"Successfully loaded: {currentChallenge.Name}");
            }
        }
        catch (System.Exception ex)
        {
            Plugin.Log.LogError($"Load Error: {ex.Message}");
            currentChallenge = new CurrentChallenge();
        }

        onComplete?.Invoke();
    }

    public class SupabaseResponse { public CurrentChallenge config { get; set; } }
}