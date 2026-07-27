using System;
using System.Collections.Generic;
using NutriMind.Core.Data;

namespace NutriMind.Core.Networking
{
    /// <summary>
    /// Maps mock-only fixture DTOs to neutral domain models.
    /// </summary>
    public static class MockFixtureMapper
    {
        public static StudentProfile ToProfile(MockStudentProfileFixture fixture)
        {
            if (fixture == null)
            {
                return null;
            }

            return new StudentProfile
            {
                Id = NullIfEmpty(fixture.id),
                DisplayName = NullIfEmpty(fixture.display_name),
                LrnMasked = NullIfEmpty(fixture.lrn_masked),
                GradeId = NullIfEmpty(fixture.grade_id),
                IsActive = fixture.is_active,
                Section = ToSection(fixture.section)
            };
        }

        public static StudentSection ToSection(MockSectionFixture fixture)
        {
            if (fixture == null)
            {
                return null;
            }

            return new StudentSection
            {
                Id = NullIfEmpty(fixture.id),
                Name = NullIfEmpty(fixture.name),
                GradeId = NullIfEmpty(fixture.grade_id)
            };
        }

        public static LoginResult ToLoginResult(MockLoginSuccessFixture fixture)
        {
            if (fixture == null)
            {
                return null;
            }

            return new LoginResult
            {
                TokenType = string.IsNullOrWhiteSpace(fixture.token_type) ? "Bearer" : fixture.token_type,
                AccessToken = NullIfEmpty(fixture.access_token),
                ExpiresAt = ParseDate(fixture.expires_at),
                Student = ToProfile(fixture.student)
            };
        }

        public static ClientConfiguration ToConfig(MockConfigFixture fixture)
        {
            if (fixture == null)
            {
                return null;
            }

            return new ClientConfiguration
            {
                ApiVersion = NullIfEmpty(fixture.api_version),
                MinimumClientVersion = NullIfEmpty(fixture.minimum_client_version),
                RequiredManifestVersion = NullIfEmpty(fixture.required_manifest_version),
                MaintenanceMode = fixture.maintenance_mode,
                MaintenanceMessage = NullIfEmpty(fixture.maintenance_message),
                SyncMaxEventsPerBatch = fixture.sync_max_events_per_batch > 0
                    ? fixture.sync_max_events_per_batch
                    : 100,
                SyncMaxRequestBytes = fixture.sync_max_request_bytes > 0
                    ? fixture.sync_max_request_bytes
                    : 512 * 1024,
                SyncMaxEventPayloadBytes = fixture.sync_max_event_payload_bytes > 0
                    ? fixture.sync_max_event_payload_bytes
                    : 16 * 1024,
                SyncMaxEventAgeDays = fixture.sync_max_event_age_days > 0
                    ? fixture.sync_max_event_age_days
                    : 90
            };
        }

        public static StudentSettings ToSettings(MockSettingsFixture fixture)
        {
            if (fixture == null)
            {
                return null;
            }

            return new StudentSettings
            {
                AudioVolume = fixture.audio_volume,
                MusicVolume = fixture.music_volume,
                Language = NullIfEmpty(fixture.language),
                ReducedMotion = fixture.reduced_motion,
                NotificationsEnabled = fixture.notifications_enabled
            };
        }

        public static SubjectSummary ToSubject(MockSubjectFixture fixture)
        {
            if (fixture == null)
            {
                return null;
            }

            return new SubjectSummary
            {
                Id = NullIfEmpty(fixture.id),
                Slug = NullIfEmpty(fixture.slug),
                Name = NullIfEmpty(fixture.name),
                IsActive = fixture.is_active
            };
        }

        public static IReadOnlyList<SubjectSummary> ToSubjects(MockSubjectFixture[] fixtures)
        {
            if (fixtures == null || fixtures.Length == 0)
            {
                return Array.Empty<SubjectSummary>();
            }

            var list = new List<SubjectSummary>(fixtures.Length);
            for (int i = 0; i < fixtures.Length; i++)
            {
                SubjectSummary mapped = ToSubject(fixtures[i]);
                if (mapped != null)
                {
                    list.Add(mapped);
                }
            }

            return list;
        }

        public static TermSummary ToTerm(MockTermFixture fixture)
        {
            if (fixture == null)
            {
                return null;
            }

            return new TermSummary
            {
                Id = NullIfEmpty(fixture.id),
                Name = NullIfEmpty(fixture.name),
                Order = fixture.order,
                IsActive = fixture.is_active
            };
        }

