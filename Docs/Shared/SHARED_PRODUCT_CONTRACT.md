# Shared Product Contract

## Product

NutriMind is a Grade 5 and Grade 6 educational platform with a Laravel–Inertia–React classroom server and a Unity Student application. It provides static open-world learning missions, a separate server-managed Quiz Portal, local-first gameplay, classroom progress tracking, mission release controls, reports, rewards, certificates, announcements, and leaderboards.

## Product boundary

### Unity owns gameplay content and execution

Unity owns and loads developer-editable static JSON for:

- mission premise, story, and objective;
- dialogue and learning clues;
- gameplay questions, options, answer keys, hints, explanations, and feedback;
- NPCs, interactables, subject activities, world results, and collectibles;
- local mission execution and immediate local scoring.

SQLite stores learner state and a sync outbox. It does not become an authored-content database.

### Server owns classroom operations and tracking

For static gameplay, the server owns only:

- Student identity, grade, school year, section, and classroom scope;
- mission publication and classroom mission lock/unlock policy;
- prerequisite-aware mission availability returned to Unity;
- canonical mission/area progress facts synchronized from Unity;
- Teacher progress views, reports, and audit history;
- idempotent sync receipts and canonical revisions.

A Teacher may view progress for assigned Students and lock or unlock missions for the Teacher’s own classroom. A Teacher does not edit static gameplay dialogue, questions, or answer keys through the server.

Quiz Portal remains a separate server-managed system. Its authoring, assignment, delivery, answer protection, scoring, and results are not part of the static gameplay-content pipeline.

## Roles

### Admin

Manages schools, school years, users, curriculum structure, platform publication, permissions, audits, announcements, reports, and operational controls.

### Teacher

Within assigned classroom scope, manages Students, views gameplay progress, controls mission availability, manages Quiz Portal work, and issues or reviews supported reports and certificates.

### Student

Authenticates in Unity, plays grade-eligible and classroom-released missions, completes separate Quiz Portal assignments, views progress and rewards, and synchronizes local gameplay facts.

## Curriculum structure and IDs

```text
Grade → Subject → Term → Mission → exactly three logical Areas
```

```text
grade_5 | grade_6
subject_literaquest | subject_pe_health | subject_science
term_1 | term_2 | term_3
```

Mission IDs: `g<grade>_<subject>_t<term>_m<mission>`.
Area IDs: `<mission_id>_a01` through `<mission_id>_a03`.
Stable IDs never change after progress exists.

## Mission scene model

Each mission is one Unity open-world scene containing three spatially distinct logical areas, three checkpoints, three collectibles, and one integrated final challenge in Area 3. Area transitions are world-state changes, not scene loads.

## Optimized core loop

Mission introduction appears once. Each area then uses one continuous chain:

```text
Explore → Observe/Read → Interact → Learn → Answer → Apply subject action → See world result → Collect → Checkpoint → Unlock
```

Only show a review panel when an incorrect answer requires review. Do not show a separate “all correct” completion panel before the collectible. Use a lightweight success state, one collectible reveal, and one checkpoint toast. Area 3 integrates synthesis and the final world action; there is no separate final-challenge scene or API.

### Subject identity

- LiteraQuest: inspect text/visual evidence, answer, repair/arrange/publish, collect a Story Fragment.
- PE & Health: observe a health situation, decide, apply a safe healthy action, observe the result, collect a Wellness Symbol.
- Science: observe, make an unscored prediction, investigate, record evidence, answer, conclude, apply, collect a Science Evidence Token.

## Question policy

Maximum five scored questions per area. Scored closed-answer questions permit at most two attempts. First incorrect attempt gives a hint; second incorrect attempt reveals the correct concept, marks review-required, and continues. No lives, forced mission restart, or brute-force clicking.

## Progress and availability

Area progression is monotonic:

```text
Area 1 complete → Area 2 available
Area 2 complete → Area 3 available
Area 3 complete → mission complete
```

Mission availability is the intersection of:

- manifest publication;
- Student grade/term eligibility;
- prerequisite completion unless an authorized classroom override permits access;
- Teacher classroom lock/unlock policy.

A Teacher unlock does not complete prerequisites or fabricate progress. A Teacher lock prevents new entry but does not delete local or canonical progress. Resume policy for an already-started mission must be explicit and auditable.

## Offline model

After successful authentication, installed and locally permitted missions may run offline using the last signed availability snapshot. Unity commits progress and an idempotent outbox event in one SQLite transaction, updates UI immediately, then synchronizes later. Server policy wins on reconciliation without deleting valid local evidence silently.

## UI contract

Application scenes use UI Toolkit. Gameplay scenes use a deliberate hybrid:

- UI Toolkit screen-space panels for complex/data-heavy content such as mission introduction, dialogue/reading, question-and-answer, hint/review, journal/guide detail, and learning summary;
- uGUI screen-space Canvas for HUD, objectives, interaction prompts, toasts, pause, collectible feedback, and transitions;
- uGUI world-space Canvas for NPC/object/path markers, interaction anchors, and status indicators.

One modal coordinator gates player/camera input and prevents simultaneous UI Toolkit and uGUI modal interaction.

## API

Canonical Student API prefix: `/api/v1/student`.
`shared/API/openapi.yaml` is authoritative for transport. Static gameplay JSON is never returned by the Student API.
