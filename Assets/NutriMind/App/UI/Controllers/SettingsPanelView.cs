using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace NutriMind.App.UI
{
    /// <summary>
    /// Presentation-only settings route view. Hosts own navigation and confirmations;
    /// this view owns the local draft and all settings control presentation.
    /// </summary>
    public sealed class SettingsPanelView : IAppScreenView
    {
        private enum SettingsStatusState { Current, Saving, Saved, Error }

        private const string RootName = "settings-root";
        private const string CompactClass = "settings-panel--compact";
        private const string NarrowClass = "settings-panel--narrow";
        private const string MobileClass = "mobile";
        private const string TextSmallClass = "settings-panel--text-small";
        private const string TextLargeClass = "settings-panel--text-large";
        private const string TextXLargeClass = "settings-panel--text-xlarge";
        private const string HighContrastClass = "settings-panel--high-contrast";
        private const string HiddenClass = "settings-panel__page--hidden";
        private const string StatusSavingClass = "settings-panel__status--saving";
        private const string StatusSavedClass = "settings-panel__status--saved";
        private const string StatusErrorClass = "settings-panel__status--error";
        private const float CompactBreakpoint = 1100f;
        private const float NarrowBreakpoint = 820f;
        private const long SaveFeedbackHoldMilliseconds = 3000L;

        private static readonly string[] PageNames =
        {
            "page-audio-display", "page-accessibility", "page-language",
            "page-account", "page-help", "page-about"
        };

        private static readonly string[] NavNames =
        {
            "nav-audio-display", "nav-accessibility", "nav-language",
            "nav-account", "nav-help", "nav-about"
        };

        private VisualElement _root;
        private VisualElement _sidebar;
        private Label _statusLabel;
        private VisualElement _privacyDetail;
        private Button _backButton;
        private Button _restoreDefaultsButton;
        private Button _saveChangesButton;
        private Button _privacyButton;
        private Button _privacyOverviewButton;
        private Button _aboutPrivacyButton;
        private Button _resetTutorialButton;
        private Button _resetTutorialOverviewButton;
        private Button _replayTutorialButton;
        private Button _supportButton;
        private Slider _sliderMaster;
        private Slider _sliderMusic;
        private Slider _sliderAmbient;
        private Slider _sliderSfx;
        private Slider _sliderVoice;
        private Slider _sliderBrightness;
        private Slider _sliderSensitivity;
        private Slider _sliderSensitivityOverview;
        private VisualElement _fillMaster;
        private VisualElement _fillMusic;
        private VisualElement _fillAmbient;
        private VisualElement _fillSfx;
        private VisualElement _fillVoice;
        private VisualElement _fillBrightness;
        private VisualElement _fillSensitivity;
        private VisualElement _fillSensitivityOverview;
        private Label _pctMaster;
        private Label _pctMusic;
        private Label _pctAmbient;
        private Label _pctSfx;
        private Label _pctVoice;
        private Label _pctBrightness;
        private Label _pctSensitivity;
        private Label _pctSensitivityOverview;
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
        private AppLocalSettings _draft;
        private IVisualElementScheduledItem _clearStatusSchedule;
        private bool _suppressUiEvents;
        private bool _disposed;
        private float _lastWidth = -1f;

        public event Action BackRequested;
        public event Action SaveRequested;
        public event Action RestoreDefaultsRequested;
        public event Action ResetTutorialRequested;

        public SettingsPanelView(VisualElement root, StyleSheet dropdownPopupStyle = null)
        {
            ResolveRoot(root);
            if (_root == null)
            {
                Debug.LogWarning(
                    "[SettingsPanelView] Could not resolve settings-root inside the supplied element.");
                return;
            }

            AttachPanelScopedDropdownStyles(dropdownPopupStyle);
            CacheElements();
            _draft = AppLocalSettings.Load();
            PushDraftToUi();
            ApplyPresentationClasses();
            _draft.ApplyRuntimeEffects();
            ShowPage(0);
            RegisterCallbacks();
            ApplyResponsiveClasses(_root.resolvedStyle.width);
        }

        public VisualElement Root => _root;

        public bool IsBound => _root != null && !_disposed;

        public bool HasPreviewChanges { get; private set; }

        public void MarkPreviewSaved()
        {
            if (!IsBound || _draft == null)
            {
                return;
            }

            _draft.Save();
            _draft.ApplyRuntimeEffects();
            HasPreviewChanges = false;
            SetStatus("Settings saved on this device.", SettingsStatusState.Saved);
        }

        public void RestorePreviewDefaults()
        {
            if (!IsBound)
            {
                return;
            }

            _draft = AppLocalSettings.CreateDefaults();
            PushDraftToUi();
            ApplyPresentationClasses();
            _draft.ApplyRuntimeEffects();
            HasPreviewChanges = true;
            SetStatus("Defaults restored. Press Save Changes to keep them.", SettingsStatusState.Current);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            UnregisterCallbacks();
            _clearStatusSchedule?.Pause();
            _clearStatusSchedule = null;
            BackRequested = null;
            SaveRequested = null;
            RestoreDefaultsRequested = null;
            ResetTutorialRequested = null;
            _root = null;
            _sidebar = null;
            _statusLabel = null;
            _privacyDetail = null;
            _backButton = null;
            _restoreDefaultsButton = null;
            _saveChangesButton = null;
            _privacyButton = null;
            _privacyOverviewButton = null;
            _aboutPrivacyButton = null;
            _resetTutorialButton = null;
            _resetTutorialOverviewButton = null;
            _replayTutorialButton = null;
            _supportButton = null;
            _lastWidth = -1f;
        }

        private void ResolveRoot(VisualElement root)
        {
            if (root == null)
            {
                return;
            }

            _root = root.name == RootName ? root : root.Q<VisualElement>(RootName);
        }

        private void AttachPanelScopedDropdownStyles(StyleSheet dropdownPopupStyle)
        {
            if (dropdownPopupStyle == null || _root?.panel == null)
            {
                return;
            }

            VisualElement panelScope = _root.panel.visualTree;
            if (!panelScope.styleSheets.Contains(dropdownPopupStyle))
            {
                panelScope.styleSheets.Add(dropdownPopupStyle);
            }
        }

        private void CacheElements()
        {
            _sidebar = _root.Q<VisualElement>("settings-sidebar");
            _statusLabel = _root.Q<Label>("settings-status");
            _privacyDetail = _root.Q<VisualElement>("privacy-detail");
            _backButton = _root.Q<Button>("back-button");
            _restoreDefaultsButton = _root.Q<Button>("btn-restore-defaults");
            _saveChangesButton = _root.Q<Button>("btn-save-changes");
            _privacyButton = _root.Q<Button>("btn-privacy");
            _privacyOverviewButton = _root.Q<Button>("btn-privacy-overview");
            _aboutPrivacyButton = _root.Q<Button>("btn-about-privacy");
            _resetTutorialButton = _root.Q<Button>("btn-reset-tutorial");
            _resetTutorialOverviewButton = _root.Q<Button>("btn-reset-tutorial-overview");
            _replayTutorialButton = _root.Q<Button>("btn-replay-tutorial");
            _supportButton = _root.Q<Button>("btn-support");

            _sliderMaster = _root.Q<Slider>("slider-master");
            _sliderMusic = _root.Q<Slider>("slider-music");
            _sliderAmbient = _root.Q<Slider>("slider-ambient");
            _sliderSfx = _root.Q<Slider>("slider-sfx");
            _sliderVoice = _root.Q<Slider>("slider-voice");
            _sliderBrightness = _root.Q<Slider>("slider-brightness");
            _sliderSensitivity = _root.Q<Slider>("slider-sensitivity");
            _sliderSensitivityOverview = _root.Q<Slider>("slider-sensitivity-overview");
            _fillMaster = _root.Q<VisualElement>("fill-master");
            _fillMusic = _root.Q<VisualElement>("fill-music");
            _fillAmbient = _root.Q<VisualElement>("fill-ambient");
            _fillSfx = _root.Q<VisualElement>("fill-sfx");
            _fillVoice = _root.Q<VisualElement>("fill-voice");
            _fillBrightness = _root.Q<VisualElement>("fill-brightness");
            _fillSensitivity = _root.Q<VisualElement>("fill-sensitivity");
            _fillSensitivityOverview = _root.Q<VisualElement>("fill-sensitivity-overview");
            _pctMaster = _root.Q<Label>("pct-master");
            _pctMusic = _root.Q<Label>("pct-music");
            _pctAmbient = _root.Q<Label>("pct-ambient");
            _pctSfx = _root.Q<Label>("pct-sfx");
            _pctVoice = _root.Q<Label>("pct-voice");
            _pctBrightness = _root.Q<Label>("pct-brightness");
            _pctSensitivity = _root.Q<Label>("pct-sensitivity");
            _pctSensitivityOverview = _root.Q<Label>("pct-sensitivity-overview");
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
            _backButton?.RegisterCallback<ClickEvent>(OnBackClicked);
            if (_sidebar != null)
            {
                _sidebar.Query<Button>(className: "settings-panel__nav-item")
                    .ForEach(button => button.RegisterCallback<ClickEvent>(OnNavClickEvent));
            }

            RegisterSlider(_sliderMaster, OnMasterChanged);
            RegisterSlider(_sliderMusic, OnMusicChanged);
            RegisterSlider(_sliderAmbient, OnAmbientChanged);
            RegisterSlider(_sliderSfx, OnSfxChanged);
            RegisterSlider(_sliderVoice, OnVoiceChanged);
            RegisterSlider(_sliderBrightness, OnBrightnessChanged);
            RegisterSlider(_sliderSensitivity, OnSensitivityChanged);
            RegisterSlider(_sliderSensitivityOverview, OnSensitivityOverviewChanged);
            RegisterDropdown(_dropdownGraphics, OnGraphicsChanged);
            RegisterDropdown(_dropdownLanguage, OnLanguageChanged);
            RegisterDropdown(_dropdownLanguageOverview, OnLanguageOverviewChanged);
            RegisterDropdown(_dropdownTextSize, OnTextSizeChanged);
            RegisterDropdown(_dropdownTextSizeOverview, OnTextSizeOverviewChanged);
            RegisterToggle(_toggleReduceMotion, OnReduceMotionChanged);
            RegisterToggle(_toggleReduceMotionOverview, OnReduceMotionOverviewChanged);
            RegisterToggle(_toggleHighContrast, OnHighContrastChanged);
            RegisterToggle(_toggleHighContrastOverview, OnHighContrastOverviewChanged);
            _restoreDefaultsButton?.RegisterCallback<ClickEvent>(OnRestoreDefaultsClicked);
            _saveChangesButton?.RegisterCallback<ClickEvent>(OnSaveChangesClicked);
            _privacyButton?.RegisterCallback<ClickEvent>(OnPrivacyClicked);
            _privacyOverviewButton?.RegisterCallback<ClickEvent>(OnPrivacyClicked);
            _aboutPrivacyButton?.RegisterCallback<ClickEvent>(OnPrivacyClicked);
            _resetTutorialButton?.RegisterCallback<ClickEvent>(OnResetTutorialClicked);
            _resetTutorialOverviewButton?.RegisterCallback<ClickEvent>(OnResetTutorialClicked);
            _replayTutorialButton?.RegisterCallback<ClickEvent>(OnResetTutorialClicked);
            _supportButton?.RegisterCallback<ClickEvent>(OnSupportClicked);
            _root.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        private void UnregisterCallbacks()
        {
            _backButton?.UnregisterCallback<ClickEvent>(OnBackClicked);
            if (_sidebar != null)
            {
                _sidebar.Query<Button>(className: "settings-panel__nav-item")
                    .ForEach(button => button.UnregisterCallback<ClickEvent>(OnNavClickEvent));
            }

            UnregisterSlider(_sliderMaster, OnMasterChanged);
            UnregisterSlider(_sliderMusic, OnMusicChanged);
            UnregisterSlider(_sliderAmbient, OnAmbientChanged);
            UnregisterSlider(_sliderSfx, OnSfxChanged);
            UnregisterSlider(_sliderVoice, OnVoiceChanged);
            UnregisterSlider(_sliderBrightness, OnBrightnessChanged);
            UnregisterSlider(_sliderSensitivity, OnSensitivityChanged);
            UnregisterSlider(_sliderSensitivityOverview, OnSensitivityOverviewChanged);
            UnregisterDropdown(_dropdownGraphics, OnGraphicsChanged);
            UnregisterDropdown(_dropdownLanguage, OnLanguageChanged);
            UnregisterDropdown(_dropdownLanguageOverview, OnLanguageOverviewChanged);
            UnregisterDropdown(_dropdownTextSize, OnTextSizeChanged);
            UnregisterDropdown(_dropdownTextSizeOverview, OnTextSizeOverviewChanged);
            UnregisterToggle(_toggleReduceMotion, OnReduceMotionChanged);
            UnregisterToggle(_toggleReduceMotionOverview, OnReduceMotionOverviewChanged);
            UnregisterToggle(_toggleHighContrast, OnHighContrastChanged);
            UnregisterToggle(_toggleHighContrastOverview, OnHighContrastOverviewChanged);
            _restoreDefaultsButton?.UnregisterCallback<ClickEvent>(OnRestoreDefaultsClicked);
            _saveChangesButton?.UnregisterCallback<ClickEvent>(OnSaveChangesClicked);
            _privacyButton?.UnregisterCallback<ClickEvent>(OnPrivacyClicked);
            _privacyOverviewButton?.UnregisterCallback<ClickEvent>(OnPrivacyClicked);
            _aboutPrivacyButton?.UnregisterCallback<ClickEvent>(OnPrivacyClicked);
            _resetTutorialButton?.UnregisterCallback<ClickEvent>(OnResetTutorialClicked);
            _resetTutorialOverviewButton?.UnregisterCallback<ClickEvent>(OnResetTutorialClicked);
            _replayTutorialButton?.UnregisterCallback<ClickEvent>(OnResetTutorialClicked);
            _supportButton?.UnregisterCallback<ClickEvent>(OnSupportClicked);
            _root?.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
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

        private void OnBackClicked(ClickEvent evt) => BackRequested?.Invoke();

        private void OnGeometryChanged(GeometryChangedEvent evt) =>
            ApplyResponsiveClasses(evt.newRect.width);

        private void ApplyResponsiveClasses(float width)
        {
            if (_root == null || float.IsNaN(width) || width <= 0f ||
                Mathf.Approximately(width, _lastWidth))
            {
                return;
            }

            _lastWidth = width;
            bool compact = width < CompactBreakpoint;
            bool narrow = width < NarrowBreakpoint;
            _root.EnableInClassList(CompactClass, compact);
            _root.EnableInClassList(NarrowClass, narrow);
            _root.EnableInClassList(MobileClass, narrow);
        }

        private void OnNavClickEvent(ClickEvent evt)
        {
            if (evt.currentTarget is Button button)
            {
                int index = Array.IndexOf(NavNames, button.name);
                if (index >= 0)
                {
                    ShowPage(index);
                }
            }
        }

        private void ShowPage(int index)
        {
            if (_root == null || index < 0 || index >= PageNames.Length)
            {
                return;
            }

            for (int i = 0; i < PageNames.Length; i++)
            {
                _root.Q<VisualElement>(PageNames[i])?.EnableInClassList(HiddenClass, i != index);
            }

            _sidebar?.Query<Button>(className: "settings-panel__nav-item").ForEach(button =>
                button.EnableInClassList("is-active", button.name == NavNames[index]));

            if (index != 3)
            {
                _privacyDetail?.EnableInClassList(HiddenClass, true);
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
            SetSlider(_sliderAmbient, _fillAmbient, _pctAmbient, _draft.AmbientVolume);
            SetSlider(_sliderSfx, _fillSfx, _pctSfx, _draft.SfxVolume);
            SetSlider(_sliderVoice, _fillVoice, _pctVoice, _draft.VoiceVolume);
            SetSlider(_sliderBrightness, _fillBrightness, _pctBrightness, _draft.Brightness);
            SetSlider(_sliderSensitivity, _fillSensitivity, _pctSensitivity, _draft.InputSensitivity);
            SetSlider(_sliderSensitivityOverview, _fillSensitivityOverview, _pctSensitivityOverview, _draft.InputSensitivity);
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
            slider?.SetValueWithoutNotify(value);
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
                pct.text = $"{Mathf.RoundToInt(clamped)}%";
            }
        }

        private static void SetDropdownIndex(DropdownField field, int index)
        {
            if (field?.choices == null || field.choices.Count == 0)
            {
                return;
            }

            int clamped = Mathf.Clamp(index, 0, field.choices.Count - 1);
            field.SetValueWithoutNotify(field.choices[clamped]);
            field.index = clamped;
        }

        private static void SetToggle(Toggle toggle, Label label, bool value)
        {
            toggle?.SetValueWithoutNotify(value);
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
            if (!CanApplyChange()) return;
            _draft.MasterVolume = evt.newValue / 100f;
            UpdateFill(_fillMaster, _pctMaster, evt.newValue);
            AudioListener.volume = Mathf.Clamp01(_draft.MasterVolume);
            MarkChanged();
        }

        private void OnMusicChanged(ChangeEvent<float> evt)
        {
            if (!CanApplyChange()) return;
            _draft.MusicVolume = evt.newValue / 100f;
            UpdateFill(_fillMusic, _pctMusic, evt.newValue);
            MarkChanged();
        }

        private void OnAmbientChanged(ChangeEvent<float> evt)
        {
            if (!CanApplyChange()) return;
            _draft.AmbientVolume = evt.newValue / 100f;
            UpdateFill(_fillAmbient, _pctAmbient, evt.newValue);
            MarkChanged();
        }

        private void OnSfxChanged(ChangeEvent<float> evt)
        {
            if (!CanApplyChange()) return;
            _draft.SfxVolume = evt.newValue / 100f;
            UpdateFill(_fillSfx, _pctSfx, evt.newValue);
            MarkChanged();
        }

        private void OnVoiceChanged(ChangeEvent<float> evt)
        {
            if (!CanApplyChange()) return;
            _draft.VoiceVolume = evt.newValue / 100f;
            UpdateFill(_fillVoice, _pctVoice, evt.newValue);
            MarkChanged();
        }

        private void OnBrightnessChanged(ChangeEvent<float> evt)
        {
            if (!CanApplyChange()) return;
            _draft.Brightness = evt.newValue / 100f;
            UpdateFill(_fillBrightness, _pctBrightness, evt.newValue);
            _draft.ApplyRuntimeEffects();
            MarkChanged();
        }

        private void OnSensitivityChanged(ChangeEvent<float> evt) =>
            SetSensitivity(evt.newValue, _fillSensitivity, _pctSensitivity,
                _sliderSensitivityOverview, _fillSensitivityOverview, _pctSensitivityOverview);

        private void OnSensitivityOverviewChanged(ChangeEvent<float> evt) =>
            SetSensitivity(evt.newValue, _fillSensitivityOverview, _pctSensitivityOverview,
                _sliderSensitivity, _fillSensitivity, _pctSensitivity);

        private void SetSensitivity(
            float value, VisualElement changedFill, Label changedLabel, Slider mirrorSlider,
            VisualElement mirrorFill, Label mirrorLabel)
        {
            if (!CanApplyChange()) return;
            _draft.InputSensitivity = value / 100f;
            UpdateFill(changedFill, changedLabel, value);
            _suppressUiEvents = true;
            SetSlider(mirrorSlider, mirrorFill, mirrorLabel, _draft.InputSensitivity);
            _suppressUiEvents = false;
            MarkChanged();
        }

        private void OnGraphicsChanged(ChangeEvent<string> evt)
        {
            if (!CanApplyChange() || _dropdownGraphics == null) return;
            _draft.GraphicsQualityIndex = Mathf.Max(0, _dropdownGraphics.index);
            _draft.ApplyRuntimeEffects();
            MarkChanged();
        }

        private void OnLanguageChanged(ChangeEvent<string> evt) =>
            HandleLanguageSelection(_dropdownLanguage?.index ?? 0);

        private void OnLanguageOverviewChanged(ChangeEvent<string> evt) =>
            HandleLanguageSelection(_dropdownLanguageOverview?.index ?? 0);

        private void HandleLanguageSelection(int selectedIndex)
        {
            if (!CanApplyChange()) return;
            _draft.LanguageIndex = 0;
            _suppressUiEvents = true;
            SetDropdownIndex(_dropdownLanguage, 0);
            SetDropdownIndex(_dropdownLanguageOverview, 0);
            _suppressUiEvents = false;
            if (selectedIndex != 0)
            {
                SetStatus("That language is coming soon — English stays selected for now.", SettingsStatusState.Current);
            }
        }

        private void OnTextSizeChanged(ChangeEvent<string> evt) =>
            SetTextSize(_dropdownTextSize?.index ?? 0, _dropdownTextSizeOverview);

        private void OnTextSizeOverviewChanged(ChangeEvent<string> evt) =>
            SetTextSize(_dropdownTextSizeOverview?.index ?? 0, _dropdownTextSize);

        private void SetTextSize(int index, DropdownField mirror)
        {
            if (!CanApplyChange()) return;
            _draft.TextSizeIndex = Mathf.Max(0, index);
            _suppressUiEvents = true;
            SetDropdownIndex(mirror, _draft.TextSizeIndex);
            _suppressUiEvents = false;
            ApplyPresentationClasses();
            MarkChanged();
        }

        private void OnReduceMotionChanged(ChangeEvent<bool> evt) => SetReduceMotion(evt.newValue);
        private void OnReduceMotionOverviewChanged(ChangeEvent<bool> evt) => SetReduceMotion(evt.newValue);

        private void SetReduceMotion(bool value)
        {
            if (!CanApplyChange()) return;
            _draft.ReduceMotion = value;
            _suppressUiEvents = true;
            SetToggle(_toggleReduceMotion, _labelReduceMotion, value);
            SetToggle(_toggleReduceMotionOverview, _labelReduceMotionOverview, value);
            _suppressUiEvents = false;
            MarkChanged();
        }

        private void OnHighContrastChanged(ChangeEvent<bool> evt) => SetHighContrast(evt.newValue);
        private void OnHighContrastOverviewChanged(ChangeEvent<bool> evt) => SetHighContrast(evt.newValue);

        private void SetHighContrast(bool value)
        {
            if (!CanApplyChange()) return;
            _draft.HighContrast = value;
            _suppressUiEvents = true;
            SetToggle(_toggleHighContrast, _labelHighContrast, value);
            SetToggle(_toggleHighContrastOverview, _labelHighContrastOverview, value);
            _suppressUiEvents = false;
            ApplyPresentationClasses();
            MarkChanged();
        }

        private bool CanApplyChange() => !_suppressUiEvents && _draft != null;
        private void MarkChanged() => HasPreviewChanges = true;

        private void OnRestoreDefaultsClicked(ClickEvent evt) => RestoreDefaultsRequested?.Invoke();

        private void OnSaveChangesClicked(ClickEvent evt)
        {
            if (_draft == null) return;
            SetStatus("Saving...", SettingsStatusState.Saving);
            SaveRequested?.Invoke();
        }

        private void OnPrivacyClicked(ClickEvent evt)
        {
            ShowPage(3);
            _privacyDetail?.EnableInClassList(HiddenClass, false);
            SetStatus("Privacy summary opened.", SettingsStatusState.Current);
        }

        private void OnResetTutorialClicked(ClickEvent evt) => ResetTutorialRequested?.Invoke();

        private void OnSupportClicked(ClickEvent evt) =>
            SetStatus("Ask your teacher or classroom admin for support.", SettingsStatusState.Current);

        private void SetStatus(string message, SettingsStatusState state)
        {
            if (_statusLabel == null) return;

            _clearStatusSchedule?.Pause();
            _statusLabel.text = message ?? string.Empty;
            _statusLabel.EnableInClassList(StatusSavingClass, state == SettingsStatusState.Saving);
            _statusLabel.EnableInClassList(StatusSavedClass, state == SettingsStatusState.Saved);
            _statusLabel.EnableInClassList(StatusErrorClass, state == SettingsStatusState.Error);

            if (state != SettingsStatusState.Saving && _root != null)
            {
                _clearStatusSchedule = _root.schedule.Execute(ClearStatus)
                    .StartingIn(SaveFeedbackHoldMilliseconds);
            }
        }

        private void ClearStatus()
        {
            if (_statusLabel == null) return;
            _statusLabel.text = string.Empty;
            _statusLabel.RemoveFromClassList(StatusSavingClass);
            _statusLabel.RemoveFromClassList(StatusSavedClass);
            _statusLabel.RemoveFromClassList(StatusErrorClass);
            _clearStatusSchedule?.Pause();
            _clearStatusSchedule = null;
        }
    }
}
