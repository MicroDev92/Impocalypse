namespace StateMachine
{
    /// <summary>
    /// Interface for game states in the state machine.
    /// Each state handles a specific phase of the game.
    /// </summary>
    public interface IGameState
    {
        /// <summary>
        /// Display name of the state.
        /// </summary>
        string StateName { get; }

        /// <summary>
        /// Called when entering this state.
        /// </summary>
        void Enter();

        /// <summary>
        /// Called when exiting this state.
        /// </summary>
        void Exit();

        /// <summary>
        /// Called every frame while in this state.
        /// </summary>
        void Update();

        /// <summary>
        /// Called at fixed intervals while in this state.
        /// </summary>
        void FixedUpdate();
    }

    /// <summary>
    /// Base class for game states with default implementations.
    /// </summary>
    public abstract class GameStateBase : IGameState
    {
        public abstract string StateName { get; }

        public virtual void Enter() { }
        public virtual void Exit() { }
        public virtual void Update() { }
        public virtual void FixedUpdate() { }
    }
}
