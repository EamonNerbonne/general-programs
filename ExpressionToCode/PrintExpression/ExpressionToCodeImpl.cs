﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace ExpressionToCodeLib {
	class ExpressionToCodeImpl : IExpressionTypeDispatch {
		#region General Helpers
		Action<ExprTextPart> sink;
		internal ExpressionToCodeImpl(Action<ExprTextPart> sink) { this.sink = sink; }
		void Sink(ExprTextPart etp) { sink(etp); }
		void Sink(string text) { sink(ExprTextPart.TextOnly(text)); }
		void Sink(string text, Expression value) { sink(ExprTextPart.TextAndExpr(text, value)); }

		void NestExpression(ExpressionType? parentType, Expression child, bool parensIfEqualRank = false) {
			int parentRank = parentType == null ? 0 : ExpressionPrecedence.Rank(parentType.Value);
			bool needsParens = parentRank > 0 && (parensIfEqualRank ? parentRank - 1 : parentRank) < ExpressionPrecedence.Rank(child.NodeType);
			if (needsParens) sink(ExprTextPart.TextOnly("("));
			RawChildDispatch(child);
			if (needsParens) sink(ExprTextPart.TextOnly(")"));
		}

		void RawChildDispatch(Expression child) { this.ExpressionDispatch(child); }

		void JoinDispatch<T>(IEnumerable<T> children, string joiner, Action<T> childVisitor) {
			bool isfirst = true;
			foreach (var child in children) {
				if (isfirst) isfirst = false;
				else Sink(joiner);
				childVisitor(child);
			}
		}

		void JoinDispatch(IEnumerable<Expression> children, string joiner) { JoinDispatch(children, joiner, RawChildDispatch); }

		void ArgListDispatch(IEnumerable<Expression> children, Expression value = null, string open = "(", string close = ")", string joiner = ", ") {
			if (value != null)
				Sink(open, value);
			else
				Sink(open);
			JoinDispatch(children, joiner);
			Sink(close);
		}

		void BinaryDispatch(string op, Expression e) {
			BinaryExpression be = (BinaryExpression)e;
			NestExpression(be.NodeType, be.Left);
			Sink(" " + op + " ", e);
			NestExpression(be.NodeType, be.Right, true);
		}

		void UnaryDispatch(string op, Expression e) {
			var ue = (UnaryExpression)e;
			bool needsSpace = ExpressionPrecedence.TokenizerConfusable(ue.NodeType, ue.Operand.NodeType);
			Sink(op + (needsSpace ? " " : ""), e);
			NestExpression(ue.NodeType, ue.Operand);
		}

		void UnaryPostfixDispatch(string op, Expression e) { UnaryExpression ue = (UnaryExpression)e; NestExpression(ue.NodeType, ue.Operand); Sink(op, e); }
		void TypeOpDispatch(string op, Expression e) { NestExpression(e.NodeType, ((TypeBinaryExpression)e).Expression); Sink(" " + op + " " + CSharpFriendlyTypeName.Get(((TypeBinaryExpression)e).TypeOperand), e); }
		#endregion

		#region Hard Cases
		public void DispatchLambda(Expression e) {
			LambdaExpression le = (LambdaExpression)e;
			if (le.Parameters.Count == 1)
				NestExpression(e.NodeType, le.Parameters.Single());
			else
				ArgListDispatch(le.Parameters);
			Sink(" => ");
			NestExpression(le.NodeType, le.Body);
		}

		public void DispatchMemberAccess(Expression e) {
			MemberExpression me = (MemberExpression)e;
			Expression memberOfExpr = me.Expression;
			if (memberOfExpr != null && !memberOfExpr.Type.Name.StartsWith("<>")) {
				NestExpression(e.NodeType, memberOfExpr);
				Sink(".");
			} else if (ReflectionHelpers.IsMemberInfoStatic(me.Member))
				Sink(CSharpFriendlyTypeName.Get(me.Member.ReflectedType) + ".");

			Sink(me.Member.Name, e);
		}

		public void DispatchCall(Expression e) {
			MethodCallExpression mce = (MethodCallExpression)e;
			var optPropertyInfo = ReflectionHelpers.GetPropertyIfGetter(mce.Method);
			if (optPropertyInfo != null)
				DispatchIndex(Expression.MakeIndex(mce.Object, optPropertyInfo, mce.Arguments));
			else {
				bool isExtensionMethod = mce.Method.IsStatic && mce.Method.GetCustomAttributes(typeof(ExtensionAttribute), false).Any() && mce.Arguments.Any() && mce.Object == null;
				Expression objectExpr = isExtensionMethod ? mce.Arguments.First() : mce.Object;
				SinkMethodName(mce, objectExpr);
				ArgListDispatch(isExtensionMethod ? mce.Arguments.Skip(1) : mce.Arguments);
			}
		}

		void SinkMethodName(MethodCallExpression mce, Expression objExpr) {
			if (objExpr != null)
				NestExpression(mce.NodeType, objExpr);
			else if (mce.Method.IsStatic) //TODO: better deal with own members both static and non-static);
				Sink(CSharpFriendlyTypeName.Get(mce.Method.ReflectedType));
			Sink("." + mce.Method.Name, mce);
		}

		public void DispatchIndex(Expression e) {
			var ie = (IndexExpression)e;
			NestExpression(ie.NodeType, ie.Object);
			if (ie.Indexer.Name != "Item") Sink("." + ie.Indexer.Name);//TODO: is this OK?
			ArgListDispatch(ie.Arguments, ie, "[", "]");
		}

		public void DispatchInvoke(Expression e) {
			InvocationExpression ie = (InvocationExpression)e;
			NestExpression(ie.NodeType, ie.Expression);
			ArgListDispatch(ie.Arguments, ie);
		}


		public void DispatchConstant(Expression e) {
			string codeRepresentation = ObjectToCode.PlainObjectToCode(((ConstantExpression)e).Value);
			if (codeRepresentation == null)
				throw new NotImplementedException();
			else
				Sink(codeRepresentation);
		}

		public void DispatchConditional(Expression e) {
			ConditionalExpression ce = (ConditionalExpression)e;
			NestExpression(ce.NodeType, ce.Test);
			Sink(" ? ", e);
			NestExpression(ce.NodeType, ce.IfTrue);
			Sink(" : ");
			NestExpression(ce.NodeType, ce.IfFalse);
		}

		public void DispatchListInit(Expression e) {
			ListInitExpression lie = (ListInitExpression)e;
			Sink("new ", lie);
			Sink(CSharpFriendlyTypeName.Get(lie.NewExpression.Constructor.ReflectedType));
			if (lie.NewExpression.Arguments.Any())
				ArgListDispatch(lie.NewExpression.Arguments);

			Sink(" { ");
			JoinDispatch(lie.Initializers, ", ", DispatchElementInit);
			Sink(" }");
		}

		void DispatchElementInit(ElementInit elemInit) {
			if (elemInit.Arguments.Count != 1)
				ArgListDispatch(elemInit.Arguments, open: "{ ", close: " }");
			else
				RawChildDispatch(elemInit.Arguments.Single());
		}

		void DispatchMemberBinding(MemberBinding mb) {
			Sink(mb.Member.Name + " = ");
			if (mb is MemberMemberBinding) {
				var mmb = (MemberMemberBinding)mb;
				Sink("{ ");
				JoinDispatch(mmb.Bindings, ", ", DispatchMemberBinding);
				Sink(" }");
			} else if (mb is MemberListBinding) {
				var mlb = (MemberListBinding)mb;
				Sink("{ ");
				JoinDispatch(mlb.Initializers, ", ", DispatchElementInit);
				Sink(" }");
			} else if (mb is MemberAssignment) {
				NestExpression(ExpressionType.Assign, ((MemberAssignment)mb).Expression, true);
			} else
				throw new NotImplementedException("Member binding of unknown type: " + mb.GetType());
		}

		public void DispatchMemberInit(Expression e) {
			var mie = (MemberInitExpression)e;
			Sink("new ", mie);
			Sink(CSharpFriendlyTypeName.Get(mie.NewExpression.Constructor.ReflectedType));
			if (mie.NewExpression.Arguments.Any())
				ArgListDispatch(mie.NewExpression.Arguments);

			Sink(" { ");
			JoinDispatch(mie.Bindings, ", ", DispatchMemberBinding);
			Sink(" }");
		}

		public void DispatchNew(Expression e) {
			NewExpression ne = (NewExpression)e;
			Sink("new " + CSharpFriendlyTypeName.Get(ne.Type), ne);
			ArgListDispatch(ne.Arguments);
			//TODO: deal with anonymous types.
		}

		public void DispatchNewArrayInit(Expression e) {
			NewArrayExpression nae = (NewArrayExpression)e;
			Type arrayElemType = nae.Type.GetElementType();
			bool implicitTypeOK = nae.Expressions.Any() && nae.Expressions.All(expr => expr.Type == arrayElemType);
			Sink("new" + (implicitTypeOK ? "" :" "+ CSharpFriendlyTypeName.Get(arrayElemType)) + "[] ", nae);
			ArgListDispatch(nae.Expressions, open: "{ ", close: " }");
		}

		public void DispatchNewArrayBounds(Expression e) {
			NewArrayExpression nae = (NewArrayExpression)e;
			Type arrayElemType = nae.Type.GetElementType();
			Sink("new " + CSharpFriendlyTypeName.Get(arrayElemType), nae);
			ArgListDispatch(nae.Expressions, open: "[", close: "]");
		}
		#endregion

		#region Easy Cases
		public void DispatchPower(Expression e) { Sink("Math.Pow", e); ArgListDispatch(new[] { ((BinaryExpression)e).Left, ((BinaryExpression)e).Right }); }
		public void DispatchAdd(Expression e) { BinaryDispatch("+", e); }
		public void DispatchAddChecked(Expression e) { BinaryDispatch("+", e); } //TODO: checked
		public void DispatchAnd(Expression e) { BinaryDispatch("&", e); }
		public void DispatchAndAlso(Expression e) { BinaryDispatch("&&", e); }
		public void DispatchArrayLength(Expression e) { NestExpression(e.NodeType, ((UnaryExpression)e).Operand); Sink(".Length", e); }
		public void DispatchArrayIndex(Expression e) { NestExpression(e.NodeType, ((BinaryExpression)e).Left); Sink("[", e); NestExpression(null, ((BinaryExpression)e).Right); Sink("]"); }
		public void DispatchCoalesce(Expression e) { BinaryDispatch("??", e); }
		public void DispatchConvert(Expression e) { UnaryDispatch("(" + CSharpFriendlyTypeName.Get(e.Type) + ")", e); }
		public void DispatchConvertChecked(Expression e) { UnaryDispatch("(" + CSharpFriendlyTypeName.Get(e.Type) + ")", e); } //TODO: get explicit and implicit conversion operators right.
		public void DispatchDivide(Expression e) { BinaryDispatch("/", e); }
		public void DispatchEqual(Expression e) { BinaryDispatch("==", e); }
		public void DispatchExclusiveOr(Expression e) { BinaryDispatch("^", e); }
		public void DispatchGreaterThan(Expression e) { BinaryDispatch(">", e); }
		public void DispatchGreaterThanOrEqual(Expression e) { BinaryDispatch(">=", e); }
		public void DispatchLeftShift(Expression e) { BinaryDispatch("<<", e); }
		public void DispatchLessThan(Expression e) { BinaryDispatch("<", e); }
		public void DispatchLessThanOrEqual(Expression e) { BinaryDispatch("<=", e); }
		public void DispatchModulo(Expression e) { BinaryDispatch("%", e); }
		public void DispatchMultiply(Expression e) { BinaryDispatch("*", e); }
		public void DispatchMultiplyChecked(Expression e) { BinaryDispatch("*", e); }
		public void DispatchNegate(Expression e) { UnaryDispatch("-", e); }
		public void DispatchUnaryPlus(Expression e) { UnaryDispatch("+", e); }
		public void DispatchNegateChecked(Expression e) { UnaryDispatch("-", e); }
		public void DispatchNot(Expression e) { UnaryDispatch(e.Type == typeof(bool) || e.Type == typeof(bool?) ? "!" : "~", e); }
		public void DispatchNotEqual(Expression e) { BinaryDispatch("!=", e); }
		public void DispatchOr(Expression e) { BinaryDispatch("|", e); }
		public void DispatchOrElse(Expression e) { BinaryDispatch("||", e); }
		public void DispatchParameter(Expression e) { Sink(((ParameterExpression)e).Name, e); }
		public void DispatchQuote(Expression e) { NestExpression(e.NodeType, ((UnaryExpression)e).Operand); }
		public void DispatchRightShift(Expression e) { BinaryDispatch(">>", e); }
		public void DispatchSubtract(Expression e) { BinaryDispatch("-", e); }
		public void DispatchSubtractChecked(Expression e) { BinaryDispatch("-", e); }
		public void DispatchTypeAs(Expression e) { UnaryPostfixDispatch(" as " + CSharpFriendlyTypeName.Get(e.Type), e); }
		public void DispatchTypeIs(Expression e) { TypeOpDispatch("is", e); }
		public void DispatchAssign(Expression e) { BinaryDispatch("=", e); }
		public void DispatchDecrement(Expression e) { UnaryPostfixDispatch(" - 1", e); }
		public void DispatchIncrement(Expression e) { UnaryPostfixDispatch(" + 1", e); }
		public void DispatchAddAssign(Expression e) { BinaryDispatch("+=", e); }
		public void DispatchAndAssign(Expression e) { BinaryDispatch("&=", e); }
		public void DispatchDivideAssign(Expression e) { BinaryDispatch("/=", e); }
		public void DispatchExclusiveOrAssign(Expression e) { BinaryDispatch("^=", e); }
		public void DispatchLeftShiftAssign(Expression e) { BinaryDispatch("<<=", e); }
		public void DispatchModuloAssign(Expression e) { BinaryDispatch("%=", e); }
		public void DispatchMultiplyAssign(Expression e) { BinaryDispatch("*=", e); }
		public void DispatchOrAssign(Expression e) { BinaryDispatch("|=", e); }
		public void DispatchRightShiftAssign(Expression e) { BinaryDispatch(">>=", e); }
		public void DispatchSubtractAssign(Expression e) { BinaryDispatch("-=", e); }
		public void DispatchAddAssignChecked(Expression e) { BinaryDispatch("+=", e); }
		public void DispatchMultiplyAssignChecked(Expression e) { BinaryDispatch("*=", e); }
		public void DispatchSubtractAssignChecked(Expression e) { BinaryDispatch("-=", e); }
		public void DispatchPreIncrementAssign(Expression e) { UnaryDispatch("++", e); }
		public void DispatchPreDecrementAssign(Expression e) { UnaryDispatch("--", e); }
		public void DispatchPostIncrementAssign(Expression e) { UnaryPostfixDispatch("++ ", e); }
		public void DispatchPostDecrementAssign(Expression e) { UnaryPostfixDispatch("-- ", e); }
		public void DispatchOnesComplement(Expression e) { UnaryDispatch("~", e); }
		#endregion

		#region Unused by C#'s expression support; or unavailable in the language at all.
		public void DispatchTypeEqual(Expression e) { throw new NotImplementedException(); }
		public void DispatchBlock(Expression e) { throw new NotImplementedException(); }
		public void DispatchDebugInfo(Expression e) { throw new NotImplementedException(); }
		public void DispatchDynamic(Expression e) { throw new NotImplementedException(); }
		public void DispatchDefault(Expression e) { throw new NotImplementedException(); }
		public void DispatchExtension(Expression e) { throw new NotImplementedException(); }
		public void DispatchGoto(Expression e) { throw new NotImplementedException(); }
		public void DispatchLabel(Expression e) { throw new NotImplementedException(); }
		public void DispatchRuntimeVariables(Expression e) { throw new NotImplementedException(); }
		public void DispatchLoop(Expression e) { throw new NotImplementedException(); }
		public void DispatchSwitch(Expression e) { throw new NotImplementedException(); }
		public void DispatchThrow(Expression e) { throw new NotImplementedException(); }
		public void DispatchTry(Expression e) { throw new NotImplementedException(); }
		public void DispatchUnbox(Expression e) { throw new NotImplementedException(); }
		public void DispatchPowerAssign(Expression e) { throw new NotImplementedException(); }
		public void DispatchIsTrue(Expression e) { throw new NotImplementedException(); }
		public void DispatchIsFalse(Expression e) { throw new NotImplementedException(); }
		#endregion
	}
}
