# Mission Open-World Architecture

Each mission is one scene with exactly three connected logical areas.

```text
Area 1 — Discover and Connect
Area 2 — Practice and Apply
Area 3 — Resolve and Master + integrated final challenge
```

## Optimized area runtime

```text
Enter/Resume Area
→ Explore and follow in-world guidance
→ Observe/read required clues
→ Interact with NPC/object/station
→ Open the data-heavy UI Toolkit learning/question panel
→ Resolve at most five scored questions
→ Show review only when required
→ Close modal and apply the subject action in world
→ Show visible world result
→ Reveal and collect one mission item
→ Commit SQLite checkpoint + sync outbox event
→ lightweight checkpoint toast
→ unlock next area in the same scene
```

Do not insert a second mission introduction, a separate “all correct” panel, a second loading screen, or a collectible ceremony between every small task.

## Subject actions

- LiteraQuest repairs, arranges, interprets, or publishes story/text/visual content.
- PE & Health applies a safe healthy action and shows the NPC/environment result.
- Science performs a guided investigation, records evidence, forms a conclusion, and applies the scientific solution.

## Checkpoint transaction

The local transaction persists required interaction states, question outcomes, review state, world action, collectible, area completion, checkpoint transform, content version, and one idempotent outbox event. Area 3 commits mission completion in the same transaction as its final area completion.
