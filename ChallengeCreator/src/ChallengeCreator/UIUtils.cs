using HarmonyLib;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ChallengeCreator;
public class UIUtils
{
    public static void DisplayChallenge(GUIManager guiManager)
    {
        var font = guiManager.heroDayText.font;
        var challenge = ChallengeReader.currentChallenge;

        GameObject canvasObj = new GameObject("ChallengeUI_Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<GraphicRaycaster>();

        GameObject container = new GameObject("TextContainer");
        container.transform.SetParent(canvasObj.transform, false);

        CanvasGroup group = container.AddComponent<CanvasGroup>();
        group.alpha = 0f;

        RectTransform containerRect = container.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.5f, 1f);
        containerRect.anchorMax = new Vector2(0.5f, 1f);
        containerRect.pivot = new Vector2(0.5f, 1f);
        containerRect.anchoredPosition = new Vector2(0, 0);
        containerRect.sizeDelta = new Vector2(800, 300);

        VerticalLayoutGroup layout = container.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.spacing = -100;

        CreateChallengeText("NameText", challenge.Name, 45, font, container.transform);
        CreateChallengeText("CreatorsText", $"BY: {challenge.Creators}", 25, font, container.transform);
        CreateChallengeText("NotesText", challenge.Notes, 20, font, container.transform);

        guiManager.StartCoroutine(FadeChallengeUI(group, canvasObj));
    }

    private static HashSet<string> activeBreakingMessages = new HashSet<string>();

    public static void ChallengeBreakingMessage(string message)
    {
        if (!Plugin.showMessage.Value) return;

        if (activeBreakingMessages.Contains(message)) return;

        var guiManager = GameObject.FindFirstObjectByType<GUIManager>();
        if (guiManager == null) return;

        var font = guiManager.heroDayText.font;

        activeBreakingMessages.Add(message);

        GameObject canvasObj = new GameObject("ChallengeBreaking_Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 101;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        GameObject container = new GameObject("BreakingMessageContainer");
        container.transform.SetParent(canvasObj.transform, false);

        CanvasGroup group = container.AddComponent<CanvasGroup>();
        group.alpha = 0f;

        RectTransform rect = container.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 0);
        rect.anchorMax = new Vector2(1, 0);
        rect.pivot = new Vector2(0, 0);
        rect.anchoredPosition = new Vector2(20, 20);
        rect.sizeDelta = new Vector2(-40, 60);

        GameObject textObj = new GameObject("BreakingText");
        textObj.transform.SetParent(container.transform, false);

        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = message.ToUpper();
        tmp.font = font;
        tmp.fontSize = 28;
        tmp.alignment = TextAlignmentOptions.BottomLeft;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.overflowMode = TextOverflowModes.Ellipsis;

        tmp.color = new Color(1f, 0.2f, 0.2f);
        tmp.outlineColor = new Color(0.1f, 0f, 0f);
        tmp.outlineWidth = 0.08f;

        guiManager.StartCoroutine(FadeAlertUI(group, canvasObj, message));
    }

    private static IEnumerator FadeAlertUI(CanvasGroup group, GameObject fullCanvas, string message)
    {
        float elapsed = 0f;
        while (elapsed < 0.5f)
        {
            elapsed += Time.deltaTime;
            group.alpha = elapsed / 0.5f;
            yield return null;
        }

        yield return new WaitForSeconds(4f);

        elapsed = 0f;
        while (elapsed < 1f)
        {
            elapsed += Time.deltaTime;
            group.alpha = 1f - (elapsed / 1f);
            yield return null;
        }

        activeBreakingMessages.Remove(message);
        UnityEngine.Object.Destroy(fullCanvas);
    }

    private static float GetCalculatedWaitTime()
    {
        float waitTime = Plugin.messageOnScreenTime.Value;

        if (SceneManager.GetActiveScene().name.Contains("Level"))
        {
            waitTime += 5f; // This is to cover up for the blackout on the beach.
        }

        return waitTime;
    }

