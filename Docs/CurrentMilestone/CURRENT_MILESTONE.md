# Current Unity Milestone

## Scope — unchanged

Deliver:

1. complete application scenes and routing;
2. complete Quiz Portal application flow;
3. reusable static gameplay content loader, validator, question engine, and SQLite progress/outbox foundation;
4. complete playable Grade 5 LiteraQuest Term 1 Mission 1;
5. content/data foundations and implementation-ready overviews only for Grade 5 PE & Health Term 1 Mission 1 and Grade 5 Science Term 1 Mission 1.

Do not build all 90 mission scenes in this milestone. The 90 JSON packs are the canonical future content definitions, not a scene-production commitment.

## Required gameplay UI slice

The LiteraQuest scene must prove:

- UI Toolkit mission introduction, dialogue/reading, question, hint/review, and summary panels;
- uGUI screen-space HUD, objective, feedback, collectible, checkpoint, pause, and transition UI;
- uGUI world-space NPC/object/path markers;
- one modal/input coordinator across both UI systems.

## Required gameplay/server slice

- local static content and local scoring;
- SQLite local progress and duplicate-safe outbox;
- server mission availability and classroom lock reason display;
- idempotent progress synchronization;
- no static dialogue or answer key in server DTOs.

## Acceptance mission

```text
g5_lq_t1_m01
SCN_G5_LQ_T1_M01_TheFestivalStorybookRescue
```

PE & Health and Science Mission 1 remain scene-deferred.
