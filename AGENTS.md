# NutriMind Unity Repository — AGENTS.md

These rules apply to all AI agents and human contributors working in the Unity repository.

Use one shared project structure. Do not create separate folders for agents, developers, experiments, or individual contributors.

## 1. Source of truth

Before making changes, read:

1. `AGENTS.md`
2. The current Unity milestone document.
3. The relevant architecture document.
4. The affected mission JSON and schema.
5. The existing implementation being changed.

When documents conflict, preserve these non-negotiable product rules:

* One Unity scene per mission.
* Exactly three logical areas per mission.
* Area 3 contains the integrated final challenge.
* Static gameplay content belongs to local JSON.
* SQLite stores learner state and the synchronization outbox.
* The server controls classroom mission availability and tracks gameplay progress.
* Quiz Portal remains a separate server-managed system.

## 2. Current milestone

The current milestone is unchanged:

* Complete application scenes and routing.
* Complete the Quiz Portal application flow.
* Build reusable content-loading, validation, question, SQLite, and synchronization foundations.
* Fully implement `g5_lq_t1_m01`.
* Keep `g5_peh_t1_m01` and `g5_sci_t1_m01` as content/data foundations and implementation-ready overviews only.

Playable milestone scene:

```text
SCN_G5_LQ_T1_M01_TheFestivalStorybookRescue
```

Do not build all future mission scenes merely because their JSON content exists.

## 3. Canonical folder structure

All first-party files belong under `Assets/NutriMind`.

```text
Assets/
├── NutriMind/
│   ├── Core/
│   │   ├── Scripts/
│   │   │   ├── Bootstrap/
│   │   │   ├── Data/
│   │   │   ├── Persistence/
│   │   │   ├── Networking/
│   │   │   └── Utilities/
│   │   └── Prefabs/
│   │
│   ├── App/
│   │   ├── Scenes/
│   │   ├── Scripts/
│   │   ├── Prefabs/
│   │   └── UI/
│   │       ├── UXML/
│   │       ├── USS/
│   │       └── Controllers/
│   │
│   ├── Gameplay/
│   │   ├── Scripts/
│   │   │   ├── Player/
│   │   │   ├── Camera/
│   │   │   ├── Interaction/
│   │   │   ├── Missions/
│   │   │   ├── Questions/
│   │   │   ├── Progress/
│   │   │   └── UI/
│   │   ├── Prefabs/
│   │   │   ├── Player/
│   │   │   ├── Interactions/
│   │   │   ├── Collectibles/
│   │   │   └── MissionObjects/
│   │   └── UI/
│   │       ├── UITK/
│   │       │   ├── UXML/
│   │       │   └── USS/
│   │       ├── Canvas/
│   │       └── WorldSpace/
│   │
│   ├── Content/
│   │   ├── Catalogs/
│   │   ├── Missions/
│   │   │   ├── Grade5/
│   │   │   │   ├── LiteraQuest/
│   │   │   │   ├── PEHealth/
│   │   │   │   └── Science/
│   │   │   └── Grade6/
│   │   │       ├── LiteraQuest/
│   │   │       ├── PEHealth/
│   │   │       └── Science/
│   │   └── Schemas/
│   │
│   ├── Missions/
│   │   ├── Grade5/
│   │   │   ├── LiteraQuest/
│   │   │   ├── PEHealth/
│   │   │   └── Science/
│   │   └── Grade6/
│   │       ├── LiteraQuest/
│   │       ├── PEHealth/
│   │       └── Science/
│   │
│   ├── Shared/
│   │   ├── UI/
│   │   │   ├── Icons/
│   │   │   ├── SVG/
│   │   │   ├── Illustrations/
│   │   │   ├── Backgrounds/
│   │   │   └── Fonts/
│   │   ├── Characters/
│   │   ├── Environment/
│   │   ├── Props/
│   │   ├── Materials/
│   │   ├── Textures/
│   │   ├── Animations/
│   │   ├── Audio/
│   │   ├── VFX/
│   │   └── Prefabs/
│   │
│   ├── Settings/
│   │   ├── Input/
│   │   ├── Rendering/
│   │   └── UI/
│   │
│   ├── Editor/
│   └── Tests/
│       ├── EditMode/
│       ├── PlayMode/
│       └── TestData/
│
└── ThirdParty/
```

