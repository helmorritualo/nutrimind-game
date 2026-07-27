using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace NutriMind.App.UI
{
    /// <summary>
    /// Presentation-only Certificates route states for static preview.
    /// </summary>
    public enum CertificatesPreviewState
    {
        Content = 0,
        Loading = 1,
        Empty = 2,
        OfflineCached = 3,
        RecoverableError = 4,
        OfflineUnavailable = 5
    }

    /// <summary>
    /// Presentation-only certificate availability for the Certificates static preview.
    /// Not a Student API enum. Do not infer that the backend will use these exact values.
    /// </summary>
    public enum CertificatePreviewAvailability
    {
        Issued = 0,
        InProgress = 1,
        Locked = 2
    }

    /// <summary>
    /// Static UI preview only.
    /// Not the Student API certificate DTO.
    /// Replace when the certificate item contract is defined.
    /// PresentationId is not a canonical certificateId.
    /// </summary>
    public sealed class CertificatePreviewItem
    {
        public CertificatePreviewItem(
            string presentationId,
            string title,
            string typeLabel,
            CertificatePreviewAvailability availability,
            string issueDateText,
            string eligibilityDescription,
            string recognitionText,
            string lockedReason,
            string documentHeading,
            string awardedToText,
            string iconClass)
        {
            PresentationId = presentationId ?? string.Empty;
            Title = title ?? string.Empty;
            TypeLabel = typeLabel ?? string.Empty;
            Availability = availability;
            IssueDateText = issueDateText ?? string.Empty;
            EligibilityDescription = eligibilityDescription ?? string.Empty;
            RecognitionText = recognitionText ?? string.Empty;
            LockedReason = lockedReason ?? string.Empty;
            DocumentHeading = documentHeading ?? string.Empty;
            AwardedToText = awardedToText ?? string.Empty;
            IconClass = iconClass ?? string.Empty;
        }

        /// <summary>
        /// Presentation-only id. Not a canonical certificateId.
        /// </summary>
        public string PresentationId { get; }

        public string Title { get; }
        public string TypeLabel { get; }
        public CertificatePreviewAvailability Availability { get; }

        /// <summary>
        /// Deterministic UI-preview issue date text when Issued; otherwise empty.
        /// Not sourced from a certificate fixture.
        /// </summary>
        public string IssueDateText { get; }

        public string EligibilityDescription { get; }
        public string RecognitionText { get; }
        public string LockedReason { get; }
        public string DocumentHeading { get; }
        public string AwardedToText { get; }
        public string IconClass { get; }
    }

    /// <summary>
    /// Static UI preview only.
    /// Not the Student API certificate DTO.
    /// PresentationId is not a canonical certificateId.
    /// </summary>
    public readonly struct CertificatePreviewSelection
    {
        public CertificatePreviewSelection(string presentationId, string title)
        {
            PresentationId = presentationId ?? string.Empty;
            Title = title ?? string.Empty;
        }

        /// <summary>
        /// Presentation-only id. Not a canonical certificateId.
        /// </summary>
        public string PresentationId { get; }

        public string Title { get; }
    }

    /// <summary>
    /// Deterministic static-preview catalog for Certificates.
    /// Static UI preview examples only. Not the Student API certificate DTO.
    /// Replace when the certificate item contract is defined.
    /// </summary>
    public static class CertificatesPreviewCatalog
    {
        /// <summary>
        /// Builds exactly three presentation-only certificate examples.
        /// </summary>
        public static IReadOnlyList<CertificatePreviewItem> CreateItems()
        {
            return new CertificatePreviewItem[]
            {
                new(
                    "preview_pages_of_nation_term1",
                    "Pages of the Nation — Term 1",
                    "LiteraQuest Certificate",
                    CertificatePreviewAvailability.Issued,
                    "July 19, 2026",
                    "Completed the required LiteraQuest Term 1 learning path.",
                    "Recognizes completion of the Pages of the Nation term journey.",
                    string.Empty,
                    "Certificate of Completion",
                    "Grade 5 Learner",
                    "ds-icon--medal"),
                new(
                    "preview_literaquest_term2",
                    "LiteraQuest — Term 2",
                    "Certificate in progress",
                    CertificatePreviewAvailability.InProgress,
                    string.Empty,
                    "Complete the required LiteraQuest Term 2 learning path.",
                    "Your certificate will appear after the required learning path is complete and the server reports it as issued.",
                    string.Empty,
                    "Eligibility in progress",
                    "Grade 5 Learner",
                    "ds-icon--book"),
                new(
                    "preview_grade5_nutrimind_completion",
                    "Grade 5 NutriMind Completion",
                    "Year-level Certificate",
                    CertificatePreviewAvailability.Locked,
                    string.Empty,
                    "Complete the school-defined Grade 5 requirements across LiteraQuest, PE & Health, and Science.",
                    "This certificate represents completion of the school-defined Grade 5 NutriMind program.",
                    "Eligibility requirements are not yet complete.",
                    "Certificate locked",
                    "Grade 5 Learner",
                    "ds-icon--trophy")
            };
        }
    }

    /// <summary>
    /// Presentation-only Certificates route view. Binds local preview fixtures and
    /// raises user intent for the host to handle.
    /// Does not call APIs, create files, download certificates, or persist.
    /// </summary>
    public sealed class CertificatesPanelView : IAppScreenView
    {
        private const string RootName = "certificates-root";
        private const string CompactClass = "certificates-panel--compact";
        private const string NarrowClass = "certificates-panel--narrow";
        private const string MobileClass = "mobile";
        private const string DataStateHostVisibleClass = "certificates-panel__data-state-host--visible";
        private const string ContentShellHiddenClass = "certificates-panel__content-shell--hidden";
        private const string OfflineNoticeHiddenClass = "certificates-panel__offline-notice--hidden";
        private const string DownloadHiddenClass = "certificates-panel__download--hidden";
        private const string DownloadHelperHiddenClass = "certificates-panel__download-helper--hidden";
        private const string DocumentDateHiddenClass = "certificates-panel__document-date--hidden";
        private const string ListItemSelectedClass = "certificates-panel__list-item--selected";
        private const string CompactScreenClass = "app-screen-content--compact";
        private const string NarrowScreenClass = "app-screen-content--narrow";
        private const float CompactBreakpoint = 1100f;
        private const float NarrowBreakpoint = 820f;

        private sealed class CertificateListBinding
        {
            public Button Button { get; set; }
            public EventCallback<ClickEvent> Callback { get; set; }
            public CertificatePreviewItem Item { get; set; }
        }

        private VisualElement _root;
        private VisualElement _contentShell;
        private ScrollView _scroll;
        private VisualElement _body;
        private VisualElement _offlineNotice;
        private Label _issuedValue;
        private Label _progressValue;
        private Label _lockedValue;
        private Label _listCount;
        private VisualElement _list;
        private VisualElement _detailSection;
        private VisualElement _documentIcon;
        private Label _documentHeading;
        private Label _documentTitle;
        private Label _documentAwardedTo;
        private Label _documentRecognition;
        private Label _documentDate;
        private Label _typeValue;
        private Label _statusValue;
        private VisualElement _statusIcon;
        private Label _issueValue;
        private Label _eligibilityDescription;
        private Label _eligibilitySupport;
        private VisualElement _eligibilityIcon;
        private Button _downloadButton;
        private Label _downloadHelper;
        private VisualElement _dataStateHost;

        private TemplateContainer _ownedDataStateInstance;
        private DataStatePanelView _dataStateView;
        private bool _warnedMissingDataStateAsset;
        private bool _warnedInvalidItems;

        private readonly List<CertificatePreviewItem> _loadedItems = new();
        private readonly List<CertificateListBinding> _listBindings = new();

        private EventCallback<ClickEvent> _downloadClicked;
        private EventCallback<GeometryChangedEvent> _geometryChanged;
        private bool _disposed;
        private float _lastWidth = -1f;
        private string _selectedPresentationId = string.Empty;

        public CertificatesPanelView(VisualElement root, VisualTreeAsset dataStatePanelAsset)
        {
            ResolveRoot(root);
            if (_root == null)
            {
                return;
            }

            CacheElements();
            BindDataStatePanel(dataStatePanelAsset);
            RegisterCallbacks();
            SetPreviewState(CertificatesPreviewState.Content);
            ApplyResponsiveClasses(_root.resolvedStyle.width);
        }

        public VisualElement Root => _root;
        public bool IsBound => _root != null && !_disposed;
        public CertificatesPreviewState PreviewState { get; private set; }
        public string SelectedPresentationId => _selectedPresentationId;
        public int LoadedCertificateCount => _loadedItems.Count;

        public CertificatePreviewItem SelectedItem
        {
            get
            {
                for (int i = 0; i < _loadedItems.Count; i++)
                {
                    if (string.Equals(
                            _loadedItems[i].PresentationId,
                            _selectedPresentationId,
                            StringComparison.Ordinal))
                    {
                        return _loadedItems[i];
                    }
                }

                return null;
            }
        }

        public event Action BackToRewardsRequested;
        public event Action<CertificatePreviewSelection> SelectionChanged;
        public event Action<CertificatePreviewSelection> DownloadRequested;
        public event Action RetryRequested;

        public void SetItems(IReadOnlyList<CertificatePreviewItem> items)
        {
            if (!IsBound)
            {
                return;
            }

            _warnedInvalidItems = false;
            string previousSelection = _selectedPresentationId;
            _loadedItems.Clear();

            if (items != null)
            {
                var seenIds = new HashSet<string>(StringComparer.Ordinal);
                for (int i = 0; i < items.Count; i++)
                {
                    CertificatePreviewItem item = items[i];
                    if (!TryValidateItem(item, seenIds, out string warning))
                    {
                        if (!_warnedInvalidItems)
                        {
                            Debug.LogWarning(
                                $"[CertificatesPanelView] Skipping invalid certificate item(s). {warning}");
                            _warnedInvalidItems = true;
                        }

                        continue;
                    }

                    seenIds.Add(item.PresentationId);
                    _loadedItems.Add(item);
                }
            }

            RefreshOverviewMetrics();
            RebuildList();

            if (_loadedItems.Count == 0)
            {
                _selectedPresentationId = string.Empty;
                ApplyEmptyDetailPlaceholder();
                return;
            }

            CertificatePreviewItem preserved = FindByPresentationId(previousSelection);
            if (preserved != null)
            {
                _selectedPresentationId = preserved.PresentationId;
            }
            else
            {
                _selectedPresentationId = _loadedItems[0].PresentationId;
            }

            UpdateListSelectionClasses();
            ApplyDetail(SelectedItem);
        }

        public void SelectByPresentationId(string presentationId)
        {
            if (!IsBound || string.IsNullOrWhiteSpace(presentationId))
            {
                return;
            }

            CertificatePreviewItem item = FindByPresentationId(presentationId);
            if (item == null)
            {
                return;
            }

            _selectedPresentationId = item.PresentationId;
            UpdateListSelectionClasses();
            ApplyDetail(item);
        }

        public void ResetSelection()
        {
            if (!IsBound || _loadedItems.Count == 0)
            {
                _selectedPresentationId = string.Empty;
                return;
            }

            _selectedPresentationId = _loadedItems[0].PresentationId;
            UpdateListSelectionClasses();
            ApplyDetail(_loadedItems[0]);
        }

        public void SetPreviewState(CertificatesPreviewState state)
        {
            if (!IsBound)
            {
                return;
            }

            if (state != CertificatesPreviewState.Content
                && state != CertificatesPreviewState.OfflineCached
                && (_dataStateView == null || !_dataStateView.IsBound))
            {
                return;
            }

            PreviewState = state;

            switch (state)
            {
                case CertificatesPreviewState.Content:
                    ShowContent(showOfflineNotice: false);
                    _dataStateView?.SetState(DataStatePanelState.Content);
                    ApplyDetail(SelectedItem);
                    break;

                case CertificatesPreviewState.OfflineCached:
                    ShowContent(showOfflineNotice: true);
                    _dataStateView?.SetState(DataStatePanelState.Content);
                    ApplyDetail(SelectedItem);
                    break;

                case CertificatesPreviewState.Loading:
                    HideContent();
                    _dataStateView.SetState(DataStatePanelState.Loading);
                    ApplyRouteDataStateCopy(state);
                    break;

                case CertificatesPreviewState.Empty:
                    HideContent();
                    _dataStateView.SetState(DataStatePanelState.Empty);
                    ApplyRouteDataStateCopy(state);
                    break;

                case CertificatesPreviewState.RecoverableError:
                    HideContent();
                    _dataStateView.SetState(DataStatePanelState.RecoverableError);
                    ApplyRouteDataStateCopy(state);
                    break;

                case CertificatesPreviewState.OfflineUnavailable:
                    HideContent();
                    _dataStateView.SetState(DataStatePanelState.OfflineUnavailable);
                    ApplyRouteDataStateCopy(state);
                    break;
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            UnregisterCallbacks();
            ClearListBindings();
            DisposeOwnedDataState();

            BackToRewardsRequested = null;
            SelectionChanged = null;
            DownloadRequested = null;
            RetryRequested = null;

            _root = null;
            _contentShell = null;
            _scroll = null;
            _body = null;
            _offlineNotice = null;
            _issuedValue = null;
            _progressValue = null;
            _lockedValue = null;
            _listCount = null;
            _list = null;
            _detailSection = null;
            _documentIcon = null;
            _documentHeading = null;
            _documentTitle = null;
            _documentAwardedTo = null;
            _documentRecognition = null;
            _documentDate = null;
            _typeValue = null;
            _statusValue = null;
            _statusIcon = null;
            _issueValue = null;
            _eligibilityDescription = null;
            _eligibilitySupport = null;
            _eligibilityIcon = null;
            _downloadButton = null;
            _downloadHelper = null;
            _dataStateHost = null;
            _lastWidth = -1f;
            _selectedPresentationId = string.Empty;
            _loadedItems.Clear();
        }

        private void ResolveRoot(VisualElement root)
        {
            if (root == null)
            {
                return;
            }

            _root = root.name == RootName ? root : root.Q<VisualElement>(RootName);
        }

        private void CacheElements()
        {
            _contentShell = _root.Q<VisualElement>("certificates-content-shell");
            _scroll = _root.Q<ScrollView>("certificates-scroll");
            _body = _root.Q<VisualElement>("certificates-body");
            _offlineNotice = _root.Q<VisualElement>("certificates-offline-notice");
            _issuedValue = _root.Q<Label>("certificates-issued-value");
            _progressValue = _root.Q<Label>("certificates-progress-value");
            _lockedValue = _root.Q<Label>("certificates-locked-value");
            _listCount = _root.Q<Label>("certificates-list-count");
            _list = _root.Q<VisualElement>("certificates-list");
            _detailSection = _root.Q<VisualElement>("certificates-detail-section");
            _documentIcon = _root.Q<VisualElement>("certificates-document-icon");
            _documentHeading = _root.Q<Label>("certificates-document-heading");
            _documentTitle = _root.Q<Label>("certificates-document-title");
            _documentAwardedTo = _root.Q<Label>("certificates-document-awarded-to");
            _documentRecognition = _root.Q<Label>("certificates-document-recognition");
            _documentDate = _root.Q<Label>("certificates-document-date");
            _typeValue = _root.Q<Label>("certificates-type-value");
            _statusValue = _root.Q<Label>("certificates-status-value");
            _statusIcon = _root.Q<VisualElement>("certificates-status-icon");
            _issueValue = _root.Q<Label>("certificates-issue-value");
            _eligibilityDescription = _root.Q<Label>("certificates-eligibility-description");
            _eligibilitySupport = _root.Q<Label>("certificates-eligibility-support");
            _eligibilityIcon = _root.Q<VisualElement>("certificates-eligibility-icon");
            _downloadButton = _root.Q<Button>("certificates-download-button");
            _downloadHelper = _root.Q<Label>("certificates-download-helper");
            _dataStateHost = _root.Q<VisualElement>("certificates-data-state-host");
        }

        private void BindDataStatePanel(VisualTreeAsset dataStatePanelAsset)
        {
            if (_dataStateHost == null)
            {
                return;
            }

            if (dataStatePanelAsset == null)
            {
                if (!_warnedMissingDataStateAsset)
                {
                    Debug.LogWarning(
                        "[CertificatesPanelView] DataStatePanel VisualTreeAsset is missing. " +
                        "Content and OfflineCached remain usable; non-content states are no-ops.");
                    _warnedMissingDataStateAsset = true;
                }

                return;
            }

            _ownedDataStateInstance = dataStatePanelAsset.CloneTree();
            _ownedDataStateInstance.style.flexGrow = 1;
            _ownedDataStateInstance.style.width = Length.Percent(100);
            _ownedDataStateInstance.style.height = Length.Percent(100);
            _dataStateHost.Add(_ownedDataStateInstance);

            _dataStateView = new DataStatePanelView(_ownedDataStateInstance);
            if (!_dataStateView.IsBound)
            {
                Debug.LogWarning(
                    "[CertificatesPanelView] Failed to bind DataStatePanelView from the assigned asset.");
                DisposeOwnedDataState();
                return;
            }

            _dataStateView.PrimaryActionRequested += OnDataStatePrimaryAction;
            _dataStateView.SecondaryActionRequested += OnDataStateSecondaryAction;
            _dataStateView.SetVisible(false);
        }

        private void RegisterCallbacks()
        {
            _downloadClicked = _ => OnDownloadClicked();
            _geometryChanged = OnGeometryChanged;

            _downloadButton?.RegisterCallback(_downloadClicked);
            _root?.RegisterCallback(_geometryChanged);
        }

        private void UnregisterCallbacks()
        {
            if (_downloadButton != null && _downloadClicked != null)
            {
                _downloadButton.UnregisterCallback(_downloadClicked);
            }

            if (_root != null && _geometryChanged != null)
            {
                _root.UnregisterCallback(_geometryChanged);
            }

            _downloadClicked = null;
            _geometryChanged = null;
        }

        private void DisposeOwnedDataState()
        {
            if (_dataStateView != null)
            {
                _dataStateView.PrimaryActionRequested -= OnDataStatePrimaryAction;
                _dataStateView.SecondaryActionRequested -= OnDataStateSecondaryAction;
                _dataStateView.Dispose();
                _dataStateView = null;
            }

            if (_ownedDataStateInstance != null)
            {
                _ownedDataStateInstance.RemoveFromHierarchy();
                _ownedDataStateInstance = null;
            }
        }

        private void RefreshOverviewMetrics()
        {
            int issued = 0;
            int inProgress = 0;
            int locked = 0;

            for (int i = 0; i < _loadedItems.Count; i++)
            {
                switch (_loadedItems[i].Availability)
                {
                    case CertificatePreviewAvailability.Issued:
                        issued++;
                        break;
                    case CertificatePreviewAvailability.InProgress:
                        inProgress++;
                        break;
                    case CertificatePreviewAvailability.Locked:
                        locked++;
                        break;
                }
            }

            if (_issuedValue != null) _issuedValue.text = issued.ToString();
            if (_progressValue != null) _progressValue.text = inProgress.ToString();
            if (_lockedValue != null) _lockedValue.text = locked.ToString();

            if (_listCount != null)
            {
                _listCount.text = _loadedItems.Count == 1
                    ? "1 certificate"
                    : $"{_loadedItems.Count} certificates";
            }
        }

        private void RebuildList()
        {
            ClearListBindings();
            if (_list == null)
            {
                return;
            }

            bool compact = _root != null && _root.ClassListContains(CompactClass)
                && !_root.ClassListContains(NarrowClass);

            for (int i = 0; i < _loadedItems.Count; i++)
            {
                CertificatePreviewItem item = _loadedItems[i];
                bool last = i == _loadedItems.Count - 1;
                bool lastInRow = compact && (i % 2 == 1 || last);
                _list.Add(CreateListItem(item, last, lastInRow));
            }
        }

        private VisualElement CreateListItem(
            CertificatePreviewItem item,
            bool last,
            bool lastInRow)
        {
            var button = new Button();
            button.AddToClassList("certificates-panel__list-item");
            button.AddToClassList(GetListItemAvailabilityClass(item.Availability));
            if (last)
            {
                button.AddToClassList("certificates-panel__list-item--last");
            }

            if (lastInRow)
            {
                button.AddToClassList("certificates-panel__list-item--last-in-row");
            }

            button.tooltip = item.Title;
            button.focusable = true;

            var icon = new VisualElement();
            icon.AddToClassList("ds-icon");
            icon.AddToClassList(item.IconClass);
            icon.AddToClassList("certificates-panel__list-item-icon");
            icon.pickingMode = PickingMode.Ignore;

            var copy = new VisualElement();
            copy.AddToClassList("certificates-panel__list-item-copy");
            copy.pickingMode = PickingMode.Ignore;

            var chip = new VisualElement();
            chip.AddToClassList("ds-chip");
            chip.AddToClassList("certificates-panel__list-item-chip");
            chip.AddToClassList(GetListChipClass(item.Availability));
            chip.pickingMode = PickingMode.Ignore;

            var chipIcon = new VisualElement();
            chipIcon.AddToClassList("ds-icon");
            chipIcon.AddToClassList(GetStatusIconClass(item.Availability));
            chipIcon.AddToClassList("certificates-panel__list-item-chip-icon");
            chipIcon.pickingMode = PickingMode.Ignore;

            var chipLabel = new Label(GetAvailabilityLabel(item.Availability));
            chipLabel.AddToClassList("certificates-panel__list-item-chip-label");
            chipLabel.pickingMode = PickingMode.Ignore;

            chip.Add(chipIcon);
            chip.Add(chipLabel);

            var title = new Label(item.Title);
            title.AddToClassList("certificates-panel__list-item-title");
            title.pickingMode = PickingMode.Ignore;

            var type = new Label(item.TypeLabel);
            type.AddToClassList("certificates-panel__list-item-type");
            type.pickingMode = PickingMode.Ignore;

            var statusLine = new Label(GetListStatusLine(item));
            statusLine.AddToClassList("certificates-panel__list-item-status");
            statusLine.pickingMode = PickingMode.Ignore;

            copy.Add(chip);
            copy.Add(title);
            copy.Add(type);
            copy.Add(statusLine);

            var chevron = new VisualElement();
            chevron.AddToClassList("ds-icon");
            chevron.AddToClassList("ds-icon--chevron-right");
            chevron.AddToClassList("certificates-panel__list-item-chevron");
            chevron.pickingMode = PickingMode.Ignore;

            button.Add(icon);
            button.Add(copy);
            button.Add(chevron);

            var binding = new CertificateListBinding
            {
                Button = button,
                Item = item
            };
            binding.Callback = evt => OnListItemClicked(binding);
            button.RegisterCallback(binding.Callback);
            _listBindings.Add(binding);

            return button;
        }

        private void OnListItemClicked(CertificateListBinding binding)
        {
            if (_disposed || binding?.Item == null)
            {
                return;
            }

            if (string.Equals(
                    binding.Item.PresentationId,
                    _selectedPresentationId,
                    StringComparison.Ordinal))
            {
                return;
            }

            _selectedPresentationId = binding.Item.PresentationId;
            UpdateListSelectionClasses();
            ApplyDetail(binding.Item);
            SelectionChanged?.Invoke(
                new CertificatePreviewSelection(binding.Item.PresentationId, binding.Item.Title));
        }

        private void OnDownloadClicked()
        {
            if (_disposed
                || PreviewState != CertificatesPreviewState.Content
                || SelectedItem == null
                || SelectedItem.Availability != CertificatePreviewAvailability.Issued)
            {
                return;
            }

            DownloadRequested?.Invoke(
                new CertificatePreviewSelection(
                    SelectedItem.PresentationId,
                    SelectedItem.Title));
        }

        private void ClearListBindings()
        {
            for (int i = 0; i < _listBindings.Count; i++)
            {
                CertificateListBinding binding = _listBindings[i];
                if (binding.Button != null && binding.Callback != null)
                {
                    binding.Button.UnregisterCallback(binding.Callback);
                }
            }

            _listBindings.Clear();
            _list?.Clear();
        }

        private void UpdateListSelectionClasses()
        {
            for (int i = 0; i < _listBindings.Count; i++)
            {
                CertificateListBinding binding = _listBindings[i];
                if (binding.Button == null || binding.Item == null)
                {
                    continue;
                }

                bool selected = string.Equals(
                    binding.Item.PresentationId,
                    _selectedPresentationId,
                    StringComparison.Ordinal);
                binding.Button.EnableInClassList(ListItemSelectedClass, selected);
            }
        }

        private void ApplyDetail(CertificatePreviewItem item)
        {
            if (item == null)
            {
                ApplyEmptyDetailPlaceholder();
                return;
            }

            ApplyDetailSectionTone(item.Availability);
            SetDocumentIcon(item.IconClass);

            if (_documentHeading != null) _documentHeading.text = item.DocumentHeading;
            if (_documentTitle != null) _documentTitle.text = item.Title;
            if (_documentAwardedTo != null) _documentAwardedTo.text = item.AwardedToText;
            if (_documentRecognition != null) _documentRecognition.text = item.RecognitionText;

            bool issued = item.Availability == CertificatePreviewAvailability.Issued;
            if (_documentDate != null)
            {
                if (issued)
                {
                    _documentDate.text = $"Issued {item.IssueDateText}";
                    _documentDate.RemoveFromClassList(DocumentDateHiddenClass);
                }
                else
                {
                    _documentDate.text = string.Empty;
                    _documentDate.AddToClassList(DocumentDateHiddenClass);
                }
            }

            if (_typeValue != null) _typeValue.text = item.TypeLabel;
            if (_statusValue != null) _statusValue.text = GetAvailabilityLabel(item.Availability);
            SetStatusIcon(item.Availability);

            if (_issueValue != null)
            {
                _issueValue.text = issued ? item.IssueDateText : "Not issued";
            }

            if (_eligibilityDescription != null)
            {
                _eligibilityDescription.text = item.EligibilityDescription;
            }

            if (_eligibilitySupport != null)
            {
                _eligibilitySupport.text = item.Availability switch
                {
                    CertificatePreviewAvailability.Issued => "Requirements complete",
                    CertificatePreviewAvailability.InProgress => "Requirements in progress",
                    CertificatePreviewAvailability.Locked => item.LockedReason,
                    _ => "—"
                };
            }

            SetEligibilityIcon(item.Availability);
            ApplyDownloadAction(item);
        }

        private void ApplyDetailSectionTone(CertificatePreviewAvailability availability)
        {
            if (_detailSection == null)
            {
                return;
            }

            _detailSection.RemoveFromClassList("certificates-panel__detail-section--issued");
            _detailSection.RemoveFromClassList("certificates-panel__detail-section--progress");
            _detailSection.RemoveFromClassList("certificates-panel__detail-section--locked");

            switch (availability)
            {
                case CertificatePreviewAvailability.Issued:
                    _detailSection.AddToClassList("certificates-panel__detail-section--issued");
                    break;
                case CertificatePreviewAvailability.InProgress:
                    _detailSection.AddToClassList("certificates-panel__detail-section--progress");
                    break;
                case CertificatePreviewAvailability.Locked:
                    _detailSection.AddToClassList("certificates-panel__detail-section--locked");
                    break;
                default:
                    _detailSection.AddToClassList("certificates-panel__detail-section--locked");
                    break;
            }
        }

        private void ApplyEmptyDetailPlaceholder()
        {
            ApplyDetailSectionTone(CertificatePreviewAvailability.Locked);

            if (_documentHeading != null) _documentHeading.text = "No certificate selected";
            if (_documentTitle != null) _documentTitle.text = "Certificate collection is empty";
            if (_documentAwardedTo != null) _documentAwardedTo.text = "—";
            if (_documentRecognition != null)
            {
                _documentRecognition.text =
                    "Issued certificates will appear here when they become available.";
            }

            if (_documentDate != null)
            {
                _documentDate.text = string.Empty;
                _documentDate.AddToClassList(DocumentDateHiddenClass);
            }

            if (_typeValue != null) _typeValue.text = "—";
            if (_statusValue != null) _statusValue.text = "—";
            if (_issueValue != null) _issueValue.text = "Not issued";
            if (_eligibilityDescription != null)
            {
                _eligibilityDescription.text =
                    "You can continue learning and check your eligibility again later.";
            }

            if (_eligibilitySupport != null) _eligibilitySupport.text = "No certificates loaded";

            _downloadButton?.AddToClassList(DownloadHiddenClass);
            if (_downloadHelper != null)
            {
                _downloadHelper.text = string.Empty;
                _downloadHelper.AddToClassList(DownloadHelperHiddenClass);
            }
        }

        private void ApplyDownloadAction(CertificatePreviewItem item)
        {
            if (_downloadButton == null || _downloadHelper == null)
            {
                return;
            }

            bool offline = PreviewState == CertificatesPreviewState.OfflineCached;

            if (item.Availability == CertificatePreviewAvailability.Issued)
            {
                _downloadButton.RemoveFromClassList(DownloadHiddenClass);
                _downloadButton.SetEnabled(!offline);

                if (offline)
                {
                    _downloadHelper.text = "Connect to check download availability.";
                    _downloadHelper.RemoveFromClassList(DownloadHelperHiddenClass);
                }
                else
                {
                    _downloadHelper.text = string.Empty;
                    _downloadHelper.AddToClassList(DownloadHelperHiddenClass);
                }
            }
            else
            {
                _downloadButton.AddToClassList(DownloadHiddenClass);
                _downloadButton.SetEnabled(false);
                _downloadHelper.text =
                    "Download will be available only after the certificate is issued and supported.";
                _downloadHelper.RemoveFromClassList(DownloadHelperHiddenClass);
            }
        }

        private void SetDocumentIcon(string iconClass)
        {
            if (_documentIcon == null)
            {
                return;
            }

            RemoveIconClasses(_documentIcon);
            _documentIcon.AddToClassList("ds-icon");
            _documentIcon.AddToClassList(iconClass);
            _documentIcon.AddToClassList("certificates-panel__document-icon");
        }

        private void SetStatusIcon(CertificatePreviewAvailability availability)
        {
            if (_statusIcon == null)
            {
                return;
            }

            RemoveIconClasses(_statusIcon);
            _statusIcon.AddToClassList("ds-icon");
            _statusIcon.AddToClassList(GetStatusIconClass(availability));
            _statusIcon.AddToClassList("certificates-panel__meta-status-icon");
        }

        private void SetEligibilityIcon(CertificatePreviewAvailability availability)
        {
            if (_eligibilityIcon == null)
            {
                return;
            }

            RemoveIconClasses(_eligibilityIcon);
            _eligibilityIcon.AddToClassList("ds-icon");
            _eligibilityIcon.AddToClassList(GetStatusIconClass(availability));
            _eligibilityIcon.AddToClassList("certificates-panel__eligibility-icon");
        }

        private static void RemoveIconClasses(VisualElement element)
        {
            if (element == null)
            {
                return;
            }

            var toRemove = new List<string>();
            foreach (string className in element.GetClasses())
            {
                if (className.StartsWith("ds-icon", StringComparison.Ordinal))
                {
                    toRemove.Add(className);
                }
            }

            for (int i = 0; i < toRemove.Count; i++)
            {
                element.RemoveFromClassList(toRemove[i]);
            }
        }

        private CertificatePreviewItem FindByPresentationId(string presentationId)
        {
            if (string.IsNullOrWhiteSpace(presentationId))
            {
                return null;
            }

            for (int i = 0; i < _loadedItems.Count; i++)
            {
                if (string.Equals(
                        _loadedItems[i].PresentationId,
                        presentationId,
                        StringComparison.Ordinal))
                {
                    return _loadedItems[i];
                }
            }

            return null;
        }

        private void ShowContent(bool showOfflineNotice)
        {
            _contentShell?.RemoveFromClassList(ContentShellHiddenClass);
            if (_scroll != null)
            {
                _scroll.style.display = DisplayStyle.Flex;
            }

            _offlineNotice?.EnableInClassList(OfflineNoticeHiddenClass, !showOfflineNotice);
            _dataStateHost?.RemoveFromClassList(DataStateHostVisibleClass);
            _dataStateView?.SetVisible(false);
        }

        private void HideContent()
        {
            _contentShell?.AddToClassList(ContentShellHiddenClass);
            if (_scroll != null)
            {
                _scroll.style.display = DisplayStyle.None;
            }

            _offlineNotice?.AddToClassList(OfflineNoticeHiddenClass);
            _dataStateHost?.AddToClassList(DataStateHostVisibleClass);
            _dataStateView?.SetVisible(true);
        }

        private void ApplyRouteDataStateCopy(CertificatesPreviewState state)
        {
            if (_dataStateView == null || !_dataStateView.IsBound)
            {
                return;
            }

            switch (state)
            {
                case CertificatesPreviewState.Loading:
                    _dataStateView.Configure(
                        new DataStatePanelConfiguration(
                            "Loading certificates",
                            "Getting your certificate collection.",
                            "Certificate availability and issue details are provided by the NutriMind server.",
                            null,
                            string.Empty,
                            string.Empty,
                            true));
                    break;

                case CertificatesPreviewState.Empty:
                    _dataStateView.Configure(
                        "No certificates yet",
                        "Issued certificates will appear here when they become available.",
                        "You can continue learning and check your eligibility again later.",
                        "ds-icon--medal",
                        "Back to Rewards",
                        string.Empty);
                    break;

                case CertificatesPreviewState.RecoverableError:
                    _dataStateView.Configure(
                        "Certificates could not be loaded",
                        "Check your connection and try again.",
                        "No certificate information was changed.",
                        "ds-icon--error",
                        "Try Again",
                        "Back to Rewards");
                    break;
            }
        }

        private void OnDataStatePrimaryAction()
        {
            if (PreviewState == CertificatesPreviewState.Empty)
            {
                BackToRewardsRequested?.Invoke();
            }
            else if (PreviewState == CertificatesPreviewState.RecoverableError)
            {
                RetryRequested?.Invoke();
            }
        }

        private void OnDataStateSecondaryAction()
        {
            if (PreviewState == CertificatesPreviewState.RecoverableError)
            {
                BackToRewardsRequested?.Invoke();
            }
        }

        private void OnGeometryChanged(GeometryChangedEvent evt) =>
            ApplyResponsiveClasses(evt.newRect.width);

        private void ApplyResponsiveClasses(float width)
        {
            if (_root == null || float.IsNaN(width) || width <= 0f || Mathf.Approximately(width, _lastWidth))
            {
                return;
            }

            bool wasCompact = _root.ClassListContains(CompactClass)
                && !_root.ClassListContains(NarrowClass);
            _lastWidth = width;
            bool compact = width < CompactBreakpoint;
            bool narrow = width < NarrowBreakpoint;
            _root.EnableInClassList(CompactClass, compact);
            _root.EnableInClassList(NarrowClass, narrow);
            _root.EnableInClassList(MobileClass, narrow);
            _root.EnableInClassList(CompactScreenClass, compact);
            _root.EnableInClassList(NarrowScreenClass, narrow);

            bool isCompact = compact && !narrow;
            if (wasCompact != isCompact && _loadedItems.Count > 0)
            {
                RebuildList();
                UpdateListSelectionClasses();
            }
        }

        private static bool TryValidateItem(
            CertificatePreviewItem item,
            HashSet<string> seenIds,
            out string warning)
        {
            if (item == null)
            {
                warning = "Item was null.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(item.PresentationId)
                || string.IsNullOrWhiteSpace(item.Title)
                || string.IsNullOrWhiteSpace(item.TypeLabel)
                || string.IsNullOrWhiteSpace(item.EligibilityDescription)
                || string.IsNullOrWhiteSpace(item.RecognitionText)
                || string.IsNullOrWhiteSpace(item.DocumentHeading)
                || string.IsNullOrWhiteSpace(item.AwardedToText)
                || string.IsNullOrWhiteSpace(item.IconClass)
                || !item.IconClass.StartsWith("ds-icon--", StringComparison.Ordinal)
                || !Enum.IsDefined(typeof(CertificatePreviewAvailability), item.Availability))
            {
                warning = "Required presentation fields were missing or invalid.";
                return false;
            }

            if (seenIds.Contains(item.PresentationId))
            {
                warning = $"Duplicate PresentationId '{item.PresentationId}'.";
                return false;
            }

            switch (item.Availability)
            {
                case CertificatePreviewAvailability.Issued:
                    if (string.IsNullOrWhiteSpace(item.IssueDateText)
                        || !string.IsNullOrWhiteSpace(item.LockedReason))
                    {
                        warning = "Issued items require issue date and empty locked reason.";
                        return false;
                    }

                    break;

                case CertificatePreviewAvailability.InProgress:
                    if (!string.IsNullOrWhiteSpace(item.IssueDateText)
                        || !string.IsNullOrWhiteSpace(item.LockedReason))
                    {
                        warning = "In-progress items require empty issue date and locked reason.";
                        return false;
                    }

                    break;

                case CertificatePreviewAvailability.Locked:
                    if (!string.IsNullOrWhiteSpace(item.IssueDateText)
                        || string.IsNullOrWhiteSpace(item.LockedReason))
                    {
                        warning = "Locked items require empty issue date and a locked reason.";
                        return false;
                    }

                    break;
            }

            warning = null;
            return true;
        }

        private static string GetAvailabilityLabel(CertificatePreviewAvailability availability) =>
            availability switch
            {
                CertificatePreviewAvailability.Issued => "Issued",
                CertificatePreviewAvailability.InProgress => "In progress",
                CertificatePreviewAvailability.Locked => "Locked",
                _ => "—"
            };

        private static string GetStatusIconClass(CertificatePreviewAvailability availability) =>
            availability switch
            {
                CertificatePreviewAvailability.Issued => "ds-icon--check",
                CertificatePreviewAvailability.InProgress => "ds-icon--info",
                CertificatePreviewAvailability.Locked => "ds-icon--lock",
                _ => "ds-icon--info"
            };

        private static string GetListItemAvailabilityClass(
            CertificatePreviewAvailability availability) =>
            availability switch
            {
                CertificatePreviewAvailability.Issued => "certificates-panel__list-item--issued",
                CertificatePreviewAvailability.InProgress => "certificates-panel__list-item--progress",
                CertificatePreviewAvailability.Locked => "certificates-panel__list-item--locked",
                _ => "certificates-panel__list-item--locked"
            };

        private static string GetListChipClass(CertificatePreviewAvailability availability) =>
            availability switch
            {
                CertificatePreviewAvailability.Issued => "certificates-panel__list-item-chip--issued",
                CertificatePreviewAvailability.InProgress => "certificates-panel__list-item-chip--progress",
                CertificatePreviewAvailability.Locked => "certificates-panel__list-item-chip--locked",
                _ => "certificates-panel__list-item-chip--locked"
            };

        private static string GetListStatusLine(CertificatePreviewItem item) =>
            item.Availability switch
            {
                CertificatePreviewAvailability.Issued => $"Issued {item.IssueDateText}",
                CertificatePreviewAvailability.InProgress => "Eligibility in progress",
                CertificatePreviewAvailability.Locked => "Eligibility locked",
                _ => "—"
            };
    }
}
