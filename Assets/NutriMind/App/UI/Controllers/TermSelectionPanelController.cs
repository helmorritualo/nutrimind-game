using UnityEngine;
using UnityEngine.UIElements;

namespace NutriMind.App.UI
{
    /// <summary>
    /// Standalone <c>UIDocument</c> preview adapter for <see cref="TermSelectionPanelView"/>.
    /// Binds the content-only Term Selection route for isolated layout review.
    /// Logs Back / selection / open / unavailable requests without recreating
    /// AppShell chrome, navigation, or toasts.
    /// Does not perform routing, progress loading, or networking.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class TermSelectionPanelController : MonoBehaviour
    {
        private UIDocument _uiDocument;
        private TermSelectionPanelView _view;
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
                panelRoot?.Q<VisualElement>("term-selection-root");
            if (componentRoot == null)
            {
                Invoke(nameof(BindWhenReady), 0.05f);
                return;
            }

            panelRoot.style.flexGrow = 1;
            panelRoot.style.width = Length.Percent(100);
            panelRoot.style.height = Length.Percent(100);

            UnbindView();

            _view = new TermSelectionPanelView(componentRoot);
            if (!_view.IsBound)
            {
                Debug.LogWarning(
                    "[TermSelectionPanelController] TermSelectionPanelView " +
                    "failed to bind term-selection-root.");
                _view.Dispose();
                _view = null;
                return;
            }

            if (!_eventsRegistered)
            {
                _view.BackRequested += OnBackRequested;
                _view.TermSelected += OnTermSelected;
                _view.OpenTermRequested += OnOpenTermRequested;
                _view.UnavailableTermRequested += OnUnavailableTermRequested;
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
                    _view.TermSelected -= OnTermSelected;
                    _view.OpenTermRequested -= OnOpenTermRequested;
                    _view.UnavailableTermRequested -= OnUnavailableTermRequested;
                    _eventsRegistered = false;
                }

                _view.Dispose();
                _view = null;
            }
        }

        private void OnBackRequested()
        {
            Debug.Log(
                "[TermSelectionPanelController] Back to Subjects requested — preview only.");
        }

        private void OnTermSelected(NutriMindTerm term)
        {
            Debug.Log(
                $"[TermSelectionPanelController] Term selected: Term {(int)term}.");
        }

        private void OnOpenTermRequested(NutriMindTerm term)
        {
            Debug.Log(
                $"[TermSelectionPanelController] View Missions requested: Term {(int)term} — preview only.");
        }

        private void OnUnavailableTermRequested(NutriMindTerm term)
        {
            string reason = _view != null
                ? _view.GetUnavailableReason(term)
                : "Previous Term Incomplete";

            Debug.Log(
                $"[TermSelectionPanelController] Term {(int)term} unavailable: {reason}.");
        }
    }
}
