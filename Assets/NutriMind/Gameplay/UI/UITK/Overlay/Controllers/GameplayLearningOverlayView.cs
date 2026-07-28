using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace NutriMind.Gameplay.UI
{
    public sealed class GameplayLearningOverlayView
    {
        private const string HiddenClass = "gameplay-learning-overlay--hidden";
        private const string SpeakerHiddenClass = "gameplay-learning-overlay__speaker--hidden";
        private const string EyebrowHiddenClass = "gameplay-learning-overlay__eyebrow--hidden";
        private const string OptionsHiddenClass = "gameplay-learning-overlay__options-host--hidden";
        private const string SequenceHiddenClass = "gameplay-learning-overlay__sequence-panel--hidden";
        private const string ResetHiddenClass = "gameplay-learning-overlay__reset-button--hidden";
        private const string SecondaryHiddenClass = "gameplay-learning-overlay__secondary-button--hidden";
        private const string ConfirmHiddenClass = "gameplay-learning-overlay__confirm-button--hidden";
        private const string CardHintClass = "gameplay-learning-overlay__card--hint";
        private const string CardCorrectClass = "gameplay-learning-overlay__card--correct";
        private const string CardReviewClass = "gameplay-learning-overlay__card--review";

        private static readonly string[] OptionLetters = { "A", "B", "C", "D", "E", "F" };

        private readonly VisualElement _root;
        private readonly VisualElement _card;
        private readonly Label _eyebrow;
        private readonly Label _title;
        private readonly Label _speaker;
        private readonly Label _body;
        private readonly VisualElement _optionsHost;
        private readonly ScrollView _optionsScroll;
        private readonly VisualElement _optionsList;
        private readonly VisualElement _sequencePanel;
        private readonly ScrollView _sequenceScroll;
        private readonly VisualElement _sequenceCards;
        private readonly VisualElement _sequenceSlots;
        private readonly Label _sequenceHint;
        private readonly Button _primaryButton;
        private readonly Button _secondaryButton;
        private readonly Button _confirmButton;
        private readonly Button _resetButton;

        public event Action PrimaryActionRequested;
        public event Action SecondaryActionRequested;
        public event Action ConfirmActionRequested;
        public event Action ResetActionRequested;
        public event Action<int> OptionSelected;
        public event Action<int> SequenceCardSelected;
        public event Action<int> SequenceSlotSelected;

        public GameplayLearningOverlayView(VisualElement root)
        {
            _root = root.Q<VisualElement>("gameplay-learning-overlay-root") ?? root;
            _card = _root.Q<VisualElement>("overlay-card");
            _eyebrow = _root.Q<Label>("overlay-eyebrow");
            _title = _root.Q<Label>("overlay-title");
            _speaker = _root.Q<Label>("overlay-speaker");
            _body = _root.Q<Label>("overlay-body");
            _optionsHost = _root.Q<VisualElement>("overlay-options-host");
            _optionsScroll = _root.Q<ScrollView>("overlay-options-scroll");
            _optionsList = _root.Q<VisualElement>("overlay-options-list");
            _sequencePanel = _root.Q<VisualElement>("overlay-sequence-panel");
            _sequenceScroll = _root.Q<ScrollView>("overlay-sequence-scroll");
            _sequenceCards = _root.Q<VisualElement>("overlay-sequence-cards");
            _sequenceSlots = _root.Q<VisualElement>("overlay-sequence-slots");
            _sequenceHint = _root.Q<Label>("overlay-sequence-hint");
            _primaryButton = _root.Q<Button>("overlay-primary-button");
            _secondaryButton = _root.Q<Button>("overlay-secondary-button");
            _confirmButton = _root.Q<Button>("overlay-confirm-button");
            _resetButton = _root.Q<Button>("overlay-reset-button");

            if (_optionsScroll != null)
            {
                _optionsScroll.mouseWheelScrollSize = 48f;
                _optionsScroll.touchScrollBehavior = ScrollView.TouchScrollBehavior.Clamped;
                _optionsScroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            }

            if (_sequenceScroll != null)
            {
                _sequenceScroll.mouseWheelScrollSize = 48f;
                _sequenceScroll.touchScrollBehavior = ScrollView.TouchScrollBehavior.Clamped;
                _sequenceScroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            }

            _primaryButton?.RegisterCallback<ClickEvent>(_ => PrimaryActionRequested?.Invoke());
            _secondaryButton?.RegisterCallback<ClickEvent>(_ => SecondaryActionRequested?.Invoke());
            _confirmButton?.RegisterCallback<ClickEvent>(_ => ConfirmActionRequested?.Invoke());
            _resetButton?.RegisterCallback<ClickEvent>(_ => ResetActionRequested?.Invoke());
        }

        public void SetViewModel(GameplayLearningOverlayViewModel model)
        {
            if (model == null)
            {
                Hide();
                return;
            }

            bool visible = model.State != GameplayLearningOverlayState.Hidden;
            _root.EnableInClassList(HiddenClass, !visible);
            if (!visible)
            {
                return;
            }

            ApplyCardStateClass(model.State);

            string eyebrow = GetEyebrow(model.State);
            if (_eyebrow != null)
            {
                bool showEyebrow = !string.IsNullOrWhiteSpace(eyebrow);
                _eyebrow.EnableInClassList(EyebrowHiddenClass, !showEyebrow);
                _eyebrow.text = eyebrow;
            }

            if (_title != null)
            {
                _title.text = model.Title ?? string.Empty;
            }

            if (_speaker != null)
            {
                bool showSpeaker = !string.IsNullOrWhiteSpace(model.Speaker);
                _speaker.EnableInClassList(SpeakerHiddenClass, !showSpeaker);
                _speaker.text = model.Speaker ?? string.Empty;
            }

            if (_body != null)
            {
                bool showSequenceBody = model.State == GameplayLearningOverlayState.EventSequence;
                // Sequence mode already shows guidance in the sequence hint — avoid duplicate/overlapping body text.
                bool showBody = !string.IsNullOrWhiteSpace(model.Body) && !showSequenceBody;
                _body.style.display = showBody ? DisplayStyle.Flex : DisplayStyle.None;
                _body.text = model.Body ?? string.Empty;
            }

            bool showOptions = model.State == GameplayLearningOverlayState.Question
                || model.State == GameplayLearningOverlayState.CaptionSelection;
            _optionsHost?.EnableInClassList(OptionsHiddenClass, !showOptions);
            RebuildOptions(model, showOptions);

            bool showSequence = model.State == GameplayLearningOverlayState.EventSequence;
            _sequencePanel?.EnableInClassList(SequenceHiddenClass, !showSequence);
            RebuildSequence(model, showSequence);

            if (_sequenceHint != null)
            {
                _sequenceHint.text = showSequence ? model.Body ?? string.Empty : string.Empty;
                _sequenceHint.style.display = showSequence && !string.IsNullOrWhiteSpace(model.Body)
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
            }

            if (_primaryButton != null)
            {
                _primaryButton.text = model.PrimaryActionLabel ?? "Continue";
                _primaryButton.style.display = showOptions || showSequence ? DisplayStyle.None : DisplayStyle.Flex;
            }

            _resetButton?.EnableInClassList(ResetHiddenClass, !model.ShowResetAction);
            _secondaryButton?.EnableInClassList(SecondaryHiddenClass, !model.ShowSecondaryAction);
            if (_secondaryButton != null && model.ShowSecondaryAction)
            {
                _secondaryButton.text = model.SecondaryActionLabel ?? string.Empty;
            }

            _confirmButton?.EnableInClassList(ConfirmHiddenClass, !model.ShowConfirmAction);
            if (_confirmButton != null)
            {
                _confirmButton.SetEnabled(model.ConfirmEnabled);
            }
        }

        public void Hide()
        {
            _root.EnableInClassList(HiddenClass, true);
        }

        public void Dispose()
        {
            PrimaryActionRequested = null;
            SecondaryActionRequested = null;
            ConfirmActionRequested = null;
            ResetActionRequested = null;
            OptionSelected = null;
            SequenceCardSelected = null;
            SequenceSlotSelected = null;
            _optionsList?.Clear();
            _sequenceCards?.Clear();
            _sequenceSlots?.Clear();
        }

        private void RebuildOptions(GameplayLearningOverlayViewModel model, bool showOptions)
        {
            if (_optionsList == null)
            {
                return;
            }

            _optionsList.Clear();
            if (!showOptions || model.OptionLabels == null)
            {
                return;
            }

            for (int i = 0; i < model.OptionLabels.Length; i++)
            {
                int index = i;
                var button = new Button
                {
                    name = "overlay-option-" + index,
                    focusable = true,
                    pickingMode = PickingMode.Position
                };
                button.AddToClassList("gameplay-learning-overlay__option-button");

                var letter = new Label(GetOptionLetter(index))
                {
                    pickingMode = PickingMode.Ignore
                };
                letter.AddToClassList("gameplay-learning-overlay__option-letter");

                var label = new Label(model.OptionLabels[index] ?? string.Empty)
                {
                    pickingMode = PickingMode.Ignore
                };
                label.AddToClassList("gameplay-learning-overlay__option-label");

                button.Add(letter);
                button.Add(label);
                RegisterOptionActivation(button, index);
                _optionsList.Add(button);
            }
        }

        private void RebuildSequence(GameplayLearningOverlayViewModel model, bool showSequence)
        {
            if (_sequenceCards == null || _sequenceSlots == null)
            {
                return;
            }

            _sequenceCards.Clear();
            _sequenceSlots.Clear();
            if (!showSequence)
            {
                return;
            }

            if (model.OptionLabels != null)
            {
                for (int i = 0; i < model.OptionLabels.Length; i++)
                {
                    int index = i;
                    var card = new Button
                    {
                        text = string.Empty,
                        pickingMode = PickingMode.Position
                    };
                    card.AddToClassList("gameplay-learning-overlay__sequence-card");
                    if (!string.IsNullOrEmpty(model.SelectedCardLabel)
                        && model.SelectedCardLabel == model.OptionLabels[index])
                    {
                        card.AddToClassList("gameplay-learning-overlay__sequence-card--selected");
                    }

                    var cardLabel = new Label(model.OptionLabels[index] ?? string.Empty)
                    {
                        pickingMode = PickingMode.Ignore
                    };
                    cardLabel.AddToClassList("gameplay-learning-overlay__sequence-card-label");
                    card.Add(cardLabel);

                    RegisterSimpleActivation(card, () => SequenceCardSelected?.Invoke(index));
                    _sequenceCards.Add(card);
                }
            }

            if (model.SlotLabels != null)
            {
                for (int i = 0; i < model.SlotLabels.Length; i++)
                {
                    int index = i;
                    string slotValue = model.SlotValues != null && i < model.SlotValues.Length
                        ? model.SlotValues[i]
                        : string.Empty;
                    bool filled = !string.IsNullOrEmpty(slotValue);

                    var slot = new Button
                    {
                        text = string.Empty,
                        pickingMode = PickingMode.Position
                    };
                    slot.AddToClassList("gameplay-learning-overlay__sequence-slot");
                    if (filled)
                    {
                        slot.AddToClassList("gameplay-learning-overlay__sequence-slot--filled");
                    }

                    var slotTitle = new Label(model.SlotLabels[index] ?? string.Empty)
                    {
                        pickingMode = PickingMode.Ignore
                    };
                    slotTitle.AddToClassList("gameplay-learning-overlay__sequence-slot-title");

                    var slotBody = new Label(filled ? slotValue : "Tap to place selected card")
                    {
                        pickingMode = PickingMode.Ignore
                    };
                    slotBody.AddToClassList("gameplay-learning-overlay__sequence-slot-body");
                    slotBody.EnableInClassList("gameplay-learning-overlay__sequence-slot-body--empty", !filled);

                    slot.Add(slotTitle);
                    slot.Add(slotBody);

                    RegisterSimpleActivation(slot, () => SequenceSlotSelected?.Invoke(index));
                    _sequenceSlots.Add(slot);
                }
            }
        }

        private void RegisterOptionActivation(Button button, int index)
        {
            RegisterReliableActivation(button, () => OptionSelected?.Invoke(index));
        }

        private static void RegisterSimpleActivation(Button button, Action action)
        {
            RegisterReliableActivation(button, action);
        }

        private static void RegisterReliableActivation(Button button, Action action)
        {
            // Prevent parent ScrollView from capturing the press gesture so the
            // button still receives a completed click on touch devices.
            button.RegisterCallback<PointerDownEvent>(
                evt => evt.StopPropagation(),
                TrickleDown.TrickleDown);

            button.RegisterCallback<ClickEvent>(evt =>
            {
                action?.Invoke();
                evt.StopPropagation();
            });
        }

        private void ApplyCardStateClass(GameplayLearningOverlayState state)
        {
            if (_card == null)
            {
                return;
            }

            _card.EnableInClassList(CardHintClass, state == GameplayLearningOverlayState.FirstWrongHint);
            _card.EnableInClassList(CardCorrectClass, state == GameplayLearningOverlayState.CorrectAcknowledgement);
            _card.EnableInClassList(CardReviewClass, state == GameplayLearningOverlayState.SecondWrongExplanation);
        }

        private static string GetEyebrow(GameplayLearningOverlayState state)
        {
            switch (state)
            {
                case GameplayLearningOverlayState.Dialogue:
                    return "Conversation";
                case GameplayLearningOverlayState.Evidence:
                    return "Clue Found";
                case GameplayLearningOverlayState.Question:
                    return "Question";
                case GameplayLearningOverlayState.FirstWrongHint:
                    return "Hint";
                case GameplayLearningOverlayState.SecondWrongExplanation:
                    return "Review";
                case GameplayLearningOverlayState.CorrectAcknowledgement:
                    return "Correct";
                case GameplayLearningOverlayState.CaptionSelection:
                    return "Caption Repair";
                case GameplayLearningOverlayState.EventSequence:
                    return "Event Sequence";
                default:
                    return string.Empty;
            }
        }

        private static string GetOptionLetter(int index)
        {
            if (index < 0 || index >= OptionLetters.Length)
            {
                return (index + 1).ToString();
            }

            return OptionLetters[index];
        }
    }
}
