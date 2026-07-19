# Rebuild Decisions

## Superseded

- five areas per mission or one scene per area;
- Mission Shell plus additive area scenes;
- a separate final-challenge scene or API route;
- repeated mission introductions, micro-quiz panels, gate ceremonies, and duplicate explanations;
- uGUI for every application screen;
- UI Toolkit for in-world markers;
- uGUI-only gameplay panels when data-heavy UI Toolkit panels are more appropriate;
- server-managed static gameplay dialogue, questions, or answer keys;
- Teacher gameplay-content authoring through the classroom server;
- saving authored content as authoritative rows in gameplay SQLite.

## Accepted

- one open-world scene and exactly three logical areas per mission;
- one continuous area loop and one integrated final challenge in Area 3;
- developer-editable static JSON, one file per mission, packaged with Unity;
- SQLite for local progress, question outcomes, checkpoints, world state, and sync outbox;
- server-only classroom mission lock/unlock, tracking, reporting, and canonical synchronization for static gameplay;
- UI Toolkit for application UI and complex/data-heavy gameplay panels;
- uGUI screen-space Canvas for gameplay HUD/feedback and uGUI world-space Canvas for in-world UI;
- separate server-managed Quiz Portal;
- frozen stable IDs, content versions, validation, and contract tests.
