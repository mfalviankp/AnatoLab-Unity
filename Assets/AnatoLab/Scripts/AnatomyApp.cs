using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AnatoLab
{
    public sealed class AnatomyApp : MonoBehaviour
    {
        public OrganSelector selector;
        public OrbitCamera orbit;
        public TMP_InputField search;
        public RectTransform listContent;
        public ScrollRect listScroll;
        public GameObject infoPanel, welcomePanel;
        public TMP_Text titleText, latinText, descriptionText, functionText, regionText, countText, hoverText, xrayText, selectionText;
        public Button closeButton, resetButton, frontButton, backButton, leftButton, rightButton, xrayButton, focusButton, zoomInButton, zoomOutButton, clearSearchButton;
        public TMP_FontAsset font;
        readonly Dictionary<OrganData, Button> rows = new Dictionary<OrganData, Button>();
        readonly Color ink = new Color(0.88f, 0.94f, 0.96f);
        readonly Color muted = new Color(0.48f, 0.61f, 0.68f);
        readonly Color accent = new Color(0.19f, 0.87f, 0.74f);
        OrganData[] organs;
        GameObject emptyMessage;
        void Start()
        {
            organs = FindObjectsByType<OrganData>(FindObjectsSortMode.None).OrderBy(x => x.organName).ToArray();
            selector.Initialize(organs);
            selector.SelectionChanged += OnSelection;
            selector.HoverChanged += OnHover;
            search.onValueChanged.AddListener(Filter);
            closeButton.onClick.AddListener(selector.ClosePanel);
            resetButton.onClick.AddListener(() => { selector.ClosePanel(); orbit.ResetView(); });
            frontButton.onClick.AddListener(() => orbit.SetView(0));
            backButton.onClick.AddListener(() => orbit.SetView(180));
            leftButton.onClick.AddListener(() => orbit.SetView(90));
            rightButton.onClick.AddListener(() => orbit.SetView(-90));
            zoomInButton.onClick.AddListener(() => orbit.Zoom(1));
            zoomOutButton.onClick.AddListener(() => orbit.Zoom(-1));
            xrayButton.onClick.AddListener(() => { selector.ToggleXray(); UpdateXray(); });
            focusButton.onClick.AddListener(() => { if (selector.Selected != null) orbit.FocusOn(selector.Selected); });
            clearSearchButton.onClick.AddListener(() => { search.text = ""; search.ActivateInputField(); });
            foreach (var organ in organs) CreateRow(organ);
            emptyMessage = UiKit.Text("No results", listContent, "Tidak ada hasil.\nCoba nama atau wilayah lain.", font, 16, muted).gameObject;
            var el = emptyMessage.AddComponent<LayoutElement>(); el.preferredHeight = 80;
            Filter(""); OnSelection(null); UpdateXray();
            Canvas.ForceUpdateCanvases();
            listScroll.verticalNormalizedPosition = 1;
        }
        void Update()
        {
            if (search != null && search.isFocused) return;
            if (Input.GetKeyDown(KeyCode.Escape)) selector.ClosePanel();
            if (Input.GetKeyDown(KeyCode.Home)) { selector.ClosePanel(); orbit.ResetView(); }
            if (Input.GetKeyDown(KeyCode.F) && selector.Selected != null) orbit.FocusOn(selector.Selected);
        }
        void CreateRow(OrganData organ)
        {
            var row = UiKit.Panel(organ.organName, listContent, new Color(0.065f, 0.10f, 0.15f));
            var height = row.gameObject.AddComponent<LayoutElement>(); height.minHeight = height.preferredHeight = 66;
            var button = row.gameObject.AddComponent<Button>();
            button.targetGraphic = row.GetComponent<Image>();
            var colors = button.colors; colors.normalColor = Color.white; colors.highlightedColor = new Color(0.65f, 0.95f, 0.9f); colors.pressedColor = accent; colors.selectedColor = Color.white; button.colors = colors;
            var name = UiKit.Text("Name", row, organ.organName, font, 16, ink); UiKit.Stretch(name.rectTransform, 13, 9, 8, 27); name.fontStyle = FontStyles.Bold;
            var region = UiKit.Text("Region", row, organ.region.ToUpperInvariant(), font, 10, muted); UiKit.Stretch(region.rectTransform, 13, 36, 8, 8); region.characterSpacing = 1;
            button.onClick.AddListener(() => selector.Select(organ));
            rows.Add(organ, button);
        }
        void Filter(string query)
        {
            query = (query ?? "").Trim().ToLowerInvariant(); int count = 0;
            foreach (var pair in rows) { bool match = pair.Key.SearchText.Contains(query); pair.Value.gameObject.SetActive(match); if (match) count++; }
            countText.text = count + " / " + organs.Length + " WILAYAH";
            if (emptyMessage != null) emptyMessage.SetActive(count == 0);
            if (listScroll != null) listScroll.verticalNormalizedPosition = 1;
        }
        void OnSelection(OrganData organ)
        {
            bool selected = organ != null; infoPanel.SetActive(selected); welcomePanel.SetActive(!selected);
            selectionText.text = selected ? "TERPILIH  /  " + organ.organName : "MODE EKSPLORASI  /  SELURUH TUBUH";
            if (selected)
            {
                titleText.text = organ.organName; latinText.text = organ.latinName;
                descriptionText.text = organ.description; functionText.text = organ.function;
                regionText.text = organ.region.ToUpperInvariant();
            }
            foreach (var pair in rows) pair.Value.GetComponent<Image>().color = pair.Key == organ ? new Color(0.12f, 0.31f, 0.32f) : new Color(0.065f, 0.10f, 0.15f);
        }
        void OnHover(OrganData organ) { hoverText.text = organ != null ? organ.organName + "   /   Klik untuk mempelajari" : "Arahkan kursor ke model untuk mengenali wilayah"; }
        void UpdateXray() { xrayText.text = selector.xrayEnabled ? "X-RAY  ON" : "X-RAY  OFF"; }
        void OnDestroy() { if (selector != null) { selector.SelectionChanged -= OnSelection; selector.HoverChanged -= OnHover; } }
    }

    public static class UiKit
    {
        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false); return go.GetComponent<RectTransform>();
        }
        public static RectTransform Panel(string name, Transform parent, Color color)
        {
            var r = Rect(name, parent); var image = r.gameObject.AddComponent<Image>(); image.color = color; return r;
        }
        public static TextMeshProUGUI Text(string name, Transform parent, string value, TMP_FontAsset font, float size, Color color)
        {
            var r = Rect(name, parent); var t = r.gameObject.AddComponent<TextMeshProUGUI>(); t.font = font; t.text = value; t.fontSize = size; t.color = color;
            t.textWrappingMode = TextWrappingModes.Normal; t.raycastTarget = false; t.overflowMode = TextOverflowModes.Overflow;
            return t;
        }
        public static void Stretch(RectTransform r, float left = 0, float top = 0, float right = 0, float bottom = 0)
        {
            r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = new Vector2(left, bottom); r.offsetMax = new Vector2(-right, -top);
        }
        public static void Box(RectTransform r, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            r.anchorMin = r.anchorMax = anchor; r.pivot = pivot; r.anchoredPosition = position; r.sizeDelta = size;
        }
        public static Button Button(string name, Transform parent, string label, TMP_FontAsset font, Color background, Color text, int size = 14)
        {
            var r = Panel(name, parent, background); var b = r.gameObject.AddComponent<Button>(); b.targetGraphic = r.GetComponent<Image>();
            var t = Text("Label", r, label, font, size, text); Stretch(t.rectTransform, 8, 4, 8, 4); t.alignment = TextAlignmentOptions.Center;
            var c = b.colors; c.highlightedColor = new Color(0.65f, 0.9f, 0.85f); c.selectedColor = Color.white; b.colors = c; return b;
        }
    }
}

// AnatoLab package revision: 1795099051093679200