        public static IReadOnlyList<TermSummary> ToTerms(MockTermFixture[] fixtures)
        {
            if (fixtures == null || fixtures.Length == 0)
            {
                return Array.Empty<TermSummary>();
            }

            var list = new List<TermSummary>(fixtures.Length);
            for (int i = 0; i < fixtures.Length; i++)
            {
                TermSummary mapped = ToTerm(fixtures[i]);
                if (mapped != null)
                {
                    list.Add(mapped);
                }
            }

            return list;
        }

        public static MissionProgressSummary ToMissionProgress(MockMissionProgressFixture fixture)
        {
            if (fixture == null)
            {
                return new MissionProgressSummary();
            }

            return new MissionProgressSummary
            {
                State = NullIfEmpty(fixture.state),
                ActiveAreaId = NullIfEmpty(fixture.active_area_id),
                CompletedAreaCount = fixture.completed_area_count,
                RequiredAreaCount = fixture.required_area_count > 0 ? fixture.required_area_count : 3,
                CollectibleCount = fixture.collectible_count,
                RequiredCollectibleCount = fixture.required_collectible_count > 0
                    ? fixture.required_collectible_count
                    : 3,
                CompletedAt = ParseDate(fixture.completed_at),
                Revision = fixture.revision
            };
        }

        public static MissionSummary ToMissionSummary(MockMissionSummaryFixture fixture)
        {
            if (fixture == null)
            {
                return null;
            }

            return new MissionSummary
            {
                Id = NullIfEmpty(fixture.id),
                GradeId = NullIfEmpty(fixture.grade_id),
                SubjectId = NullIfEmpty(fixture.subject_id),
                TermId = NullIfEmpty(fixture.term_id),
                Title = NullIfEmpty(fixture.title),
                Order = fixture.order,
                Status = NullIfEmpty(fixture.status),
                LockedReason = NullIfEmpty(fixture.locked_reason),
                AvailabilitySource = NullIfEmpty(fixture.availability_source),
                TeacherPolicy = NullIfEmpty(fixture.teacher_policy),
                AreaCount = fixture.area_count > 0 ? fixture.area_count : 3,
                Progress = ToMissionProgress(fixture.progress)
            };
        }

        public static IReadOnlyList<MissionSummary> ToMissionSummaries(MockMissionSummaryFixture[] fixtures)
        {
            if (fixtures == null || fixtures.Length == 0)
            {
                return Array.Empty<MissionSummary>();
            }

            var list = new List<MissionSummary>(fixtures.Length);
            for (int i = 0; i < fixtures.Length; i++)
            {
                MissionSummary mapped = ToMissionSummary(fixtures[i]);
                if (mapped != null)
                {
                    list.Add(mapped);
                }
            }

            return list;
        }

        public static AreaProgressSummary ToArea(MockAreaProgressFixture fixture)
        {
            if (fixture == null)
            {
                return null;
            }

            return new AreaProgressSummary
            {
                Id = NullIfEmpty(fixture.id),
                Order = fixture.order,
                Phase = NullIfEmpty(fixture.phase),
                State = NullIfEmpty(fixture.state),
                ReviewRequired = fixture.review_required,
                CollectibleId = NullIfEmpty(fixture.collectible_id),
                CollectibleCollected = fixture.collectible_collected,
                CompletedAt = ParseDate(fixture.completed_at)
            };
        }

        public static MissionDetail ToMissionDetail(MockMissionDetailFixture fixture)
        {
            if (fixture == null)
            {
                return null;
            }

            var areas = new List<AreaProgressSummary>();
            if (fixture.areas != null)
            {
                for (int i = 0; i < fixture.areas.Length; i++)
                {
                    AreaProgressSummary area = ToArea(fixture.areas[i]);
                    if (area != null)
                    {
                        areas.Add(area);
                    }
                }
            }

            IReadOnlyList<string> unlocked = Array.Empty<string>();
            if (fixture.newly_unlocked_ids != null && fixture.newly_unlocked_ids.Length > 0)
            {
                unlocked = (string[])fixture.newly_unlocked_ids.Clone();
            }

            return new MissionDetail
            {
                Mission = ToMissionSummary(fixture.mission),
                Areas = areas,
                NewlyUnlockedIds = unlocked
            };
        }