Do not create competing first-party folders such as `Assets/Scripts`, `Assets/UI`, or `Assets/Game`.

## 4. Folder responsibilities

| Folder       | Use                                                                                                  |
| ------------ | ---------------------------------------------------------------------------------------------------- |
| `Core`       | Bootstrap, data contracts, JSON loading, SQLite, networking, synchronization, and reusable utilities |
| `App`        | Login, home, subject/term/mission selection, profile, settings, progress, and Quiz Portal screens    |
| `Gameplay`   | Player, camera, interactions, mission runtime, questions, progress, collectibles, and gameplay UI    |
| `Content`    | Mission catalogs, static mission JSON, schemas, and content-version metadata                         |
| `Missions`   | Unity assets unique to one implemented mission                                                       |
| `Shared`     | Art, audio, UI assets, prefabs, and materials reused by multiple features or missions                |
| `Settings`   | Input, rendering, UI Toolkit panel settings, and other Unity configuration assets                    |
| `Editor`     | Editor-only tools and validators                                                                     |
| `Tests`      | EditMode tests, PlayMode tests, and non-production fixtures                                          |
| `ThirdParty` | Imported vendor packages and external assets                                                         |

Use this placement rule:

```text
Reusable technical system       -> Core or Gameplay
Application screen or route     -> App
Static curriculum content       -> Content
One mission's Unity assets      -> Missions/<mission>
Reusable art or audio asset     -> Shared
Unity configuration asset       -> Settings
Editor-only tool                -> Editor
Automated test                  -> Tests
External package                -> ThirdParty
```

## 5. Mission folders

Each implemented mission receives one folder.

```text
Assets/NutriMind/Missions/Grade5/LiteraQuest/G5_LQ_T1_M01/
├── Scenes/
├── Environment/
├── Prefabs/
├── Materials/
├── Lighting/
├── Animations/
└── Audio/
```

Place only mission-specific assets here.

Reusable benches, trees, characters, icons, materials, or interaction prefabs belong in `Shared` or `Gameplay`.

Do not create additive subscenes until the main scene becomes difficult to manage or collaboration explicitly requires them.

## 6. Protect developer-authored scenes

The developer owns environment design, asset placement, lighting composition, and visual staging.

Unless the task explicitly requests it, agents must not:

* Move, rotate, scale, replace, or delete environment objects.
* Redesign terrain, lighting, or camera composition.
* Reorganize the scene hierarchy broadly.
* Rename authoring anchors.
* Replace manually placed assets.
* Convert scene objects into prefabs unnecessarily.

Agents may add non-destructive integration such as:

* Scripts and components.
* Stable ID bindings.
* Triggers and interaction adapters.
* Marker anchors.
* System root objects.
* Reusable prefabs.
* Validation tools.
* Written instructions for manual Unity Editor wiring.

Prefer additive changes over restructuring a hand-authored scene.

## 7. Gameplay scene rules

Every mission must have:

* One main scene.
* Exactly three logical areas.
* One main collectible per area.
* Area 3 as the final integrated challenge.
* Stable mission, area, interaction, question, and collectible IDs.

Do not create a separate final-challenge scene.

Recommended hierarchy for new mission scenes:

```text
_SCENE
├── _SYSTEMS
├── _PLAYER
├── _ENVIRONMENT
├── _AREAS
│   ├── Area_01
│   ├── Area_02
│   └── Area_03
├── _INTERACTIONS
├── _COLLECTIBLES
├── _GAMEPLAY_UI
├── _LIGHTING
└── _DEBUG
```

Do not force this hierarchy onto an existing scene when doing so would risk references or disrupt the developer's work.

## 8. Hybrid UI rules

### UI Toolkit

Use UI Toolkit for complex or data-heavy panels:

* Mission introduction.
* Dialogue, reading, and learning clues.
* Question-and-answer panels.
* Multiple-answer selection.
* Hints, explanations, and review.
* Science Journal and detailed Wellness Guide.
* Mission summary and detailed results.

Place files in:

