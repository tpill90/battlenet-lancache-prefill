using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace BattleNetPrefill.Structs
{
    /// <summary>
    /// This class is intended to be used to create "strongly typed enums", as an alternative to regular "int" enums in C#.
    /// The main goal is to avoid "stringly" typed functions that take in a large number of string parameters.  Frequently, these parameters
    /// are usually constrained to only a few values, making them ideal for enums.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class EnumBase<T> where T : EnumBase<T>
    {
        private static readonly List<T> _allEnumValues = new List<T>();

        // Despite analysis tool warnings, we want this static bool to be on this generic type (so that each T has its own bool).
        private static bool _invoked; //NOSONAR - See above message

        private static object _lockObject = new object();
        public static List<T> AllEnumValues
        {
            get
            {
                lock (_lockObject)
                {
                    if (!_invoked)
                    {
                        _invoked = true;
                        // Force initialization by calling one of the derived fields/properties.  Failure to do this will result in this list being empty.
                        typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Static).FirstOrDefault(p => p.PropertyType == typeof(T))?.GetValue(null, null);
                        typeof(T).GetFields(BindingFlags.Public | BindingFlags.Static).FirstOrDefault(p => p.FieldType == typeof(T))?.GetValue(null);
                    }
                    return _allEnumValues;
                }
            }
        }

        public string Name { get; }

        protected EnumBase(string name)
        {
            Name = name;
            AllEnumValues.Add(this as T);
        }

        /// <summary>
        /// Used to parse a value type, into the strongly typed "enum" equivalent.
        ///
        /// Throws an exception if an invalid value is passed in.
        /// </summary>
        /// <param name="toParse"></param>
        /// <returns>A strongly typed "enum" equivalent.</returns>
        public static T Parse(string toParse)
        {
            /*
             * TODO this sometimes throws "Collection was modified; enumeration operation may not execute" exceptions in some cases when running parallel tests
             * This seems to only occur if the logs have not yet been coalesced, re-running the tests afterwards doesn't seem to show this issue.
            */
            foreach (var type in AllEnumValues)
            {
                if (toParse == type.Name)
                {
                    return type;
                }
            }

            throw new FormatException($"{toParse} is not a valid enum value for {typeof(T).Name}!");
        }

        public override string ToString()
        {
            return Name;
        }
    }
}