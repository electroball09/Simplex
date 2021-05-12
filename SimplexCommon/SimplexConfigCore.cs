using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text;

namespace Simplex
{
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
