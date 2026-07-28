# G5 LQ T1 M01 — Environment Blockout Design

**Date:** 2026-07-28  
**Scene:** `Assets/NutriMind/Missions/Grade5/LiteraQuest/G5_LQ_T1_M01/Scenes/SCN_G5_LQ_T1_M01_TheFestivalStorybookRescue.unity`  
**Status:** Implemented on branch `feat/g5-lq-t1-m01-environment-blockout` (2026-07-28)  
**Related:** `Docs/CurrentMilestone/G5_LITERAQUEST_T1_M1_FULL_DESIGN.md`, `Docs/Shared/THREE_AREA_GAMEPLAY_LOOP_CONTRACT.md`

## Goal

Block out the full three-area festival corridor in the mission scene so the team can walk Story Square → Banner Market Lane → Chronicle Courtyard. Use a lightly flattened copy of the Polytope `Environment_Free` terrain look, dressed with already-imported kits (Polytope Village/Environments, Medieval Village MegaKit, Fantasy Props MegaKit).

This pass is **environment + named anchors only**. No gameplay wiring, UITK, or production NPC animation.

## Decisions locked

| Decision | Choice |
| -------- | ------ |
| Completeness | **B** — all three areas visually blocked out; Area 1 denser props; polish pass later |
| Footprint | **A** — compact ~90 m south→north play corridor |
| Approach | **1** — terrain-first, kit-dress second |
| Demo assets | Duplicate TerrainData into mission `Environment/`; do not mutate Polytope demo TerrainData in place |
| Ground | Lightly flat (readable walk); mild slopes only; dirt path splat through corridor |

## World layout (south → north)

```
Player Spawn / PlayerEntry_A01
        │
        ▼
┌───────────────────────────────┐
│ AREA 1 — STORY SQUARE (~28 m) │
│ Open circular festival square │
└──────────────┬────────────────┘
               │ Gate 1 (~8 m)
               ▼
       ┌────────────────────────────┐
       │ AREA 2 — BANNER MARKET (~30 m)
       └─────────────┬──────────────┘
                     │ Gate 2 (~6 m)
                     ▼
           ┌──────────────────────────┐
           │ AREA 3 — CHRONICLE (~28 m)
           └──────────────────────────┘
```

Approximate world origin: Area 1 center near `(0, 0, 0)`; corridor axis +Z (north).

## Scene hierarchy

```
_LIGHTING
├── DirectionalLight
├── GlobalVolume
├── ReflectionProbe_StorySquare
└── ReflectionProbe_Chronicle

_ENVIRONMENT
├── SharedTerrain
├── SharedBackground
├── A01_StorySquare
├── A02_BannerMarketLane
└── A03_ChronicleCourtyard

_INTERACTIONS          (empty transforms / simple markers; exact names below)
_COLLECTIBLES
_PLAYER                (reserved; spawn faces Farmer Lira)
```

Existing default `Main Camera` / loose `Directional Light` may be absorbed into `_LIGHTING` or removed once the structured light exists.

## Terrain plan

1. Copy 1–2 demo terrain tiles (or a merged working TerrainData) into  
   `Assets/NutriMind/Missions/Grade5/LiteraQuest/G5_LQ_T1_M01/Environment/Terrain/`.
2. Flatten height samples toward a near-level play plane (keep subtle variation).
3. Parent Terrain object(s) under `_ENVIRONMENT/SharedTerrain`.
4. Paint grass base + dirt path along corridor and square/courtyard rings.
5. Thin tree/detail density on the play strip; keep denser foliage in `SharedBackground` margins.

## Area visual blockout

### Area 1 — Story Square (Discover) — densest this pass

- Large acacia-like tree (west/side); central damaged storybook stand focal.
- GuideNPC_FarmerLira placeholder left of stand.
- 2–3 village building masses forming square edge (Medieval / Polytope).
- Festival bunting, benches, crates, barrels, pots, cloth props (Fantasy Props).
- Closed gate north (`NextAreaGate_A01_A02`).
- Strong sightline: `PlayerEntry_A01` (south) → Lira + storybook.

**Required anchors**

