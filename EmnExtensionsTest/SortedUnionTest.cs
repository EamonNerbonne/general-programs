﻿using System;
using System.Linq;
using Xunit;
using EmnExtensions.MathHelpers;
using EmnExtensions.Algorithms;
using EmnExtensions.DebugTools;
using ExpressionToCodeLib;

namespace EmnExtensionsTest {
	public class SortedUnionTest {
		[Fact]
		public void ComparedWithDistinct() {
			var rng = new MersenneTwister(37);
			var sets = Enumerable.Range(0, 3).Select(
				si => Enumerable.Range(0, 10000).Select(ei => rng.Next(100000)).OrderBy(x => x).ToArray()
				).ToArray();


			TimeSpan distinctTime = TimeSpan.Zero, sortedUnionTime = TimeSpan.Zero;

			var distinctList = DTimer.TimeFunc(() => sets.SelectMany(x => x).Distinct().OrderBy(x => x).ToArray(), t => distinctTime = t);
			var sortedUnionList = DTimer.TimeFunc(() => SortedUnionAlgorithm.SortedUnion(sets).ToArray(), t => sortedUnionTime = t);

			PAssert.That(() => distinctList.SequenceEqual(sortedUnionList));
			PAssert.That(() => TimeSpan.FromMilliseconds(sortedUnionTime.TotalMilliseconds*5.0) < distinctTime);
		}
	}
}
