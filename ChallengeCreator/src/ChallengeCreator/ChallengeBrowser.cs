using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using Newtonsoft.Json;

namespace ChallengeCreator
{
    public class ChallengeBrowser : MonoBehaviour
    {
        private const string SUPABASE_URL = "https://dujpxiwctslbpcziquqr.supabase.co/rest/v1/challenges";
        private const string ANON_KEY = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImR1anB4aXdjdHNsYnBjemlxdXFyIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NzA1MzkzMjIsImV4cCI6MjA4NjExNTMyMn0.FKJ3Mei_i3psMBkWbrZ11HwMuNl2h6-wEFloYGRkOnw";

        private GameObject mainCanvas;
        private GameObject backgroundOverlay;
        private GameObject mainPanel;
        private GameObject contentArea;
        private TMP_InputField searchInput;
        private ScrollRect scrollRect;
        private Transform contentContainer;
        private Material cellMaterial;
        private bool isInitialized = false;

        private List<ChallengeEntry> allChallenges = new List<ChallengeEntry>();
        private List<ChallengeEntry> filteredChallenges = new List<ChallengeEntry>();
        private List<GameObject> entryObjects = new List<GameObject>();

        private GameObject currentExpandedEntry = null;
        private MainMenu cachedMainMenu;

        [Serializable]
        public class ChallengeEntry
        {
            public string id;
            public CurrentChallenge config;

            public string DisplayName => config?.Name ?? "Unnamed";
            public string DisplayCreators => config?.Creators ?? "Unknown";
            public string DisplayNotes => config?.Notes ?? "";
            public string ShortId => id;
        }

        [Serializable]
        public class SupabaseResponse
        {
            public string id;
            public CurrentChallenge config;
        }

        private void Awake()
        {
        }

        public void Show(MainMenu mainMenu)
        {
            cachedMainMenu = mainMenu;

            if (mainCanvas != null)
            {
                mainCanvas.SetActive(true);
                if (!isInitialized)
                {
                    StartCoroutine(FetchChallenges());
                }
                return;
            }

            CreateUI();
            StartCoroutine(FetchChallengesDelayed());
        }

        private IEnumerator FetchChallengesDelayed()
        {
            yield return null;
            yield return null;
            yield return null;
            yield return null;

            if (contentContainer == null)
            {
                if (mainPanel != null)
                {
                    Transform foundContainer = mainPanel.transform.Find("ContentArea/Viewport/Content");
                    if (foundContainer != null)
                    {
                        contentContainer = foundContainer;
                    }
                }
                if (contentContainer == null)
                {
                    yield break;
                }
            }

            StartCoroutine(FetchChallenges());
        }

        public void Hide()
        {
            if (mainCanvas != null)
                mainCanvas.SetActive(false);

            if (cachedMainMenu != null)
            {
                Transform mainPage = cachedMainMenu.transform.Find("Canvas/MainPage");
                if (mainPage != null)
                    mainPage.gameObject.SetActive(true);
            }
        }

        private void CreateUI()
        {
            mainCanvas = new GameObject("ChallengeBrowser");
            mainCanvas.transform.SetParent(transform);
            Canvas canvas = mainCanvas.AddComponent<Canvas>();
            CanvasScaler scaler = mainCanvas.AddComponent<CanvasScaler>();
            GraphicRaycaster raycaster = mainCanvas.AddComponent<GraphicRaycaster>();

            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            backgroundOverlay = new GameObject("BackgroundOverlay");
            backgroundOverlay.transform.SetParent(mainCanvas.transform, false);
            Image overlayImage = backgroundOverlay.AddComponent<Image>();
            overlayImage.color = new Color(0, 0, 0, 0.87f);
            RectTransform overlayRect = backgroundOverlay.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.sizeDelta = Vector2.zero;

            mainPanel = new GameObject("MainPanel");
            mainPanel.transform.SetParent(mainCanvas.transform, false);
            RectTransform panelRect = mainPanel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.05f, 0.05f);
            panelRect.anchorMax = new Vector2(0.95f, 0.95f);
            panelRect.sizeDelta = Vector2.zero;

            Image panelImage = mainPanel.AddComponent<Image>();
            panelImage.color = new Color(0.1792f, 0.1253f, 0.0905f, 0.7294f);

