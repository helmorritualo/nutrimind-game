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
        private const string MenuPath = "NutriMind/Gameplay/Mission 1/Validate and Wire Areas 1-2 Prototype";
        private const string ScenePath =
            "Assets/NutriMind/Missions/Grade5/LiteraQuest/G5_LQ_T1_M01/Scenes/SCN_G5_LQ_T1_M01_TheFestivalStorybookRescue.unity";
        private const string MissionJsonPath =
            "Assets/NutriMind/Missions/Grade5/LiteraQuest/G5_LQ_T1_M01/Data/g5_lq_t1_m01.json";
        private const string HudUxmlPath = "Assets/NutriMind/Gameplay/UI/UITK/HUD/UXML/GameplayStudentHud.uxml";
        private const string OverlayUxmlPath =
            "Assets/NutriMind/Gameplay/UI/UITK/Overlay/UXML/GameplayLearningOverlay.uxml";
        private const string PanelSettingsPath = "Assets/NutriMind/Gameplay/UI/Settings/PS_GameplayStudentHud.asset";

        [MenuItem(MenuPath)]
        public static void ValidateAndWirePrototype()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError("[MissionPrototypeSetupUtility] Could not open mission scene.");
                return;
            }

            EnsurePanelSettings();
            Transform missionRuntime = EnsureRoot("_MISSION_RUNTIME");
            Transform interactions = EnsureRoot("_INTERACTIONS");
            Transform collectibles = EnsureRoot("_COLLECTIBLES");
            Transform gameplayUi = EnsureRoot("_GAMEPLAY_UI");
            Transform playerRoot = EnsureRoot("_PLAYER");

            Transform area1 = EnsureChild(interactions, "A01_StorySquare");
            Transform area2 = EnsureChild(interactions, "A02_BannerMarketLane");
            Transform areaRootA01 = FindDeep(scene, "AreaRoot_A01") ?? area1;

            GameplayPrototypePlayerController player = EnsurePlayer(playerRoot, scene);
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
                new Vector3(-2f, 0f, -2f));
            StorybookInteractable storybook = EnsureStorybook(area1, areaRootA01);
            EvidenceClueInteractable openingClue = EnsureClue(
                storybook.transform,
                "CluePoint01_OpeningIllustration",
                MissionContentIds.ClueOpeningIllustration,
                "Opening Illustration",
                "Children gather near the large acacia tree in Story Square.",
                FindExisting(scene, "CLUE_Opening_illustration"));
            EvidenceClueInteractable survivingClue = EnsureClue(
                storybook.transform,
                "CluePoint02_SurvivingLines",
                MissionContentIds.ClueSurvivingLines,
                "Surviving Lines",
                "They plan to carry a friendship banner to the Chronicle Courtyard.",
                FindExisting(scene, "CLUE_Surviving_Lines"));
            CaptionRepairInteractable captionBoard = EnsureCaptionBoard(storybook.transform);
            WorldStateController area1World = storybook.GetComponent<WorldStateController>();
            StoryFragmentCollectible fragment1 = EnsureFragment(
                collectibles,
                "CollectibleSpawn_Fragment01",
                MissionContentIds.Fragment1,
                storybook.transform.position + new Vector3(0f, 1.25f, 0.5f));
            AreaGateController gate1 = EnsureGate(area1, "NextAreaGate_A01_A02", MissionContentIds.Gate1, FindExisting(scene, "Gate"));
            CheckpointTrigger checkpointA01 = EnsureCheckpoint(area1, "Checkpoint_A01", MissionContentIds.CheckpointA01);
            AreaEntryTrigger area2Entry = EnsureAreaEntry(area2, "PlayerEntry_A02", MissionContentIds.Area2Id);

            NpcGuideInteractable mina = EnsureNpc(
                area2,
                "GuideNPC_Mina_A02",
                MissionContentIds.MinaNpc,
                "Talk",
                "ds-icon--speak",
                new Vector3(0f, 0f, 2f));
            EvidenceClueInteractable clue1 = EnsureClue(
                area2,
                "CluePoint01_ChildrenGather",
                MissionContentIds.ClueChildrenGather,
                "Children Gather",
                "The children gather at the acacia tree.");
            EvidenceClueInteractable clue2 = EnsureClue(
                area2,
                "CluePoint02_StorybookOpened",
                MissionContentIds.ClueStorybookOpened,
                "Storybook Opened",
                "Farmer Lira opens the damaged storybook.",
                null,
                new Vector3(0f, 0f, 10f));
            EvidenceClueInteractable clue3 = EnsureClue(
                area2,
                "CluePoint03_CaptionRepaired",
                MissionContentIds.ClueCaptionRepaired,
                "Caption Repaired",
                "The Pathfinder repairs the missing opening caption.",
                null,
                new Vector3(0f, 0f, 20f));
            EventSequenceBoardInteractable sequenceBoard = EnsureSequenceBoard(area2);
            Transform sequenceNode = sequenceBoard.transform;
            Transform beforeCompletion = EnsureChild(sequenceNode, "BeforeCompletion");
            Transform afterCompletion = EnsureChild(sequenceNode, "AfterCompletion");
            afterCompletion.gameObject.SetActive(false);
            WorldStateController area2World = GetOrAdd<WorldStateController>(sequenceNode.gameObject);
            SerializedObject worldSo = new SerializedObject(area2World);
            Assign(worldSo, "_beforeStateRoot", beforeCompletion.gameObject);
            Assign(worldSo, "_afterStateRoot", afterCompletion.gameObject);
            worldSo.ApplyModifiedPropertiesWithoutUndo();
            StoryFragmentCollectible fragment2 = EnsureFragment(
                collectibles,
                "CollectibleSpawn_Fragment02",
                MissionContentIds.Fragment2,
                sequenceBoard.transform.position + new Vector3(0f, 1.5f, 1f));
            AreaGateController gate2 = EnsureGate(area2, "NextAreaGate_A02_A03", MissionContentIds.Gate2);
            CheckpointTrigger checkpointA02 = EnsureCheckpoint(area2, "Checkpoint_A02", MissionContentIds.CheckpointA02);

            Transform playerSpawn = FindDeep(scene, "PlayerSpawnPoint") ?? EnsureChild(playerRoot, "PlayerEntry_A01");
            PlayerInteractionController interaction = GetOrAdd<PlayerInteractionController>(player.gameObject);

            SerializedObject so = new SerializedObject(bindings);
            Assign(so, "_missionJson", AssetDatabase.LoadAssetAtPath<TextAsset>(MissionJsonPath));
            Assign(so, "_missionController", missionController);
            Assign(so, "_uiCoordinator", uiCoordinator);
            Assign(so, "_hudController", hud);
            Assign(so, "_overlayController", overlay);
            Assign(so, "_player", player);
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

            DisableLooseCamera(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            if (bindings.TryValidate(out string error))
            {
                Debug.Log("[MissionPrototypeSetupUtility] Scene wired and validated successfully.");
            }
            else
            {
                Debug.LogWarning("[MissionPrototypeSetupUtility] Scene wired with validation warnings:\n" + error);
            }
        }

        private static void EnsurePanelSettings()
        {
            const string panelSettingsPath = "Assets/NutriMind/Gameplay/UI/Settings/PS_GameplayStudentHud.asset";
            if (!AssetDatabase.IsValidFolder("Assets/NutriMind/Gameplay/UI/Settings"))
            {
                AssetDatabase.CreateFolder("Assets/NutriMind/Gameplay/UI", "Settings");
            }

            PanelSettings panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(panelSettingsPath);
            if (panelSettings == null)
            {
                panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
                AssetDatabase.CreateAsset(panelSettings, panelSettingsPath);
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

        private static Transform EnsureRoot(string name)
        {
            GameObject existing = GameObject.Find(name);
            if (existing == null)
            {
                existing = new GameObject(name);
            }

            return existing.transform;
        }

        private static Transform EnsureChild(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            if (child == null)
            {
                child = new GameObject(name).transform;
                child.SetParent(parent, false);
            }

            return child;
        }

        private static GameplayPrototypePlayerController EnsurePlayer(Transform parent, Scene scene)
        {
            Transform spawn = FindDeep(scene, "PlayerSpawnPoint");
            Transform existing = parent.Find("PlayerRoot");
            GameObject playerGo;
            if (existing != null)
            {
                playerGo = existing.gameObject;
            }
            else
            {
                playerGo = new GameObject("PlayerRoot");
                playerGo.tag = "Player";
                playerGo.transform.SetParent(parent, false);
            }

            if (spawn != null)
            {
                playerGo.transform.SetPositionAndRotation(spawn.position, spawn.rotation);
            }

            EnsurePlayerVisual(playerGo.transform);

            CharacterController controller = GetOrAdd<CharacterController>(playerGo);
            controller.height = 1.8f;
            controller.radius = 0.35f;
            controller.center = new Vector3(0f, 0.9f, 0f);

            GameplayPrototypePlayerController player = GetOrAdd<GameplayPrototypePlayerController>(playerGo);
            EnsurePlayerCameraRig(playerGo.transform, player);
            return player;
        }

        private static void EnsurePlayerCameraRig(Transform playerRoot, GameplayPrototypePlayerController player)
        {
            Transform pivot = playerRoot.Find("CameraPivot");
            if (pivot == null)
            {
                GameObject pivotGo = new GameObject("CameraPivot");
                pivotGo.transform.SetParent(playerRoot, false);
                pivot = pivotGo.transform;
            }

            pivot.localPosition = new Vector3(0f, 1.5f, 0f);
            pivot.localRotation = Quaternion.identity;

            Camera playerCamera = null;
            Transform cameraTransform = pivot.Find("PlayerCamera");
            if (cameraTransform != null)
            {
                playerCamera = cameraTransform.GetComponent<Camera>();
            }

            var strayCameras = new List<Transform>();
            for (int i = 0; i < playerRoot.childCount; i++)
            {
                Transform child = playerRoot.GetChild(i);
                if (child == pivot || child.name == "PlayerBody_Visual")
                {
                    continue;
                }

                if (child.GetComponent<Camera>() != null || child.name == "PlayerCamera")
                {
                    strayCameras.Add(child);
                }
            }

            foreach (Transform stray in strayCameras)
            {
                if (playerCamera == null)
                {
                    playerCamera = stray.GetComponent<Camera>();
                    if (playerCamera == null)
                    {
                        playerCamera = stray.gameObject.AddComponent<Camera>();
                    }

                    cameraTransform = stray;
                    stray.SetParent(pivot, false);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(stray.gameObject);
                }
            }

            if (playerCamera == null)
            {
                Camera sceneCamera = Camera.main;
                GameObject camGo;
                if (sceneCamera != null)
                {
                    camGo = sceneCamera.gameObject;
                    camGo.transform.SetParent(pivot, false);
                    playerCamera = sceneCamera;
                }
                else
                {
                    camGo = new GameObject("PlayerCamera");
                    camGo.transform.SetParent(pivot, false);
                    playerCamera = camGo.AddComponent<Camera>();
                    camGo.AddComponent<AudioListener>();
                }

                cameraTransform = camGo.transform;
            }

            cameraTransform.localPosition = new Vector3(0f, 0.2f, -3.75f);
            cameraTransform.localRotation = Quaternion.identity;
            playerCamera.tag = "MainCamera";

            AudioListener[] listeners = playerRoot.GetComponentsInChildren<AudioListener>(true);
            bool keptListener = false;
            foreach (AudioListener listener in listeners)
            {
                if (!keptListener && listener.gameObject == playerCamera.gameObject)
                {
                    keptListener = true;
                    continue;
                }

                UnityEngine.Object.DestroyImmediate(listener);
            }

            if (playerCamera.GetComponent<AudioListener>() == null)
            {
                playerCamera.gameObject.AddComponent<AudioListener>();
            }

            SerializedObject so = new SerializedObject(player);
            Assign(so, "_cameraPivot", pivot);
            Assign(so, "_playerCamera", playerCamera);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsurePlayerVisual(Transform playerRoot)
        {
            Transform visual = playerRoot.Find("PlayerBody_Visual");
            if (visual != null && !IsCapsulePrimitive(visual.gameObject))
            {
                UnityEngine.Object.DestroyImmediate(visual.gameObject);
                visual = null;
            }

            GameObject visualGo;
            if (visual == null)
            {
                visualGo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                visualGo.name = "PlayerBody_Visual";
                visualGo.transform.SetParent(playerRoot, false);
                Collider primitiveCollider = visualGo.GetComponent<Collider>();
                if (primitiveCollider != null)
                {
                    UnityEngine.Object.DestroyImmediate(primitiveCollider);
                }
            }
            else
            {
                visualGo = visual.gameObject;
            }

            visualGo.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            visualGo.transform.localRotation = Quaternion.identity;
            visualGo.transform.localScale = new Vector3(0.7f, 0.9f, 0.7f);
            ApplyPlayerVisualMaterial(visualGo);
        }

        private static bool IsCapsulePrimitive(GameObject go)
        {
            MeshFilter filter = go.GetComponent<MeshFilter>();
            return filter != null && filter.sharedMesh != null && filter.sharedMesh.name.Contains("Capsule");
        }

        private static void ApplyPlayerVisualMaterial(GameObject visualGo)
        {
            Renderer renderer = visualGo.GetComponent<Renderer>();
            if (renderer == null)
            {
                return;
            }

            Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
            if (litShader == null)
            {
                return;
            }

            Material bodyMaterial = new Material(litShader)
            {
                color = new Color(0.22f, 0.55f, 0.95f, 1f)
            };
            renderer.sharedMaterial = bodyMaterial;
        }

        private static GameplayStudentHudRuntimeController EnsureHud(Transform parent)
        {
            Transform host = EnsureChild(parent, "UITK_StudentHud");
            UIDocument document = GetOrAdd<UIDocument>(host.gameObject);
            document.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(HudUxmlPath);
            document.panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
            document.sortingOrder = 100;
            return GetOrAdd<GameplayStudentHudRuntimeController>(host.gameObject);
        }

        private static GameplayLearningOverlayController EnsureOverlay(Transform parent)
        {
            Transform host = EnsureChild(parent, "UITK_LearningOverlay");
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
            Vector3 localPosition)
        {
            Transform node = EnsureChild(parent, objectName);
            node.localPosition = localPosition;
            GameObject visual = EnsurePrimitiveChild(node, "NPC_Visual", PrimitiveType.Capsule, new Vector3(0f, 1f, 0f), new Vector3(0.6f, 1f, 0.6f));
            visual.GetComponent<Renderer>().sharedMaterial.color = new Color(0.55f, 0.35f, 0.2f);
            SphereCollider trigger = GetOrAdd<SphereCollider>(node.gameObject);
            trigger.isTrigger = true;
            trigger.radius = 1.5f;
            trigger.center = new Vector3(0f, 1f, 0f);
            Transform focus = EnsureChild(node, "FocusPoint");
            focus.localPosition = new Vector3(0f, 1.5f, 0f);
            return ConfigureInteractable<NpcGuideInteractable>(node.gameObject, interactionId, label, iconClass, focus, 2);
        }

        private static StorybookInteractable EnsureStorybook(Transform parent, Transform areaRoot)
        {
            Transform node = parent.Find("PrimaryInteraction_DamagedStorybook");
            if (node == null)
            {
                node = new GameObject("PrimaryInteraction_DamagedStorybook").transform;
                node.SetParent(parent, false);
                if (areaRoot != null)
                {
                    node.position = areaRoot.position + new Vector3(0f, 0f, 2f);
                }
            }

            Transform before = EnsureChild(node, "BeforeRepair");
            Transform after = EnsureChild(node, "AfterRepair");
            after.gameObject.SetActive(false);
            EnsurePrimitiveChild(before, "DamagedBookVisual", PrimitiveType.Cube, new Vector3(0f, 0.8f, 0f), new Vector3(1.2f, 0.2f, 0.9f));
            EnsurePrimitiveChild(after, "RepairedBookVisual", PrimitiveType.Cube, new Vector3(0f, 0.8f, 0f), new Vector3(1.2f, 0.2f, 0.9f))
                .GetComponent<Renderer>().sharedMaterial.color = new Color(0.75f, 0.55f, 0.2f);
            Transform interactionPoint = EnsureChild(node, "InteractionPoint");
            interactionPoint.localPosition = new Vector3(0f, 0f, 1.2f);
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
            }

            EnsurePrimitiveChild(node, "BoardVisual", PrimitiveType.Cube, Vector3.zero, new Vector3(0.8f, 0.5f, 0.08f));
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
            GameObject existing = null,
            Vector3? localPosition = null)
        {
            Transform node;
            if (existing != null)
            {
                existing.name = objectName;
                node = existing.transform;
                node.SetParent(parent, true);
            }
            else
            {
                node = EnsureChild(parent, objectName);
                if (localPosition.HasValue)
                {
                    node.localPosition = localPosition.Value;
                }

                EnsurePrimitiveChild(node, "IllustratedClueVisual", PrimitiveType.Quad, new Vector3(0f, 1.2f, 0f), new Vector3(0.8f, 0.8f, 1f));
            }

            SphereCollider trigger = GetOrAdd<SphereCollider>(node.gameObject);
            trigger.isTrigger = true;
            trigger.radius = 1.5f;
            Transform focus = EnsureChild(node, "InteractionPoint");
            focus.localPosition = new Vector3(0f, 1.2f, 0f);
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
            Transform node = EnsureChild(parent, "PrimaryInteraction_EventSequenceBoard");
            node.localPosition = new Vector3(0f, 0f, 28f);
            EnsurePrimitiveChild(node, "BoardVisual", PrimitiveType.Cube, new Vector3(0f, 1f, 0f), new Vector3(3f, 1.2f, 0.2f));
            EnsureChild(node, "EventSlot_Beginning").localPosition = new Vector3(-1.2f, 1f, 0f);
            EnsureChild(node, "EventSlot_Middle").localPosition = new Vector3(0f, 1f, 0f);
            EnsureChild(node, "EventSlot_End").localPosition = new Vector3(1.2f, 1f, 0f);
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

        private static WorldStateController EnsureWorldState(GameObject host, string afterChildName)
        {
            WorldStateController controller = GetOrAdd<WorldStateController>(host);
            Transform after = host.transform.Find(afterChildName);
            if (after == null)
            {
                after = new GameObject(afterChildName).transform;
                after.SetParent(host.transform, false);
            }

            after.gameObject.SetActive(false);
            SerializedObject so = new SerializedObject(controller);
            Assign(so, "_beforeStateRoot", host);
            Assign(so, "_afterStateRoot", after.gameObject);
            so.ApplyModifiedPropertiesWithoutUndo();
            return controller;
        }

        private static StoryFragmentCollectible EnsureFragment(
            Transform parent,
            string objectName,
            string collectibleId,
            Vector3 worldPosition)
        {
            Transform node = EnsureChild(parent, objectName);
            node.position = worldPosition;
            GameObject visual = EnsurePrimitiveChild(node, "FragmentVisual", PrimitiveType.Sphere, Vector3.zero, Vector3.one * 0.5f);
            visual.GetComponent<Renderer>().sharedMaterial.color = new Color(0.9f, 0.75f, 0.2f);
            SphereCollider trigger = GetOrAdd<SphereCollider>(node.gameObject);
            trigger.isTrigger = true;
            trigger.radius = 1f;
            node.gameObject.SetActive(false);
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
            Transform node = existing != null ? existing.transform : EnsureChild(parent, objectName);
            if (existing == null)
            {
                node.SetParent(parent, false);
            }

            Transform locked = EnsureChild(node, "LockedVisual");
            Transform unlocked = EnsureChild(node, "UnlockedVisual");
            unlocked.gameObject.SetActive(false);
            BoxCollider blocker = GetOrAdd<BoxCollider>(locked.gameObject);
            blocker.isTrigger = false;
            blocker.size = new Vector3(4f, 3f, 0.5f);
            blocker.center = new Vector3(0f, 1.5f, 0f);
            EnsurePrimitiveChild(locked, "GateBlockerMesh", PrimitiveType.Cube, new Vector3(0f, 1.5f, 0f), new Vector3(4f, 3f, 0.5f));
            AreaGateController gate = GetOrAdd<AreaGateController>(node.gameObject);
            SerializedObject so = new SerializedObject(gate);
            Assign(so, "_gateId", gateId);
            Assign(so, "_blockerCollider", blocker);
            Assign(so, "_lockedVisual", locked.gameObject);
            Assign(so, "_unlockedVisual", unlocked.gameObject);
            so.ApplyModifiedPropertiesWithoutUndo();
            return gate;
        }

        private static CheckpointTrigger EnsureCheckpoint(Transform parent, string objectName, string checkpointId)
        {
            Transform node = EnsureChild(parent, objectName);
            BoxCollider trigger = GetOrAdd<BoxCollider>(node.gameObject);
            trigger.isTrigger = true;
            trigger.size = new Vector3(6f, 3f, 6f);
            trigger.center = new Vector3(0f, 1.5f, 0f);
            trigger.enabled = false;
            CheckpointTrigger checkpoint = GetOrAdd<CheckpointTrigger>(node.gameObject);
            SerializedObject so = new SerializedObject(checkpoint);
            Assign(so, "_checkpointId", checkpointId);
            Assign(so, "_respawnPoint", node);
            so.ApplyModifiedPropertiesWithoutUndo();
            return checkpoint;
        }

        private static AreaEntryTrigger EnsureAreaEntry(Transform parent, string objectName, string areaId)
        {
            Transform node = EnsureChild(parent, objectName);
            BoxCollider trigger = GetOrAdd<BoxCollider>(node.gameObject);
            trigger.isTrigger = true;
            trigger.size = new Vector3(6f, 3f, 4f);
            trigger.center = new Vector3(0f, 1.5f, 0f);
            AreaEntryTrigger entry = GetOrAdd<AreaEntryTrigger>(node.gameObject);
            SerializedObject so = new SerializedObject(entry);
            Assign(so, "_areaId", areaId);
            so.ApplyModifiedPropertiesWithoutUndo();
            return entry;
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
            Vector3 localScale)
        {
            Transform child = parent.Find(name);
            if (child == null)
            {
                child = GameObject.CreatePrimitive(type).transform;
                child.name = name;
                child.SetParent(parent, false);
            }

            child.localPosition = localPosition;
            child.localScale = localScale;
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

        private static void DisableLooseCamera(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == "Camera" && root.GetComponent<Camera>() != null && root.transform.parent == null)
                {
                    root.SetActive(false);
                }
            }
        }
    }
}
