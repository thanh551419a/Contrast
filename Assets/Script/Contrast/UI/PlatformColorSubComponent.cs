using System;
using Contrast.Color;
using UnityEngine;
using UnityEngine.UI;

namespace Contrast.UI
{
    /// <summary>
    /// Dedicated sub-component for choosing and displaying platform color.
    /// Supports selecting by logical type:
    /// - White: 0 -> 84
    /// - Gray: 85 -> 169
    /// - Black: 170 -> 254
    /// - Everything / Universal: 255 (value of everything, all player colors can stand)
    /// </summary>
    public sealed class PlatformColorSubComponent
    {
        private Dropdown typeDropdown;
        private InputField rawValueField;
        private Image swatchImage;
        private Text swatchLabel;
        private Button btnWhite;
        private Button btnGray;
        private Button btnBlack;
        private Button btnAll;

        private float currentRawColor = 255f;
        private bool suppressCallbacks;
        private Action<float> onColorChangedCallback;

        public float CurrentRawColor => currentRawColor;
        public LogicalColor CurrentLogicalColor => ColorClassifier.Classify(currentRawColor);

        /// <summary>
        /// Build the UI elements inside the inspector hierarchy.
        /// </summary>
        public void BuildUI(
            Transform parent,
            ref float y,
            Func<string, Transform, GameObject> newUiFunc,
            Func<Font> getFontFunc,
            Action<Transform, string, Vector2, int, TextAnchor> createLabelFunc,
            Func<Transform, string, string[], float, Dropdown> createDropdownFunc,
            Action<float> onColorChanged)
        {
            onColorChangedCallback = onColorChanged;

            // Section header
            createLabelFunc(parent, "COLOR & TYPE", new Vector2(22f, y), 24, TextAnchor.MiddleLeft);
            y -= 45f;

            // Type Dropdown row
            createLabelFunc(parent, "Type", new Vector2(22f, y), 18, TextAnchor.MiddleLeft);

            string[] typeOptions = new string[]
            {
                "White (0..84)",
                "Gray (85..169)",
                "Black (170..254)",
                "Everything (255)"
            };

            typeDropdown = createDropdownFunc(parent, "ColorType", typeOptions, y);
            typeDropdown.onValueChanged.AddListener(OnTypeDropdownChanged);

            // Preview Swatch next to dropdown
            GameObject swatchGo = newUiFunc("ColorSwatch", parent);
            swatchImage = swatchGo.AddComponent<Image>();
            swatchImage.color = ColorClassifier.GetPlatformVisualColor(255f);
            RectTransform swatchRt = swatchGo.GetComponent<RectTransform>();
            swatchRt.anchorMin = new Vector2(0f, 1f);
            swatchRt.anchorMax = new Vector2(0f, 1f);
            swatchRt.pivot = new Vector2(0f, 1f);
            swatchRt.sizeDelta = new Vector2(36f, 34f);
            swatchRt.anchoredPosition = new Vector2(285f, y + 5f);

            // Swatch mini label
            GameObject swatchLabelGo = newUiFunc("SwatchLabel", swatchGo.transform);
            swatchLabel = swatchLabelGo.AddComponent<Text>();
            swatchLabel.font = getFontFunc();
            swatchLabel.text = "ALL";
            swatchLabel.fontSize = 11;
            swatchLabel.fontStyle = FontStyle.Bold;
            swatchLabel.color = UnityEngine.Color.black;
            swatchLabel.alignment = TextAnchor.MiddleCenter;
            RectTransform lblRt = swatchLabelGo.GetComponent<RectTransform>();
            lblRt.anchorMin = Vector2.zero;
            lblRt.anchorMax = Vector2.one;
            lblRt.offsetMin = Vector2.zero;
            lblRt.offsetMax = Vector2.zero;

            // Raw numeric input field
            y -= 45f;
            createLabelFunc(parent, "Raw (0..255)", new Vector2(22f, y), 18, TextAnchor.MiddleLeft);

            GameObject fieldGo = newUiFunc("RawColorField", parent);
            Image fieldBg = fieldGo.AddComponent<Image>();
            fieldBg.color = UnityEngine.Color.white;
            RectTransform fieldRt = fieldGo.GetComponent<RectTransform>();
            fieldRt.anchorMin = new Vector2(0f, 1f);
            fieldRt.anchorMax = new Vector2(0f, 1f);
            fieldRt.pivot = new Vector2(0f, 1f);
            fieldRt.sizeDelta = new Vector2(165f, 34f);
            fieldRt.anchoredPosition = new Vector2(155f, y + 5f);

            GameObject textGo = newUiFunc("Text", fieldGo.transform);
            Text rawText = textGo.AddComponent<Text>();
            rawText.font = getFontFunc();
            rawText.fontSize = 18;
            rawText.color = UnityEngine.Color.black;
            rawText.alignment = TextAnchor.MiddleLeft;
            RectTransform textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(8f, 0f);
            textRt.offsetMax = new Vector2(-8f, 0f);

            rawValueField = fieldGo.AddComponent<InputField>();
            rawValueField.textComponent = rawText;
            rawValueField.contentType = InputField.ContentType.DecimalNumber;
            rawValueField.onEndEdit.AddListener(OnRawFieldEndEdit);

            // Quick preset buttons row: [ WHITE ] [ GRAY ] [ BLACK ] [ ALL ]
            y -= 45f;
            float btnW = 68f;
            float btnH = 28f;
            float btnSpacing = 74f;
            float btnStartX = 22f;

            btnWhite = CreateQuickButton(parent, newUiFunc, getFontFunc, "WHITE",
                new Vector2(btnStartX, y), new Vector2(btnW, btnH),
                UnityEngine.Color.white, UnityEngine.Color.black, () => SetRawColor(0f, true));

            btnGray = CreateQuickButton(parent, newUiFunc, getFontFunc, "GRAY",
                new Vector2(btnStartX + btnSpacing, y), new Vector2(btnW, btnH),
                new UnityEngine.Color(0.55f, 0.55f, 0.55f, 1f), UnityEngine.Color.white, () => SetRawColor(127f, true));

            btnBlack = CreateQuickButton(parent, newUiFunc, getFontFunc, "BLACK",
                new Vector2(btnStartX + btnSpacing * 2f, y), new Vector2(btnW, btnH),
                new UnityEngine.Color(0.18f, 0.18f, 0.18f, 1f), UnityEngine.Color.white, () => SetRawColor(212f, true));

            btnAll = CreateQuickButton(parent, newUiFunc, getFontFunc, "ALL 255",
                new Vector2(btnStartX + btnSpacing * 3f, y), new Vector2(btnW, btnH),
                new UnityEngine.Color(0.95f, 0.78f, 0.22f, 1f), UnityEngine.Color.black, () => SetRawColor(255f, true));

            y -= 35f;
        }

