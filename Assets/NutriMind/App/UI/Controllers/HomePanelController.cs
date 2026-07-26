using UnityEngine;
using UnityEngine.UIElements;

namespace NutriMind.App.UI
{
    /// <summary>
    /// Standalone <c>UIDocument</c> preview adapter for <see cref="HomePanelView"/>.
    /// Binds the content-only Home route for isolated layout review.
    /// Logs Continue / Quiz Portal / Announcements requests without recreating AppShell chrome or toasts.
    /// Does not perform routing, progress loading, sync, or networking.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class HomePanelController : MonoBehaviour
    {
        private UIDocument _uiDocument;
        private HomePanelView _view;
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
            VisualElement componentRoot = panelRoot?.Q<VisualElement>("home-root");
            if (componentRoot == null)
            {
                Invoke(nameof(BindWhenReady), 0.05f);
                return;
            }

            panelRoot.style.flexGrow = 1;
            panelRoot.style.width = Length.Percent(100);
            panelRoot.style.height = Length.Percent(100);

            UnbindView();

            _view = new HomePanelView(componentRoot);
            if (!_view.IsBound)
            {
                Debug.LogWarning(
                    "[HomePanelController] HomePanelView failed to bind home-root.");
                _view.Dispose();
                _view = null;
                return;
            }

            if (!_eventsRegistered)
            {
                _view.ContinueMissionRequested += OnContinueMissionRequested;
                _view.QuizPortalRequested += OnQuizPortalRequested;
                _view.AnnouncementsRequested += OnAnnouncementsRequested;
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
                    _view.ContinueMissionRequested -= OnContinueMissionRequested;
                    _view.QuizPortalRequested -= OnQuizPortalRequested;
                    _view.AnnouncementsRequested -= OnAnnouncementsRequested;
                    _eventsRegistered = false;
                }

                _view.Dispose();
                _view = null;
            }
        }

        private void OnContinueMissionRequested()
        {
            Debug.Log("[HomePanelController] Continue Mission requested — preview only.");
        }

        private void OnQuizPortalRequested()
        {
            Debug.Log("[HomePanelController] Quiz Portal requested — preview only.");
        }

        private void OnAnnouncementsRequested()
        {
            Debug.Log("[HomePanelController] Announcements requested — preview only.");
        }
    }
}
