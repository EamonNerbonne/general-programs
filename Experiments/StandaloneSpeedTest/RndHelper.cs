﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;


namespace EmnExtensions.MathHelpers
{
    public static class RndHelper
    {
        public static double NextNorm(this Random r) {//from http://www.cs.princeton.edu/introcs/21function/MyMath.java.html
            return MakeNormal(r.NextDouble(), r.NextDouble());
        }
        static RNGCryptoServiceProvider cryptGen = new RNGCryptoServiceProvider();
        static float MaxValF;
        static double MaxValD;
        static RndHelper() {
            long offset = 0;
            while ((float)uint.MaxValue/((float)uint.MaxValue + (float)offset) >=1.0 ) offset++;
            MaxValF = (float)uint.MaxValue + (float)offset;//offset will be 0x101
            offset = 0;
            while ((double)ulong.MaxValue/((double)ulong.MaxValue + (double)offset) >=1.0 ) offset++;
            MaxValD = (double)ulong.MaxValue + (double)offset;//offset will be 0x801
        }

        public static double MakeNormal(double randVal1, double randVal2) {
            return Math.Sin(2 * Math.PI * randVal1) * Math.Sqrt((-2 * Math.Log(1 - randVal2)));
        }
        /// <summary>
        /// Returns a double normally distributed around 0 with standard deviation 1.
        /// </summary>
        public static double MakeSecureNormal() {
            return MakeNormal(MakeSecureDouble(), MakeSecureDouble());
        }

        public static uint MakeSecureUInt() {
            byte[] bytes = new byte[4];
            cryptGen.GetBytes(bytes);
            uint retval = (uint)bytes[0] + ((uint)bytes[1] << 8) + ((uint)bytes[2] << 16) + ((uint)bytes[3] << 24);
            return retval;
        }
        public static ulong MakeSecureULong() {
            byte[] bytes = new byte[8];
            cryptGen.GetBytes(bytes);
            ulong retval = (ulong)bytes[0] + ((ulong)bytes[1] << 8) + ((ulong)bytes[2] << 16) + ((ulong)bytes[3] << 24)
                + ((ulong)bytes[4] << 32) + ((ulong)bytes[5] << 40) + ((ulong)bytes[6] << 48) + ((ulong)bytes[7] << 56);
            return retval;
        }
        /// <summary>
        /// Returns a double on a uniform distribution over [0..1)
        /// </summary>
        public static double MakeSecureDouble() {
            return (double)MakeSecureULong() / (MaxValD);
        }

        /// <summary>
        /// Returns a float on a uniform distribution over [0..1)
        /// </summary>
        public static float MakeSecureSingle() {
            return (float)MakeSecureUInt() / (MaxValF);
        }

    }
}
