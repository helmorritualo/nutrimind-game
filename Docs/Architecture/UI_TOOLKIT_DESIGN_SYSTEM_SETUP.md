# UI Toolkit Design-System Setup

Use one pinned Unity 6-compatible UI Toolkit design-system version behind NutriMind-owned UXML/USS and presenter abstractions.

## Panel settings

Create separate assets for application UI and gameplay data-heavy panels:

```text
PS_App.asset
PS_GameplayModal.asset
```

Gameplay modal panels include mission introduction, dialogue/reading, question/answer, hint/review, detailed journal/guide, and learning summary. They must not own mission state; presenters bind view models from the gameplay application layer.

Keep uGUI HUD and world-space Canvas styling in separate prefabs. Shared visual tokens may be mirrored intentionally, but do not create direct component dependencies between UI Toolkit and uGUI.

Validate keyboard, gamepad, mouse, and touch focus with the chosen Input System and one gameplay modal coordinator.