| Name | Placement |
| ---- | --------- |
| PlayerEntry_A01 | South edge, facing Lira |
| GuideNPC_FarmerLira_A01 | Left of storybook stand |
| CluePoint01_OpeningIllustration | Left storybook page |
| CluePoint02_SurvivingLines | Right storybook page |
| PrimaryInteraction_DamagedStorybook | Central stand |
| CaptionRepairBoard | On/beside stand |
| QuestionTrigger_A01 | Invisible, in front of stand |
| WorldResult_RepairedStorybook | Disabled repaired stand |
| CollectibleSpawn_Fragment01 | Above/behind repaired stand |
| Checkpoint_A01 | Between stand and exit |
| NextAreaGate_A01_A02 | North side |

### Area 2 — Banner Market Lane (Apply)

- Narrow lane, 4–6 stalls alternating L/R.
- Cloth banners above route.
- Mina placeholder at first stall; three separated clue props.
- Sequencing board at lane end; closed arch to Area 3.

**Required anchors**

| Name | Placement |
| ---- | --------- |
| PlayerEntry_A02 | Just after Gate 1 |
| GuideNPC_Mina_A02 | First market stall |
| CluePoint01_ChildrenGather | Early lane, left |
| CluePoint02_StorybookOpened | Mid lane, right |
| CluePoint03_CaptionRepaired | Final lane segment |
| PrimaryInteraction_EventSequenceBoard | End of lane |
| EventSlot_Beginning / Middle / End | Left / center / right board slots |
| QuestionTrigger_A02 | In front of board |
| WorldResult_RestoredBannerRoute | Disabled; raised banners + open arch |
| CollectibleSpawn_Fragment02 | Above completed board |
| Checkpoint_A02 | Before Gate 2 |
| NextAreaGate_A02_A03 | End archway |

### Area 3 — Chronicle Courtyard (Master)

- Wider ceremonial court; raised stage/pavilion; Chronicle display at far (north) end.
- Chapter assembly table in foreground; sparse audience props.
- Décor starts dim/lowered; `WorldResult_RestoredCourtyard` holds celebration variant (disabled).

**Required anchors**

| Name | Placement |
| ---- | --------- |
| PlayerEntry_A03 | Courtyard entrance |
| GuideNPC_FarmerLira_A03 | Near chapter table |
| PrimaryInteraction_ChapterAssembly | Center foreground |
| ChapterSlot_Beginning / Middle / Ending | Left / center / right |
| FinalChallenge_EndingSelection | Same table |
| QuestionTrigger_A03 | In front of table |
| WorldAction_PresentChapter | Between table and Chronicle |
| WorldResult_RestoredCourtyard | Disabled celebration set |
| CollectibleSpawn_Fragment03 | Above Chronicle |
| Checkpoint_A03 | Near stage |
| MissionCompletionTrigger | At final Chronicle interaction |

## Asset sources (dressing)

- **Terrain look / foliage:** Polytope Lowpoly Environments (+ demo terrain layers as references).
- **Village modular / fences / bridge motifs:** Polytope Lowpoly Village + Medieval Village MegaKit FBX.
- **Props / stalls / cloth / barrels:** Fantasy Props MegaKit (+ Medieval props as needed).
- **Materials:** Prefer `Assets/Materials/Fixes` URP remaps already applied for MegaKits; Polytope materials must stay URP Lit (not Built-in Amplify buildings shader).

## Out of scope (this pass)

- Mission runtime, JSON binding, SQLite, sync.
- UI Toolkit / HUD / GameplayUiCoordinator wiring.
- Final character meshes, VO, VFX polish beyond simple placeholders.
- Separate Area 1 art-polish pass (follows after blockout review).

## Acceptance checklist

- [ ] Mission scene opens with `_LIGHTING` and `_ENVIRONMENT` hierarchy as specified.
- [ ] SharedTerrain is a lightly flat corridor ~90 m S→N with dirt path.
- [ ] A01 / A02 / A03 contain readable massing (square / lane / courtyard).
- [ ] All required anchor names exist and are positioned per tables above.
- [ ] WorldResult_* objects exist and start inactive.
- [ ] Demo Polytope TerrainData assets under `Lowpoly_Demos` are unchanged.
- [ ] No pink/error shaders on placed kit instances.
- [ ] Scene remains one mission scene with exactly three logical areas (no additive area scenes).

## Spec self-review

- No unresolved placeholders for hierarchy names or area order.
- Aligns with existing mission full design (three areas, fragment collectibles, Area 3 integrated mastery).
- Scope limited to environment blockout; gameplay deferred intentionally.
- Conflict note: full design mentions ProBuilder greybox option; this spec chooses imported-kit + terrain copy per explicit user request.
