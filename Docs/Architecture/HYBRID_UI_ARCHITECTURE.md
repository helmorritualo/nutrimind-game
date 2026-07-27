# Hybrid UI Architecture

## Application scenes

Use UI Toolkit for authentication, home, subject/term/mission browsing, profile, settings, progress, rewards, certificates, announcements, leaderboards, and Quiz Portal application screens.

## Gameplay scenes

Gameplay scenes use UI Toolkit only for complex blocking overlays and uGUI for moment-to-moment gameplay UI.

### UI Toolkit — limited blocking overlays

Use one gameplay `UIDocument` and gameplay-specific `PanelSettings` for:

- mission introduction;
- the reusable learning-and-question overlay;
- mission-complete result;
- optional exit confirmation;
- pause when it shares the same navigation/focus implementation.

The learning overlay has internal states for evidence/reading, question, first-wrong hint, second-wrong explanation, and acknowledgement. These are not separate modal stacks.

### uGUI screen-space Canvas — HUD, subtitles, and feedback

Use uGUI for:

- current objective and `x/3` collectible HUD;
- interaction prompt and controller hints;
- subtitles and short NPC status;
- concise correct-answer feedback;
- area-restored banner;
- collectible reveal and checkpoint toast;
- loading/transition and urgent system overlay;
- animation-heavy moment-to-moment feedback.

### uGUI world-space Canvas — in-world UI

Use world-space Canvas for NPC, object, station, path, collectible, and interaction markers. Do not place full questions or long reading content in world space.

## Scene hierarchy

```text
_GAMEPLAY_UI
├── UITK_GameplayOverlay
│   ├── UIDocument_GameplayOverlay
│   └── GameplayPanelSettings
├── Canvas_HUD
├── Canvas_Subtitles
├── Canvas_Feedback
├── Canvas_System
├── Canvas_Transition
└── WorldSpaceUI
    ├── NPCMarkers
    ├── ObjectMarkers
    ├── StationMarkers
    ├── PathMarkers
    └── InteractionAnchors
```

## Coordination

Use one `GameplayUiCoordinator` as the sole blocking-overlay authority. It opens only one blocking surface, gates player/camera input, coordinates focus, prevents raycast fall-through, and restores the prior input map on close.

Non-modal HUD, subtitles, prompts, and feedback must not unnecessarily block movement. UI presenters consume view models and do not own mission state or reference server DTOs directly.
