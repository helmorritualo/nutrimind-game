using UnityEngine;
using UnityEngine.UIElements;

namespace NutriMind.App.UI
{
    /// <summary>
    /// Standalone <c>UIDocument</c> preview adapter for <see cref="SubjectSelectionPanelView"/>.
    /// Binds the content-only Subject Selection route for isolated layout review.
    /// Logs Back / selection / Continue / unavailable requests without recreating
    /// AppShell chrome, navigation, or toasts.
    /// Does not perform routing, progress loading, availability checks, or networking.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class SubjectSelectionPanelController : MonoBehaviour
    {
        private UIDocument _uiDocument;
        private SubjectSelectionPanelView _view;
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
            VisualElement componentRoot =
                panelRoot?.Q<VisualElement>("subject-selection-root");
            if (componentRoot == null)
            {
                Invoke(nameof(BindWhenReady), 0.05f);
                return;
            }

            panelRoot.style.flexGrow = 1;
            panelRoot.style.width = Length.Percent(100);
            panelRoot.style.height = Length.Percent(100);

            UnbindView();

            _view = new SubjectSelectionPanelView(componentRoot);
            if (!_view.IsBound)
            {
                Debug.LogWarning(
                    "[SubjectSelectionPanelController] SubjectSelectionPanelView " +
                    "failed to bind subject-selection-root.");
                _view.Dispose();
                _view = null;
                return;
            }

            if (!_eventsRegistered)
            {
                _view.BackRequested += OnBackRequested;
                _view.SubjectSelected += OnSubjectSelected;
                _view.ContinueSubjectRequested += OnContinueSubjectRequested;
                _view.UnavailableSubjectRequested += OnUnavailableSubjectRequested;
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
            if (_view != null)
            {
                if (_eventsRegistered)
                {
                    _view.BackRequested -= OnBackRequested;
                    _view.SubjectSelected -= OnSubjectSelected;
                    _view.ContinueSubjectRequested -= OnContinueSubjectRequested;
                    _view.UnavailableSubjectRequested -= OnUnavailableSubjectRequested;
                    _eventsRegistered = false;
                }

                _view.Dispose();
                _view = null;
            }
        }

        private void OnBackRequested()
        {
            Debug.Log(
                "[SubjectSelectionPanelController] Back to Home requested — preview only.");
        }

        private void OnSubjectSelected(NutriMindSubject subject)
        {
            Debug.Log(
                $"[SubjectSelectionPanelController] Subject selected: {GetSubjectLabel(subject)}.");
        }

        private void OnContinueSubjectRequested(NutriMindSubject subject)
        {
            Debug.Log(
                $"[SubjectSelectionPanelController] View Terms requested: {GetSubjectLabel(subject)} — preview only.");
        }

        private void OnUnavailableSubjectRequested(NutriMindSubject subject)
        {
            Debug.Log(
                $"[SubjectSelectionPanelController] {GetSubjectLabel(subject)} is unavailable in this classroom.");
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
    }
}
