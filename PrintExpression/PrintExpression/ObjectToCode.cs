﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace ExpressionToCodeLib {
	static class ObjectToCode {
		static string EscapeChar(char c) {
			if (c < 32 || CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.Control) { //this is a little too rigorous; but easier to read 
				if (c == '\r') return "\\r";
				else if (c == '\t') return "\\t";
				else if (c == '\n') return "\\n";
				else return "\\x" + Convert.ToString((int)c, 16);
			} else if (c == '\\') return "\\\\";
			else return c.ToString();
		}
		static string EscapeStringChars(string str) {
			StringBuilder sb = new StringBuilder(str.Length);
			foreach (char c in str)
				sb.Append(EscapeChar(c));
			return sb.ToString();
		}

		public static string PlainObjectToCode(object val) {
			if (val == null)
				return "null";
			else if (val is string) {
				bool useLiteralSyntax = ((string)val).Any(c => c < 32 || c == '\\') && ((string)val).All(c => c != '\n' && c != '\r' && c != '\t');
				if (useLiteralSyntax)
					return "@\"" + ((string)val).Replace("\"", "\"\"") + "\"";
				else
					return "\"" + EscapeStringChars((string)val) + "\"";
			} else if (val is char)
				return "'" + EscapeStringChars(val.ToString()) + "'";
			else if (val is decimal)
				return Convert.ToString(val, CultureInfo.InvariantCulture) + "m";
			else if (val is float)
				return FloatToCode((float)val);
			else if (val is double)
				return DoubleToCode((double)val);
			else if (val is byte || val is sbyte || val is short || val is ushort || val is int || val is uint || val is long || val is ulong)
				return (Convert.ToString(val, CultureInfo.InvariantCulture));//TODO: get numeric suffixes right - is this OK?
			else if (val is bool && val.Equals(true))
				return "true";
			else if (val is bool && val.Equals(false))
				return "false";
			else if (val is Enum && Enum.IsDefined(val.GetType(), val))
				return val.GetType().Name + "." + val.ToString();
			else if (val.GetType().IsValueType && Activator.CreateInstance(val.GetType()).Equals(val))
				return "default(" + CSharpFriendlyTypeName.Get(val.GetType()) + ")";
			else
				return null;
		}

		private static string DoubleToCode(double p) {
			if (double.IsNaN(p))
				return "double.NaN";
			else if (double.IsNegativeInfinity(p))
				return "double.NegativeInfinity";
			else if (double.IsPositiveInfinity(p))
				return "double.PositiveInfinity";
			else if (Math.Abs(p) > UInt32.MaxValue)
				return p.ToString("0.0########################e0");
			else
				return p.ToString("0.0########################");
		}

		private static string FloatToCode(float p) {
			if (float.IsNaN(p))
				return "float.NaN";
			else if (float.IsNegativeInfinity(p))
				return "float.NegativeInfinity";
			else if (float.IsPositiveInfinity(p))
				return "float.PositiveInfinity";
			else if (Math.Abs(p) >= (1<<24))
				return p.ToString("0.0########e0")+"f";
			else
				return p.ToString("0.0########")+"f";
		}


		static string FormatEnumerable(IEnumerable list) {
			return "{" + string.Join(", ", ExtractFirst10((IEnumerable)list).ToArray()) + "}";
		}

		static IEnumerable<string> ExtractFirst10(IEnumerable list) {
			int count = 0;
			foreach (var item in list) {
				count++;
				if (count > 10) {
					yield return "...";
					yield break;
				} else
					yield return ComplexObjectToPseudoCode(item);
			}
		}

		public static string ComplexObjectToPseudoCode(object val) {
			string retval = PlainObjectToCode(val);
			if (retval != null) return retval;
			else if (val is IEnumerable)
				return FormatEnumerable((IEnumerable)val);
			else if (val is Expression)
				return ExpressionToCode.ToCode((Expression)val);
			else
				return val.ToString();

		}
	}
}