    private static IEnumerator FadeChallengeUI(CanvasGroup group, GameObject fullCanvas)
    {
        float elapsed = 0f;
        while (elapsed < 2f)
        {
            elapsed += Time.deltaTime;
            group.alpha = Mathf.Clamp01(elapsed / 2f);
            yield return null;
        }
        group.alpha = 1f;

        yield return new WaitForSeconds(GetCalculatedWaitTime());

        elapsed = 0f;
        while (elapsed < 2f)
        {
            elapsed += Time.deltaTime;
            group.alpha = Mathf.Clamp01(1f - (elapsed / 2f));
            yield return null;
        }
        group.alpha = 0f;

        Object.Destroy(fullCanvas);
    }

    private static void CreateChallengeText(string objName, string content, float fontSize, TMP_FontAsset font, Transform parent)
    {
        if (string.IsNullOrEmpty(content)) return;

        GameObject textObj = new GameObject(objName);
        textObj.transform.SetParent(parent, false);

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = content.ToUpper();
        tmp.font = font;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;

        tmp.color = new Color(0.2f, 1f, 0.2f);
        tmp.outlineColor = new Color(0f, 0.3f, 0f);
        tmp.outlineWidth = 0.1f;
    }

    public static void UpdateBoardingPass(BoardingPass __instance)
    {
        var challenge = ChallengeReader.currentChallenge;
        if (challenge.noMultiplayer && PhotonNetwork.CurrentRoom.PlayerCount > 1)
        {
            // Don't allow starting the run
            LockBoardingPass(__instance, "Multiplayer is disabled for this challenge.", "Start");
            return;
        }

        var selectedAscent = __instance.ascentIndex;

        if (selectedAscent < challenge.MinAscent)
        {
            LockBoardingPass(__instance, $"This challenge requires at least Ascent {challenge.MinAscent}.", "Start");
            return;
        }

        if (selectedAscent > challenge.MinAscent && !challenge.AllowHigherAscents)
        {
            LockBoardingPass(__instance, $"This challenge must be played exactly on Ascent {challenge.MinAscent}.", "Start");
            return;
        }

        UnlockBoardingPass(__instance);
    }

    public static void LockBoardingPass(BoardingPass __instance, string message, string disableStartMsg)
    {
        var UI = __instance.gameObject.transform.Find("BoardingPass/Panel/Ascent");
        UI.GetComponent<Image>().color = new UnityEngine.Color(1f, 0f, 0f, 0.5f);

        UI.Find("Description").GetComponent<TextMeshProUGUI>().text = message;
        BoardingPassLock(__instance, disableStartMsg);
    }

    public static void UnlockBoardingPass(BoardingPass __instance)
    {
        var UI = __instance.gameObject.transform.Find("BoardingPass/Panel/Ascent");
        UI.GetComponent<Image>().color = new UnityEngine.Color(0f, 0f, 0f, 0.2f);

        BoardingPassUnlock(__instance);
    }

    public static void BoardingPassLock(BoardingPass __instance, string message)
    {
        var Button = __instance.gameObject.transform.Find("BoardingPass/Panel/StartGameButton");
        Button.GetComponent<Button>().enabled = false;

        Button.Find("Text").GetComponent<TextMeshProUGUI>().text = message;
    }

    public static void BoardingPassUnlock(BoardingPass __instance)
    {
        var Button = __instance.gameObject.transform.Find("BoardingPass/Panel/StartGameButton");
        Button.GetComponent<Button>().enabled = true;

        Button.Find("Text").GetComponent<TextMeshProUGUI>().text = "Start";
    }

    public static void EndRunScreen(EndScreen __instance, bool validRun)
    {
        Color textColor = __instance.transform.Find("Panel/BG").GetComponent<Image>().color;
        TMP_FontAsset font = GUIManager.instance.heroDayText.font;

        CurrentChallenge challenge = ChallengeReader.currentChallenge;

        GameObject challengeInfoPanel = new GameObject("ChallengeInfoPanel");
        challengeInfoPanel.transform.SetParent(__instance.transform);

        RectTransform panelRT = challengeInfoPanel.AddComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0f, 1f);
        panelRT.anchorMax = new Vector2(0f, 1f);
        panelRT.pivot = new Vector2(0f, 1f);
        panelRT.anchoredPosition = new Vector2(20f, -20f);
        panelRT.sizeDelta = new Vector2(400f, 200f);