```text
Gameplay/UI/UITK/UXML/
Gameplay/UI/UITK/USS/
Gameplay/Scripts/UI/
```

### uGUI screen-space Canvas

Use uGUI Canvas for:

* HUD.
* Compact objectives.
* Interaction prompts.
* Reticle and control hints.
* Short feedback.
* Collectible and checkpoint notifications.
* Pause, loading, and transition overlays.

Place reusable prefabs in:

```text
Gameplay/UI/Canvas/
```

### uGUI world-space Canvas

Use world-space Canvas for:

* NPC, object, station, and path markers.
* Interaction anchors.
* Lock indicators.
* Short in-world status displays.

Place reusable prefabs in:

```text
Gameplay/UI/WorldSpace/
```

Do not put long dialogue or full question panels in world space.

Use one `GameplayUiCoordinator` to control modal state, player input, camera input, focus, cursor state, and pointer/raycast blocking across both UI systems.

UI scripts must not own mission progression, SQL transactions, or raw server DTOs.

## 9. Static content rules

Use one primary JSON file per mission.

```text
Assets/NutriMind/Content/Missions/Grade5/LiteraQuest/Term1/g5_lq_t1_m01.json
```

The file contains all three mission areas, including:

* Story and objectives.
* Dialogue and learning clues.
* Questions, choices, and answer keys.
* Hints, explanations, and feedback.
* Interactions and subject actions.
* World-state results.
* Collectibles and completion summary.

Use lowercase snake-case stable IDs:

```text
g5_lq_t1_m01
g5_lq_t1_m01_a01
g5_lq_t1_m01_a01_q01
```

After learner progress exists, do not rename stable IDs.

Every content change must:

* Pass schema validation.
* Keep answer keys valid.
* Keep scored questions at five or fewer per area.
* Update the content version when learner outcomes may change.
* Update the catalog hash when hashes are enabled.

Do not send static dialogue, questions, choices, answer keys, hints, or explanations to the gameplay server.

## 10. SQLite and server boundaries

Create the runtime database under:

```text
Application.persistentDataPath/NutriMind/nutrimind.db
```

Do not store or commit learner databases inside `Assets`.

SQLite stores:

* Mission and area progress.
* Interaction completion.
* Question attempts and outcomes.
* Review flags.
* World-state facts.
* Collectibles.
* Checkpoints.
* Content version used.
* Pending synchronization events.

Static authored content remains in JSON, not SQLite.

Commit a local progress change and its outbox event in the same transaction.

Synchronization must be idempotent and safe to retry.

The classroom server may lock or unlock mission availability and track progress. It must not become the authoring source for static gameplay content.

## 11. C# conventions

```text
Namespace       NutriMind.Gameplay.Questions
Class           QuestionPanelController
Interface       IGameplayPanel
Enum            MissionRuntimeState
Method          LoadMissionAsync
Property        CurrentAreaId
Private field   _currentMission
Local variable  missionData
Constant        MaxQuestionAttempts
```

Rules:

* Use PascalCase for types, methods, properties, events, and namespaces.
* Use `_camelCase` for private instance fields.
* Use camelCase for parameters and local variables.
* Prefix interfaces with `I`.
* Match the filename to the primary public type.
* Prefer one primary public type per file.
* Add `Async` to asynchronous method names.
* Avoid vague names such as `Helper`, `Thing`, or unnecessary `Manager` classes.

Namespaces follow folders:

```text
Core       -> NutriMind.Core...
App        -> NutriMind.App...
Gameplay   -> NutriMind.Gameplay...
Editor     -> NutriMind.Editor...
Tests      -> NutriMind.Tests...
```

## 12. Unity asset naming

