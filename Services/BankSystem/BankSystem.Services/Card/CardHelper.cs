﻿namespace BankSystem.Services.Card
{
    using System;
    using System.Linq;
    using System.Text;
    using Common.Configuration;
    using Microsoft.Extensions.Options;

    public class CardHelper : ICardHelper
    {
        // Convert to int.
        private static readonly Func<char, int> CharToInt = c => c - '0';

        private readonly BankConfiguration bankConfiguration;

        // New Double Concept => 7 * 2 = 14 => 1 + 4 = 5.
        private readonly Func<int, int> doubleDigit = i => (i * 2).ToString().ToCharArray().Select(CharToInt).Sum();
        private readonly Func<int, bool> isEven = i => i % 2 == 0;
        private readonly Random random;

        public CardHelper(IOptions<BankConfiguration> bankConfigurationOptions)
        {
            bankConfiguration = bankConfigurationOptions.Value;
            random = new Random();
        }

        /// <summary>
        /// Verify if the card number is valid.
        /// </summary>
        /// <param name="creditCardNumber"></param>
        /// <returns></returns>
        public bool CheckLuhn(string creditCardNumber)
        {
            var checkSum = creditCardNumber
                .ToCharArray()
                .Where(c => !char.IsWhiteSpace(c))
                .ToArray()
                .Select(CharToInt)
                .Reverse()
                .Select((digit, index) => isEven(index + 1) ? doubleDigit(digit) : digit)
                .Sum();

            return checkSum % 10 == 0;
        }

        public string Generate16DigitNumber()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(bankConfiguration.First3CardDigits);
            for (int i = 0; i < 12; i++)
            {
                sb.Append(random.Next(0, 10));
            }

            sb.Append(CreateCheckDigit(sb.ToString()));

            return !CheckLuhn(sb.ToString()) ? null : sb.ToString();
        }

        public string Generate3DigitSecurityCode()
        {
            StringBuilder stringBuilder = new StringBuilder();
            while (stringBuilder.Length < 3)
            {
                stringBuilder.Append(random.Next(10).ToString());
            }

            return stringBuilder.ToString();
        }

        private string CreateCheckDigit(string number)
        {
            var digitsSum = number
                .ToCharArray()
                .Where(c => !char.IsWhiteSpace(c))
                .ToArray()
                .Reverse()
                .Select(CharToInt)
                .Select((digit, index) => isEven(index) ? doubleDigit(digit) : digit)
                .Sum();

            digitsSum *= 9;

            return digitsSum
                .ToString()
                .ToCharArray()
                .Last()
                .ToString();
        }
    }
}