using System.Threading;
using System.Threading.Tasks;
using NutriMind.App.Presentation;
using NutriMind.App.Routing;
using NutriMind.App.State;
using NutriMind.App.UI;
using NutriMind.Core.Bootstrap;
using NutriMind.Core.Data;
using NutriMind.Core.Utilities;

namespace NutriMind.App.Presentation
{
    /// <summary>
    /// Runtime presenter for the Home route.
    /// Binds AuthenticatedStudentState to HomePanelView greeting and progress labels.
    /// Navigates to Quiz Portal or Announcements on view request.
    /// </summary>
    public sealed class HomePresenter : RoutePresenterBase
    {
        private readonly HomePanelView _view;

        public HomePresenter(AppLifetime lifetime, HomePanelView view)
            : base(lifetime)
        {
            _view = view;

            if (_view.IsBound)
            {
                _view.ContinueMissionRequested += OnContinueMission;
                _view.QuizPortalRequested += OnQuizPortalRequested;
                _view.AnnouncementsRequested += OnAnnouncementsRequested;
            }
        }

        public void LoadAsync()
        {
            if (Disposed || !_view.IsBound)
            {
                return;
            }

            ApplyState();
        }

        protected override void OnDispose()
        {
            if (_view.IsBound)
            {
                _view.ContinueMissionRequested -= OnContinueMission;
                _view.QuizPortalRequested -= OnQuizPortalRequested;
                _view.AnnouncementsRequested -= OnAnnouncementsRequested;
            }
        }

        private void ApplyState()
        {
            AuthenticatedStudentState state = Lifetime.AuthenticatedStudentState;
            if (state == null || !_view.IsBound)
            {
                return;
            }

            _view.SetGreeting(AppViewMappers.FormatDisplayName(state.Profile));

            MissionSummary active = state.ActiveMission;
            if (active?.Progress != null)
            {
                int completed = active.Progress.CompletedAreaCount;
                int required = active.Progress.RequiredAreaCount > 0 ? active.Progress.RequiredAreaCount : 3;
                _view.SetProgress(completed, required);
            }
            else
            {
                _view.SetProgress(0, 3);
            }
        }

        private void OnContinueMission()
        {
            if (Disposed)
            {
                return;
            }

            MissionSummary active = Lifetime.AuthenticatedStudentState?.ActiveMission;
            string missionId = active?.Id;
            string subjectId = active?.SubjectId;
            string termId = active?.TermId;

            if (!string.IsNullOrWhiteSpace(missionId)
                && !string.IsNullOrWhiteSpace(subjectId)
                && !string.IsNullOrWhiteSpace(termId))
            {
                TaskUtilities.ForgetSafely(
                    Lifetime.Router?.NavigateAsync(
                        AppRouteId.MissionDetail,
                        AppRouteContext.ForMission(missionId, subjectId, termId),
                        NavigationToken),
                    NavigationToken,
                    "Home.ContinueMissionDetail");
                return;
            }

            if (!string.IsNullOrWhiteSpace(subjectId) && !string.IsNullOrWhiteSpace(termId))
            {
                TaskUtilities.ForgetSafely(
                    Lifetime.Router?.NavigateAsync(
                        AppRouteId.MissionList,
                        AppRouteContext.ForTerm(subjectId, termId),
                        NavigationToken),
                    NavigationToken,
                    "Home.ContinueMissionList");
                return;
            }

            TaskUtilities.ForgetSafely(
                Lifetime.Router?.NavigateAsync(AppRouteId.Subjects, AppRouteContext.Empty, NavigationToken),
                NavigationToken,
                "Home.ContinueSubjects");
        }

        private void OnQuizPortalRequested()
        {
            if (Disposed)
            {
                return;
            }

            // Must use NavigationToken — EnterQuizPortal unloads Main and disposes this presenter.
            TaskUtilities.ForgetSafely(
                Lifetime.Router?.EnterQuizPortalAsync(
                    AppRouteContext.Empty.WithReturnToMainOnQuizBack(true),
                    NavigationToken),
                NavigationToken,
                "Home.QuizPortal");
        }

        private void OnAnnouncementsRequested()
        {
            if (Disposed)
            {
                return;
            }

            TaskUtilities.ForgetSafely(
                Lifetime.Router?.PushAsync(AppRouteId.Announcements, AppRouteContext.Empty, NavigationToken),
                NavigationToken,
                "Home.Announcements");
        }
    }
}
