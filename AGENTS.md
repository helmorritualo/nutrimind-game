# Unity Agent Contract

## Non-negotiable

- One mission scene and exactly three logical areas.
- Area 3 contains the final challenge.
- Static gameplay content comes from developer-editable per-mission JSON.
- SQLite stores progress/outbox facts, not authored dialogue or answer keys.
- Server handles classroom mission availability and gameplay tracking only.
- Quiz Portal remains separate.
- Gameplay UI uses UI Toolkit for complex/data-heavy panels, uGUI screen-space Canvas for HUD/feedback, and uGUI world-space Canvas for markers.
- One modal/input coordinator controls both UI systems.
- Current milestone: complete G5 LiteraQuest T1 M1; PEH and Science T1 M1 remain data/overview only.

## Before changing code

Read the shared contract, Unity requirements, static-content schema, current milestone, mission JSON, and API contract. Preserve stable IDs and content versions. Add validation and tests for every schema or persistence change.
