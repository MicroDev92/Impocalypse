public static class NetworkEvents
{
    public const byte DealCards = 1;              // Sent by GameOrchestrator to each player with their card hand
    public const byte SubmitPicks = 2;            // Sent by players to GameOrchestrator with their selected cards
    public const byte SpawnImps = 3;              // Sent by GameOrchestrator to all clients to spawn imps
    public const byte AssignPlayerIdentities = 4; // Player identity assignment
    public const byte StartNewRound = 4;          // (Optional) Could be used to trigger round-start UI
    public const byte GameOver = 5;               // (Optional) Sent to signal end of game
}