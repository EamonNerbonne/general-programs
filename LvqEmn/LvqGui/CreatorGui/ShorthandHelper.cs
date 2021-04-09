// ReSharper disable PossibleNullReferenceException
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable UnusedMember.Global

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace LvqGui.CreatorGui
{
    public interface IHasShorthand
    {
        string Shorthand { get; set; }
        string ShorthandErrors { get; }
    }

    public abstract class HasShorthandBase : INotifyPropertyChanged, IHasShorthand
    {
        public event PropertyChangedEventHandler PropertyChanged;

        void raisePropertyChanged(string prop) => PropertyChanged(this, new(prop));

        protected void _propertyChanged(string propertyName)
        {
            if (PropertyChanged != null) {
                if (propertyName == "Shorthand") {
                    AllPropertiesChanged();
                } else {
                    raisePropertyChanged(propertyName);
                    foreach (var propName in GloballyDependantProps) {
                        raisePropertyChanged(propName);
                    }
                }
            }
        }

        protected virtual IEnumerable<string> GloballyDependantProps
        {
            get {
                yield return "Shorthand";
                yield return "ShorthandErrors";
            }
        }

        protected void AllPropertiesChanged()
        {
            foreach (var propname in GetType().GetProperties().Where(prop => prop.CanRead).Select(prop => prop.Name)) {
                raisePropertyChanged(propname);
            }
        }

        public abstract string Shorthand { get; set; }
        public abstract string ShorthandErrors { get; }
    }

    public struct Optional<T>
    {
        readonly object val;

        public bool HasValue => val != null;

        public T Value
        {
            get {
                if (!HasValue) {
                    throw new InvalidOperationException("Can't access null!");
                }

                return (T)val;
            }
        }

        public Optional(T pval)
        {
            if (pval == null) {
                throw new ArgumentNullException();
            }

            val = pval;
        }

        internal T AsNullable() => (T)val;

        internal TS? AsNullableStruct<TS>()
            where TS : struct => (TS?)val;
    }

    static class ShorthandHelper
    {
        public static void ParseShorthand(object shorthandObj, object defaults, Regex shR, string newShorthand) => Create(shorthandObj).ParseShorthand(defaults, shR, newShorthand);

        //        public static bool TryParseShorthand(object shorthandObj, object defaults, Regex shR, string newShorthand) {
        //public static string VerifyShorthand(T shorthandObj, Regex shR) { IHasShorthand
        //        public static T TryParseShorthand<T>(T defaults, Regex shR, string newShorthand) where T : class,new() {


        internal static string VerifyShorthand(object shorthandObj, Regex shR) => Create(shorthandObj).VerifyShorthand(shR);

        static IShorthandHelper Create(object shorthandObj) => (IShorthandHelper)Activator.CreateInstance(typeof(ShorthandHelper<>).MakeGenericType(shorthandObj.GetType()), shorthandObj);

        internal static Optional<T> TryParseShorthand<T>(T defaults, Regex shR, string shorthand)
            where T : new()
        {
            var helper = new ShorthandHelper<T>(new());
            if (!helper.TryParseShorthandWithErrs(defaults, shR, shorthand).Any()) {
                return new(helper.shorthandObj);
            }

            return default(Optional<T>);
        }
    }

    public interface IShorthandHelper
    {
        void ParseShorthand(object defaults, Regex shR, string newShorthand);


        string VerifyShorthand(Regex shR);

        bool TryParseShorthand(object defaults, Regex shR, string shorthand);
        object Value { get; }
    }

    [AttributeUsage(AttributeTargets.Property)]
    sealed class NotInShorthandAttribute : Attribute { }

    sealed class ShorthandHelper<T> : IShorthandHelper
    {
        public T shorthandObj;
        public object Value => shorthandObj;

        public ShorthandHelper(T obj) => shorthandObj = obj;


        //        public static void ParseShorthand(object shorthandObj, object defaults, Regex shR, string newShorthand) {
        //        public static bool TryParseShorthand(object shorthandObj, object defaults, Regex shR, string newShorthand) {
        //public static string VerifyShorthand(T shorthandObj, Regex shR) { IHasShorthand
        //        public static T TryParseShorthand<T>(T defaults, Regex shR, string newShorthand) where T : class,new() {
        public void ParseShorthand(object defaults, Regex shR, string newShorthand)
        {
            if (typeof(T).IsValueType) {
                throw new InvalidOperationException("This won't work - no reference to underlying data!");
            }

            ParseShorthand((T)defaults, shR, newShorthand);
        }

        public void ParseShorthand(T defaults, Regex shR, string newShorthand)
        {
            var errs = TryParseShorthandWithErrs(defaults, shR, newShorthand);

            if (errs.Any()) {
                throw new InvalidOperationException(string.Join("\n", errs));
            }
        }

        public bool TryParseShorthand(object defaults, Regex shR, string shorthand) => TryParseShorthandWithErrs((T)defaults, shR, shorthand).Any();

        public List<string> TryParseShorthandWithErrs(T defaults, Regex shR, string newShorthand)
        {
            var toSet = new object[PropertyStore.properties.Length];
            var errors = new List<string>();
            DecomposeShorthand(shR, newShorthand, (p, v) => toSet[p.Index] = v, errors.Add);
            if (!errors.Any()) {
                for (var i = 0; i < toSet.Length; i++) {
                    var prop = PropertyStore.properties[i];
                    shorthandObj = prop.Set(shorthandObj, toSet[i] ?? prop.Get(defaults));
                }
            }

            return errors;
        }

        public string VerifyShorthand(Regex shR)
        {
            var errs = new StringBuilder();
            var usedProperties = new bool[PropertyStore.properties.Length];
            DecomposeShorthand(shR, ((IHasShorthand)shorthandObj).Shorthand,
                (prop, val) => {
                    usedProperties[prop.Index] = true;
                    var currVal = prop.Get(shorthandObj);
                    if (!Equals(currVal, val)) {
                        errs.AppendLine(prop.Name + ": " + val + " != " + currVal);
                    }
                }, err => errs.AppendLine(err)
            );
            errs.AppendLine("defaulted: " + string.Join(", ", PropertyStore.properties.Where(prop => !usedProperties[prop.Index]).Select(prop => prop.Name)));
            return errs.ToString();
        }

        static void DecomposeShorthand(Regex shR, string shorthand, Action<PropertyDef, object> FoundVal, Action<string> registerError)
        {
            if (!shR.IsMatch(shorthand)) {
                registerError("Can't parse shorthand - enter manually?");
                return;
            }

            var groups = shR.Match(shorthand).Groups.Cast<Group>().ToArray();
            var includedProperties = new bool[PropertyStore.properties.Length];

            for (var i = 0; i < groups.Length; i++) {
                var captureGroup = groups[i];
                var groupName = shR.GroupNameFromNumber(i);
                var isHexEncodedOrNegated = groupName.EndsWith("_", StringComparison.Ordinal);
                if (isHexEncodedOrNegated) {
                    groupName = groupName[..^1];
                }

                var propIdx = PropertyStore.GetIndex(groupName);

                if (propIdx == -1 && i != 0) {
                    registerError("Invalid regex group #" + i + " called '" + groupName + "'");
                } else if (propIdx != -1) {
                    includedProperties[propIdx] = true;
                    if (captureGroup.Success) {
                        var prop = PropertyStore.properties[propIdx];
                        var captureVal = captureGroup.Value;
                        var val = prop.Type == typeof(bool)
                            ? captureVal != "" ^ isHexEncodedOrNegated
                            : isHexEncodedOrNegated && prop.Type == typeof(uint)
                                ? Convert.ToUInt32(captureVal, 16)
                                : TypeDescriptor.GetConverter(prop.Type).ConvertFromString(Regex.Replace(captureVal, "ModelType$",
                                        ""
                                    )
                                );
                        FoundVal(prop, val);
                    }
                }
            }

            var excludedProperties = PropertyStore.properties.Where(prop => !includedProperties[prop.Index]).Select(prop => prop.Name).ToArray();
            if (excludedProperties.Any()) {
                registerError("Invalid Regex doesn't set properties: " + string.Join(", ", excludedProperties.ToArray()));
            }
        }


        static class PropertyStore
        {
            public static readonly Dictionary<string, int> propIndex;
            public static readonly PropertyDef[] properties;

            public static int GetIndex(string name)
            {
                if (propIndex.TryGetValue(name, out var idx)) {
                    return idx;
                }

                return -1;
            }

            static PropertyStore()
            {
                properties = (
                        from property in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                        where property.CanRead && property.CanWrite
                        where !property.GetCustomAttributes(typeof(NotInShorthandAttribute), true).Any()
                        select new { property.Name, Type = property.PropertyType }
                    ).Concat(
                        from field in typeof(T).GetFields(BindingFlags.Public | BindingFlags.Instance)
                        where !field.GetCustomAttributes(typeof(NotInShorthandAttribute), true).Any()
                        select new { field.Name, Type = field.FieldType }
                    ).Where(mi => mi.Name != "Shorthand")
                    .Select((mi, i) => new PropertyDef(mi.Name, mi.Type, i))
                    .ToArray(); //select new PropertyDef(property.Name, property.PropertyType)
                propIndex = properties.Select((p, i) => new { p, i }).ToDictionary(pi => pi.p.Name, pi => pi.i);
            }
        }

        public sealed class PropertyDef
        {
            public readonly string Name;
            public readonly Type Type;
            public readonly Func<T, object> Get;
            public readonly Func<T, object, T> Set;
            public readonly int Index;

            public PropertyDef(string propName, Type propertyType, int index)
            {
                Name = propName;
                Type = propertyType;
                Index = index;
                var shorthandObjParam = Expression.Parameter(typeof(T), "shorthandObjParam");
                var propExpr = Expression.PropertyOrField(shorthandObjParam, propName);
                var propAsObjectExpr = Expression.Convert(propExpr, typeof(object));
                Get = Expression.Lambda<Func<T, object>>(propAsObjectExpr, shorthandObjParam).Compile();

                var newValueParam = Expression.Parameter(typeof(object), "newValueParam");
                var assignExpr = Expression.Assign(propExpr, Expression.Convert(newValueParam, propertyType));
                Set = Expression.Lambda<Func<T, object, T>>(Expression.Block(typeof(T), assignExpr, shorthandObjParam), shorthandObjParam, newValueParam).Compile();
            }
        }
    }
}