        public static SyncStatus ToSyncStatus(MockSyncStatusFixture fixture)
        {
            if (fixture == null)
            {
                return new SyncStatus();
            }

            return new SyncStatus
            {
                PendingServerActions = fixture.pending_server_actions,
                Revision = fixture.revision,
                PendingOutboxCount = fixture.pending_outbox_count,
                LastSyncedAt = ParseDate(fixture.last_synced_at)
            };
        }

        public static BootstrapSnapshot ToBootstrap(MockBootstrapFixture fixture)
        {
            if (fixture == null)
            {
                return null;
            }

            return new BootstrapSnapshot
            {
                Profile = ToProfile(fixture.profile),
                RequiredManifestVersion = NullIfEmpty(fixture.required_manifest_version),
                Subjects = ToSubjects(fixture.subjects),
                Missions = ToMissionSummaries(fixture.missions),
                QuizPortalAvailableCount = fixture.quiz_portal_available_count,
                AnnouncementsVisibleCount = fixture.announcements_visible_count,
                Sync = ToSyncStatus(fixture.sync)
            };
        }

        public static ProgressSummary ToProgressSummary(MockProgressSummaryFixture fixture)
        {
            if (fixture == null)
            {
                return new ProgressSummary();
            }

            return new ProgressSummary
            {
                MissionsStarted = fixture.missions_started,
                MissionsCompleted = fixture.missions_completed,
                AreasCompleted = fixture.areas_completed,
                ReviewRequiredCount = fixture.review_required_count,
                QuizAttempts = fixture.quiz_attempts
            };
        }

        public static QuizSummary ToQuizSummary(MockQuizSummaryFixture fixture)
        {
            if (fixture == null)
            {
                return null;
            }

            return new QuizSummary
            {
                Id = NullIfEmpty(fixture.id),
                Title = NullIfEmpty(fixture.title),
                SubjectId = NullIfEmpty(fixture.subject_id),
                TermId = NullIfEmpty(fixture.term_id),
                Status = NullIfEmpty(fixture.status),
                LockedReason = NullIfEmpty(fixture.locked_reason),
                OpensAt = ParseDate(fixture.opens_at),
                ClosesAt = ParseDate(fixture.closes_at),
                MaxAttempts = fixture.max_attempts,
                AttemptsUsed = fixture.attempts_used,
                ResultVisibility = NullIfEmpty(fixture.result_visibility)
            };
        }

        public static IReadOnlyList<QuizSummary> ToQuizSummaries(MockQuizSummaryFixture[] fixtures)
        {
            if (fixtures == null || fixtures.Length == 0)
            {
                return Array.Empty<QuizSummary>();
            }

            var list = new List<QuizSummary>(fixtures.Length);
            for (int i = 0; i < fixtures.Length; i++)
            {
                QuizSummary mapped = ToQuizSummary(fixtures[i]);
                if (mapped != null)
                {
                    list.Add(mapped);
                }
            }

            return list;
        }

        public static QuizDetail ToQuizDetail(MockQuizDetailFixture fixture)
        {
            if (fixture == null)
            {
                return null;
            }

            var questions = new List<QuizQuestionDelivery>();
            if (fixture.questions != null)
            {
                for (int i = 0; i < fixture.questions.Length; i++)
                {
                    MockQuizQuestionFixture q = fixture.questions[i];
                    if (q == null)
                    {
                        continue;
                    }

                    var options = new List<QuizOptionDelivery>();
                    if (q.options != null)
                    {
                        for (int o = 0; o < q.options.Length; o++)
                        {
                            MockQuizOptionFixture opt = q.options[o];
                            if (opt == null)
                            {
                                continue;
                            }

                            options.Add(new QuizOptionDelivery
                            {
                                Key = NullIfEmpty(opt.key),
                                Text = NullIfEmpty(opt.text)
                            });
                        }
                    }

                    questions.Add(new QuizQuestionDelivery
                    {
                        Id = NullIfEmpty(q.id),
                        Type = NullIfEmpty(q.type),
                        Prompt = NullIfEmpty(q.prompt),
                        Hint = NullIfEmpty(q.hint),
                        Points = q.points,
                        Options = options
                    });
                }
            }

            return new QuizDetail
            {
                Id = NullIfEmpty(fixture.id),
                Title = NullIfEmpty(fixture.title),
                Instructions = NullIfEmpty(fixture.instructions),
                SubjectId = NullIfEmpty(fixture.subject_id),
                TermId = NullIfEmpty(fixture.term_id),
                Status = NullIfEmpty(fixture.status),
                LockedReason = NullIfEmpty(fixture.locked_reason),
                OpensAt = ParseDate(fixture.opens_at),
                ClosesAt = ParseDate(fixture.closes_at),
                MaxAttempts = fixture.max_attempts,
                AttemptsUsed = fixture.attempts_used,
                ResultVisibility = NullIfEmpty(fixture.result_visibility),
                QuestionCount = fixture.question_count > 0 ? fixture.question_count : questions.Count,
                Questions = questions
            };
        }

