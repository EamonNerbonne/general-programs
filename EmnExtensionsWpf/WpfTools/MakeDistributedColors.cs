using System;
using System.Linq;
using System.Windows.Media;
using EmnExtensions.MathHelpers;

namespace EmnExtensions.Wpf.WpfTools
{
    public static partial class WpfTools
    {
        public static Color[] MakeDistributedColors(int N, MersenneTwister rnd = null)
        {
            rnd = rnd ?? RndHelper.ThreadLocalRandom;
            var offset = rnd.NextDouble();
            var colors = Enumerable.Range(0, N).Select(i => ColorSimple.FromColor(new HSL { H = (i + offset) / N, S = 0.8, L = 0.9 }.ToRGB())).ToArray();
            if (colors.Length > 1) {
                for (var iter = 0; iter < 10 + 10000 / (N * N); iter++) {
                    var lr = 1.0 / Math.Sqrt(iter * N * N + 1000);
                    for (var i = 0; i < colors.Length; i++) {
                        //colors[i].RepelFrom(ColorSimple.MinValue, 0.3 * lr);
                        //colors[i].RepelFrom(ColorSimple.MaxValue, 0.1 * lr);
                        //colors[i].RepelFrom(ColorSimple.LightGreenYellow, 0.05*lr);
                        colors[i].RepelFrom(ColorSimple.Random(rnd), lr * lr);
                        for (var j = 0; j < colors.Length; j++) {
                            if (i != j) {
                                colors[i].RepelFrom(colors[j], lr);
                            }
                        }
                    }

                    for (var i = 0; i < colors.Length; i++) {
                        colors[i].LimitBrightness(0.1, 0.7);
                    }

                    var min = colors.Aggregate(ColorSimple.MinValue, ColorSimple.Min);
                    var max = colors.Aggregate(ColorSimple.MaxValue, ColorSimple.Max);
                    for (var i = 0; i < colors.Length; i++) {
                        colors[i].ScaleBack(min, max);
                    }
                }
            }

            return colors.Select(c => c.ToWindowsColor()).ToArray();
        }

        struct ColorSimple
        {
            public static ColorSimple Random(MersenneTwister rnd)
                => new() { R = rnd.NextDouble0To1(), G = rnd.NextDouble0To1(), B = rnd.NextDouble0To1() };

            double R, G, B;

            public void RepelFrom(ColorSimple other, double lr)
            {
                var sqrdist = Math.Max(SqrDistTo(other), lr / 100);
                var force = lr / sqrdist;
                R = R + force * (R - other.R);
                G = G + force * (G - other.G);
                B = B + force * (B - other.B);
            }

            public void ScaleBack(ColorSimple min, ColorSimple max)
            {
                R = scaled(R, min.R, max.R);
                G = scaled(G, min.G, max.G);
                B = scaled(B, min.B, max.B);
            }

            public void LimitBrightness(double min, double max)
            {
                if (Sum > max) {
                    var scale = max / Sum;
                    R *= scale;
                    G *= scale;
                    B *= scale;
                } else if (Sum < min) {
                    var offset = min - Sum;
                    R += offset;
                    G += offset;
                    B += offset;
                }
            }

            public Color ToWindowsColor()
                => Color.FromRgb((byte)(255 * R + 0.5), (byte)(255 * G + 0.5), (byte)(255 * B + 0.5));

            public static ColorSimple Min(ColorSimple a, ColorSimple b)
                => new() { R = Math.Min(a.R, b.R), G = Math.Min(a.G, b.G), B = Math.Min(a.B, b.B) };

            public static ColorSimple Max(ColorSimple a, ColorSimple b)
                => new() { R = Math.Max(a.R, b.R), G = Math.Max(a.G, b.G), B = Math.Max(a.B, b.B) };

            public static ColorSimple MaxValue
                => new() { R = 1.0, G = 1.0, B = 1.0 };

            public static ColorSimple MinValue
                => new() { R = 0.0, G = 0.0, B = 0.0 };

            // ReSharper disable once UnusedMember.Local
            public static ColorSimple LightGreenYellow
                => new() { R = 0.9, G = 1, B = 0.7 };

            public double SqrDistTo(ColorSimple other)
                => (sqr(R - other.R) + sqr(G - other.G) + sqr(B - other.B)) / 3.0 - 0.5 * HueEmphasis * sqr(Sum - other.Sum);

            double Sum
                => 0.35 * R + 0.5 * G + 0.15 * B;

            static double sqr(double x)
                => x * x;

            static double scaled(double val, double min, double max)
                => max <= min || max < 0.99 && min > 0.01
                    ? val
                    : 0.01 + 0.98 * (val - min) / (max - min);

            const double HueEmphasis = 0.5;

            internal static ColorSimple FromColor(Color color)
                => new() {
                    R = color.ScR,
                    G = color.ScG,
                    B = color.ScB,
                };
        }
    }
}
