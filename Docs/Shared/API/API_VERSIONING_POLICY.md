# API Versioning Policy

## Current version

```text
/api/v1
```

## Compatible v1 changes

- Add optional response fields.
- Add optional request fields with defaults.
- Add new endpoints.
- Add filters or feature flags.
- Add safe error codes.

Unity must ignore unknown additive fields.

## Breaking changes requiring `/api/v2`

- Rename/remove routes.
- Change methods.
- Change required headers.
- Remove fields.
- Change field type or nullability incompatibly.
- Change enum meaning.
- Make optional input required.
- Change envelope, auth, or idempotency behavior.

API version, client version, and static manifest version are independent.

Every contract change updates OpenAPI, fixtures, Laravel tests, Unity tests, and the changelog.
