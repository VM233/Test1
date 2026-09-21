using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Test1.Combat.AI;
using Test1.Combat.Camera;
using Test1.Combat.Core;
using Test1.Combat.Player;
using Test1.Combat.UI;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using CameraComponent = UnityEngine.Camera;
using Object = UnityEngine.Object;

namespace Test1.Combat.Editor
{
    [InitializeOnLoad]
    public static class CombatDemoBuilder
    {
        private const int SetupVersion = 13;
        private const string SetupMarkerPath = "Library/Test1CombatDemoSetup.version";
        private const string ScenePath = "Assets/Game/Scenes/CombatDemo.unity";
        private const string GeneratedRoot = "Assets/Game/Generated";
        private const string SettingsRoot = "Assets/Game/Settings";
        private const string MaterialRoot = GeneratedRoot + "/Materials";
        private const string AnimatorRoot = GeneratedRoot + "/Animators";
        private const string PrefabRoot = GeneratedRoot + "/Prefabs";
        private const string PlayerAttackSettingsPath =
            SettingsRoot + "/PlayerMeleeAttack.asset";
        private const string BoarAttackSettingsPath =
            SettingsRoot + "/BoarMeleeAttack.asset";
        private const string InputActionsPath = "Assets/InputSystem_Actions.inputactions";
        private const string PlayerAttackMaskPath = AnimatorRoot + "/PlayerAttack.mask";
        private const string BoarAttackMaskPath = AnimatorRoot + "/BoarAttack.mask";

        private const string PlayerIdlePath = "Assets/Game/Art/Player/Player_Idle.fbx";
        private const string PlayerRunPath = "Assets/Game/Art/Player/Player_Run.fbx";
        private const string PlayerAttackPath = "Assets/Game/Art/Player/Player_Attack.fbx";
        private const string PlayerDeathPath = "Assets/Game/Art/Player/Player_Death.fbx";
        private const string BoarIdlePath = "Assets/Game/Art/Monster/Boar_Idle.fbx";
        private const string BoarRunPath = "Assets/Game/Art/Monster/Boar_Run.fbx";
        private const string BoarAttackPath = "Assets/Game/Art/Monster/Boar_Attack.fbx";
        private const string BoarDeathPath = "Assets/Game/Art/Monster/Boar_Death.fbx";

        static CombatDemoBuilder()
        {
            EditorApplication.delayCall += TryAutomaticBuild;
        }

        [MenuItem("Tools/Test1/Rebuild Combat Demo")]
        public static void RebuildFromMenu()
        {
            BuildEverything();
            WriteSetupMarker();
        }

