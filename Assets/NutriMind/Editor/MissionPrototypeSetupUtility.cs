using System;
using System.Collections.Generic;
using NutriMind.Gameplay.Runtime;
using NutriMind.Gameplay.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace NutriMind.Editor
{
    public static class MissionPrototypeSetupUtility
    {
        private const string ScenePath =
            "Assets/NutriMind/Missions/Grade5/LiteraQuest/G5_LQ_T1_M01/Scenes/SCN_G5_LQ_T1_M01_TheFestivalStorybookRescue.unity";
        private const string MissionJsonPath =
            "Assets/NutriMind/Missions/Grade5/LiteraQuest/G5_LQ_T1_M01/Data/g5_lq_t1_m01.json";
        private const string HudUxmlPath = "Assets/NutriMind/Gameplay/UI/UITK/HUD/UXML/GameplayStudentHud.uxml";
        private const string OverlayUxmlPath =
            "Assets/NutriMind/Gameplay/UI/UITK/Overlay/UXML/GameplayLearningOverlay.uxml";
        private const string PanelSettingsPath = "Assets/NutriMind/Gameplay/UI/Settings/PS_GameplayStudentHud.asset";

        [MenuItem("NutriMind/Gameplay/Mission 1/Validate Areas 1-2 Prototype")]
        public static void ValidatePrototype()
        {
            Scene scene = OpenMissionScene();
            if (!scene.IsValid())
            {
                return;
            }

            MissionSceneBindings bindings = UnityEngine.Object.FindFirstObjectByType<MissionSceneBindings>();
            if (bindings == null)
            {
                Debug.LogError("[MissionPrototypeSetupUtility] MissionSceneBindings is missing. Run Wire Missing Runtime Components first.");
                return;
            }

            MissionValidationReport report = bindings.Validate();
            if (report.IsValid)
            {
                Debug.Log("[MissionPrototypeSetupUtility] Validation passed.\n" + report);
            }
            else
            {
                Debug.LogWarning("[MissionPrototypeSetupUtility] Validation failed.\n" + report);
            }
        }

        [MenuItem("NutriMind/Gameplay/Mission 1/Wire Missing Runtime Components")]
        public static void WireMissingRuntimeComponents()
        {
            Scene scene = OpenMissionScene();
            if (!scene.IsValid())
            {
                return;
            }

            EnsurePanelSettings();

            Transform missionRuntime = FindOrCreateRoot("_MISSION_RUNTIME", out _);
            Transform interactions = FindOrCreateRoot("_INTERACTIONS", out _);
            Transform collectibles = FindOrCreateRoot("_COLLECTIBLES", out _);
            Transform gameplayUi = FindOrCreateRoot("_GAMEPLAY_UI", out _);
            Transform playerRoot = FindOrCreateRoot("_PLAYER", out _);

            Transform area1 = FindOrCreateChild(interactions, "A01_StorySquare", out _);
            Transform area2 = FindOrCreateChild(interactions, "A02_BannerMarketLane", out _);
            Transform areaRootA01 = FindDeep(scene, "AreaRoot_A01") ?? area1;

            if (!TryResolveExistingPlayer(playerRoot, scene, out GameplayPrototypePlayerController player, out string playerError))
            {
                Debug.LogError(
                    "[MissionPrototypeSetupUtility] " + playerError
                    + " Use NutriMind/Gameplay/Mission 1/Create Fallback Prototype Player only if intentional.");
                return;
            }

            GameplayPrototypePlayerInputAdapter adapter = GetOrAdd<GameplayPrototypePlayerInputAdapter>(player.gameObject);
            adapter.Bind(player);

            MissionPrototypeController missionController = GetOrAdd<MissionPrototypeController>(missionRuntime.gameObject);
            GameplayUiCoordinator uiCoordinator = GetOrAdd<GameplayUiCoordinator>(missionRuntime.gameObject);
            MissionSceneBindings bindings = GetOrAdd<MissionSceneBindings>(missionRuntime.gameObject);

            GameplayStudentHudRuntimeController hud = EnsureHud(gameplayUi);
            GameplayLearningOverlayController overlay = EnsureOverlay(gameplayUi);

            NpcGuideInteractable farmerLira = EnsureNpc(
                area1,
                "GuideNPC_FarmerLira_A01",
                MissionContentIds.FarmerLiraNpc,
                "Talk",
                "ds-icon--speak",
                new Vector3(-2f, 0f, -2f),
                "Farmer Lira requires manual placement beside the damaged storybook.");
            StorybookInteractable storybook = EnsureStorybook(area1, areaRootA01);
            EvidenceClueInteractable openingClue = EnsureClue(
                storybook.transform,
                "CluePoint01_OpeningIllustration",
                MissionContentIds.ClueOpeningIllustration,
                "Opening Illustration",
                "Children gather near the large acacia tree in Story Square.",
                FindExisting(scene, "CLUE_Opening_illustration"),
                null,
                null);
            EvidenceClueInteractable survivingClue = EnsureClue(
                storybook.transform,
                "CluePoint02_SurvivingLines",
                MissionContentIds.ClueSurvivingLines,
                "Surviving Lines",
                "They plan to carry a friendship banner to the Chronicle Courtyard.",
                FindExisting(scene, "CLUE_Surviving_Lines"),
                null,
                null);
            CaptionRepairInteractable captionBoard = EnsureCaptionBoard(storybook.transform);
            WorldStateController area1World = storybook.GetComponent<WorldStateController>();
            StoryFragmentCollectible fragment1 = EnsureFragment(
                collectibles,
                "CollectibleSpawn_Fragment01",
                MissionContentIds.Fragment1,
                storybook.transform.position + new Vector3(0f, 1.25f, 0.5f),
                "Fragment 1 requires manual placement above or behind the repaired storybook.");
            AreaGateController gate1 = EnsureGate(area1, "NextAreaGate_A01_A02", MissionContentIds.Gate1, FindExisting(scene, "Gate"));
            CheckpointTrigger checkpointA01 = EnsureCheckpoint(
                area1,
                "Checkpoint_A01",
                MissionContentIds.CheckpointA01,
                "Checkpoint A01 requires manual placement on the path to Gate 1.");
            AreaEntryTrigger area2Entry = EnsureAreaEntry(
                area2,
                "PlayerEntry_A02",
                MissionContentIds.Area2Id,
                "PlayerEntry_A02 requires manual placement after Gate 1.");

            NpcGuideInteractable mina = EnsureNpc(
                area2,
                "GuideNPC_Mina_A02",
                MissionContentIds.MinaNpc,
                "Talk",
                "ds-icon--speak",
                new Vector3(0f, 0f, 2f),
                "Mina requires manual placement near the first market stall.");
            EvidenceClueInteractable clue1 = EnsureClue(
                area2,
                "CluePoint01_ChildrenGather",
                MissionContentIds.ClueChildrenGather,
                "Children Gather",
                "The children gather at the acacia tree.",
                null,
                new Vector3(0f, 0f, 2f),
                "CluePoint01_ChildrenGather requires manual placement near the start of Banner Market Lane.");
            EvidenceClueInteractable clue2 = EnsureClue(
                area2,
                "CluePoint02_StorybookOpened",
                MissionContentIds.ClueStorybookOpened,
                "Storybook Opened",
                "Farmer Lira opens the damaged storybook.",
                null,
                new Vector3(0f, 0f, 10f),
                "CluePoint02_StorybookOpened requires manual placement in the middle section of Banner Market Lane.");
            EvidenceClueInteractable clue3 = EnsureClue(
                area2,
                "CluePoint03_CaptionRepaired",
                MissionContentIds.ClueCaptionRepaired,
                "Caption Repaired",
                "The Pathfinder repairs the missing opening caption.",
                null,
                new Vector3(0f, 0f, 20f),
                "CluePoint03_CaptionRepaired requires manual placement later in Banner Market Lane.");
            EventSequenceBoardInteractable sequenceBoard = EnsureSequenceBoard(area2);
            Transform sequenceNode = sequenceBoard.transform;
            Transform beforeCompletion = FindOrCreateChild(sequenceNode, "BeforeCompletion", out _);
            Transform afterCompletion = FindOrCreateChild(sequenceNode, "AfterCompletion", out bool afterCreated);
            if (afterCreated || !afterCompletion.gameObject.activeSelf)
            {
                afterCompletion.gameObject.SetActive(false);
            }

            WorldStateController area2World = GetOrAdd<WorldStateController>(sequenceNode.gameObject);
            SerializedObject worldSo = new SerializedObject(area2World);
            Assign(worldSo, "_beforeStateRoot", beforeCompletion.gameObject);
            Assign(worldSo, "_afterStateRoot", afterCompletion.gameObject);
            worldSo.ApplyModifiedPropertiesWithoutUndo();

            StoryFragmentCollectible fragment2 = EnsureFragment(
                collectibles,
                "CollectibleSpawn_Fragment02",
                MissionContentIds.Fragment2,
                sequenceBoard.transform.position + new Vector3(0f, 1.5f, 1f),
                "Fragment 2 requires manual placement near the completed sequence board.");
            AreaGateController gate2 = EnsureGate(area2, "NextAreaGate_A02_A03", MissionContentIds.Gate2);
            CheckpointTrigger checkpointA02 = EnsureCheckpoint(
                area2,
                "Checkpoint_A02",
                MissionContentIds.CheckpointA02,
                "Checkpoint A02 requires manual placement before Gate 2.");

            Transform playerSpawn = FindDeep(scene, "PlayerSpawnPoint");
            if (playerSpawn == null)
            {
                playerSpawn = FindOrCreateChild(playerRoot, "PlayerEntry_A01", out _);
            }
            PlayerInteractionController interaction = GetOrAdd<PlayerInteractionController>(player.gameObject);

            SerializedObject so = new SerializedObject(bindings);
            Assign(so, "_missionJson", AssetDatabase.LoadAssetAtPath<TextAsset>(MissionJsonPath));
            Assign(so, "_missionController", missionController);
            Assign(so, "_uiCoordinator", uiCoordinator);
            Assign(so, "_hudController", hud);
            Assign(so, "_overlayController", overlay);
            Assign(so, "_player", player);
            Assign(so, "_playerInputAdapter", adapter);
            Assign(so, "_playerInteraction", interaction);
            Assign(so, "_playerSpawn", playerSpawn);
            Assign(so, "_farmerLira", farmerLira);
            Assign(so, "_damagedStorybook", storybook);
            Assign(so, "_openingIllustrationClue", openingClue);
            Assign(so, "_survivingLinesClue", survivingClue);
            Assign(so, "_captionBoard", captionBoard);
            Assign(so, "_area1WorldState", area1World);
            Assign(so, "_fragment1", fragment1);
            Assign(so, "_gate1", gate1);
            Assign(so, "_checkpointA01", checkpointA01);
            Assign(so, "_area2Entry", area2Entry);
            Assign(so, "_mina", mina);
            Assign(so, "_childrenGatherClue", clue1);
            Assign(so, "_storybookOpenedClue", clue2);
            Assign(so, "_captionRepairedClue", clue3);
            Assign(so, "_sequenceBoard", sequenceBoard);
            Assign(so, "_area2WorldState", area2World);
            Assign(so, "_fragment2", fragment2);
            Assign(so, "_gate2", gate2);
            Assign(so, "_checkpointA02", checkpointA02);
            so.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject missionSo = new SerializedObject(missionController);
            Assign(missionSo, "_bindings", bindings);
            missionSo.ApplyModifiedPropertiesWithoutUndo();

            WarnAboutLooseCameras(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            MissionValidationReport report = bindings.Validate();
            if (report.IsValid)
            {
                Debug.Log("[MissionPrototypeSetupUtility] Wired missing components. Validation passed.\n" + report);
            }
            else
            {
                Debug.LogWarning("[MissionPrototypeSetupUtility] Wired missing components with remaining issues.\n" + report);
            }
        }

        [MenuItem("NutriMind/Gameplay/Mission 1/Create Missing Placeholder Anchors")]
        public static void CreateMissingPlaceholderAnchors()
        {
            WireMissingRuntimeComponents();
            Debug.Log(
                "[MissionPrototypeSetupUtility] Placeholder anchors use MissionPlacementRequired markers. "
                + "Confirm placements in Scene view before claiming validation success.");
        }

        [MenuItem("NutriMind/Gameplay/Mission 1/Create Fallback Prototype Player")]
        public static void CreateFallbackPrototypePlayer()
        {
            Scene scene = OpenMissionScene();
            if (!scene.IsValid())
            {
                return;
            }

            Transform playerRoot = FindOrCreateRoot("_PLAYER", out _);
            Transform existing = playerRoot.Find("PlayerRoot");
            if (existing != null && existing.GetComponent<GameplayPrototypePlayerController>() != null)
            {
                Debug.LogWarning("[MissionPrototypeSetupUtility] Fallback prototype player already exists.");
                return;
            }

            GameObject playerGo = existing != null ? existing.gameObject : new GameObject("PlayerRoot");
            playerGo.tag = "Player";
            playerGo.transform.SetParent(playerRoot, false);

            Transform spawn = FindDeep(scene, "PlayerSpawnPoint");
            if (spawn != null)
            {
                playerGo.transform.SetPositionAndRotation(spawn.position, spawn.rotation);
            }

            CharacterController controller = GetOrAdd<CharacterController>(playerGo);
            controller.height = 1.8f;
            controller.radius = 0.35f;
            controller.center = new Vector3(0f, 0.9f, 0f);

            GameplayPrototypePlayerController player = GetOrAdd<GameplayPrototypePlayerController>(playerGo);
            CreateFallbackVisual(playerGo.transform);
            CreateFallbackCameraRig(playerGo.transform, player);
            GetOrAdd<GameplayPrototypePlayerInputAdapter>(playerGo).Bind(player);
            GetOrAdd<PlayerInteractionController>(playerGo);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[MissionPrototypeSetupUtility] Created fallback prototype player.");
        }

        private static Scene OpenMissionScene()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError("[MissionPrototypeSetupUtility] Could not open mission scene.");
            }

            return scene;
        }

        private static bool TryResolveExistingPlayer(
            Transform playerRoot,
            Scene scene,
            out GameplayPrototypePlayerController player,
            out string error)
        {
            player = UnityEngine.Object.FindFirstObjectByType<GameplayPrototypePlayerController>();
            if (player == null)
            {
                Transform existingRoot = playerRoot != null ? playerRoot.Find("PlayerRoot") : null;
                if (existingRoot != null)
                {
                    player = existingRoot.GetComponent<GameplayPrototypePlayerController>();
                }
            }

            if (player == null)
            {
                error = "No compatible GameplayPrototypePlayerController was found in the scene.";
                return false;
            }

            if (player.GetComponent<CharacterController>() == null)
            {
                error = "Existing player is missing CharacterController.";
                return false;
            }

            error = null;
            return true;
        }

        private static void EnsurePanelSettings()
        {
            if (!AssetDatabase.IsValidFolder("Assets/NutriMind/Gameplay/UI/Settings"))
            {
                AssetDatabase.CreateFolder("Assets/NutriMind/Gameplay/UI", "Settings");
            }

            PanelSettings panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
            if (panelSettings == null)
            {
                panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
                AssetDatabase.CreateAsset(panelSettings, PanelSettingsPath);
            }

            panelSettings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panelSettings.referenceResolution = new Vector2Int(1920, 1080);
            panelSettings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            panelSettings.match = 0.5f;
            panelSettings.sortingOrder = 100;
            panelSettings.clearColor = false;
            panelSettings.clearDepthStencil = true;
            EditorUtility.SetDirty(panelSettings);
        }

        private static Transform FindOrCreateRoot(string name, out bool created)
        {
            GameObject existing = GameObject.Find(name);
            if (existing != null)
            {
                created = false;
                return existing.transform;
            }

            created = true;
            return new GameObject(name).transform;
        }

        private static Transform FindOrCreateChild(Transform parent, string name, out bool created)
        {
            Transform child = parent.Find(name);
            if (child != null)
            {
                created = false;
                return child;
            }

            created = true;
            child = new GameObject(name).transform;
            child.SetParent(parent, false);
            return child;
        }

        private static void CreateFallbackVisual(Transform playerRoot)
        {
            Transform visual = playerRoot.Find("PlayerBody_Visual");
            if (visual != null)
            {
                return;
            }

            GameObject visualGo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visualGo.name = "PlayerBody_Visual";
            visualGo.transform.SetParent(playerRoot, false);
            visualGo.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            visualGo.transform.localScale = new Vector3(0.7f, 0.9f, 0.7f);
            Collider primitiveCollider = visualGo.GetComponent<Collider>();
            if (primitiveCollider != null)
            {
                UnityEngine.Object.DestroyImmediate(primitiveCollider);
            }
        }

        private static void CreateFallbackCameraRig(Transform playerRoot, GameplayPrototypePlayerController player)
        {
            Transform pivot = FindOrCreateChild(playerRoot, "CameraPivot", out bool pivotCreated);
            if (pivotCreated)
            {
                pivot.localPosition = new Vector3(0f, 1.5f, 0f);
            }

            Transform cameraTransform = pivot.Find("PlayerCamera");
            Camera playerCamera;
            if (cameraTransform == null)
            {
                GameObject camGo = new GameObject("PlayerCamera");
                camGo.transform.SetParent(pivot, false);
                camGo.transform.localPosition = new Vector3(0f, 0.2f, -3.75f);
                playerCamera = camGo.AddComponent<Camera>();
                camGo.AddComponent<AudioListener>();
                cameraTransform = camGo.transform;
            }
            else
            {
                playerCamera = cameraTransform.GetComponent<Camera>();
                if (playerCamera == null)
                {
                    playerCamera = cameraTransform.gameObject.AddComponent<Camera>();
                }
            }

            playerCamera.tag = "MainCamera";
            SerializedObject so = new SerializedObject(player);
            Assign(so, "_cameraPivot", pivot);
            Assign(so, "_playerCamera", playerCamera);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameplayStudentHudRuntimeController EnsureHud(Transform parent)
        {
            Transform host = FindOrCreateChild(parent, "UITK_StudentHud", out _);
            UIDocument document = GetOrAdd<UIDocument>(host.gameObject);
            document.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(HudUxmlPath);
            document.panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
            document.sortingOrder = 100;
            return GetOrAdd<GameplayStudentHudRuntimeController>(host.gameObject);
        }

        private static GameplayLearningOverlayController EnsureOverlay(Transform parent)
        {
            Transform host = FindOrCreateChild(parent, "UITK_LearningOverlay", out _);
            UIDocument document = GetOrAdd<UIDocument>(host.gameObject);
            document.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(OverlayUxmlPath);
            document.panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
            document.sortingOrder = 200;
            return GetOrAdd<GameplayLearningOverlayController>(host.gameObject);
        }

        private static NpcGuideInteractable EnsureNpc(
            Transform parent,
            string objectName,
            string interactionId,
            string label,
            string iconClass,
            Vector3 defaultLocalPosition,
            string placementInstruction)
        {
            Transform node = FindOrCreateChild(parent, objectName, out bool created);
            if (created)
            {
                node.localPosition = defaultLocalPosition;
                EnsurePrimitiveChild(node, "NPC_Visual", PrimitiveType.Capsule, new Vector3(0f, 1f, 0f), new Vector3(0.6f, 1f, 0.6f), true);
                MarkPlacementRequired(node.gameObject, placementInstruction);
            }

            SphereCollider trigger = GetOrAdd<SphereCollider>(node.gameObject);
            trigger.isTrigger = true;
            trigger.radius = 1.5f;
            trigger.center = new Vector3(0f, 1f, 0f);
            Transform focus = FindOrCreateChild(node, "FocusPoint", out bool focusCreated);
            if (focusCreated)
            {
                focus.localPosition = new Vector3(0f, 1.5f, 0f);
            }

            return ConfigureInteractable<NpcGuideInteractable>(node.gameObject, interactionId, label, iconClass, focus, 2);
        }

        private static StorybookInteractable EnsureStorybook(Transform parent, Transform areaRoot)
        {
            Transform node = parent.Find("PrimaryInteraction_DamagedStorybook");
            bool created = false;
            if (node == null)
            {
                created = true;
                node = new GameObject("PrimaryInteraction_DamagedStorybook").transform;
                node.SetParent(parent, false);
                if (areaRoot != null)
                {
                    node.position = areaRoot.position + new Vector3(0f, 0f, 2f);
                }

                MarkPlacementRequired(node.gameObject, "Damaged storybook requires manual placement visible from spawn.");
            }

            Transform before = FindOrCreateChild(node, "BeforeRepair", out _);
            Transform after = FindOrCreateChild(node, "AfterRepair", out bool afterCreated);
            if (afterCreated)
            {
                after.gameObject.SetActive(false);
            }

            if (created)
            {
                EnsurePrimitiveChild(before, "DamagedBookVisual", PrimitiveType.Cube, new Vector3(0f, 0.8f, 0f), new Vector3(1.2f, 0.2f, 0.9f), true);
                EnsurePrimitiveChild(after, "RepairedBookVisual", PrimitiveType.Cube, new Vector3(0f, 0.8f, 0f), new Vector3(1.2f, 0.2f, 0.9f), true);
            }

            Transform interactionPoint = FindOrCreateChild(node, "InteractionPoint", out bool pointCreated);
            if (pointCreated)
            {
                interactionPoint.localPosition = new Vector3(0f, 0f, 1.2f);
            }

            BoxCollider trigger = GetOrAdd<BoxCollider>(node.gameObject);
            trigger.isTrigger = true;
            trigger.size = new Vector3(2f, 2f, 2f);
            trigger.center = new Vector3(0f, 1f, 0f);

            WorldStateController worldState = GetOrAdd<WorldStateController>(node.gameObject);
            SerializedObject worldSo = new SerializedObject(worldState);
            Assign(worldSo, "_beforeStateRoot", before.gameObject);
            Assign(worldSo, "_afterStateRoot", after.gameObject);
            worldSo.ApplyModifiedPropertiesWithoutUndo();

            return ConfigureInteractable<StorybookInteractable>(
                node.gameObject,
                MissionContentIds.DamagedStorybook,
                "Inspect",
                "ds-icon--search",
                interactionPoint,
                3);
        }

        private static CaptionRepairInteractable EnsureCaptionBoard(Transform parent)
        {
            Transform node = parent.Find("CaptionRepairBoard");
            if (node == null)
            {
                node = new GameObject("CaptionRepairBoard").transform;
                node.SetParent(parent, false);
                node.localPosition = new Vector3(1.2f, 1.1f, 0.8f);
                EnsurePrimitiveChild(node, "BoardVisual", PrimitiveType.Cube, Vector3.zero, new Vector3(0.8f, 0.5f, 0.08f), true);
                MarkPlacementRequired(node.gameObject, "Caption board requires manual placement beside the storybook.");
            }

            SphereCollider trigger = GetOrAdd<SphereCollider>(node.gameObject);
            trigger.isTrigger = true;
            trigger.radius = 1.2f;
            return ConfigureInteractable<CaptionRepairInteractable>(
                node.gameObject,
                MissionContentIds.CaptionRepairBoard,
                "Repair",
                "ds-icon--edit",
                node,
                2);
        }

        private static EvidenceClueInteractable EnsureClue(
            Transform parent,
            string objectName,
            string clueId,
            string title,
            string body,
            GameObject existing,
            Vector3? defaultLocalPosition,
            string placementInstruction)
        {
            Transform node;
            bool created = false;
            if (existing != null)
            {
                existing.name = objectName;
                node = existing.transform;
                if (node.parent != parent)
                {
                    node.SetParent(parent, true);
                }
            }
            else
            {
                node = FindOrCreateChild(parent, objectName, out created);
                if (created)
                {
                    if (defaultLocalPosition.HasValue)
                    {
                        node.localPosition = defaultLocalPosition.Value;
                    }

                    EnsurePrimitiveChild(
                        node,
                        "IllustratedClueVisual",
                        PrimitiveType.Quad,
                        new Vector3(0f, 1.2f, 0f),
                        new Vector3(0.8f, 0.8f, 1f),
                        true);
                    if (!string.IsNullOrEmpty(placementInstruction))
                    {
                        MarkPlacementRequired(node.gameObject, placementInstruction);
                    }
                }
            }

            SphereCollider trigger = GetOrAdd<SphereCollider>(node.gameObject);
            trigger.isTrigger = true;
            trigger.radius = 1.5f;
            Transform focus = FindOrCreateChild(node, "InteractionPoint", out bool focusCreated);
            if (focusCreated)
            {
                focus.localPosition = new Vector3(0f, 1.2f, 0f);
            }

            EvidenceClueInteractable clue = ConfigureInteractable<EvidenceClueInteractable>(
                node.gameObject,
                clueId,
                "Inspect",
                "ds-icon--search",
                focus,
                1);
            SerializedObject so = new SerializedObject(clue);
            Assign(so, "_clueId", clueId);
            Assign(so, "_evidenceTitle", title);
            Assign(so, "_evidenceBody", body);
            so.ApplyModifiedPropertiesWithoutUndo();
            return clue;
        }

        private static EventSequenceBoardInteractable EnsureSequenceBoard(Transform parent)
        {
            Transform node = FindOrCreateChild(parent, "PrimaryInteraction_EventSequenceBoard", out bool created);
            if (created)
            {
                node.localPosition = new Vector3(0f, 0f, 28f);
                EnsurePrimitiveChild(node, "BoardVisual", PrimitiveType.Cube, new Vector3(0f, 1f, 0f), new Vector3(3f, 1.2f, 0.2f), true);
                FindOrCreateChild(node, "EventSlot_Beginning", out _).localPosition = new Vector3(-1.2f, 1f, 0f);
                FindOrCreateChild(node, "EventSlot_Middle", out _).localPosition = new Vector3(0f, 1f, 0f);
                FindOrCreateChild(node, "EventSlot_End", out _).localPosition = new Vector3(1.2f, 1f, 0f);
                MarkPlacementRequired(
                    node.gameObject,
                    "Sequence board requires manual placement at the far end of Banner Market Lane.");
            }

            BoxCollider trigger = GetOrAdd<BoxCollider>(node.gameObject);
            trigger.isTrigger = true;
            trigger.size = new Vector3(3.5f, 2f, 2f);
            trigger.center = new Vector3(0f, 1f, 0f);
            return ConfigureInteractable<EventSequenceBoardInteractable>(
                node.gameObject,
                MissionContentIds.EventSequenceBoard,
                "Arrange",
                "ds-icon--list",
                node,
                3);
        }

        private static StoryFragmentCollectible EnsureFragment(
            Transform parent,
            string objectName,
            string collectibleId,
            Vector3 defaultWorldPosition,
            string placementInstruction)
        {
            Transform node = FindOrCreateChild(parent, objectName, out bool created);
            if (created)
            {
                node.position = defaultWorldPosition;
                MarkPlacementRequired(node.gameObject, placementInstruction);
            }

            GameObject visual = EnsurePrimitiveChild(
                node,
                "FragmentVisual",
                PrimitiveType.Sphere,
                Vector3.zero,
                Vector3.one * 0.5f,
                created);
            visual.SetActive(false);

            SphereCollider trigger = GetOrAdd<SphereCollider>(node.gameObject);
            trigger.isTrigger = true;
            trigger.radius = 1f;
            trigger.enabled = false;

            node.gameObject.SetActive(true);

            StoryFragmentCollectible collectible = GetOrAdd<StoryFragmentCollectible>(node.gameObject);
            SerializedObject so = new SerializedObject(collectible);
            Assign(so, "_collectibleId", collectibleId);
            Assign(so, "_triggerCollider", trigger);
            Assign(so, "_visualRoot", visual);
            so.ApplyModifiedPropertiesWithoutUndo();
            return collectible;
        }

        private static AreaGateController EnsureGate(
            Transform parent,
            string objectName,
            string gateId,
            GameObject existing = null)
        {
            Transform node;
            bool created = false;
            if (existing != null)
            {
                node = existing.transform;
            }
            else
            {
                node = FindOrCreateChild(parent, objectName, out created);
                if (created)
                {
                    MarkPlacementRequired(node.gameObject, objectName + " requires manual placement at the area exit.");
                }
            }

            Transform locked = FindOrCreateChild(node, "LockedVisual", out _);
            Transform unlocked = FindOrCreateChild(node, "UnlockedVisual", out bool unlockedCreated);
            if (unlockedCreated)
            {
                unlocked.gameObject.SetActive(false);
            }

            BoxCollider blocker = GetOrAdd<BoxCollider>(locked.gameObject);
            blocker.isTrigger = false;
            if (created)
            {
                blocker.size = new Vector3(4f, 3f, 0.5f);
                blocker.center = new Vector3(0f, 1.5f, 0f);
                EnsurePrimitiveChild(
                    locked,
                    "GateBlockerMesh",
                    PrimitiveType.Cube,
                    new Vector3(0f, 1.5f, 0f),
                    new Vector3(4f, 3f, 0.5f),
                    true);
            }

            AreaGateController gate = GetOrAdd<AreaGateController>(node.gameObject);
            SerializedObject so = new SerializedObject(gate);
            Assign(so, "_gateId", gateId);
            Assign(so, "_blockerCollider", blocker);
            Assign(so, "_lockedVisual", locked.gameObject);
            Assign(so, "_unlockedVisual", unlocked.gameObject);
            so.ApplyModifiedPropertiesWithoutUndo();
            return gate;
        }

        private static CheckpointTrigger EnsureCheckpoint(
            Transform parent,
            string objectName,
            string checkpointId,
            string placementInstruction)
        {
            Transform node = FindOrCreateChild(parent, objectName, out bool created);
            if (created)
            {
                MarkPlacementRequired(node.gameObject, placementInstruction);
            }

            BoxCollider trigger = GetOrAdd<BoxCollider>(node.gameObject);
            trigger.isTrigger = true;
            if (created)
            {
                trigger.size = new Vector3(6f, 3f, 6f);
                trigger.center = new Vector3(0f, 1.5f, 0f);
            }

            trigger.enabled = false;
            CheckpointTrigger checkpoint = GetOrAdd<CheckpointTrigger>(node.gameObject);
            SerializedObject so = new SerializedObject(checkpoint);
            Assign(so, "_checkpointId", checkpointId);
            Assign(so, "_respawnPoint", node);
            so.ApplyModifiedPropertiesWithoutUndo();
            return checkpoint;
        }

        private static AreaEntryTrigger EnsureAreaEntry(
            Transform parent,
            string objectName,
            string areaId,
            string placementInstruction)
        {
            Transform node = FindOrCreateChild(parent, objectName, out bool created);
            if (created)
            {
                MarkPlacementRequired(node.gameObject, placementInstruction);
            }

            BoxCollider trigger = GetOrAdd<BoxCollider>(node.gameObject);
            trigger.isTrigger = true;
            if (created)
            {
                trigger.size = new Vector3(6f, 3f, 4f);
                trigger.center = new Vector3(0f, 1.5f, 0f);
            }

            AreaEntryTrigger entry = GetOrAdd<AreaEntryTrigger>(node.gameObject);
            SerializedObject so = new SerializedObject(entry);
            Assign(so, "_areaId", areaId);
            so.ApplyModifiedPropertiesWithoutUndo();
            return entry;
        }

        private static void MarkPlacementRequired(GameObject host, string instruction)
        {
            MissionPlacementRequired marker = GetOrAdd<MissionPlacementRequired>(host);
            marker.Configure(instruction);
            Debug.LogWarning("[MissionPrototypeSetupUtility] " + instruction);
        }

        private static T ConfigureInteractable<T>(
            GameObject host,
            string interactionId,
            string label,
            string iconClass,
            Transform focus,
            int priority) where T : WorldInteractableBase
        {
            T interactable = GetOrAdd<T>(host);
            SerializedObject so = new SerializedObject(interactable);
            Assign(so, "_interactionId", interactionId);
            Assign(so, "_promptLabel", label);
            Assign(so, "_iconClass", iconClass);
            Assign(so, "_focusPoint", focus);
            Assign(so, "_priority", priority);
            so.ApplyModifiedPropertiesWithoutUndo();
            return interactable;
        }

        private static GameObject EnsurePrimitiveChild(
            Transform parent,
            string name,
            PrimitiveType type,
            Vector3 localPosition,
            Vector3 localScale,
            bool assignDefaultTransform)
        {
            Transform child = parent.Find(name);
            bool created = false;
            if (child == null)
            {
                created = true;
                child = GameObject.CreatePrimitive(type).transform;
                child.name = name;
                child.SetParent(parent, false);
            }

            if (created || assignDefaultTransform)
            {
                child.localPosition = localPosition;
                child.localScale = localScale;
            }

            Collider collider = child.GetComponent<Collider>();
            if (collider != null)
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }

            return child.gameObject;
        }

        private static T GetOrAdd<T>(GameObject host) where T : Component
        {
            T component = host.GetComponent<T>();
            if (component == null)
            {
                component = host.AddComponent<T>();
            }

            return component;
        }

        private static void Assign(SerializedObject so, string propertyName, UnityEngine.Object value)
        {
            SerializedProperty property = so.FindProperty(propertyName);
            if (property != null)
            {
                property.objectReferenceValue = value;
            }
        }

        private static void Assign(SerializedObject so, string propertyName, string value)
        {
            SerializedProperty property = so.FindProperty(propertyName);
            if (property != null)
            {
                property.stringValue = value;
            }
        }

        private static void Assign(SerializedObject so, string propertyName, int value)
        {
            SerializedProperty property = so.FindProperty(propertyName);
            if (property != null)
            {
                property.intValue = value;
            }
        }

        private static Transform FindDeep(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Transform result = FindDeep(root.transform, name);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        private static Transform FindDeep(Transform parent, string name)
        {
            if (parent.name == name)
            {
                return parent;
            }

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform result = FindDeep(parent.GetChild(i), name);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        private static GameObject FindExisting(Scene scene, string name)
        {
            Transform transform = FindDeep(scene, name);
            return transform != null ? transform.gameObject : null;
        }

        private static void WarnAboutLooseCameras(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == "Camera" && root.GetComponent<Camera>() != null && root.transform.parent == null)
                {
                    AudioListener listener = root.GetComponent<AudioListener>();
                    if (listener != null && listener.enabled)
                    {
                        Debug.LogWarning(
                            "[MissionPrototypeSetupUtility] Loose scene Camera still has an enabled AudioListener. "
                            + "Disable it manually to avoid duplicates.");
                    }
                }
            }
        }
    }
}