        public static QuizResult ToQuizResult(MockQuizResultFixture fixture)
        {
            if (fixture == null)
            {
                return null;
            }

            var answers = new List<QuizResultAnswer>();
            if (fixture.answers != null)
            {
                for (int i = 0; i < fixture.answers.Length; i++)
                {
                    MockQuizResultAnswerFixture a = fixture.answers[i];
                    if (a == null)
                    {
                        continue;
                    }

                    IReadOnlyList<string> keys = Array.Empty<string>();
                    if (a.selected_option_keys != null && a.selected_option_keys.Length > 0)
                    {
                        keys = (string[])a.selected_option_keys.Clone();
                    }

                    answers.Add(new QuizResultAnswer
                    {
                        QuestionId = NullIfEmpty(a.question_id),
                        Correct = a.correct,
                        EarnedPoints = a.earned_points,
                        SelectedOptionKeys = keys
                    });
                }
            }

            return new QuizResult
            {
                AttemptId = NullIfEmpty(fixture.attempt_id),
                QuizId = NullIfEmpty(fixture.quiz_id),
                ClientAttemptUuid = NullIfEmpty(fixture.client_attempt_uuid),
                Status = NullIfEmpty(fixture.status),
                EarnedPoints = fixture.earned_points,
                PossiblePoints = fixture.possible_points,
                Percentage = fixture.percentage,
                Passed = fixture.passed,
                CorrectCount = fixture.correct_count,
                IncorrectCount = fixture.incorrect_count,
                UnansweredCount = fixture.unanswered_count,
                SubmittedAt = ParseDate(fixture.submitted_at) ?? default,
                FeedbackVisible = fixture.feedback_visible,
                Answers = answers
            };
        }

        public static QuizHistoryEntry ToQuizHistoryEntry(MockQuizHistoryEntryFixture fixture)
        {
            if (fixture == null)
            {
                return null;
            }

            return new QuizHistoryEntry
            {
                AttemptId = NullIfEmpty(fixture.attempt_id),
                QuizId = NullIfEmpty(fixture.quiz_id),
                QuizTitle = NullIfEmpty(fixture.quiz_title),
                SubjectId = NullIfEmpty(fixture.subject_id),
                TermId = NullIfEmpty(fixture.term_id),
                Status = NullIfEmpty(fixture.status),
                Percentage = fixture.percentage,
                Passed = fixture.passed,
                SubmittedAt = ParseDate(fixture.submitted_at) ?? default,
                FeedbackVisible = fixture.feedback_visible
            };
        }

        public static IReadOnlyList<QuizHistoryEntry> ToQuizHistory(MockQuizHistoryEntryFixture[] fixtures)
        {
            if (fixtures == null || fixtures.Length == 0)
            {
                return Array.Empty<QuizHistoryEntry>();
            }

            var list = new List<QuizHistoryEntry>(fixtures.Length);
            for (int i = 0; i < fixtures.Length; i++)
            {
                QuizHistoryEntry mapped = ToQuizHistoryEntry(fixtures[i]);
                if (mapped != null)
                {
                    list.Add(mapped);
                }
            }

            return list;
        }

        public static RewardSummary ToReward(MockRewardFixture fixture)
        {
            if (fixture == null)
            {
                return null;
            }

            return new RewardSummary
            {
                RewardCode = NullIfEmpty(fixture.reward_code),
                Title = NullIfEmpty(fixture.title),
                Description = NullIfEmpty(fixture.description),
                SupportingText = NullIfEmpty(fixture.supporting_text),
                Status = NullIfEmpty(fixture.status),
                LockedReason = NullIfEmpty(fixture.locked_reason),
                EarnedAt = ParseDate(fixture.earned_at),
                UsedAt = ParseDate(fixture.used_at)
            };
        }

        public static IReadOnlyList<RewardSummary> ToRewards(MockRewardFixture[] fixtures)
        {
            if (fixtures == null || fixtures.Length == 0)
            {
                return Array.Empty<RewardSummary>();
            }

            var list = new List<RewardSummary>(fixtures.Length);
            for (int i = 0; i < fixtures.Length; i++)
            {
                RewardSummary mapped = ToReward(fixtures[i]);
                if (mapped != null)
                {
                    list.Add(mapped);
                }
            }

            return list;
        }

