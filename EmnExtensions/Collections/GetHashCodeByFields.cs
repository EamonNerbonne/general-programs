using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace EmnExtensions.Collections {
	public static class GetHashCodeByFields<T> {
		public static readonly Func<T, int> Func = init();
		static Func<T, int> init() {
			var objParam = Expression.Parameter(typeof(T), "obj");
			var fields = typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			var fieldHashCodes = fields.Select((fi, n) => {
				MemberExpression fieldExpr = Expression.Field(objParam, fi);

				UnaryExpression ulongHashCodeExpr =
					Expression.Convert(
						Expression.Convert(Expression.Call(fieldExpr, GetHashcodeMethod(fi.FieldType)), typeof(uint)),
						typeof(ulong));
				var scaledHashExpr = Expression.Multiply(Expression.Constant((ulong)(2 * n + 1)), ulongHashCodeExpr);
				return fi.FieldType.IsValueType
					? (Expression)scaledHashExpr
					: Expression.Condition(Expression.Equal(Expression.Default(typeof(object)), fieldExpr),
						Expression.Constant((ulong)n), scaledHashExpr);
			});

			var accumulatorVar = Expression.Variable(typeof(ulong), "hashcodeAccumulator");
			var accumulatedHashExpr = fieldHashCodes.Aggregate((Expression)Expression.Constant((ulong)typeof(T).GetHashCode()), Expression.Add);
			var storeHashAcc = Expression.Assign(accumulatorVar, accumulatedHashExpr);
			var finalHashExpr = Expression.ExclusiveOr(Expression.Convert(accumulatorVar, typeof(int)),
				Expression.Convert(Expression.RightShift(accumulatorVar, Expression.Constant(32)), typeof(int)));

			var compiled =
				Expression.Lambda<Func<T, int>>(
					Expression.Block(new[] { accumulatorVar }, storeHashAcc, finalHashExpr), objParam).Compile();
			return compiled;
		}

		static MethodInfo GetHashcodeMethod(Type type) {
			var objectHashcodeMethod = ((Func<int>)(new object().GetHashCode)).Method;
			var method = type.GetMethod("GetHashCode", BindingFlags.Public | BindingFlags.Instance) ?? objectHashcodeMethod;
			return method.GetBaseDefinition() != objectHashcodeMethod ? objectHashcodeMethod : method;
		}
	}
}
