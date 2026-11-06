using System;
using System.Collections.Generic;
using UnityEngine;

namespace Players
{
    /// <summary>
    /// Service for managing player data and identities.
    /// Abstracts player management from network implementation.
    /// </summary>
    public interface IPlayerService
    {
        /// <summary>
        /// Gets player data by actor number.
        /// </summary>
        PlayerData GetPlayer(int actorNumber);

        /// <summary>
        /// Gets the local player's data.
        /// </summary>
        PlayerData GetLocalPlayer();

        /// <summary>
        /// Gets all players in the current room.
        /// </summary>
        IReadOnlyList<PlayerData> GetAllPlayers();

        /// <summary>
        /// Gets players sorted by actor number.
        /// </summary>
        IReadOnlyList<PlayerData> GetPlayersInSlotOrder();

        /// <summary>
        /// Gets player by color ID.
        /// </summary>
        PlayerData GetPlayerByColor(string colorId);

        /// <summary>
        /// The display colour for a player, resolved from the identity this service
        /// assigned. Anything drawing a player's colour must come through here: the
        /// board previously derived its own from actorNumber % 4, which disagreed with
        /// the assigned imp colour for three players out of four.
        /// </summary>
        Color GetPlayerColor(int actorNumber);

        /// <summary>
        /// Number of connected players.
        /// </summary>
        int PlayerCount { get; }

        /// <summary>
        /// Assigns identities (colors) to all players.
        /// Should only be called by master client.
        /// </summary>
        void AssignPlayerIdentities();

        /// <summary>
        /// Handles identity assignment from network.
        /// </summary>
        void HandleIdentityAssignment(int actorNumber, string colorId, int slotIndex);

        /// <summary>
        /// Refreshes player list from network.
        /// </summary>
        void RefreshPlayers();

        /// <summary>
        /// Event fired when a player joins.
        /// </summary>
        event Action<PlayerData> OnPlayerJoined;

        /// <summary>
        /// Event fired when a player leaves.
        /// </summary>
        event Action<PlayerData> OnPlayerLeft;

        /// <summary>
        /// Event fired when player identities are assigned.
        /// </summary>
        event Action OnIdentitiesAssigned;
    }
}
