# Mission Open-World Architecture

Each mission is one scene with exactly three connected logical areas:

```text
Area 1 — Discover
Area 2 — Apply
Area 3 — Master + integrated final challenge
```

Stable runtime phase IDs remain `discover_and_connect`, `practice_and_apply`, and `resolve_and_master` for API compatibility.

## Optimized area runtime

```text
Enter or resume area
→ follow in-world guidance
→ observe/read two or three required clues
→ activate one NPC, object, or station
→ run the subject-specific interaction
→ open the reusable learning-and-question overlay only when needed
→ resolve two or three scored checks, maximum four
→ show hint/explanation in the same overlay when required
→ close the overlay and apply the subject action in world
→ show a visible world result
→ reveal and collect one mission item
→ commit learner-scoped SQLite checkpoint + sync outbox event
→ show a lightweight checkpoint toast
→ unlock the next area in the same scene
```

Do not insert a second mission introduction, separate all-correct panel, area-review modal, area-complete modal, learning-summary modal, second loading screen, or collectible ceremony between small tasks.

## Subject actions

- LiteraQuest inspects story evidence and manipulates one story artifact.
- PE & Health identifies a health/safety situation and performs one safe action with a visible consequence.
- Science observes, predicts when appropriate, performs one investigation, records one result, concludes, and applies the solution.

## Environment contract

Every area contains `AreaRoot`, `PlayerEntry`, a guide or station, two clue points, one primary interaction, one overlay trigger, one world action/result, one collectible spawn, one checkpoint, and one next-area gate. Area 3 adds the mastery interaction and mission-completion trigger.

Greybox geometry owns traversal and learning flow. Free modular assets may dress the greybox later but must not determine progression.

## Checkpoint transaction

The local transaction persists learner ID, required interaction states, question outcomes, review state, world action, collectible, area completion, checkpoint transform, content version, and one idempotent outbox event. Area 3 commits mission completion in the same transaction as its final area completion. UI overlay states are not persisted as canonical progress.
