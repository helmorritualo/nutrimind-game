# Static Gameplay Content Schema

## Authority

`GAMEPLAY_CONTENT_CATALOG_V1.json` indexes one developer-editable JSON file per mission under `StaticGameplayContent/`.

Static mission JSON owns learner-facing mission content:

- premise and objective;
- area story and dialogue;
- learning clues;
- question text, options, answer keys, hints, explanations, and feedback;
- subject action and world result;
- collectible definition.

The server never delivers or edits these fields.

## Loading

1. Load the catalog packaged with the Unity build.
2. Validate the selected mission file and its SHA-256/catalog entry.
3. Require exactly three unique areas in order 1–3.
4. Require unique question IDs and valid answer-key option IDs.
5. Reject a mission pack that exceeds five scored questions in any area.
6. Save the loaded content version with local progress so migrations can be handled explicitly.

A developer may add or replace a mission JSON file, then update the catalog entry and content version. Stable mission, area, interaction, and question IDs must not be changed after learner progress exists.

## SQLite boundary

SQLite stores state, not duplicated authored content:

```text
mission_state
area_state
interaction_state
question_attempt_and_outcome
review_state
world_state
collectible_state
checkpoint_state
content_version_used
sync_outbox_event
```

Do not copy dialogue, question text, option text, or answer keys into SQLite as authoritative content. Store stable IDs and outcomes.

Commit the local state change and its duplicate-safe sync outbox event in one SQLite transaction.

## Question behavior

Canonical types:

```text
multiple_choice_single
multiple_choice_multiple
true_false
prediction_single_unscored
```

Scored closed-answer questions permit at most two attempts. The first incorrect attempt shows a focused hint. The second incorrect attempt reveals the correct concept, records review-required, and continues. Predictions are recorded but not graded.

## Release status

The package is technically complete and implementation-ready. Learner-facing curriculum content still requires review and approval by the project’s curriculum owner before production release.

## Authoring status

Each pack declares one of:

```text
milestone_authored_exact_content
future_mission_structured_curriculum_draft
```

Both statuses are loadable. Neither bypasses the curriculum-owner production-release gate.