| Asset               | Prefix  | Example                                       |
| ------------------- | ------- | --------------------------------------------- |
| Scene               | `SCN_`  | `SCN_G5_LQ_T1_M01_TheFestivalStorybookRescue` |
| Prefab              | `PF_`   | `PF_NpcMarker`                                |
| Material            | `MAT_`  | `MAT_StoryFragmentGlow`                       |
| Texture             | `T_`    | `T_FestivalGround_Albedo`                     |
| Background          | `BG_`   | `BG_MissionSelection`                         |
| Icon                | `ICO_`  | `ICO_MissionLocked`                           |
| Illustration        | `ILL_`  | `ILL_LiteraQuestHeader`                       |
| Sprite atlas        | `SA_`   | `SA_GameplayIcons`                            |
| Font asset          | `FONT_` | `FONT_NutriMindPrimary`                       |
| Animation clip      | `ANIM_` | `ANIM_Npc_Wave`                               |
| Animator controller | `AC_`   | `AC_StudentCharacter`                         |
| Sound effect        | `SFX_`  | `SFX_CollectiblePickup`                       |
| Music               | `MUS_`  | `MUS_FestivalTheme`                           |
| Voice-over          | `VO_`   | `VO_Guide_Introduction`                       |
| Visual effect       | `VFX_`  | `VFX_StoryFragmentReveal`                     |
| ScriptableObject    | `SO_`   | `SO_GameplaySettings`                         |
| Input Actions       | `IA_`   | `IA_NutriMind`                                |
| Panel Settings      | `PS_`   | `PS_GameplayPanels`                           |

Do not use spaces in filenames.

Do not use suffixes such as `_Final`, `_Latest`, `_New`, or `_V2` for normal working assets. Use Git history and content-version fields.

## 13. UI Toolkit naming

Use the same base name for related files:

```text
QuestionPanel.uxml
QuestionPanel.uss
QuestionPanelController.cs
```

Use kebab case for UXML names and USS classes:

```text
question-title
answer-list
submit-button

question-panel
question-panel__answer
question-panel__answer--selected
```

Place reusable icons, SVGs, illustrations, backgrounds, and fonts under:

```text
Shared/UI/Icons/
Shared/UI/SVG/
Shared/UI/Illustrations/
Shared/UI/Backgrounds/
Shared/UI/Fonts/
```

Do not store images beside UXML or USS files.

## 14. Prefab rules

* Reusable gameplay prefab → `Gameplay/Prefabs`.
* Reusable visual prefab → `Shared/Prefabs` or the relevant shared asset folder.
* Mission-only prefab → that mission's `Prefabs` folder.

Do not create near-duplicate prefabs when one configurable prefab can serve the same role.

Do not create prefab variants only to change text, IDs, or JSON-driven content.

## 15. Unity metadata safety

Preserve Unity GUIDs.

Never:

* Delete or regenerate `.meta` files casually.
* Invent GUID values.
* Move an asset without its `.meta` file.
* Mass-edit scene or prefab YAML.
* Reserialize the whole project for an unrelated task.
* Modify `ThirdParty` content without explicit need.

Avoid direct text editing of `.unity`, `.prefab`, `.mat`, `.asset`, and Animator Controller YAML unless the task specifically requires it and the result can be validated safely.

## 16. Dependency rules

Keep dependencies directional:

```text
UI -> interfaces and view models
Mission code -> reusable Gameplay systems
Gameplay -> Core services and contracts
Core -> no dependency on App UI, Gameplay UI, or mission scenes
```

Domain and content models must not depend on UI Toolkit, uGUI, or scene GameObjects.

UI scripts must not execute SQL or construct synchronization payloads directly.

Do not create an assembly definition for every folder. Add assembly definitions only for clear runtime, Editor, or test boundaries, or to prevent invalid dependencies.

## 17. Agent workflow

Before editing:

1. Inspect existing code and assets.
2. Confirm the correct folder and filename.
3. Check the current milestone and relevant content IDs.
4. Avoid creating a second implementation of an existing system.

While editing:

1. Make the smallest coherent change.
2. Preserve public contracts, stable IDs, GUIDs, and scene composition.
3. Keep UI, gameplay logic, persistence, and networking separated.
4. Add tests or validation for progression, schema, persistence, or synchronization changes.
5. Record any manual Editor wiring that remains.

Before finishing:

1. Confirm all new files use the canonical folders and names.
2. Check `.meta` files.
3. Validate changed JSON and answer keys.
4. Run relevant EditMode and PlayMode tests.
5. Check for new Unity compile errors or warnings.
6. Confirm no unrelated scene, prefab, package, or `ProjectSettings` files changed.
7. Summarize changed files, tests, and manual setup.

## 18. Prohibited folders and files

Do not create:

