using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace NutriMind.Gameplay.UI
{
    public sealed class GameplayLearningOverlayView
    {
        private const string HiddenClass = "gameplay-learning-overlay--hidden";
        private const string SpeakerHiddenClass = "gameplay-learning-overlay__speaker--hidden";
        private const string OptionsHiddenClass = "gameplay-learning-overlay__options-scroll--hidden";
        private const string SequenceHiddenClass = "gameplay-learning-overlay__sequence-panel--hidden";
        private const string ResetHiddenClass = "gameplay-learning-overlay__reset-button--hidden";
        private const string SecondaryHiddenClass = "gameplay-learning-overlay__secondary-button--hidden";
        private const string ConfirmHiddenClass = "gameplay-learning-overlay__confirm-button--hidden";

        private readonly VisualElement _root;
        private readonly Label _title;
        private readonly Label _speaker;
        private readonly Label _body;
        private readonly ScrollView _optionsScroll;
        private readonly VisualElement _optionsList;
        private readonly VisualElement _sequencePanel;
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
            _title = _root.Q<Label>("overlay-title");
            _speaker = _root.Q<Label>("overlay-speaker");
            _body = _root.Q<Label>("overlay-body");
            _optionsScroll = _root.Q<ScrollView>("overlay-options-scroll");
            _optionsList = _root.Q<VisualElement>("overlay-options-list");
            _sequencePanel = _root.Q<VisualElement>("overlay-sequence-panel");
            _sequenceCards = _root.Q<VisualElement>("overlay-sequence-cards");
            _sequenceSlots = _root.Q<VisualElement>("overlay-sequence-slots");
            _sequenceHint = _root.Q<Label>("overlay-sequence-hint");
            _primaryButton = _root.Q<Button>("overlay-primary-button");
            _secondaryButton = _root.Q<Button>("overlay-secondary-button");
            _confirmButton = _root.Q<Button>("overlay-confirm-button");
            _resetButton = _root.Q<Button>("overlay-reset-button");

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
                _body.text = model.Body ?? string.Empty;
            }

            bool showOptions = model.State == GameplayLearningOverlayState.Question
                || model.State == GameplayLearningOverlayState.CaptionSelection;
            _optionsScroll?.EnableInClassList(OptionsHiddenClass, !showOptions);
            RebuildOptions(model, showOptions);

            bool showSequence = model.State == GameplayLearningOverlayState.EventSequence;
            _sequencePanel?.EnableInClassList(SequenceHiddenClass, !showSequence);
            RebuildSequence(model, showSequence);

            if (_sequenceHint != null)
            {
                _sequenceHint.text = showSequence ? model.Body ?? string.Empty : string.Empty;
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
                var button = new Button(() => OptionSelected?.Invoke(index))
                {
                    text = model.OptionLabels[index] ?? string.Empty
                };
                button.AddToClassList("ds-button");
                button.AddToClassList("ds-button--secondary");
                button.AddToClassList("gameplay-learning-overlay__option-button");
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
                    var card = new Button(() => SequenceCardSelected?.Invoke(index))
                    {
                        text = model.OptionLabels[index] ?? string.Empty
                    };
                    card.AddToClassList("gameplay-learning-overlay__sequence-card");
                    if (!string.IsNullOrEmpty(model.SelectedCardLabel)
                        && model.SelectedCardLabel == model.OptionLabels[index])
                    {
                        card.AddToClassList("gameplay-learning-overlay__sequence-card--selected");
                    }

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
                    var slot = new Button(() => SequenceSlotSelected?.Invoke(index))
                    {
                        text = (model.SlotLabels[index] ?? string.Empty) + "\n" + slotValue
                    };
                    slot.AddToClassList("gameplay-learning-overlay__sequence-slot");
                    if (!string.IsNullOrEmpty(slotValue))
                    {
                        slot.AddToClassList("gameplay-learning-overlay__sequence-slot--filled");
                    }

                    _sequenceSlots.Add(slot);
                }
            }
        }
    }
}
