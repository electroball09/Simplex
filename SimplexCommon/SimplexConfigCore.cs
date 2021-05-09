using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text;

namespace Simplex
{
    public static class SimplexConfigLoader
    {
        public static void LoadConfig<T>(this T obj, Func<string, string> loadFunc)
        {
            //Console.WriteLine($"loading config for obj type {obj.GetType()}");
            var props = obj.GetType().GetProperties();
            foreach (var prop in props)
            {
                //Console.WriteLine($"checking prop {prop.Name} type {prop.PropertyType}");
                var cfgClassAttr = (ConfigClassAttribute)prop.GetCustomAttribute(typeof(ConfigClassAttribute));
                var cfgValAttr = (ConfigValueAttribute)prop.GetCustomAttribute(typeof(ConfigValueAttribute));
                object newValue = null;
                if (cfgClassAttr != null)
                {
                    //Console.WriteLine($"prop type {prop.PropertyType} of type {prop.DeclaringType} has config class attribute\n");
                    newValue = Activator.CreateInstance(prop.PropertyType);
                    //Console.WriteLine($"{newValue.GetType()}");
                    newValue.LoadConfig(loadFunc);
                }
                else if (cfgValAttr != null)
                {
                    //Console.WriteLine($"prop type {prop.PropertyType} of type {prop.DeclaringType} has config value attribute");
                    newValue = cfgValAttr.LoadValue(prop.Name, loadFunc);
                }
                else
                {
                    return;
                }

                //Console.WriteLine($"setting prop to value {newValue}");
                prop.SetValue(obj, newValue);
            }
        }
    }

    public class ConfigClassAttribute : Attribute
    {

    }

    public abstract class ConfigValueAttribute : Attribute
    {
        public abstract object LoadValue(string name, Func<string, string> fetchingFunc);
    }

    public class ConfigValueStringAttribute : ConfigValueAttribute
    {
        public override object LoadValue(string name, Func<string, string> fetchingFunc) => fetchingFunc(name);
    }

    public class ConfigValueIntAttribute : ConfigValueAttribute
    {
        public override object LoadValue(string name, Func<string, string> fetchingFunc)
        {
            int.TryParse(fetchingFunc(name), out int num);
            return num;
        }
    }

    public class ConfigValueBoolAttribute : ConfigValueAttribute
    {
        public override object LoadValue(string name, Func<string, string> fetchingFunc) => fetchingFunc(name)?.ToLower() == "true";
    }

    public class ConfigValueJson : ConfigValueAttribute
    {
        readonly Type baseType;

        public ConfigValueJson(Type type)
        {
            baseType = type;
        }

        public override object LoadValue(string name, Func<string, string> fetchingFunc)
        {
            string str = fetchingFunc(name);
            if (string.IsNullOrEmpty(str))
                return null;
            return JsonSerializer.Deserialize(fetchingFunc(name), baseType);
        }
    }

    public class ConfigClassValidatorAttribute : ValidationAttribute
    {
        public override bool RequiresValidationContext => true;
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            ValidationContext ct = new ValidationContext(value);
            List<ValidationResult> results = new List<ValidationResult>();
            bool success = Validator.TryValidateObject(value, ct, results, true);
            if (success)
                return ValidationResult.Success;
            var mNames = new List<string>();
            StringBuilder sb = new StringBuilder();
            foreach (var r in results)
            {
                mNames.AddRange(r.MemberNames);
                sb.AppendLine(r.ErrorMessage);
            }
            return new ValidationResult(sb.ToString(), mNames);
        }
    }
}
