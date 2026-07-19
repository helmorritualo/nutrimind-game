# Hybrid UI Architecture

## Application scenes

Use UI Toolkit for authentication, home, subject/term/mission browsing, profile, settings, progress, rewards, certificates, announcements, leaderboards, and Quiz Portal application screens.

## Gameplay scenes

Gameplay scenes deliberately use both UI Toolkit and uGUI.

### UI Toolkit — complex/data-heavy screen-space panels

Use `UIDocument` and gameplay-specific `PanelSettings` for:

- mission introduction and objective details;
- dialogue, reading, image/text evidence, and learning-clue panels;
- question-and-answer panels, including long choices and multiple-answer state;
- hint, explanation, and review panels;
- Science Journal and detailed Wellness Guide views;
- mission learning summary and data-rich results.

UI Toolkit presenters consume pure gameplay view models. Use UXML/USS, focus management, responsive layouts, and virtualized lists when content is long.

### uGUI screen-space Canvas — immediate gameplay HUD and feedback

Use uGUI for:

- area/collectible HUD;
- compact objective tracker;
- interaction prompt;
- reticle and controller hints;
- answer feedback toast where no full panel is needed;
- collectible reveal, checkpoint toast, pause, loading/transition, and urgent system overlay;
- animation-heavy moment-to-moment feedback.

### uGUI world-space Canvas — in-world UI

Use world-space Canvas for:

- NPC, object, station, and path markers;
- interaction anchors;
- locked/unlocked indicators;
- short NPC status or progress indicators;
- collectible labels.

Do not place the full question panel or long reading content in world space.

## Scene hierarchy

```text
_GAMEPLAY_UI
├── UITK_GameplayPanels
│   ├── UIDocument_GameplayModal
│   └── GameplayPanelSettings
├── Canvas_HUD                 screen space, order 0
├── Canvas_Feedback            screen space, order 100
├── Canvas_System              screen space, order 500
├── Canvas_Transition          screen space, order 1000
└── WorldSpaceUI
    ├── NPCMarkers
    ├── ObjectMarkers
    ├── PathMarkers
    └── InteractionAnchors
```

## Coordination

Use one `GameplayUiCoordinator` as the sole modal authority.

It must:

- open only one blocking modal stack at a time;
- gate player and camera input while UI Toolkit or blocking uGUI panels are open;
- coordinate focus between UI Toolkit and the Input System UI module;
- prevent pointer/raycast fall-through;
- restore the prior input map and focus on close;
- expose framework-neutral `IGameplayPanel`, `IGameplayHud`, `IWorldMarker`, and `IInputGate` interfaces.

Do not make UI Toolkit presenters reference scene GameObjects directly. Do not make uGUI HUD scripts own mission state or Student API DTOs.