```text
AgentFiles/
AIFiles/
DeveloperFiles/
MyAssets/
Misc/
Others/
Temporary/
Old/
Backup/
Final/
FinalNew/
FinalLatest/
```

Do not commit build output, caches, learner databases, IDE state, or secrets.

Typical ignored paths include:

```text
Library/
Temp/
Obj/
Build/
Builds/
Logs/
UserSettings/
*.db
*.db-shm
*.db-wal
.env
```

Do not use `Resources` as the primary mission-content system. Use the mission catalog and the approved runtime-loading strategy.

## 19. Definition of done

A task is complete only when:

* It remains within the current milestone.
* Files use the canonical structure and naming rules.
* Developer-authored scene work is preserved.
* Static gameplay content remains local and schema-valid.
* SQLite and server responsibilities remain correctly separated.
* Hybrid UI responsibilities are respected.
* Stable IDs and Unity GUIDs are preserved.
* Relevant validation or tests pass.
* Required manual Unity Editor steps are documented.

## Learned User Preferences

- Treat design-only, presentation-only, and Penpot-only UI work as visual/layout tasks: UXML/USS, Penpot boards, and shared UI assets only—App panel controllers stay presentation-only with static preview data (no API, SQLite, auth, sync, mission loading, or production routing) unless explicitly asked; Quiz Portal previews must show canonical/server-provided scores as-is (never recalculate) and must not invent selected answers, correct answers, explanations, or per-question points; wire App panels together only after the screen set is designed; when migrating a panel into AppShell, strip duplicated global shell chrome and keep the screen content-only so it works both embedded and as a standalone UIDocument preview; BootstrapPanel is a standalone pre-route screen (never hosted in AppShell or AppShellContentPreview, no bottom nav).
- Match supplied design references closely—exact padding, margins, type sizes, logo/text placement, and panel centering; when asked for layout-only, copy structure and spacing first and ignore reference artwork until imagery is requested; for large refinement passes, inspect references and existing UXML/USS first, plan the highest-impact fixes, and wait for approval before rewriting broadly when Plan Mode is requested.
- Prefer Full HD (1920×1080) landscape framing for static UI layout tests and Penpot boards, while keeping App UITK layouts responsive across Android screen sizes.
- Prefer building UITK layouts with the installed DesignSystem package (`com.sinanata.designsystem`) reusable components when available; do not modify that package; style via `NutriMindTheme.uss` and panel USS; keep nested DesignSystem button label colors on the package button color contract (do not override them to the wrong on-accent/primary text color); keep custom illustration placeholders only for the NutriMind logo, student avatars, three subject emblems, small badge emblems, and an optional empty-state mascot.
- Omit Demo Login and other non-reference chrome unless explicitly requested; on Subject Selection and Term Selection screens, omit Profile/Settings/Logout chrome and the Quiz Portal card (Quiz Portal belongs on Home).
- Use `ConfirmDialog` only for two-action confirmations; use `SystemDialog` for system-status/info interruptions (session expired, maintenance, connection required, etc.)—do not conflate the two.
- After a temporary design/layout test is approved, delete the test files and folders when asked rather than leaving them in the project.
- Every App screen whose content overflows must actually scroll in both Game view and the Device Simulator; preview/design mode is not an acceptable reason for scrolling to be missing, so make scrolling explicit (auto or always-visible scrollbars, usable wheel steps, touch dragging, keyboard paging) instead of letting flex shrink or clip the content.
- Prefer readable, accessible App UITK scale in Game view / device sims—enlarge undersized text/controls, keep dropdown/popup menus legible, use generous padding/margins, prefer medium or bold font weights over thin ones, keep state/dialog cards clearly visible against warm App backgrounds, and design selection cards so layouts stay valid with dynamic/variable text lengths and across locked/in-progress/completed states; when a layout only looks broken in the Device Simulator, fix panel-level scaling (Panel Settings reference resolution and match) rather than hand-tuning per-panel font sizes.
- For App UITK visual QA, prefer user-provided Game view screenshots over Unity MCP capture, SceneView, or PlayMode screenshot tools when those MCP tools are unreliable.
- On Profile, do not expose a full raw LRN (use a masked preview); style Sign Out as a clear red/destructive action.
- App subjects are only LiteraQuest, PE & Health, and Science—omit Friends navigation and other subjects; mission/term locks use classroom publication, teacher lock, prerequisite completion, or server availability—not stars, learner level, or term-completion percentage.

