# UI Toolkit Design System Setup

Use the pinned Unity 6-compatible design-system package through NutriMind-owned adapters and theme files. Do not edit package assets directly.

Recommended gameplay assets:

```text
PS_GameplayOverlay.asset
UIDocument_GameplayOverlay
NutriMindGameplayOverlay.uxml
NutriMindGameplayOverlay.uss
```

The gameplay overlay supports only:

- mission introduction;
- evidence/reading state;
- question state;
- first-wrong hint state;
- second-wrong explanation/acknowledgement state;
- mission-complete result;
- optional exit confirmation and pause.

Do not create separate UI Toolkit documents for learning clue, reminder, review, all-correct, investigation completion, healthy-choice completion, area completion, or learning summary. Those states are either handled by the reusable overlay, represented by non-modal uGUI feedback, or included in the mission-complete result.

Presenters bind pure gameplay view models and never own mission state. Validate keyboard, gamepad, mouse, and touch focus with the Input System and one `GameplayUiCoordinator`.
