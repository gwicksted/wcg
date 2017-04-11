using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

namespace wcg
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    internal class ArgAttribute : Attribute
    {
        public ArgAttribute([CallerMemberName] string name = null)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            Name = name;
        }

        public bool IsPrimary { get; set; }

        public string Name { get; }

        public string ShortName { get; set; }

        public string Description { get; set; }
    }

    internal interface IFieldOrProperty
    {
        string Name { get; }

        Type FieldOrPropertyType { get; }

        T Attribute<T>() where T : Attribute;

        object Value { get; set; }
    }

    internal class Field : IFieldOrProperty
    {
        private readonly object _instance;
        private readonly FieldInfo _field;

        public Field(FieldInfo fieldInfo, object instance)
        {
            _field = fieldInfo;
            _instance = instance;
            Name = fieldInfo.Name;
            FieldOrPropertyType = fieldInfo.FieldType;
        }

        public string Name { get; }
        public Type FieldOrPropertyType { get; }

        public T Attribute<T>() where T : Attribute
        {
            return _field.GetCustomAttribute<T>();
        }

        public object Value
        {
            get => _field.GetValue(_instance);
            set => _field.SetValue(_instance, value);
        }
    }

    internal class Property : IFieldOrProperty
    {
        private readonly object _instance;
        private readonly PropertyInfo _property;

        public Property(PropertyInfo propertyInfo, object instance)
        {
            _property = propertyInfo;
            _instance = instance;
            Name = propertyInfo.Name;
            FieldOrPropertyType = propertyInfo.PropertyType;
        }

        public string Name { get; }
        public Type FieldOrPropertyType { get; }

        public T Attribute<T>() where T : Attribute
        {
            return _property.GetCustomAttribute<T>();
        }

        public object Value
        {
            get => _property.GetValue(_instance);
            set => _property.SetValue(_instance, value);
        }
    }

    internal static class DictionaryExtensions
    {
        public static IDictionary<TKey, TValue> ToDictionaryRaceCondition<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> source)
        {
            IDictionary<TKey, TValue> dictionary = new Dictionary<TKey, TValue>();

            foreach (var kv in source)
            {
                dictionary[kv.Key] = kv.Value;
            }

            return dictionary;
        }
    }

    internal class BindableArg<T> where T : class
    {
        public BindableArg(IFieldOrProperty binding)
        {
            Binding = binding;
        }

        public IFieldOrProperty Binding { get; }

        public ArgAttribute Mapping => Binding.Attribute<ArgAttribute>();

        public string Name => Mapping?.Name ?? Binding.Name;

        public string ShortName => Mapping?.ShortName;

        public Type Type => Binding.FieldOrPropertyType;

        public bool IsPrimary => Mapping?.IsPrimary ?? false;

        public bool IsBoolean => Binding.FieldOrPropertyType == typeof(bool);

        public bool IsString => Binding.FieldOrPropertyType == typeof(string);

        public bool IsNumber => MicroArgs<T>.IsIntegerType(Binding.FieldOrPropertyType);

        public object Value
        {
            get => Binding.Value;
            set => Binding.Value = value;
        }

        public string Description => Mapping?.Description ?? string.Empty;
    }

    internal class MicroArgs<T> where T : class
    {
        private const string ShortParamPrefix = "/";
        private const string ShortParamEquals = ":";
        private const string LongParamPrefix = "/";
        private const string LongParamEquals = ":";

        private const string ParameterSeparator = ", ";

        private const string ShortOptPrefix = "/";
        private const string LongOptPrefix = "/";
        
        private static readonly Regex _argParser = new Regex("^([-][-]?|[-]|[/])?([?]|[-_a-zA-Z0-9]+)(?:[=:](.+))?$");
        
        private static readonly IDictionary<Type, Tuple<int, int, Func<string, object>>> MinMaxConverter = new Dictionary<Type, Tuple<int, int, Func<string, object>>>
            {
                { typeof(sbyte), new Tuple<int, int, Func<string, object>>(sbyte.MinValue.ToString().Length, sbyte.MaxValue.ToString().Length, s => Convert.ToSByte(s)) },
                { typeof(byte), new Tuple<int, int, Func<string, object>>(byte.MinValue.ToString().Length, byte.MaxValue.ToString().Length, s => Convert.ToByte(s)) },
                { typeof(short), new Tuple<int, int, Func<string, object>>(short.MinValue.ToString().Length, short.MaxValue.ToString().Length, s => Convert.ToInt16(s)) },
                { typeof(ushort), new Tuple<int, int, Func<string, object>>(ushort.MinValue.ToString().Length, ushort.MaxValue.ToString().Length, s => Convert.ToUInt16(s)) },
                { typeof(int), new Tuple<int, int, Func<string, object>>(int.MinValue.ToString().Length, int.MaxValue.ToString().Length, s => Convert.ToInt32(s)) },
                { typeof(uint), new Tuple<int, int, Func<string, object>>(uint.MinValue.ToString().Length, uint.MaxValue.ToString().Length, s => Convert.ToUInt32(s)) },
                { typeof(long), new Tuple<int, int, Func<string, object>>(long.MinValue.ToString().Length, long.MaxValue.ToString().Length, s => Convert.ToInt64(s)) },
                { typeof(ulong), new Tuple<int, int, Func<string, object>>(ulong.MinValue.ToString().Length, ulong.MaxValue.ToString().Length, s => Convert.ToUInt64(s)) }
            };

        public MicroArgs(string[] args)
        {
            Args = args;

            Ordered = Parameters(args).ToList();

            Dictionary = Ordered.ToDictionaryRaceCondition();
            
            Parsed = Activator.CreateInstance<T>();

            Potentials = PotentialArgs();

            ApplyValues();
        }

        public T Parsed { get; }

        public string[] Args { get; }

        public IDictionary<string, string> Dictionary { get; }

        public IList<KeyValuePair<string, string>> Ordered { get; }

        public IEnumerable<BindableArg<T>> Potentials { get; }

        private IEnumerable<string> Matches(BindableArg<T> arg)
        {
            return Dictionary.Keys.Where(k => k.Equals(arg.Name, StringComparison.OrdinalIgnoreCase)
                                               || !string.IsNullOrEmpty(arg.ShortName) && k.Equals(arg.ShortName));

        }

        private static IEnumerable<KeyValuePair<string, string>> Parameters(string[] args)
        {
            int unnamed = 0;

            foreach (var arg in args)
            {
                var match = _argParser.Match(arg);

                if (match.Groups[2].Value == "?")
                {
                    yield return new KeyValuePair<string, string>("?", string.Empty);
                }
                else if (!match.Success || (!match.Groups[1].Success && !match.Groups[3].Success))
                {
                    yield return new KeyValuePair<string, string>(unnamed++.ToString(), arg);
                }
                else
                {
                    yield return new KeyValuePair<string, string>(match.Groups[2].Value, match.Groups[3].Success ? match.Groups[3].Value : string.Empty);
                }
            }
        }

        private IEnumerable<BindableArg<T>> PotentialArgs()
        {

            var bindables = ((IEnumerable<IFieldOrProperty>)
                                typeof(T).GetFields(BindingFlags.SetField | BindingFlags.Public | BindingFlags.Instance).Select(fi => new Field(fi, Parsed))
                            ).Concat(
                typeof(T).GetProperties(BindingFlags.SetProperty | BindingFlags.Public | BindingFlags.Instance).Select(pi => new Property(pi, Parsed))
            );

            foreach (var bindable in bindables)
            {
                yield return new BindableArg<T>(bindable);
            }
        }

        private void ApplyValues()
        {
            foreach (var arg in Potentials)
            {
                if (Matches(arg).Any())
                {
                    arg.Value = Read(arg.Name, arg.ShortName, arg.Type);
                }
            }
        }

        private string ReadString(string key, string shortName)
        {
            return Dictionary.TryGetValue(key, out string value) ? value : shortName != null && Dictionary.TryGetValue(shortName, out string shortValue) ? shortValue : null;
        }

        private bool IsIntegerValue(string value, int min = 0, int max = 255)
        {
            value = value?.Replace(" ", string.Empty);
            
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            char first = value[0];

            return (char.IsDigit(first) || first == '-') && value.Skip(1).All(char.IsDigit) && value.Length <= (first == '-' ? min : max);
        }

        public static bool IsIntegerType(Type type)
        {
            return MinMaxConverter.ContainsKey(type);
        }

        private object AsIntegerValue(Type type, string value)
        {
            if (MinMaxConverter.TryGetValue(type, out var minMaxConverter))
            {
                var min = minMaxConverter.Item1;
                var max = minMaxConverter.Item2;
                var converter = minMaxConverter.Item3;

                if (IsIntegerValue(value, min, max))
                {
                    try
                    {
                        return converter(value);
                    }
                    catch
                    {
                    }
                }
            }

            return Activator.CreateInstance(type);
        }

        private TValue AsIntegerValue<TValue>(string value) where TValue : struct
        {
            return (TValue)AsIntegerValue(typeof(TValue), value);
        }

        private object Read(string key, string shortName, Type type)
        {
            var value = ReadString(key, shortName);

            if (type.IsEnum)
            {
                var names = Enum.GetNames(type);
                var match = names.FirstOrDefault(n => n.Equals(value, StringComparison.OrdinalIgnoreCase));

                if (match != null)
                {
                    return Enum.Parse(type, match);
                }

                var underlying = Enum.GetUnderlyingType(type);

                return Enum.ToObject(type, AsIntegerValue(underlying, value));
            }

            if (type == typeof(DateTime))
            {
                if (DateTime.TryParse(value, out var parsed))
                {
                    return parsed;
                }

                return default(DateTime);
            }

            if (type.IsValueType)
            {
                if (IsIntegerType(type))
                {
                    return AsIntegerValue(type, value);
                }

                if (type == typeof(bool))
                {
                    return true;
                }

                return Activator.CreateInstance(type);
            }

            if (type == typeof(string))
            {
                return value;
            }

            return null;

            /*else if (type.IsArray)
            {

            }
            else if (typeof(IEnumerable<>).IsAssignableFrom(type))
            {

            }
            else if (typeof(IEnumerable).IsAssignableFrom(type))
            {

            }
            else if (type.IsInterface || type.IsAbstract)
            {

            }
            else if (type.IsClass)
            {

            }
            */
        }

        public void DisplayVersion()
        {
            Output.DisplayInfo($"{ApplicationUtilities.Application}  {ApplicationUtilities.Description}  {ApplicationUtilities.Version}");
            Output.DisplaySubInfo(ApplicationUtilities.Copyright);
        }

        public void DisplayHelp()
        {
            Output.WriteSection("Usage");
            
            Output.DisplayImportant(ApplicationUtilities.ExeName, true);

            var args = Potentials.Where(p => p.IsPrimary).ToArray();
            var options = Potentials.Where(p => !p.IsPrimary && p.IsBoolean).ToArray();
            var parameters = Potentials.Where(p => !p.IsPrimary && !p.IsBoolean).ToArray();

            string description = string.Empty;

            if (options.Any())
            {
                description += " [options]";
            }

            if (parameters.Any())
            {
                description += " [param=value ...]";
            }

            if (args.Any())
            {
                description += $" <{string.Join("|", args.Select(a => a.Name))}>";
            }

            Output.DisplayInfo(description);
            Console.WriteLine();

            if (args.Any())
            {
                DisplayArgs(args);
            }

            if (parameters.Any())
            {
                DisplayParameters(parameters);
            }

            if (options.Any())
            {
                DisplayOptions(options);
            }
        }

        private void DisplayArgs(IEnumerable<BindableArg<T>> args)
        {
            Output.WriteSection("Arguments");
            foreach (var arg in args)
            {
                Output.WriteIndented(null, arg.Name, null, null, arg.Description);
            }
            Output.EndSection();
        }

        private void DisplayParameters(IEnumerable<BindableArg<T>> args)
        {
            Output.WriteSection("Parameters");
            foreach (var arg in args)
            {
                string valuePart = arg.IsString ? "\"value\"" : "123";
                Output.WriteIndented(LongParamPrefix, arg.Name, LongParamPrefix, ParameterSeparator, ShortParamPrefix, arg.ShortName, ShortParamEquals, valuePart, arg.Description);
            }
            Output.EndSection();
        }


        private void DisplayOptions(IEnumerable<BindableArg<T>> args)
        {
            Output.WriteSection("Options");
            foreach (var arg in args)
            {
                Output.WriteIndented(LongOptPrefix, arg.Name, null, ParameterSeparator, ShortOptPrefix, arg.ShortName, null, null, arg.Description);
            }
            Output.EndSection();
        }
    }
}