        private static void TryAutomaticBuild()
        {
            if (EditorApplication.isCompiling ||
                EditorApplication.isUpdating ||
                EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.delayCall += TryAutomaticBuild;
                return;
            }

            if (ReadSetupVersion() == SetupVersion &&
                AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
            {
                return;
            }

            try
            {
                BuildEverything();
                WriteSetupMarker();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                Debug.LogError("TEST1_COMBAT_DEMO_SETUP_FAILED");
            }
        }

        private static void BuildEverything()
        {
            EnsureFolders();
            ConfigureSourceImports();

            Material playerBody = CreateLitMaterial(
                MaterialRoot + "/PlayerBody.mat",
                "Assets/Game/Art/Player/qishi_albedo.jpg",
                "Assets/Game/Art/Player/qishi_normal.jpg",
                Color.white,
                0.2f,
                0f);
            Material playerWeapon = CreateLitMaterial(
                MaterialRoot + "/PlayerWeapon.mat",
                "Assets/Game/Art/Player/Pistol_C.jpg",
                "Assets/Game/Art/Player/Pistol_N.jpg",
                Color.white,
                0.45f,
                0.15f);
            Material boarMaterial = CreateLitMaterial(
                MaterialRoot + "/Boar.mat",
                "Assets/Game/Art/Monster/T_Boar_BaseColor.jpg",
                "Assets/Game/Art/Monster/T_Boar_Nml.jpg",
                Color.white,
                0.25f,
                0f);
            Material groundMaterial = CreateColorMaterial(
                MaterialRoot + "/Ground.mat",
                new Color(0.17f, 0.29f, 0.22f),
                0.05f);
            Material laneMaterial = CreateColorMaterial(
                MaterialRoot + "/Lane.mat",
                new Color(0.28f, 0.38f, 0.29f),
                0.02f);
            Material wallMaterial = CreateColorMaterial(
                MaterialRoot + "/Wall.mat",
                new Color(0.12f, 0.16f, 0.18f),
                0.15f);
            Material playerRingMaterial = CreateColorMaterial(
                MaterialRoot + "/PlayerPad.mat",
                new Color(0.1f, 0.55f, 0.9f),
                0.2f);
            Material playerRingFadeMaterial = CreateTransparentColorMaterial(
                MaterialRoot + "/PlayerPadFade.mat",
                new Color(0.1f, 0.55f, 0.9f),
                0.2f);
            Material monsterRingMaterial = CreateColorMaterial(
                MaterialRoot + "/MonsterPad.mat",
                new Color(0.75f, 0.18f, 0.12f),
                0.2f);
            Material monsterRingFadeMaterial = CreateTransparentColorMaterial(
                MaterialRoot + "/MonsterPadFade.mat",
                new Color(0.75f, 0.18f, 0.12f),
                0.2f);
            InputActionAsset inputActions =
                AssetDatabase.LoadAssetAtPath<InputActionAsset>(
                    InputActionsPath);
            if (inputActions == null ||
                inputActions.FindAction("Player/Move", false) == null)
            {
                throw new InvalidOperationException(
                    "InputSystem_Actions must contain the Player/Move " +
                    "action.");
            }

            AvatarMask playerAttackMask = CreateUpperBodyMask(
                PlayerAttackMaskPath,
                PlayerIdlePath,
                "Spine");
            AvatarMask boarAttackMask = CreateUpperBodyMask(
                BoarAttackMaskPath,
                BoarIdlePath,
                "Spine");

            AnimatorController playerController = CreateAnimatorController(
                AnimatorRoot + "/Player.controller",
                PlayerIdlePath,
                PlayerRunPath,
                PlayerAttackPath,
                PlayerDeathPath,
                playerAttackMask);
            AnimatorController boarController = CreateAnimatorController(
                AnimatorRoot + "/Boar.controller",
                BoarIdlePath,
                BoarRunPath,
                BoarAttackPath,
                BoarDeathPath,
                boarAttackMask);
            MeleeAttackSettings playerAttackSettings =
                LoadRequiredAsset<MeleeAttackSettings>(
                    PlayerAttackSettingsPath);
            MeleeAttackSettings boarAttackSettings =
                LoadRequiredAsset<MeleeAttackSettings>(
                    BoarAttackSettingsPath);

            GameObject playerPrefab = CreatePlayerPrefab(
                playerController,
                playerBody,
                playerWeapon,
                playerRingMaterial,
                playerRingFadeMaterial,
                inputActions,
                playerAttackSettings);
            GameObject boarPrefab = CreateBoarPrefab(
                boarController,
                boarMaterial,
                monsterRingMaterial,
                monsterRingFadeMaterial,
                boarAttackSettings);
            CreateDemoScene(
                playerPrefab,
                boarPrefab,
                groundMaterial,
                laneMaterial,
                wallMaterial,
                inputActions);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            ValidateCurrentScene();

            Debug.Log("TEST1_COMBAT_DEMO_SETUP_OK - Assets/Game/Scenes/CombatDemo.unity");
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets/Game", "Generated");
            EnsureFolder(GeneratedRoot, "Materials");
            EnsureFolder(GeneratedRoot, "Animators");
            EnsureFolder(GeneratedRoot, "Prefabs");
            EnsureFolder("Assets/Game", "Settings");
            EnsureFolder("Assets/Game", "Scenes");
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = parent + "/" + child;
            if (AssetDatabase.IsValidFolder(path) == false)
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }

        private static T LoadRequiredAsset<T>(string assetPath)
            where T : Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (asset == null)
            {
                throw new InvalidOperationException(
                    $"Required asset not found: {assetPath}");
            }

            return asset;
        }

        private static void ConfigureSourceImports()
        {
            ConfigureModel(PlayerIdlePath, true);
            ConfigureModel(PlayerRunPath, true);
            ConfigureModel(PlayerAttackPath, false);
            ConfigureModel(PlayerDeathPath, false);
            ConfigureModel(BoarIdlePath, true);
            ConfigureModel(BoarRunPath, true);
            ConfigureModel(BoarAttackPath, false);
            ConfigureModel(BoarDeathPath, false);

            ConfigureTexture("Assets/Game/Art/Player/qishi_albedo.jpg", false, false);
            ConfigureTexture("Assets/Game/Art/Player/qishi_normal.jpg", true, false);
            ConfigureTexture("Assets/Game/Art/Player/Pistol_C.jpg", false, false);
            ConfigureTexture("Assets/Game/Art/Player/Pistol_N.jpg", true, false);
            ConfigureTexture("Assets/Game/Art/Monster/T_Boar_BaseColor.jpg", false, false);
            ConfigureTexture("Assets/Game/Art/Monster/T_Boar_Nml.jpg", true, false);
            ConfigureTexture("Assets/Game/Art/UI/JoystickBase.png", false, true);
            ConfigureTexture("Assets/Game/Art/UI/JoystickHandle.png", false, true);
        }

        private static void ConfigureModel(string assetPath, bool loop)
        {
            ModelImporter importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"Model importer not found: {assetPath}");
            }

            importer.importAnimation = true;
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;

            ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
            for (int index = 0; index < clips.Length; index++)
            {
                clips[index].loopTime = loop;
                clips[index].loopPose = loop;
            }

            if (clips.Length > 0)
            {
                importer.clipAnimations = clips;
            }

