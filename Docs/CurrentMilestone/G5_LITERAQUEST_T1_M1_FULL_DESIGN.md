# Grade 5 LiteraQuest Term 1 Mission 1 — Full Design

## Identity

```text
Mission: g5_lq_t1_m01
Scene: SCN_G5_LQ_T1_M01_TheFestivalStorybookRescue
Environment theme: low-poly Storybook Village Festival
Areas: Story Square → Banner Market Lane → Chronicle Courtyard
Collectibles: three Story Fragments
Static content: Docs/Data/StaticGameplayContent/g5_lq_t1_m01.json
```

## Competency contract

Primary competency: identify character, setting, goal, and major events in a short narrative.

Supporting competency: use one clear noun–pronoun reference in a caption.

Mastery evidence: the learner reconstructs a coherent beginning, event sequence, and suitable ending using observed story evidence.

Topics such as noun classifications, verb-forming suffixes, several verb categories, noun complements, main-idea instruction, and visual tone/mood are moved to later owning missions. They are not required new learning in Mission 1.

## Environment and asset direction

Use a compact storybook village festival that can be greyboxed with ProBuilder and dressed with one consistent free modular village/nature asset family. The level layout owns traversal; imported demo scenes are not used as the mission map.

Only mission-specific props require custom creation: damaged storybook, three Story Fragments, event markers, caption pieces, banners, and the restored chapter display.

## Area 1 — Story Square: Discover

Meet Farmer Lira, inspect the illustrated opening, identify the character, setting, and goal, then place the correct opening caption with a clear pronoun reference. The storybook stand repairs, Story Fragment 1 appears, and the route opens.

One main source, one caption-repair interaction, and three scored checks.

## Area 2 — Banner Market Lane: Apply

Find three event clues placed along the market route, inspect them, and arrange them in chronological order. Confirm the sequence with three application checks, activate the corrected banner route, and collect Story Fragment 2.

One clue hunt, one three-position sequencing interaction, and three scored checks.

## Area 3 — Chronicle Courtyard: Master

Use the first two fragments and the restored event path to assemble a short beginning–middle–ending chapter, choose the suitable ending, and present it at the Chronicle display. The courtyard celebration activates, Story Fragment 3 appears, and Area 3/mission completion commit atomically.

One chapter-assembly interaction and three mastery checks.

## Minimal gameplay UI

Blocking UI:

- mission introduction;
- one reusable story/learning/question overlay;
- pause;
- mission-complete result;
- optional exit confirmation.

Non-modal UI:

- objective and `Fragments x/3` HUD;
- Farmer Lira subtitles;
- interaction and direction markers;
- short correct-answer feedback;
- area-restored banner;
- collectible reveal and checkpoint toast.

There is no separate learning-clue, reminder, review, all-correct, area-complete, or final-learning-summary modal.

## Persistence

Store learner-scoped stable IDs and outcomes in SQLite: mission/area state, required interactions, attempts, selected option IDs, correctness, review, world actions, fragments, checkpoint transforms, final world state, content version, and sync outbox events. Do not duplicate authored story or answer keys into SQLite.

## Acceptance

- all static content loads from the mission JSON;
- competency metadata validates;
- each area has one principal interaction and three scored checks;
- one scene and three connected areas;
- the full greybox exists before visual dressing;
- Area 1 is completed as the first vertical slice;
- hybrid UI input is conflict-free and only approved surfaces block movement;
- exit/resume restores the latest checkpoint and world state;
- Area 3 completion and mission completion are atomic;
- synchronization is learner-scoped and idempotent;
- server availability never supplies static content.
