using UnityEngine;
using UnityEngine.UIElements;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace NutriMind.App.UI
{
    /// <summary>
    /// Standalone <c>UIDocument</c> preview adapter for <see cref="MissionDetailPanelView"/>.
    /// Presentation only — applies inspector preview state and logs requests.
    /// Does not call APIs, load mission scenes, or mutate progress.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class MissionDetailPanelController : MonoBehaviour
    {
        [SerializeField]
        private VisualTreeAsset _dataStatePanelAsset;

        [SerializeField]
        private MissionDetailPreviewState _previewState =
            MissionDetailPreviewState.Content;

        [SerializeField]
        private int _previewMissionNumber = 2;

        private UIDocument _uiDocument;
        private MissionDetailPanelView _view;
        private bool _eventsRegistered;
        private MissionDetailPreviewState? _appliedState;
        private int? _appliedMissionNumber;

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
            _previewMissionNumber = Mathf.Clamp(_previewMissionNumber, 1, 3);
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
            VisualElement componentRoot = panelRoot?.Q<VisualElement>("mission-detail-root");
            if (componentRoot == null)
            {
                Invoke(nameof(BindWhenReady), 0.05f);
                return;
            }

            panelRoot.style.flexGrow = 1;
            panelRoot.style.width = Length.Percent(100);
            panelRoot.style.height = Length.Percent(100);

            UnbindView();
            _view = new MissionDetailPanelView(componentRoot, _dataStatePanelAsset);
            if (!_view.IsBound)
            {
                Debug.LogWarning(
                    "[MissionDetailPanelController] MissionDetailPanelView failed to bind mission-detail-root.");
                _view.Dispose();
                _view = null;
                return;
            }

            RegisterEvents();
            ApplyPreviewValues(force: true);
        }

        private void Unbind()
        {
            UnbindView();
            _uiDocument = null;
            _appliedState = null;
            _appliedMissionNumber = null;
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
            _view.PrimaryActionRequested += OnPrimaryActionRequested;
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
            _view.PrimaryActionRequested -= OnPrimaryActionRequested;
            _view.RetryRequested -= OnRetryRequested;
            _eventsRegistered = false;
        }

        private void ApplyPreviewValues(bool force)
        {
            if (_view == null || !_view.IsBound)
            {
                return;
            }

            int missionNumber = Mathf.Clamp(_previewMissionNumber, 1, 3);
            bool missionChanged = force || _appliedMissionNumber != missionNumber;
            if (missionChanged)
            {
                MissionPreviewSelection selection = CreateSelection(missionNumber);
                if (!MissionDetailPreviewCatalog.TryGetContent(selection, out MissionDetailPreviewContent content))
                {
                    Debug.LogWarning(
                        $"[MissionDetailPanelController] Unsupported mission preview selection: {missionNumber}.");
                    _view.SetContent(null);
                }
                else
                {
                    _view.SetContent(content);
                }

                _appliedMissionNumber = missionNumber;
            }

            bool stateChanged = force || _appliedState != _previewState;
            if (stateChanged)
            {
                _view.SetPreviewState(_previewState);
                _appliedState = _previewState;
            }
        }

        private static MissionPreviewSelection CreateSelection(int missionNumber)
        {
            return missionNumber switch
            {
                1 => new MissionPreviewSelection(
                    "g5_lq_t1_m01",
                    NutriMindSubject.LiteraQuest,
                    NutriMindTerm.Term1,
                    1,
                    "The Festival Storybook Rescue",
                    false,
                    string.Empty),
                3 => new MissionPreviewSelection(
                    "g5_lq_t1_m03",
                    NutriMindSubject.LiteraQuest,
                    NutriMindTerm.Term1,
                    3,
                    "The Hall of Speaking Sounds",
                    false,
                    string.Empty),
                _ => MissionDetailPreviewCatalog.CreateCanonicalDefaultSelection()
            };
        }

        private void OnBackRequested() =>
            Debug.Log("[MissionDetailPanelController] Back to Missions requested — preview only.");

        private void OnPrimaryActionRequested(MissionDetailPreviewActionRequest request) =>
            Debug.Log(
                $"[MissionDetailPanelController] Primary action requested: action={request.Action}, " +
                $"mission={request.MissionId} — preview only.");

        private void OnRetryRequested() =>
            Debug.Log("[MissionDetailPanelController] Retry requested — preview only.");

#if UNITY_EDITOR
        [ContextMenu("Cycle Mission Detail State")]
        private void CycleMissionDetailState()
        {
            _previewState = (MissionDetailPreviewState)(
                ((int)_previewState + 1) % System.Enum.GetValues(typeof(MissionDetailPreviewState)).Length);
            ApplyPreviewValues(force: true);
            EditorUtility.SetDirty(this);
        }

        [ContextMenu("Cycle Mission Detail Mission")]
        private void CycleMissionDetailMission()
        {
            _previewMissionNumber = _previewMissionNumber >= 3 ? 1 : _previewMissionNumber + 1;
            ApplyPreviewValues(force: true);
            EditorUtility.SetDirty(this);
        }
#endif
    }
}
