namespace Mcl.Core.Tools
{
    public static class X19SignHelper
    {
        private const string SignKey = "942894570397f6d1c9cca2535ad18a2b";
        private const string SignPrefix = "!x19sign!";

        public static string Sign(this string text)
        {
            return SignPrefix + text.Encrypt(SignKey);
        }

        public static string Decrypt(this string text)
        {
            return text.StartsWith(SignPrefix) 
                ? text.Remove(0, SignPrefix.Length).Decrypt(SignKey) 
                : text;
        }

        public static bool IsSigned(this string text)
        {
            return text.StartsWith(SignPrefix);
        }
    }
}