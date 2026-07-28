using UnityEngine;
using UnityEngine.UIElements;

namespace NutriMind.Gameplay.UI
{
    /// <summary>
    /// Standalone UI Toolkit preview adapter for <see cref="GameplayStudentHudView"/>.
    /// Presentation only — applies inspector preview data and logs UI intent events.
    /// Does not perform mission loading, player control, networking, or persistence.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class GameplayStudentHudPreviewController : MonoBehaviour
    {
        [SerializeField]
        private string _previewMissionTitle = "The Festival Storybook Rescue";

        [SerializeField]
        private string _previewAreaPhase = "Area 1 • Discover";

        [SerializeField]
        [TextArea]
        private string _previewObjective = "Inspect the damaged storybook beside Farmer Lira.";

        [SerializeField]
        private int _previewCollectedFragments;

        [SerializeField]
        private int _previewTotalFragments = 3;

        [SerializeField]
        private string _previewInteractionLabel = "Inspect";

        [SerializeField]
        private bool _previewInteractionAvailable = true;

        [SerializeField]
        private bool _previewPauseAvailable = true;

        [SerializeField]
        private bool _previewShowLookHelper = true;

        [SerializeField]
        private bool _previewInputEnabled = true;

        private UIDocument _uiDocument;
        private GameplayStudentHudView _view;
        private string _appliedSignature;
        private bool _eventsRegistered;

        private void OnEnable()
        {
            _uiDocument = GetComponent<UIDocument>();
            BindWhenReady();
        }

        private void OnDisable()
        {
            Unbind();
            CancelInvoke(nameof(BindWhenReady));
        }

        private void OnValidate()
        {
            if (!isActiveAndEnabled || _view == null || !_view.IsBound)
            {
                return;
            }

            ApplyPreviewModel(force: true);
        }

        [ContextMenu("Reset HUD Preview")]
        private void ResetHudPreview()
        {
            _previewMissionTitle = "The Festival Storybook Rescue";
            _previewAreaPhase = "Area 1 • Discover";
            _previewObjective = "Inspect the damaged storybook beside Farmer Lira.";
            _previewCollectedFragments = 0;
            _previewTotalFragments = 3;
            _previewInteractionLabel = "Inspect";
            _previewInteractionAvailable = true;
            _previewPauseAvailable = true;
            _previewShowLookHelper = true;
            _previewInputEnabled = true;
            ApplyPreviewModel(force: true);
        }

        [ContextMenu("Toggle Input Enabled")]
        private void ToggleInputEnabled()
        {
            _previewInputEnabled = !_previewInputEnabled;
            ApplyPreviewModel(force: true);
        }

        [ContextMenu("Cycle Fragment Count")]
        private void CycleFragmentCount()
        {
            int total = Mathf.Max(1, _previewTotalFragments);
            _previewCollectedFragments = (_previewCollectedFragments + 1) % (total + 1);
            ApplyPreviewModel(force: true);
        }

        [ContextMenu("Toggle Interaction Availability")]
        private void ToggleInteractionAvailability()
        {
            _previewInteractionAvailable = !_previewInteractionAvailable;
            ApplyPreviewModel(force: true);
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
            if (panelRoot == null)
            {
                Invoke(nameof(BindWhenReady), 0.05f);
                return;
            }

            VisualElement hudRoot = panelRoot.name == "gameplay-student-hud-root"
                ? panelRoot
                : panelRoot.Q<VisualElement>("gameplay-student-hud-root");

            if (hudRoot == null)
            {
                Invoke(nameof(BindWhenReady), 0.05f);
                return;
            }

            panelRoot.style.flexGrow = 1;
            panelRoot.style.width = Length.Percent(100);
            panelRoot.style.height = Length.Percent(100);

            UnbindViewOnly();
            _view = new GameplayStudentHudView(hudRoot);
            if (!_view.IsBound)
            {
                _view.Dispose();
                _view = null;
                Invoke(nameof(BindWhenReady), 0.05f);
                return;
            }

            RegisterViewEvents();
            ApplyPreviewModel(force: true);
        }

        private void Unbind()
        {
            UnbindViewOnly();
            _uiDocument = null;
            _appliedSignature = null;
        }

        private void UnbindViewOnly()
        {
            UnregisterViewEvents();

            if (_view != null)
            {
                _view.Dispose();
                _view = null;
            }
        }

        private void RegisterViewEvents()
        {
            if (_view == null || _eventsRegistered)
            {
                return;
            }

            _view.MoveChanged += OnMoveChanged;
            _view.LookDeltaChanged += OnLookDeltaChanged;
            _view.InteractionRequested += OnInteractionRequested;
            _view.PauseRequested += OnPauseRequested;
            _eventsRegistered = true;
        }

        private void UnregisterViewEvents()
        {
            if (_view == null || !_eventsRegistered)
            {
                _eventsRegistered = false;
                return;
            }

            _view.MoveChanged -= OnMoveChanged;
            _view.LookDeltaChanged -= OnLookDeltaChanged;
            _view.InteractionRequested -= OnInteractionRequested;
            _view.PauseRequested -= OnPauseRequested;
            _eventsRegistered = false;
        }

        private void ApplyPreviewModel(bool force)
        {
            if (_view == null || !_view.IsBound)
            {
                return;
            }

            string signature = string.Join("|",
                _previewMissionTitle,
                _previewAreaPhase,
                _previewObjective,
                _previewCollectedFragments,
                _previewTotalFragments,
                _previewInteractionLabel,
                _previewInteractionAvailable,
                _previewPauseAvailable,
                _previewShowLookHelper,
                _previewInputEnabled);

            if (!force && signature == _appliedSignature)
            {
                return;
            }

            _appliedSignature = signature;
            _view.SetViewModel(new GameplayStudentHudViewModel
            {
                MissionTitle = _previewMissionTitle,
                AreaPhaseLabel = _previewAreaPhase,
                ObjectiveText = _previewObjective,
                CollectedFragments = _previewCollectedFragments,
                TotalFragments = _previewTotalFragments,
                InteractionLabel = _previewInteractionLabel,
                InteractionIconClass = GameplayStudentHudViewModel.DefaultInteractionIconClass,
                InteractionAvailable = _previewInteractionAvailable,
                PauseAvailable = _previewPauseAvailable,
                ShowLookHelper = _previewShowLookHelper,
                InputEnabled = _previewInputEnabled
            });
        }

        private void OnMoveChanged(Vector2 move)
        {
            if (move.sqrMagnitude > 0.0001f)
            {
                Debug.Log($"[GameplayStudentHudPreview] Move intent: {move}");
            }
        }

        private void OnLookDeltaChanged(Vector2 delta)
        {
            if (delta.sqrMagnitude > 0.0001f)
            {
                Debug.Log($"[GameplayStudentHudPreview] Look delta intent: {delta}");
            }
        }

        private void OnInteractionRequested()
        {
            Debug.Log("[GameplayStudentHudPreview] Interaction requested.");
        }

        private void OnPauseRequested()
        {
            Debug.Log("[GameplayStudentHudPreview] Pause requested.");
        }
    }
}
