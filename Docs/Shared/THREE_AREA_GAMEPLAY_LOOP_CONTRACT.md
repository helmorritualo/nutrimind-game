# Three-Area Gameplay Loop Contract

## Purpose

This contract replaces the original five-area mission loops with one reusable three-area structure that is simpler to author, implement, test, and maintain without turning gameplay into repeated quiz panels.

Every mission remains one Unity scene with exactly three connected logical areas:

```text
Area 1 — Discover
Area 2 — Apply
Area 3 — Master
```

The stable transport phase IDs remain:

```text
discover_and_connect
practice_and_apply
resolve_and_master
```

The shorter labels are the intended learner-facing and design-facing meaning. Area 3 contains the former final challenge; there is no separate fourth area, fifth area, final-challenge scene, or final-challenge API.

## Mission learning scope

Each mission must define:

- exactly one primary competency owned by that mission;
- at most one supporting competency;
- prerequisite competency IDs when earlier learning is required;
- review competency IDs when an earlier skill is practiced again;
- one mastery-evidence statement;
- one principal mechanic family.

A later mission may review or integrate an earlier competency, but it must not present the same competency as new learning again. Mission 5 of a term may integrate Missions 1–4, but its content must label those competencies as review or integrated application.

## Shared area progression

### Area 1 — Discover

```text
Meet the guide or inspect the problem
→ observe two or three clues
→ complete one guided subject interaction
→ resolve two or three scored checks
→ perform one world action
→ collect Item 1
→ save checkpoint
→ unlock Area 2
```

Area 1 introduces the mission’s primary competency with strong guidance.

### Area 2 — Apply

```text
Explore the opened area
→ inspect related evidence
→ complete one less-guided subject interaction
→ resolve two or three application checks
→ perform one world action
→ collect Item 2
→ save checkpoint
→ unlock Area 3
```

Area 2 applies the same primary competency in a different situation. It must not introduce an unrelated grammar, health, or science lesson merely to fill the area.

### Area 3 — Master

```text
Inspect the integrated problem
→ use evidence or items from Areas 1 and 2
→ complete one mastery interaction
→ resolve up to three mastery checks
→ perform the final restoration
→ collect Item 3
→ save completion
→ complete the mission
```

Area 3 absorbs the former final challenge. Its main evidence should come from applying the competency in the world, not from a long final quiz.

## Shared runtime state chain

```text
EnterArea
→ OrientPlayer
→ Explore
→ ActivatePrimaryInteraction
→ PresentLearningEvidence
→ RunSubjectActivity
→ RunKnowledgeChecks
→ ApplyWorldAction
→ ShowWorldResult
→ SpawnCollectible
→ CommitCheckpoint
→ UnlockNextAreaOrCompleteMission
```

The reusable runtime owns loading, objectives, question attempts, feedback, collectibles, checkpoints, persistence, area unlocking, mission completion, and sync. Subject modules own only the learning activity and its world action.

## Subject loops

### LiteraQuest

```text
Explore
→ inspect story evidence
→ manipulate a story artifact
→ confirm understanding
→ restore the story world
→ collect a Story Fragment
```

Typical principal interactions include arranging event markers, repairing a caption, matching dialogue to a character, selecting supporting evidence, or assembling a short story artifact.

Each area has one main story source and one main manipulation interaction. Do not build several independent reading stations inside one area.

### PE & Health

```text
Explore
→ observe a health or safety situation
→ identify relevant clues
→ choose a healthy or safe action
→ perform the action
→ observe the consequence
→ collect a Wellness Symbol
```

Answering alone is insufficient. The learner must perform a predefined in-world action and see the NPC or environment change. Use safe educational scenarios; do not diagnose, prescribe, or replace professional guidance.

### Science

```text
Explore
→ observe
→ predict
→ perform one investigation
→ record one result
→ conclude
→ apply the solution
→ collect a Science Evidence Token
```

Grade 5 uses guided, visual, single-action investigations. Grade 6 may add variables, measurements, repeated trials, and evidence-supported conclusions. Predictions are recorded but are not graded as failed answers.

## Question policy

- Default: two or three scored checks per area.
- Hard maximum: four scored checks per area.
- Science may additionally include one unscored prediction.
- Scored closed-answer questions allow no more than two attempts.
- First incorrect attempt shows a focused hint inside the active learning overlay.
- Second incorrect attempt reveals the correct concept, records `review_required`, and continues.
- No lives, mission restart, or full quiz repetition.
- Review-required items appear in the optional objective/journal drawer and the final mission result; they do not require a separate area-review modal.

## Minimal gameplay UI policy

Blocking gameplay UI is limited to:

1. mission introduction before entering or immediately after scene load;
2. one reusable learning-and-question overlay;
3. pause menu;
4. mission-complete result;
5. optional exit confirmation.

The reusable learning overlay changes state to present evidence, questions, first-attempt hints, second-attempt explanations, and required acknowledgement. Do not create separate modal prefabs for learning clue, question, reminder, correct answer, area completion, investigation completion, healthy-choice completion, and review.

Use non-modal presentation for:

- NPC subtitles;
- interaction prompts;
- current objective and `1/3` collectible HUD;
- direction and world markers;
- short correct-answer feedback;
- area-restored banner;
- collectible reveal;
- checkpoint toast;
- gates, paths, machines, lighting, NPC state, and other world results.

The environment should communicate progress wherever practical. Collecting the item, seeing the world change, and opening the next route replace an Area Complete modal.

## Environment authoring contract

Each area requires only:

```text
AreaRoot
PlayerEntry
GuideNPC or PrimaryStation
CluePoint01
CluePoint02
PrimaryInteraction
LearningOverlayTrigger
WorldAction
WorldResult
CollectibleSpawn
Checkpoint
NextAreaGate
```

Area 3 additionally requires:

```text
MasteryInteraction
MissionCompletionTrigger
```

A mission may reuse a free modular environment asset family. Greybox layout owns navigation and pedagogy; third-party assets provide visual dressing and must not dictate the mission flow.

## Persistence and server boundary

Unity persists learner-scoped mission, area, interaction, question outcome, review, world-state, collectible, checkpoint, and outbox data. Static dialogue, questions, answer keys, learning clues, and environment definitions remain versioned Unity content.

The server tracks canonical learner progress, classroom availability, revisions, events, collectibles, and reporting facts. It does not track modal states or require separate events for opening hints, closing panels, or showing an area-complete banner.
