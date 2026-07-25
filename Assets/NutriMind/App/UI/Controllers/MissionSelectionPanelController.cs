using UnityEngine;
using UnityEngine.UIElements;

namespace NutriMind.App.UI
{
    /// <summary>Standalone preview adapter for <see cref="MissionSelectionPanelView"/>.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class MissionSelectionPanelController : MonoBehaviour
    {
        private UIDocument _uiDocument;
        private MissionSelectionPanelView _view;
        private bool _eventsRegistered;

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
            VisualElement componentRoot = panelRoot?.Q<VisualElement>("mission-selection-root");
            if (componentRoot == null)
            {
                Invoke(nameof(BindWhenReady), 0.05f);
                return;
            }

            panelRoot.style.flexGrow = 1;
            panelRoot.style.width = Length.Percent(100);
            panelRoot.style.height = Length.Percent(100);

            UnbindView();
            _view = new MissionSelectionPanelView(componentRoot);
            if (!_view.IsBound)
            {
                Debug.LogWarning("[MissionSelectionPanelController] MissionSelectionPanelView failed to bind mission-selection-root.");
                _view.Dispose();
                _view = null;
                return;
            }

            if (!_eventsRegistered)
            {
                _view.BackRequested += OnBackRequested;
                _view.MissionSelected += OnMissionSelected;
                _view.StartMissionRequested += OnStartMissionRequested;
                _view.ContinueMissionRequested += OnContinueMissionRequested;
                _view.ReviewMissionRequested += OnReviewMissionRequested;
                _view.LockedMissionRequested += OnLockedMissionRequested;
                _eventsRegistered = true;
            }
        }

        private void Unbind()
        {
            UnbindView();
            _uiDocument = null;
        }

        private void UnbindView()
        {
            if (_view == null)
            {
                return;
            }

            if (_eventsRegistered)
            {
                _view.BackRequested -= OnBackRequested;
                _view.MissionSelected -= OnMissionSelected;
                _view.StartMissionRequested -= OnStartMissionRequested;
                _view.ContinueMissionRequested -= OnContinueMissionRequested;
                _view.ReviewMissionRequested -= OnReviewMissionRequested;
                _view.LockedMissionRequested -= OnLockedMissionRequested;
                _eventsRegistered = false;
            }

            _view.Dispose();
            _view = null;
        }

        private void OnBackRequested() => Debug.Log("[MissionSelectionPanelController] Back requested — preview only.");
        private void OnMissionSelected(MissionPreviewSelection selection) =>
            Debug.Log($"[MissionSelectionPanelController] Mission {selection.MissionNumber} selected.");
        private void OnStartMissionRequested(MissionPreviewSelection selection) =>
            Debug.Log($"[MissionSelectionPanelController] Start Mission {selection.MissionNumber} requested — preview only.");
        private void OnContinueMissionRequested(MissionPreviewSelection selection) =>
            Debug.Log($"[MissionSelectionPanelController] Continue Mission {selection.MissionNumber} requested — preview only.");
        private void OnReviewMissionRequested(MissionPreviewSelection selection) =>
            Debug.Log($"[MissionSelectionPanelController] Review Mission {selection.MissionNumber} requested — preview only.");
        private void OnLockedMissionRequested(MissionPreviewSelection selection) =>
            Debug.Log($"[MissionSelectionPanelController] Locked Mission {selection.MissionNumber}: {selection.LockReason}.");
    }
}
