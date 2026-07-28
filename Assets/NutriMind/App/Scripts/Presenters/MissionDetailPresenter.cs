using System;
using System.Threading;
using System.Threading.Tasks;
using NutriMind.App.Features;
using NutriMind.App.Routing;
using NutriMind.App.UI;
using NutriMind.Core.Bootstrap;
using NutriMind.Core.Data;
using NutriMind.Core.Networking;
using NutriMind.Core.Utilities;

namespace NutriMind.App.Presentation
{
    /// <summary>
    /// Runtime presenter for Mission Detail.
    /// Uses catalog presentation content when available; launch records local
    /// progress/outbox and opens the playable scene for supported missions.
    /// </summary>
    public sealed class MissionDetailPresenter : RoutePresenterBase
    {
        private readonly MissionDetailPanelView _view;
        private readonly AppRouteContext _ctx;
        private readonly AppShellRuntimeController _shellRuntime;
        private bool _launchInProgress;

        public MissionDetailPresenter(
            AppLifetime lifetime,
            MissionDetailPanelView view,
            AppRouteContext ctx,
            AppShellRuntimeController shellRuntime = null)
            : base(lifetime)
        {
            _view = view;
            _ctx = ctx;
            _shellRuntime = shellRuntime;
            _view.PrimaryActionRequested += OnPrimaryAction;
            _view.BackRequested += OnBack;
            _view.RetryRequested += OnRetry;
        }

        public void LoadAsync()
        {
            if (Disposed)
            {
                return;
            }

            TaskUtilities.ForgetSafely(FetchAsync(Cts.Token), Cts.Token, "MissionDetail.Load");
        }

        protected override void OnDispose()
        {
            _view.PrimaryActionRequested -= OnPrimaryAction;
            _view.BackRequested -= OnBack;
            _view.RetryRequested -= OnRetry;
        }

        private async Task FetchAsync(CancellationToken token)
        {
            _view.SetPreviewState(MissionDetailPreviewState.Loading);

            if (string.IsNullOrWhiteSpace(_ctx?.MissionId))
            {
                NutriMindLog.RuntimeWarning("MissionDetailPresenter: missing mission id; returning.");
                TaskUtilities.ForgetSafely(
                    Lifetime.Router?.BackAsync(token),
                    token,
                    "MissionDetail.InvalidContext");
                return;
            }

            if (Lifetime.Connectivity != null && !Lifetime.Connectivity.IsOnline)
            {
                if (TryBindLocalPlayableCatalog())
                {
                    _view.SetPreviewState(MissionDetailPreviewState.Content);
                    return;
                }

                _view.SetPreviewState(MissionDetailPreviewState.OfflineUnavailable);
                return;
            }

            var request = new MissionIdRequest { MissionId = _ctx.MissionId };
            AppResult<MissionDetail> result =
                await Lifetime.Gateway.GetMissionDetailAsync(request, token).ConfigureAwait(true);

            if (Disposed || token.IsCancellationRequested)
            {
                return;
            }

            if (result.IsSuccess)
            {
                BindPresentation(result.Value);
                _view.SetPreviewState(MissionDetailPreviewState.Content);
                return;
            }

            if (IsUnauthorized(result.Error))
            {
                HandleUnauthorized();
                return;
            }

            if (result.Error != null
                && string.Equals(result.Error.Code, AppErrorCodes.MissionLocked, StringComparison.Ordinal))
            {
                _view.SetPreviewState(MissionDetailPreviewState.Locked);
                return;
            }

            if (IsOffline(result.Error))
            {
                if (TryBindLocalPlayableCatalog())
                {
                    _view.SetPreviewState(MissionDetailPreviewState.Content);
                    return;
                }

                _view.SetPreviewState(MissionDetailPreviewState.OfflineUnavailable);
                return;
            }

            _view.SetPreviewState(MissionDetailPreviewState.RecoverableError);
        }

        private bool TryBindLocalPlayableCatalog()
        {
            if (!MissionGameplaySceneCatalog.TryGet(_ctx.MissionId, out _))
            {
                return false;
            }

            var selection = new MissionPreviewSelection(
                MissionGameplaySceneCatalog.FestivalStorybookMissionId,
                NutriMindSubject.LiteraQuest,
                NutriMindTerm.Term1,
                1,
                "The Festival Storybook Rescue",
                false,
                string.Empty);

            if (!MissionDetailPreviewCatalog.TryGetContent(
                    selection,
                    out MissionDetailPreviewContent content))
            {
                return false;
            }

            _view.SetContent(content);
            return true;
        }

