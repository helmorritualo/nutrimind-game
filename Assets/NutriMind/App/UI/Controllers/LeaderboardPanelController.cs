using UnityEngine;
using UnityEngine.UIElements;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace NutriMind.App.UI
{
    /// <summary>
    /// Standalone <c>UIDocument</c> preview adapter for <see cref="LeaderboardPanelView"/>.
    /// Presentation only — applies inspector preview state and logs requests.
    /// Does not call APIs, cache standings, or persist ranking data.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class LeaderboardPanelController : MonoBehaviour
    {
        [SerializeField]
        private VisualTreeAsset _dataStatePanelAsset;

        [SerializeField]
        private LeaderboardPreviewState _previewState =
            LeaderboardPreviewState.Content;

        private UIDocument _uiDocument;
        private LeaderboardPanelView _view;
        private bool _eventsRegistered;
        private LeaderboardPreviewState? _appliedState;

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
            VisualElement componentRoot = panelRoot?.Q<VisualElement>("leaderboard-root");
            if (componentRoot == null)
            {
                Invoke(nameof(BindWhenReady), 0.05f);
                return;
            }

            panelRoot.style.flexGrow = 1;
            panelRoot.style.width = Length.Percent(100);
            panelRoot.style.height = Length.Percent(100);

            UnbindView();
            _view = new LeaderboardPanelView(componentRoot, _dataStatePanelAsset);
            if (!_view.IsBound)
            {
                Debug.LogWarning(
                    "[LeaderboardPanelController] LeaderboardPanelView failed to bind leaderboard-root.");
                _view.Dispose();
                _view = null;
                return;
            }

            RegisterEvents();
            LeaderboardPreviewData preview = LeaderboardPreviewCatalog.CreateCanonicalPreview();
            _view.SetData(preview);
            if (_view.LoadedEntryCount == 0)
            {
                _view.SetPreviewState(LeaderboardPreviewState.Empty);
                _appliedState = LeaderboardPreviewState.Empty;
            }
            else
            {
                ApplyPreviewValues(force: true);
            }
        }

        private void Unbind()
        {
            UnbindView();
            _uiDocument = null;
            _appliedState = null;
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

            _view.BackToProgressRequested += OnBackToProgressRequested;
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

            _view.BackToProgressRequested -= OnBackToProgressRequested;
            _view.RetryRequested -= OnRetryRequested;
            _eventsRegistered = false;
        }

        private void ApplyPreviewValues(bool force)
        {
            if (_view == null || !_view.IsBound)
            {
                return;
            }

            bool stateChanged = force || _appliedState != _previewState;
            if (!stateChanged)
            {
                return;
            }

            if (_view.LoadedEntryCount == 0
                && _previewState == LeaderboardPreviewState.Content)
            {
                _view.SetPreviewState(LeaderboardPreviewState.Empty);
                _appliedState = LeaderboardPreviewState.Empty;
                return;
            }

            _view.SetPreviewState(_previewState);
            _appliedState = _previewState;
        }

        private void OnBackToProgressRequested() =>
            Debug.Log("[LeaderboardPanelController] Back to Progress requested — preview only.");

        private void OnRetryRequested() =>
            Debug.Log("[LeaderboardPanelController] Retry requested — preview only.");

#if UNITY_EDITOR
        [ContextMenu("Cycle Leaderboard State")]
        private void CycleLeaderboardState()
        {
            _previewState = (LeaderboardPreviewState)(
                ((int)_previewState + 1) % System.Enum.GetValues(typeof(LeaderboardPreviewState)).Length);
            ApplyPreviewValues(force: true);
            EditorUtility.SetDirty(this);
        }
#endif
    }
}
