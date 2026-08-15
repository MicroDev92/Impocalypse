using System.Text;
using UnityEngine;

/// <summary>
/// Short room codes, made to be read out loud rather than copy-pasted.
/// </summary>
public static class RoomCode
{
    /// <summary>
    /// I and O are missing on purpose: they are the letters people confuse with 1
    /// and 0 when a code is read over voice chat, and dropping them removes that
    /// whole question rather than answering it. 24 letters in 4 places is ~332k
    /// codes - collisions are handled anyway, but they will effectively never happen.
    /// </summary>
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ";

    public const int Length = 4;

    public static string Generate()
    {
        var sb = new StringBuilder(Length);

        for (int i = 0; i < Length; i++)
            sb.Append(Alphabet[Random.Range(0, Alphabet.Length)]);

        return sb.ToString();
    }

    /// <summary>
    /// Uppercases and drops anything outside the alphabet. Photon room names are
    /// case-sensitive and players typing a code are not, so every code goes through
    /// here before it reaches the network.
    /// </summary>
    public static string Normalize(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return string.Empty;

        var sb = new StringBuilder(Length);

        foreach (char c in raw.ToUpperInvariant())
        {
            if (Alphabet.IndexOf(c) < 0) continue;

            sb.Append(c);
            if (sb.Length == Length) break;
        }

        return sb.ToString();
    }

    public static bool IsValid(string code) => Normalize(code).Length == Length;
}
