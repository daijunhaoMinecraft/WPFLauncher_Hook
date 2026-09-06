using System;

namespace Mcl.Core.Tools;

public static class StringExtensions
{
    private static readonly Random Random = new Random();
    private static readonly object Sync = new object();

    public static string RandStringRunes(int length) =>
        RandomCharacters(length, "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789");

    internal static string RandomCharacters(int length, string alphabet)
    {
        if (length < 0) throw new ArgumentOutOfRangeException(nameof(length));
        var result = new char[length];
        lock (Sync)
            for (var index = 0; index < result.Length; index++)
                result[index] = alphabet[Random.Next(alphabet.Length)];
        return new string(result);
    }
}