using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Simplex
{
    public static class SimplexConfigLoader
    {
        public static void LoadConfig<T>(this T obj, Func<string, string> loadFunc)
        {
            var props = obj.GetType().GetProperties();
            foreach (var prop in props)
            {
                if (typeof(ValidatedConfigValue).IsAssignableFrom(prop.PropertyType))
                {
                    Console.WriteLine($"loading config value for {prop.Name}");
                    Console.WriteLine($"{prop.PropertyType}");
                    string name = prop.Name;
                    foreach (var attr in prop.GetCustomAttributes(false))
                        if (attr.GetType() == typeof(SimplexConfigValueNameAttribute))
                            name = ((SimplexConfigValueNameAttribute)attr).Name;
                    var cfgValue = (ValidatedConfigValue)prop.GetValue(obj);
                    cfgValue.Load(name, loadFunc);
                    if (!cfgValue.Validate() && name != prop.Name)
                    {
                        cfgValue.Load(prop.Name, loadFunc);
                    }
                }
            }
        }
    }

    public static class SimplexValidator
    {
        public static bool ValidateObject<T>(this T obj)
        {
            var props = obj.GetType().GetProperties();
            foreach (var prop in props)
            {
                if (prop.PropertyType.IsAssignableFrom(typeof(ValidatedConfigValue)))
                {
                    if (!((ValidatedConfigValue)prop.GetValue(obj)).Validate())
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }

    public class SimplexConfigValueNameAttribute : Attribute
    {
        public string Name { get; }

        public SimplexConfigValueNameAttribute(string name)
        {
            Name = name;
        }
    }

    public class ValidatedConfigValueConverter : JsonConverter<ValidatedConfigValue>
    {
        public override ValidatedConfigValue Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            string str = reader.GetString();
            ValidatedConfigValue v = (ValidatedConfigValue)Activator.CreateInstance(typeToConvert);
            v.Deserialize(str, options);
            return v;
        }

        public override void Write(Utf8JsonWriter writer, ValidatedConfigValue value, JsonSerializerOptions options)
        {
            string str = value.Serialize(options);
            writer.WriteString("val", str);
        }
    }

    [JsonConverter(typeof(ValidatedConfigValueConverter))]
    public abstract class ValidatedConfigValue
    {
        public abstract void Load(string value, Func<string, string> loadFunc);
        public abstract bool Validate();
        public abstract string Serialize(JsonSerializerOptions options);
        public abstract void Deserialize(string json, JsonSerializerOptions options);
    }

    public class ValidatedConfigValue<T> : ValidatedConfigValue
    {
        protected T val;
        protected Func<T, bool> validationFunc;
        protected Func<string, T> loadingFunc;

        protected ValidatedConfigValue() { }

        public ValidatedConfigValue(Func<T, bool> valFunc, Func<string, T> loadFunc)
        {
            validationFunc = valFunc;
            loadingFunc = loadFunc;
        }

        public ValidatedConfigValue(Func<T, bool> valFunc, Func<string, T> loadFunc, T defaultValue)
            : this(valFunc, loadFunc)
        {
            val = defaultValue;
        }

        public override void Load(string value, Func<string, string> loadFunc)
        {
            val = loadingFunc(loadFunc(value));
        }

        public override bool Validate()
        {
            if (val == null)
                return false;

            return validationFunc(val);
        }

        public override string ToString()
        {
            return val?.ToString();
        }

        public override string Serialize(JsonSerializerOptions options)
        {
            return JsonSerializer.Serialize(val, options);
        }

        public override void Deserialize(string json, JsonSerializerOptions options)
        {
            val = JsonSerializer.Deserialize<T>(json, options);
        }

        public T ToValue() => val;
        public void SetValue(T value) => val = value;

        public static implicit operator T(ValidatedConfigValue<T> value) => value.ToValue();
    }

    public class ValidatedConfigValueComplex<T> : ValidatedConfigValue<T>
    {
        private Func<string, Func<string, string>, T> complexLoadFunc;

        ValidatedConfigValueComplex() { }

        public ValidatedConfigValueComplex(Func<T, bool> valFunc, Func<string, Func<string, string>, T> complexFunc)
            : base(valFunc, null)
        {
            complexLoadFunc = complexFunc;
        }

        public ValidatedConfigValueComplex(Func<T, bool> valFunc, Func<string, Func<string, string>, T> complexFunc, T defaultValue)
            : this(valFunc, complexFunc)
        {
            val = defaultValue;
        }

        public override void Load(string value, Func<string, string> loadFunc)
        {
            val = complexLoadFunc(value, loadFunc);
        }
    }
}
