# Quiz Portal and Static Gameplay Question Systems

These are separate systems.

## Static gameplay questions

Source: developer-editable mission JSON packaged with Unity.

Runtime:

- loaded and validated locally;
- displayed in gameplay through UI Toolkit screen-space panels;
- answered and scored locally;
- outcomes saved to SQLite by stable question ID;
- progress facts synchronized to the server;
- answer keys never sent to or managed by the server.

Canonical types:

```text
multiple_choice_single
multiple_choice_multiple
true_false
prediction_single_unscored
```

Use two or three scored checks per area by default and never exceed four. The first incorrect scored attempt changes the reusable learning overlay to a hint state. The second changes the same overlay to an explanation state, marks review-required, and continues without a separate review modal. A prediction is recorded but not graded.

## Quiz Portal

Source, assignments, delivery, scoring, answer protection, and results remain on the server. Unity displays Quiz Portal screens in the application layer. Quiz Portal DTOs, storage, presenters, services, and analytics do not share the static mission question model.

The gameplay revisions in v2 do not change the current Quiz Portal milestone.
