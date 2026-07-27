# Rebuild Decisions

The following legacy directions are superseded:

- five areas per mission or one scene per area;
- a separate final-challenge scene, route, or progress record;
- several independent learning stations inside every optimized area;
- five scored questions as the normal area target;
- separate learning-clue, reminder, review, all-correct, area-complete, and learning-summary modals;
- server-managed static gameplay dialogue, questions, or answer keys;
- static gameplay answer keys in Student API payloads;
- environment layout dictated by a purchased or free demo scene;
- mission content that teaches several unrelated competencies as new learning in one mission.

The rebuild uses:

- one mission scene with exactly three logical areas;
- Area 1 Discover, Area 2 Apply, and Area 3 Master;
- Area 3 as the integrated final challenge;
- one primary competency and at most one supporting competency per mission;
- explicit prerequisite, review, and competency-ownership metadata;
- one principal subject interaction and one world action per area;
- two or three scored checks per area by default, maximum four;
- one reusable learning-and-question overlay instead of several gameplay modals;
- non-modal subtitles, prompts, markers, feedback, collectible reveal, and checkpoint toast;
- UI Toolkit for application screens and the small set of complex gameplay overlays;
- uGUI for lightweight HUD, feedback, subtitles, and world-space markers;
- one gameplay input/modal coordinator across both UI systems;
- developer-editable static gameplay JSON packaged with Unity;
- SQLite for learner-scoped local progress, question outcomes, checkpoints, world state, and sync outbox;
- a Laravel server for identity, classroom mission policy, canonical progress, reporting, and sync;
- a separate server-managed Quiz Portal;
- greybox-first level design with a consistent free modular environment family used only as visual dressing.
