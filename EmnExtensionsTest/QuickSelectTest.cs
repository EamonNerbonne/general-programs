using System;
using System.Collections.Generic;
using System.Linq;
using EmnExtensions.Algorithms;
using EmnExtensions.DebugTools;
using EmnExtensions.MathHelpers;
using ExpressionToCodeLib;
using Xunit;

namespace EmnExtensionsTest
{
    public sealed class QuickSelectTest
    {
        const int MaxSize = 2000000;

        public static IEnumerable<int> Sizes()
        {
            for (var i = 1; i < MaxSize; i = (int)(i * 1.1) + 1) {
                yield return i;
            }
        }

        public static IEnumerable<object[]> SizesMemberData()
            => Sizes().Select(i => new[] { (object)i });

        [Theory]
        [MemberData(nameof(SizesMemberData))]
        public void RndTest(int size)
        {
            for (var i = 0; i < MaxSize / size; i++) {
                var list = Enumerable.Repeat(0, size).Select(x => RndHelper.ThreadLocalRandom.NextNormal()).ToArray();
                var listB = list.ToArray();
                var k = RndHelper.ThreadLocalRandom.Next(size);
                Assert.Equal(SelectionAlgorithm.QuickSelect(list, k), SelectionAlgorithm.SlowSelect(listB, k));
                Array.Sort(list);
                Assert.Equal(list, listB);
            }
        }


        [Fact]
        public void SpeedTest()
        {
            var rnd = RndHelper.ThreadLocalRandom;
            var list0 = Enumerable.Repeat(0, MaxSize).Select(x => rnd.NextNormal()).ToArray();
            var kf = rnd.Next();
            double ignoreQ = 0, ignoreS = 0;
            var listQ = list0.ToArray();
            var listS = list0.ToArray();
            foreach (var size in Sizes()) {
                var durationS_ms = DTimer.BenchmarkAction(() => { ignoreS += SelectionAlgorithm.SlowSelect(listS, kf % size, 0, size); }, 10).TotalMilliseconds;
                var durationQ_ms = DTimer.BenchmarkAction(() => { ignoreQ += SelectionAlgorithm.QuickSelect(listQ, kf % size, 0, size); }, 10).TotalMilliseconds;
                var details = "kf % size: " + kf % size + "\nsize: " + size;
                Assert.Equal(ignoreQ, ignoreS);
                PAssert.That(() => durationQ_ms <= durationS_ms, details);
                var scaling = 1 + 2 * (Math.Log(Math.E * Math.E * Math.E * Math.E + size) - 4);
                PAssert.That(() => scaling >= 1.0);
                PAssert.That(() => durationQ_ms - TimeSpan.FromTicks(1).TotalMilliseconds <= durationS_ms / scaling, "scaling: " + scaling + "\n" + details);
            }
        }
    }
}
