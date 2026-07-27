# NutriMind Unity Requirements — Fresh Rebuild

## 1. Authority

This document is the canonical product and engineering contract for the new Unity repository.

Read with:

```text
Docs/Shared/SHARED_PRODUCT_CONTRACT.md
Docs/Shared/THREE_AREA_GAMEPLAY_LOOP_CONTRACT.md
Docs/Shared/API/openapi.yaml
Docs/Data/GAMEPLAY_CONTENT_MANIFEST_V5.json
Docs/Data/GAMEPLAY_CONTENT_CATALOG_V1.json
Docs/Data/StaticGameplayContent/*.json
AGENTS.md
.cursor/rules/
```

The previous one-scene-per-area and five-area documents are superseded.

## 2. Product scope

The Unity Student application provides:

- Student authentication;
- application navigation;
- profile and settings;
- subject, term, and mission selection;
- server-managed Quiz Portal;
- static open-world educational missions;
- local-first progress;
- offline play after prior authentication;
- rewards, certificates, announcements, leaderboards, and progress views;
- synchronization.

Supported grades:

```text
grade_5
grade_6
```

Supported subjects:

```text
subject_literaquest
subject_pe_health
subject_science
```

## 3. Rebuild baseline

Use a fresh Unity 6 project because the selected UI Toolkit design system requires Unity 6 features.

The exact Unity editor version must be frozen in:

```text
ProjectSettings/ProjectVersion.txt
```

The exact packages must be frozen in:

```text
Packages/manifest.json
Packages/packages-lock.json
```

Do not upgrade Unity or packages during a milestone without a compatibility decision.

Required foundations:

- a supported render pipeline selected at project bootstrap;
- Input System;
- Cinemachine;
- UI Toolkit;
- Unity uGUI;
- TextMeshPro;
- a supported local SQLite solution selected only after target-platform validation;
- `com.sinanata.designsystem` or a pinned vendored equivalent for application UI.

The project must not depend directly on third-party UI classes outside a NutriMind-owned adapter/theme boundary.

## 4. Scene model

### Application scenes

Use UI Toolkit.

Recommended scenes:

```text
SCN_App_Bootstrap
SCN_App_Authentication
SCN_App_Main
SCN_App_QuizPortal
```

Application scenes use screen routing rather than one Unity scene per page.

### Gameplay scenes

Use one Unity scene per mission. Gameplay scenes use UI Toolkit for complex/data-heavy screen-space panels, uGUI screen-space Canvas for HUD and feedback, and uGUI world-space Canvas for in-world UI.

```text
90 missions
→ 90 mission scenes
```

Each mission scene contains exactly three logical open-world areas.

```text
Mission Scene
├── Area 1 — Discover
├── Area 2 — Apply
└── Area 3 — Master
```

There is no scene load between areas.

Area unlocks are represented through:

- gates;
- bridges;
- paths;
- cleared mist;
- restored machines;
- opened archive wings;
- world-state VFX;
- objective changes.

The mission scene loads once and exits only when the player leaves or completes the mission.

## 5. Three-area content and competency model

The original five-area plans are compressed into three substantial learning situations, not three areas filled with several miniature stations.

Every mission declares:

- one primary competency owned by the mission;
- at most one supporting competency;
- prerequisite and review competency IDs;
- one mastery-evidence statement;
- one principal mechanic family.

### Area 1 — Discover

Combines the introductory purpose of former Areas 1 and 2.

Required shape:

- one mission introduction;
- one primary NPC, station, or problem;
- two or three observable clues;
- one guided subject interaction;
- two or three scored checks by default, maximum four;
- one world action and visible result;
- first collectible;
- checkpoint;
- in-world unlock of Area 2.

### Area 2 — Apply

Combines the application purpose of former Areas 3 and 4.

Required shape:

- one related application or comparison situation;
- one less-guided subject interaction;
- two or three scored application checks by default, maximum four;
- inline review state only when required;
- one world action and visible result;
- second collectible;
- checkpoint;
- in-world unlock of Area 3.

Area 2 must apply the same primary competency. It must not introduce an unrelated lesson merely to fill the area.

### Area 3 — Master

