# Sources and Package Notes

## Curriculum basis

Static content is organized around the Grade 5 and Grade 6 English, Physical Education/Health, and Science competencies represented in the project manifest. Before production release, the curriculum owner must validate every learner-facing mission pack against the curriculum adopted by the school and approved classroom materials.

Primary validation references:

```text
DepEd Grade 5 resources
https://lrmds.deped.gov.ph/grade/5

DepEd Grade 6 resources
https://lrmds.deped.gov.ph/grade/6

K to 12 Curriculum Guide: English
https://lrmds.deped.gov.ph/detail/5449

K to 12 Curriculum Guide: Science
https://lrmds.deped.gov.ph/detail/5459
```

Use current DepEd-issued revisions when a school adopts a newer guide. Do not copy protected lesson text verbatim. Author original learner-facing dialogue and questions aligned to the competency.

For Philippine hazard missions, verify operational facts against current PAGASA or PHIVOLCS materials before release.

## Unity UI

Official Unity references:

```text
UI Toolkit runtime UI
https://docs.unity3d.com/6000.3/Documentation/Manual/UIElements.html

Runtime UI event system
https://docs.unity3d.com/6000.3/Documentation/Manual/UIE-Runtime-Event-System.html

World-space UI
https://docs.unity3d.com/6000.3/Documentation/Manual/ui-systems/world-space-ui.html
```

The project uses UI Toolkit for application screens and complex/data-heavy gameplay panels. uGUI remains the selected system for lightweight gameplay HUD, animated feedback, and world-space Canvas markers. Both systems coexist through one input and modal-coordination boundary.

## SQLite

```text
SQLite transactional behavior
https://www.sqlite.org/transactional.html
```

Use one supported Unity-compatible SQLite solution after target-platform validation. Commit local mission state and its duplicate-safe sync outbox event together. Authored dialogue and answer keys remain in versioned JSON files.

## Third-party UI design system

```text
https://github.com/sinanata/unity-ui-document-design-system
```

The package may be used behind NutriMind-owned UXML/USS wrappers. Pin an exact version or commit, keep vendor files unmodified, and verify compatibility with the frozen Unity 6 version.
