using System;
using System.Collections.Generic;
using UnityEngine;
using _Project.Scripts.Utility;
using Core;
using Core.Events;

namespace StateMachine
{
    /// <summary>
    /// Game state machine that manages state transitions.
    /// Provides a clean way to handle game phases.
    /// </summary>
    public class GameStateMachine : MonoBehaviour
    {
        [SerializeField] private bool logStateChanges = true;

        private readonly Dictionary<Type, IGameState> _states = new();
        private IGameState _currentState;
        private IGameState _previousState;

        public IGameState CurrentState => _currentState;
        public IGameState PreviousState => _previousState;
        public string CurrentStateName => _currentState?.StateName ?? "None";

        // Events
        public event Action<IGameState, IGameState> OnStateChanged;

        private void Awake()
        {
            ServiceLocator.RegisterService<GameStateMachine>(this);
        }

        private void OnDestroy()
        {
            _currentState?.Exit();
            ServiceLocator.UnregisterService<GameStateMachine>();
        }

        private void Update()
        {
            _currentState?.Update();
        }

        private void FixedUpdate()
        {
            _currentState?.FixedUpdate();
        }

        /// <summary>
        /// Registers a state with the state machine.
        /// </summary>
        /// <typeparam name="T">State type</typeparam>
        /// <param name="state">State instance</param>
        public void RegisterState<T>(T state) where T : class, IGameState
        {
            _states[typeof(T)] = state;

            if (logStateChanges)
            {
                Debug.Log($"[StateMachine] Registered state: {state.StateName}");
            }
        }

        /// <summary>
        /// Registers a state using a factory function.
        /// </summary>
        /// <typeparam name="T">State type</typeparam>
        /// <param name="factory">Factory function to create the state</param>
        public void RegisterState<T>(Func<T> factory) where T : class, IGameState
        {
            var state = factory();
            RegisterState(state);
        }

        /// <summary>
        /// Changes to a new state.
        /// </summary>
        /// <typeparam name="T">Target state type</typeparam>
        /// <returns>True if state change was successful</returns>
        public bool ChangeState<T>() where T : class, IGameState
        {
            if (!_states.TryGetValue(typeof(T), out var newState))
            {
                Debug.LogError($"[StateMachine] State not registered: {typeof(T).Name}");
                return false;
            }

            return ChangeToState(newState);
        }

        /// <summary>
        /// Changes to a state by instance.
        /// </summary>
        /// <param name="newState">Target state</param>
        /// <returns>True if state change was successful</returns>
        public bool ChangeToState(IGameState newState)
        {
            if (newState == null)
            {
                Debug.LogError("[StateMachine] Cannot change to null state");
                return false;
            }

            // Exit current state
            _previousState = _currentState;
            _currentState?.Exit();

            // Enter new state
            _currentState = newState;

            if (logStateChanges)
            {
                var fromName = _previousState?.StateName ?? "None";
                Debug.Log($"[StateMachine] State changed: {fromName} -> {_currentState.StateName}");
            }

            _currentState.Enter();

            // Notify listeners
            OnStateChanged?.Invoke(_previousState, _currentState);

            // Publish event through EventBus if available
            if (ServiceLocator.TryGetService<IEventBus>(out var eventBus))
            {
                // Convert to GamePhase for compatibility
                var phase = GetGamePhaseFromState(_currentState);
                var prevPhase = GetGamePhaseFromState(_previousState);
                eventBus.Publish(new GamePhaseChangedEvent(phase, prevPhase));
            }

            return true;
        }

        /// <summary>
        /// Gets a registered state by type.
        /// </summary>
        /// <typeparam name="T">State type</typeparam>
        /// <returns>State instance or null</returns>
        public T GetState<T>() where T : class, IGameState
        {
            return _states.TryGetValue(typeof(T), out var state) ? (T)state : null;
        }

        /// <summary>
        /// Checks if currently in a specific state.
        /// </summary>
        /// <typeparam name="T">State type to check</typeparam>
        /// <returns>True if in the specified state</returns>
        public bool IsInState<T>() where T : class, IGameState
        {
            return _currentState is T;
        }

        /// <summary>
        /// Returns to the previous state.
        /// </summary>
        /// <returns>True if successful</returns>
        public bool ReturnToPreviousState()
        {
            if (_previousState == null)
            {
                Debug.LogWarning("[StateMachine] No previous state to return to");
                return false;
            }

            return ChangeToState(_previousState);
        }

        /// <summary>
        /// Converts a state to the corresponding GamePhase enum.
        /// </summary>
        private GamePhase GetGamePhaseFromState(IGameState state)
        {
            if (state == null) return GamePhase.Lobby;

            return state.StateName switch
            {
                "Lobby" => GamePhase.Lobby,
                "Draft" => GamePhase.Draft,
                "Build" => GamePhase.Build,
                "Scoring" => GamePhase.Scoring,
                "GameOver" => GamePhase.GameOver,
                _ => GamePhase.Lobby
            };
        }

        /// <summary>
        /// Debug: Lists all registered states.
        /// </summary>
        public void LogRegisteredStates()
        {
            Debug.Log("[StateMachine] Registered states:");
            foreach (var kvp in _states)
            {
                var marker = kvp.Value == _currentState ? " (current)" : "";
                Debug.Log($"  - {kvp.Value.StateName}{marker}");
            }
        }
    }
}
