using System;
using UnityEngine;
using UnityEngine.UIElements;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace NutriMind.App.UI
{
    /// <summary>
    /// Settings panel wiring for UI Toolkit preview.
    /// Handles sidebar pages, responsive classes, and local-only settings.
    /// Does not perform networking or SQLite persistence.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class SettingsPanelController : MonoBehaviour
    {
        private const string CompactClass = "settings-panel--compact";
        private const string NarrowClass = "settings-panel--narrow";
        private const string MobileClass = "mobile";
        private const string TextSmallClass = "settings-panel--text-small";
        private const string TextLargeClass = "settings-panel--text-large";
        private const string TextXLargeClass = "settings-panel--text-xlarge";
        private const string HighContrastClass = "settings-panel--high-contrast";
        private const string HiddenClass = "settings-panel__page--hidden";
        private const float CompactBreakpoint = 1100f;
        private const float NarrowBreakpoint = 820f;
        private const string DropdownPopupAssetPath =
            "Assets/NutriMind/App/UI/USS/SettingsDropdownPopup.uss";

        private static readonly string[] PageNames =
        {
            "page-audio-display",
            "page-accessibility",
            "page-language",
            "page-account",
            "page-help",
            "page-about"
        };

        private static readonly string[] NavNames =
        {
            "nav-audio-display",
            "nav-accessibility",
            "nav-language",
            "nav-account",
            "nav-help",
            "nav-about"
        };

        [SerializeField]
        private StyleSheet _dropdownPopupStyle;

        private UIDocument _uiDocument;
        private VisualElement _root;
        private VisualElement _sidebar;
        private Label _statusLabel;
        private float _lastWidth = -1f;
        private AppLocalSettings _draft;
        private bool _suppressUiEvents;

        private Slider _sliderMaster;
        private Slider _sliderMusic;
        private Slider _sliderSfx;
        private Slider _sliderVoice;
        private Slider _sliderBrightness;
        private Slider _sliderSensitivity;

        private VisualElement _fillMaster;
        private VisualElement _fillMusic;
        private VisualElement _fillSfx;
        private VisualElement _fillVoice;
        private VisualElement _fillBrightness;
        private VisualElement _fillSensitivity;

        private Label _pctMaster;
        private Label _pctMusic;
        private Label _pctSfx;
        private Label _pctVoice;
        private Label _pctBrightness;
        private Label _pctSensitivity;

        private DropdownField _dropdownGraphics;
        private DropdownField _dropdownLanguage;
        private DropdownField _dropdownLanguageOverview;
        private DropdownField _dropdownTextSize;
        private DropdownField _dropdownTextSizeOverview;

        private Toggle _toggleReduceMotion;
        private Toggle _toggleReduceMotionOverview;
        private Toggle _toggleHighContrast;
        private Toggle _toggleHighContrastOverview;

        private Label _labelReduceMotion;
        private Label _labelReduceMotionOverview;
        private Label _labelHighContrast;
        private Label _labelHighContrastOverview;

        private VisualElement _privacyDetail;

        private void OnEnable()
        {
            _uiDocument = GetComponent<UIDocument>();
            BindWhenReady();
        }

        private void OnDisable()
        {
            Unbind();
            CancelInvoke(nameof(BindWhenReady));
            CancelInvoke(nameof(ClearStatus));
        }

        private void Update()
        {
            if (_root == null)
            {
                return;
            }

            float width = _root.resolvedStyle.width;
            if (float.IsNaN(width) || width <= 0f || Mathf.Approximately(width, _lastWidth))
            {
                return;
            }

            _lastWidth = width;
            ApplyResponsiveClasses(width);
        }

        private void BindWhenReady()
        {
            if (_uiDocument == null)
            {
                _uiDocument = GetComponent<UIDocument>();
            }

            if (_uiDocument == null)
            {
                return;
            }

            _root = _uiDocument.rootVisualElement?.Q<VisualElement>("settings-root");
            if (_root == null)
            {
                Invoke(nameof(BindWhenReady), 0.05f);
                return;
            }

            var panelRoot = _uiDocument.rootVisualElement;
            panelRoot.style.flexGrow = 1;
            panelRoot.style.width = Length.Percent(100);
            panelRoot.style.height = Length.Percent(100);

            AttachPanelScopedDropdownStyles();
            CacheElements();
            RegisterCallbacks();

            _draft = AppLocalSettings.Load();
            PushDraftToUi();
            ApplyPresentationClasses();
            _draft.ApplyRuntimeEffects();

            ShowPage(0);
            ApplyResponsiveClasses(_root.resolvedStyle.width);
            _root.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        /// <summary>
        /// Dropdown menus live outside .ds-root (Unity panel siblings), so UXML
        /// Style tags cannot style them. Attach kid-friendly popup chrome here.
        /// </summary>
        private void AttachPanelScopedDropdownStyles()
        {
            VisualElement panelScope = _uiDocument.rootVisualElement.parent
                ?? _uiDocument.rootVisualElement.panel?.visualTree;
            if (panelScope == null)
            {
                return;
            }

            ResolveDropdownPopupStyle();

            // Self-contained NutriMind light popup chrome (avoids DesignTokens dark defaults).
            if (_dropdownPopupStyle != null && !panelScope.styleSheets.Contains(_dropdownPopupStyle))
            {
                panelScope.styleSheets.Add(_dropdownPopupStyle);
            }
        }

        private void ResolveDropdownPopupStyle()
        {
            if (_dropdownPopupStyle != null)
            {
                return;
            }

#if UNITY_EDITOR
            _dropdownPopupStyle = AssetDatabase.LoadAssetAtPath<StyleSheet>(DropdownPopupAssetPath);
#endif
        }

        private void CacheElements()
        {
            _sidebar = _root.Q<VisualElement>("settings-sidebar");
            _statusLabel = _root.Q<Label>("settings-status");
            _privacyDetail = _root.Q<VisualElement>("privacy-detail");

            _sliderMaster = _root.Q<Slider>("slider-master");
            _sliderMusic = _root.Q<Slider>("slider-music");
            _sliderSfx = _root.Q<Slider>("slider-sfx");
            _sliderVoice = _root.Q<Slider>("slider-voice");
            _sliderBrightness = _root.Q<Slider>("slider-brightness");
            _sliderSensitivity = _root.Q<Slider>("slider-sensitivity");

            _fillMaster = _root.Q<VisualElement>("fill-master");
            _fillMusic = _root.Q<VisualElement>("fill-music");
            _fillSfx = _root.Q<VisualElement>("fill-sfx");
            _fillVoice = _root.Q<VisualElement>("fill-voice");
            _fillBrightness = _root.Q<VisualElement>("fill-brightness");
            _fillSensitivity = _root.Q<VisualElement>("fill-sensitivity");

            _pctMaster = _root.Q<Label>("pct-master");
            _pctMusic = _root.Q<Label>("pct-music");
            _pctSfx = _root.Q<Label>("pct-sfx");
            _pctVoice = _root.Q<Label>("pct-voice");
            _pctBrightness = _root.Q<Label>("pct-brightness");
            _pctSensitivity = _root.Q<Label>("pct-sensitivity");

            _dropdownGraphics = _root.Q<DropdownField>("dropdown-graphics");
            _dropdownLanguage = _root.Q<DropdownField>("dropdown-language");
            _dropdownLanguageOverview = _root.Q<DropdownField>("dropdown-language-overview");
            _dropdownTextSize = _root.Q<DropdownField>("dropdown-text-size");
            _dropdownTextSizeOverview = _root.Q<DropdownField>("dropdown-text-size-overview");

            _toggleReduceMotion = _root.Q<Toggle>("toggle-reduce-motion");
            _toggleReduceMotionOverview = _root.Q<Toggle>("toggle-reduce-motion-overview");
            _toggleHighContrast = _root.Q<Toggle>("toggle-high-contrast");
            _toggleHighContrastOverview = _root.Q<Toggle>("toggle-high-contrast-overview");

            _labelReduceMotion = _root.Q<Label>("label-reduce-motion");
            _labelReduceMotionOverview = _root.Q<Label>("label-reduce-motion-overview");
            _labelHighContrast = _root.Q<Label>("label-high-contrast");
            _labelHighContrastOverview = _root.Q<Label>("label-high-contrast-overview");
        }

        private void RegisterCallbacks()
        {
            if (_sidebar != null)
            {
                foreach (var button in _sidebar.Query<Button>(className: "settings-panel__nav-item").ToList())
                {
                    button.RegisterCallback<ClickEvent>(OnNavClickEvent);
                }
            }

            RegisterSlider(_sliderMaster, OnMasterChanged);
            RegisterSlider(_sliderMusic, OnMusicChanged);
            RegisterSlider(_sliderSfx, OnSfxChanged);
            RegisterSlider(_sliderVoice, OnVoiceChanged);
            RegisterSlider(_sliderBrightness, OnBrightnessChanged);
            RegisterSlider(_sliderSensitivity, OnSensitivityChanged);

            RegisterDropdown(_dropdownGraphics, OnGraphicsChanged);
            RegisterDropdown(_dropdownLanguage, OnLanguageChanged);
            RegisterDropdown(_dropdownLanguageOverview, OnLanguageOverviewChanged);
            RegisterDropdown(_dropdownTextSize, OnTextSizeChanged);
            RegisterDropdown(_dropdownTextSizeOverview, OnTextSizeOverviewChanged);

            RegisterToggle(_toggleReduceMotion, OnReduceMotionChanged);
            RegisterToggle(_toggleReduceMotionOverview, OnReduceMotionOverviewChanged);
            RegisterToggle(_toggleHighContrast, OnHighContrastChanged);
            RegisterToggle(_toggleHighContrastOverview, OnHighContrastOverviewChanged);

            RegisterButton("btn-restore-defaults", OnRestoreDefaults);
            RegisterButton("btn-save-changes", OnSaveChanges);
            RegisterButton("btn-privacy", OnPrivacyClicked);
            RegisterButton("btn-privacy-overview", OnPrivacyOverviewClicked);
            RegisterButton("btn-about-privacy", OnPrivacyClicked);
            RegisterButton("btn-reset-tutorial", OnResetTutorial);
            RegisterButton("btn-reset-tutorial-overview", OnResetTutorial);
            RegisterButton("btn-replay-tutorial", OnResetTutorial);
            RegisterButton("btn-support", OnSupportClicked);
        }

        private void Unbind()
        {
            if (_sidebar != null)
            {
                foreach (var button in _sidebar.Query<Button>(className: "settings-panel__nav-item").ToList())
                {
                    button.UnregisterCallback<ClickEvent>(OnNavClickEvent);
                }
            }

            UnregisterSlider(_sliderMaster, OnMasterChanged);
            UnregisterSlider(_sliderMusic, OnMusicChanged);
            UnregisterSlider(_sliderSfx, OnSfxChanged);
            UnregisterSlider(_sliderVoice, OnVoiceChanged);
            UnregisterSlider(_sliderBrightness, OnBrightnessChanged);
            UnregisterSlider(_sliderSensitivity, OnSensitivityChanged);

            UnregisterDropdown(_dropdownGraphics, OnGraphicsChanged);
            UnregisterDropdown(_dropdownLanguage, OnLanguageChanged);
            UnregisterDropdown(_dropdownLanguageOverview, OnLanguageOverviewChanged);
            UnregisterDropdown(_dropdownTextSize, OnTextSizeChanged);
            UnregisterDropdown(_dropdownTextSizeOverview, OnTextSizeOverviewChanged);

            UnregisterToggle(_toggleReduceMotion, OnReduceMotionChanged);
            UnregisterToggle(_toggleReduceMotionOverview, OnReduceMotionOverviewChanged);
            UnregisterToggle(_toggleHighContrast, OnHighContrastChanged);
            UnregisterToggle(_toggleHighContrastOverview, OnHighContrastOverviewChanged);

            if (_root != null)
            {
                _root.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            }

            _root = null;
            _sidebar = null;
            _lastWidth = -1f;
        }

        private void RegisterButton(string name, EventCallback<ClickEvent> callback)
        {
            var button = _root.Q<Button>(name);
            button?.RegisterCallback(callback);
        }

        private static void RegisterSlider(Slider slider, EventCallback<ChangeEvent<float>> callback)
        {
            slider?.RegisterValueChangedCallback(callback);
        }

        private static void UnregisterSlider(Slider slider, EventCallback<ChangeEvent<float>> callback)
        {
            slider?.UnregisterValueChangedCallback(callback);
        }

        private static void RegisterDropdown(DropdownField field, EventCallback<ChangeEvent<string>> callback)
        {
            field?.RegisterValueChangedCallback(callback);
        }

        private static void UnregisterDropdown(DropdownField field, EventCallback<ChangeEvent<string>> callback)
        {
            field?.UnregisterValueChangedCallback(callback);
        }

        private static void RegisterToggle(Toggle toggle, EventCallback<ChangeEvent<bool>> callback)
        {
            toggle?.RegisterValueChangedCallback(callback);
        }

        private static void UnregisterToggle(Toggle toggle, EventCallback<ChangeEvent<bool>> callback)
        {
            toggle?.UnregisterValueChangedCallback(callback);
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            ApplyResponsiveClasses(evt.newRect.width);
        }

        private void ApplyResponsiveClasses(float width)
        {
            if (_root == null || float.IsNaN(width) || width <= 0f)
            {
                return;
            }

            bool compact = width < CompactBreakpoint;
            bool narrow = width < NarrowBreakpoint;

            _root.EnableInClassList(CompactClass, compact);
            _root.EnableInClassList(NarrowClass, narrow);
            _root.EnableInClassList(MobileClass, narrow);
        }

        private void OnNavClickEvent(ClickEvent evt)
        {
            if (evt.currentTarget is not Button button)
            {
                return;
            }

            int index = Array.IndexOf(NavNames, button.name);
            if (index < 0)
            {
                return;
            }

            ShowPage(index);
        }

        private void ShowPage(int index)
        {
            if (_root == null || index < 0 || index >= PageNames.Length)
            {
                return;
            }

            for (int i = 0; i < PageNames.Length; i++)
            {
                var page = _root.Q<VisualElement>(PageNames[i]);
                page?.EnableInClassList(HiddenClass, i != index);
            }

            if (_sidebar != null)
            {
                _sidebar.Query<Button>(className: "settings-panel__nav-item").ForEach(button =>
                {
                    bool active = button.name == NavNames[index];
                    button.EnableInClassList("is-active", active);
                });
            }

            if (index != 3 && _privacyDetail != null)
            {
                _privacyDetail.EnableInClassList(HiddenClass, true);
            }
        }

        private void PushDraftToUi()
        {
            if (_draft == null)
            {
                return;
            }

            _suppressUiEvents = true;

            SetSlider(_sliderMaster, _fillMaster, _pctMaster, _draft.MasterVolume);
            SetSlider(_sliderMusic, _fillMusic, _pctMusic, _draft.MusicVolume);
            SetSlider(_sliderSfx, _fillSfx, _pctSfx, _draft.SfxVolume);
            SetSlider(_sliderVoice, _fillVoice, _pctVoice, _draft.VoiceVolume);
            SetSlider(_sliderBrightness, _fillBrightness, _pctBrightness, _draft.Brightness);
            SetSlider(_sliderSensitivity, _fillSensitivity, _pctSensitivity, _draft.InputSensitivity);

            SetDropdownIndex(_dropdownGraphics, _draft.GraphicsQualityIndex);
            SetDropdownIndex(_dropdownLanguage, _draft.LanguageIndex);
            SetDropdownIndex(_dropdownLanguageOverview, _draft.LanguageIndex);
            SetDropdownIndex(_dropdownTextSize, _draft.TextSizeIndex);
            SetDropdownIndex(_dropdownTextSizeOverview, _draft.TextSizeIndex);

            SetToggle(_toggleReduceMotion, _labelReduceMotion, _draft.ReduceMotion);
            SetToggle(_toggleReduceMotionOverview, _labelReduceMotionOverview, _draft.ReduceMotion);
            SetToggle(_toggleHighContrast, _labelHighContrast, _draft.HighContrast);
            SetToggle(_toggleHighContrastOverview, _labelHighContrastOverview, _draft.HighContrast);

            _suppressUiEvents = false;
        }

        private static void SetSlider(Slider slider, VisualElement fill, Label pct, float normalized)
        {
            float value = Mathf.Clamp01(normalized) * 100f;
            if (slider != null)
            {
                slider.SetValueWithoutNotify(value);
            }

            UpdateFill(fill, pct, value);
        }

        private static void UpdateFill(VisualElement fill, Label pct, float value)
        {
            float clamped = Mathf.Clamp(value, 0f, 100f);
            if (fill != null)
            {
                fill.style.width = Length.Percent(clamped);
            }

            if (pct != null)
            {
                pct.text = Mathf.RoundToInt(clamped) + "%";
            }
        }

        private static void SetDropdownIndex(DropdownField field, int index)
        {
            if (field == null || field.choices == null || field.choices.Count == 0)
            {
                return;
            }

            int clamped = Mathf.Clamp(index, 0, field.choices.Count - 1);
            field.SetValueWithoutNotify(field.choices[clamped]);
            field.index = clamped;
        }

        private static void SetToggle(Toggle toggle, Label label, bool value)
        {
            if (toggle != null)
            {
                toggle.SetValueWithoutNotify(value);
            }

            if (label != null)
            {
                label.text = value ? "On" : "Off";
            }
        }

        private void ApplyPresentationClasses()
        {
            if (_root == null || _draft == null)
            {
                return;
            }

            _root.EnableInClassList(TextSmallClass, _draft.TextSizeIndex == 0);
            _root.EnableInClassList(TextLargeClass, _draft.TextSizeIndex == 2);
            _root.EnableInClassList(TextXLargeClass, _draft.TextSizeIndex >= 3);
            _root.EnableInClassList(HighContrastClass, _draft.HighContrast);
        }

        private void OnMasterChanged(ChangeEvent<float> evt)
        {
            if (_suppressUiEvents || _draft == null)
            {
                return;
            }

            _draft.MasterVolume = evt.newValue / 100f;
            UpdateFill(_fillMaster, _pctMaster, evt.newValue);
            AudioListener.volume = Mathf.Clamp01(_draft.MasterVolume);
        }

        private void OnMusicChanged(ChangeEvent<float> evt)
        {
            if (_suppressUiEvents || _draft == null)
            {
                return;
            }

            _draft.MusicVolume = evt.newValue / 100f;
            UpdateFill(_fillMusic, _pctMusic, evt.newValue);
        }

        private void OnSfxChanged(ChangeEvent<float> evt)
        {
            if (_suppressUiEvents || _draft == null)
            {
                return;
            }

            _draft.SfxVolume = evt.newValue / 100f;
            UpdateFill(_fillSfx, _pctSfx, evt.newValue);
        }

        private void OnVoiceChanged(ChangeEvent<float> evt)
        {
            if (_suppressUiEvents || _draft == null)
            {
                return;
            }

            _draft.VoiceVolume = evt.newValue / 100f;
            UpdateFill(_fillVoice, _pctVoice, evt.newValue);
        }

        private void OnBrightnessChanged(ChangeEvent<float> evt)
        {
            if (_suppressUiEvents || _draft == null)
            {
                return;
            }

            _draft.Brightness = evt.newValue / 100f;
            UpdateFill(_fillBrightness, _pctBrightness, evt.newValue);
            _draft.ApplyRuntimeEffects();
        }

        private void OnSensitivityChanged(ChangeEvent<float> evt)
        {
            if (_suppressUiEvents || _draft == null)
            {
                return;
            }

            _draft.InputSensitivity = evt.newValue / 100f;
            UpdateFill(_fillSensitivity, _pctSensitivity, evt.newValue);
        }

        private void OnGraphicsChanged(ChangeEvent<string> evt)
        {
            if (_suppressUiEvents || _draft == null || _dropdownGraphics == null)
            {
                return;
            }

            _draft.GraphicsQualityIndex = Mathf.Max(0, _dropdownGraphics.index);
            _draft.ApplyRuntimeEffects();
        }

        private void OnLanguageChanged(ChangeEvent<string> evt)
        {
            if (_suppressUiEvents || _draft == null || _dropdownLanguage == null)
            {
                return;
            }

            _draft.LanguageIndex = Mathf.Max(0, _dropdownLanguage.index);
            _suppressUiEvents = true;
            SetDropdownIndex(_dropdownLanguageOverview, _draft.LanguageIndex);
            _suppressUiEvents = false;
        }

        private void OnLanguageOverviewChanged(ChangeEvent<string> evt)
        {
            if (_suppressUiEvents || _draft == null || _dropdownLanguageOverview == null)
            {
                return;
            }

            _draft.LanguageIndex = Mathf.Max(0, _dropdownLanguageOverview.index);
            _suppressUiEvents = true;
            SetDropdownIndex(_dropdownLanguage, _draft.LanguageIndex);
            _suppressUiEvents = false;
        }

        private void OnTextSizeChanged(ChangeEvent<string> evt)
        {
            if (_suppressUiEvents || _draft == null || _dropdownTextSize == null)
            {
                return;
            }

            _draft.TextSizeIndex = Mathf.Max(0, _dropdownTextSize.index);
            _suppressUiEvents = true;
            SetDropdownIndex(_dropdownTextSizeOverview, _draft.TextSizeIndex);
            _suppressUiEvents = false;
            ApplyPresentationClasses();
        }

        private void OnTextSizeOverviewChanged(ChangeEvent<string> evt)
        {
            if (_suppressUiEvents || _draft == null || _dropdownTextSizeOverview == null)
            {
                return;
            }

            _draft.TextSizeIndex = Mathf.Max(0, _dropdownTextSizeOverview.index);
            _suppressUiEvents = true;
            SetDropdownIndex(_dropdownTextSize, _draft.TextSizeIndex);
            _suppressUiEvents = false;
            ApplyPresentationClasses();
        }

        private void OnReduceMotionChanged(ChangeEvent<bool> evt)
        {
            if (_suppressUiEvents || _draft == null)
            {
                return;
            }

            _draft.ReduceMotion = evt.newValue;
            _suppressUiEvents = true;
            SetToggle(_toggleReduceMotion, _labelReduceMotion, evt.newValue);
            SetToggle(_toggleReduceMotionOverview, _labelReduceMotionOverview, evt.newValue);
            _suppressUiEvents = false;
        }

        private void OnReduceMotionOverviewChanged(ChangeEvent<bool> evt)
        {
            OnReduceMotionChanged(evt);
        }

        private void OnHighContrastChanged(ChangeEvent<bool> evt)
        {
            if (_suppressUiEvents || _draft == null)
            {
                return;
            }

            _draft.HighContrast = evt.newValue;
            _suppressUiEvents = true;
            SetToggle(_toggleHighContrast, _labelHighContrast, evt.newValue);
            SetToggle(_toggleHighContrastOverview, _labelHighContrastOverview, evt.newValue);
            _suppressUiEvents = false;
            ApplyPresentationClasses();
        }

        private void OnHighContrastOverviewChanged(ChangeEvent<bool> evt)
        {
            OnHighContrastChanged(evt);
        }

        private void OnRestoreDefaults(ClickEvent evt)
        {
            _draft = AppLocalSettings.CreateDefaults();
            PushDraftToUi();
            ApplyPresentationClasses();
            _draft.ApplyRuntimeEffects();
            SetStatus("Defaults restored. Press Save Changes to keep them.");
        }

        private void OnSaveChanges(ClickEvent evt)
        {
            if (_draft == null)
            {
                return;
            }

            _draft.Save();
            _draft.ApplyRuntimeEffects();
            SetStatus("Settings saved on this device.");
        }

        private void OnPrivacyClicked(ClickEvent evt)
        {
            ShowPage(3);
            _privacyDetail?.EnableInClassList(HiddenClass, false);
            SetStatus("Privacy summary opened.");
        }

        private void OnPrivacyOverviewClicked(ClickEvent evt)
        {
            OnPrivacyClicked(evt);
        }

        private void OnResetTutorial(ClickEvent evt)
        {
            if (_draft == null)
            {
                return;
            }

            _draft.TutorialCompleted = false;
            SetStatus("Tutorial reset. It will replay on next launch when wired.");
        }

        private void OnSupportClicked(ClickEvent evt)
        {
            SetStatus("Ask your teacher or classroom admin for support.");
        }

        private void SetStatus(string message)
        {
            if (_statusLabel == null)
            {
                return;
            }

            _statusLabel.text = message ?? string.Empty;
            CancelInvoke(nameof(ClearStatus));
            Invoke(nameof(ClearStatus), 3.5f);
        }

        private void ClearStatus()
        {
            if (_statusLabel != null)
            {
                _statusLabel.text = string.Empty;
            }
        }
    }
}
