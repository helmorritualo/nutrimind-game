# Stable Error-Code Catalog

Unity branches on `error.code`, not `error.message`.

| Code | HTTP | Meaning |
|---|---:|---|
| AUTH_INVALID_CREDENTIALS | 401 | Invalid LRN/PIN |
| AUTH_TOKEN_MISSING | 401 | No bearer token |
| AUTH_TOKEN_INVALID | 401 | Invalid token |
| AUTH_TOKEN_REVOKED | 401 | Revoked token |
| ACCOUNT_INACTIVE | 403 | Inactive Student |
| ROLE_NOT_STUDENT | 403 | Wrong account role |
| GRADE_CONTEXT_MISMATCH | 403 | Wrong grade context |
| RESOURCE_NOT_FOUND | 404 | Missing or out of scope |
| MISSION_NOT_FOUND | 404 | Unknown mission |
| AREA_NOT_FOUND | 404 | Unknown area |
| QUIZ_NOT_FOUND | 404 | Unknown quiz |
| ATTEMPT_NOT_FOUND | 404 | Unknown attempt |
| CERTIFICATE_NOT_FOUND | 404 | Unknown certificate |
| MISSION_LOCKED | 409 | Mission unavailable |
| AREA_LOCKED | 409 | Area unavailable |
| INVALID_PROGRESS_TRANSITION | 409 | Out-of-order progress |
| MISSION_CHALLENGE_INCOMPLETE | 409 | Area 3 integrated challenge incomplete |
| IDEMPOTENCY_PAYLOAD_MISMATCH | 409 | UUID reused with different payload |
| STALE_CLIENT_REVISION | 409 | Client state is stale |
| MANIFEST_AREA_COUNT_INVALID | 409/422 | Published mission does not contain exactly three areas |
| MANIFEST_VERSION_UNSUPPORTED | 409/426 | Manifest incompatible |
| CLIENT_VERSION_UNSUPPORTED | 426 | App update required |
| QUIZ_NOT_AVAILABLE | 409 | Quiz not assigned or available |
| QUIZ_NOT_OPEN | 409 | Quiz not open |
| QUIZ_CLOSED | 409 | Quiz closed |
| ATTEMPT_LIMIT_REACHED | 409 | No attempts remain |
| RESULT_NOT_VISIBLE | 403/409 | Result hidden by policy |
| REWARD_NOT_AVAILABLE | 409 | Reward unavailable |
| REWARD_ALREADY_USED | 409 | Reward already used |
| SYNC_BATCH_TOO_LARGE | 413 | Batch bytes too large |
| SYNC_EVENT_LIMIT_EXCEEDED | 413 | Too many events |
| SYNC_EVENT_TOO_OLD | 422 | Event older than limit |
| SYNC_EVENT_TYPE_UNSUPPORTED | 422 | Unknown event type |
| VALIDATION_FAILED | 422 | Invalid request |
| RATE_LIMITED | 429 | Retry after limit window |
| SERVER_BUSY | 503 | Temporary capacity issue |
| SERVICE_UNAVAILABLE | 503 | Temporary outage |
| INTERNAL_ERROR | 500 | Unexpected failure |

## Required details

- Validation details map field paths to message arrays.
- Rate-limit details may include `retry_after_seconds`.
- Conflict responses may include canonical state and revision.
- Messages may be localized later; codes remain stable.
