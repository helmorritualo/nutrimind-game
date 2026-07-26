using UnityEngine;
using UnityEngine.UIElements;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace NutriMind.App.UI
{
    /// <summary>
    /// Standalone <c>UIDocument</c> preview adapter for <see cref="AnnouncementsPanelView"/>.
    /// Presentation only — applies inspector preview state and logs requests.
    /// Does not call APIs, persist read state, or open a separate detail route.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class AnnouncementsPanelController : MonoBehaviour
    {
        [SerializeField]
        private VisualTreeAsset _dataStatePanelAsset;

        [SerializeField]
        private AnnouncementsPreviewState _previewState =
            AnnouncementsPreviewState.Content;

        [SerializeField]
        private AnnouncementsPreviewFilter _previewFilter =
            AnnouncementsPreviewFilter.All;

        [SerializeField]
        private int _previewAnnouncementIndex;

        private UIDocument _uiDocument;
        private AnnouncementsPanelView _view;
        private readonly System.Collections.Generic.HashSet<string> _readPresentationIds =
            new(System.StringComparer.Ordinal);
        private bool _eventsRegistered;
        private AnnouncementsPreviewState? _appliedState;
        private AnnouncementsPreviewFilter? _appliedFilter;
        private int? _appliedIndex;

        private void OnEnable()
        {
            _uiDocument = GetComponent<UIDocument>();
            BindWhenReady();
        }

        private void OnDisable()
        {
            CancelInvoke(nameof(BindWhenReady));
            Unbind();
        }

        private void OnValidate()
        {
            _previewAnnouncementIndex = Mathf.Clamp(_previewAnnouncementIndex, 0, 2);

            if (!isActiveAndEnabled || _view == null || !_view.IsBound)
            {
                return;
            }

            ApplyPreviewValues(force: true);
        }

        private void Update()
        {
            if (_view == null || !_view.IsBound)
            {
                return;
            }

            ApplyPreviewValues(force: false);
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

            VisualElement panelRoot = _uiDocument.rootVisualElement;
            VisualElement componentRoot = panelRoot?.Q<VisualElement>("announcements-root");
            if (componentRoot == null)
            {
                Invoke(nameof(BindWhenReady), 0.05f);
                return;
            }

            panelRoot.style.flexGrow = 1;
            panelRoot.style.width = Length.Percent(100);
            panelRoot.style.height = Length.Percent(100);

            UnbindView();
            _view = new AnnouncementsPanelView(componentRoot, _dataStatePanelAsset);
            if (!_view.IsBound)
            {
                Debug.LogWarning(
                    "[AnnouncementsPanelController] AnnouncementsPanelView failed to bind announcements-root.");
                _view.Dispose();
                _view = null;
                return;
            }

            RegisterEvents();
            _view.SetItems(AnnouncementsPreviewCatalog.CreateItems());
            _view.SetReadPresentationIds(_readPresentationIds);
            ApplyPreviewValues(force: true);
        }

        private void Unbind()
        {
            UnbindView();
            _uiDocument = null;
            _appliedState = null;
            _appliedFilter = null;
            _appliedIndex = null;
        }

        private void UnbindView()
        {
            if (_view == null)
            {
                return;
            }

            UnregisterEvents();
            _view.Dispose();
            _view = null;
        }

        private void RegisterEvents()
        {
            if (_view == null || _eventsRegistered)
            {
                return;
            }

            _view.BackRequested += OnBackRequested;
            _view.SelectionChanged += OnSelectionChanged;
            _view.ReadStateChanged += OnReadStateChanged;
            _view.FilterChanged += OnFilterChanged;
            _view.RetryRequested += OnRetryRequested;
            _eventsRegistered = true;
        }

        private void UnregisterEvents()
        {
            if (_view == null || !_eventsRegistered)
            {
                _eventsRegistered = false;
                return;
            }

            _view.BackRequested -= OnBackRequested;
            _view.SelectionChanged -= OnSelectionChanged;
            _view.ReadStateChanged -= OnReadStateChanged;
            _view.FilterChanged -= OnFilterChanged;
            _view.RetryRequested -= OnRetryRequested;
            _eventsRegistered = false;
        }

        private void ApplyPreviewValues(bool force)
        {
            if (_view == null || !_view.IsBound)
            {
                return;
            }

            _previewAnnouncementIndex = Mathf.Clamp(_previewAnnouncementIndex, 0, 2);

            bool filterChanged = force || _appliedFilter != _previewFilter;
            if (filterChanged)
            {
                _view.SetFilter(_previewFilter);
                _appliedFilter = _previewFilter;
            }

            bool indexChanged = force || _appliedIndex != _previewAnnouncementIndex;
            if (indexChanged)
            {
                var items = AnnouncementsPreviewCatalog.CreateItems();
                if (_previewAnnouncementIndex >= 0 && _previewAnnouncementIndex < items.Count)
                {
                    _view.SelectByPresentationId(items[_previewAnnouncementIndex].PresentationId);
                }

                _appliedIndex = _previewAnnouncementIndex;
            }

            bool stateChanged = force || _appliedState != _previewState;
            if (stateChanged)
            {
                _view.SetPreviewState(_previewState);
                _appliedState = _previewState;
            }
        }

        private void OnBackRequested() =>
            Debug.Log("[AnnouncementsPanelController] Back requested — preview only.");

        private void OnSelectionChanged(AnnouncementPreviewSelection selection) =>
            Debug.Log(
                $"[AnnouncementsPanelController] Selection changed: id={selection.PresentationId}, " +
                $"title='{selection.Title}'.");

        private void OnReadStateChanged(AnnouncementsPreviewReadState snapshot)
        {
            _readPresentationIds.Clear();
            for (int i = 0; i < snapshot.ReadPresentationIds.Count; i++)
            {
                _readPresentationIds.Add(snapshot.ReadPresentationIds[i]);
            }

            Debug.Log(
                $"[AnnouncementsPanelController] Read state changed: unread={snapshot.UnreadCount}.");
        }

        private void OnFilterChanged(AnnouncementsPreviewFilter filter)
        {
            _previewFilter = filter;
            _appliedFilter = filter;
            Debug.Log($"[AnnouncementsPanelController] Filter changed: {filter}.");
        }

        private void OnRetryRequested() =>
            Debug.Log("[AnnouncementsPanelController] Retry requested — preview only.");

#if UNITY_EDITOR
        [ContextMenu("Cycle Announcements State")]
        private void CycleAnnouncementsState()
        {
            _previewState = (AnnouncementsPreviewState)(
                ((int)_previewState + 1) % System.Enum.GetValues(typeof(AnnouncementsPreviewState)).Length);
            ApplyPreviewValues(force: true);
            EditorUtility.SetDirty(this);
        }

        [ContextMenu("Cycle Announcements Filter")]
        private void CycleAnnouncementsFilter()
        {
            _previewFilter = (AnnouncementsPreviewFilter)(
                ((int)_previewFilter + 1) % System.Enum.GetValues(typeof(AnnouncementsPreviewFilter)).Length);
            ApplyPreviewValues(force: true);
            EditorUtility.SetDirty(this);
        }

        [ContextMenu("Cycle Selected Announcement")]
        private void CycleSelectedAnnouncement()
        {
            _previewAnnouncementIndex = (_previewAnnouncementIndex + 1) % 3;
            ApplyPreviewValues(force: true);
            EditorUtility.SetDirty(this);
        }

        [ContextMenu("Reset Preview Read State")]
        private void ResetPreviewReadState()
        {
            _readPresentationIds.Clear();
            if (_view != null && _view.IsBound)
            {
                _view.SetReadPresentationIds(_readPresentationIds);
            }

            EditorUtility.SetDirty(this);
        }
#endif
    }
}