        private static Button CreateQuickButton(
            Transform parent,
            Func<string, Transform, GameObject> newUiFunc,
            Func<Font> getFontFunc,
            string label,
            Vector2 pos,
            Vector2 size,
            UnityEngine.Color bgCol,
            UnityEngine.Color textCol,
            UnityEngine.Events.UnityAction action)
        {
            GameObject go = newUiFunc("QuickColor_" + label, parent);
            Image img = go.AddComponent<Image>();
            img.color = bgCol;
            Button btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(action);

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;

            GameObject textGo = newUiFunc("Label", go.transform);
            Text txt = textGo.AddComponent<Text>();
            txt.font = getFontFunc();
            txt.text = label;
            txt.fontSize = 12;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = textCol;
            txt.raycastTarget = false;
            RectTransform trt = textGo.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;

            return btn;
        }

        /// <summary>
        /// External update to synchronize the component with the selected platform.
        /// </summary>
        public void SetRawColor(float rawColor, bool notify = false)
        {
            currentRawColor = Mathf.Clamp(rawColor, 0f, 255f);
            LogicalColor logical = ColorClassifier.Classify(currentRawColor);

            suppressCallbacks = true;

            if (rawValueField != null)
                rawValueField.text = currentRawColor.ToString("F1");

            if (typeDropdown != null)
                typeDropdown.value = (int)logical;

            UpdateVisualSwatch(logical, currentRawColor);

            suppressCallbacks = false;

            if (notify)
                onColorChangedCallback?.Invoke(currentRawColor);
        }

        public void SetInteractable(bool interactable)
        {
            if (typeDropdown != null) typeDropdown.interactable = interactable;
            if (rawValueField != null) rawValueField.interactable = interactable;
            if (btnWhite != null) btnWhite.interactable = interactable;
            if (btnGray != null) btnGray.interactable = interactable;
            if (btnBlack != null) btnBlack.interactable = interactable;
            if (btnAll != null) btnAll.interactable = interactable;
        }

        private void OnTypeDropdownChanged(int index)
        {
            if (suppressCallbacks)
                return;

            LogicalColor chosen = (LogicalColor)index;
            float newRaw = ColorClassifier.DefaultValue(chosen);
            SetRawColor(newRaw, true);
        }

        private void OnRawFieldEndEdit(string text)
        {
            if (suppressCallbacks)
                return;

            if (float.TryParse(text, out float val))
            {
                SetRawColor(val, true);
            }
            else
            {
                if (rawValueField != null)
                    rawValueField.text = currentRawColor.ToString("F1");
            }
        }

        private void UpdateVisualSwatch(LogicalColor logical, float rawColor)
        {
            if (swatchImage == null)
                return;

            swatchImage.color = ColorClassifier.GetPlatformVisualColor(rawColor);

            if (swatchLabel != null)
            {
                switch (logical)
                {
                    case LogicalColor.Universal:
                        swatchLabel.text = "ALL";
                        swatchLabel.color = UnityEngine.Color.black;
                        break;
                    case LogicalColor.White:
                        swatchLabel.text = "WHT";
                        swatchLabel.color = UnityEngine.Color.black;
                        break;
                    case LogicalColor.Gray:
                        swatchLabel.text = "GRY";
                        swatchLabel.color = UnityEngine.Color.white;
                        break;
                    case LogicalColor.Black:
                    default:
                        swatchLabel.text = "BLK";
                        swatchLabel.color = UnityEngine.Color.white;
                        break;
                }
            }
        }
    }
}
