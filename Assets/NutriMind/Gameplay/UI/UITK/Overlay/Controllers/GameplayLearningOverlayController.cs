using System;
using System.Collections.Generic;
using NutriMind.Gameplay.Runtime;
using UnityEngine;
using UnityEngine.UIElements;

namespace NutriMind.Gameplay.UI
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class GameplayLearningOverlayController : MonoBehaviour
    {
        [SerializeField] private UIDocument _uiDocument;

        private GameplayLearningOverlayView _view;
        private GameplayLearningOverlayViewModel _currentModel = new GameplayLearningOverlayViewModel();
        private readonly List<string> _sequenceSlotAssignments = new List<string> { string.Empty, string.Empty, string.Empty };
        private string _selectedSequenceCardId = string.Empty;
        private string _sequenceHint = string.Empty;

        public event Action OverlayOpened;
        public event Action OverlayClosed;

        private void OnEnable()
        {
            Bind();
            Hide();
        }

        private void OnDisable()
        {
            Unbind();
        }

        private void Bind()
        {
            if (_view != null)
            {
                return;
            }

            if (_uiDocument == null)
            {
                _uiDocument = GetComponent<UIDocument>();
            }

            if (_uiDocument?.rootVisualElement == null)
            {
                return;
            }

            _view = new GameplayLearningOverlayView(_uiDocument.rootVisualElement);
            _view.PrimaryActionRequested += OnPrimaryActionRequested;
            _view.OptionSelected += OnOptionSelected;
            _view.SequenceCardSelected += OnSequenceCardSelected;
            _view.SequenceSlotSelected += OnSequenceSlotSelected;
            _view.ConfirmActionRequested += OnConfirmActionRequested;
            _view.ResetActionRequested += OnResetActionRequested;
        }

        private void Unbind()
        {
            if (_view == null)
            {
                return;
            }

            _view.PrimaryActionRequested -= OnPrimaryActionRequested;
            _view.OptionSelected -= OnOptionSelected;
            _view.SequenceCardSelected -= OnSequenceCardSelected;
            _view.SequenceSlotSelected -= OnSequenceSlotSelected;
            _view.ConfirmActionRequested -= OnConfirmActionRequested;
            _view.ResetActionRequested -= OnResetActionRequested;
            _view = null;
        }

        public void Hide()
        {
            _currentModel.State = GameplayLearningOverlayState.Hidden;
            _view?.SetViewModel(_currentModel);
            OverlayClosed?.Invoke();
        }

        public void ShowDialogue(string speaker, string body, Action onContinue)
        {
            _continueCallback = onContinue;
            _currentModel = new GameplayLearningOverlayViewModel
            {
                State = GameplayLearningOverlayState.Dialogue,
                Speaker = speaker,
                Body = body,
                PrimaryActionLabel = "Continue"
            };
            ApplyAndOpen();
        }

        public void ShowEvidence(string title, string body, Action onContinue)
        {
            _continueCallback = onContinue;
            _currentModel = new GameplayLearningOverlayViewModel
            {
                State = GameplayLearningOverlayState.Evidence,
                Title = title,
                Body = body,
                PrimaryActionLabel = "Continue"
            };
            ApplyAndOpen();
        }

        public void ShowQuestion(MissionQuestionDto question, Action<string> onOptionChosen)
        {
            _optionCallback = onOptionChosen;
            _currentModel = new GameplayLearningOverlayViewModel
            {
                State = GameplayLearningOverlayState.Question,
                Title = question.prompt,
                Body = string.Empty,
                OptionLabels = ExtractOptionLabels(question),
                OptionIds = ExtractOptionIds(question)
            };
            ApplyAndOpen();
        }

        public void ShowHint(string title, string body, Action onContinue)
        {
            _continueCallback = onContinue;
            _currentModel = new GameplayLearningOverlayViewModel
            {
                State = GameplayLearningOverlayState.FirstWrongHint,
                Title = title,
                Body = body,
                PrimaryActionLabel = "Try Again"
            };
            ApplyAndOpen();
        }

        public void ShowExplanation(string title, string body, Action onContinue)
        {
            _continueCallback = onContinue;
            _currentModel = new GameplayLearningOverlayViewModel
            {
                State = GameplayLearningOverlayState.SecondWrongExplanation,
                Title = title,
                Body = body,
                PrimaryActionLabel = "Continue"
            };
            ApplyAndOpen();
        }

        public void ShowCorrectAcknowledgement(string body, Action onContinue)
        {
            _continueCallback = onContinue;
            _currentModel = new GameplayLearningOverlayViewModel
            {
                State = GameplayLearningOverlayState.CorrectAcknowledgement,
                Title = "Correct",
                Body = body,
                PrimaryActionLabel = "Continue"
            };
            ApplyAndOpen();
        }

        public void ShowCaptionSelection(IReadOnlyList<MissionWorldActionContent.CaptionOption> options, Action<string> onOptionChosen)
        {
            _optionCallback = onOptionChosen;
            var labels = new string[options.Count];
            var ids = new string[options.Count];
            for (int i = 0; i < options.Count; i++)
            {
                labels[i] = options[i].Text;
                ids[i] = options[i].Id;
            }

            _currentModel = new GameplayLearningOverlayViewModel
            {
                State = GameplayLearningOverlayState.CaptionSelection,
                Title = "Repair the missing caption",
                Body = "Choose the caption where “They” clearly refers to the children.",
                OptionLabels = labels,
                OptionIds = ids
            };
            ApplyAndOpen();
        }

        public void ShowEventSequence(IReadOnlyList<MissionWorldActionContent.EventCard> cards, Action<bool> onConfirmed)
        {
            _sequenceConfirmCallback = onConfirmed;
            ResetSequenceState();
            var labels = new string[cards.Count];
            for (int i = 0; i < cards.Count; i++)
            {
                labels[i] = cards[i].Text;
            }

            _currentModel = new GameplayLearningOverlayViewModel
            {
                State = GameplayLearningOverlayState.EventSequence,
                Title = "Arrange the events",
                Body = _sequenceHint,
                OptionLabels = labels,
                SlotLabels = MissionWorldActionContent.EventSlotLabels,
                SlotValues = _sequenceSlotAssignments.ToArray(),
                SelectedCardLabel = string.Empty,
                ShowResetAction = true,
                ShowConfirmAction = true,
                ConfirmEnabled = false
            };
            ApplyAndOpen();
        }

        private Action _continueCallback;
        private Action<string> _optionCallback;
        private Action<bool> _sequenceConfirmCallback;

        private void ApplyAndOpen()
        {
            Bind();
            _view?.SetViewModel(_currentModel);
            OverlayOpened?.Invoke();
        }

        private void OnPrimaryActionRequested()
        {
            Action callback = _continueCallback;
            _continueCallback = null;
            Hide();
            callback?.Invoke();
        }

        private void OnOptionSelected(int index)
        {
            if (_currentModel.OptionIds == null || index < 0 || index >= _currentModel.OptionIds.Length)
            {
                return;
            }

            string optionId = _currentModel.OptionIds[index];
            Action<string> callback = _optionCallback;
            _optionCallback = null;
            Hide();
            callback?.Invoke(optionId);
        }

        private void OnSequenceCardSelected(int index)
        {
            if (_currentModel.OptionLabels == null || index < 0 || index >= _currentModel.OptionLabels.Length)
            {
                return;
            }

            _selectedSequenceCardId = MissionContentIds.EventSequenceCardIds[index];
            _currentModel.SelectedCardLabel = _currentModel.OptionLabels[index];
            RefreshSequenceModel();
        }

        private void OnSequenceSlotSelected(int slotIndex)
        {
            if (string.IsNullOrEmpty(_selectedSequenceCardId) || slotIndex < 0 || slotIndex >= _sequenceSlotAssignments.Count)
            {
                return;
            }

            for (int i = 0; i < _sequenceSlotAssignments.Count; i++)
            {
                if (_sequenceSlotAssignments[i] == _selectedSequenceCardId)
                {
                    _sequenceSlotAssignments[i] = string.Empty;
                }
            }

            _sequenceSlotAssignments[slotIndex] = _selectedSequenceCardId;
            _selectedSequenceCardId = string.Empty;
            _currentModel.SelectedCardLabel = string.Empty;
            RefreshSequenceModel();
        }

        private void OnResetActionRequested()
        {
            ResetSequenceState();
            _sequenceHint = string.Empty;
            RefreshSequenceModel();
        }

        private void OnConfirmActionRequested()
        {
            bool correct = EventSequenceValidator.IsCorrectOrder(_sequenceSlotAssignments);
            if (!correct)
            {
                _sequenceHint = "That order is not correct. Try again.";
                RefreshSequenceModel();
                return;
            }

            Action<bool> callback = _sequenceConfirmCallback;
            _sequenceConfirmCallback = null;
            Hide();
            callback?.Invoke(true);
        }

        private void ResetSequenceState()
        {
            _selectedSequenceCardId = string.Empty;
            for (int i = 0; i < _sequenceSlotAssignments.Count; i++)
            {
                _sequenceSlotAssignments[i] = string.Empty;
            }
        }

        private void RefreshSequenceModel()
        {
            _currentModel.Body = _sequenceHint;
            _currentModel.SlotValues = _sequenceSlotAssignments.ToArray();
            _currentModel.ConfirmEnabled = EventSequenceValidator.CanConfirm(_sequenceSlotAssignments);
            _view?.SetViewModel(_currentModel);
        }

        private static string[] ExtractOptionLabels(MissionQuestionDto question)
        {
            var labels = new string[question.options.Length];
            for (int i = 0; i < question.options.Length; i++)
            {
                labels[i] = question.options[i].text;
            }

            return labels;
        }

        private static string[] ExtractOptionIds(MissionQuestionDto question)
        {
            var ids = new string[question.options.Length];
            for (int i = 0; i < question.options.Length; i++)
            {
                ids[i] = question.options[i].id;
            }

            return ids;
        }
    }
}
