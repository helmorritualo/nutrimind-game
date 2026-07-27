using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NutriMind.App.Routing;
using NutriMind.App.UI;
using NutriMind.Core.Bootstrap;
using NutriMind.Core.Data;
using NutriMind.Core.Persistence;
using NutriMind.Core.Utilities;

namespace NutriMind.App.Presentation
{
    /// <summary>
    /// Runtime presenter for Announcements.
    /// Maps domain summaries to presentation Preview items (transitional boundary).
    /// Persists read state in SQLite; updates the shell badge via AuthenticatedStudentState.
    /// </summary>
    public sealed class AnnouncementsPresenter : RoutePresenterBase
    {
        private readonly AnnouncementsPanelView _view;
        private readonly AppShellRuntimeController _shellRuntime;

        public AnnouncementsPresenter(
            AppLifetime lifetime,
            AnnouncementsPanelView view,
            AppShellRuntimeController shellRuntime)
            : base(lifetime)
        {
            _view = view;
            _shellRuntime = shellRuntime;
            _view.BackRequested += OnBack;
            _view.SelectionChanged += OnSelectionChanged;
            _view.RetryRequested += OnRetry;
        }

        public void LoadAsync()
        {
            if (Disposed)
            {
                return;
            }

            TaskUtilities.ForgetSafely(FetchAsync(Cts.Token), Cts.Token, "Announcements.Load");
        }

        protected override void OnDispose()
        {
            _view.BackRequested -= OnBack;
            _view.SelectionChanged -= OnSelectionChanged;
            _view.RetryRequested -= OnRetry;
        }

        private async Task FetchAsync(CancellationToken token)
        {
            _view.SetPreviewState(AnnouncementsPreviewState.Loading);

            AppResult<IReadOnlyList<AnnouncementSummary>> result =
                await Lifetime.Gateway.GetAnnouncementsAsync(token).ConfigureAwait(true);

            if (Disposed || token.IsCancellationRequested)
            {
                return;
            }

            if (result.IsSuccess)
            {
                IReadOnlyList<AnnouncementSummary> items =
                    result.Value ?? Array.Empty<AnnouncementSummary>();

                if (items.Count == 0)
                {
                    _view.SetItems(Array.Empty<AnnouncementPreviewItem>());
                    _view.SetPreviewState(AnnouncementsPreviewState.Empty);
                    return;
                }

                AnnouncementPreviewItem[] preview = MapAnnouncements(items);
                _view.SetItems(preview);

                var readIds = new List<string>();
                IAnnouncementReadRepository repo = Lifetime.AnnouncementReadRepository;
                for (int i = 0; i < items.Count; i++)
                {
                    string id = items[i]?.Id;
                    if (string.IsNullOrEmpty(id) || repo == null)
                    {
                        continue;
                    }

                    AppResult<bool> isRead = repo.IsRead(id);
                    if (isRead.IsSuccess && isRead.Value)
                    {
                        readIds.Add(id);
                    }
                }

                _view.SetReadPresentationIds(readIds);
                _view.SetPreviewState(AnnouncementsPreviewState.Content);
                RefreshUnreadBadge(items.Count - readIds.Count);
                return;
            }

            if (IsUnauthorized(result.Error))
            {
                HandleUnauthorized();
                return;
            }

            if (IsOffline(result.Error))
            {
                _view.SetPreviewState(AnnouncementsPreviewState.OfflineCached);
                return;
            }

            _view.SetPreviewState(AnnouncementsPreviewState.RecoverableError);
        }

        private void OnSelectionChanged(AnnouncementPreviewSelection selection)
        {
            if (Disposed || string.IsNullOrEmpty(selection.PresentationId))
            {
                return;
            }

            IAnnouncementReadRepository repo = Lifetime.AnnouncementReadRepository;
            if (repo == null)
            {
                return;
            }

            string readUtc = DateTimeOffset.UtcNow.ToUniversalTime().ToString("o");
            repo.MarkRead(selection.PresentationId, readUtc);

            int current = Lifetime.AuthenticatedStudentState?.AnnouncementVisibleCount ?? 0;
            Lifetime.AuthenticatedStudentState?.SetAnnouncementVisibleCount(Math.Max(0, current - 1));
            _shellRuntime?.RefreshBadges();
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
                Lifetime.Router?.BackAsync(Cts.Token),
                Cts.Token,
                "Announcements.Back");
        }

        private void RefreshUnreadBadge(int unread)
        {
            Lifetime.AuthenticatedStudentState?.SetAnnouncementVisibleCount(Math.Max(0, unread));
            _shellRuntime?.RefreshBadges();
        }

        private static AnnouncementPreviewItem[] MapAnnouncements(IReadOnlyList<AnnouncementSummary> items)
        {
            var mapped = new AnnouncementPreviewItem[items.Count];
            for (int i = 0; i < items.Count; i++)
            {
                AnnouncementSummary item = items[i];
                mapped[i] = new AnnouncementPreviewItem(
                    presentationId: item.Id ?? ("announcement-" + i),
                    title: item.Title ?? string.Empty,
                    summary: item.Summary ?? string.Empty,
                    bodyPlainText: item.Body ?? item.Summary ?? string.Empty,
                    audienceLabel: item.AudienceLabel ?? string.Empty,
                    publishedDateText: item.PublishedAt?.ToString("yyyy-MM-dd") ?? string.Empty,
                    publicationWindowText: string.Empty,
                    initiallyUnread: item.IsUnread,
                    kind: MapKind(item.Kind),
                    iconClass: "ds-icon--bell");
            }

            return mapped;
        }

        private static AnnouncementPreviewKind MapKind(string kind)
        {
            if (string.IsNullOrWhiteSpace(kind))
            {
                return AnnouncementPreviewKind.Learning;
            }

            if (kind.IndexOf("schedule", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return AnnouncementPreviewKind.Schedule;
            }

            if (kind.IndexOf("offline", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return AnnouncementPreviewKind.OfflineReminder;
            }

            return AnnouncementPreviewKind.Learning;
        }
    }
}
