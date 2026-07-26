using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace NutriMind.App.UI
{
    /// <summary>
    /// Static content-screen choices available to the AppShell preview host.
    /// Bootstrap and Login are intentionally excluded because they live outside
    /// the authenticated application shell.
    /// </summary>
    public enum AppShellContentPreviewScreen
    {
        None,
        Home,
        Subjects,
        Terms,
        Missions,
        LockedMission,
        Profile,
        Settings,
        Progress,
        QuizList,
        QuizDetail,
        QuizAttempt,
        QuizResult
    }

    /// <summary>
    /// Serialized presentation metadata for one content-only static preview.
    /// Contains no route history, scene, authentication, server, or learner state.
    /// </summary>
    [System.Serializable]
    public sealed class AppShellContentPreviewEntry
    {
        [SerializeField]
        private AppShellContentPreviewScreen _screen;

        [SerializeField]
        private VisualTreeAsset _contentAsset;

        [SerializeField]
        private AppShellPreviewRoute _activeNavigation = AppShellPreviewRoute.Home;

        [SerializeField]
        private string _pageTitle = "Home";

        [SerializeField]
        private string _pageContext;

        [SerializeField]
        private bool _selectFromBottomNavigation = true;

        public AppShellContentPreviewScreen Screen => _screen;

        public VisualTreeAsset ContentAsset => _contentAsset;

        public AppShellPreviewRoute ActiveNavigation => _activeNavigation;

        public string PageTitle => _pageTitle;

        public string PageContext => _pageContext;

        public bool SelectFromBottomNavigation => _selectFromBottomNavigation;
    }

    /// <summary>
    /// Presentation-only host that clones one serialized content UXML into AppShell.
    /// This component is a static UI preview mechanism, not a production router.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AppShellController))]
    public sealed class AppShellContentPreviewController : MonoBehaviour
    {
        private enum PreviewConfirmationAction
        {
            None,
            SignOut,
            RestoreDefaults,
            ResetTutorial
        }

        private const string ContentInstanceClass = "app-shell__content-instance";
        private const string ScreenContentClass = "app-screen-content";
        private const string EmbeddedContentClass = "app-screen-content--embedded";

        [SerializeField]
        [Tooltip("Static screen preview shown inside AppShell. This is not production routing.")]
        private AppShellContentPreviewScreen _previewScreen =
            AppShellContentPreviewScreen.None;

        [SerializeField]
        [Tooltip("Content-only screen UXML assets and shell metadata used for static preview.")]
        private List<AppShellContentPreviewEntry> _previewEntries = new();

        [SerializeField]
        [Tooltip("Shared DataStatePanel UXML used when a preview screen is not assigned.")]
        private VisualTreeAsset _dataStatePanelAsset;

        [SerializeField]
        [Tooltip("Shared ConfirmDialog UXML used for Profile and Settings preview confirmations.")]
        private VisualTreeAsset _confirmDialogAsset;

        [SerializeField]
        [Tooltip("When enabled, AppShell bottom-navigation clicks select matching preview entries.")]
        private bool _respondToShellNavigation = true;

        [SerializeField]
        [Tooltip("When enabled, the AppShell profile button selects the Profile preview entry.")]
        private bool _respondToProfileRequest = true;

        [SerializeField]
        private NutriMindSubject _selectedSubject = NutriMindSubject.LiteraQuest;

        [SerializeField]
        private NutriMindTerm _selectedTerm = NutriMindTerm.Term1;

        [SerializeField]
        [Tooltip("DataStatePanel state used only while previewing Progress inside AppShell.")]
        private DataStatePanelState _progressPreviewState = DataStatePanelState.Content;

        private MissionPreviewSelection _selectedMission =
            new(2, "Needs of Living Things", false, string.Empty);

        private LockedMissionPreviewContext _lockedMissionContext =
            new(
                NutriMindSubject.Science,
                NutriMindTerm.Term2,
                5,
                "Ecosystems and Balance",
                MissionLockReason.TeacherRestricted,
                "Prerequisite complete — no additional missions required.");

        private AppShellController _appShell;
        private VisualElement _contentRegion;

        private TemplateContainer _currentContentInstance;
        private VisualElement _currentContentRoot;
        private IAppScreenView _currentScreenView;
        private HomePanelView _currentHomeView;
        private SubjectSelectionPanelView _currentSubjectSelectionView;
        private TermSelectionPanelView _currentTermSelectionView;
        private MissionSelectionPanelView _currentMissionSelectionView;
        private LockedMissionPanelView _currentLockedMissionView;
        private ProfilePanelView _currentProfileView;
        private SettingsPanelView _currentSettingsView;
        private ProgressPanelView _currentProgressView;

        private TemplateContainer _fallbackInstance;
        private DataStatePanelView _fallbackView;

        private TemplateContainer _confirmDialogInstance;
        private ConfirmDialogView _confirmDialogView;
        private PreviewConfirmationAction _pendingConfirmation;
        private bool _warnedMissingConfirmDialogAsset;

        private AppShellContentPreviewScreen? _appliedScreen;
        private VisualTreeAsset _appliedAsset;
        private string _appliedTitle;
        private string _appliedContext;
        private AppShellPreviewRoute? _appliedNavigation;

        private bool _isBound;
        private bool _warnedMissingDataStateAsset;
        private bool _entriesValidated;
        private bool _requestFallbackActive;
        private AppShellContentPreviewScreen _requestFallbackSelection;

        public AppShellContentPreviewScreen PreviewScreen => _previewScreen;

        public VisualElement CurrentContentRoot => _currentContentRoot;

        public bool HasCurrentContent => _currentContentInstance != null;

        private void OnEnable()
        {
            _appShell = GetComponent<AppShellController>();
            BindWhenReady();
        }

        private void OnDisable()
        {
            CancelInvoke(nameof(BindWhenReady));
            Unbind();
        }

        private void OnValidate()
        {
            ValidatePreviewEntries();
            _entriesValidated = true;

            if (!isActiveAndEnabled || !_isBound)
            {
                return;
            }

            _requestFallbackActive = false;
            ApplyPreviewScreen(_previewScreen, force: true);
        }

        private void Update()
        {
            if (!_isBound)
            {
                return;
            }

            if (_requestFallbackActive)
            {
                if (_previewScreen == _requestFallbackSelection)
                {
                    return;
                }

                _requestFallbackActive = false;
            }

            ApplyPreviewScreen(_previewScreen);
        }

        /// <summary>
        /// Selects and immediately applies a static content preview when bound.
        /// Does not perform production navigation.
        /// </summary>
        public void SetPreviewScreen(AppShellContentPreviewScreen screen)
        {
            _previewScreen = screen;
            _requestFallbackActive = false;

            if (_isBound)
            {
                ApplyPreviewScreen(screen);
            }
        }

        private void BindWhenReady()
        {
            if (_isBound)
            {
                return;
            }

            if (_appShell == null)
            {
                _appShell = GetComponent<AppShellController>();
            }

            if (_appShell == null)
            {
                return;
            }

            _contentRegion = _appShell.GetContentRegion();
            if (_contentRegion == null)
            {
                Invoke(nameof(BindWhenReady), 0.05f);
                return;
            }

            _appShell.PreviewRouteRequested += OnPreviewRouteRequested;
            _appShell.ProfileRequested += OnProfileRequested;
            _appShell.NotificationsRequested += OnNotificationsRequested;
            BindConfirmDialog();
            _isBound = true;

            if (!_entriesValidated)
            {
                ValidatePreviewEntries();
                _entriesValidated = true;
            }

            ApplyPreviewScreen(_previewScreen, force: true);
        }

        private void Unbind()
        {
            if (_appShell != null && _isBound)
            {
                _appShell.PreviewRouteRequested -= OnPreviewRouteRequested;
                _appShell.ProfileRequested -= OnProfileRequested;
                _appShell.NotificationsRequested -= OnNotificationsRequested;
            }

            ClearCurrentContent();
            ClearFallback();
            UnbindConfirmDialog();

            _contentRegion = null;
            _appShell = null;
            _isBound = false;
            _requestFallbackActive = false;
            ResetAppliedTracking();
        }

        private AppShellContentPreviewEntry FindEntry(
            AppShellContentPreviewScreen screen)
        {
            if (screen == AppShellContentPreviewScreen.None || _previewEntries == null)
            {
                return null;
            }

            for (int i = 0; i < _previewEntries.Count; i++)
            {
                AppShellContentPreviewEntry entry = _previewEntries[i];
                if (entry != null && entry.Screen == screen)
                {
                    return entry;
                }
            }

            return null;
        }

        private AppShellContentPreviewEntry FindEntryForNavigation(
            AppShellPreviewRoute route)
        {
            if (_previewEntries == null)
            {
                return null;
            }

            for (int i = 0; i < _previewEntries.Count; i++)
            {
                AppShellContentPreviewEntry entry = _previewEntries[i];
                if (entry != null
                    && entry.SelectFromBottomNavigation
                    && entry.ActiveNavigation == route)
                {
                    return entry;
                }
            }

            return null;
        }

        private void ApplyPreviewScreen(
            AppShellContentPreviewScreen screen,
            bool force = false)
        {
            if (!_isBound || _contentRegion == null)
            {
                return;
            }

            AppShellContentPreviewEntry entry = FindEntry(screen);
            VisualTreeAsset asset = entry?.ContentAsset;
            string title = entry == null
                ? null
                : ResolvePageTitle(entry.PageTitle, screen);
            string context = entry == null
                ? null
                : ResolvePageContext(entry, screen);
            AppShellPreviewRoute? navigation = entry?.ActiveNavigation;

            if (!force
                && _appliedScreen == screen
                && _appliedAsset == asset
                && _appliedTitle == title
                && _appliedContext == context
                && _appliedNavigation == navigation)
            {
                return;
            }

            _requestFallbackActive = false;

            if (screen == AppShellContentPreviewScreen.None)
            {
                ClearCurrentContent();
                ShowFallback(
                    "AppShell content preview",
                    "Select a migrated application screen in the AppShell Content Preview component.",
                    "Existing panels remain standalone until their migration prompt is completed.");
                SetAppliedTracking(screen, null, null, null, null);
                return;
            }

            if (entry == null)
            {
                ClearCurrentContent();
                ShowFallback(
                    "Preview screen is not configured",
                    $"No AppShell content entry is available for {GetScreenDisplayName(screen)}.",
                    "Add a content-only UXML asset after that panel has been migrated.");
                SetAppliedTracking(screen, null, null, null, null);
                return;
            }

            ApplyShellMetadata(entry, screen);

            if (asset == null)
            {
                ClearCurrentContent();
                ShowFallback(
                    $"{GetScreenDisplayName(screen)} preview is not assigned",
                    "Assign the migrated content-only UXML asset to this AppShell preview entry.",
                    "The existing standalone panel has not been replaced.");
                SetAppliedTracking(screen, null, title, context, navigation);
                return;
            }

            ClearCurrentContent();
            ClearFallback();

            TemplateContainer instance = asset.CloneTree();
            instance.style.width = Length.Percent(100);
            instance.style.height = Length.Percent(100);
            instance.style.flexGrow = 1;
            instance.style.flexShrink = 1;
            instance.AddToClassList(ContentInstanceClass);

            _contentRegion.Add(instance);
            _currentContentInstance = instance;
            _currentContentRoot = ResolveContentRoot(instance);
            if (_currentContentRoot != null)
            {
                _currentContentRoot.AddToClassList(ScreenContentClass);
                _currentContentRoot.AddToClassList(EmbeddedContentClass);
            }

            _currentScreenView = CreateScreenView(screen, _currentContentRoot);
            SetAppliedTracking(screen, asset, title, context, navigation);

            Debug.Log(
                $"[AppShellContentPreview] Showing {GetScreenDisplayName(screen)} " +
                "inside AppShell (preview only).");
        }

        private void ApplyShellMetadata(
            AppShellContentPreviewEntry entry,
            AppShellContentPreviewScreen screen)
        {
            if (_appShell == null || entry == null)
            {
                return;
            }

            _appShell.SetPreviewRoute(entry.ActiveNavigation);
            _appShell.SetPageTitle(
                ResolvePageTitle(entry.PageTitle, screen),
                ResolvePageContext(entry, screen));
        }

        private void RefreshShellPageContext()
        {
            if (!_isBound || _appShell == null || _previewScreen == AppShellContentPreviewScreen.None)
            {
                return;
            }

            AppShellContentPreviewEntry entry = FindEntry(_previewScreen);
            if (entry == null)
            {
                return;
            }

            string title = ResolvePageTitle(entry.PageTitle, _previewScreen);
            string context = ResolvePageContext(entry, _previewScreen);
            _appShell.SetPageTitle(title, context);
            _appliedTitle = title;
            _appliedContext = context;
        }

        private string ResolvePageContext(
            AppShellContentPreviewEntry entry,
            AppShellContentPreviewScreen screen)
        {
            switch (screen)
            {
                case AppShellContentPreviewScreen.Home:
                case AppShellContentPreviewScreen.Profile:
                    return "Grade 5 • Section Emerald";

                case AppShellContentPreviewScreen.Subjects:
                    return "Grade 5";

                case AppShellContentPreviewScreen.Terms:
                    return GetSubjectLabel(_selectedSubject);

                case AppShellContentPreviewScreen.Missions:
                    return $"{GetSubjectLabel(_selectedSubject)} • Term {(int)_selectedTerm}";

                case AppShellContentPreviewScreen.LockedMission:
                    return
                        $"{GetSubjectLabel(_lockedMissionContext.Subject)} • " +
                        $"Term {(int)_lockedMissionContext.Term} • " +
                        $"Mission {_lockedMissionContext.MissionNumber}";

                case AppShellContentPreviewScreen.Settings:
                    return "Profile & application";

                case AppShellContentPreviewScreen.Progress:
                    return $"Grade 5 • {GetSubjectLabel(_selectedSubject)}";

                default:
                    return NormalizeOptionalText(entry?.PageContext);
            }
        }

        private static VisualElement ResolveContentRoot(
            TemplateContainer instance)
        {
            if (instance == null)
            {
                return null;
            }

            return instance.childCount == 1
                ? instance.ElementAt(0)
                : instance;
        }

        private IAppScreenView CreateScreenView(
            AppShellContentPreviewScreen screen,
            VisualElement contentRoot)
        {
            switch (screen)
            {
                case AppShellContentPreviewScreen.Home:
                    return CreateHomeView(contentRoot);

                case AppShellContentPreviewScreen.Subjects:
                    return CreateSubjectSelectionView(contentRoot);

                case AppShellContentPreviewScreen.Terms:
                    return CreateTermSelectionView(contentRoot);

                case AppShellContentPreviewScreen.Missions:
                    return CreateMissionSelectionView(contentRoot);

                case AppShellContentPreviewScreen.LockedMission:
                    return CreateLockedMissionView(contentRoot);

                case AppShellContentPreviewScreen.Profile:
                    return CreateProfileView(contentRoot);

                case AppShellContentPreviewScreen.Settings:
                    return CreateSettingsView(contentRoot);

                case AppShellContentPreviewScreen.Progress:
                    return CreateProgressView(contentRoot);

                default:
                    return null;
            }
        }

        private IAppScreenView CreateHomeView(VisualElement contentRoot)
        {
            var homeView = new HomePanelView(contentRoot);
            if (!homeView.IsBound)
            {
                Debug.LogWarning(
                    "[AppShellContentPreview] HomePanelView failed to bind home-root.");
                homeView.Dispose();
                return null;
            }

            homeView.ContinueMissionRequested += OnHomeContinueMissionRequested;
            homeView.QuizPortalRequested += OnHomeQuizPortalRequested;
            _currentHomeView = homeView;
            return homeView;
        }

        private void OnHomeContinueMissionRequested()
        {
            Debug.Log(
                "[AppShellContentPreview] Home Continue Mission requested — preview only.");

            _appShell?.ShowToast(
                "Continue Mission selected. Mission routing will be connected later.",
                AppShellToastTone.Information);
        }

        private void OnHomeQuizPortalRequested()
        {
            Debug.Log(
                "[AppShellContentPreview] Home Quiz Portal requested — preview only.");

            _appShell?.ShowToast(
                "Quiz Portal selected. Quiz routing will be connected later.",
                AppShellToastTone.Information);
        }

        private IAppScreenView CreateSubjectSelectionView(
            VisualElement contentRoot)
        {
            var subjectView = new SubjectSelectionPanelView(contentRoot);
            if (!subjectView.IsBound)
            {
                Debug.LogWarning(
                    "[AppShellContentPreview] SubjectSelectionPanelView failed to bind " +
                    "subject-selection-root.");
                subjectView.Dispose();
                return null;
            }

            subjectView.BackRequested += OnSubjectBackRequested;
            subjectView.SubjectSelected += OnSubjectSelected;
            subjectView.ContinueSubjectRequested += OnContinueSubjectRequested;
            subjectView.UnavailableSubjectRequested += OnUnavailableSubjectRequested;
            _currentSubjectSelectionView = subjectView;
            return subjectView;
        }

        private void OnSubjectBackRequested()
        {
            Debug.Log(
                "[AppShellContentPreview] Subjects Back requested — showing Home preview.");

            SetPreviewScreen(AppShellContentPreviewScreen.Home);
        }

        private void OnSubjectSelected(NutriMindSubject subject)
        {
            _selectedSubject = subject;
            Debug.Log(
                $"[AppShellContentPreview] Subject selected: {GetSubjectLabel(subject)}.");
        }

        private void OnContinueSubjectRequested(NutriMindSubject subject)
        {
            _selectedSubject = subject;
            string label = GetSubjectLabel(subject);

            Debug.Log(
                $"[AppShellContentPreview] View Terms requested: {label} — showing Terms.");

            SetPreviewScreen(AppShellContentPreviewScreen.Terms);
        }

        private void OnUnavailableSubjectRequested(NutriMindSubject subject)
        {
            string label = GetSubjectLabel(subject);

            Debug.Log(
                $"[AppShellContentPreview] {label} unavailable in classroom preview.");

            _appShell?.ShowToast(
                $"{label} is not available in your classroom yet.",
                AppShellToastTone.Warning);
        }

        private IAppScreenView CreateTermSelectionView(VisualElement contentRoot)
        {
            var termView = new TermSelectionPanelView(contentRoot);
            if (!termView.IsBound)
            {
                Debug.LogWarning(
                    "[AppShellContentPreview] TermSelectionPanelView failed to bind " +
                    "term-selection-root.");
                termView.Dispose();
                return null;
            }

            termView.SetSubject(_selectedSubject);
            termView.BackRequested += OnTermBackRequested;
            termView.TermSelected += OnTermSelected;
            termView.OpenTermRequested += OnOpenTermRequested;
            termView.UnavailableTermRequested += OnUnavailableTermRequested;
            _currentTermSelectionView = termView;
            return termView;
        }

        private void OnTermBackRequested()
        {
            Debug.Log(
                "[AppShellContentPreview] Terms Back requested — showing Subjects preview.");

            SetPreviewScreen(AppShellContentPreviewScreen.Subjects);
        }

        private void OnTermSelected(NutriMindTerm term)
        {
            _selectedTerm = term;
            RefreshShellPageContext();
            Debug.Log(
                $"[AppShellContentPreview] Term selected: Term {(int)term}.");
        }

        private void OnOpenTermRequested(NutriMindTerm term)
        {
            _selectedTerm = term;
            Debug.Log(
                $"[AppShellContentPreview] Open Term {(int)term} — showing Missions.");

            SetPreviewScreen(AppShellContentPreviewScreen.Missions);
        }

        private void OnUnavailableTermRequested(NutriMindTerm term)
        {
            string reason = _currentTermSelectionView != null
                ? _currentTermSelectionView.GetUnavailableReason(term)
                : "Previous Term Incomplete";

            Debug.Log(
                $"[AppShellContentPreview] Term {(int)term} unavailable: {reason}.");

            _appShell?.ShowToast(reason, AppShellToastTone.Warning);
        }

        private IAppScreenView CreateMissionSelectionView(VisualElement contentRoot)
        {
            var missionView = new MissionSelectionPanelView(contentRoot);
            if (!missionView.IsBound)
            {
                Debug.LogWarning(
                    "[AppShellContentPreview] MissionSelectionPanelView failed to bind " +
                    "mission-selection-root.");
                missionView.Dispose();
                return null;
            }

            missionView.SetContext(_selectedSubject, _selectedTerm);
            missionView.BackRequested += OnMissionBackRequested;
            missionView.MissionSelected += OnMissionSelected;
            missionView.StartMissionRequested += OnStartMissionRequested;
            missionView.ContinueMissionRequested += OnContinueMissionRequested;
            missionView.ReviewMissionRequested += OnReviewMissionRequested;
            missionView.LockedMissionRequested += OnLockedMissionRequested;
            _currentMissionSelectionView = missionView;
            return missionView;
        }

        private void OnMissionBackRequested()
        {
            Debug.Log(
                "[AppShellContentPreview] Missions Back requested — showing Terms preview.");

            SetPreviewScreen(AppShellContentPreviewScreen.Terms);
        }

        private void OnMissionSelected(MissionPreviewSelection selection)
        {
            _selectedMission = selection;
            RefreshShellPageContext();
            Debug.Log(
                $"[AppShellContentPreview] Mission selected: {selection.MissionTitle}.");
        }

        private void OnStartMissionRequested(MissionPreviewSelection selection)
        {
            _selectedMission = selection;
            ShowMissionActionToast("Start", selection.MissionTitle);
        }

        private void OnContinueMissionRequested(MissionPreviewSelection selection)
        {
            _selectedMission = selection;
            ShowMissionActionToast("Continue", selection.MissionTitle);
        }

        private void OnReviewMissionRequested(MissionPreviewSelection selection)
        {
            _selectedMission = selection;
            ShowMissionActionToast("Review", selection.MissionTitle);
        }

        private void ShowMissionActionToast(string action, string missionTitle)
        {
            Debug.Log(
                $"[AppShellContentPreview] {action} selected for {missionTitle} — preview only.");

            _appShell?.ShowToast(
                $"{action} selected for {missionTitle}. Gameplay routing is not connected in this static preview.",
                AppShellToastTone.Information);
        }

        private void OnLockedMissionRequested(MissionPreviewSelection selection)
        {
            _selectedMission = selection;
            _lockedMissionContext = BuildLockedContext(selection);

            Debug.Log(
                $"[AppShellContentPreview] Locked mission requested: {selection.MissionTitle}.");

            SetPreviewScreen(AppShellContentPreviewScreen.LockedMission);
        }

        private LockedMissionPreviewContext BuildLockedContext(
            MissionPreviewSelection selection)
        {
            MissionLockReason reason = selection.MissionNumber == 4
                ? MissionLockReason.PrerequisiteRequired
                : MissionLockReason.TeacherRestricted;

            string requirement = selection.MissionNumber == 4
                ? "Requires: Habitats Around Us (Mission 3)"
                : "Prerequisite complete — no additional missions required.";

            if (!string.IsNullOrWhiteSpace(selection.LockReason)
                && selection.MissionNumber == 4)
            {
                requirement = selection.LockReason.Contains("Requires")
                    ? selection.LockReason
                    : requirement;
            }

            return new LockedMissionPreviewContext(
                _selectedSubject,
                _selectedTerm,
                selection.MissionNumber,
                selection.MissionTitle,
                reason,
                requirement);
        }

        private IAppScreenView CreateLockedMissionView(VisualElement contentRoot)
        {
            var lockedView = new LockedMissionPanelView(contentRoot);
            if (!lockedView.IsBound)
            {
                Debug.LogWarning(
                    "[AppShellContentPreview] LockedMissionPanelView failed to bind " +
                    "locked-mission-root.");
                lockedView.Dispose();
                return null;
            }

            lockedView.SetContext(_lockedMissionContext);
            lockedView.BackRequested += OnLockedBackRequested;
            lockedView.PrimaryActionRequested += OnLockedPrimaryActionRequested;
            lockedView.SecondaryActionRequested += OnLockedSecondaryActionRequested;
            _currentLockedMissionView = lockedView;
            return lockedView;
        }

        private void OnLockedBackRequested()
        {
            Debug.Log(
                "[AppShellContentPreview] Locked Mission Back — showing Missions.");

            SetPreviewScreen(AppShellContentPreviewScreen.Missions);
        }

        private void OnLockedPrimaryActionRequested()
        {
            MissionLockReason reason = _lockedMissionContext.Reason;

            Debug.Log(
                $"[AppShellContentPreview] Locked Mission primary action ({reason}).");

            switch (reason)
            {
                case MissionLockReason.PrerequisiteRequired:
                    SetPreviewScreen(AppShellContentPreviewScreen.Missions);
                    Debug.Log(
                        "[AppShellContentPreview] Required mission: Habitats Around Us (Mission 3).");
                    break;

                case MissionLockReason.NotDownloaded:
                    _appShell?.ShowToast(
                        "Download options are not connected in this static preview.",
                        AppShellToastTone.Information);
                    break;

                default:
                    SetPreviewScreen(AppShellContentPreviewScreen.Missions);
                    break;
            }
        }

        private void OnLockedSecondaryActionRequested()
        {
            Debug.Log(
                "[AppShellContentPreview] Locked Mission secondary — showing Missions.");

            SetPreviewScreen(AppShellContentPreviewScreen.Missions);
        }

        private IAppScreenView CreateProfileView(VisualElement contentRoot)
        {
            var profileView = new ProfilePanelView(contentRoot);
            if (!profileView.IsBound)
            {
                Debug.LogWarning(
                    "[AppShellContentPreview] ProfilePanelView failed to bind profile-root.");
                profileView.Dispose();
                return null;
            }

            profileView.BackRequested += OnProfileBackRequested;
            profileView.SettingsRequested += OnProfileSettingsRequested;
            profileView.SignOutRequested += OnProfileSignOutRequested;
            _currentProfileView = profileView;
            return profileView;
        }

        private void OnProfileBackRequested()
        {
            Debug.Log(
                "[AppShellContentPreview] Profile Back — showing Home.");

            SetPreviewScreen(AppShellContentPreviewScreen.Home);
        }

        private void OnProfileSettingsRequested()
        {
            Debug.Log(
                "[AppShellContentPreview] Profile Settings — showing Settings.");

            SetPreviewScreen(AppShellContentPreviewScreen.Settings);
        }

        private void OnProfileSignOutRequested()
        {
            Debug.Log(
                "[AppShellContentPreview] Profile Sign Out — showing ConfirmDialog.");

            ShowPreviewConfirmation(
                PreviewConfirmationAction.SignOut,
                ConfirmDialogPresets.SignOut());
        }

        private IAppScreenView CreateSettingsView(VisualElement contentRoot)
        {
            var settingsView = new SettingsPanelView(contentRoot);
            if (!settingsView.IsBound)
            {
                Debug.LogWarning(
                    "[AppShellContentPreview] SettingsPanelView failed to bind settings-root.");
                settingsView.Dispose();
                return null;
            }

            settingsView.BackRequested += OnSettingsBackRequested;
            settingsView.SaveRequested += OnSettingsSaveRequested;
            settingsView.RestoreDefaultsRequested += OnSettingsRestoreDefaultsRequested;
            settingsView.ResetTutorialRequested += OnSettingsResetTutorialRequested;
            _currentSettingsView = settingsView;
            return settingsView;
        }

        private void OnSettingsBackRequested()
        {
            Debug.Log(
                "[AppShellContentPreview] Settings Back — showing Profile.");

            SetPreviewScreen(AppShellContentPreviewScreen.Profile);
        }

        private void OnSettingsSaveRequested()
        {
            Debug.Log(
                "[AppShellContentPreview] Settings Save requested — preview only.");

            _currentSettingsView?.MarkPreviewSaved();
            _appShell?.ShowToast(
                "Settings saved for this preview.",
                AppShellToastTone.Success);
        }

        private void OnSettingsRestoreDefaultsRequested()
        {
            Debug.Log(
                "[AppShellContentPreview] Restore Defaults — showing ConfirmDialog.");

            ShowPreviewConfirmation(
                PreviewConfirmationAction.RestoreDefaults,
                ConfirmDialogPresets.RestoreDefaults());
        }

        private void OnSettingsResetTutorialRequested()
        {
            Debug.Log(
                "[AppShellContentPreview] Reset Tutorial — showing ConfirmDialog.");

            ShowPreviewConfirmation(
                PreviewConfirmationAction.ResetTutorial,
                ConfirmDialogPresets.ResetTutorial());
        }

        private IAppScreenView CreateProgressView(VisualElement contentRoot)
        {
            var progressView = new ProgressPanelView(contentRoot, _dataStatePanelAsset);
            if (!progressView.IsBound)
            {
                Debug.LogWarning(
                    "[AppShellContentPreview] ProgressPanelView failed to bind progress-root.");
                progressView.Dispose();
                return null;
            }

            progressView.SetSelection(_selectedSubject, _selectedTerm);
            progressView.SetDataState(_progressPreviewState);
            progressView.SubjectSelected += OnProgressSubjectSelected;
            progressView.TermSelected += OnProgressTermSelected;
            progressView.MissionReviewRequested += OnProgressMissionReviewRequested;
            progressView.QuizPortalRequested += OnProgressQuizPortalRequested;
            progressView.RetryRequested += OnProgressRetryRequested;
            _currentProgressView = progressView;
            return progressView;
        }

        private void OnProgressSubjectSelected(NutriMindSubject subject)
        {
            _selectedSubject = subject;
            _selectedTerm = NutriMindTerm.Term1;
            _currentProgressView?.SetSelection(_selectedSubject, _selectedTerm);
            RefreshShellPageContext();
            Debug.Log(
                $"[AppShellContentPreview] Progress subject selected: {GetSubjectLabel(subject)}.");
        }

        private void OnProgressTermSelected(NutriMindTerm term)
        {
            _selectedTerm = term;
            RefreshShellPageContext();
            Debug.Log(
                $"[AppShellContentPreview] Progress term selected: Term {(int)term}.");
        }

        private void OnProgressMissionReviewRequested(ProgressMissionPreviewSelection selection)
        {
            _selectedSubject = selection.Subject;
            _selectedTerm = selection.Term;
            RefreshShellPageContext();

            Debug.Log(
                $"[AppShellContentPreview] Progress review requested: {selection.MissionTitle} " +
                $"(Mission {selection.MissionNumber}).");

            _appShell?.ShowToast(
                $"Review selected for {selection.MissionTitle}. Review routing will be connected later.",
                AppShellToastTone.Information);
        }

        private void OnProgressQuizPortalRequested()
        {
            Debug.Log(
                "[AppShellContentPreview] Progress Quiz Portal requested — preview only.");

            _appShell?.ShowToast(
                "Quiz Portal selected. Quiz routing will be connected in the next screen prompts.",
                AppShellToastTone.Information);
        }

        private void OnProgressRetryRequested()
        {
            Debug.Log(
                "[AppShellContentPreview] Progress retry requested — preview only.");

            _appShell?.ShowToast(
                "Progress refresh requested. Data loading is not connected in this static preview.",
                AppShellToastTone.Information);
        }

        private void BindConfirmDialog()
        {
            if (_confirmDialogView != null)
            {
                return;
            }

            VisualElement modalLayer = _appShell?.GetModalLayer();
            if (_confirmDialogAsset == null || modalLayer == null)
            {
                if (!_warnedMissingConfirmDialogAsset)
                {
                    Debug.LogWarning(
                        "[AppShellContentPreview] ConfirmDialog VisualTreeAsset or modal layer " +
                        "is missing. Assign Assets/NutriMind/App/UI/UXML/Shared/ConfirmDialog.uxml.");
                    _warnedMissingConfirmDialogAsset = true;
                }

                return;
            }

            _confirmDialogInstance = _confirmDialogAsset.CloneTree();
            modalLayer.Add(_confirmDialogInstance);
            _confirmDialogView = new ConfirmDialogView(_confirmDialogInstance);
            if (!_confirmDialogView.IsBound)
            {
                Debug.LogWarning(
                    "[AppShellContentPreview] ConfirmDialogView failed to bind.");
                UnbindConfirmDialog();
                return;
            }

            _confirmDialogView.Confirmed += OnConfirmDialogConfirmed;
            _confirmDialogView.Cancelled += OnConfirmDialogCancelled;
        }

        private void UnbindConfirmDialog()
        {
            if (_confirmDialogView != null)
            {
                _confirmDialogView.Confirmed -= OnConfirmDialogConfirmed;
                _confirmDialogView.Cancelled -= OnConfirmDialogCancelled;
                _confirmDialogView.Dispose();
                _confirmDialogView = null;
            }

            if (_confirmDialogInstance != null)
            {
                _confirmDialogInstance.RemoveFromHierarchy();
                _confirmDialogInstance = null;
            }

            _pendingConfirmation = PreviewConfirmationAction.None;
        }

        private void ShowPreviewConfirmation(
            PreviewConfirmationAction action,
            ConfirmDialogConfiguration configuration)
        {
            if (_confirmDialogView == null || !_confirmDialogView.IsBound)
            {
                Debug.LogWarning(
                    "[AppShellContentPreview] ConfirmDialog is unavailable for this preview action.");
                _appShell?.ShowToast(
                    "Confirmation dialog is not assigned for this preview.",
                    AppShellToastTone.Warning);
                return;
            }

            _pendingConfirmation = action;
            _confirmDialogView.Show(configuration);
        }

        private void OnConfirmDialogConfirmed()
        {
            PreviewConfirmationAction action = _pendingConfirmation;
            _pendingConfirmation = PreviewConfirmationAction.None;

            switch (action)
            {
                case PreviewConfirmationAction.SignOut:
                    Debug.Log(
                        "[AppShellContentPreview] Sign Out confirmed — no auth change.");
                    _appShell?.ShowToast(
                        "Sign Out confirmed. Authentication is not connected in this static preview.",
                        AppShellToastTone.Information);
                    break;

                case PreviewConfirmationAction.RestoreDefaults:
                    _currentSettingsView?.RestorePreviewDefaults();
                    Debug.Log(
                        "[AppShellContentPreview] Defaults restored for preview.");
                    _appShell?.ShowToast(
                        "Default settings restored for this preview.",
                        AppShellToastTone.Success);
                    break;

                case PreviewConfirmationAction.ResetTutorial:
                    Debug.Log(
                        "[AppShellContentPreview] Tutorial reset requested for preview.");
                    _appShell?.ShowToast(
                        "Tutorial reset requested for this preview.",
                        AppShellToastTone.Information);
                    break;
            }
        }

        private void OnConfirmDialogCancelled()
        {
            _pendingConfirmation = PreviewConfirmationAction.None;
        }

        private void ClearCurrentContent()
        {
            if (_currentHomeView != null)
            {
                _currentHomeView.ContinueMissionRequested -=
                    OnHomeContinueMissionRequested;
                _currentHomeView.QuizPortalRequested -=
                    OnHomeQuizPortalRequested;
                _currentHomeView = null;
            }

            if (_currentSubjectSelectionView != null)
            {
                _currentSubjectSelectionView.BackRequested -=
                    OnSubjectBackRequested;
                _currentSubjectSelectionView.SubjectSelected -=
                    OnSubjectSelected;
                _currentSubjectSelectionView.ContinueSubjectRequested -=
                    OnContinueSubjectRequested;
                _currentSubjectSelectionView.UnavailableSubjectRequested -=
                    OnUnavailableSubjectRequested;
                _currentSubjectSelectionView = null;
            }

            if (_currentTermSelectionView != null)
            {
                _currentTermSelectionView.BackRequested -= OnTermBackRequested;
                _currentTermSelectionView.TermSelected -= OnTermSelected;
                _currentTermSelectionView.OpenTermRequested -= OnOpenTermRequested;
                _currentTermSelectionView.UnavailableTermRequested -=
                    OnUnavailableTermRequested;
                _currentTermSelectionView = null;
            }

            if (_currentMissionSelectionView != null)
            {
                _currentMissionSelectionView.BackRequested -= OnMissionBackRequested;
                _currentMissionSelectionView.MissionSelected -= OnMissionSelected;
                _currentMissionSelectionView.StartMissionRequested -=
                    OnStartMissionRequested;
                _currentMissionSelectionView.ContinueMissionRequested -=
                    OnContinueMissionRequested;
                _currentMissionSelectionView.ReviewMissionRequested -=
                    OnReviewMissionRequested;
                _currentMissionSelectionView.LockedMissionRequested -=
                    OnLockedMissionRequested;
                _currentMissionSelectionView = null;
            }

            if (_currentLockedMissionView != null)
            {
                _currentLockedMissionView.BackRequested -= OnLockedBackRequested;
                _currentLockedMissionView.PrimaryActionRequested -=
                    OnLockedPrimaryActionRequested;
                _currentLockedMissionView.SecondaryActionRequested -=
                    OnLockedSecondaryActionRequested;
                _currentLockedMissionView = null;
            }

            if (_currentProfileView != null)
            {
                _currentProfileView.BackRequested -= OnProfileBackRequested;
                _currentProfileView.SettingsRequested -= OnProfileSettingsRequested;
                _currentProfileView.SignOutRequested -= OnProfileSignOutRequested;
                _currentProfileView = null;
            }

            if (_currentSettingsView != null)
            {
                _currentSettingsView.BackRequested -= OnSettingsBackRequested;
                _currentSettingsView.SaveRequested -= OnSettingsSaveRequested;
                _currentSettingsView.RestoreDefaultsRequested -=
                    OnSettingsRestoreDefaultsRequested;
                _currentSettingsView.ResetTutorialRequested -=
                    OnSettingsResetTutorialRequested;
                _currentSettingsView = null;
            }

            if (_currentProgressView != null)
            {
                _currentProgressView.SubjectSelected -= OnProgressSubjectSelected;
                _currentProgressView.TermSelected -= OnProgressTermSelected;
                _currentProgressView.MissionReviewRequested -=
                    OnProgressMissionReviewRequested;
                _currentProgressView.QuizPortalRequested -= OnProgressQuizPortalRequested;
                _currentProgressView.RetryRequested -= OnProgressRetryRequested;
                _currentProgressView = null;
            }

            _currentScreenView?.Dispose();
            _currentScreenView = null;

            if (_currentContentInstance != null)
            {
                _currentContentInstance.RemoveFromHierarchy();
                _currentContentInstance = null;
            }

            _currentContentRoot = null;
            _appliedAsset = null;
        }

        private void ShowFallback(
            string title,
            string message,
            string detail)
        {
            ClearFallback();

            if (_contentRegion == null)
            {
                return;
            }

            if (_dataStatePanelAsset == null)
            {
                if (!_warnedMissingDataStateAsset)
                {
                    Debug.LogWarning(
                        "[AppShellContentPreview] DataStatePanel VisualTreeAsset is not assigned. " +
                        "Assign Assets/NutriMind/App/UI/UXML/Shared/DataStatePanel.uxml " +
                        "to _dataStatePanelAsset to display preview fallback states.");
                    _warnedMissingDataStateAsset = true;
                }

                return;
            }

            _fallbackInstance = _dataStatePanelAsset.CloneTree();
            _fallbackInstance.style.width = Length.Percent(100);
            _fallbackInstance.style.height = Length.Percent(100);
            _fallbackInstance.style.flexGrow = 1;
            _fallbackInstance.style.flexShrink = 1;
            _fallbackInstance.AddToClassList(ContentInstanceClass);
            _contentRegion.Add(_fallbackInstance);

            _fallbackView = new DataStatePanelView(_fallbackInstance);
            if (!_fallbackView.IsBound)
            {
                Debug.LogWarning(
                    "[AppShellContentPreview] Failed to bind DataStatePanelView from the assigned fallback asset.");
                ClearFallback();
                return;
            }

            _fallbackView.SetState(DataStatePanelState.Empty);
            _fallbackView.Configure(
                title: title,
                message: message,
                detail: detail,
                iconClass: "ds-icon--info",
                primaryActionLabel: string.Empty,
                secondaryActionLabel: string.Empty);
        }

        private void ClearFallback()
        {
            _fallbackView?.Dispose();
            _fallbackView = null;

            if (_fallbackInstance != null)
            {
                _fallbackInstance.RemoveFromHierarchy();
                _fallbackInstance = null;
            }
        }

        private void OnPreviewRouteRequested(AppShellPreviewRoute route)
        {
            if (!_respondToShellNavigation)
            {
                return;
            }

            AppShellContentPreviewEntry entry = FindEntryForNavigation(route);
            if (entry != null)
            {
                SetPreviewScreen(entry.Screen);
                return;
            }

            ClearCurrentContent();
            ShowFallback(
                $"{GetRouteDisplayName(route)} is not migrated yet",
                "This AppShell navigation destination does not have a content-only preview assigned.",
                "Complete its migration or screen-creation prompt before adding it here.");
            HoldRequestFallback();
            Debug.Log(
                $"[AppShellContentPreview] {GetRouteDisplayName(route)} has no matching " +
                "content-only preview entry (preview only).");
        }

        private void OnProfileRequested()
        {
            if (!_respondToProfileRequest)
            {
                return;
            }

            AppShellContentPreviewEntry entry =
                FindEntry(AppShellContentPreviewScreen.Profile);
            if (entry != null)
            {
                SetPreviewScreen(entry.Screen);
                return;
            }

            ClearCurrentContent();
            ShowFallback(
                "Profile is not migrated yet",
                "Assign the content-only Profile panel after its migration prompt is complete.",
                "The existing standalone Profile panel remains unchanged.");
            HoldRequestFallback();
        }

        private void OnNotificationsRequested()
        {
            Debug.Log(
                "[AppShellContentPreview] Notifications requested — " +
                "no Announcements screen is assigned in this milestone preview.");
        }

        private void HoldRequestFallback()
        {
            _requestFallbackActive = true;
            _requestFallbackSelection = _previewScreen;
            ResetAppliedTracking();
        }

        private void SetAppliedTracking(
            AppShellContentPreviewScreen screen,
            VisualTreeAsset asset,
            string title,
            string context,
            AppShellPreviewRoute? navigation)
        {
            _appliedScreen = screen;
            _appliedAsset = asset;
            _appliedTitle = title;
            _appliedContext = context;
            _appliedNavigation = navigation;
        }

        private void ResetAppliedTracking()
        {
            _appliedScreen = null;
            _appliedAsset = null;
            _appliedTitle = null;
            _appliedContext = null;
            _appliedNavigation = null;
        }

        private static string ResolvePageTitle(
            string configuredTitle,
            AppShellContentPreviewScreen screen)
        {
            return string.IsNullOrWhiteSpace(configuredTitle)
                ? GetScreenDisplayName(screen)
                : configuredTitle.Trim();
        }

        private static string NormalizeOptionalText(string text)
        {
            return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
        }

        private static string GetSubjectLabel(NutriMindSubject subject)
        {
            switch (subject)
            {
                case NutriMindSubject.LiteraQuest:
                    return "LiteraQuest";

                case NutriMindSubject.PeAndHealth:
                    return "PE & Health";

                case NutriMindSubject.Science:
                    return "Science";

                default:
                    return subject.ToString();
            }
        }

        private static string GetScreenDisplayName(
            AppShellContentPreviewScreen screen)
        {
            switch (screen)
            {
                case AppShellContentPreviewScreen.Home:
                    return "Home";
                case AppShellContentPreviewScreen.Subjects:
                    return "Subjects";
                case AppShellContentPreviewScreen.Terms:
                    return "Terms";
                case AppShellContentPreviewScreen.Missions:
                    return "Missions";
                case AppShellContentPreviewScreen.LockedMission:
                    return "Mission Availability";
                case AppShellContentPreviewScreen.Profile:
                    return "Profile";
                case AppShellContentPreviewScreen.Settings:
                    return "Settings";
                case AppShellContentPreviewScreen.Progress:
                    return "Progress";
                case AppShellContentPreviewScreen.QuizList:
                    return "Quiz Portal";
                case AppShellContentPreviewScreen.QuizDetail:
                    return "Quiz Details";
                case AppShellContentPreviewScreen.QuizAttempt:
                    return "Quiz";
                case AppShellContentPreviewScreen.QuizResult:
                    return "Quiz Result";
                default:
                    return "AppShell content preview";
            }
        }

        private static string GetRouteDisplayName(AppShellPreviewRoute route)
        {
            switch (route)
            {
                case AppShellPreviewRoute.Home:
                    return "Home";
                case AppShellPreviewRoute.Subjects:
                    return "Subjects";
                case AppShellPreviewRoute.Missions:
                    return "Missions";
                case AppShellPreviewRoute.Progress:
                    return "Progress";
                case AppShellPreviewRoute.Rewards:
                    return "Rewards";
                case AppShellPreviewRoute.More:
                    return "More";
                default:
                    return route.ToString();
            }
        }

        private void ValidatePreviewEntries()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (_previewEntries == null)
            {
                return;
            }

            var screens = new HashSet<AppShellContentPreviewScreen>();
            var navigationRoutes = new HashSet<AppShellPreviewRoute>();

            for (int i = 0; i < _previewEntries.Count; i++)
            {
                AppShellContentPreviewEntry entry = _previewEntries[i];
                if (entry == null)
                {
                    continue;
                }

                if (entry.Screen == AppShellContentPreviewScreen.None)
                {
                    Debug.LogWarning(
                        $"[AppShellContentPreview] Preview entry {i} uses None. " +
                        "None is reserved for the unselected fallback.");
                }
                else if (!screens.Add(entry.Screen))
                {
                    Debug.LogWarning(
                        $"[AppShellContentPreview] Multiple preview entries use {entry.Screen}. " +
                        "The first entry will be used.");
                }

                if (entry.SelectFromBottomNavigation
                    && !navigationRoutes.Add(entry.ActiveNavigation))
                {
                    Debug.LogWarning(
                        $"[AppShellContentPreview] Multiple bottom-navigation preview entries use " +
                        $"{entry.ActiveNavigation}. The first entry will be used.");
                }

                if (entry.ContentAsset == null)
                {
                    Debug.LogWarning(
                        $"[AppShellContentPreview] Preview entry {i} ({entry.Screen}) has no " +
                        "content-only VisualTreeAsset assigned.");
                }
            }
#endif
        }
    }
}