Combines former Area 5 and the former final challenge.

Required shape:

- one integrated mastery situation;
- use of evidence or items from Areas 1 and 2;
- one mastery interaction;
- up to three mastery checks by default, maximum four;
- final subject action;
- third collectible;
- final world restoration;
- mission completion in the same transaction as Area 3 completion.

### Required optimization

Remove:

- repeated mission introductions and scene loads;
- repeated NPC explanation of the same premise;
- repeated tutorial, reminder, review, all-correct, area-complete, and learning-summary modals;
- one-question micro-stations;
- duplicate question wording or duplicate new-learning ownership;
- manually carried quest inventory when automatic station transfer is sufficient;
- a separate final-challenge loading sequence.

Preserve:

- required curriculum concepts;
- one clear subject-specific interaction per area;
- feedback and review state;
- world consequence;
- three progress checkpoints;
- three mission collectibles.

The complete behavioral contract is `Docs/Shared/THREE_AREA_GAMEPLAY_LOOP_CONTRACT.md`.

## 6. Mission scene hierarchy

```text
SCN_G<grade>_<subject>_T<term>_M<mission>_<MissionTitle>
├── _MISSION
│   ├── MissionIdentity
│   ├── MissionDefinitionBinding
│   ├── MissionRuntime
│   ├── MissionFlowController
│   ├── MissionWorldStateController
│   ├── MissionCheckpointController
│   ├── MissionCompletionController
│   └── MissionContentValidator
├── _PLAYER
│   ├── PlayerSpawn_MissionEntry
│   ├── PlayerCheckpointRouter
│   ├── PlayerRoot
│   └── CameraRig
├── _ENVIRONMENT
│   ├── SharedTerrainOrGround
│   ├── SharedArchitecture
│   ├── SharedVegetation
│   ├── SharedPaths
│   ├── DistantLandmarks
│   ├── BoundaryGeometry
│   ├── Lighting
│   ├── ReflectionAndLightProbes
│   └── Navigation
├── _AREAS
│   ├── ZONE_A01_<Title>
│   │   ├── AreaIdentity
│   │   ├── AreaBounds
│   │   ├── EntryCheckpoint
│   │   ├── Objectives
│   │   ├── GuidanceTargets
│   │   ├── NPCs
│   │   ├── Interactables
│   │   ├── LearningContent
│   │   ├── ActivityObjects
│   │   ├── QuestionContent
│   │   ├── Collectible
│   │   ├── WorldResult
│   │   └── UnlockToNextArea
│   ├── ZONE_A02_<Title>
│   └── ZONE_A03_<Title>
├── _GAMEPLAY_UI
│   ├── UITK_GameplayPanels
│   ├── Canvas_HUD
│   ├── Canvas_Feedback
│   ├── Canvas_System
│   ├── Canvas_Transition
│   └── WorldSpaceUI
├── _AUDIO
├── _VFX
├── _SERVICES
└── _DEBUG_EDITOR_ONLY
```

The environment is visible as one coherent open world.

Locked areas may keep their environment visible while heavy gameplay objects remain inactive until required.

## 7. Mission runtime state

Mission states:

```text
Locked
Available
Entering
Started
InProgress
ReviewRequired
MissionCompleted
```

Area states:

```text
Locked
Available
Started
InProgress
ReviewRequired
CollectibleUnlocked
CollectibleCollected
Completed
```

Subject-specific actions use their own explicit states.

Do not model progression with unrelated booleans.

Important state must persist outside scene-only MonoBehaviours.

## 8. Subject gameplay loops

### LiteraQuest

```text
Explore
→ inspect story evidence
→ manipulate one story artifact
→ resolve two or three short checks
→ confirm the interpretation
→ restore the story world
→ collect a Story Fragment
→ checkpoint or mission completion
```

Each area contains one main story source and one principal manipulation interaction such as arranging events, repairing a caption, matching dialogue, selecting evidence, or assembling a short narrative artifact.

### PE & Health

```text
Explore
→ observe one health or safety situation
→ identify relevant clues
→ resolve two or three situation checks
→ choose a healthy or safe action
→ perform the action in the world
→ observe the NPC/environment result
→ collect a Wellness Symbol
→ checkpoint or mission completion
```

