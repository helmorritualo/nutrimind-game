# Grade 5 LiteraQuest Term 1 Mission 1 — Full Design

## Identity

```text
Mission: g5_lq_t1_m01
Scene: SCN_G5_LQ_T1_M01_TheFestivalStorybookRescue
Areas: Parade Meadow → Drumbeat Lane → Freedom Stage
Collectibles: three Story Fragments
Static content: Docs/Data/StaticGameplayContent/g5_lq_t1_m01.json
```

## Premise

On the morning of Bayang Haraya’s Freedom and Friendship Festival, the opening chapter of the town storybook becomes incomplete. Events, captions, action words, and visual meaning are scattered across three connected festival zones. Farmer Lira asks the Pathfinder to restore one coherent chapter before the opening ceremony.

## Area 1 — Parade Meadow

The learner reads the opening page, identifies character, setting, goal, and first event, then repairs noun/pronoun references on the parade board. The repaired board opens the banner route and reveals Story Fragment 1.

Required questions and exact answer keys are defined in the mission JSON. They assess Farmer Lira as guide, Parade Meadow as setting, the banner goal, first-event sequence, and the pronoun reference for “They.”

## Area 2 — Drumbeat Lane

The learner repairs parade instructions using helping, linking, and transitive verbs, completes the role complement in a sentence, and replaces a misleading poster with a welcoming layout. The world action restores drum rhythm and lantern mood, revealing Story Fragment 2.

## Area 3 — Freedom Stage

The learner arranges seven mission events, selects the main idea and title, and chooses a coherent hopeful ending. The three fragments are bound and placed in the Festival Chronicle. Stage restoration and mission completion commit atomically.

## Optimized runtime

- one mission introduction only;
- one continuous interaction chain per area;
- five scored questions per area;
- no separate “all correct” completion panel;
- review panel only after required correction;
- one collectible reveal and one checkpoint toast per area;
- no area scene load;
- integrated final challenge in Area 3.

## UI

UI Toolkit:

- introduction;
- story/dialogue panel;
- question-and-answer panel;
- hint/explanation/review;
- final learning summary.

uGUI screen-space:

- HUD (`Area x/3`, `Fragments x/3`);
- objective and interaction prompts;
- immediate feedback, collectible reveal, checkpoint toast, pause, transition.

uGUI world-space:

- Farmer Lira marker;
- parade board, drum station, poster, event-card, fragment, and path markers.

## Persistence

Store stable IDs and outcomes in SQLite: mission/area state, required interactions, attempts, selected option IDs, correctness, review, world actions, fragments, checkpoint transforms, final world state, content version, and sync outbox events. Do not duplicate the authored story or answer keys into SQLite.

## Acceptance

- all static content loads from the mission JSON;
- all question keys validate;
- one scene and three areas;
- hybrid UI input is conflict-free;
- offline completion works;
- Area 3 completion and mission completion are atomic;
- synchronization is idempotent;
- server availability never supplies static content.