        private void BindPresentation(MissionDetail detail)
        {
            MissionSummary mission = detail?.Mission;
            NutriMindSubject subject = AppViewMappers.MapSubject(mission?.SubjectId ?? _ctx.SubjectId);
            NutriMindTerm term = AppViewMappers.MapTerm(mission?.TermId ?? _ctx.TermId);
            string title = mission?.Title ?? _ctx.MissionId;
            string missionId = mission?.Id ?? _ctx.MissionId;
            var selection = new MissionPreviewSelection(
                missionId,
                subject,
                term,
                Math.Max(1, mission?.Order ?? 1),
                title,
                false,
                string.Empty);

            if (MissionDetailPreviewCatalog.TryGetContent(selection, out MissionDetailPreviewContent content))
            {
                _view.SetContent(content);
                return;
            }

            if (MissionDetailPreviewCatalog.TryGetContent(
                    MissionDetailPreviewCatalog.CreateCanonicalDefaultSelection(),
                    out MissionDetailPreviewContent fallback))
            {
                _view.SetContent(fallback);
            }
        }

        private void OnPrimaryAction(MissionDetailPreviewActionRequest request)
        {
            if (Disposed || _launchInProgress)
            {
                return;
            }

            MissionLaunchKind kind = request.Action switch
            {
                MissionDetailPrimaryAction.Continue =>
                    MissionLaunchKind.SimulatedContinue,

                MissionDetailPrimaryAction.Review =>
                    MissionLaunchKind.SimulatedReview,

                _ =>
                    MissionLaunchKind.SimulatedStart
            };

            TaskUtilities.ForgetSafely(
                LaunchAsync(request.MissionId, kind),
                Cts.Token,
                "MissionDetail.Launch");
        }

        private async Task LaunchAsync(string missionId, MissionLaunchKind kind)
        {
            if (_launchInProgress)
            {
                return;
            }

            IMissionLaunchService launcher = Lifetime.MissionLaunchService;
            if (launcher == null)
            {
                _shellRuntime?.ShowToast(
                    "Mission launch is not available.",
                    AppShellToastTone.Warning);
                return;
            }

            _launchInProgress = true;
            _shellRuntime?.ShowGlobalLoading("Preparing mission…");

            try
            {
                MissionLaunchResult result = await launcher.LaunchAsync(
                    missionId,
                    kind,
                    Cts.Token).ConfigureAwait(true);

                if (result.Succeeded && result.GameplaySceneOpened)
                {
                    // Main scene / shell may already be destroyed by the single-scene load.
                    return;
                }

                if (Disposed)
                {
                    return;
                }

                _shellRuntime?.HideGlobalLoading();

                if (result.Succeeded)
                {
                    _shellRuntime?.ShowToast(
                        result.Message ?? "Mission launch recorded locally.",
                        AppShellToastTone.Success);
                }
                else
                {
                    string toast = !string.IsNullOrWhiteSpace(result.Message)
                        ? result.Message
                        : LearnerFacingErrorMapper.Map(result.Error);
                    _shellRuntime?.ShowToast(toast, AppShellToastTone.Danger);
                }
            }
            catch (OperationCanceledException)
            {
                if (!Disposed)
                {
                    _shellRuntime?.HideGlobalLoading();
                }
            }
            catch (Exception)
            {
                if (!Disposed)
                {
                    _shellRuntime?.HideGlobalLoading();
                    _shellRuntime?.ShowToast(
                        "The mission scene could not be opened.",
                        AppShellToastTone.Danger);
                }
            }
            finally
            {
                _launchInProgress = false;
            }
        }

        private void OnRetry()
        {
            if (!Disposed)
            {
                LoadAsync();
            }
        }

        private void OnBack()
        {
            if (Disposed)
            {
                return;
            }

            TaskUtilities.ForgetSafely(
                Lifetime.Router?.BackAsync(NavigationToken),
                NavigationToken,
                "MissionDetail.Back");
        }
    }
}
