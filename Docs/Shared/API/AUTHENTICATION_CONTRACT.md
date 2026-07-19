# Authentication Contract

## Login

```text
POST /api/v1/student/auth/login
```

```http
Accept: application/json
Content-Type: application/json
X-Client-Version: 1.0.0
X-Device-ID: <installation UUID>
```

```json
{
  "lrn": "123456789012",
  "pin": "1234",
  "device_name": "Android Tablet"
}
```

## Policy

- API v1 uses bearer tokens.
- API v1 has no refresh-token endpoint.
- `expires_at` is nullable and authoritative.
- Login revokes the previous token for the same Student/device.
- Logout revokes the current token.
- Account deactivation and PIN reset revoke all Student tokens.

## Unity behavior on token 401

1. Clear the invalid token.
2. Preserve SQLite progress and outbox.
3. Stop authenticated retries.
4. Return to login.
5. Sync after reauthentication.

Do not store PINs or bearer tokens in gameplay SQLite.

`X-Device-ID` is a random installation UUID, not a hardware identifier.
