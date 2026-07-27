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
        private readonly HashSet<string> _locallyReadIds = new(StringComparer.Ordinal);

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

            TaskUtilities.ForgetSafely(FetchAsync(RequestToken), RequestToken, "Announcements.Load");
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
                    _locallyReadIds.Clear();
                    _view.SetItems(Array.Empty<AnnouncementPreviewItem>());
                    _view.SetReadPresentationIds(Array.Empty<string>());
                    _view.SetPreviewState(AnnouncementsPreviewState.Empty);
                    RefreshUnreadBadge(0);
                    return;
                }

                AnnouncementPreviewItem[] preview = MapAnnouncements(items);
                _view.SetItems(preview);

                _locallyReadIds.Clear();
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
                        _locallyReadIds.Add(id);
                    }
                }

                _view.SetReadPresentationIds(_locallyReadIds);
                _view.SetPreviewState(AnnouncementsPreviewState.Content);
                RefreshUnreadBadge(CountEffectiveUnread(items, _locallyReadIds));
                return;
            }

            if (IsUnauthorized(result.Error))
            {
                HandleUnauthorized();
                return;
            }

            if (IsOffline(result.Error))
            {
                _view.SetItems(Array.Empty<AnnouncementPreviewItem>());
                _view.SetPreviewState(AnnouncementsPreviewState.OfflineUnavailable);
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

            if (!_view.IsPresentationUnread(selection.PresentationId))
            {
                return;
            }

            IAnnouncementReadRepository repo = Lifetime.AnnouncementReadRepository;
            if (repo == null)
            {
                return;
            }

            AppResult<bool> existing = repo.IsRead(selection.PresentationId);
            if (existing.IsFailure)
            {
                return;
            }

            if (existing.Value)
            {
                _locallyReadIds.Add(selection.PresentationId);
                _view.SetReadPresentationIds(_locallyReadIds);
                RefreshUnreadBadge(_view.UnreadCount);
                return;
            }

            string readUtc = DateTimeOffset.UtcNow.ToUniversalTime().ToString("o");
            AppResult marked = repo.MarkRead(selection.PresentationId, readUtc);
            if (marked.IsFailure)
            {
                return;
            }

            _locallyReadIds.Add(selection.PresentationId);
            _view.SetReadPresentationIds(_locallyReadIds);
            RefreshUnreadBadge(_view.UnreadCount);
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
                    publishedDateText: item.PublishedAt?.ToString("yyyy-MM-dd") ?? "Date unavailable",
                    publicationWindowText: item.ExpiresAt.HasValue
                        ? "Visible until " + item.ExpiresAt.Value.ToString("yyyy-MM-dd")
                        : "Currently visible",
                    initiallyUnread: item.IsUnread,
                    kind: MapKind(item.Kind),
                    iconClass: "ds-icon--bell");
            }

            return mapped;
        }

        private static int CountEffectiveUnread(
            IReadOnlyList<AnnouncementSummary> items,
            IReadOnlyCollection<string> locallyReadIds)
        {
            if (items == null || items.Count == 0)
            {
                return 0;
            }

            var local = locallyReadIds == null
                ? new HashSet<string>(StringComparer.Ordinal)
                : new HashSet<string>(locallyReadIds, StringComparer.Ordinal);
            int unread = 0;
            for (int i = 0; i < items.Count; i++)
            {
                AnnouncementSummary item = items[i];
                bool locallyRead = item != null && local.Contains(item.Id ?? string.Empty);
                if (AppViewMappers.IsAnnouncementEffectivelyUnread(item, locallyRead))
                {
                    unread++;
                }
            }

            return unread;
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
