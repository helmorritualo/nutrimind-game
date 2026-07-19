# Application Screen Specification

## Application shell

Use a shared UI Toolkit application shell containing:

```text
AppRoot
├── GlobalLoadingLayer
├── GlobalToastLayer
├── GlobalModalLayer
├── TopBar
├── NavigationRegion
├── ContentRegion
└── OfflineSyncBanner
```

Use screen routes rather than enabling many hidden UIDocuments.

## Bootstrap

States:

```text
initializing_local_storage
checking_secure_token
checking_connectivity
checking_client_version
checking_manifest
loading_bootstrap
offline_eligible
authentication_required
maintenance
required_update
recoverable_error
ready
```

## Login

Fields:

```text
LRN
PIN
Show PIN
Login
Help
Privacy
```

Behavior:

- client-side format validation;
- server error-code mapping;
- rate-limit countdown;
- never persist PIN;
- clear PIN after failure/success;
- route to Main after bootstrap.

## Home

Sections:

- greeting and grade;
- Continue Mission;
- subject cards;
- Quiz Portal assignments;
- overall progress;
- announcement preview;
- latest reward/certificate;
- sync status.

## Subject and mission browsing

Flow:

```text
Subjects
→ Terms
→ Mission list/map
→ Mission detail
→ Play/Continue
```

Mission card shows:

- title;
- status;
- three-area progress;
- three collectible progress;
- locked reason;
- downloaded/offline availability.

## Profile

- Student display name;
- LRN masked when displayed;
- grade;
- section;
- school year;
- avatar selection when supported;
- sign out.

## Settings

- music;
- ambient;
- SFX;
- dialogue;
- text size;
- motion reduction;
- input sensitivity;
- graphics preset;
- language when localization is added;
- privacy/about.

Settings save locally immediately and synchronize only when part of the server contract.

## Progress

- overall grade progress;
- subject progress;
- term progress;
- mission list;
- review-required indicators;
- Quiz Portal summary.

## Rewards and certificates

Rewards:

- owned;
- available;
- used;
- locked reason.

Certificates:

- list;
- issue date;
- eligibility description;
- open/download when supported.

## Announcements

- visible publication window;
- unread state locally;
- empty/offline state;
- no HTML execution from server content.

## Leaderboard

- metric;
- period;
- privacy-safe name;
- Student's own position;
- empty/unavailable state.

## Quiz Portal

Specified separately in `QUIZ_PORTAL_AND_STATIC_QUESTION_SYSTEM.md`.

## State requirements

Every data screen has:

```text
Loading
Content
Empty
OfflineCached
OfflineUnavailable
RecoverableError
PermissionOrLocked
```

No screen may remain blank while data is loading or absent.
