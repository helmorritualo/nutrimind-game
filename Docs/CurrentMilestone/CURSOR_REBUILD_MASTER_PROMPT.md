# Cursor Master Prompt — First Unity Rebuild Milestone

Implement the current milestone from a fresh Unity repository.

## Read first

```text
AGENTS.md
Docs/UNITY_REQUIREMENTS.md
Docs/Architecture/*
Docs/CurrentMilestone/*
Docs/Data/GAMEPLAY_CONTENT_MANIFEST_V5.json
Docs/Data/GAMEPLAY_CONTENT_CATALOG_V1.json
Docs/Data/StaticGameplayContent/*.json
Docs/Shared/API/openapi.yaml
.cursor/rules/*
```

## Preflight

Inspect and report:

- Unity version;
- render pipeline;
- Input System;
- Cinemachine;
- UI Toolkit;
- `com.sinanata.designsystem`;
- uGUI/TextMeshPro;
- target platforms;
- package lock;
- current folders/assemblies;
- available environment/player assets.

Do not add packages until compatibility is proven.

## Foundation

Create the approved folder and assembly architecture.

Create clear composition roots for:

- application session;
- networking;
- secure token;
- local persistence;
- synchronization;
- application routing;
- mission runtime.

## Application UI

Create four application scenes and all specified screens using UI Toolkit.

Integrate the design system through NutriMind-owned theme overrides and UXML templates.

Every screen must implement loading/content/empty/offline/error/locked states.

## Quiz Portal

Implement list, detail, attempt, submission, result, history, and duplicate recovery.

Use exact Student API DTOs.

Never expose answer keys before submission.

## Static question engine

Implement reusable local question logic with UI Toolkit question/review presenters and uGUI feedback/HUD presenters.

Support all three question types and the two-attempt policy.

## Mission framework

Implement one-scene, three-area mission flow with:

- objectives;
- guidance;
- interactions;
- checkpoints;
- world unlocks;
- collectibles;
- persistence/outbox;
- restore;
- mission completion.

## Complete mission

Implement:

```text
g5_lq_t1_m01
SCN_G5_LQ_T1_M01_TheFestivalStorybookRescue
```

Use the full design document.

## Overview foundations

Create validated data definitions and placeholder scene/content plans only for:

```text
g5_peh_t1_m01
g5_sci_t1_m01
```

Do not implement their full scenes.

## Testing

Add EditMode, PlayMode, API-fixture, UI presenter, persistence, replay, and mission-flow tests.

No live production API calls.

## Completion report

Report:

- versions and package evidence;
- files/folders/assemblies;
- scenes and routes;
- UXML/USS and design-system boundary;
- UI Toolkit gameplay panels and uGUI screen/world-space prefabs;
- question systems;
- mission state machines;
- local persistence/outbox;
- API routes/DTOs;
- tests and exact results;
- missing human art/content/Inspector work;
- blockers and unverified platform behavior.
