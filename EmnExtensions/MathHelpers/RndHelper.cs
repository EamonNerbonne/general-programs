using System;
using System.Security.Cryptography;

namespace EmnExtensions.MathHelpers
{
    public static class RndHelper
    {
        public static double NextNorm(this Random r)
            => MakeNormal(r.NextDouble(), r.NextDouble());

        static readonly RNGCryptoServiceProvider cryptGen = new();
        static readonly float MaxValF = ComputeMaxValF();
        static readonly double MaxValD = ComputeMaxValD();

        static float ComputeMaxValF()
        {
            long offset = 0;
            while (uint.MaxValue / (uint.MaxValue + (float)offset) >= 1.0) {
                offset++;
            }

            return uint.MaxValue + (float)offset; //offset will be 0x101
        }

        static double ComputeMaxValD()
        {
            long offset = 0;
            while (ulong.MaxValue / (ulong.MaxValue + (double)offset) >= 1.0) {
                offset++;
            }

            return ulong.MaxValue + (double)offset; //offset will be 0x801
        }

        public static double MakeNormal(double randVal1, double randVal2)
            => Math.Sin(2 * Math.PI * randVal1) * Math.Sqrt(-2 * Math.Log(1 - randVal2));

        /// <summary>
        /// Returns a double normally distributed around 0 with standard deviation 1.
        /// </summary>
        public static double MakeSecureNormal()
            => MakeNormal(MakeSecureDouble(), MakeSecureDouble());

        public static int usages;

        public static uint MakeSecureUInt()
        {
            var bytes = new byte[4];
            cryptGen.GetBytes(bytes);
            var retval = bytes[0] + ((uint)bytes[1] << 8) + ((uint)bytes[2] << 16) + ((uint)bytes[3] << 24);
            usages++;
            return retval;
        }

        public static ulong MakeSecureULong()
        {
            var bytes = new byte[8];
            cryptGen.GetBytes(bytes);
            var retval = bytes[0] + ((ulong)bytes[1] << 8) + ((ulong)bytes[2] << 16) + ((ulong)bytes[3] << 24)
                + ((ulong)bytes[4] << 32) + ((ulong)bytes[5] << 40) + ((ulong)bytes[6] << 48) + ((ulong)bytes[7] << 56);
            return retval;
        }

        /// <summary>
        /// Returns a double on a uniform distribution over [0..1)
        /// </summary>
        public static double MakeSecureDouble()
            => MakeSecureULong() / MaxValD;

        /// <summary>
        /// Returns a float on a uniform distribution over [0..1)
        /// </summary>
        public static float MakeSecureSingle()
            => MakeSecureUInt() / MaxValF;

        [ThreadStatic]
        static MersenneTwister randomImpl;

        public static MersenneTwister ThreadLocalRandom
        {
            get {
                if (randomImpl == null) {
                    randomImpl = new();
                }

                return randomImpl;
            }
        }
    }
}
