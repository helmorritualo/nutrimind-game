using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NutriMind.App.Presentation;
using NutriMind.App.Routing;
using NutriMind.App.UI;
using NutriMind.Core.Bootstrap;
using NutriMind.Core.Data;
using NutriMind.Core.Utilities;

namespace NutriMind.App.Presentation
{
    /// <summary>
    /// Runtime presenter for the Certificates route.
    /// Fetches earned certificates from the server and maps them to <see cref="CertificatePreviewItem"/>.
    /// Certificates are server-authoritative; they are never calculated locally.
    /// </summary>
    public sealed class CertificatesPresenter : RoutePresenterBase
    {
        private readonly CertificatesPanelView _view;

        public CertificatesPresenter(AppLifetime lifetime, CertificatesPanelView view)
            : base(lifetime)
        {
            _view = view;
            _view.BackToRewardsRequested += OnBack;
            _view.RetryRequested += OnRetry;
        }

        public void LoadAsync()
        {
            if (Disposed)
            {
                return;
            }

            TaskUtilities.ForgetSafely(FetchAsync(Cts.Token), Cts.Token, "Certificates.Load");
        }

        protected override void OnDispose()
        {
            _view.BackToRewardsRequested -= OnBack;
            _view.RetryRequested -= OnRetry;
        }

        private async Task FetchAsync(CancellationToken token)
        {
            _view.SetPreviewState(CertificatesPreviewState.Loading);

            AppResult<IReadOnlyList<CertificateSummary>> result =
                await Lifetime.Gateway.GetCertificatesAsync(token).ConfigureAwait(true);

            if (Disposed || token.IsCancellationRequested)
            {
                return;
            }

            if (result.IsSuccess)
            {
                IReadOnlyList<CertificateSummary> items =
                    result.Value ?? (IReadOnlyList<CertificateSummary>)System.Array.Empty<CertificateSummary>();

                if (items.Count == 0)
                {
                    _view.SetPreviewState(CertificatesPreviewState.Empty);
                }
                else
                {
                    _view.SetItems(AppViewMappers.MapCertificateSummaries(items));
                    _view.SetPreviewState(CertificatesPreviewState.Content);
                }
            }
            else if (IsUnauthorized(result.Error))
            {
                HandleUnauthorized();
            }
            else
            {
                _view.SetPreviewState(AppViewMappers.ErrorToCertificatesPreviewState(result.Error));
            }
        }

        private void OnRetry()
        {
            if (Disposed)
            {
                return;
            }

            TaskUtilities.ForgetSafely(FetchAsync(Cts.Token), Cts.Token, "Certificates.Retry");
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
                "Certificates.Back");
        }
    }
}
