using System;
using System.Collections.Generic;
using System.IO;
using Contrast.Core;
using Contrast.Data;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Contrast.UI
{
    public enum LevelSelectMode
    {
        Play,
        EditorImport
    }

    /// <summary>
    /// Level Selection and File Browser UI.
    /// Lists all JSON levels in Application.persistentDataPath/Levels, sorted by newest first.
    /// Reused as the in-game IMPORT file picker for the Level Editor.
    /// Follows the optical laboratory dark translucent aesthetics of MainMenuUI.
    /// </summary>
    public sealed class LevelSelectUI : MonoBehaviour
    {
        private GameObject panel;
        private Text titleText;
        private Text subtitleText;
        private Text statusErrorText;
        private Text actionButtonLabel;
        private Text pathHintText;
        private RectTransform listContent;

        private LevelSelectMode mode = LevelSelectMode.Play;
        private Action<string> onImportCallback;

        private readonly List<string> filePaths = new List<string>();
        private readonly List<GameObject> rowItems = new List<GameObject>();
        private int selectedIndex = -1;

        private void Awake()
        {
            EnsureEventSystem();
            BuildUI();
            Show(false);
        }

        public void Show(bool visible)
        {
            if (panel != null)
                panel.SetActive(visible);

            if (visible)
            {
                RefreshFileList();
            }
        }

        public void OpenForPlay()
        {
            mode = LevelSelectMode.Play;
            onImportCallback = null;

            if (titleText != null)
                titleText.text = "LEVEL SELECT";

            if (subtitleText != null)
                subtitleText.text = "CHỌN MÀN CHƠI TỪ BỘ NHỚ · SELECT LEVEL FILE";

            if (actionButtonLabel != null)
                actionButtonLabel.text = "LOAD";

            ClearError();
            Show(true);
        }

        public void OpenForImport(Action<string> onFileSelected)
        {
            mode = LevelSelectMode.EditorImport;
            onImportCallback = onFileSelected;

            if (titleText != null)
                titleText.text = "IMPORT LEVEL";

            if (subtitleText != null)
                subtitleText.text = "CHỌN FILE ĐỂ IMPORT VÀO TRÌNH THIẾT KẾ · SELECT FILE TO IMPORT";

            if (actionButtonLabel != null)
                actionButtonLabel.text = "IMPORT";

            ClearError();
            Show(true);
        }

        public void ShowError(string errorMessage)
        {
            if (statusErrorText != null)
            {
                statusErrorText.text = errorMessage;
                statusErrorText.color = new UnityEngine.Color(1f, 0.35f, 0.35f, 1f);
            }
            Debug.LogError($"[LEVEL_SELECT] {errorMessage}");
        }

        public void ClearError()
        {
            if (statusErrorText != null)
            {
                statusErrorText.text = string.Empty;
            }
        }

        public void RefreshFileList()
        {
            ClearError();
            LevelRepository.EnsureDirectory();
            LevelRepository.EnsureDefaultLevelFile();

            string[] files = LevelRepository.ListLevelFiles();
            filePaths.Clear();
            if (files != null)
                filePaths.AddRange(files);

            if (pathHintText != null)
                pathHintText.text = "VỊ TRÍ LƯU TRỮ: " + LevelRepository.GetLevelsDirectory();

            PopulateRows();

            // Automatically select the first file in the list if available
            if (filePaths.Count > 0)
            {
                SelectIndex(0);
            }
            else
            {
                selectedIndex = -1;
                UpdateRowHighlights();
            }
        }

        private void PopulateRows()
        {
            foreach (GameObject row in rowItems)
            {
                if (row != null)
                    Destroy(row);
            }
            rowItems.Clear();

            if (listContent == null)
                return;

            for (int i = 0; i < filePaths.Count; i++)
            {
                string path = filePaths[i];
                int index = i;

                GameObject rowGo = NewUI($"FileRow_{i}", listContent);
                Image rowBg = rowGo.AddComponent<Image>();
                rowBg.color = new UnityEngine.Color(0.06f, 0.10f, 0.16f, 0.90f);

                Outline rowOutline = rowGo.AddComponent<Outline>();
                rowOutline.effectColor = new UnityEngine.Color(0.75f, 0.88f, 0.98f, 0.20f);
                rowOutline.effectDistance = new Vector2(1f, -1f);

                Button btn = rowGo.AddComponent<Button>();
                btn.targetGraphic = rowBg;
                btn.onClick.AddListener(() => SelectIndex(index));

                RectTransform rowRt = rowGo.GetComponent<RectTransform>();
                rowRt.sizeDelta = new Vector2(1040f, 54f);

                // Filename text (left)
                GameObject nameGo = NewUI("FileName", rowGo.transform);
                Text nameText = nameGo.AddComponent<Text>();
                nameText.font = GetFont();
                nameText.text = Path.GetFileName(path);
                nameText.fontSize = 17;
                nameText.fontStyle = FontStyle.Bold;
                nameText.alignment = TextAnchor.MiddleLeft;
                nameText.color = new UnityEngine.Color(0.94f, 0.97f, 1f, 1f);

                RectTransform nameRt = nameGo.GetComponent<RectTransform>();
                nameRt.anchorMin = new Vector2(0f, 0f);
                nameRt.anchorMax = new Vector2(0.65f, 1f);
                nameRt.offsetMin = new Vector2(20f, 0f);
                nameRt.offsetMax = new Vector2(0f, 0f);

                // Date & size text (right)
                string infoStr = "—";
                try
                {
                    FileInfo fi = new FileInfo(path);
                    infoStr = $"{fi.LastWriteTime:yyyy-MM-dd HH:mm:ss}   ({fi.Length / 1024f:F1} KB)";
                }
                catch { }

                GameObject infoGo = NewUI("FileInfo", rowGo.transform);
                Text infoText = infoGo.AddComponent<Text>();
                infoText.font = GetFont();
                infoText.text = infoStr;
                infoText.fontSize = 14;
                infoText.fontStyle = FontStyle.Normal;
                infoText.alignment = TextAnchor.MiddleRight;
                infoText.color = new UnityEngine.Color(0.70f, 0.82f, 0.92f, 0.65f);

                RectTransform infoRt = infoGo.GetComponent<RectTransform>();
                infoRt.anchorMin = new Vector2(0.65f, 0f);
                infoRt.anchorMax = new Vector2(1f, 1f);
                infoRt.offsetMin = new Vector2(0f, 0f);
                infoRt.offsetMax = new Vector2(-20f, 0f);

                rowItems.Add(rowGo);
            }
        }

        private void SelectIndex(int index)
        {
            if (index < 0 || index >= filePaths.Count)
                return;

            selectedIndex = index;
            UpdateRowHighlights();
            ClearError();
        }

        private void UpdateRowHighlights()
        {
            for (int i = 0; i < rowItems.Count; i++)
            {
                GameObject row = rowItems[i];
                if (row == null) continue;

                Image img = row.GetComponent<Image>();
                Outline outline = row.GetComponent<Outline>();

                bool isSelected = (i == selectedIndex);
                if (img != null)
                {
                    img.color = isSelected
                        ? new UnityEngine.Color(0.12f, 0.22f, 0.34f, 0.95f)
                        : new UnityEngine.Color(0.06f, 0.10f, 0.16f, 0.90f);
                }

                if (outline != null)
                {
                    outline.effectColor = isSelected
                        ? new UnityEngine.Color(0.35f, 0.85f, 1f, 0.85f)
                        : new UnityEngine.Color(0.75f, 0.88f, 0.98f, 0.20f);
                    outline.effectDistance = isSelected ? new Vector2(1.8f, -1.8f) : new Vector2(1f, -1f);
                }
            }
        }

        private void OnActionClicked()
        {
            if (selectedIndex < 0 || selectedIndex >= filePaths.Count)
            {
                ShowError("Vui lòng chọn một file level từ danh sách.");
                return;
            }

            string selectedPath = filePaths[selectedIndex];
            if (!File.Exists(selectedPath))
            {
                ShowError($"File không tồn tại trên đĩa: {selectedPath}");
                return;
            }

            if (mode == LevelSelectMode.Play)
            {
                Show(false);
                GameManager.Instance?.PlayLevelFromFile(selectedPath);
            }
            else if (mode == LevelSelectMode.EditorImport)
            {
                Show(false);
                onImportCallback?.Invoke(selectedPath);
            }
        }

        private void OnBackClicked()
        {
            Show(false);
            if (mode == LevelSelectMode.Play)
            {
                GameManager.Instance?.ReturnToMainMenu();
            }
            else if (mode == LevelSelectMode.EditorImport)
            {
                GameManager.Instance?.OpenEditor();
            }
        }

        private void BuildUI()
        {
            GameObject canvasGo = NewUI("Canvas", transform);
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 300;

            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            panel = NewUI("Panel", canvasGo.transform);
            Image bg = panel.AddComponent<Image>();
            bg.color = new UnityEngine.Color(0.027f, 0.043f, 0.067f, 0.96f);
            Stretch(panel.GetComponent<RectTransform>());

            // Title
            GameObject titleGo = NewUI("Title", panel.transform);
            titleText = titleGo.AddComponent<Text>();
            titleText.font = GetFont();
            titleText.text = "LEVEL SELECT";
            titleText.fontSize = 58;
            titleText.fontStyle = FontStyle.Bold;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = new UnityEngine.Color(0.95f, 0.97f, 1f, 1f);

            Outline titleOutline = titleGo.AddComponent<Outline>();
            titleOutline.effectColor = new UnityEngine.Color(0.75f, 0.88f, 0.98f, 0.35f);
            titleOutline.effectDistance = new Vector2(1.5f, -1.5f);

            RectTransform titleRt = titleGo.GetComponent<RectTransform>();
            titleRt.sizeDelta = new Vector2(1200f, 75f);
            titleRt.anchoredPosition = new Vector2(0f, 440f);

            // Subtitle
            GameObject subGo = NewUI("Subtitle", panel.transform);
            subtitleText = subGo.AddComponent<Text>();
            subtitleText.font = GetFont();
            subtitleText.text = "CHỌN MÀN CHƠI TỪ BỘ NHỚ · SELECT LEVEL FILE";
            subtitleText.fontSize = 15;
            subtitleText.fontStyle = FontStyle.Normal;
            subtitleText.alignment = TextAnchor.MiddleCenter;
            subtitleText.color = new UnityEngine.Color(0.72f, 0.82f, 0.90f, 0.65f);

            RectTransform subRt = subGo.GetComponent<RectTransform>();
            subRt.sizeDelta = new Vector2(1200f, 30f);
            subRt.anchoredPosition = new Vector2(0f, 385f);

            // File List Card Container
            GameObject cardGo = NewUI("ListCard", panel.transform);
            Image cardBg = cardGo.AddComponent<Image>();
            cardBg.color = new UnityEngine.Color(0.040f, 0.060f, 0.095f, 0.94f);

            Outline cardOutline = cardGo.AddComponent<Outline>();
            cardOutline.effectColor = new UnityEngine.Color(0.75f, 0.85f, 0.95f, 0.28f);
            cardOutline.effectDistance = new Vector2(1.5f, -1.5f);

            RectTransform cardRt = cardGo.GetComponent<RectTransform>();
            cardRt.sizeDelta = new Vector2(1100f, 540f);
            cardRt.anchoredPosition = new Vector2(0f, 75f);

            // ScrollRect
            GameObject scrollGo = NewUI("ScrollView", cardGo.transform);
            RectTransform scrollRt = scrollGo.GetComponent<RectTransform>();
            scrollRt.anchorMin = Vector2.zero;
            scrollRt.anchorMax = Vector2.one;
            scrollRt.offsetMin = new Vector2(16f, 16f);
            scrollRt.offsetMax = new Vector2(-16f, -16f);

            ScrollRect scroll = scrollGo.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.scrollSensitivity = 25f;

            GameObject viewportGo = NewUI("Viewport", scrollGo.transform);
            RectTransform vpRt = viewportGo.GetComponent<RectTransform>();
            vpRt.anchorMin = Vector2.zero;
            vpRt.anchorMax = Vector2.one;
            vpRt.offsetMin = Vector2.zero;
            vpRt.offsetMax = Vector2.zero;
            viewportGo.AddComponent<RectMask2D>();
            scroll.viewport = vpRt;

            GameObject contentGo = NewUI("Content", viewportGo.transform);
            listContent = contentGo.GetComponent<RectTransform>();
            listContent.anchorMin = new Vector2(0f, 1f);
            listContent.anchorMax = new Vector2(1f, 1f);
            listContent.pivot = new Vector2(0.5f, 1f);
            listContent.anchoredPosition = Vector2.zero;
            listContent.sizeDelta = new Vector2(0f, 0f);

            VerticalLayoutGroup layout = contentGo.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.spacing = 8f;
            layout.padding = new RectOffset(0, 0, 8, 8);

            ContentSizeFitter fitter = contentGo.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = listContent;

            // Status / Error message banner
            GameObject errGo = NewUI("StatusError", panel.transform);
            statusErrorText = errGo.AddComponent<Text>();
            statusErrorText.font = GetFont();
            statusErrorText.fontSize = 16;
            statusErrorText.fontStyle = FontStyle.Bold;
            statusErrorText.alignment = TextAnchor.MiddleCenter;
            statusErrorText.color = new UnityEngine.Color(1f, 0.35f, 0.35f, 1f);
            statusErrorText.text = string.Empty;

            RectTransform errRt = errGo.GetComponent<RectTransform>();
            errRt.sizeDelta = new Vector2(1100f, 32f);
            errRt.anchoredPosition = new Vector2(0f, -225f);

            // Button Row
            GameObject btnRow = NewUI("ButtonRow", panel.transform);
            RectTransform rowRt = btnRow.GetComponent<RectTransform>();
            rowRt.sizeDelta = new Vector2(800f, 65f);
            rowRt.anchoredPosition = new Vector2(0f, -285f);

            // LOAD / IMPORT Action Button
            actionButtonLabel = CreateButton(
                btnRow.transform,
                "LOAD",
                new Vector2(-180f, 0f),
                new Vector2(200f, 54f),
                new UnityEngine.Color(0.12f, 0.45f, 0.30f, 0.95f),
                OnActionClicked);

            // REFRESH Button
            CreateButton(
                btnRow.transform,
                "REFRESH",
                new Vector2(40f, 0f),
                new Vector2(160f, 54f),
                new UnityEngine.Color(0.08f, 0.12f, 0.18f, 0.92f),
                RefreshFileList);

            // BACK Button
            CreateButton(
                btnRow.transform,
                "BACK",
                new Vector2(230f, 0f),
                new Vector2(150f, 54f),
                new UnityEngine.Color(0.14f, 0.16f, 0.20f, 0.92f),
                OnBackClicked);

            // Storage Path Hint (bottom)
            GameObject pathGo = NewUI("PathHint", panel.transform);
            pathHintText = pathGo.AddComponent<Text>();
            pathHintText.font = GetFont();
            pathHintText.fontSize = 13;
            pathHintText.alignment = TextAnchor.MiddleCenter;
            pathHintText.color = new UnityEngine.Color(0.70f, 0.80f, 0.90f, 0.45f);
            pathHintText.text = string.Empty;

            RectTransform pathRt = pathGo.GetComponent<RectTransform>();
            pathRt.sizeDelta = new Vector2(1400f, 30f);
            pathRt.anchoredPosition = new Vector2(0f, -360f);
        }

        private static Text CreateButton(
            Transform parent,
            string label,
            Vector2 pos,
            Vector2 size,
            UnityEngine.Color bgCol,
            UnityEngine.Events.UnityAction callback)
        {
            GameObject btnGo = NewUI(label + "Btn", parent);
            Image img = btnGo.AddComponent<Image>();
            img.color = bgCol;

            Outline outline = btnGo.AddComponent<Outline>();
            outline.effectColor = new UnityEngine.Color(0.75f, 0.88f, 0.98f, 0.32f);
            outline.effectDistance = new Vector2(1.2f, -1.2f);

            Button btn = btnGo.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(callback);

            ColorBlock cb = btn.colors;
            cb.normalColor = bgCol;
            cb.highlightedColor = bgCol * 1.25f;
            cb.pressedColor = bgCol * 0.85f;
            btn.colors = cb;

            RectTransform rt = btnGo.GetComponent<RectTransform>();
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;

            GameObject textGo = NewUI("Label", btnGo.transform);
            Text text = textGo.AddComponent<Text>();
            text.font = GetFont();
            text.text = label;
            text.fontSize = 17;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new UnityEngine.Color(0.95f, 0.97f, 1f, 1f);
            Stretch(textGo.GetComponent<RectTransform>());

            return text;
        }

        private static GameObject NewUI(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            return go;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static Font GetFont()
        {
#if UNITY_2022_2_OR_NEWER
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
#else
            return Resources.GetBuiltinResource<Font>("Arial.ttf");
#endif
        }

        private static void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null)
                return;

            GameObject go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            go.AddComponent<StandaloneInputModule>();
#endif
        }
    }
}
