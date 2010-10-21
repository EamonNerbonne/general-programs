using System;
using System.Collections.Generic;
using System.Linq;
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
		static object[] empty = new object[] { };
		public static void ParseShorthand(IHasShorthand shorthandObj, Regex shR, string newShorthand) {
			if (!shR.IsMatch(newShorthand)) throw new ArgumentException("Can't parse shorthand - enter manually?");
			var groups = shR.Match(newShorthand).Groups.Cast<Group>().ToArray();
			var includedProperties = new HashSet<string> { "Shorthand" };

			for (int i = 0; i < groups.Length; i++) {
				string groupName = shR.GroupNameFromNumber(i);
				includedProperties.Add(groupName);
				var prop = shorthandObj.GetType().GetProperty(groupName);
				if (prop == null && i != 0) {
					throw new ArgumentException("Invalid regex group #" + i + " called '" + groupName + "'");
				} else if (prop != null && groups[i].Success) {
					var val = prop.PropertyType.Equals(typeof(bool)) ? groups[i].Value != ""
						: TypeDescriptor.GetConverter(prop.PropertyType).ConvertFromString(groups[i].Value);
					prop.SetValue(shorthandObj, val, empty);
				}
			}
			var excludedProperties =
				from property in shorthandObj.GetType().GetProperties()
				where property.CanRead && property.CanWrite
				where !property.GetCustomAttributes(typeof(NotInShorthandAttribute), true).Any()
				where !includedProperties.Contains(property.Name)
				select property.Name;

			if (excludedProperties.Any()) {
				throw new ArgumentException("Invalid Regex doesn't set properties: " + string.Join(", ", excludedProperties.ToArray()));
			}
		}

		public static string VerifyShorthand(IHasShorthand shorthandObj, Regex shR) {
			var errs = new List<string>();
			if (!shR.IsMatch(shorthandObj.Shorthand))
				return "Can't parse shorthand - enter manually?";
			var groups = shR.Match(shorthandObj.Shorthand).Groups.Cast<Group>().ToArray();
			var includedProperties = new HashSet<string> { "Shorthand" };

			for (int i = 0; i < groups.Length; i++) {
				string groupName = shR.GroupNameFromNumber(i);
				includedProperties.Add(groupName);
				var prop = shorthandObj.GetType().GetProperty(groupName);
				if (prop == null && i != 0) {
					errs.Add("Invalid regex group #" + i + " called '" + groupName + "'");
				} else if (prop != null && groups[i].Success) {
					object val = prop.PropertyType.Equals(typeof(bool)) ? groups[i].Value != ""
						: TypeDescriptor.GetConverter(prop.PropertyType).ConvertFromString(groups[i].Value);
					object curVal = prop.GetValue(shorthandObj, empty);
					if (!object.Equals( curVal , val))
						errs.Add(groupName + ": " + val + " != " + curVal);
				} else if (prop != null && !groups[i].Success) {
					errs.Add(groupName + " unused");
				}
			}
			var excludedProperties =
				from property in shorthandObj.GetType().GetProperties()
				where property.CanRead && property.CanWrite
				where !property.GetCustomAttributes(typeof(NotInShorthandAttribute), true).Any()
				where !includedProperties.Contains(property.Name)
				select property.Name;

			if (excludedProperties.Any())
				errs.Add("Invalid Regex doesn't set properties: " + string.Join(", ", excludedProperties.ToArray()));
			return string.Join("\n", errs.ToArray());
		}
	}
}
