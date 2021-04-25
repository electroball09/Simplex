using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Simplex
{
    public static class SimplexConfigLoader
    {
        public static void LoadConfig<T>(this T obj, Func<string, string> loadFunc)
        {
            var props = obj.GetType().GetProperties();
            foreach (var prop in props)
            {
                var cfgClassAttr = prop.GetCustomAttribute<ConfigClassAttribute>();
                var cfgValAttr = prop.GetCustomAttribute<ConfigValueAttribute>();
                object newValue = null;
                if (cfgClassAttr != null)
                {
                    newValue = Activator.CreateInstance(prop.DeclaringType);
                    newValue.LoadConfig(loadFunc);
                }
                else if (cfgValAttr != null)
                {
                    newValue = cfgValAttr.LoadValue(prop.Name, loadFunc);
                }
                else
                {
                    return;
                }

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
        public override object LoadValue(string name, Func<string, string> fetchingFunc) => fetchingFunc(name).ToLower() == "true";
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
            return JsonSerializer.Deserialize(fetchingFunc(name), baseType);
        }
    }

    public abstract class SimplexConfigClassValidatorAttribute : ValidationAttribute
    {
        public override bool RequiresValidationContext => true;
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            ValidationContext ct = new ValidationContext(value);
            List<ValidationResult> results = new List<ValidationResult>();
            bool success = Validator.TryValidateObject(value, ct, results);
            if (success)
                return ValidationResult.Success;
            var mNames = new List<string>();
            foreach (var r in results)
                mNames.AddRange(r.MemberNames);
            return new ValidationResult("Class properties did not validate!", mNames);
        }
    }
}
