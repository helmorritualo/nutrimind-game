# Prompt 1 foundation tests

## Layout

- `Assets/NutriMind/Tests/EditMode/**` — NUnit EditMode coverage
- `Assets/NutriMind/Tests/PlayMode/**` — UnityTest PlayMode scene flows
- `Assets/NutriMind/Tests/TestData/**` — `TestDatabaseFactory`, fakes, helpers

## Assemblies

- `NutriMind` — first-party runtime scripts under `Assets/NutriMind` (excluding nested test/editor asmdefs)
- `NutriMind.Editor` — Editor tools
- `NutriMind.Tests.EditMode` / `NutriMind.Tests.PlayMode` / `NutriMind.Tests.TestData`

## PlayMode notes

PlayMode scene tests need `SCN_App_*` in Build Settings (0–3). They use `[Timeout]` attributes.

AppLifetime currently opens the default persistent DB path and stores tokens in memory. Offline-eligible coverage therefore re-runs `AppStartupCoordinator` on the live lifetime after toggling `IConnectivityService` (true process restart would drop the in-memory token in Prompt 1).

If the Unity Test Runner cannot auto-run PlayMode scene loads in batch/CI, execute them from the Editor **Test Runner → PlayMode** tab.

## Mock credentials

- LRN `123456789012`
- PIN `1234`

Mock latency in EditMode gateway tests is `0/0` except cancellation coverage.
