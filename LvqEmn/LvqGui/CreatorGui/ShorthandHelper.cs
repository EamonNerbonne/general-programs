﻿// ReSharper disable PossibleNullReferenceException
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable UnusedMember.Global

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.ComponentModel;
namespace LvqGui {
	interface IHasShorthand {
		string Shorthand { get; set; }
		string ShorthandErrors { get; }
	}

	[AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
	sealed class NotInShorthandAttribute : Attribute { }

	static class ShorthandHelper {
		static readonly object[] empty = new object[] { };



		public static void ParseShorthand(IHasShorthand shorthandObj, Regex shR, string newShorthand) {
			DecomposeShorthand(shorthandObj, shR, newShorthand, (prop, val) => { prop.Value = val; }, err => { throw new ArgumentException(err); });
		}

		public static string VerifyShorthand(IHasShorthand shorthandObj, Regex shR) {
			var errs = new StringBuilder();
			HashSet<string> usedProperties = new HashSet<string> { "Shorthand" };
			DecomposeShorthand(shorthandObj, shR, shorthandObj.Shorthand,
				(prop, val) => {
					usedProperties.Add(prop.Name);
					if (!Equals(prop.Value, val))
						errs.AppendLine(prop.Name + ": " + val + " != " + prop.Value);
				}, err => errs.AppendLine(err));
			foreach (var unusedName in Property.All(shorthandObj).Select(p => p.Name).Except(usedProperties))
				errs.AppendLine("unused: " + unusedName);
			return errs.ToString();
		}

		static void DecomposeShorthand(IHasShorthand shorthandObj, Regex shR, string shorthand, Action<Property, object> FoundVal, Action<string> registerError) {
			if (!shR.IsMatch(shorthand)) {
				registerError("Can't parse shorthand - enter manually?");
				return;
			}
			var groups = shR.Match(shorthand).Groups.Cast<Group>().ToArray();
			var includedProperties = new HashSet<string> { "Shorthand" };

			for (int i = 0; i < groups.Length; i++) {
				string groupName = shR.GroupNameFromNumber(i);
				bool isHexEncoded = groupName.EndsWith("_");
				if (isHexEncoded) groupName = groupName.Substring(0, groupName.Length - 1);
				includedProperties.Add(groupName);
				var prop = Property.Create(shorthandObj, groupName);

				if (prop == null && i != 0) {
					registerError("Invalid regex group #" + i + " called '" + groupName + "'");
				} else if (prop != null && groups[i].Success) {
					object val = prop.Type.Equals(typeof(bool)) ? groups[i].Value != ""
									: isHexEncoded && prop.Type == typeof(uint) ? Convert.ToUInt32(groups[i].Value, 16)
										: TypeDescriptor.GetConverter(prop.Type).ConvertFromString(groups[i].Value);
					FoundVal(prop, val);
				}
			}
			IEnumerable<string> propertyNames = Property.All(shorthandObj).Select(p => p.Name).ToArray();
			string[] excludedProperties = propertyNames.Where(name => !includedProperties.Contains(name)).ToArray();
			if (excludedProperties.Any())
				registerError("Invalid Regex doesn't set properties: " + string.Join(", ", excludedProperties.ToArray()));
		}


		abstract class Property {
			// ReSharper disable MemberCanBeProtected.Global
			public abstract string Name { get; } //public for debuggins ease.
			// ReSharper restore MemberCanBeProtected.Global
			public abstract object Value { get; set; }
			public abstract Type Type { get; }
			protected readonly IHasShorthand Obj;
			protected Property(IHasShorthand obj) { Obj = obj; }

			public static Property Create(IHasShorthand obj, string name) {
				var propertyInfo = obj.GetType().GetProperty(name);
				if (propertyInfo != null)
					return new PropertyProperty(obj, propertyInfo);
				var fieldInfo = obj.GetType().GetField(name);
				if (fieldInfo != null)
					return new FieldProperty(obj, fieldInfo);
				return null;
			}
			public static IEnumerable<Property> All(IHasShorthand shorthandObj) {
				return (
						from property in shorthandObj.GetType().GetProperties()
						where property.CanRead && property.CanWrite
						where !property.GetCustomAttributes(typeof(NotInShorthandAttribute), true).Any()
						select (Property)new PropertyProperty(shorthandObj, property)
					).Concat(
						from field in shorthandObj.GetType().GetFields()
						where !field.GetCustomAttributes(typeof(NotInShorthandAttribute), true).Any()
						select new FieldProperty(shorthandObj, field)
					);
			}
		}

		class PropertyProperty : Property {
			public override string Name { get { return property.Name; } }
			public override Type Type { get { return property.PropertyType; } }
			public override object Value { get { return property.GetValue(Obj, null); } set { property.SetValue(Obj, value, null); } }
			readonly PropertyInfo property;
			public PropertyProperty(IHasShorthand o, PropertyInfo property)
				: base(o) {
				if (!(property.CanRead && property.CanWrite)) //non R/W properties don't make sense for shorthand.
					throw new Exception("Non read/write property " + property.Name + " on type " + o.GetType().FullName);
				this.property = property;
			}
		}

		class FieldProperty : Property {
			readonly FieldInfo field;
			public override string Name { get { return field.Name; } }
			public override Type Type { get { return field.FieldType; } }
			public override object Value { get { return field.GetValue(Obj); } set { field.SetValue(Obj, value); } }
			public FieldProperty(IHasShorthand o, FieldInfo field) : base(o) { this.field = field; }
		}

	}
}
