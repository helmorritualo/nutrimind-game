# DTO and Sync Contract

## Serialization

- JSON only.
- snake_case fields.
- public IDs are strings.
- UUIDs use canonical UUID text.
- timestamps use UTC ISO-8601.
- unknown additive response fields may be ignored.
- required fields may not be silently omitted.

## Mission DTO

Mission responses expose:

```text
id
grade_id
subject_id
term_id
order
status
locked_reason
area_count = 3
progress
```

Mission detail exposes exactly three `AreaProgress` items ordered 1–3.

## Area DTO

```text
id
order
phase
state
review_required
collectible_id
collectible_collected
completed_at
```

`phase` is one of:

```text
discover_and_connect
practice_and_apply
resolve_and_master
```

## Mission completion

Area 3 completion may return:

```text
area_state = completed
mission_state = mission_completed
active_area_id = null
newly_unlocked_ids = [...]
revision = <new revision>
```

## Sync

Unity writes semantic events locally and sends them in ascending `local_sequence`.

The server returns per-event:

```text
accepted
duplicate
rejected
deferred
```

Accepted and duplicate results are terminal for that local event.

Rejected events store a stable error code and require a correction policy.

Network failure returns `sending` events to `pending`.

## Static gameplay content boundary

Sync and mission DTOs contain stable IDs, progress states, outcomes, revisions, and availability. They never contain static dialogue, question text, options, answer keys, hints, or world actions.