            LoadCellMaterial();

            if (cellMaterial != null)
            {
                panelImage.material = cellMaterial;
            }

            CreateSearchBar();
            CreateContentArea();
            CreateBottomLink();
            CreateCloseButton();

            isInitialized = true;
        }

        private void LoadCellMaterial()
        {
            try
            {
                GameObject settingsPage = GameObject.Find("MainMenu/Canvas/SettingsPage");
                if (settingsPage != null)
                {
                    Transform settingsCell = settingsPage.transform.Find("SettingsPageShared/Content/Parent/SettingsCell(Clone)");
                    if (settingsCell != null)
                    {
                        Image cellImage = settingsCell.GetComponent<Image>();
                        if (cellImage != null && cellImage.material != null)
                        {
                            cellMaterial = Instantiate(cellImage.material);
                            return;
                        }
                    }
                }
            }
            catch (Exception) { }
        }

        private void CreateSearchBar()
        {
            GameObject searchBar = new GameObject("SearchBar");
            searchBar.transform.SetParent(mainPanel.transform, false);

            RectTransform searchRect = searchBar.AddComponent<RectTransform>();
            searchRect.anchorMin = new Vector2(0.02f, 0.92f);
            searchRect.anchorMax = new Vector2(0.98f, 0.98f);
            searchRect.sizeDelta = Vector2.zero;

            Image background = searchBar.AddComponent<Image>();
            background.color = new Color(0.2f, 0.16f, 0.13f, 0.9f);

            searchInput = searchBar.AddComponent<TMP_InputField>();

            GameObject textArea = new GameObject("TextArea");
            textArea.transform.SetParent(searchBar.transform, false);
            RectTransform textAreaRect = textArea.AddComponent<RectTransform>();
            textAreaRect.anchorMin = Vector2.zero;
            textAreaRect.anchorMax = Vector2.one;
            textAreaRect.sizeDelta = Vector2.zero;

            searchInput.textComponent = CreateInputText(textArea.transform);
            searchInput.placeholder = CreatePlaceholderText(textArea.transform);
            searchInput.onValueChanged.AddListener(OnSearchChanged);
        }

        private TextMeshProUGUI CreateInputText(Transform parent)
        {
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(parent, false);
            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = 20;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Left;
            tmp.fontStyle = FontStyles.Normal;

            if (UIUtils.gameFont != null)
                tmp.font = UIUtils.gameFont;

            RectTransform rect = textObj.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(15, 5);
            rect.offsetMax = new Vector2(-15, -5);

            return tmp;
        }

        private TextMeshProUGUI CreatePlaceholderText(Transform parent)
        {
            GameObject placeholderObj = new GameObject("Placeholder");
            placeholderObj.transform.SetParent(parent, false);
            TextMeshProUGUI tmp = placeholderObj.AddComponent<TextMeshProUGUI>();
            tmp.text = "Search by name or shortcode...";
            tmp.fontSize = 20;
            tmp.color = new Color(0.7f, 0.7f, 0.7f);
            tmp.alignment = TextAlignmentOptions.Left;
            tmp.fontStyle = FontStyles.Normal;

            if (UIUtils.gameFont != null)
                tmp.font = UIUtils.gameFont;

            RectTransform rect = placeholderObj.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(15, 5);
            rect.offsetMax = new Vector2(-15, -5);

            return tmp;
        }

        private void CreateContentArea()
        {
            contentArea = new GameObject("ContentArea");
            contentArea.transform.SetParent(mainPanel.transform, false);

            RectTransform contentAreaRect = contentArea.AddComponent<RectTransform>();
            contentAreaRect.anchorMin = new Vector2(0.02f, 0.08f);
            contentAreaRect.anchorMax = new Vector2(0.98f, 0.9f);
            contentAreaRect.sizeDelta = Vector2.zero;

            scrollRect = contentArea.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Elastic;
            scrollRect.inertia = true;
            scrollRect.scrollSensitivity = 20;

            GameObject viewport = new GameObject("Viewport");
            viewport.transform.SetParent(contentArea.transform, false);
            RectTransform viewportRect = viewport.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.sizeDelta = Vector2.zero;

            Mask mask = viewport.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            Image viewportImage = viewport.AddComponent<Image>();
            viewportImage.color = new Color(0, 0, 0, 0.3f);
            viewportImage.raycastTarget = false;

            GameObject content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            contentContainer = content.transform;

            RectTransform contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0, 0);