Answering alone is not enough. The predefined healthy action and visible result are required. Do not diagnose, prescribe, or replace professional guidance.

### Science

```text
Explore
→ observe
→ make one unscored prediction when appropriate
→ perform one investigation
→ record one result
→ resolve two or three evidence checks
→ form one conclusion
→ apply the scientific solution
→ observe the world result
→ collect a Science Evidence Token
→ checkpoint or mission completion
```

A prediction may be incorrect without being treated as a failed scored question. Grade 5 investigations are guided, visual, and normally one principal action. Grade 6 may use variables, repeated trials, fair testing, precise measurement, and evidence-supported conclusions.

## 9. Question systems

### Static gameplay question engine

Static gameplay questions are stored locally in Unity.

Supported types:

```text
multiple_choice_single
multiple_choice_multiple
true_false
prediction_single_unscored
```

Default policy:

- two or three scored checks per area;
- hard maximum of four scored checks per area;
- one additional unscored Science prediction when appropriate;
- maximum two attempts per scored closed-answer question;
- first wrong answer changes the active learning overlay to a focused hint state;
- second wrong answer changes the same overlay to an explanation/correct-concept state and records review-required;
- no life loss, mission restart, or mandatory full quiz repetition;
- review-required items appear in the optional objective/journal drawer and final mission result rather than a separate area-review modal.

Use replaceable data assets. Do not hard-code final text or answer keys in controllers. Load them from the versioned per-mission JSON packs indexed by `GAMEPLAY_CONTENT_CATALOG_V1.json`.

### Quiz Portal client

Quiz Portal is a separate server-managed application feature.

Unity receives:

- quiz summary;
- quiz detail without answer keys;
- question/options;
- availability;
- attempt rules;
- scored result after submission;
- result history.

The server scores Quiz Portal attempts. Static gameplay and Quiz Portal DTOs, storage, presenters, and services remain separate. Static gameplay answer keys never enter Student API DTOs.

## 10. Hybrid UI architecture

### UI Toolkit application UI

Use UI Toolkit for bootstrap, authentication, home, subject/term/mission browsing, profile, progress, rewards, certificates, announcements, leaderboards, settings, and Quiz Portal screens.

Use the pinned design-system package through NutriMind-owned templates, tokens, adapters, and overrides. Do not edit package styles.

### Blocking gameplay overlays

Use UI Toolkit for the small set of complex blocking gameplay surfaces:

- mission introduction;
- one reusable learning-and-question overlay;
- pause menu when implemented in the same framework;
- mission-complete result;
- optional exit confirmation.

The reusable learning overlay handles evidence, reading, question, answer choices, first-wrong hint, second-wrong explanation, and acknowledgement as states of one presenter. Do not create separate modal prefabs for learning clue, reminder, review, all-correct, investigation-complete, healthy-choice-complete, area-complete, and learning-summary states.

### uGUI gameplay UI

Use uGUI screen-space Canvas for:

- compact objective and `x/3` collectible HUD;
- interaction prompts and controller hints;
- subtitles and short NPC status;
- correct-answer and area-restored feedback;
- collectible reveal and checkpoint toast;
- pause/loading/transition where animation requirements make uGUI preferable.

Use uGUI world-space Canvas for NPC, object, station, path, collectible, and interaction markers.

One `GameplayUiCoordinator` is the sole blocking-overlay and input-gating authority. Player and camera input are blocked only while a blocking overlay is open. Non-modal subtitles, prompts, HUD, and feedback do not stop exploration.

## 11. Application scenes and screens

### SCN_App_Bootstrap

Screens/states:

- splash;
- local data initialization;
- secure token check;
- connectivity check;
- client version check;
- manifest version check;
- bootstrap loading;
- offline eligibility;
- required-update state;
- maintenance state;
- recoverable error;
- route to Authentication or Main.

### SCN_App_Authentication

Screens:

- Student login;
- PIN visibility toggle;
- validation error;
- rate-limit message;
- offline-unavailable explanation;
- privacy/help content;
- successful-login transition.

Do not store PIN.

### SCN_App_Main

