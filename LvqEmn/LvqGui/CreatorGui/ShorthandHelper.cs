// ReSharper disable PossibleNullReferenceException
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
	public interface IHasShorthand {
		string Shorthand { get; set; }
		string ShorthandErrors { get; }
	}

	public abstract class HasShorthandBase : INotifyPropertyChanged, IHasShorthand {

		public event PropertyChangedEventHandler PropertyChanged;

		void raisePropertyChanged(string prop) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }

		protected void _propertyChanged(string propertyName) {
			if (PropertyChanged != null) {
				if (propertyName == "Shorthand")
					AllPropertiesChanged();
				else {
					raisePropertyChanged(propertyName);
					foreach (var propName in GloballyDependantProps)
						raisePropertyChanged(propName);
				}
			}
		}
		virtual protected IEnumerable<string> GloballyDependantProps {
			get {
				yield return "Shorthand";
				yield return "ShorthandErrors";
			}
		}

		protected void AllPropertiesChanged() {
			foreach (var propname in GetType().GetProperties().Where(prop => prop.CanRead).Select(prop => prop.Name))
				raisePropertyChanged(propname);
		}

		public abstract string Shorthand { get; set; }
		public abstract string ShorthandErrors { get; }
	}


	[AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
	sealed class NotInShorthandAttribute : Attribute { }

	static class ShorthandHelper {
		static readonly object[] empty = new object[] { };

		public static HashSet<string> ParseShorthand(object shorthandObj, Regex shR, string newShorthand) {
			HashSet<string> updated = new HashSet<string>();
			DecomposeShorthand(shorthandObj, shR, newShorthand, (prop, val) => { prop.Value = val; updated.Add(prop.Name); }, err => { throw new ArgumentException(err); });
			return updated;
		}

		public static HashSet<string> TryParseShorthand(object shorthandObj, Regex shR, string newShorthand) {
			var toSet = new Dictionary<Property, object>();
			bool error = false;
			DecomposeShorthand(shorthandObj, shR, newShorthand, toSet.Add, err => error = true);
			if (error) return null;
			foreach (var entry in toSet)
				entry.Key.Value = entry.Value;
			return new HashSet<string>(toSet.Keys.Select(p => p.Name));
		}

		public static T TryParseShorthand<T>(Regex shR, string newShorthand) where T : class,new() {
			T shorthandObj = new T();
			var toSet = new Dictionary<Property, object>();
			bool error = false;
			DecomposeShorthand(shorthandObj, shR, newShorthand, toSet.Add, err => error = true);
			if (error) return null;
			foreach (var entry in toSet)
				entry.Key.Value = entry.Value;
			return shorthandObj;
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
			errs.AppendLine("defaulted: " + string.Join(", ", Property.All(shorthandObj).Select(p => p.Name).Except(usedProperties)));
			return errs.ToString();
		}

		static void DecomposeShorthand(object shorthandObj, Regex shR, string shorthand, Action<Property, object> FoundVal, Action<string> registerError) {
			if (!shR.IsMatch(shorthand)) {
				registerError("Can't parse shorthand - enter manually?");
				return;
			}
			var groups = shR.Match(shorthand).Groups.Cast<Group>().ToArray();
			var includedProperties = new HashSet<string> { "Shorthand" };

			for (int i = 0; i < groups.Length; i++) {
				Group captureGroup = groups[i];
				string groupName = shR.GroupNameFromNumber(i);
				bool isHexEncoded = groupName.EndsWith("_");
				if (isHexEncoded) groupName = groupName.Substring(0, groupName.Length - 1);
				includedProperties.Add(groupName);
				var prop = Property.Create(shorthandObj, groupName);

				if (prop == null && i != 0) {
					registerError("Invalid regex group #" + i + " called '" + groupName + "'");
				} else if (prop != null && captureGroup.Success) {
					string captureVal = captureGroup.Value;
					object val = prop.Type.Equals(typeof(bool)) ? captureVal != ""
									: isHexEncoded && prop.Type == typeof(uint) ? Convert.ToUInt32(captureVal, 16)
										: TypeDescriptor.GetConverter(prop.Type).ConvertFromString(Regex.Replace(captureVal, "ModelType$", ""));
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
			protected readonly object Obj;
			protected Property(object obj) { Obj = obj; }

			public static Property Create(object obj, string name) {
				var propertyInfo = obj.GetType().GetProperty(name);
				if (propertyInfo != null)
					return new PropertyProperty(obj, propertyInfo);
				var fieldInfo = obj.GetType().GetField(name);
				if (fieldInfo != null)
					return new FieldProperty(obj, fieldInfo);
				return null;
			}
			public static IEnumerable<Property> All(object shorthandObj) {
				return (
						from property in shorthandObj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
						where property.CanRead && property.CanWrite
						where !property.GetCustomAttributes(typeof(NotInShorthandAttribute), true).Any()
						select (Property)new PropertyProperty(shorthandObj, property)
					).Concat(
						from field in shorthandObj.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance)
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
			public PropertyProperty(object o, PropertyInfo property)
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
			public FieldProperty(object o, FieldInfo field) : base(o) { this.field = field; }
		}

	}
}
