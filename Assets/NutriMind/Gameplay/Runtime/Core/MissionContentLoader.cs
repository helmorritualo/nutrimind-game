using System;
using System.Collections.Generic;
using UnityEngine;

namespace NutriMind.Gameplay.Runtime
{
    [Serializable]
    public sealed class MissionDialogueLineDto
    {
        public string speaker;
        public string text;
    }

    [Serializable]
    public sealed class MissionQuestionOptionDto
    {
        public string id;
        public string text;
    }

    [Serializable]
    public sealed class MissionQuestionDto
    {
        public string id;
        public string type;
        public bool is_scored;
        public string prompt;
        public MissionQuestionOptionDto[] options;
        public string[] correct_option_ids;
        public int attempt_limit;
        public string first_wrong_hint;
        public string second_wrong_explanation;
        public string correct_feedback;
    }

    [Serializable]
    public sealed class MissionCollectibleDto
    {
        public string id;
        public string type;
        public int quantity;
    }

    [Serializable]
    public sealed class MissionAreaDto
    {
        public string area_id;
        public int area_number;
        public string title;
        public string phase;
        public string story;
        public MissionDialogueLineDto[] opening_dialogue;
        public string[] learning_clues;
        public string[] required_interactions;
        public MissionQuestionDto[] questions;
        public string world_action;
        public string world_result;
        public MissionCollectibleDto collectible;
        public string story_source;
    }

    [Serializable]
    public sealed class MissionContentDto
    {
        public string schema_version;
        public string content_version;
        public string mission_id;
        public string title;
        public int collectible_count;
        public MissionAreaDto[] areas;
    }

    public sealed class MissionAreaContent
    {
        public MissionAreaDto Area { get; set; }
        public IReadOnlyList<MissionQuestionDto> Questions => Area.questions ?? Array.Empty<MissionQuestionDto>();
    }

    public sealed class MissionContentData
    {
        public MissionContentDto Raw { get; set; }
        public MissionAreaContent Area1 { get; set; }
        public MissionAreaContent Area2 { get; set; }

        public static bool TryLoad(TextAsset jsonAsset, out MissionContentData data, out string error)
        {
            data = null;
            error = string.Empty;

            if (jsonAsset == null)
            {
                error = "Mission JSON TextAsset is missing.";
                return false;
            }

            MissionContentDto dto;
            try
            {
                dto = JsonUtility.FromJson<MissionContentDto>(jsonAsset.text);
            }
            catch (Exception ex)
            {
                error = "Failed to parse mission JSON: " + ex.Message;
                return false;
            }

            if (dto == null)
            {
                error = "Mission JSON deserialized to null.";
                return false;
            }

            if (!string.Equals(dto.mission_id, MissionContentIds.MissionId, StringComparison.Ordinal))
            {
                error = "Unexpected mission_id: " + dto.mission_id;
                return false;
            }

            if (dto.collectible_count != 3)
            {
                error = "Expected collectible_count = 3.";
                return false;
            }

            MissionAreaDto area1 = FindArea(dto, MissionContentIds.Area1Id);
            MissionAreaDto area2 = FindArea(dto, MissionContentIds.Area2Id);
            if (area1 == null || area2 == null)
            {
                error = "Required areas 1 and 2 are missing from mission JSON.";
                return false;
            }

            if (!ValidateAreaQuestions(area1, MissionContentIds.Area1QuestionIds, out error))
            {
                return false;
            }

            if (!ValidateAreaQuestions(area2, MissionContentIds.Area2QuestionIds, out error))
            {
                return false;
            }

            data = new MissionContentData
            {
                Raw = dto,
                Area1 = new MissionAreaContent { Area = area1 },
                Area2 = new MissionAreaContent { Area = area2 }
            };
            return true;
        }

        private static MissionAreaDto FindArea(MissionContentDto dto, string areaId)
        {
            if (dto.areas == null)
            {
                return null;
            }

            foreach (MissionAreaDto area in dto.areas)
            {
                if (area != null && string.Equals(area.area_id, areaId, StringComparison.Ordinal))
                {
                    return area;
                }
            }

            return null;
        }

        private static bool ValidateAreaQuestions(MissionAreaDto area, string[] expectedIds, out string error)
        {
            error = string.Empty;
            if (area.questions == null || area.questions.Length != expectedIds.Length)
            {
                error = "Area " + area.area_id + " must contain exactly " + expectedIds.Length + " questions.";
                return false;
            }

            for (int i = 0; i < expectedIds.Length; i++)
            {
                MissionQuestionDto question = area.questions[i];
                if (question == null || !string.Equals(question.id, expectedIds[i], StringComparison.Ordinal))
                {
                    error = "Missing or mismatched question id at index " + i + " for area " + area.area_id + ".";
                    return false;
                }

                if (question.options == null || question.options.Length == 0)
                {
                    error = "Question " + question.id + " has no options.";
                    return false;
                }

                if (question.correct_option_ids == null || question.correct_option_ids.Length == 0)
                {
                    error = "Question " + question.id + " has no correct_option_ids.";
                    return false;
                }

                if (question.attempt_limit != 2)
                {
                    error = "Question " + question.id + " must use attempt_limit = 2.";
                    return false;
                }
            }

            return true;
        }
    }

    public static class MissionWorldActionContent
    {
        public sealed class CaptionOption
        {
            public string Id { get; set; }
            public string Text { get; set; }
            public bool IsCorrect { get; set; }
        }

        public sealed class EventCard
        {
            public string Id { get; set; }
            public string Text { get; set; }
            public int CorrectSlotIndex { get; set; }
        }

        public static IReadOnlyList<CaptionOption> GetArea1CaptionOptions()
        {
            return new[]
            {
                new CaptionOption
                {
                    Id = "caption_correct",
                    Text = "They plan to carry a friendship banner to the Chronicle Courtyard.",
                    IsCorrect = true
                },
                new CaptionOption
                {
                    Id = "caption_wrong_pronoun",
                    Text = "He plans to cancel the festival in Story Square.",
                    IsCorrect = false
                },
                new CaptionOption
                {
                    Id = "caption_wrong_goal",
                    Text = "The banner carries them to a hidden laboratory.",
                    IsCorrect = false
                },
                new CaptionOption
                {
                    Id = "caption_wrong_setting",
                    Text = "She hides the storybook before the children gather.",
                    IsCorrect = false
                }
            };
        }

        public static IReadOnlyList<EventCard> GetArea2EventCards()
        {
            return new[]
            {
                new EventCard
                {
                    Id = MissionContentIds.EventSequenceCardIds[0],
                    Text = "The children gather at the acacia tree.",
                    CorrectSlotIndex = 0
                },
                new EventCard
                {
                    Id = MissionContentIds.EventSequenceCardIds[1],
                    Text = "Farmer Lira opens the damaged storybook.",
                    CorrectSlotIndex = 1
                },
                new EventCard
                {
                    Id = MissionContentIds.EventSequenceCardIds[2],
                    Text = "The Pathfinder repairs the missing opening caption.",
                    CorrectSlotIndex = 2
                }
            };
        }

        public static readonly string[] EventSlotLabels =
        {
            "Beginning",
            "Middle",
            "End"
        };
    }
}