Routes:

- Home;
- Subjects;
- Terms;
- Mission List/Map;
- Mission Detail;
- Progress;
- Profile;
- Rewards;
- Certificates;
- Announcements;
- Leaderboard;
- Settings;
- About/Credits/Privacy;
- Sign out.

Home shows:

- Student name;
- grade;
- continue mission;
- available Quiz Portal assignments;
- progress summary;
- announcements;
- recent reward/certificate state;
- sync status.

Mission Detail shows:

- title;
- premise;
- learning focus;
- three-area progress;
- three collectibles;
- status;
- play/continue;
- offline availability.

### SCN_App_QuizPortal

Routes:

- Quiz list;
- Quiz detail;
- Quiz attempt;
- submission confirmation;
- result;
- result history;
- recoverable submission state;
- duplicate submission recovery.

Every application screen needs:

```text
loading
content
empty
offline
error
disabled/locked
```

Use responsive layouts and keyboard/touch navigation.

## 12. Application UI architecture

Recommended pattern:

```text
AppSceneBootstrap
→ AppRouter
→ ScreenCoordinator
→ feature service
→ API client/cache
→ presenter/view model
→ UXML view
```

Do not place HTTP calls in VisualElement subclasses.

Feature structure:

```text
AppUI/Features/<Feature>/
├── Views/
├── Presenters/
├── Models/
├── Services/
└── Tests/
```

Use reusable:

- navigation shell;
- top bar;
- side/bottom navigation;
- cards;
- tabs;
- filters;
- loading states;
- empty states;
- error states;
- modal/confirm;
- toast;
- pagination/list virtualization.

## 13. Gameplay UI hierarchy

```text
_GAMEPLAY_UI
├── EventSystem
├── UITK_GameplayPanels
│   ├── UIDocument_GameplayModal
│   └── PS_GameplayModal
├── Canvas_HUD                  screen space, order 0
│   ├── MissionHeader
│   ├── ObjectiveTracker
│   ├── AreaProgress_1of3
│   ├── CollectibleProgress_0of3
│   ├── InteractionPrompt
│   └── SubjectJournalButton
├── Canvas_Feedback             screen space, order 100
│   ├── AnswerFeedbackToast
│   ├── CollectibleReveal
│   └── CheckpointToast
├── Canvas_System               screen space, order 500
│   ├── PausePanel
│   ├── ConfirmationDialog
│   └── ErrorPanel
├── Canvas_Transition           screen space, order 1000
└── WorldSpaceUI                world-space uGUI Canvas roots
    ├── NPCMarkers
    ├── ObjectMarkers
    ├── PathMarkers
    └── InteractionAnchors
```

`GameplayUiCoordinator` is the single blocking-modal and input-gating authority. UI Toolkit owns complex/data-heavy panels. uGUI owns lightweight HUD/feedback and in-world UI. Player movement and camera input are blocked while a blocking panel is open.

## 14. Player, camera, and interaction

Create reusable project-owned prefabs/adapters.

Player requirements:

- third-person movement;
- camera-relative locomotion;
- walk and light run;
- slope/ground handling;
- no combat;
- configurable jump;
- input gating;
- checkpoint teleport;
- animation bridge;
- interaction scanner.

Camera requirements:

- Cinemachine-based third-person follow;
- pitch limits;
- damping;
- collision handling;
- configurable distance/FOV;
- disabled look during modals/pause.

Interaction requirements:

- typed `Interactable`;
- prompt anchor;
- player stand point;
- camera focus target;
- optional look target;
- interaction range;
- one active target;
- no `GameObject.Find` dependency chains.

## 15. Mission checkpoints and open-world unlocking

Each area has one checkpoint.

Checkpoint commit:

```text
begin local transaction
→ update area progress
→ insert outbox event
→ update mission progress
→ commit
→ update HUD/world state
→ unlock next area in world
```

Area unlock is not a scene load.

Examples:

- open a gate;
- repair a bridge;
- clear mist;
- activate path lights;
- restore a machine;
- open an archive door.

When re-entering a mission, restore:

- completed area world states;
- collected items;
- gates/paths;
- active objective;
- player checkpoint;
- mission completion result.

