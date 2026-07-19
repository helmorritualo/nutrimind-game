# Shared Student API Contract

Canonical prefix:

```text
/api/v1/student
```

`openapi.yaml` is authoritative.

Rebuild-specific rules:

- every mission has exactly three areas;
- Area 3 includes the integrated final challenge;
- there are no final-challenge endpoints;
- completing Area 3 may complete the mission atomically;
- static gameplay answer keys remain in Unity;
- Quiz Portal scoring remains on the server.
