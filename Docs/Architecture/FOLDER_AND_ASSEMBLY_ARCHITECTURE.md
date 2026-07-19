# Unity Folder and Assembly Architecture

```text
Assets/NutriMind/
├── Application/
├── Gameplay/
│   ├── Content/
│   │   ├── Catalog/
│   │   ├── MissionPacks/
│   │   ├── Validation/
│   │   └── EditorImport/
│   ├── Runtime/
│   ├── Persistence/
│   ├── Sync/
│   └── UI/
│       ├── UIToolkitPanels/
│       ├── UGUIHud/
│       ├── UGUIWorldSpace/
│       └── Coordination/
├── Infrastructure/
├── Shared/
└── Tests/
```

Recommended assemblies keep content contracts, runtime state, persistence, sync, UI Toolkit presenters, uGUI presenters, and Editor validation separate. Domain/content models must not depend on either UI framework or on scene GameObjects.
