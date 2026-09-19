using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Test1.Combat.AI;
using Test1.Combat.Core;
using Test1.Combat.Player;
using Test1.Combat.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Test1.Combat.Editor
{
    public static class CombatDemoSmokeTest
    {
        private const string ScenePath = "Assets/Game/Scenes/CombatDemo.unity";

        private enum SmokePhase
        {
            WaitingForPlayMode,
            InitialIdle,
            Chase,
            AttackMovement,
            Combat,
            DeathPresentation,
            Respawn,
            Separation,
            LeavingPlayMode
        }

        private static readonly List<string> results = new();

        private static SmokePhase phase;
        private static double phaseStartedAt;
        private static double testStartedAt;
        private static PlayerCombatController player;
        private static MonsterSpawnArea spawnArea;
        private static VirtualJoystick joystick;
        private static MonsterCombatController[] monsters;
        private static Vector3 firstMonsterSpawn;
        private static Vector3 attackMovementStart;
        private static bool observedAttackMovement;
        private static float playerHealthBeforeCombat;
        private static float monsterHealthBeforeCombat;
        private static bool exitEditorOnComplete;
        private static bool previousEnterPlayModeOptionsEnabled;
        private static EnterPlayModeOptions previousEnterPlayModeOptions;
        private static int completionExitCode;

        [MenuItem("Tools/Test1/Run Combat Runtime Smoke Test")]
        public static void RunFromMenu()
        {
            StartTest(false);
        }

        public static void RunFromCommandLine()
        {
            StartTest(true);
        }

        private static void StartTest(bool shouldExitEditorOnComplete)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException("Exit Play Mode before starting the combat smoke test.");
            }

            results.Clear();
            EditorSceneManager.OpenScene(ScenePath);
            exitEditorOnComplete = shouldExitEditorOnComplete;
            previousEnterPlayModeOptionsEnabled = EditorSettings.enterPlayModeOptionsEnabled;
            previousEnterPlayModeOptions = EditorSettings.enterPlayModeOptions;
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;

            phase = SmokePhase.WaitingForPlayMode;
            phaseStartedAt = EditorApplication.timeSinceStartup;
            testStartedAt = phaseStartedAt;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            EditorApplication.isPlaying = true;
        }

        private static void Tick()
        {
            try
            {
                if (EditorApplication.timeSinceStartup - testStartedAt > 30d)
                {
                    Fail("Runtime smoke test exceeded the 30 second timeout.");
                    return;
                }

                switch (phase)
                {
                    case SmokePhase.WaitingForPlayMode:
                        TickWaitingForPlayMode();
                        break;
                    case SmokePhase.InitialIdle:
                        TickInitialIdle();
                        break;
                    case SmokePhase.Chase:
                        TickChase();
                        break;
                    case SmokePhase.AttackMovement:
                        TickAttackMovement();
                        break;
                    case SmokePhase.Combat:
                        TickCombat();
                        break;
                    case SmokePhase.DeathPresentation:
                        TickDeathPresentation();
                        break;
                    case SmokePhase.Respawn:
                        TickRespawn();
                        break;
                    case SmokePhase.Separation:
                        TickSeparation();
                        break;
                    case SmokePhase.LeavingPlayMode:
                        TickLeavingPlayMode();
                        break;
                }
            }
            catch (Exception exception)
            {
                Fail(exception.ToString());
            }
        }

        private static void TickWaitingForPlayMode()
        {
            if (EditorApplication.isPlaying == false)
            {
                return;
            }

            player = Object.FindAnyObjectByType<PlayerCombatController>();
            spawnArea = Object.FindAnyObjectByType<MonsterSpawnArea>();
            joystick = Object.FindAnyObjectByType<VirtualJoystick>();
            monsters = Object.FindObjectsByType<MonsterCombatController>()
                .OrderBy(monster => monster.name, StringComparer.Ordinal)
                .ToArray();

            if ((player == null || spawnArea == null || joystick == null || monsters.Length != 3) && PhaseElapsed < 8d)
            {
                return;
            }

            Require(player != null, "PlayerCombatController was not found in Play Mode.");
            Require(spawnArea != null, "MonsterSpawnArea was not found in Play Mode.");
            Require(joystick != null, "VirtualJoystick was not found in Play Mode.");
            Require(monsters.Length == 3, $"Expected three monsters in Play Mode, found {monsters.Length}.");
            Require(spawnArea.SpawnedMonsters.Count == 3, $"Spawn area tracked {spawnArea.SpawnedMonsters.Count} monsters instead of three.");
            Require(monsters.All(monster => spawnArea.ContainsWorldPoint(monster.transform.position)), "A runtime-spawned monster was outside the configured spawn area.");
            Require(GameObject.Find("Monster Spawn Area") == null, "A visible monster spawn range marker still exists in the runtime scene.");
            Require(player.transform.Find("Visual/Player Ring") != null, "The player is missing its blue actor ring.");
            Require(monsters.All(monster => monster.transform.Find("Visual/Monster Ring") != null), "A runtime monster is missing its red actor ring.");
            results.Add(
                "PASS runtime spawn area created 3 monsters inside " +
                "configured range");
            results.Add(
                "PASS spawn range is hidden and blue/red rings follow " +
                "their actors");

            firstMonsterSpawn = monsters[0].transform.position;
            BeginPhase(SmokePhase.InitialIdle);
        }

        private static void TickInitialIdle()
        {
            if (PhaseElapsed < 0.7d)
            {
                return;
            }

            float horizontalTravel = HorizontalDistance(monsters[0].transform.position, firstMonsterSpawn);
            Require(monsters[0].State == MonsterActionState.Idle, $"Monster should idle outside detection range, state was {monsters[0].State}.");
            Require(horizontalTravel < 0.08f, $"Idle monster drifted {horizontalTravel:0.000}m.");
            results.Add("PASS idle outside the 7m detection radius");

            Vector3 approachDirection = player.transform.position - firstMonsterSpawn;
            approachDirection.y = 0f;
            if (approachDirection.sqrMagnitude < 0.001f)
            {
                approachDirection = Vector3.back;
            }

            Vector3 approachPosition = firstMonsterSpawn + approachDirection.normalized * 4.25f;
            approachPosition.y = 0.26f;
            Teleport(player.gameObject, approachPosition);
            BeginPhase(SmokePhase.Chase);
        }

        private static void TickChase()
        {
            if (PhaseElapsed < 0.8d)
            {
                return;
            }

            float horizontalTravel = HorizontalDistance(monsters[0].transform.position, firstMonsterSpawn);
            Require(horizontalTravel > 0.35f, $"Monster did not chase the nearby player; travel was {horizontalTravel:0.000}m.");
            Require(monsters[0].State != MonsterActionState.Idle, "Monster remained idle after the player entered detection range.");
            results.Add(
                "PASS direct Rigidbody chase without NavMesh " +
                $"({horizontalTravel:0.00}m travel)");

            Vector3 combatPosition = monsters[0].transform.position - monsters[0].transform.forward * 1.15f;
            combatPosition.y = 0.26f;
            Teleport(player.gameObject, combatPosition);
            attackMovementStart = player.transform.position;
            observedAttackMovement = false;
            SetJoystickDirection(Vector2.right);
            BeginPhase(SmokePhase.AttackMovement);
        }

        private static void TickAttackMovement()
        {
            MeleeAttack playerAttack = player.GetComponent<MeleeAttack>();
            Rigidbody playerBody = player.GetComponent<Rigidbody>();
            Vector3 planarVelocity = playerBody.linearVelocity;
            planarVelocity.y = 0f;
            if (playerAttack.IsAttacking && planarVelocity.sqrMagnitude > 0.25f)
            {
                observedAttackMovement = true;
            }

            if (PhaseElapsed < 0.42d)
            {
                return;
            }

            SetJoystickDirection(Vector2.zero);
            float travel = HorizontalDistance(player.transform.position, attackMovementStart);
            Require(observedAttackMovement, "Player locomotion was not active while the attack was playing.");
            Require(travel > 0.3f, $"Player did not move during the attack; travel was {travel:0.000}m.");
            results.Add(
                "PASS attack and movement run concurrently " +
                $"({travel:0.00}m travel)");

            Vector3 combatPosition = monsters[0].transform.position - monsters[0].transform.forward * 1.15f;
            combatPosition.y = 0.26f;
            Teleport(player.gameObject, combatPosition);
            playerHealthBeforeCombat = player.GetComponent<Health>().CurrentHealth;
            monsterHealthBeforeCombat = monsters[0].GetComponent<Health>().CurrentHealth;
            BeginPhase(SmokePhase.Combat);
        }

        private static void TickCombat()
        {
            if (PhaseElapsed < 1.35d)
            {
                return;
            }

            Health playerHealth = player.GetComponent<Health>();
            Health monsterHealth = monsters[0].GetComponent<Health>();
            Require(monsterHealth.CurrentHealth < monsterHealthBeforeCombat, "Player did not auto-attack a monster in melee range.");
            Require(playerHealth.CurrentHealth < playerHealthBeforeCombat, "Monster did not attack the player in melee range.");
            results.Add(
                $"PASS mutual range attacks (player {playerHealthBeforeCombat:0}->{playerHealth.CurrentHealth:0}, monster {monsterHealthBeforeCombat:0}->{monsterHealth.CurrentHealth:0})");

            monsterHealth.TakeDamage(9999f, player.GetComponent<Combatant>());
            Require(
                monsterHealth.IsAlive == false,
                "Lethal damage did not put the monster into its dead " +
                "state.");
            Teleport(player.gameObject, new Vector3(0f, 0.26f, -9f));
            BeginPhase(SmokePhase.DeathPresentation);
        }

        private static void TickDeathPresentation()
        {
            if (PhaseElapsed < 1.35d)
            {
                return;
            }

            Health monsterHealth = monsters[0].GetComponent<Health>();
            Transform visual = monsters[0].transform.Find("Visual");
            Require(!monsterHealth.IsAlive, "Monster revived before the configured delay elapsed.");
            Require(
                visual != null && visual.gameObject.activeSelf == false,
                "Monster visual was not hidden after the death " +
                "presentation.");
            results.Add("PASS death state and corpse hide");
            BeginPhase(SmokePhase.Respawn);
        }

        private static void TickRespawn()
        {
            Health monsterHealth = monsters[0].GetComponent<Health>();
            if (monsterHealth.IsAlive == false && PhaseElapsed < 4d)
            {
                return;
            }

            Transform visual = monsters[0].transform.Find("Visual");
            float spawnError = HorizontalDistance(monsters[0].transform.position, firstMonsterSpawn);
            Require(monsterHealth.IsAlive, "Monster did not revive after the four second respawn delay.");
            Require(Mathf.Approximately(monsterHealth.CurrentHealth, monsterHealth.MaxHealth), "Monster did not respawn at full health.");
            Require(visual != null && visual.gameObject.activeSelf, "Monster visual was not restored on respawn.");
            Require(spawnError < 0.25f, $"Monster did not return to its spawn point; error was {spawnError:0.000}m.");
            results.Add(
                "PASS delayed full-health respawn at spawn point");

            Vector3 overlapPosition = new Vector3(5f, 0.26f, 8f);
            Teleport(monsters[1].gameObject, overlapPosition);
            Teleport(monsters[2].gameObject, overlapPosition);
            BeginPhase(SmokePhase.Separation);
        }

        private static void TickSeparation()
        {
            if (PhaseElapsed < 0.8d)
            {
                return;
            }

            float separation = HorizontalDistance(monsters[1].transform.position, monsters[2].transform.position);
            Require(separation > 0.25f, $"Overlapping monsters did not separate; distance was {separation:0.000}m.");
            results.Add(
                "PASS Rigidbody collision plus separation steering " +
                $"({separation:0.00}m apart)");
            Pass();
        }

        private static void TickLeavingPlayMode()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            EditorApplication.update -= Tick;
            EditorSettings.enterPlayModeOptionsEnabled = previousEnterPlayModeOptionsEnabled;
            EditorSettings.enterPlayModeOptions = previousEnterPlayModeOptions;
            if (exitEditorOnComplete)
            {
                EditorApplication.Exit(completionExitCode);
            }
        }

        private static double PhaseElapsed => EditorApplication.timeSinceStartup - phaseStartedAt;

        private static void BeginPhase(SmokePhase nextPhase)
        {
            phase = nextPhase;
            phaseStartedAt = EditorApplication.timeSinceStartup;
        }

        private static void Teleport(GameObject actor, Vector3 position)
        {
            Rigidbody body = actor.GetComponent<Rigidbody>();
            body.position = position;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            actor.transform.position = position;
            Physics.SyncTransforms();
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }

        private static void SetJoystickDirection(Vector2 direction)
        {
            PropertyInfo directionProperty = typeof(VirtualJoystick).GetProperty(
                nameof(VirtualJoystick.Direction),
                BindingFlags.Instance | BindingFlags.Public);
            MethodInfo setter = directionProperty?.GetSetMethod(true);
            if (setter == null)
            {
                throw new InvalidOperationException("VirtualJoystick.Direction setter was not found.");
            }

            setter.Invoke(joystick, new object[] { Vector2.ClampMagnitude(direction, 1f) });
        }

        private static void Require(bool condition, string message)
        {
            if (condition == false)
            {
                throw new InvalidOperationException(message);
            }
        }

        private static void Pass()
        {
            results.Insert(0, "TEST1_COMBAT_RUNTIME_SMOKE_OK");
            Debug.Log(
                "TEST1_COMBAT_RUNTIME_SMOKE_OK\n" +
                string.Join("\n", results));
            completionExitCode = 0;
            BeginPhase(SmokePhase.LeavingPlayMode);
            EditorApplication.isPlaying = false;
        }

        private static void Fail(string message)
        {
            results.Insert(0, "TEST1_COMBAT_RUNTIME_SMOKE_FAILED");
            results.Add(message);
            Debug.LogError("TEST1_COMBAT_RUNTIME_SMOKE_FAILED\n" + message);
            completionExitCode = 1;
            BeginPhase(SmokePhase.LeavingPlayMode);
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.isPlaying = false;
            }
        }

    }
}
