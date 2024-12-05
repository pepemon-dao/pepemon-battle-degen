using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace System
{
    static public class RandomBigInt
    {
        /// <summary>
        /// Returns a random BigInteger that is within a specified range.
        /// The lower bound is inclusive, and the upper bound is exclusive.
        /// </summary>
        /// <see cref="https://stackoverflow.com/a/68593532"/>
        public static BigInteger NextBigInteger(this Random random,
            BigInteger minValue, BigInteger maxValue)
        {
            if (minValue > maxValue) throw new ArgumentException();
            if (minValue == maxValue) return minValue;
            BigInteger zeroBasedUpperBound = maxValue - 1 - minValue; // Inclusive

            byte[] bytes = zeroBasedUpperBound.ToByteArray();

            // Search for the most significant non-zero bit
            byte lastByteMask = 0b11111111;
            for (byte mask = 0b10000000; mask > 0; mask >>= 1, lastByteMask >>= 1)
            {
                if ((bytes[bytes.Length - 1] & mask) == mask) break; // We found it
            }

            while (true)
            {
                random.NextBytes(bytes);
                bytes[bytes.Length - 1] &= lastByteMask;
                var result = new BigInteger(bytes);
                if (result <= zeroBasedUpperBound) return result + minValue;
            }
        }

        /// <summary>
        /// Returns a random BigInteger
        /// </summary>
        public static BigInteger NextBigInteger(this Random random)
        {
            // use max value for uint256 https://forum.openzeppelin.com/t/using-the-maximum-integer-in-solidity/3000
            return NextBigInteger(random,BigInteger.Zero,
                BigInteger.Parse("115792089237316195423570985008687907853269984665640564039457584007913129639935"));
        }
    }
}