        VerticalLayoutGroup layout = challengeInfoPanel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(15, 15, 15, 15);
        layout.spacing = 10f;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        ContentSizeFitter sizeFitter = challengeInfoPanel.AddComponent<ContentSizeFitter>();
        sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        sizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

        CreateTextObject("ChallengeTitle", "CHALLENGE INFO", new Color(1f, 0.8f, 0.2f), 20, font, TextAlignmentOptions.Center).transform.SetParent(challengeInfoPanel.transform);
        CreateTextObject("ChallengeName", $"Name: {challenge.Name}", Color.white, 18, font, TextAlignmentOptions.Left).transform.SetParent(challengeInfoPanel.transform);
        CreateTextObject("ChallengeCreators", $"Creators: {challenge.Creators}", Color.white, 18, font, TextAlignmentOptions.Left).transform.SetParent(challengeInfoPanel.transform);

        bool isPreset = ChallengeReader.isPreset();
        Color presetColor = isPreset ? new Color(0.2f, 0.8f, 0.3f) : new Color(0.8f, 0.6f, 0.2f);
        CreateTextObject("PresetStatus", $"Type: {(isPreset ? "PRESET (OFFICIAL/BUILT-IN)" : "CUSTOM")}", presetColor, 16, font, TextAlignmentOptions.Left).transform.SetParent(challengeInfoPanel.transform);

        Color validColor = validRun ? new Color(0.2f, 1f, 0.2f) : new Color(1f, 0.2f, 0.2f);
        string validString = validRun ? "VALID RUN" : "INVALID RUN";
        CreateTextObject("ValidityStatus", $"Status: {validString}", validColor, 16, font, TextAlignmentOptions.Left).transform.SetParent(challengeInfoPanel.transform);

        GameObject divider = new GameObject("Divider");
        divider.transform.SetParent(challengeInfoPanel.transform);
        RectTransform dividerRT = divider.AddComponent<RectTransform>();
        dividerRT.sizeDelta = new Vector2(350f, 2f);
        Image dividerImg = divider.AddComponent<Image>();
        dividerImg.color = new Color(0.3f, 0.3f, 0.3f, 0.8f);
        LayoutElement divLayout = divider.AddComponent<LayoutElement>();
        divLayout.minHeight = 2f;

        if (!string.IsNullOrEmpty(challenge.Notes))
        {
            CreateTextObject("NotesTitle", "Notes:", new Color(0.6f, 0.8f, 1f), 16, font, TextAlignmentOptions.Left).transform.SetParent(challengeInfoPanel.transform);

            GameObject notesObj = new GameObject("ChallengeNotes");
            notesObj.transform.SetParent(challengeInfoPanel.transform);

            TextMeshProUGUI notesTmp = notesObj.AddComponent<TextMeshProUGUI>();
            notesTmp.text = challenge.Notes;
            notesTmp.color = new Color(0.9f, 0.9f, 0.9f);
            notesTmp.font = font;
            notesTmp.fontSize = 14;
            notesTmp.alignment = TextAlignmentOptions.TopLeft;

            notesTmp.textWrappingMode = TextWrappingModes.Normal;
            notesTmp.overflowMode = TextOverflowModes.Overflow;

            LayoutElement notesLayout = notesObj.AddComponent<LayoutElement>();
            notesLayout.preferredWidth = 350f;

            notesTmp.ForceMeshUpdate();
        }
    }

    private static GameObject CreateTextObject(string name, string text, Color color, int fontSize,
        TMP_FontAsset font, TextAlignmentOptions alignment)
    {
        GameObject textObj = new GameObject(name);
        RectTransform rt = textObj.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(350f, 30f);

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.color = color;
        tmp.font = font;
        tmp.fontSize = fontSize;
        tmp.alignment = alignment;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.textWrappingMode = TextWrappingModes.Normal;

        LayoutElement layout = textObj.AddComponent<LayoutElement>();
        layout.preferredHeight = 30f;
        layout.minHeight = 20f;

        return textObj;
    }
}