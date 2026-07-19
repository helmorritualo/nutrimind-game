# Idempotency and Synchronization Contract

## UUID fields

```text
client_attempt_uuid  Quiz Portal attempt
event_uuid           Direct gameplay mutation
batch_uuid           Sync batch
event_uuid           Sync event
request_uuid         Reward use
```

No generic `Idempotency-Key` header is used in v1.

## Replay

Same UUID and same normalized payload:

- return original committed result;
- never duplicate state, rewards, collectibles, events, mail, or jobs.

Same UUID and different payload:

```text
409 IDEMPOTENCY_PAYLOAD_MISMATCH
```

A network timeout must retry with the same UUID and full original payload.

## SQLite outbox states

```text
pending
sending
accepted
duplicate
rejected
deferred
```

Atomic local change:

```text
begin SQLite transaction
→ update local progress
→ insert outbox event
→ commit
→ update UI
```

## Default sync limits

The authoritative values come from `/api/v1/student/config`.

Defaults:

```text
100 events
512 KiB request
16 KiB event payload
90-day event age
```

## Ordering and canonical state

- Send ascending `local_sequence`.
- Server revision is authoritative.
- Client timestamps are informational.
- Completion is monotonic.
- Process each event transactionally.
- Return accepted/duplicate/rejected/deferred per event.
- Preserve successful events when another event fails.

Static gameplay correctness is client-reported. Quiz Portal scoring is server-authoritative.