## 16. Content architecture

Recommended authored assets:

```text
GradeCatalog
SubjectDefinition
TermDefinition
MissionDefinition
AreaDefinition
DialogueSequence
LearningClueDefinition
StaticQuestionSet
StaticQuestionDefinition
CollectibleDefinition
WorldStateDefinition
SubjectActionDefinition
MissionCompletionDefinition
```

Every definition has a stable ID.

Mission definitions reference three areas.

Scene bindings connect authored definitions to placed GameObjects.

Use editor validation for:

- duplicate IDs;
- wrong grade/subject/term/mission;
- missing area zones;
- wrong area order;
- missing collectible;
- missing checkpoint;
- missing objective/guidance;
- missing question data;
- missing final challenge in Area 3;
- invalid scene registry;
- missing persistence binding.

## 17. Local persistence

Required abstractions:

```text
IProfileCache
ISettingsRepository
IMissionProgressRepository
IProgressOutbox
ISyncStateRepository
ISecureTokenStore
```

Recommended gameplay storage:

```text
SQLite under Application.persistentDataPath

SQLite stores progress and outbox state only. Authored dialogue, questions, option text, learning clues, and answer keys remain in versioned JSON.
```

Select the SQLite package only after verifying:

- Unity version;
- target platforms;
- IL2CPP/AOT;
- license;
- maintenance;
- existing package constraints.

Tokens use secure storage, not gameplay SQLite.

Required local entities:

```text
LocalProfile
LocalSettings
LocalMissionProgress
LocalAreaProgress
LocalQuestionProgress
LocalCollectibleProgress
LocalOutboxEvent
LocalSyncState
LocalQuizAttemptDraft
```

## 18. Offline behavior

After prior authentication:

- application may use cached profile and grade;
- cached mission availability may be viewed;
- installed static missions may be played;
- progress is committed locally;
- outbox waits for connectivity;
- Quiz Portal submission requires connectivity unless a specific offline draft policy is implemented;
- server-only screens show cached or offline state clearly.

Token expiry must not delete local gameplay progress.

## 19. API integration

Canonical prefix:

```text
/api/v1/student
```

Required headers follow the shared OpenAPI contract.

Use separate layers:

```text
DTO
transport
authentication
retry
serialization
application cache
domain mapping
gameplay services
```

Gameplay systems never call HTTP directly.

Unity branches on `error.code`.

Ignore unknown additive response fields.

Use UUIDs exactly as defined by the shared contract.

Retry uncertain requests with the same UUID and payload.

## 20. Folder architecture

Recommended:

```text
Assets/NutriMind/
├── Runtime/
│   ├── Core/
│   ├── Application/
│   ├── Authentication/
│   ├── Networking/
│   ├── Persistence/
│   ├── Sync/
│   ├── Player/
│   ├── Camera/
│   ├── Interaction/
│   ├── Guidance/
│   ├── Questions/
│   ├── Gameplay/
│   ├── Subjects/
│   │   ├── LiteraQuest/
│   │   ├── PEHealth/
│   │   └── Science/
│   └── UI/
│       ├── AppToolkit/
│       └── GameplayCanvas/
├── Editor/
│   ├── Validation/
│   ├── ContentTools/
│   └── SceneTools/
├── Content/
│   ├── Grade5/
│   └── Grade6/
├── Prefabs/
│   ├── Player/
│   ├── Camera/
│   ├── GameplayUI/
│   ├── Interaction/
│   └── Shared/
├── AppUI/
│   ├── UXML/
│   ├── USS/
│   ├── Theme/
│   ├── Components/
│   └── Screens/
├── Scenes/
│   ├── Application/
│   ├── Grade5/
│   └── Grade6/
├── Tests/
│   ├── EditMode/
│   └── PlayMode/
└── Docs/
```

Assembly boundaries:

```text
NutriMind.Core
NutriMind.Application
NutriMind.Networking
NutriMind.Persistence
NutriMind.Gameplay
NutriMind.LiteraQuest
NutriMind.PEHealth
NutriMind.Science
NutriMind.AppUI
NutriMind.GameplayUI
NutriMind.Editor
NutriMind.Tests.EditMode
NutriMind.Tests.PlayMode
```

