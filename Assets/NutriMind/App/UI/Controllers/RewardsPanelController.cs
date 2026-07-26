using UnityEngine;
using UnityEngine.UIElements;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace NutriMind.App.UI
{
    /// <summary>
    /// Standalone <c>UIDocument</c> preview adapter for <see cref="RewardsPanelView"/>.
    /// Presentation only — applies inspector preview state and logs requests.
    /// Does not call APIs, generate request UUIDs, mutate reward state, or persist.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class RewardsPanelController : MonoBehaviour
    {
        [SerializeField]
        private VisualTreeAsset _dataStatePanelAsset;

        [SerializeField]
        private RewardsPreviewState _previewState =
            RewardsPreviewState.Content;

        [SerializeField]
        private RewardsPreviewFilter _previewFilter =
            RewardsPreviewFilter.All;

        private UIDocument _uiDocument;
        private RewardsPanelView _view;
        private bool _eventsRegistered;
        private RewardsPreviewState? _appliedState;
        private RewardsPreviewFilter? _appliedFilter;

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
            VisualElement componentRoot = panelRoot?.Q<VisualElement>("rewards-root");
            if (componentRoot == null)
            {
                Invoke(nameof(BindWhenReady), 0.05f);
                return;
            }

            panelRoot.style.flexGrow = 1;
            panelRoot.style.width = Length.Percent(100);
            panelRoot.style.height = Length.Percent(100);

            UnbindView();
            _view = new RewardsPanelView(componentRoot, _dataStatePanelAsset);
            if (!_view.IsBound)
            {
                Debug.LogWarning(
                    "[RewardsPanelController] RewardsPanelView failed to bind rewards-root.");
                _view.Dispose();
                _view = null;
                return;
            }

            RegisterEvents();
            _view.SetItems(RewardsPreviewCatalog.CreateItems());
            ApplyPreviewValues(force: true);
        }

        private void Unbind()
        {
            UnbindView();
            _uiDocument = null;
            _appliedState = null;
            _appliedFilter = null;
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

            _view.BackToHomeRequested += OnBackToHomeRequested;
            _view.UseRewardRequested += OnUseRewardRequested;
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

            _view.BackToHomeRequested -= OnBackToHomeRequested;
            _view.UseRewardRequested -= OnUseRewardRequested;
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

            bool filterChanged = force || _appliedFilter != _previewFilter;
            if (filterChanged)
            {
                _view.SetFilter(_previewFilter);
                _appliedFilter = _previewFilter;
            }

            bool stateChanged = force || _appliedState != _previewState;
            if (stateChanged)
            {
                _view.SetPreviewState(_previewState);
                _appliedState = _previewState;
            }
        }

        private void OnBackToHomeRequested() =>
            Debug.Log("[RewardsPanelController] Back to Home requested — preview only.");

        private void OnUseRewardRequested(RewardsPreviewSelection selection) =>
            Debug.Log(
                $"[RewardsPanelController] Use Reward requested: key={selection.PresentationKey}, " +
                $"title='{selection.Title}' — preview only. No request UUID generated.");

        private void OnFilterChanged(RewardsPreviewFilter filter) =>
            Debug.Log($"[RewardsPanelController] Filter changed: {filter}.");

        private void OnRetryRequested() =>
            Debug.Log("[RewardsPanelController] Retry requested — preview only.");

#if UNITY_EDITOR
        [ContextMenu("Cycle Rewards State")]
        private void CycleRewardsState()
        {
            _previewState = (RewardsPreviewState)(
                ((int)_previewState + 1) % System.Enum.GetValues(typeof(RewardsPreviewState)).Length);
            ApplyPreviewValues(force: true);
            EditorUtility.SetDirty(this);
        }

        [ContextMenu("Cycle Rewards Filter")]
        private void CycleRewardsFilter()
        {
            _previewFilter = (RewardsPreviewFilter)(
                ((int)_previewFilter + 1) % System.Enum.GetValues(typeof(RewardsPreviewFilter)).Length);
            ApplyPreviewValues(force: true);
            EditorUtility.SetDirty(this);
        }
#endif
    }
}
