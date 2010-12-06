using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using NUnit.Framework;
using PrintExpression;

namespace PrintExpressionTest {
	[TestFixture]
	public class ExpressionPrinterTest {
		[Test]
		public void TestAdd() {
			var x = 0;
			Console.WriteLine(ExpressionToCode.ToCode(() => 1 + x + 2 == 4));
		}

	}
}