## Learned Workspace Facts

- Canonical first-party content lives under `Assets/NutriMind/` using Core, App, Gameplay, Content, Missions, Shared, Settings, Editor, and Tests (not a nested `NutriMindUnity/` wrapper).
- Application UI design references are under `Assets/NutriMind/Shared/UI/DesignRefs/` (PNG and JPG variants).
- Application UI Toolkit screens belong under `Assets/NutriMind/App/UI/` (UXML, USS including `NutriMindTheme.uss`, Controllers); do not edit `Packages/com.sinanata.designsystem`; App panel settings belong under `Assets/NutriMind/Settings/UI/`, where `PS_AppPanels` scales with screen size from a 1920×1080 reference at match 0.5 so Game view and Device Simulator stay proportional.
- Shared App UITK building blocks live under `Assets/NutriMind/App/UI/{UXML,USS,Controllers}/Shared/` (`DataStatePanel`, `ConfirmDialog`, `SystemDialog`, `LoadingOverlay`, `OfflineSyncBanner`); `AppShell` (`AppShell.uxml` / `AppShellController`) hosts shared chrome and the content region for remaining App screens.
- `DataStatePanel` is an in-content data-state host (loading, empty, cache, offline, recoverable error, permission/locked)—not a modal overlay; modal-style interruptions use `ConfirmDialog` or `SystemDialog`.
- Application UI is designed in Penpot via the `user-penpot` MCP as the layout source of truth; keep that work separate from Unity App UXML/USS/C#/scene implementation unless explicitly requested.
- The Unity Design System package is at `Packages/com.sinanata.designsystem`.
- The project targets Unity 6 (`6000.3.x` per `ProjectSettings/ProjectVersion.txt`).
- Temporary App UITK screen and shared-component previews (BootstrapPanel as standalone pre-route, Login, Home, Subject/Term/Mission Selection, Locked Mission, Profile, Settings, Progress, QuizListPanel, QuizDetailPanel, QuizAttemptPanel, QuizResultPanel, QuizHistoryPanel, MissionDetailPanel, RewardsPanel, CertificatesPanel, AppShell, DataStatePanel, ConfirmDialog, SystemDialog, LoadingOverlay, OfflineSyncBanner) are wired through `Assets/Scenes/SampleScene.unity` (`AppUiPreview`) until dedicated App scenes exist; prefer Unity MCP Inspector wiring when the Editor is stable, otherwise give step-by-step manual wiring instead of repeatedly retrying heavy MCP calls.
- AppShell content migration uses `IAppScreenView`, `AppScreenContent.uss`, and `AppShellContentPreviewController` to host content-only route screens (embedded with `app-screen-content--embedded` or standalone UIDocument); Home, Subject Selection, Term Selection, Mission Selection, Locked Mission, Profile, Settings, Progress, QuizListPanel (Quiz Portal list), QuizDetailPanel (Quiz Portal detail), QuizAttemptPanel (Quiz Portal attempt), QuizResultPanel (Quiz Portal result), QuizHistoryPanel (Quiz Portal history), MissionDetailPanel, RewardsPanel, and CertificatesPanel are all migrated, each with a reusable `<Panel>View`, a standalone controller, and an AppShell factory case—AppShell-embedded is the primary authenticated-route preview and standalone SampleScene objects are secondary dual-host checks; shared scroll rules in `AppScreenContent.uss` size scroll content to its children so screens scroll instead of being squeezed and clipped.
- Local-only App Settings options should work without the server via `AppLocalSettings`; server-backed settings stay deferred.
- App UITK iteration commonly uses the Singularity Group Hot Reload package; pause Hot Reload and free RAM before Unity MCP `ManageGameObject`/scene wiring; use Unity MCP sparingly (prefer `ReadConsole`, `ValidateScript`, and narrow `RunCommand`; avoid hierarchy dumps, broad object searches, list-property overwrites, and forced reimports) so the Editor does not hang in an infinite reload.
