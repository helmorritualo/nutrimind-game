# Gameplay Static Content Schema

## Ownership

One developer-editable JSON file is packaged with Unity per mission. The server never returns this authored content.

A mission pack contains:

- stable mission, grade, subject, and term IDs;
- title, curriculum block, premise, and objective;
- competency contract;
- one scene contract with exactly three logical areas;
- dialogue and learning clues;
- one principal subject activity per area;
- question text, options, answer keys, hints, explanations, and feedback;
- world action/result and collectible definition;
- progress-key declarations.

## Competency contract

Required fields:

```text
primary_competency_id
primary_competency_summary
supporting_competency_ids
prerequisite_competency_ids
review_competency_ids
mastery_evidence
mechanic_family
```

`primary_competency_id` must be unique to its owning mission. Reused learning in later missions must appear under prerequisite or review IDs rather than as a second owner.

## Structural validation

1. Require exactly three areas in orders 1, 2, and 3.
2. Require phases `discover_and_connect`, `practice_and_apply`, and `resolve_and_master` in that order.
3. Require one collectible and checkpoint contract per area.
4. Require unique interaction, question, option, and collectible IDs.
5. Require valid answer-key option IDs.
6. Recommend two or three scored checks and reject more than four scored checks in any area.
7. Permit at most one additional `prediction_single_unscored` record in a Science area.
8. Require at most two attempts for scored closed-answer questions and one attempt for unscored predictions.
9. Require one principal world action and one visible world result per area.
10. Require Area 3 to complete the mission without a separate final-challenge content object.

## Versioning

A developer may add or replace a mission JSON file, then update the catalog entry and content version. Stable mission, area, interaction, collectible, competency, and question IDs must not change after learner progress exists.

## Runtime versus authored content

SQLite stores stable IDs and learner outcomes only:

```text
mission_state
area_state
interaction_state
question_attempt_and_outcome
review_state
world_state
collectible_state
checkpoint_state
sync_outbox
```

Do not copy dialogue, question text, option text, answer keys, or learning clues into SQLite as authoritative content.

## Question behavior

Scored closed-answer questions permit at most two attempts. The first incorrect attempt shows a focused hint inside the active reusable overlay. The second reveals the correct concept, records review-required, and continues. Predictions are recorded but not graded. Review is surfaced through the objective/journal drawer and mission result, not a separate mandatory area-review modal.
