using UnityEngine;
using UnityEngine.UIElements;

namespace NutriMind.App.UI
{
    /// <summary>Standalone preview adapter for <see cref="LockedMissionPanelView"/>.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class LockedMissionPanelController : MonoBehaviour
    {
        [SerializeField]
        private MissionLockReason _previewReason = MissionLockReason.TeacherRestricted;

        private UIDocument _uiDocument;
        private LockedMissionPanelView _view;
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
            _uiDocument ??= GetComponent<UIDocument>();
            if (_uiDocument == null) return;

            VisualElement panelRoot = _uiDocument.rootVisualElement;
            VisualElement componentRoot = panelRoot?.Q<VisualElement>("locked-mission-root");
            if (componentRoot == null)
            {
                Invoke(nameof(BindWhenReady), 0.05f);
                return;
            }

            panelRoot.style.flexGrow = 1;
            panelRoot.style.width = Length.Percent(100);
            panelRoot.style.height = Length.Percent(100);
            UnbindView();

            _view = new LockedMissionPanelView(componentRoot);
            if (!_view.IsBound)
            {
                Debug.LogWarning("[LockedMissionPanelController] LockedMissionPanelView failed to bind locked-mission-root.");
                _view.Dispose();
                _view = null;
                return;
            }

            _view.SetContext(CreatePreviewContext(_previewReason));
            _view.BackRequested += OnBackRequested;
            _view.PrimaryActionRequested += OnPrimaryActionRequested;
            _view.SecondaryActionRequested += OnSecondaryActionRequested;
            _eventsRegistered = true;
        }

        private void Unbind()
        {
            UnbindView();
            _uiDocument = null;
        }

        private void UnbindView()
        {
            if (_view == null) return;
            if (_eventsRegistered)
            {
                _view.BackRequested -= OnBackRequested;
                _view.PrimaryActionRequested -= OnPrimaryActionRequested;
                _view.SecondaryActionRequested -= OnSecondaryActionRequested;
                _eventsRegistered = false;
            }
            _view.Dispose();
            _view = null;
        }

        private static LockedMissionPreviewContext CreatePreviewContext(MissionLockReason reason)
        {
            return reason switch
            {
                MissionLockReason.PrerequisiteRequired => new LockedMissionPreviewContext(NutriMindSubject.Science, NutriMindTerm.Term2, 4, "Life Cycles", reason, "Requires: Habitats Around Us (Mission 3)"),
                MissionLockReason.NotPublished => new LockedMissionPreviewContext(NutriMindSubject.Science, NutriMindTerm.Term2, 6, "Producers and Consumers", reason, "Prerequisite complete — no additional missions required."),
                MissionLockReason.NotDownloaded => new LockedMissionPreviewContext(NutriMindSubject.Science, NutriMindTerm.Term2, 7, "Adaptations for Survival", reason, "Prerequisite complete — no additional missions required."),
                MissionLockReason.OfflineUnavailable => new LockedMissionPreviewContext(NutriMindSubject.PeAndHealth, NutriMindTerm.Term2, 3, "Human Body Systems", reason, "Prerequisite complete — no additional missions required."),
                _ => new LockedMissionPreviewContext(NutriMindSubject.Science, NutriMindTerm.Term2, 5, "Ecosystems and Balance", MissionLockReason.TeacherRestricted, "Prerequisite complete — no additional missions required.")
            };
        }

        private void OnBackRequested() => Debug.Log("[LockedMissionPanelController] Back requested — preview only.");
        private void OnPrimaryActionRequested() => Debug.Log($"[LockedMissionPanelController] {_view?.Context.Reason} action requested — preview only.");
        private void OnSecondaryActionRequested() => Debug.Log("[LockedMissionPanelController] Secondary action requested — preview only.");
    }
}
