using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NutriMind.App.Presentation;
using NutriMind.App.Routing;
using NutriMind.App.UI;
using NutriMind.Core.Bootstrap;
using NutriMind.Core.Data;
using NutriMind.Core.Persistence;
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

            TaskUtilities.ForgetSafely(FetchAsync(RequestToken), RequestToken, "Certificates.Load");
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
                    result.Value ?? (IReadOnlyList<CertificateSummary>)Array.Empty<CertificateSummary>();
                PersistCertificatesCache(items);

                if (items.Count == 0)
                {
                    _view.SetItems(Array.Empty<CertificatePreviewItem>());
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
            else if (IsOffline(result.Error))
            {
                IReadOnlyList<CertificateSummary> cached = LoadCertificatesCache();
                if (cached != null)
                {
                    if (cached.Count == 0)
                    {
                        _view.SetItems(Array.Empty<CertificatePreviewItem>());
                        _view.SetPreviewState(CertificatesPreviewState.Empty);
                    }
                    else
                    {
                        _view.SetItems(AppViewMappers.MapCertificateSummaries(cached));
                        _view.SetPreviewState(CertificatesPreviewState.OfflineCached);
                    }
                }
                else
                {
                    _view.SetItems(Array.Empty<CertificatePreviewItem>());
                    _view.SetPreviewState(CertificatesPreviewState.OfflineUnavailable);
                }
            }
            else
            {
                _view.SetPreviewState(AppViewMappers.ErrorToCertificatesPreviewState(result.Error));
            }
        }

        private void PersistCertificatesCache(IReadOnlyList<CertificateSummary> items)
        {
            string studentId = Lifetime.AuthenticatedStudentState?.Profile?.Id
                ?? Lifetime.CurrentProfile?.Id;
            if (string.IsNullOrWhiteSpace(studentId) || Lifetime.ResourceCacheRepository == null)
            {
                return;
            }

            LearnerRouteCache.SaveCertificates(
                Lifetime.ResourceCacheRepository,
                studentId,
                items ?? Array.Empty<CertificateSummary>(),
                DateTimeOffset.UtcNow.ToString("o"));
        }

        private IReadOnlyList<CertificateSummary> LoadCertificatesCache()
        {
            string studentId = Lifetime.AuthenticatedStudentState?.Profile?.Id
                ?? Lifetime.CurrentProfile?.Id;
            if (string.IsNullOrWhiteSpace(studentId) || Lifetime.ResourceCacheRepository == null)
            {
                return null;
            }

            AppResult<IReadOnlyList<CertificateSummary>> cached = LearnerRouteCache.LoadCertificates(
                Lifetime.ResourceCacheRepository,
                studentId);
            return cached.IsSuccess ? cached.Value : null;
        }

        private void OnRetry()
        {
            if (Disposed)
            {
                return;
            }

            TaskUtilities.ForgetSafely(FetchAsync(RequestToken), RequestToken, "Certificates.Retry");
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
