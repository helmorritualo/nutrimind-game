# G5 LQ T1 M01 Environment Blockout Implementation Plan

> **For agentic workers:** Implement task-by-task in Unity Editor via MCP `Unity_RunCommand`. Checkboxes track progress.

**Goal:** Block out the compact three-area festival corridor in `SCN_G5_LQ_T1_M01_TheFestivalStorybookRescue` with lightly flattened terrain, kit dressing, and exact gameplay anchor names.

**Architecture:** Duplicate Polytope demo TerrainData into the mission `Environment/` folder, flatten for walkability, parent under `_ENVIRONMENT/SharedTerrain`, then dress A01→A02→A03 with imported Prefab/FBX instances and empty named anchor transforms.

**Tech Stack:** Unity 6, URP, Polytope + Medieval Village MegaKit + Fantasy Props MegaKit, mission scene under `Assets/NutriMind/Missions/.../G5_LQ_T1_M01/`.

## Global Constraints

- One mission scene; exactly three logical areas; Area 3 includes mastery (no separate final scene).
- Do not mutate Polytope demo TerrainData in place — copy first.
- Keep MegaKit materials on URP Lit (`Assets/Materials/Fixes`); do not reassign Built-in Amplify buildings shader.
- Preserve developer scene rules: additive hierarchy only; use specified root names.
- Anchors use exact hierarchy names from the approved spec.
- WorldResult_* start inactive.
- No gameplay/SQLite/UI wiring in this pass.

---

### Task 1: Terrain copy + flatten

**Files:**
- Create: `Assets/NutriMind/Missions/Grade5/LiteraQuest/G5_LQ_T1_M01/Environment/Terrain/TD_G5_LQ_T1_M01_Shared.asset` (copied)
- Modify: mission scene

- [x] Copy `New Terrain.asset` (or best center tile) via `AssetDatabase.CopyAsset`
- [x] Flatten heights toward ~0.02–0.05 normalized (lightly flat)
- [x] Place Terrain at world origin covering ~100×100 playable; parent under `SharedTerrain`
- [x] Verify scene has 1 terrain, no pink materials on terrain layers

### Task 2: Scene hierarchy + lighting shells

**Files:**
- Modify: `.../SCN_G5_LQ_T1_M01_TheFestivalStorybookRescue.unity`

- [x] Create `_LIGHTING`, `_ENVIRONMENT`, `_INTERACTIONS`, `_COLLECTIBLES`, `_PLAYER`
- [x] Under `_LIGHTING`: DirectionalLight, GlobalVolume (empty Volume OK), ReflectionProbe_StorySquare, ReflectionProbe_Chronicle
- [x] Under `_ENVIRONMENT`: SharedTerrain, SharedBackground, A01_StorySquare, A02_BannerMarketLane, A03_ChronicleCourtyard
- [x] Remove or reparent default loose Main Camera / Directional Light into structure
- [x] Save scene

### Task 3: Area 1 blockout + A01 anchors

- [x] Place acacia-like tree, storybook stand proxy, 2–3 buildings, props, north gate under `A01_StorySquare`
- [x] Create all A01 anchors under `_INTERACTIONS` / `_COLLECTIBLES` at specified relative positions
- [x] `WorldResult_RepairedStorybook` inactive
- [x] Sightline: PlayerEntry_A01 south looking +Z toward stand

### Task 4: Area 2 + Gate path + A02 anchors

- [x] Decorated Gate 1 path props between A1 north and A2 south
- [x] 4–6 stalls, banners, Mina placeholder, sequence board, arch under `A02_BannerMarketLane`
- [x] Create all A02 anchors; `WorldResult_RestoredBannerRoute` inactive

### Task 5: Area 3 + A03 anchors

- [x] Stage, Chronicle display, chapter table, sparse audience under `A03_ChronicleCourtyard`
- [x] Create all A03 anchors; `WorldResult_RestoredCourtyard` inactive
- [x] Save scene; screenshot / hierarchy verify acceptance checklist

### Task 6: Acceptance verify

- [x] All required anchor names present (FindObjects / hierarchy dump)
- [x] No InternalErrorShader on renderers
- [x] Demo TerrainData paths unchanged
- [x] Scene saved

---

## Spec coverage

| Spec item | Task |
| --------- | ---- |
| Terrain copy + flatten | 1 |
| Hierarchy `_LIGHTING` / `_ENVIRONMENT` | 2 |
| A1 massing + anchors | 3 |
| A2 massing + anchors | 4 |
| A3 massing + anchors | 5 |
| Acceptance checklist | 6 |