Avoid cyclic references.

## 21. Dependency rules

Before adding a package:

1. inspect project version and lock files;
2. verify target-platform compatibility;
3. verify license;
4. verify maintenance and Unity support;
5. confirm existing packages do not provide the feature;
6. pin exact version/commit;
7. run compilation, tests, and target build.

For the UI Toolkit design system:

- use the package only with a compatible Unity 6 version;
- pin its package version or commit;
- keep vendor code separate;
- do not edit vendor files;
- maintain NutriMind-owned tokens and overrides;
- retain an exit path if the package is replaced later.

## 22. Accessibility and UX

Application and gameplay UI must support:

- readable contrast;
- text scaling strategy;
- safe areas;
- keyboard and touch navigation;
- visible focus;
- non-color status indicators;
- captions/text alternatives for critical audio;
- reduced motion option for non-essential effects;
- clear offline/sync states;
- age-appropriate language.

Do not use punitive feedback.

## 23. Security and privacy

- never store PIN;
- store bearer token through secure-storage abstraction;
- never log token or sensitive payloads;
- never trust locally edited grade/profile as server authority;
- validate manifest and IDs;
- keep Quiz Portal answer keys out of local pre-submission DTOs;
- clear token on authentication failure while preserving local progress;
- keep debug menus out of production builds.

## 24. Performance requirements

- avoid unnecessary `Update`;
- avoid per-frame allocations;
- pool frequently repeated UI/gameplay effects when justified;
- use LODs and occlusion/profile-based scene design;
- deactivate heavy locked-area gameplay objects when safe;
- keep environment continuity;
- use virtualized UI Toolkit lists;
- avoid loading all quiz history or announcements at once;
- profile target hardware;
- preserve maintainability before speculative optimization.

## 25. Testing

### EditMode

- stable IDs;
- three-area manifest validation;
- content validation;
- mission/area state transitions;
- question policy;
- review tracking;
- collectible idempotency;
- persistence serialization;
- outbox replay;
- API DTO fixture deserialization;
- AppRouter/presenter behavior;
- UI state models.

### PlayMode

Application:

- bootstrap routing;
- login;
- application navigation;
- loading/empty/offline/error states;
- Quiz Portal attempt and duplicate recovery.

Gameplay:

- mission entry;
- Area 1–3 unlock sequence;
- objective/guidance;
- question feedback;
- subject action;
- collectible;
- checkpoint restore;
- mission completion;
- offline play;
- sync acknowledgement.

No automated test calls a live production server.

## 26. Current Unity rebuild milestone

Deliver:

### Application foundation

- four application scenes;
- complete application screens;
- hybrid UI architecture;
- Sinan Ata design-system integration;
- screen routing;
- loading/empty/offline/error states;
- API client and local cache boundaries;
- profile/settings/progress/rewards/certificates/announcements/leaderboard screens.

### Quiz Portal

- quiz list;
- detail;
- attempt;
- submission;
- result;
- history;
- duplicate recovery;
- server-scored DTO integration.

### Static question engine

- reusable question data;
- single choice;
- multiple answer;
- true/false;
- two-attempt policy;
- hint/explanation;
- review summary;
- uGUI presenters.

### Complete gameplay mission

```text
Grade 5
LiteraQuest
Term 1
Mission 1
The Festival Storybook Rescue
```

One mission scene with three areas.

### Overview-only mission definitions

```text
Grade 5 PE & Health T1 M1 — The Storm Inside
Grade 5 Science T1 M1 — The Vanishing Supply Cart
```

Provide implementation-ready content/data/scene overviews, not full gameplay scenes.

## 27. Definition of done

A Unity feature is complete only when:

- stable IDs and content assets exist;
- scene/UI hierarchy is explicit;
- logic is separate from final art;
- persistence and outbox behavior are defined;
- API boundary is respected;
- editor validation exists;
- EditMode/PlayMode tests exist;
- compilation and applicable build checks pass;
- human Inspector setup is documented;
- placeholder content is labeled;
- missing approvals/assets are reported;
- no superseded five-area or one-scene-per-area architecture remains.
