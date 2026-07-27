# Three-Area Progress Contract

The three logical design roles are Discover, Apply, and Master. Existing phase enum values remain unchanged for compatibility.

## Mission definition

Every published static gameplay mission has exactly three areas.

```text
a01 discover_and_connect
a02 practice_and_apply
a03 resolve_and_master
```

## Mission states

```text
locked
available
started
in_progress
review_required
mission_completed
```

## Area states

```text
locked
available
started
in_progress
review_required
collectible_unlocked
collectible_collected
completed
```

## Area completion

Area completion requires:

- valid grade and mission;
- valid manifest version;
- correct prerequisite area;
- required interactions;
- required question resolutions;
- review acknowledgement when required;
- required collectible;
- duplicate-safe event UUID.

Area 3 completion may atomically set the mission to `mission_completed`.

## Client events

Recommended semantic events:

```text
mission.started
area.started
learning_clue.viewed
question.attempted
question.hint_shown
question.review_marked
subject_action.completed
collectible.collected
area.completed
mission.completed
```

The server validates IDs and state transitions. It does not re-score static Unity answer keys.

## Static gameplay content boundary

Sync and mission DTOs contain stable IDs, progress states, outcomes, revisions, and availability. They never contain static dialogue, question text, options, answer keys, hints, or world actions.

## UI-state exclusion

Canonical progress does not include learning-overlay state, hint-open state, reminder acknowledgement UI, area-complete banners, or learning-summary screens. Unity reports resolved question outcomes, review-required state, world action, collectible, checkpoint, area completion, and mission completion.
