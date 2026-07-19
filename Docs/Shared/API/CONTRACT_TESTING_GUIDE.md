# Contract Testing Guide

## Laravel

For every v1 endpoint assert:

- method/path and required headers;
- authentication, active status, grade, and scope;
- exact envelope;
- field types and nullability;
- stable error code;
- public IDs as strings;
- UTC timestamps;
- pagination;
- no secrets or answer keys;
- duplicate/conflict behavior;
- critical query budgets.

## Unity

Deserialize every JSON fixture into typed DTOs and assert:

- stable IDs;
- Grade 5/6 isolation;
- null handling;
- pagination and revision;
- error-code mapping;
- unknown additive fields ignored;
- no answer-key DTO field.

## Drift prevention

CI should compare Laravel `/api/v1/student` routes with `openapi.yaml` and fail on missing or extra contract routes.

A contract change must update:

1. OpenAPI;
2. fixtures;
3. Laravel contract tests;
4. Unity DTO/serialization tests;
5. changelog.
