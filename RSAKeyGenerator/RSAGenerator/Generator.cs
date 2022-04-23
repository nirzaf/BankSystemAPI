namespace RSAGenerator
{
    using static System.Console;
    using System;
    using System.Security.Cryptography;

    public static class Generator
    {
        public static void Main()
        {
            RSACryptoServiceProvider rsa = new(4096);
            var publicKey = rsa.ToXmlString(false);
            var privateKey = rsa.ToXmlString(true);
            WriteLine($"Public key: {publicKey}");
            WriteLine("=====================================================================");
            WriteLine($"Private key: {privateKey}");
        }
    }
}