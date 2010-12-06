﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PrintExpression {
	static class CSharpFriendlyTypeName {
		public static string Get(Type type) { return GenericTypeName(type) ?? ArrayTypeName(type) ?? AliasName(type) ?? type.Name; }

		static string AliasName(Type type) {
			if (type == typeof(byte)) return "byte";
			else if (type == typeof(sbyte)) return "sbyte";
			else if (type == typeof(short)) return "short";
			else if (type == typeof(ushort)) return "ushort";
			else if (type == typeof(int)) return "int";
			else if (type == typeof(uint)) return "uint";
			else if (type == typeof(long)) return "long";
			else if (type == typeof(ulong)) return "ulong";
			else if (type == typeof(float)) return "float";
			else if (type == typeof(double)) return "double";
			else if (type == typeof(decimal)) return "decimal";
			else if (type == typeof(object)) return "object";
			else if (type == typeof(string)) return "string";
			else if (type == typeof(bool)) return "bool";
			else if (type == typeof(char)) return "char";
			else return null;
		}
		static string GenericTypeName(Type type) {
			if (!type.IsGenericType) return null;
			string basename = type.GetGenericTypeDefinition().Name;
			string argsString = string.Join(", ", type.GetGenericArguments().Select(Get).ToArray());
			return basename.Substring(0, basename.IndexOf('`')) + "<" + argsString + ">";
		}
		static string ArrayTypeName(Type type) {
			if (!type.IsArray) return null;
			string basename = Get(type.GetElementType());
			string rankCommas = new string(',', type.GetArrayRank() - 1);
			return basename + "[" + rankCommas + "]";
		}
	}
}