            VerticalLayoutGroup layoutGroup = content.AddComponent<VerticalLayoutGroup>();
            layoutGroup.padding = new RectOffset(20, 20, 20, 20);
            layoutGroup.spacing = 15;
            layoutGroup.childAlignment = TextAnchor.UpperCenter;
            layoutGroup.childControlHeight = true;
            layoutGroup.childControlWidth = true;
            layoutGroup.childForceExpandWidth = true;
            layoutGroup.childForceExpandHeight = false;

            ContentSizeFitter sizeFitter = content.AddComponent<ContentSizeFitter>();
            sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            sizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            scrollRect.viewport = viewportRect;
            scrollRect.content = contentRect;
        }

        private void CreateBottomLink()
        {
            GameObject linkArea = new GameObject("BottomLink");
            linkArea.transform.SetParent(mainPanel.transform, false);

            RectTransform linkRect = linkArea.AddComponent<RectTransform>();
            linkRect.anchorMin = new Vector2(0.02f, 0.01f);
            linkRect.anchorMax = new Vector2(0.98f, 0.07f);
            linkRect.sizeDelta = Vector2.zero;

            HorizontalLayoutGroup layout = linkArea.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 8;
            layout.padding = new RectOffset(10, 10, 10, 10);
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            TextMeshProUGUI prefixText = CreateText("Want to know how to upload challenges? Go to:", 14, FontStyles.Normal);
            prefixText.transform.SetParent(linkArea.transform, false);
            prefixText.color = new Color(0.8f, 0.8f, 0.8f);

            LayoutElement prefixLayout = prefixText.gameObject.AddComponent<LayoutElement>();
            prefixLayout.minWidth = 300;

            GameObject linkButton = CreateLinkButton("atomictyler.dev/#peakPresets");
            linkButton.transform.SetParent(linkArea.transform, false);

            LayoutElement linkLayout = linkButton.AddComponent<LayoutElement>();
            linkLayout.minWidth = 200;
        }

        private GameObject CreateLinkButton(string url)
        {
            GameObject buttonObj = new GameObject("LinkButton");

            // This button is no longer clickable to not get flagged ^^

            TextMeshProUGUI tmp = buttonObj.AddComponent<TextMeshProUGUI>();
            tmp.text = url;
            tmp.fontSize = 14;
            tmp.fontStyle = FontStyles.Normal;
            tmp.color = new Color(0.4f, 0.8f, 1f);
            tmp.textWrappingMode = TextWrappingModes.Normal;

            if (UIUtils.gameFont != null)
                tmp.font = UIUtils.gameFont;

            return buttonObj;
        }

        private GameObject CreateCloseButton()
        {
            GameObject buttonObj = new GameObject("CloseButton");
            buttonObj.transform.SetParent(mainPanel.transform, false);

            RectTransform buttonRect = buttonObj.AddComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.98f, 0.98f);
            buttonRect.anchorMax = new Vector2(0.99f, 0.99f);
            buttonRect.sizeDelta = new Vector2(40, 40);

            Image buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = new Color(0.4f, 0.3f, 0.25f);

            Button button = buttonObj.AddComponent<Button>();
            button.onClick.AddListener(Hide);

            GameObject buttonText = new GameObject("X");
            buttonText.transform.SetParent(buttonObj.transform, false);
            TextMeshProUGUI tmp = buttonText.AddComponent<TextMeshProUGUI>();
            tmp.text = "X";
            tmp.fontSize = 24;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.fontStyle = FontStyles.Normal;

            if (UIUtils.gameFont != null)
                tmp.font = UIUtils.gameFont;

            RectTransform textRect = buttonText.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            return buttonObj;
        }

        private IEnumerator FetchChallenges()
        {
            if (contentContainer == null)
            {
                yield break;
            }

            ClearContentContainer();
            GameObject loadingText = CreateLoadingText();

            string url = SUPABASE_URL + "?select=id,config&limit=100";
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.SetRequestHeader("apikey", ANON_KEY);
                request.SetRequestHeader("Authorization", $"Bearer {ANON_KEY}");

                yield return request.SendWebRequest();

                if (loadingText != null)
                    DestroyImmediate(loadingText);

                if (request.result != UnityWebRequest.Result.Success)
                {
                    ShowErrorMessage($"Failed to load challenges: {request.error}");
                    yield break;
                }

                try
                {
                    List<SupabaseResponse> response = JsonConvert.DeserializeObject<List<SupabaseResponse>>(request.downloadHandler.text);
                    if (response != null)
                    {
                        allChallenges.Clear();
                        foreach (SupabaseResponse item in response)
                        {
                            if (item.config != null)
                            {
                                allChallenges.Add(new ChallengeEntry { id = item.id, config = item.config });
                            }
                        }
                        filteredChallenges = new List<ChallengeEntry>(allChallenges);
                        RefreshChallengeList();
                    }
                }
                catch (Exception ex)
                {
                    ShowErrorMessage($"Error parsing challenge data: {ex.Message}");
                }
            }
        }

        private void ClearContentContainer()
        {
            if (contentContainer == null)
            {
                return;
            }

            foreach (GameObject obj in entryObjects)
            {
                if (obj != null)
                    DestroyImmediate(obj);
            }
            entryObjects.Clear();

            for (int i = contentContainer.childCount - 1; i >= 0; i--)
            {
                if (contentContainer.GetChild(i) != null)
                    DestroyImmediate(contentContainer.GetChild(i).gameObject);
            }
        }

        private GameObject CreateLoadingText()
        {
            if (contentContainer == null)
            {
                return null;
            }

            GameObject loadingObj = new GameObject("LoadingText");
            loadingObj.transform.SetParent(contentContainer, false);
            TextMeshProUGUI tmp = loadingObj.AddComponent<TextMeshProUGUI>();
            tmp.text = "Loading challenges...";
            tmp.fontSize = 24;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.fontStyle = FontStyles.Normal;
            if (UIUtils.gameFont != null) tmp.font = UIUtils.gameFont;
            return loadingObj;
        }

        private void ShowErrorMessage(string message)
        {
            if (contentContainer == null)
            {
                return;
            }

            GameObject errorObj = new GameObject("ErrorText");
            errorObj.transform.SetParent(contentContainer, false);
            TextMeshProUGUI tmp = errorObj.AddComponent<TextMeshProUGUI>();
            tmp.text = message;
            tmp.fontSize = 20;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(1f, 0.5f, 0.5f);
            tmp.fontStyle = FontStyles.Normal;
            if (UIUtils.gameFont != null) tmp.font = UIUtils.gameFont;
        }

        private void OnSearchChanged(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText)) filteredChallenges = new List<ChallengeEntry>(allChallenges);
            else
            {
                string lowerSearch = searchText.ToLower();
                filteredChallenges = allChallenges.Where(c => c.DisplayName.ToLower().Contains(lowerSearch) || c.ShortId.ToLower().Contains(lowerSearch)).ToList();
            }
            RefreshChallengeList();
        }

        private void RefreshChallengeList()
        {
            if (contentContainer == null)
            {
                return;
            }

            ClearContentContainer();
            currentExpandedEntry = null;

            if (filteredChallenges.Count == 0)
            {
                ShowErrorMessage("No challenges match your search.");
                return;
            }

            for (int i = 0; i < filteredChallenges.Count; i++)
            {
                GameObject entryObj = CreateChallengeEntry(filteredChallenges[i], i);
                if (entryObj != null)
                {
                    entryObj.transform.SetParent(contentContainer, false);
                    entryObjects.Add(entryObj);
                }
            }

            if (scrollRect != null && scrollRect.content != null)
                scrollRect.content.anchoredPosition = Vector2.zero;
        }

        private GameObject CreateChallengeEntry(ChallengeEntry entry, int index)
        {
            if (contentContainer == null)
            {
                return null;
            }

            GameObject entryObj = new GameObject($"Entry_{index}");
            entryObj.transform.SetParent(contentContainer, false);

            LayoutElement layout = entryObj.AddComponent<LayoutElement>();
            layout.preferredHeight = 130;
            layout.minHeight = 130;
            layout.flexibleWidth = 1;

            Image background = entryObj.AddComponent<Image>();
            background.color = new Color(0.1792f, 0.1253f, 0.0905f, 0.7294f);
            if (cellMaterial != null) background.material = cellMaterial;

            GameObject staticHeader = new GameObject("StaticHeader");
            staticHeader.transform.SetParent(entryObj.transform, false);
            RectTransform headerRect = staticHeader.AddComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0, 1);
            headerRect.anchorMax = new Vector2(1, 1);
            headerRect.pivot = new Vector2(0.5f, 1);
            headerRect.sizeDelta = new Vector2(0, 130);

            HorizontalLayoutGroup horizontalLayout = staticHeader.AddComponent<HorizontalLayoutGroup>();
            horizontalLayout.padding = new RectOffset(25, 20, 20, 20);
            horizontalLayout.spacing = 20;
            horizontalLayout.childAlignment = TextAnchor.MiddleLeft;
            horizontalLayout.childControlHeight = true;
            horizontalLayout.childControlWidth = true;
            horizontalLayout.childForceExpandWidth = true;
            horizontalLayout.childForceExpandHeight = false;

            GameObject infoArea = new GameObject("InfoArea");
            infoArea.transform.SetParent(staticHeader.transform, false);
            VerticalLayoutGroup infoLayout = infoArea.AddComponent<VerticalLayoutGroup>();
            infoLayout.childControlHeight = true;
            infoLayout.childControlWidth = true;
            infoLayout.childForceExpandWidth = true;
            infoLayout.spacing = 5;

            LayoutElement infoLayoutElement = infoArea.AddComponent<LayoutElement>();
            infoLayoutElement.flexibleWidth = 1;
            infoLayoutElement.minWidth = 450;

            TextMeshProUGUI nameText = CreateText(entry.DisplayName, 22, FontStyles.Normal);
            nameText.transform.SetParent(infoArea.transform, false);
            nameText.color = new Color(1f, 0.9f, 0.7f);

            TextMeshProUGUI creatorsText = CreateText($"By: {entry.DisplayCreators}", 14, FontStyles.Normal);
            creatorsText.transform.SetParent(infoArea.transform, false);
            creatorsText.color = new Color(0.8f, 0.8f, 0.8f);

            string notes = string.IsNullOrEmpty(entry.DisplayNotes) ? "" : $" • {entry.DisplayNotes}";
            string notesDisplay = notes.Length > 100 ? notes.Substring(0, 97) + "..." : notes;
            TextMeshProUGUI infoText = CreateText($"ID: {entry.ShortId}{notesDisplay}", 12, FontStyles.Normal);
            infoText.transform.SetParent(infoArea.transform, false);
            infoText.color = new Color(0.7f, 0.7f, 0.7f);

            GameObject buttonsArea = new GameObject("ButtonsArea");
            buttonsArea.transform.SetParent(staticHeader.transform, false);
            HorizontalLayoutGroup buttonsLayout = buttonsArea.AddComponent<HorizontalLayoutGroup>();
            buttonsLayout.spacing = 10;
            buttonsLayout.childAlignment = TextAnchor.MiddleRight;
            buttonsLayout.childControlHeight = true;
            buttonsLayout.childControlWidth = true;
            buttonsLayout.childForceExpandWidth = false;
            buttonsLayout.childForceExpandHeight = false;

            LayoutElement buttonsLayoutElement = buttonsArea.AddComponent<LayoutElement>();
            buttonsLayoutElement.preferredWidth = 160;

            GameObject applyButton = CreateSquareButton("Apply", new Color(0.3f, 0.6f, 0.3f), out TextMeshProUGUI applyTMP, 65);
            applyButton.transform.SetParent(buttonsArea.transform, false);
            applyButton.GetComponent<Button>().onClick.AddListener(() => ShowApplyConfirmation(entry));

            GameObject expandButton = CreateSquareButton("Expand", new Color(0.5f, 0.5f, 0.6f), out TextMeshProUGUI expandTMP, 65);
            expandButton.transform.SetParent(buttonsArea.transform, false);
            expandButton.GetComponent<Button>().onClick.AddListener(() =>
            {
                bool isExpanding = currentExpandedEntry != entryObj;

                StartCoroutine(ToggleExpand(entryObj, entry));

                expandTMP.text = isExpanding ? "Collapse" : "Expand";
            });

            return entryObj;
        }

        private GameObject CreateSquareButton(string text, Color color, out TextMeshProUGUI tmp, float size = 45)
        {
            GameObject buttonObj = new GameObject($"Button_{text}");

            RectTransform rect = buttonObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(size, size);

            Image buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = color;
            Button button = buttonObj.AddComponent<Button>();

            LayoutElement le = buttonObj.AddComponent<LayoutElement>();
            le.preferredWidth = size;
            le.preferredHeight = size;
            le.minWidth = size;
            le.minHeight = size;

            GameObject buttonText = new GameObject("Text");
            buttonText.transform.SetParent(buttonObj.transform, false);
            tmp = buttonText.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 12;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontStyle = FontStyles.Normal;
            if (UIUtils.gameFont != null) tmp.font = UIUtils.gameFont;

            RectTransform textRect = buttonText.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            return buttonObj;
        }

        private IEnumerator ToggleExpand(GameObject entryObj, ChallengeEntry entry)
        {
            LayoutElement layout = entryObj.GetComponent<LayoutElement>();

            if (currentExpandedEntry == entryObj)
            {
                Transform existing = entryObj.transform.Find("ExpandedContent");
                if (existing != null)
                    DestroyImmediate(existing.gameObject);

                layout.preferredHeight = 130;
                currentExpandedEntry = null;
                yield break;
            }

            if (currentExpandedEntry != null)
            {
                Transform oldExpandBtn = currentExpandedEntry.transform.Find("StaticHeader/ButtonsArea/Button_Expand");
                if (oldExpandBtn != null)
                {
                    TextMeshProUGUI oldTmp = oldExpandBtn.GetComponentInChildren<TextMeshProUGUI>();
                    if (oldTmp != null)
                    {
                        oldTmp.text = "Expand";
                    }
                }

                Transform old = currentExpandedEntry.transform.Find("ExpandedContent");
                if (old != null)
                    DestroyImmediate(old.gameObject);

                currentExpandedEntry.GetComponent<LayoutElement>().preferredHeight = 130;
            }

            GameObject expand = new GameObject("ExpandedContent");
            expand.transform.SetParent(entryObj.transform, false);

            RectTransform expandRect = expand.AddComponent<RectTransform>();
            expandRect.anchorMin = new Vector2(0, 0);
            expandRect.anchorMax = new Vector2(1, 0);
            expandRect.pivot = new Vector2(0.5f, 0);
            expandRect.sizeDelta = new Vector2(0, 350);

            Image bg = expand.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.4f);

            GameObject scroll = new GameObject("ScrollView");
            scroll.transform.SetParent(expand.transform, false);

            RectTransform scrollRect = scroll.AddComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0.02f, 0.02f);
            scrollRect.anchorMax = new Vector2(0.98f, 0.98f);
            scrollRect.offsetMin = Vector2.zero;
            scrollRect.offsetMax = Vector2.zero;

            ScrollRect sr = scroll.AddComponent<ScrollRect>();
            sr.horizontal = false;
            sr.vertical = true;
            sr.movementType = ScrollRect.MovementType.Clamped;
            sr.scrollSensitivity = 20;

            GameObject viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scroll.transform, false);

            RectTransform vpRect = viewport.AddComponent<RectTransform>();
            vpRect.anchorMin = Vector2.zero;
            vpRect.anchorMax = Vector2.one;
            vpRect.offsetMin = Vector2.zero;
            vpRect.offsetMax = Vector2.zero;

            Image vpImage = viewport.AddComponent<Image>();
            vpImage.color = new Color(0, 0, 0, 0.25f);

            Mask mask = viewport.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            GameObject content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);

            RectTransform contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0, 0);

            VerticalLayoutGroup vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.childForceExpandWidth = true;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.spacing = 0;
            vlg.padding = new RectOffset(10, 10, 10, 10);

            ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            sr.viewport = vpRect;
            sr.content = contentRect;

            GameObject textObj = new GameObject("ConfigText");
            textObj.transform.SetParent(content.transform, false);

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0, 1);
            textRect.anchorMax = new Vector2(1, 1);
            textRect.pivot = new Vector2(0.5f, 1);
            textRect.anchoredPosition = Vector2.zero;
            textRect.sizeDelta = new Vector2(0, 0);

            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = JsonConvert.SerializeObject(entry.config, Formatting.Indented);
            tmp.fontSize = 12;
            tmp.alignment = TextAlignmentOptions.TopLeft;
            tmp.fontStyle = FontStyles.Normal;
            tmp.enableWordWrapping = true;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.color = Color.white;

            if (UIUtils.gameFont != null)
                tmp.font = UIUtils.gameFont;

            LayoutElement le = textObj.AddComponent<LayoutElement>();
            le.flexibleWidth = 1;

            ContentSizeFitter textFitter = textObj.AddComponent<ContentSizeFitter>();
            textFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);

            layout.preferredHeight = 480;
            currentExpandedEntry = entryObj;

            yield return null;
        }
        private void ShowApplyConfirmation(ChallengeEntry entry)
        {
            if (mainCanvas == null)
                return;

            GameObject root = new GameObject("ConfirmationDialog");
            root.transform.SetParent(mainCanvas.transform, false);

            RectTransform rootRect = root.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            Canvas canvas = root.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = 300;

            root.AddComponent<GraphicRaycaster>();

            Image overlay = root.AddComponent<Image>();
            overlay.color = new Color(0, 0, 0, 0.85f);

            GameObject panel = new GameObject("Panel");
            panel.transform.SetParent(root.transform, false);

            RectTransform panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(420, 220);

            Image bg = panel.AddComponent<Image>();
            bg.color = new Color(0.18f, 0.14f, 0.1f);

            VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(20, 20, 20, 20);
            layout.spacing = 15;
            layout.childAlignment = TextAnchor.MiddleCenter;

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(panel.transform, false);

            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = "Apply this challenge?\nYour current config will be lost.";
            tmp.fontSize = 18;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontStyle = FontStyles.Normal;
            tmp.color = Color.white;

            if (UIUtils.gameFont != null)
                tmp.font = UIUtils.gameFont;

            GameObject row = new GameObject("Buttons");
            row.transform.SetParent(panel.transform, false);

            HorizontalLayoutGroup rowLayout = row.AddComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 20;
            rowLayout.childAlignment = TextAnchor.MiddleCenter;
            rowLayout.childForceExpandWidth = false;

            GameObject apply = CreateRectangularButton("Apply", new Color(0.25f, 0.65f, 0.3f), 130, 45);
            apply.transform.SetParent(row.transform, false);

            apply.GetComponent<Button>().onClick.AddListener(() =>
            {
                ApplyChallenge(entry);
                Destroy(root);
            });

            GameObject cancel = CreateRectangularButton("Cancel", new Color(0.65f, 0.25f, 0.25f), 130, 45);
            cancel.transform.SetParent(row.transform, false);

            cancel.GetComponent<Button>().onClick.AddListener(() =>
            {
                Destroy(root);
            });
        }

        private GameObject CreateRectangularButton(string text, Color color, float width, float height)
        {
            GameObject buttonObj = new GameObject($"Button_{text}");

            RectTransform rect = buttonObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(width, height);

            Image buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = color;
            Button button = buttonObj.AddComponent<Button>();

            LayoutElement le = buttonObj.AddComponent<LayoutElement>();
            le.preferredWidth = width;
            le.preferredHeight = height;
            le.minWidth = width;
            le.minHeight = height;

            GameObject buttonText = new GameObject("Text");
            buttonText.transform.SetParent(buttonObj.transform, false);
            TextMeshProUGUI tmp = buttonText.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 16;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontStyle = FontStyles.Normal;
            tmp.color = Color.white;
            if (UIUtils.gameFont != null) tmp.font = UIUtils.gameFont;

            RectTransform textRect = buttonText.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            return buttonObj;
        }

        private TextMeshProUGUI CreateText(string text, int fontSize, FontStyles style)
        {
            GameObject textObj = new GameObject("Text");
            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.color = Color.white;

            if (UIUtils.gameFont != null)
                tmp.font = UIUtils.gameFont;

            return tmp;
        }

        private void ApplyChallenge(ChallengeEntry entry)
        {
            if (entry.config == null) return;
            Plugin.challengeCustom.Value = entry.ShortId;
            Plugin.challengePreset.Value = "Custom";
            UIUtils.WarningMessage($"Applied: {entry.DisplayName}");
        }
    }
}