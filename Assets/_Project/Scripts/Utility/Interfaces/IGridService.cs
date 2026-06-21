using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Interface for the grid management service.
/// Defines the contract for grid operations and tile management.
/// </summary>
public interface IGridService
{
    /// <summary>
    /// Gets a tile by its coordinate string (e.g., "A1", "B3")
    /// </summary>
    /// <param name="coord">Coordinate string</param>
    /// <returns>Tile data or null if not found</returns>
    TileData GetTileByCoord(string coord);
    
    /// <summary>
    /// Gets a tile by its grid position
    /// </summary>
    /// <param name="position">Grid position</param>
    /// <returns>Tile data or null if not found</returns>
    TileData GetTileByPosition(Vector2Int position);
    
    /// <summary>
    /// Gets random coordinates for card dealing
    /// </summary>
    /// <param name="count">Number of coordinates to get</param>
    /// <returns>Array of coordinate strings</returns>
    string[] GetRandomCoords(int count);
    
    /// <summary>
    /// Gets all tiles in the grid
    /// </summary>
    /// <returns>List of all tiles</returns>
    List<TileData> GetAllTiles();
    
    /// <summary>
    /// Gets tiles of a specific type
    /// </summary>
    /// <param name="tileType">Type of tiles to get</param>
    /// <returns>List of tiles of the specified type</returns>
    List<TileData> GetTilesByType(TileData.TileType tileType);
    
    /// <summary>
    /// Gets tiles within a radius of a center point
    /// </summary>
    /// <param name="center">Center position</param>
    /// <param name="radius">Radius in grid units</param>
    /// <returns>List of tiles within radius</returns>
    List<TileData> GetTilesInRadius(Vector2Int center, float radius);
    
    /// <summary>
    /// Checks if a coordinate is valid
    /// </summary>
    /// <param name="coord">Coordinate to validate</param>
    /// <returns>True if coordinate is valid</returns>
    bool IsValidCoordinate(string coord);
    
    /// <summary>
    /// Gets the grid dimensions
    /// </summary>
    /// <returns>Grid dimensions as Vector2Int</returns>
    Vector2Int GetGridDimensions();
    
    /// <summary>
    /// Regenerates the grid
    /// </summary>
    void RegenerateGrid();
    
    /// <summary>
    /// Gets all resource tiles (wood, carrot, fish)
    /// </summary>
    /// <returns>List of all resource tiles</returns>
    List<TileData> GetResourceTiles();
    
    /// <summary>
    /// Gets tiles of a specific resource type
    /// </summary>
    /// <param name="resourceType">Resource type to get</param>
    /// <returns>List of tiles of the specified resource type</returns>
    List<TileData> GetTilesByResourceType(TileData.TileType resourceType);

    /// <summary>
    /// Gets all playable tile coordinates (for deck building).
    /// </summary>
    string[] GetAllPlayableCoords();

    /// <summary>
    /// Gets orthogonal (4-directional) neighbors of a tile.
    /// </summary>
    List<TileData> GetOrthogonalNeighbors(string coord);

    /// <summary>
    /// Gets all tiles owned by a specific player.
    /// </summary>
    List<TileData> GetOwnedTiles(int actorNumber);
}
