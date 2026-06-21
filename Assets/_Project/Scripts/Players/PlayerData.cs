using System;

namespace Players
{
    /// <summary>
    /// Data container for player information.
    /// Used across network and game systems.
    /// </summary>
    [Serializable]
    public class PlayerData
    {
        /// <summary>
        /// Network actor number (unique per room).
        /// </summary>
        public int ActorNumber { get; set; }

        /// <summary>
        /// Player's display name / nickname.
        /// </summary>
        public string Nickname { get; set; }

        /// <summary>
        /// Player's imp color (e.g., "red", "blue").
        /// </summary>
        public string ImpColor { get; set; }

        /// <summary>
        /// Player slot index (0-3).
        /// </summary>
        public int SlotIndex { get; set; }

        /// <summary>
        /// Whether this is the local player.
        /// </summary>
        public bool IsLocal { get; set; }

        /// <summary>
        /// Whether this player is the master client.
        /// </summary>
        public bool IsMasterClient { get; set; }

        /// <summary>
        /// Whether the player is ready in lobby.
        /// </summary>
        public bool IsReady { get; set; }

        /// <summary>
        /// Default constructor for object initializer usage.
        /// </summary>
        public PlayerData()
        {
            ActorNumber = -1;
            Nickname = "";
            ImpColor = "";
            SlotIndex = -1;
            IsLocal = false;
            IsMasterClient = false;
            IsReady = false;
        }

        /// <summary>
        /// Constructor with common parameters.
        /// </summary>
        public PlayerData(int actorNumber, string nickname, bool isLocal, bool isMasterClient = false)
        {
            ActorNumber = actorNumber;
            Nickname = nickname ?? $"Player {actorNumber}";
            IsLocal = isLocal;
            IsMasterClient = isMasterClient;
            ImpColor = "";
            SlotIndex = -1;
            IsReady = false;
        }

        /// <summary>
        /// Assigns identity (color and slot) to this player.
        /// </summary>
        public void AssignIdentity(string colorId, int slotIndex)
        {
            ImpColor = colorId;
            SlotIndex = slotIndex;
        }

        /// <summary>
        /// Sets the ready state.
        /// </summary>
        public void SetReady(bool ready)
        {
            IsReady = ready;
        }

        /// <summary>
        /// Updates master client status.
        /// </summary>
        public void SetMasterClient(bool isMaster)
        {
            IsMasterClient = isMaster;
        }

        public override string ToString()
        {
            return $"Player({ActorNumber}, {Nickname}, Color:{ImpColor}, Slot:{SlotIndex})";
        }
    }
}