        public static CertificateSummary ToCertificate(MockCertificateFixture fixture)
        {
            if (fixture == null)
            {
                return null;
            }

            return new CertificateSummary
            {
                Id = NullIfEmpty(fixture.id),
                Title = NullIfEmpty(fixture.title),
                TypeLabel = NullIfEmpty(fixture.type_label),
                Status = NullIfEmpty(fixture.status),
                EligibilityDescription = NullIfEmpty(fixture.eligibility_description),
                RecognitionText = NullIfEmpty(fixture.recognition_text),
                LockedReason = NullIfEmpty(fixture.locked_reason),
                IssuedAt = ParseDate(fixture.issued_at),
                AwardedToDisplayName = NullIfEmpty(fixture.awarded_to_display_name)
            };
        }

        public static IReadOnlyList<CertificateSummary> ToCertificates(MockCertificateFixture[] fixtures)
        {
            if (fixtures == null || fixtures.Length == 0)
            {
                return Array.Empty<CertificateSummary>();
            }

            var list = new List<CertificateSummary>(fixtures.Length);
            for (int i = 0; i < fixtures.Length; i++)
            {
                CertificateSummary mapped = ToCertificate(fixtures[i]);
                if (mapped != null)
                {
                    list.Add(mapped);
                }
            }

            return list;
        }

        public static AnnouncementSummary ToAnnouncement(MockAnnouncementFixture fixture)
        {
            if (fixture == null)
            {
                return null;
            }

            return new AnnouncementSummary
            {
                Id = NullIfEmpty(fixture.id),
                Title = NullIfEmpty(fixture.title),
                Summary = NullIfEmpty(fixture.summary),
                Body = NullIfEmpty(fixture.body),
                AudienceLabel = NullIfEmpty(fixture.audience_label),
                Kind = NullIfEmpty(fixture.kind),
                IsUnread = fixture.is_unread,
                PublishedAt = ParseDate(fixture.published_at),
                ExpiresAt = ParseDate(fixture.expires_at)
            };
        }

        public static IReadOnlyList<AnnouncementSummary> ToAnnouncements(MockAnnouncementFixture[] fixtures)
        {
            if (fixtures == null || fixtures.Length == 0)
            {
                return Array.Empty<AnnouncementSummary>();
            }

            var list = new List<AnnouncementSummary>(fixtures.Length);
            for (int i = 0; i < fixtures.Length; i++)
            {
                AnnouncementSummary mapped = ToAnnouncement(fixtures[i]);
                if (mapped != null)
                {
                    list.Add(mapped);
                }
            }

            return list;
        }

        public static LeaderboardPage ToLeaderboard(MockLeaderboardFixture fixture)
        {
            if (fixture == null)
            {
                return new LeaderboardPage();
            }

            LeaderboardContext context = null;
            if (fixture.context != null)
            {
                context = new LeaderboardContext
                {
                    Scope = NullIfEmpty(fixture.context.scope),
                    ScopeLabel = NullIfEmpty(fixture.context.scope_label),
                    Metric = NullIfEmpty(fixture.context.metric),
                    MetricLabel = NullIfEmpty(fixture.context.metric_label),
                    PeriodLabel = NullIfEmpty(fixture.context.period_label),
                    ContextLabel = NullIfEmpty(fixture.context.context_label)
                };
            }

            var entries = new List<LeaderboardEntry>();
            if (fixture.entries != null)
            {
                for (int i = 0; i < fixture.entries.Length; i++)
                {
                    MockLeaderboardEntryFixture e = fixture.entries[i];
                    if (e == null)
                    {
                        continue;
                    }

                    entries.Add(new LeaderboardEntry
                    {
                        Rank = e.rank,
                        PrivacySafeName = NullIfEmpty(e.privacy_safe_name),
                        MissionsCompleted = e.missions_completed,
                        IsCurrentStudent = e.is_current_student
                    });
                }
            }

            return new LeaderboardPage
            {
                Context = context,
                Entries = entries
            };
        }

        public static string NullIfEmpty(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        public static DateTimeOffset? ParseDate(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            if (DateTimeOffset.TryParse(value.Trim(), out DateTimeOffset parsed))
            {
                return parsed.ToUniversalTime();
            }

            return null;
        }
    }
}
