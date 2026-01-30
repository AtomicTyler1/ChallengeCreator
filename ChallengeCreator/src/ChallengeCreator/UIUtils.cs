using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
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

        yield return new WaitForSeconds(8f);

        elapsed = 0f;
        while (elapsed < 2f)
        {
            elapsed += Time.deltaTime;
            group.alpha = Mathf.Clamp01(1f - (elapsed / 2f));
            yield return null;
        }
        group.alpha = 0f;

        UnityEngine.Object.Destroy(fullCanvas);
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
}