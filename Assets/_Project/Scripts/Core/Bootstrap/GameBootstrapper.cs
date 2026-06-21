using System.Collections;
using UnityEngine;
using _Project.Scripts.Utility;
using Core.Events;
using Core.Pooling;
using StateMachine;
using StateMachine.States;

namespace Core.Bootstrap
{
    /// <summary>
    /// Main bootstrapper that initializes all game systems.
    /// Ensures proper initialization order and service registration.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class GameBootstrapper : MonoBehaviour
    {
        [Header("Core Systems")]
        [SerializeField] private bool initializeEventBus = true;
        [SerializeField] private bool initializePoolManager = true;
        [SerializeField] private bool initializeStateMachine = true;

        [Header("Prefabs (Optional)")]
        [SerializeField] private GameObject eventBusPrefab;
        [SerializeField] private GameObject poolManagerPrefab;
        [SerializeField] private GameObject stateMachinePrefab;

        [Header("Debug")]
        [SerializeField] private bool enableServiceLogging = true;

        private GameStateMachine _stateMachine;

        private void Awake()
        {
            ServiceLocator.LoggingEnabled = enableServiceLogging;

            Debug.Log("[GameBootstrapper] Initializing game systems...");

            InitializeCoreServices();
            InitializeStateMachine();

            Debug.Log("[GameBootstrapper] Initialization complete");
        }

        /// <summary>
        /// Deferred state transition - waits one frame so all services
        /// have finished their Start() and have their references ready.
        /// </summary>
        private IEnumerator Start()
        {
            // Wait one frame for all Start() methods to complete
            yield return null;

            if (_stateMachine == null) yield break;

            // The lobby/waiting room is the separate GameRoom scene. By the time the
            // GameScene loads, the host has already pressed Start there - so we begin
            // the draft immediately rather than sitting in a now-redundant LobbyState.
            Debug.Log("[GameBootstrapper] Starting in DraftState");
            _stateMachine.ChangeState<DraftState>();
        }

        private void InitializeCoreServices()
        {
            // Event Bus
            if (initializeEventBus && !ServiceLocator.TryGetService<IEventBus>(out _))
            {
                if (eventBusPrefab != null)
                {
                    Instantiate(eventBusPrefab);
                }
                else
                {
                    var go = new GameObject("[EventBus]");
                    go.AddComponent<EventBus>();
                    DontDestroyOnLoad(go);
                }
                Debug.Log("[GameBootstrapper] EventBus initialized");
            }

            // Pool Manager
            if (initializePoolManager && !ServiceLocator.TryGetService<PoolManager>(out _))
            {
                if (poolManagerPrefab != null)
                {
                    Instantiate(poolManagerPrefab);
                }
                else
                {
                    var go = new GameObject("[PoolManager]");
                    go.AddComponent<PoolManager>();
                    DontDestroyOnLoad(go);
                }
                Debug.Log("[GameBootstrapper] PoolManager initialized");
            }
        }

        private void InitializeStateMachine()
        {
            if (!initializeStateMachine) return;

            if (ServiceLocator.TryGetService<GameStateMachine>(out var existing))
            {
                _stateMachine = existing;
                RegisterStates(existing);
                return;
            }

            GameStateMachine stateMachine;

            if (stateMachinePrefab != null)
            {
                var go = Instantiate(stateMachinePrefab);
                stateMachine = go.GetComponent<GameStateMachine>();
            }
            else
            {
                var go = new GameObject("[StateMachine]");
                stateMachine = go.AddComponent<GameStateMachine>();
                DontDestroyOnLoad(go);
            }

            if (stateMachine != null)
            {
                RegisterStates(stateMachine);
                Debug.Log("[GameBootstrapper] StateMachine initialized");
            }
        }

        private void RegisterStates(GameStateMachine stateMachine)
        {
            stateMachine.RegisterState(new LobbyState());
            stateMachine.RegisterState(new DraftState());
            stateMachine.RegisterState(new BuildState());
            stateMachine.RegisterState(new ScoringState());
            stateMachine.RegisterState(new GameOverState());

            _stateMachine = stateMachine;

            Debug.Log("[GameBootstrapper] Game states registered");
        }

        /// <summary>
        /// Validates that all required services are registered.
        /// Call this after scene load to verify setup.
        /// </summary>
        public static bool ValidateServices()
        {
            bool valid = true;

            if (!ServiceLocator.TryGetService<IEventBus>(out _))
            {
                Debug.LogError("[GameBootstrapper] Missing: IEventBus");
                valid = false;
            }

            if (!ServiceLocator.TryGetService<INetworkService>(out _))
            {
                Debug.LogWarning("[GameBootstrapper] Missing: INetworkService (may be expected in offline mode)");
            }

            if (!ServiceLocator.TryGetService<GameStateMachine>(out _))
            {
                Debug.LogError("[GameBootstrapper] Missing: GameStateMachine");
                valid = false;
            }

            if (!ServiceLocator.TryGetService<IGameManager>(out _))
            {
                Debug.LogWarning("[GameBootstrapper] Missing: IGameManager");
            }

            return valid;
        }
    }
}