            importer.SaveAndReimport();
        }

        private static void ConfigureTexture(
            string assetPath,
            bool normalMap,
            bool sprite)
        {
            TextureImporter importer =
                AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"Texture importer not found: {assetPath}");
            }

            if (sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
            }
            else if (normalMap)
            {
                importer.textureType = TextureImporterType.NormalMap;
                importer.sRGBTexture = false;
            }
            else
            {
                importer.textureType = TextureImporterType.Default;
                importer.sRGBTexture = true;
            }

            importer.SaveAndReimport();
        }

        private static Material CreateLitMaterial(
            string assetPath,
            string baseTexturePath,
            string normalTexturePath,
            Color tint,
            float smoothness,
            float metallic)
        {
            Material material = LoadOrCreateMaterial(assetPath);
            material.SetColor("_BaseColor", tint);
            material.SetTexture(
                "_BaseMap",
                AssetDatabase.LoadAssetAtPath<Texture2D>(baseTexturePath));
            material.SetFloat("_Smoothness", smoothness);
            material.SetFloat("_Metallic", metallic);

            Texture2D normal =
                AssetDatabase.LoadAssetAtPath<Texture2D>(
                    normalTexturePath);
            material.SetTexture("_BumpMap", normal);
            if (normal != null)
            {
                material.EnableKeyword("_NORMALMAP");
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material CreateColorMaterial(
            string assetPath,
            Color color,
            float smoothness)
        {
            Material material = LoadOrCreateMaterial(assetPath);
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", smoothness);
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_Surface", 0f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_SrcBlend", (float)BlendMode.One);
            material.SetFloat("_DstBlend", (float)BlendMode.Zero);
            material.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            material.SetFloat("_DstBlendAlpha", (float)BlendMode.Zero);
            material.SetFloat("_ZWrite", 1f);
            material.SetOverrideTag("RenderType", "Opaque");
            material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = -1;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material CreateTransparentColorMaterial(
            string assetPath,
            Color color,
            float smoothness)
        {
            Material material = CreateColorMaterial(
                assetPath,
                color,
                smoothness);
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_BlendModePreserveSpecular", 0f);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat(
                "_DstBlend",
                (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            material.SetFloat(
                "_DstBlendAlpha",
                (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.SetFloat("_AlphaClip", 0f);
            material.SetOverrideTag("RenderType", "Transparent");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHATEST_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material LoadOrCreateMaterial(string assetPath)
        {
            Material material =
                AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            Shader shader = Shader.Find(
                                "Universal Render Pipeline/Lit") ??
                            Shader.Find("Standard");
            if (shader == null)
            {
                throw new InvalidOperationException("No compatible Lit shader was found.");
            }

            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, assetPath);
            }
            else
            {
                material.shader = shader;
            }

            return material;
        }

        private static AvatarMask CreateUpperBodyMask(
            string assetPath,
            string modelPath,
            string upperBodyRootSuffix)
        {
            if (AssetDatabase.LoadAssetAtPath<AvatarMask>(assetPath) != null)
            {
                AssetDatabase.DeleteAsset(assetPath);
            }

            GameObject modelAsset =
                AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (modelAsset == null)
            {
                throw new InvalidOperationException(
                    $"Model asset not found for animation mask: " +
                    modelPath);
            }

            GameObject instance =
                PrefabUtility.InstantiatePrefab(modelAsset) as GameObject;
            if (instance == null)
            {
                throw new InvalidOperationException(
                    $"Could not instantiate model for animation mask: " +
                    modelPath);
            }

            try
            {
                Animator animator =
                    instance.GetComponentInChildren<Animator>(true);
                Transform animationRoot = animator != null
                    ? animator.transform
                    : instance.transform;
                Transform upperBodyRoot = animationRoot
                    .GetComponentsInChildren<Transform>(true)
                    .FirstOrDefault(transform =>
                        transform.name.EndsWith(
                            upperBodyRootSuffix,
                            StringComparison.OrdinalIgnoreCase));
                if (upperBodyRoot == null)
                {
                    throw new InvalidOperationException(
                        $"Upper-body root ending in " +
                        $"'{upperBodyRootSuffix}' was not found in " +
                        $"{modelPath}.");
                }

                HashSet<string> activePaths = new(
                    StringComparer.Ordinal);
                foreach (Transform transform in animationRoot
                             .GetComponentsInChildren<Transform>(true))
                {
                    if (transform != upperBodyRoot &&
                        transform.IsChildOf(upperBodyRoot) == false)
                    {
                        continue;
                    }

                    string relativePath =
                        AnimationUtility.CalculateTransformPath(
                            transform,
                            animationRoot);
                    activePaths.Add(relativePath);
                    activePaths.Add(string.IsNullOrEmpty(relativePath)
                        ? animationRoot.name
                        : animationRoot.name + "/" + relativePath);
                }

                AvatarMask mask = new AvatarMask
                {
                    name = Path.GetFileNameWithoutExtension(assetPath)
                };
                mask.AddTransformPath(animationRoot, true);
                int activeTransformCount = 0;
                for (int index = 0; index < mask.transformCount; index++)
                {
                    bool active = activePaths.Contains(
                        mask.GetTransformPath(index));
                    mask.SetTransformActive(index, active);
                    if (active)
                    {
                        activeTransformCount++;
                    }
                }

                if (activeTransformCount == 0)
                {
                    Object.DestroyImmediate(mask);
                    throw new InvalidOperationException(
                        $"Animation mask for {modelPath} contains no " +
                        "active transforms.");
                }

                AssetDatabase.CreateAsset(mask, assetPath);
                return mask;
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        private static AnimatorController CreateAnimatorController(
            string assetPath,
            string idlePath,
            string runPath,
            string attackPath,
            string deathPath,
            AvatarMask attackMask)
        {
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(assetPath) != null)
            {
                AssetDatabase.DeleteAsset(assetPath);
            }

            AnimationClip idleClip = LoadPrimaryClip(idlePath);
            AnimationClip runClip = LoadPrimaryClip(runPath);
            AnimationClip attackClip = LoadPrimaryClip(attackPath);
            AnimationClip deathClip = LoadPrimaryClip(deathPath);

            AnimatorController controller =
                AnimatorController.CreateAnimatorControllerAtPath(
                    assetPath);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Die", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Respawn", AnimatorControllerParameterType.Trigger);

            AnimatorControllerLayer locomotionLayer = controller.layers[0];
            locomotionLayer.name = "Locomotion";
            AnimatorControllerLayer[] initialLayers = controller.layers;
            initialLayers[0] = locomotionLayer;
            controller.layers = initialLayers;
            AnimatorStateMachine locomotionStateMachine =
                locomotionLayer.stateMachine;
            AnimatorState idle = locomotionStateMachine.AddState("Idle");
            AnimatorState run = locomotionStateMachine.AddState("Run");
            AnimatorState death = locomotionStateMachine.AddState("Death");
            idle.motion = idleClip;
            run.motion = runClip;
            death.motion = deathClip;
            locomotionStateMachine.defaultState = idle;

            AnimatorStateTransition idleToRun = idle.AddTransition(run);
            idleToRun.hasExitTime = false;
            idleToRun.duration = 0.12f;
            idleToRun.AddCondition(
                AnimatorConditionMode.Greater,
                0.1f,
                "Speed");

            AnimatorStateTransition runToIdle = run.AddTransition(idle);
            runToIdle.hasExitTime = false;
            runToIdle.duration = 0.12f;
            runToIdle.AddCondition(
                AnimatorConditionMode.Less,
                0.1f,
                "Speed");

            AnimatorStateTransition anyToDeath =
                locomotionStateMachine.AddAnyStateTransition(death);
            anyToDeath.hasExitTime = false;
            anyToDeath.duration = 0.05f;
            anyToDeath.canTransitionToSelf = false;
            anyToDeath.AddCondition(AnimatorConditionMode.If, 0f, "Die");

            AnimatorStateTransition deathToIdle = death.AddTransition(idle);
            deathToIdle.hasExitTime = false;
            deathToIdle.duration = 0.05f;
            deathToIdle.AddCondition(
                AnimatorConditionMode.If,
                0f,
                "Respawn");

            controller.AddLayer("Attack Overlay");
            AnimatorControllerLayer[] configuredLayers = controller.layers;
            AnimatorControllerLayer attackLayer = configuredLayers[1];
            attackLayer.defaultWeight = 1f;
            attackLayer.blendingMode = AnimatorLayerBlendingMode.Override;
            attackLayer.avatarMask = attackMask;
            configuredLayers[1] = attackLayer;
            controller.layers = configuredLayers;

            AnimatorStateMachine attackStateMachine =
                attackLayer.stateMachine;
            AnimatorState empty = attackStateMachine.AddState("Empty");
            AnimatorState attack = attackStateMachine.AddState("Attack");
            attack.motion = attackClip;
            attackStateMachine.defaultState = empty;

            AnimatorStateTransition anyToAttack =
                attackStateMachine.AddAnyStateTransition(attack);
            anyToAttack.hasExitTime = false;
            anyToAttack.duration = 0.05f;
            anyToAttack.canTransitionToSelf = false;
            anyToAttack.AddCondition(AnimatorConditionMode.If, 0f, "Attack");

            AnimatorStateTransition attackToEmpty =
                attack.AddTransition(empty);
            attackToEmpty.hasExitTime = true;
            attackToEmpty.exitTime = 0.9f;
            attackToEmpty.duration = 0.1f;

            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static AnimationClip LoadPrimaryClip(string assetPath)
        {
            AnimationClip clip = AssetDatabase.LoadAllAssetsAtPath(assetPath)
                .OfType<AnimationClip>()
                .FirstOrDefault(candidate =>
                    candidate.name.StartsWith(
                        "__preview__",
                        StringComparison.OrdinalIgnoreCase) == false);
            if (clip == null)
            {
                throw new InvalidOperationException(
                    $"Animation clip not found: {assetPath}");
            }

            return clip;
        }

        private static GameObject CreatePlayerPrefab(
            RuntimeAnimatorController animatorController,
            Material bodyMaterial,
            Material weaponMaterial,
            Material ringMaterial,
            Material ringFadeMaterial,
            InputActionAsset inputActions,
            MeleeAttackSettings attackSettings)
        {
            const string prefabPath = PrefabRoot + "/Player.prefab";
            GameObject root = CreateActorRoot(
                "Player",
                ActorTeam.Player,
                130f,
                4.6f,
                attackSettings);
            root.tag = "Player";

            Rigidbody body = root.GetComponent<Rigidbody>();
            CapsuleCollider collider = root.AddComponent<CapsuleCollider>();
            collider.height = 1.9f;
            collider.radius = 0.46f;
            collider.center = new Vector3(0f, 0.95f, 0f);

            Transform visualRoot = CreateVisual(
                root.transform,
                PlayerIdlePath,
                2.05f,
                animatorController,
                bodyMaterial,
                weaponMaterial);
            CreateActorRing(
                visualRoot,
                root.GetComponent<Health>(),
                "Player Ring",
                1.45f,
                ringMaterial,
                ringFadeMaterial);
            ConfigureSharedActorComponents(root, visualRoot, body, 3f, 1.1f);

            PlayerInputSource input = root.AddComponent<PlayerInputSource>();
            input.Configure(null, inputActions);
            PlayerCombatController controller =
                root.AddComponent<PlayerCombatController>();
            controller.Configure(
                input,
                root.GetComponent<ActorMotor>(),
                root.GetComponent<TargetSensor>(),
                root.GetComponent<MeleeAttack>(),
                root.GetComponent<Health>());

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject CreateBoarPrefab(
            RuntimeAnimatorController animatorController,
            Material material,
            Material ringMaterial,
            Material ringFadeMaterial,
            MeleeAttackSettings attackSettings)
        {
            const string prefabPath = PrefabRoot + "/Boar.prefab";
            GameObject root = CreateActorRoot(
                "Boar",
                ActorTeam.Monster,
                72f,
                3.25f,
                attackSettings);

            Rigidbody body = root.GetComponent<Rigidbody>();
            body.mass = 1.35f;
            CapsuleCollider collider = root.AddComponent<CapsuleCollider>();
            collider.height = 1.25f;
            collider.radius = 0.47f;
            collider.center = new Vector3(0f, 0.625f, 0f);

            Transform visualRoot = CreateVisual(
                root.transform,
                BoarIdlePath,
                1.35f,
                animatorController,
                material,
                null);
            CreateActorRing(
                visualRoot,
                root.GetComponent<Health>(),
                "Monster Ring",
                1.35f,
                ringMaterial,
                ringFadeMaterial);
            ConfigureSharedActorComponents(root, visualRoot, body, 4f, 1.15f);
            CombatDemoUiFactory.CreateMonsterHealthBar(
                root.transform,
                root.GetComponent<Health>());

            SeparationSteering separation =
                root.AddComponent<SeparationSteering>();
            separation.Configure(1.2f, 2.35f);
            MonsterCombatController controller =
                root.AddComponent<MonsterCombatController>();
            controller.Configure(
                root.GetComponent<ActorMotor>(),
                root.GetComponent<TargetSensor>(),
                root.GetComponent<MeleeAttack>(),
                root.GetComponent<Health>());

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject CreateActorRoot(
            string actorName,
            ActorTeam team,
            float maxHealth,
            float moveSpeed,
            MeleeAttackSettings attackSettings)
        {
            GameObject root = new GameObject(actorName);
            Rigidbody body = root.AddComponent<Rigidbody>();
            body.useGravity = true;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode =
                CollisionDetectionMode.ContinuousDynamic;
            body.constraints =
                RigidbodyConstraints.FreezeRotationX |
                RigidbodyConstraints.FreezeRotationZ;

            Health health = root.AddComponent<Health>();
            health.Configure(maxHealth);
            Combatant combatant = root.AddComponent<Combatant>();
            ActorMotor motor = root.AddComponent<ActorMotor>();
            motor.Configure(moveSpeed, 720f);
            MeleeAttack attack = root.AddComponent<MeleeAttack>();
            attack.Configure(attackSettings);
            TargetSensor sensor = root.AddComponent<TargetSensor>();
            sensor.Configure(
                team == ActorTeam.Player
                    ? ActorTeam.Monster
                    : ActorTeam.Player,
                team == ActorTeam.Player
                    ? attackSettings.Range
                    : 7f);

            GameObject aimPoint = new GameObject("AimPoint");
            aimPoint.transform.SetParent(root.transform, false);
            aimPoint.transform.localPosition = Vector3.up *
                                               (team == ActorTeam.Player
                                                   ? 1f
                                                   : 0.65f);
            combatant.Configure(team, aimPoint.transform);
            return root;
        }

        private static void ConfigureSharedActorComponents(
            GameObject root,
            Transform visualRoot,
            Rigidbody body,
            float respawnDelay,
            float corpseVisibleDuration)
        {
            Health health = root.GetComponent<Health>();
            ActorMotor motor = root.GetComponent<ActorMotor>();
            MeleeAttack attack = root.GetComponent<MeleeAttack>();
            Animator animator = visualRoot.GetComponentInChildren<Animator>(true);

            ActorAnimation actorAnimation = root.AddComponent<ActorAnimation>();
            actorAnimation.Configure(animator, motor, attack, health);

            RespawnController respawn = root.AddComponent<RespawnController>();
            respawn.Configure(
                visualRoot,
                motor,
                body,
                respawnDelay,
                corpseVisibleDuration);
        }

        private static Transform CreateVisual(
            Transform actorRoot,
            string modelPath,
            float targetHeight,
            RuntimeAnimatorController animatorController,
            Material primaryMaterial,
            Material secondaryMaterial)
        {
            GameObject visualRootObject = new GameObject("Visual");
            Transform visualRoot = visualRootObject.transform;
            visualRoot.SetParent(actorRoot, false);

            GameObject modelAsset =
                AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (modelAsset == null)
            {
                throw new InvalidOperationException(
                    $"Model asset not found: {modelPath}");
            }

            GameObject model = PrefabUtility.InstantiatePrefab(
                modelAsset,
                visualRoot) as GameObject;
            if (model == null)
            {
                throw new InvalidOperationException(
                    $"Could not instantiate model: {modelPath}");
            }

            model.name = "Model";
            NormalizeModel(model, visualRoot, targetHeight);
            ApplyMaterials(
                model,
                primaryMaterial,
                secondaryMaterial,
                modelPath);

            Animator animator = model.GetComponentInChildren<Animator>(true);
            if (animator == null)
            {
                animator = model.AddComponent<Animator>();
            }

            animator.runtimeAnimatorController = animatorController;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            return visualRoot;
        }

        private static void NormalizeModel(
            GameObject model,
            Transform visualRoot,
            float targetHeight)
        {
            Renderer[] renderers =
                model.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                throw new InvalidOperationException(
                    $"Model contains no renderers: {model.name}");
            }

            Bounds bounds = renderers[0].bounds;
            for (int index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }

            float scale = bounds.size.y > 0.001f
                ? targetHeight / bounds.size.y
                : 1f;
            Vector3 offset = new(
                -bounds.center.x,
                -bounds.min.y,
                -bounds.center.z);
            model.transform.position += offset;
            visualRoot.localScale = Vector3.one * scale;
        }

        private static void ApplyMaterials(
            GameObject model,
            Material primary,
            Material secondary,
            string sourcePath)
        {
            Renderer[] renderers =
                model.GetComponentsInChildren<Renderer>(true);
            for (int rendererIndex = 0;
                 rendererIndex < renderers.Length;
                 rendererIndex++)
            {
                Renderer renderer = renderers[rendererIndex];
                Material[] sourceMaterials = renderer.sharedMaterials;
                Material[] replacements =
                    new Material[sourceMaterials.Length];
                for (int materialIndex = 0;
                     materialIndex < sourceMaterials.Length;
                     materialIndex++)
                {
                    string sourceName = sourceMaterials[materialIndex] != null
                        ? sourceMaterials[materialIndex].name
                        : "null";
                    string key = (renderer.name + " " + sourceName)
                        .ToLowerInvariant();
                    bool looksLikeWeapon =
                        key.Contains("weapon") ||
                        key.Contains("pistol") ||
                        key.Contains("axe") ||
                        key.Contains("sword") ||
                        key.Contains("gun") ||
                        key.Contains("dao");
                    bool useSecondary = secondary != null &&
                        (looksLikeWeapon ||
                         sourceMaterials.Length > 1 &&
                         materialIndex > 0);
                    replacements[materialIndex] = useSecondary
                        ? secondary
                        : primary;
                }

                renderer.sharedMaterials = replacements;
            }
        }

        private static void CreateDemoScene(
            GameObject playerPrefab,
            GameObject boarPrefab,
            Material groundMaterial,
            Material laneMaterial,
            Material wallMaterial,
            InputActionAsset inputActions)
        {
            Scene scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single);
            scene.name = "CombatDemo";

            RenderSettings.fog = false;
            RenderSettings.ambientLight = new Color(0.48f, 0.52f, 0.58f);

            Transform environment = CreateArena(
                groundMaterial,
                laneMaterial,
                wallMaterial);

            GameObject player =
                PrefabUtility.InstantiatePrefab(playerPrefab) as GameObject;
            player.name = "Player";
            player.transform.SetPositionAndRotation(
                new Vector3(0f, 0.26f, -7f),
                Quaternion.identity);

            CreateMonsterSpawnArea(environment, boarPrefab);

            CreateLighting();
            CreateCamera(player.transform);
            VirtualJoystick joystick = CreateHud(player);
            player.GetComponent<PlayerInputSource>().Configure(
                joystick,
                inputActions);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            UpdateBuildSettings();
        }

        private static Transform CreateArena(
            Material groundMaterial,
            Material laneMaterial,
            Material wallMaterial)
        {
            GameObject environment = new GameObject("Environment");

            GameObject ground = CreateCube(
                "Ground",
                new Vector3(0f, 0f, 1f),
                new Vector3(20f, 0.5f, 24f),
                groundMaterial,
                environment.transform);
            ground.isStatic = true;

            GameObject lane = CreateCube(
                "Combat Lane",
                new Vector3(0f, 0.255f, 0.5f),
                new Vector3(6.5f, 0.02f, 18f),
                laneMaterial,
                environment.transform);
            Object.DestroyImmediate(lane.GetComponent<Collider>());
            lane.isStatic = true;

            CreateCube(
                "Wall North",
                new Vector3(0f, 0.65f, 13f),
                new Vector3(21f, 1.3f, 0.6f),
                wallMaterial,
                environment.transform);
            CreateCube(
                "Wall South",
                new Vector3(0f, 0.65f, -11f),
                new Vector3(21f, 1.3f, 0.6f),
                wallMaterial,
                environment.transform);
            CreateCube(
                "Wall West",
                new Vector3(-10f, 0.65f, 1f),
                new Vector3(0.6f, 1.3f, 24f),
                wallMaterial,
                environment.transform);
            CreateCube(
                "Wall East",
                new Vector3(10f, 0.65f, 1f),
                new Vector3(0.6f, 1.3f, 24f),
                wallMaterial,
                environment.transform);

            return environment.transform;
        }

        private static void CreateMonsterSpawnArea(
            Transform parent,
            GameObject boarPrefab)
        {
            var spawnAreaObject = new GameObject(
                "Monster Spawn Area Controller");
            spawnAreaObject.transform.SetParent(parent, false);
            spawnAreaObject.transform.position = new Vector3(0f, 0.26f, 4f);

            MonsterSpawnArea spawnArea =
                spawnAreaObject.AddComponent<MonsterSpawnArea>();
            spawnArea.Configure(
                boarPrefab,
                3,
                SpawnAreaShape.Circle,
                3.25f,
                new Vector2(6.5f, 6.5f),
                1.4f,
                20260919);
        }

        private static GameObject CreateCube(
            string name,
            Vector3 position,
            Vector3 scale,
            Material material,
            Transform parent)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent, false);
            cube.transform.position = position;
            cube.transform.localScale = scale;
            cube.GetComponent<Renderer>().sharedMaterial = material;
            return cube;
        }

        private static void CreateActorRing(
            Transform visualRoot,
            Health health,
            string name,
            float diameter,
            Material visibleMaterial,
            Material fadeMaterial)
        {
            GameObject ring =
                GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = name;
            ring.transform.SetParent(visualRoot, false);
            ring.transform.localPosition = new Vector3(0f, 0.015f, 0f);
            ring.transform.localScale = new Vector3(diameter, 0.015f, diameter);
            Renderer ringRenderer = ring.GetComponent<Renderer>();
            ringRenderer.sharedMaterial = visibleMaterial;
            ringRenderer.shadowCastingMode = ShadowCastingMode.Off;
            ringRenderer.receiveShadows = false;
            ActorRingFade fade = ring.AddComponent<ActorRingFade>();
            fade.Configure(
                health,
                ringRenderer,
                visibleMaterial,
                fadeMaterial,
                0.35f);
            Object.DestroyImmediate(ring.GetComponent<Collider>());
        }

        private static void CreateLighting()
        {
            GameObject lightObject = new GameObject("Directional Light");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.94f, 0.84f);
            light.intensity = 1.8f;
            light.shadows = LightShadows.Soft;
            lightObject.transform.rotation = Quaternion.Euler(48f, -32f, 0f);

            Type additionalLightData = Type.GetType(
                "UnityEngine.Rendering.Universal.UniversalAdditionalLightData, Unity.RenderPipelines.Universal.Runtime");
            if (additionalLightData != null)
            {
                lightObject.AddComponent(additionalLightData);
            }
        }

        private static void CreateCamera(Transform target)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            CameraComponent camera =
                cameraObject.AddComponent<CameraComponent>();
            camera.fieldOfView = 50f;
            camera.nearClipPlane = 0.15f;
            camera.farClipPlane = 100f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.14f, 0.2f, 0.27f);
            cameraObject.AddComponent<AudioListener>();

            Type additionalCameraData = Type.GetType(
                "UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime");
            if (additionalCameraData != null)
            {
                cameraObject.AddComponent(additionalCameraData);
            }

            Vector3 offset = new Vector3(0f, 11f, -10f);
            Vector3 lookOffset = new Vector3(0f, 0.8f, 2.2f);
            cameraObject.transform.position = target.position + offset;
            cameraObject.transform.rotation = Quaternion.LookRotation(target.position + lookOffset - cameraObject.transform.position);
            TopDownCameraFollow follow = cameraObject.AddComponent<TopDownCameraFollow>();
            follow.Configure(target, offset, lookOffset, 0.16f);
        }

        private static VirtualJoystick CreateHud(GameObject player)
        {
            GameObject canvasObject = new GameObject(
                "Combat HUD",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            eventSystemObject.transform.SetSiblingIndex(0);

            Sprite baseSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Game/Art/UI/JoystickBase.png");
            Sprite handleSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Game/Art/UI/JoystickHandle.png");

            GameObject joystickObject = CombatDemoUiFactory.CreateObject(
                "Move Joystick",
                canvasObject.transform,
                typeof(CanvasRenderer),
                typeof(Image));
            RectTransform joystickRect = joystickObject.GetComponent<RectTransform>();
            joystickRect.anchorMin = Vector2.zero;
            joystickRect.anchorMax = Vector2.zero;
            joystickRect.pivot = new Vector2(0.5f, 0.5f);
            joystickRect.anchoredPosition = new Vector2(165f, 165f);
            joystickRect.sizeDelta = new Vector2(250f, 250f);
            Image joystickImage = joystickObject.GetComponent<Image>();
            joystickImage.sprite = baseSprite;
            joystickImage.color = new Color(1f, 1f, 1f, 0.78f);
            joystickImage.preserveAspect = true;

            GameObject handleObject = CombatDemoUiFactory.CreateObject(
                "Handle",
                joystickObject.transform,
                typeof(CanvasRenderer),
                typeof(Image));
            RectTransform handleRect = handleObject.GetComponent<RectTransform>();
            handleRect.anchorMin = new Vector2(0.5f, 0.5f);
            handleRect.anchorMax = new Vector2(0.5f, 0.5f);
            handleRect.pivot = new Vector2(0.5f, 0.5f);
            handleRect.anchoredPosition = Vector2.zero;
            handleRect.sizeDelta = new Vector2(105f, 105f);
            Image handleImage = handleObject.GetComponent<Image>();
            handleImage.sprite = handleSprite;
            handleImage.color = new Color(1f, 1f, 1f, 0.95f);
            handleImage.preserveAspect = true;
            handleImage.raycastTarget = false;

            VirtualJoystick joystick = joystickObject.AddComponent<VirtualJoystick>();
            joystick.Configure(joystickRect, handleRect);

            CombatDemoUiFactory.CreatePlayerHealthBar(
                canvasObject.transform,
                player.GetComponent<Health>());
            CombatDemoUiFactory.CreateExitButton(canvasObject.transform);

            return joystick;
        }

        private static void UpdateBuildSettings()
        {
            List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes
                .Where(scene => string.Equals(
                    scene.path,
                    ScenePath,
                    StringComparison.OrdinalIgnoreCase) == false)
                .ToList();
            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void ValidateCurrentScene()
        {
            PlayerCombatController[] players =
                Object.FindObjectsByType<PlayerCombatController>();
            MonsterCombatController[] monsters =
                Object.FindObjectsByType<MonsterCombatController>();
            MonsterSpawnArea[] spawnAreas =
                Object.FindObjectsByType<MonsterSpawnArea>();
            VirtualJoystick[] joysticks =
                Object.FindObjectsByType<VirtualJoystick>();

            if (players.Length != 1)
            {
                throw new InvalidOperationException($"Expected one player, found {players.Length}.");
            }

            if (monsters.Length != 0)
            {
                throw new InvalidOperationException($"Expected zero pre-placed monsters, found {monsters.Length}.");
            }

            if (spawnAreas.Length != 1 || spawnAreas[0].ConfiguredSpawnCount != 3 || spawnAreas[0].ConfiguredOptionCount < 1)
            {
                throw new InvalidOperationException("Expected one runtime spawn area configured for three monsters.");
            }

            if (spawnAreas[0].GetComponent<Renderer>() != null || GameObject.Find("Monster Spawn Area") != null)
            {
                throw new InvalidOperationException("The runtime spawn range must not have a visible scene marker.");
            }

            if (players[0].transform.Find("Visual/Player Ring") == null)
            {
                throw new InvalidOperationException("Player prefab instance is missing its actor-following blue ring.");
            }

            GameObject boarPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabRoot + "/Boar.prefab");
            if (boarPrefab == null || boarPrefab.transform.Find("Visual/Monster Ring") == null)
            {
                throw new InvalidOperationException("Monster prefab is missing its actor-following red ring.");
            }

            Transform monsterHealthBar =
                boarPrefab.transform.Find("Monster Health Bar");
            if (monsterHealthBar == null ||
                monsterHealthBar.GetComponent<TransientHealthBarVisibility>() == null ||
                monsterHealthBar.GetComponent<CameraFacingBillboard>() == null)
            {
                throw new InvalidOperationException(
                    "Monster prefab is missing its transient world health bar.");
            }

            if (joysticks.Length != 1)
            {
                throw new InvalidOperationException($"Expected one virtual joystick, found {joysticks.Length}.");
            }

            GameObject playerHealthBar = GameObject.Find("Player Health");
            Text playerHealthLabel =
                playerHealthBar?.GetComponentInChildren<Text>(true);
            if (playerHealthBar == null ||
                playerHealthBar.GetComponent<RectTransform>().sizeDelta.x < 500f ||
                playerHealthLabel == null ||
                playerHealthLabel.text.StartsWith("玩家") == false)
            {
                throw new InvalidOperationException(
                    "Player HUD requires the large Chinese health bar.");
            }

            if (GameObject.Find("Title") != null || GameObject.Find("Instructions") != null)
            {
                throw new InvalidOperationException("The title and instruction labels must not exist in the combat HUD.");
            }

            InputActionAsset inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            InputActionMap playerActions = inputActions?.FindActionMap("Player", false);
            if (inputActions == null || inputActions.actionMaps.Count != 1 ||
                playerActions == null || playerActions.actions.Count != 1 ||
                playerActions.FindAction("Move", false) == null)
            {
                throw new InvalidOperationException("Input actions must contain only the used Player/Move action.");
            }

            AnimatorController playerAnimator = AssetDatabase.LoadAssetAtPath<AnimatorController>(AnimatorRoot + "/Player.controller");
            AnimatorController boarAnimator = AssetDatabase.LoadAssetAtPath<AnimatorController>(AnimatorRoot + "/Boar.controller");
            ValidateLayeredAnimator(playerAnimator, "player");
            ValidateLayeredAnimator(boarAnimator, "monster");

            Scene scene = SceneManager.GetActiveScene();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Component[] components = root.GetComponentsInChildren<Component>(true);
                for (int index = 0; index < components.Length; index++)
                {
                    Component component = components[index];
                    if (component != null && component.GetType().FullName == "UnityEngine.AI.NavMeshAgent")
                    {
                        throw new InvalidOperationException("NavMeshAgent found, but this demo must not use NavMesh.");
                    }
                }
            }

        }

        private static void ValidateLayeredAnimator(AnimatorController controller, string actorLabel)
        {
            if (controller == null || controller.layers.Length != 2 ||
                controller.layers[0].name != "Locomotion" ||
                controller.layers[1].name != "Attack Overlay" ||
                controller.layers[1].avatarMask == null)
            {
                throw new InvalidOperationException(
                    $"The {actorLabel} Animator must have Locomotion plus masked Attack Overlay layers.");
            }
        }

        private static int ReadSetupVersion()
        {
            if (File.Exists(SetupMarkerPath) == false)
            {
                return 0;
            }

            return int.TryParse(File.ReadAllText(SetupMarkerPath), out int value) ? value : 0;
        }

        private static void WriteSetupMarker()
        {
            File.WriteAllText(SetupMarkerPath, SetupVersion.ToString());
        }
    }
}
