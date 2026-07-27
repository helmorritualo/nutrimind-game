# Shared Product Contract

## Product

NutriMind is a Grade 5 and Grade 6 educational platform with a Laravel–Inertia–React classroom server and a Unity Student application. It provides static open-world learning missions, a separate server-managed Quiz Portal, local-first gameplay, classroom progress tracking, mission release controls, reports, rewards, certificates, announcements, and leaderboards.

## Product boundary

### Unity owns gameplay content and execution

Unity owns and loads developer-editable static JSON for:

- mission premise, competency ownership, story, and objective;
- dialogue and learning clues;
- gameplay questions, options, answer keys, hints, explanations, and feedback;
- NPCs, interactables, subject activities, world results, and collectibles;
- local mission execution and immediate local scoring.

SQLite stores learner-scoped state and a sync outbox. It does not become an authored-content database.

### Server owns classroom operations and tracking

For static gameplay, the server owns only:

- Student identity, grade, school year, section, and classroom scope;
- mission publication and classroom mission lock/unlock policy;
- prerequisite-aware mission availability returned to Unity;
- canonical mission/area progress facts synchronized from Unity;
- competency-aware progress summaries when competency metadata is included in the manifest;
- Teacher progress views, reports, and audit history;
- idempotent sync receipts and canonical revisions.

A Teacher may view progress for assigned Students and lock or unlock missions for the Teacher’s own classroom. A Teacher does not edit static gameplay dialogue, questions, answer keys, environment objects, or subject activities through the server.

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

## Competency ownership

Each mission owns one primary competency and may include one supporting competency. Static mission content declares:

```text
primary_competency_id
primary_competency_summary
supporting_competency_ids
prerequisite_competency_ids
review_competency_ids
mastery_evidence
mechanic_family
```

Later missions may review or integrate an earlier competency but must not silently reintroduce it as new learning. Future curriculum-draft packs require curriculum-owner review of these fields before production release.

## Mission scene model

Each mission is one Unity open-world scene containing three spatially distinct logical areas, three checkpoints, three collectibles, and one integrated mastery challenge in Area 3. Area transitions are world-state changes, not scene loads.

The stable phase IDs are:

```text
discover_and_connect
practice_and_apply
resolve_and_master
```

Their design roles are Discover, Apply, and Master.

## Optimized core loop

Mission introduction appears once. The complete requirements are defined in `THREE_AREA_GAMEPLAY_LOOP_CONTRACT.md`.

```text
Area 1: Discover
→ Area 2: Apply
→ Area 3: Master and complete
```

Each area uses one principal learning situation, one subject interaction, one world action, one collectible, and one checkpoint. Area 3 contains the former final challenge; there is no separate final-challenge scene or API.

### Subject identity

- LiteraQuest: inspect story evidence, manipulate a story artifact, confirm understanding, restore the story world, collect a Story Fragment.
- PE & Health: observe a health or safety situation, choose and perform a safe action, observe the consequence, collect a Wellness Symbol.
- Science: observe, predict, investigate, record, conclude, apply, collect a Science Evidence Token.

## Question policy

Use two or three scored checks per area by default and never exceed four. Science may add one unscored prediction. Scored closed-answer questions permit at most two attempts. The first incorrect attempt gives a focused inline hint; the second reveals the correct concept, marks review-required, and continues. No lives, forced mission restart, brute-force clicking, or mandatory area-quiz repetition.

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

After successful authentication, installed and locally permitted missions may run offline using the last signed availability snapshot. Unity commits progress and an idempotent outbox event in one learner-scoped SQLite transaction, updates UI immediately, then synchronizes later. Server policy wins on reconciliation without deleting valid local evidence silently.

## Minimal gameplay UI contract

Application scenes use UI Toolkit. Gameplay scenes use a deliberate hybrid.

Blocking gameplay UI is limited to:

- mission introduction;
- one reusable learning-and-question overlay;
- pause;
- mission complete;
- optional exit confirmation.

The learning overlay owns evidence, question, hint, explanation, and acknowledgement states. It is not split into separate learning, reminder, review, and completion modals.

Use uGUI screen-space and world-space UI for the compact HUD, objectives, prompts, subtitles, markers, feedback, collectible reveal, checkpoint toast, and transitions. Prefer visible world changes over panels. One `GameplayUiCoordinator` gates player/camera input and prevents simultaneous blocking UI.

## API

Canonical Student API prefix: `/api/v1/student`.
`shared/API/openapi.yaml` is authoritative for transport. Static gameplay JSON is never returned by the Student API. UI modal states are not server progress states.
