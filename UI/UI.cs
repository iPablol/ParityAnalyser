using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ParityAnalyser
{
    public class UI
    {
        private GameObject _menu;
        private readonly ParityAnalyser _plugin;
        private readonly ExtensionButton _extensionBtn = new ExtensionButton();

        public UI(ParityAnalyser plugin)
        {
            this._plugin = plugin;

            //Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Automapper.Icon.png");
            //byte[] data = new byte[stream.Length];
            //stream.Read(data, 0, (int)stream.Length);

            //Texture2D texture2D = new Texture2D(256, 256);
            //texture2D.LoadImage(data);

            //_extensionBtn.Icon = Sprite.Create(texture2D, new Rect(0, 0, texture2D.width, texture2D.height), new Vector2(0, 0), 100.0f);
            _extensionBtn.Tooltip = "Parity Analyser";
            ExtensionButtons.AddButton(_extensionBtn);
        }



        public void AddMenu(MapEditorUI mapEditorUI)
        {
            CanvasGroup parent = mapEditorUI.MainUIGroup[5];
            _menu = new GameObject("Automapper Menu");
            _menu.transform.parent = parent.transform;

            AttachTransform(_menu, 50, 50, 1, 1, 0, 0, 1, 1);

            Image image = _menu.AddComponent<Image>();
            image.sprite = PersistentUI.Instance.Sprites.Background;
            image.type = Image.Type.Sliced;
            image.color = new Color(0.24f, 0.24f, 0.24f);

            //AddLabel(_menu.transform, "Light", "Light", new Vector2(-70, -20));
            //AddCheckbox(_menu.transform, "Fused", "Fused Only", new Vector2(-60, -215), Options.Mapper.GenerateFused, (check) =>
            //{
            //    Options.Mapper.GenerateFused = check;
            //});
            //AddCheckbox(_menu.transform, "Line", "Randomize Line", new Vector2(-125, -215), Options.Mapper.RandomizeLine, (check) =>
            //{
            //    Options.Mapper.RandomizeLine = check;
            //});
            //AddCheckbox(_menu.transform, "Bottom", "Bottom Row Only", new Vector2(-60, -190), Options.Mapper.BottomRowOnly, (check) =>
            //{
            //    Options.Mapper.BottomRowOnly = check;
            //});
            //AddCheckbox(_menu.transform, "UpDown", "Up and Down Only", new Vector2(20, -190), Options.Mapper.UpDownOnly, (check) =>
            //{
            //    Options.Mapper.UpDownOnly = check;
            //});
            //AddCheckbox(_menu.transform, "Use Chroma", "Use Chroma", new Vector2(20, -25), Options.Light.Chroma, (check) =>
            //{
            //    Options.Light.Chroma = check;
            //});
            //AddCheckbox(_menu.transform, "Ignore Bombs", "Ignore Bombs", new Vector2(20, -55), Options.Light.IgnoreBomb, (check) =>
            //{
            //    Options.Light.IgnoreBomb = check;
            //});
            //AddCheckbox(_menu.transform, "Nerf Strobes", "Nerf Strobes", new Vector2(20, -90), Options.Light.NerfStrobes, (check) =>
            //{
            //    Options.Light.NerfStrobes = check;
            //});
            //AddCheckbox(_menu.transform, "Use Boost Events", "Use Boost Events", new Vector2(20, -125), Options.Light.UseBoostColor, (check) =>
            //{
            //    Options.Light.UseBoostColor = check;
            //});

            //AddCheckbox(_menu.transform, "Wrist Limiter", "Wrist Limiter", new Vector2(20, -160), Options.Mapper.Limiter, (check) =>
            //{
            //    Options.Mapper.Limiter = check;
            //});
            //AddCheckbox(_menu.transform, "Timing Only", "Timing Only", new Vector2(-60, -160), Options.Mapper.GenerateAsTiming, (check) =>
            //{
            //    Options.Mapper.GenerateAsTiming = check;
            //});

            //// Swap, Speed, Boost, BPM
            //AddLabel(_menu.transform, "Audio", "Audio", new Vector2(100, -20));
            //AddLabel(_menu.transform, "Audio", "Audio Range", new Vector2(-10, -210));
            //AddLabel(_menu.transform, "Map", "Map", new Vector2(-150, -170));
            //AddTextInput(_menu.transform, "Minimum", "Min", new Vector2(35, -210), Options.Mapper.MinRange.ToString(), (value) =>
            //{
            //    float res;
            //    if (float.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture.NumberFormat, out res))
            //    {
            //        Options.Mapper.MinRange = res;
            //    }
            //});
            //AddTextInput(_menu.transform, "Max", "Max", new Vector2(110, -210), Options.Mapper.MaxRange.ToString(), (value) =>
            //{
            //    float res;
            //    if (float.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture.NumberFormat, out res))
            //    {
            //        Options.Mapper.MaxRange = res;
            //    }
            //});
            //AddTextInput(_menu.transform, "Indistinguishable Range", "Indistinguishable Range", new Vector2(110, -190), Options.Mapper.IndistinguishableRange.ToString(), (value) =>
            //{
            //    float res;
            //    if (float.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture.NumberFormat, out res))
            //    {
            //        Options.Mapper.IndistinguishableRange = res;
            //    }
            //});
            //AddTextInput(_menu.transform, "Color Swap Speed", "Speed", new Vector2(-90, -50), Options.Light.ColorSwap.ToString(), (value) =>
            //{
            //    float res;
            //    if (float.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture.NumberFormat, out res))
            //    {
            //        Options.Light.ColorSwap = res;
            //    }
            //});
            //AddTextInput(_menu.transform, "Color Offset", "Offset", new Vector2(-90, -85), Options.Light.ColorOffset.ToString(), (value) =>
            //{
            //    float res;
            //    if (float.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture.NumberFormat, out res))
            //    {
            //        Options.Light.ColorOffset = res;
            //    }
            //});
            //AddTextInput(_menu.transform, "Color Swap Boost", "Boost", new Vector2(-90, -120), Options.Light.ColorBoostSwap.ToString(), (value) =>
            //{
            //    float res;
            //    if (float.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture.NumberFormat, out res))
            //    {
            //        Options.Light.ColorBoostSwap = res;
            //    }
            //});
            //AddTextInput(_menu.transform, "OnsetSensitivity", "Onset Sensitivity", new Vector2(110, -155), Options.Mapper.OnsetSensitivity.ToString(), (value) =>
            //{
            //    float res;
            //    if (float.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture.NumberFormat, out res))
            //    {
            //        Options.Mapper.OnsetSensitivity = res;
            //    }
            //});
            //AddTextInput(_menu.transform, "DoubleThreshold", "Double Threshold", new Vector2(110, -120), Options.Mapper.DoubleThreshold.ToString(), (value) =>
            //{
            //    float res;
            //    if (float.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture.NumberFormat, out res))
            //    {
            //        Options.Mapper.DoubleThreshold = res;
            //    }
            //});
            //AddTextInput(_menu.transform, "MaxSpeed", "Max Speed", new Vector2(110, -50), Options.Mapper.MaxSpeed.ToString(), (value) =>
            //{
            //    float res;
            //    if (float.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture.NumberFormat, out res))
            //    {
            //        Options.Mapper.MaxSpeed = res;
            //    }
            //});
            //AddTextInput(_menu.transform, "MaxDoubleSpeed", "Max Double Speed", new Vector2(110, -85), Options.Mapper.MaxDoubleSpeed.ToString(), (value) =>
            //{
            //    float res;
            //    if (float.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture.NumberFormat, out res))
            //    {
            //        Options.Mapper.MaxDoubleSpeed = res;
            //    }
            //});

            //// Button
            //AddLabel(_menu.transform, "Algorithm", "Algorithm", new Vector2(-150, -20));
            //AddButton(_menu.transform, "GenAudio", "Audio", new Vector2(-150, -50), () =>
            //{
            //    _automapper.Audio();
            //});
            //AddButton(_menu.transform, "GenMap", "Map", new Vector2(-150, -85), () =>
            //{
            //    _automapper.Converter();
            //});
            //AddButton(_menu.transform, "GenLight", "Light", new Vector2(-150, -120), () =>
            //{
            //    _automapper.Light();
            //});

            AddButton(_menu.transform, "Analyse", "Analyse", new Vector2(0, -20), _plugin.Analyse);
            AddButton(_menu.transform, "Clear Renders", "Clear Renders", new Vector2(0, -40), ParityAnalyser.ClearRenders);

            _menu.SetActive(false);
            _extensionBtn.Click = () =>
            {
                _menu.SetActive(!_menu.activeSelf);
            };
        }

        // i ended up copying Top_Cat's CM-JS UI helper, too useful to make my own tho
        // after askin TC if it's one of the only way, he let me use this
        private void AddButton(Transform parent, string title, string text, Vector2 pos, UnityAction onClick)
        {
            var button = Object.Instantiate(PersistentUI.Instance.ButtonPrefab, parent);
            MoveTransform(button.transform, 60, 25, 0.5f, 1, pos.x, pos.y);

            button.name = title;
            button.Button.onClick.AddListener(onClick);

            button.SetText(text);
            button.Text.enableAutoSizing = false;
            button.Text.fontSize = 12;
        }

        private void AddLabel(Transform parent, string title, string text, Vector2 pos, Vector2? size = null)
        {
            var entryLabel = new GameObject(title + " Label", typeof(TextMeshProUGUI));
            var rectTransform = ((RectTransform)entryLabel.transform);
            rectTransform.SetParent(parent);

            MoveTransform(rectTransform, 110, 24, 0.5f, 1, pos.x, pos.y);
            var textComponent = entryLabel.GetComponent<TextMeshProUGUI>();

            textComponent.name = title;
            textComponent.font = PersistentUI.Instance.ButtonPrefab.Text.font;
            textComponent.alignment = TextAlignmentOptions.Center;
            textComponent.fontSize = 14;
            textComponent.text = text;
        }

        private void AddTextInput(Transform parent, string title, string text, Vector2 pos, string value, UnityAction<string> onChange)
        {
            var entryLabel = new GameObject(title + " Label", typeof(TextMeshProUGUI));
            var rectTransform = ((RectTransform)entryLabel.transform);
            rectTransform.SetParent(parent);

            MoveTransform(rectTransform, 50, 16, 0.5f, 1, pos.x - 27.5f, pos.y);
            var textComponent = entryLabel.GetComponent<TextMeshProUGUI>();

            textComponent.name = title;
            textComponent.font = PersistentUI.Instance.ButtonPrefab.Text.font;
            textComponent.alignment = TextAlignmentOptions.Right;
            textComponent.fontSize = 12;
            textComponent.text = text;

            var textInput = Object.Instantiate(PersistentUI.Instance.TextInputPrefab, parent);
            MoveTransform(textInput.transform, 55, 20, 0.5f, 1, pos.x + 27.5f, pos.y);
            textInput.GetComponent<Image>().pixelsPerUnitMultiplier = 3;
            textInput.InputField.text = value;
            textInput.InputField.onFocusSelectAll = false;
            textInput.InputField.textComponent.alignment = TextAlignmentOptions.Left;
            textInput.InputField.textComponent.fontSize = 10;

            textInput.InputField.onValueChanged.AddListener(onChange);
        }

        private void AddCheckbox(Transform parent, string title, string text, Vector2 pos, bool value, UnityAction<bool> onClick)
        {
            var entryLabel = new GameObject(title + " Label", typeof(TextMeshProUGUI));
            var rectTransform = ((RectTransform)entryLabel.transform);
            rectTransform.SetParent(parent);
            MoveTransform(rectTransform, 50, 16, 0.45f, 1, pos.x + 10, pos.y + 5);
            var textComponent = entryLabel.GetComponent<TextMeshProUGUI>();

            textComponent.name = title;
            textComponent.font = PersistentUI.Instance.ButtonPrefab.Text.font;
            textComponent.alignment = TextAlignmentOptions.Left;
            textComponent.fontSize = 12;
            textComponent.text = text;

            var original = GameObject.Find("Strobe Generator").GetComponentInChildren<Toggle>(true);
            var toggleObject = Object.Instantiate(original, parent.transform);
            MoveTransform(toggleObject.transform, 100, 25, 0.5f, 1, pos.x, pos.y);

            var toggleComponent = toggleObject.GetComponent<Toggle>();
            var colorBlock = toggleComponent.colors;
            colorBlock.normalColor = Color.white;
            toggleComponent.colors = colorBlock;
            toggleComponent.isOn = value;

            toggleComponent.onValueChanged.AddListener(onClick);
        }

        private RectTransform AttachTransform(GameObject obj, float sizeX, float sizeY, float anchorX, float anchorY, float anchorPosX, float anchorPosY, float pivotX = 0.5f, float pivotY = 0.5f)
        {
            RectTransform rectTransform = obj.AddComponent<RectTransform>();
            rectTransform.localScale = new Vector3(1, 1, 1);
            rectTransform.sizeDelta = new Vector2(sizeX, sizeY);
            rectTransform.pivot = new Vector2(pivotX, pivotY);
            rectTransform.anchorMin = rectTransform.anchorMax = new Vector2(anchorX, anchorY);
            rectTransform.anchoredPosition = new Vector3(anchorPosX, anchorPosY, 0);

            return rectTransform;
        }

        private void MoveTransform(Transform transform, float sizeX, float sizeY, float anchorX, float anchorY, float anchorPosX, float anchorPosY, float pivotX = 0.5f, float pivotY = 0.5f)
        {
            if (!(transform is RectTransform rectTransform)) return;

            rectTransform.localScale = new Vector3(1, 1, 1);
            rectTransform.sizeDelta = new Vector2(sizeX, sizeY);
            rectTransform.pivot = new Vector2(pivotX, pivotY);
            rectTransform.anchorMin = rectTransform.anchorMax = new Vector2(anchorX, anchorY);
            rectTransform.anchoredPosition = new Vector3(anchorPosX, anchorPosY, 0);
        }
    }
}
