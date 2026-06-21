/// <summary>
/// Network event codes for Photon RaiseEvent communication.
/// Each code must be unique to prevent event collision.
/// </summary>
public static class NetworkEventCodes
{
    public const byte DealCards = 1;              // Master -> Players: Deal card coordinates
    public const byte SubmitPicks = 2;            // Players -> Master: Submit selected cards
    public const byte SpawnImps = 3;              // Master -> All: Spawn imps on grid
    public const byte AssignPlayerIdentities = 4; // Master -> All: Assign player colors
    public const byte StartNewRound = 5;          // Master -> All: Signal new round start
    public const byte GameOver = 6;               // Master -> All: Signal game end
    public const byte SyncGameState = 7;          // Master -> All: Full state sync (late joiners)

    // Construction phase
    public const byte PlaceCity = 8;              // Player -> Master: Request city placement
    public const byte CityPlaced = 9;             // Master -> All: Confirm city was placed
    public const byte ConstructionDone = 10;      // Player -> Master: Player finished constructing
    public const byte ConstructionPhaseEnd = 11;  // Master -> All: Construction phase over
}
